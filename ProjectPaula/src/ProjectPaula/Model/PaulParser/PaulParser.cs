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



        public async Task UpdateAllCourses(List<Course> allCourses)
        {
            try
            {
                var counter = 0;
                PaulRepository.AddLog("Update for all courses started!", FatilityLevel.Normal, "");

                var catalogues = (await PaulRepository.GetCourseCataloguesAsync()).Take(2);
                foreach (var c in catalogues)
                {
                    using (var db = new DatabaseContext(PaulRepository.Filename))
                    {
                        var courseList = allCourses.Where(co => co.Catalogue.InternalID == c.InternalID).ToList();
                        counter = 1;
                        var message = await SendPostRequest(c.InternalID, "", "2");
                        var document = new HtmlDocument();
                        document.Load(await message.Content.ReadAsStreamAsync());
                        var pageResult = await GetPageSearchResult(document, db, c, counter, courseList);
                        try
                        {
                            while (pageResult.LinksToNextPages.Count > 0)
                            {

                                var docs = await Task.WhenAll(pageResult.LinksToNextPages.Select(s => SendGetRequest(_baseUrl + s)));
                                //Getting course list for maxiumum 3 pages
                                var courses = await Task.WhenAll(docs.Select(d => GetCourseList(db, d, c, courseList)));
                                //Get Details for all courses
                                await Task.WhenAll(courses.SelectMany(list => list.Select(course => GetCourseDetailAsync(course, db, courseList))));
                                await db.SaveChangesAsync();

                                await Task.WhenAll(courses.SelectMany(list => list.Select(course => GetTutorialDetailAsync(course, db))));
                                await Task.WhenAll(courses.SelectMany(list => list.SelectMany(s => s.ParsedConnectedCourses.Select(course => GetCourseDetailAsync(course, db, courseList, true)))));

                                await Task.WhenAll(courses.SelectMany(list => list.SelectMany(s => s.ParsedConnectedCourses.Select(course => GetTutorialDetailAsync(course, db)))));
                                db.Logs.Add(new Log() { Message = "Run completed: " + counter, Date = DateTime.Now });
                                await db.SaveChangesAsync();
                                counter += pageResult.LinksToNextPages.Count;
                                pageResult = await GetPageSearchResult(docs.Last(), db, c, counter, courseList);
                            }

                        }
                        catch (DbUpdateConcurrencyException e)
                        {
                            //db.ChangeTracker.Entries().First(entry => entry.Equals(e)).State == EntityState.Detached;
                            var str = new StringBuilder();
                            foreach (var entry in e.Entries)
                            {
                                str.AppendLine("Entry involved: " + entry.Entity.ToString() + " Type: " + entry.Entity.GetType().Name);
                            }

                            PaulRepository.AddLog("DbUpdateConcurrency failure: " + e.ToString() + " in " + c.Title + " at round " + counter, FatilityLevel.Critical, "Nightly Update");
                            PaulRepository.AddLog("DbUpdateConcurrency failure: " + str.ToString() + " in " + c.Title, FatilityLevel.Critical, "Nightly Update");

                        }
                        catch (Exception e)
                        {

                            PaulRepository.AddLog("Update failure: " + e.ToString() + " in " + c.Title, FatilityLevel.Critical, "Nightly Update");
                        }
                    }

                }

                PaulRepository.AddLog("Update completed!", FatilityLevel.Normal, "");
            }
            catch
            {
                //In case logging failes,server shouldn't crash
            }



        }

        public async Task<PageSearchResult> GetCourseSearchDataAsync(CourseCatalog catalogue, string search, DatabaseContext db, List<Course> courses = null)
        {
            var message = await SendPostRequest(catalogue.InternalID, search);
            HtmlDocument doc = new HtmlDocument();
            doc.Load(await message.Content.ReadAsStreamAsync(), Encoding.UTF8);
            return await GetPageSearchResult(doc, db, catalogue, 1, courses);

        }

        private async Task<List<Course>> GetCourseList(DatabaseContext db, HtmlDocument doc, CourseCatalog catalogue, List<Course> courses)
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
                    await _writeLock.WaitAsync();

                    Course c = courses.FirstOrDefault(course => course.Id == $"{catalogue.InternalID},{id}");
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
                        db.Courses.Add(c);
                        courses.Add(c);
                        list.Add(c);
                    }
                    else
                    {
                        if (name != c.Name)
                        {
                            c.Name = name;
                            db.ChangeTracker.TrackObject(c);
                        }

                        list.Add(c);
                    }

                    _writeLock.Release();

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

        public async Task GetCourseDetailAsync(Course course, DatabaseContext db, List<Course> list, bool isConnectedCourse = false)
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
                if (course.ParsedConnectedCourses.All(c => c.Name.Length > course.Name.Length))
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
            var dates = GetDates(doc).ToList();
            await UpdateDatesInDatabase(course, dates, db);


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

                    await _writeLock.WaitAsync();
                    Course c2 = list.FirstOrDefault(co => co.Id == $"{course.Catalogue.InternalID},{id}");
                    if (c2 == null)
                    {
                        c2 = new Course() { Name = name, Url = url, Catalogue = course.Catalogue, Id = $"{course.Catalogue.InternalID},{id}" };
                        db.Courses.Add(c2);
                        list.Add(c2);

                    }

                    //prevent that two seperat theads add the connected courses

                    if (course.Id != c2.Id && !course.ParsedConnectedCourses.Any(co => co.Id == c2.Id) && !c2.ParsedConnectedCourses.Any(co => co.Id == course.Id))
                    {
                        var con1 = new ConnectedCourse() { CourseId = course.Id, CourseId2 = c2.Id };
                        course.ParsedConnectedCourses.Add(c2);
                        db.ConnectedCourses.Add(con1);

                        var con2 = new ConnectedCourse() { CourseId = c2.Id, CourseId2 = course.Id };
                        c2.ParsedConnectedCourses.Add(course);
                        db.ConnectedCourses.Add(con2);
                    }

                    _writeLock.Release();
                }
            }

            //Gruppen parsen
            var groups = divs.FirstOrDefault(l => l.InnerHtml.Contains("Kleingruppe anzeigen"))?.ChildNodes.Where(l => l.Name == "li");
            if (groups != null)
            {

                var parsedTutorials = groups.Select(group =>
                {

                    var name = group.Descendants().First(n => n.Name == "strong")?.InnerText;
                    var url = group.Descendants().First(n => n.Name == "a")?.Attributes["href"].Value;
                    return new Course() { Id = course.Id + $",{name}", Name = name, Url = url, CourseId = course.Id, IsTutorial = true, Catalogue = course.Catalogue };
                });

                var newTutorials = parsedTutorials.Except(course.ParsedTutorials);
                if (newTutorials.Any())
                {
                    db.Courses.AddRange(newTutorials);
                    course.ParsedTutorials.AddRange(newTutorials);
                }

                var oldTutorials = course.ParsedTutorials.Except(parsedTutorials);

                if (oldTutorials.Any() && parsedTutorials.Any())
                {
                    await db.Database.ExecuteSqlCommandAsync($"DELETE FROM Date Where CourseId IN ({string.Join(",", oldTutorials.Select(o => "'" + o.Id + "'"))})");
                    await db.Database.ExecuteSqlCommandAsync($"DELETE FROM Course Where Id IN ({string.Join(",", oldTutorials.Select(o => "'" + o.Id + "'"))})");
                    foreach (var old in oldTutorials) course.ParsedTutorials.Remove(old);

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
            foreach (var t in c.ParsedTutorials)
            {
                try
                {
                    var res = await _client.GetAsync((_baseUrl + WebUtility.HtmlDecode(t.Url)));

                    HtmlDocument d = new HtmlDocument();
                    d.Load(await res.Content.ReadAsStreamAsync(), Encoding.UTF8);

                    //Termine parsen
                    var dates = GetDates(d).ToList();
                    await UpdateDatesInDatabase(t, dates, db);

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
                    if (!tr.GetDescendantsByName("appointmentDate").First().InnerText.Contains('*'))
                    {
                        //Umlaute werden falsch geparst, deshalb werden Umlaute ersetzt
                        var date = DateTimeOffset.Parse(tr.GetDescendantsByName("appointmentDate").First().InnerText.Replace("Mär", "Mar"), new CultureInfo("de-DE"));
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
            }

            return list;
        }

        private async Task UpdateDatesInDatabase(Course course, List<Date> dates, DatabaseContext db)
        {
            var difference = dates.Except(course.Dates).ToList();
            var old = course.Dates.Except(dates).ToList();

            await _writeLock.WaitAsync();
            if (difference.Any() && dates.Any())
            {
                difference.ForEach(d => d.Course = course);
                db.Dates.AddRange(difference);
                course.DatesChanged = true;
            }

            if (old.Any() && dates.Any())
            {
                await db.Database.ExecuteSqlCommandAsync($"Delete from Date Where Id IN ({String.Join(",", old.Select(d => d.Id))})");
                //db.Dates.RemoveRange(old);
                course.DatesChanged = true;
            }

            _writeLock.Release();
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
