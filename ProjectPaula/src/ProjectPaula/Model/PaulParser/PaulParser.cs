using CodeComb.HtmlAgilityPack;
using Microsoft.Data.Entity;
using ProjectPaula.DAL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectPaula.Model.PaulParser
{
    class PaulParser
    {
        private HttpClient _client;
        private const string _searchUrl = "https://paul.uni-paderborn.de/scripts/mgrqispi.dll?APPNAME=CampusNet&PRGNAME=ACTION&ARGUMENTS=-A6grKs5PHq2rFF2cazDrKQT4oecxio0CjK9Y7W9Jd3DdiHke0Qf8QZdI4tyCkNAXXLn5WwUf1J-8nbwl3GO3wniMX-TGs97==";
        private const string _dllUrl = "https://paul.uni-paderborn.de/scripts/mgrqispi.dll";
        private const string _baseUrl = "https://paul.uni-paderborn.de/";
        private SemaphoreSlim _writeLock = new SemaphoreSlim(1);

        public PaulParser()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Accept-Language", "de-DE,de;q=0.8,en-US;q=0.5,en;q=0.3");
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            _client.DefaultRequestHeaders.Add("Accept", "text/html, application/xhtml+xml, image/jxr, */*");
            _client.DefaultRequestHeaders.Remove("Expect");
        }

        public async Task<IEnumerable<CourseCatalog>> GetAvailabeCourseCatalogs()
        {
            HtmlDocument doc = new HtmlDocument();
            doc.Load(await _client.GetStreamAsync(_searchUrl), Encoding.UTF8);
            var catalogue = doc.GetElementbyId("course_catalogue");
            var options = catalogue.Descendants().Where(c => c.Name == "option" && c.Attributes.Any(a => a.Name == "title" && a.Value.Contains("Vorlesungsverzeichnis")));
            return options.Select(n => new CourseCatalog() { InternalID = n.Attributes["value"].Value, Title = n.Attributes["title"].Value });
        }

        private async Task<HttpResponseMessage> SendPostRequest(string couseCatalogueId, string search, string logo = "0")
        {
            var par = $"APPNAME=CampusNet&PRGNAME=ACTION&ARGUMENTS=ARGS_SEARCHCOURSE&ARGS_SEARCHCOURSE=6grKs5PHq2rFF2cazDrKQT4oecxio0CjK9Y7W9Jd3DdiHke0Qf8QZdI4tyCkNAXXLn5WwUf1J-8nbwl3GO3wniMX-TGs97%3D%3D&sessionno=000000000000001&menuid=000443&submit_search=Suche&course_catalogue={couseCatalogueId}&course_catalogue_section=0&faculty=0&course_type=0&course_number=&course_name=&course_short_name=&with_logo={logo}&module_number=&module_name=&instructor_firstname=&instructor_surname=&free_text={search}";

            return await _client.PostAsync(_dllUrl, new StringContent(par));
        }

        private async Task<HtmlDocument> SendGetRequest(string url)
        {
            var response = await _client.GetAsync(url);
            var doc = new HtmlDocument();
            doc.Load(await response.Content.ReadAsStreamAsync());
            return doc;
        }



        public async Task UpdateAllCourses(DatabaseContext db, List<Course> l)
        {

            db.Logs.Add(new Log() { Message = "Update for all courses started!", Date = DateTime.Now });

            var catalogues = (await PaulRepository.GetCourseCataloguesAsync()).Take(2);
            foreach (var c in catalogues)
            {
                var counter = 1;
                var message = await SendPostRequest(c.InternalID, "", "2");
                var document = new HtmlDocument();
                document.Load(await message.Content.ReadAsStreamAsync());
                var pageResult = await GetPageSearchResult(document, db, c, counter, l);
                try
                {
                    while (pageResult.LinksToNextPages.Count > 0)
                    {

                        var docs = await Task.WhenAll(pageResult.LinksToNextPages.Select(s => SendGetRequest(_baseUrl + s)));
                        //Getting course list for maxiumum 3 pages
                        var courses = await Task.WhenAll(docs.Select(d => GetCourseList(db, d, c, l)));
                        //Get Details for all courses
                        await Task.WhenAll(courses.SelectMany(list => list.Select(course => GetCourseDetailAsync(course, db, l))));
                        await db.SaveChangesAsync();

                        await Task.WhenAll(courses.SelectMany(list => list.Select(course => GetTutorialDetailAsync(course, db))));
                        await Task.WhenAll(courses.SelectMany(list => list.SelectMany(s => s.ConnectedCourses.Select(course => GetCourseDetailAsync(course, db, l, true)))));

                        await Task.WhenAll(courses.SelectMany(list => list.SelectMany(s => s.ConnectedCourses.Select(course => GetTutorialDetailAsync(course, db)))));
                        db.Logs.Add(new Log() { Message = "Run completed: " + counter, Date = DateTime.Now });
                        await db.SaveChangesAsync();
                        counter += pageResult.LinksToNextPages.Count;
                        pageResult = await GetPageSearchResult(docs.Last(), db, c, counter, l);
                    }

                }
                catch (Exception e)
                {
                    PaulRepository.AddLog("Update failure: " + e.ToString() + " in " + c.Title, FatilityLevel.Critical, "Nightly Update");
                }

            }

            PaulRepository.AddLog("Update completed!", FatilityLevel.Normal, "");

        }

        public async Task<PageSearchResult> GetCourseSearchDataAsync(CourseCatalog catalogue, string search, DatabaseContext db, List<Course> courses = null)
        {
            var message = await SendPostRequest(catalogue.InternalID, search);
            HtmlDocument doc = new HtmlDocument();
            doc.Load(await message.Content.ReadAsStreamAsync(), Encoding.UTF8);
            return await GetPageSearchResult(doc, db, catalogue, 1, courses);

        }

        private async Task<List<Course>> GetCourseList(DatabaseContext db, HtmlDocument doc, CourseCatalog catalogue, List<Course> courses = null)
        {
            var list = new List<Course>();
            var data = doc.DocumentNode.Descendants().Where((d) => d.Name == "tr" && d.Attributes.Any(a => a.Name == "class" && a.Value == "tbdata"));


            foreach (var tr in data)
            {
                try
                {
                    var td = tr.ChildNodes.Where(ch => ch.Name == "td").Skip(1).First();
                    var text = td.ChildNodes.First(ch => ch.Name == "a").InnerText;
                    var name = text.Split(new char[] { ' ' }, 2)[1];
                    var id = text.Split(new char[] { ' ' }, 2)[0];
                    Course c;
                    if (courses == null)
                    {
                        c = db.Courses.IncludeAll().LocalChanges(db).FirstOrDefault(course => course.Id == $"{catalogue.InternalID},{id}");
                    }
                    else
                    {
                        c = courses.FirstOrDefault(course => course.Id == $"{catalogue.InternalID},{id}");

                    }

                    if (c == null)
                    {
                        c = new Course()
                        {
                            Name = name,
                            Docent = td.ChildNodes.Where(ch => ch.Name == "#text").Skip(1).First().InnerText.Trim('\r', '\t', '\n', ' '),
                            Url = td.ChildNodes.First(ch => ch.Name == "a").Attributes["href"].Value,
                            Catalogue = catalogue,
                            Id = $"{catalogue.InternalID},{id}",
                            InternalCourseID = id
                        };
                        await _writeLock.WaitAsync();
                        db.Courses.Add(c);
                        courses.Add(c);
                        list.Add(c);
                        _writeLock.Release();
                    }
                    else list.Add(c);

                }
                catch { /*something went wrong while parsing for example there's no name*/ }
            }

            return list;
        }

        private async Task<PageSearchResult> GetPageSearchResult(HtmlDocument doc, DatabaseContext db, CourseCatalog catalogue, int number, List<Course> courses)
        {
            var navi = doc.GetElementbyId("searchCourseListPageNavi");
            var next = navi.ChildNodes.Where(c => c.Name == "a").SkipWhile(h => h.InnerText != number.ToString());


            var result = new PageSearchResult()
            {
                Courses = await GetCourseList(db, doc, catalogue, courses),
                LinksToNextPages = next.Skip(1).Take(Math.Min(3, next.Count() - 1)).Select(h => WebUtility.HtmlDecode(h.Attributes["href"].Value)).ToList()
            };

            return result;
        }

        public async Task GetCourseDetailAsync(Course course, DatabaseContext db, List<Course> list = null, bool isConnectedCourse = false)
        {
            HtmlDocument doc = null;
            bool changed = false;
            try
            {
                var response = await _client.GetAsync((_baseUrl + WebUtility.HtmlDecode(course.Url)));

                doc = new HtmlDocument();
                doc.Load(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
            }
            catch
            { //In case the web request fails return
                return;
            }

            //case of isConnectedCourse is set to false (on PAUL website) is not handled
            if (isConnectedCourse)
            {
                if (course.ConnectedCourses.All(c => c.Name.Length > course.Name.Length))
                {
                    var valueBefore = course.IsConnectedCourse;
                    course.IsConnectedCourse = false;
                    if (course.IsConnectedCourse != valueBefore) changed = true;
                }
                else if (course.IsConnectedCourse != isConnectedCourse)
                {
                    course.IsConnectedCourse = isConnectedCourse;
                    changed = true;
                }
            }

            //Update InternalID if not set before (migration code)
            if (course.InternalCourseID == null)
            {
                course.InternalCourseID = course.Id.Split(',')[1];
                changed = true;
            }

            //Get Shortname
            var descr = doc.DocumentNode.GetDescendantsByName("shortdescription").FirstOrDefault();
            if (descr != null && course.ShortName != descr.Attributes["value"].Value)
            {
                course.ShortName = descr.Attributes["value"].Value;
                changed = true;
            }

            //Termine parsen
            var dates = GetDates(doc);
            var difference = dates.Except(course.Dates).ToList();
            if (difference.Any() && dates.Any())
            {
                await _writeLock.WaitAsync();
                dates.ForEach(d => d.Course = course);
                db.Dates.AddRange(difference);
                course.Dates.AddRange(difference);
                _writeLock.Release();
                course.DatesChanged = true;
            }

            var old = course.Dates.Except(dates).ToList();
            if (old.Any() && dates.Any())
            {
                await _writeLock.WaitAsync();
                foreach (var o in old)
                {
                    course.Dates.Remove(o);
                }
                db.Dates.RemoveRange(old);
                _writeLock.Release();
                course.DatesChanged = true;
            }


            //Verbundene Veranstaltungen parsen
            var divs = doc.DocumentNode.GetDescendantsByClass("dl-ul-listview");
            var courses = divs.FirstOrDefault(l => l.InnerHtml.Contains("Veranstaltung anzeigen"))?.ChildNodes.Where(l => l.Name == "li" && l.InnerHtml.Contains("Veranstaltung anzeigen"));
            if (courses != null)
            {
                foreach (var c in courses)
                {
                    var text = c.Descendants().First(n => n.Name == "strong")?.InnerText;
                    var name = text.Split(new char[] { ' ' }, 2)[1];
                    var id = text.Split(new char[] { ' ' }, 2)[0];
                    var url = c.Descendants().First(n => n.Name == "a")?.Attributes["href"].Value;
                    var docent = c.Descendants().Where(n => n.Name == "p").Skip(2).First().InnerText;
                    Course c2;
                    if (list == null)
                    {
                        c2 = db.Courses.Include(d => d.ConnectedCoursesInternal).Include(d => d.Catalogue).Include(d => d.Tutorials).Include(d => d.Dates).ToList().LocalChanges(db).FirstOrDefault(co => co.Id == $"{course.Catalogue.InternalID},{id}");
                    }
                    else
                    {
                        c2 = list.FirstOrDefault(co => co.Id == $"{course.Catalogue.InternalID},{id}");
                    }

                    if (c2 == null)
                    {
                        c2 = new Course() { Name = name, Url = url, Catalogue = course.Catalogue, Id = $"{course.Catalogue.InternalID},{id}" };
                        await _writeLock.WaitAsync();
                        db.Courses.Add(c2);
                        list.Add(c2);
                        _writeLock.Release();

                    }

                    //prevent that two seperat theads add the connected courses
                    await _writeLock.WaitAsync();
                    if (course.Id != c2.Id && !course.ConnectedCoursesInternal.Any(co => co.CourseId2 == c2.Id))
                    {
                        var con1 = new ConnectedCourse() { CourseId2 = c2.Id };
                        course.ConnectedCoursesInternal.Add(con1);
                        db.ConnectedCourses.Add(con1);

                        var con2 = new ConnectedCourse() { CourseId2 = course.Id };
                        c2.ConnectedCoursesInternal.Add(con2);
                        db.ConnectedCourses.Add(con2);
                    }

                    _writeLock.Release();
                }
            }

            //Gruppen parsen
            var groups = divs.FirstOrDefault(l => l.InnerHtml.Contains("Kleingruppe anzeigen"))?.ChildNodes.Where(l => l.Name == "li");
            if (groups != null)
            {
                foreach (var group in groups)
                {
                    var name = group.Descendants().First(n => n.Name == "strong")?.InnerText;
                    var url = group.Descendants().First(n => n.Name == "a")?.Attributes["href"].Value;
                    Course t;
                    if (course.Tutorials.Any(tut => tut.Name == name))
                    {
                        t = course.Tutorials.First(tut => tut.Name == name);
                    }
                    else
                    {
                        t = new Course() { Id = course.Id + $",{name}", Name = name, Url = url };
                        await _writeLock.WaitAsync();
                        course.Tutorials.Add(t);
                        t.Catalogue = course.Catalogue;
                        t.IsTutorial = true;
                        //db.Courses.Add(t);
                        db.ChangeTracker.TrackObject(course);
                        _writeLock.Release();
                    }
                }
            }

            //mark course as modified
            if (changed)
            {
                await _writeLock.WaitAsync();
                db.ChangeTracker.TrackObject(course);
                _writeLock.Release();
            }

        }

        public async Task GetTutorialDetailAsync(Course c, DatabaseContext db)
        {
            foreach (var t in c.Tutorials)
            {
                try
                {
                    var res = await _client.GetAsync((_baseUrl + WebUtility.HtmlDecode(t.Url)));

                    HtmlDocument d = new HtmlDocument();
                    d.Load(await res.Content.ReadAsStreamAsync(), Encoding.UTF8);

                    //Termine parsen
                    var dates = GetDates(d).Except(c.Dates);
                    var difference = dates.Except(t.Dates).ToList();
                    if (difference.Any() && dates.Any())
                    {
                        await _writeLock.WaitAsync();
                        difference.ForEach(date => date.Course = t);
                        t.Dates.AddRange(difference);
                        db.Dates.AddRange(difference);
                        _writeLock.Release();
                        t.DatesChanged = true;
                    }

                    var old = t.Dates.Except(dates).ToList();
                    if (old.Any() && dates.Any())
                    {
                        await _writeLock.WaitAsync();
                        foreach (var o in old)
                        {
                            t.Dates.Remove(o);
                        }
                        db.Dates.RemoveRange(old);
                        _writeLock.Release();
                        t.DatesChanged = true;
                    }

                }
                catch
                {
                    //in case http request fails not the whole parsing run should fail
                }

            }
        }

        static List<Date> GetDates(HtmlDocument doc)
        {
            var list = new List<Date>();
            var tables = doc.DocumentNode.GetDescendantsByClass("tb list");
            var table = tables.FirstOrDefault(t => t.ChildNodes.Any(n => n.InnerText == "Termine"));
            var trs = table.ChildNodes.Where(n => n.Name == "tr").Skip(1);
            if (!table.InnerHtml.Contains("Es liegen keine Termine vor"))
            {
                foreach (var tr in trs)
                {
                    //Umlaute werden falsch geparst, deshalb werden Umlaute ersetzt
                    var date = DateTimeOffset.Parse(tr.GetDescendantsByName("appointmentDate").First().InnerText.Trim('*').Replace("Mär", "Mar"), new CultureInfo("de-DE"));
                    var fromNode = tr.GetDescendantsByName("appointmentTimeFrom").First();
                    var toNode = tr.GetDescendantsByName("appointmentDateTo").First();
                    var from = date.Add(TimeSpan.Parse(fromNode.InnerText));
                    DateTimeOffset to;
                    if (toNode.InnerText.Trim() != "24:00")
                    {
                        to = date.Add(TimeSpan.Parse(toNode.InnerText));
                    }
                    else
                    {
                        to = date.Add(new TimeSpan(23, 59, 59));
                    }
                    var room = tr.GetDescendantsByName("appointmentRooms").FirstOrDefault()?.InnerText;
                    if (room == null)
                    {
                        room = tr.Descendants("td").Skip(4).FirstOrDefault()?.InnerText.TrimWhitespace();
                    }

                    var instructor = tr.GetDescendantsByName("appointmentInstructors").First().InnerText.TrimWhitespace();
                    list.Add(new Date() { From = from, To = to, Room = room, Instructor = instructor });
                }
            }

            return list;
        }
    }
    static class ExtensionMethods
    {
        public static List<CodeComb.HtmlAgilityPack.HtmlNode> GetDescendantsByClass(this CodeComb.HtmlAgilityPack.HtmlNode node, string c)
        {
            return node.Descendants().Where((d) => d.Attributes.Any(a => a.Name == "class" && a.Value == c)).ToList();
        }

        public static List<CodeComb.HtmlAgilityPack.HtmlNode> GetDescendantsByName(this CodeComb.HtmlAgilityPack.HtmlNode node, string n)
        {
            return node.Descendants().Where((d) => d.Attributes.Any(a => a.Name == "name" && a.Value == n)).ToList();
        }

        public static string TrimWhitespace(this string s)
        {
            return s.Trim('\r', '\t', '\n', ' ');
        }

    }
}
