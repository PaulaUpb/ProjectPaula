using CodeComb.HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using ProjectPaula.DAL;
using System;
using System.Collections.Concurrent;
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
        public const string BaseUrl = "https://paul.uni-paderborn.de/";
        private const string _searchUrl = "https://paul.uni-paderborn.de/scripts/mgrqispi.dll?APPNAME=CampusNet&PRGNAME=ACTION&ARGUMENTS=-A6grKs5PHq2rFF2cazDrKQT4oecxio0CjK9Y7W9Jd3DdiHke0Qf8QZdI4tyCkNAXXLn5WwUf1J-8nbwl3GO3wniMX-TGs97==";
        private const string _dllUrl = "https://paul.uni-paderborn.de/scripts/mgrqispi.dll";
        private const string _categoryUrl = "https://paul.uni-paderborn.de/scripts/mgrqispi.dll?APPNAME=CampusNet&PRGNAME=EXTERNALPAGES&ARGUMENTS=-N000000000000001,-N000442,-Avvz";
        private static TimeZoneInfo _timezone;
        private SemaphoreSlim _writeLock = new SemaphoreSlim(1);
        private SemaphoreSlim requestSemaphore = new SemaphoreSlim(10);

        public PaulParser()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Accept-Language", "de-DE,de;q=0.8,en-US;q=0.5,en;q=0.3");
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            _client.DefaultRequestHeaders.Add("Accept", "text/html, application/xhtml+xml, image/jxr, */*");
            _client.DefaultRequestHeaders.Remove("Expect");
            _timezone = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(t => t.Id == "W. Europe Standard Time" || t.Id == "Europe/Berlin");
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
            await requestSemaphore.WaitAsync();
            var response = await _client.GetAsync(url);
            requestSemaphore.Release();
            var doc = new HtmlDocument();
            doc.Load(await response.Content.ReadAsStreamAsync());
            return doc;
        }



        public async Task UpdateAllCourses(List<Course> allCourses, DatabaseContext db)
        {
            try
            {
                var counter = 0;
                PaulRepository.AddLog("Update for all courses started!", FatilityLevel.Normal, "");

                var catalogs = await PaulRepository.GetCourseCataloguesAsync(db);
                foreach (var c in catalogs)
                {
                    var courseList = allCourses.Where(co => co.Catalogue.InternalID == c.InternalID).ToList();
                    counter = 1;
                    var messages = await Task.WhenAll(new[] { "1", "2" }.Select(useLogo => SendPostRequest(c.InternalID, "", useLogo)));
                    foreach (var message in messages)
                    {
                        var document = new HtmlDocument();
                        document.Load(await message.Content.ReadAsStreamAsync());
                        var pageResult = await GetPageSearchResult(document, db, c, counter, courseList);
                        try
                        {
                            while (pageResult.LinksToNextPages.Count > 0)
                            {

                                var docs = await Task.WhenAll(pageResult.LinksToNextPages.Select(s => SendGetRequest(BaseUrl + s)));
                                //Getting course list for maxiumum 3 pages
                                var courses = await Task.WhenAll(docs.Select(d => GetCourseList(db, d, c, courseList)));
                                //Get Details for all courses
                                await Task.WhenAll(courses.SelectMany(list => list.Select(course => GetCourseDetailAsync(course, db, courseList))));
                                await db.SaveChangesAsync();

                                await Task.WhenAll(courses.SelectMany(list => list.Select(course => GetTutorialDetailAsync(course, db))));
                                await Task.WhenAll(courses.SelectMany(list => list.SelectMany(s => s.ParsedConnectedCourses.Select(course => GetCourseDetailAsync(course, db, courseList, true)))));

                                await Task.WhenAll(courses.SelectMany(list => list.SelectMany(s => s.ParsedConnectedCourses.Select(course => GetTutorialDetailAsync(course, db)))));
                                PaulRepository.AddLog("Run completed: " + counter, FatilityLevel.Normal, "");
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
                                str.AppendLine("Entry involved: " + entry.Entity + " Type: " + entry.Entity.GetType().Name);
                            }

                            PaulRepository.AddLog($"DbUpdateConcurrency failure: {e} in {c.Title} at round {counter}", FatilityLevel.Critical, "Nightly Update");
                            PaulRepository.AddLog($"DbUpdateConcurrency failure: {str} in {c.Title}", FatilityLevel.Critical, "Nightly Update");

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
                    var url = td.ChildNodes.First(ch => ch.Name == "a").Attributes["href"].Value;
                    var trimmedUrl = WebUtility.HtmlDecode(url);
                    await _writeLock.WaitAsync();

                    Course c = courses.FirstOrDefault(course => course.Id == $"{catalogue.InternalID},{id}");
                    if (c == null)
                    {
                        c = new Course()
                        {
                            Name = name,
                            Docent = td.ChildNodes.Where(ch => ch.Name == "#text").Skip(1).First().InnerText.Trim('\r', '\t', '\n', ' '),
                            TrimmedUrl = url,
                            Catalogue = catalogue,
                            Id = $"{catalogue.InternalID},{id}",
                            InternalCourseID = id
                        };
                        //db.Courses.Add(c);
                        db.Entry(c).State = EntityState.Added;
                        courses.Add(c);
                        list.Add(c);
                    }
                    else
                    {
                        c.NewUrl = trimmedUrl;
                        if (!name.Equals(c.Name))
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
            if (navi == null) return PageSearchResult.Empty;

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
            HtmlDocument doc = await GetHtmlDocumentForCourse(course, db);
            if (doc == null) return;


            var changed = false;
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
            var dates = GetDates(doc, db).ToList();
            await UpdateDatesInDatabase(course, dates, db);
            await UpdateExamDates(doc, db, course);

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
                        c2 = new Course() { Name = name, TrimmedUrl = url, Catalogue = course.Catalogue, Id = $"{course.Catalogue.InternalID},{id}" };
                        //db.Courses.Add(c2);
                        db.Entry(c2).State = EntityState.Added;
                        list.Add(c2);

                    }

                    //prevent that two seperat theads add the connected courses

                    if (course.Id != c2.Id &&
                        !course.ParsedConnectedCourses.Any(co => co.Id == c2.Id) &&
                        !c2.ParsedConnectedCourses.Any(co => co.Id == course.Id))
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
                    return new Course() { Id = course.Id + $",{name}", Name = name, TrimmedUrl = url, CourseId = course.Id, IsTutorial = true, Catalogue = course.Catalogue };
                });

                foreach (var parsedTutorial in parsedTutorials)
                {
                    var tutorial = course.ParsedTutorials.FirstOrDefault(t => t == parsedTutorial);
                    if (tutorial != null) tutorial.NewUrl = parsedTutorial.TrimmedUrl;
                }

                var newTutorials = parsedTutorials.Except(course.ParsedTutorials).ToList();
                if (newTutorials.Any())
                {
                    await _writeLock.WaitAsync();
                    //db.Courses.AddRange(newTutorials);
                    foreach (var t in newTutorials)
                    {
                        db.Entry(t).State = EntityState.Added;
                    }

                    course.ParsedTutorials.AddRange(newTutorials);
                    _writeLock.Release();
                }

                var oldTutorials = course.ParsedTutorials.Except(parsedTutorials).ToList();

                if (oldTutorials.Any() && parsedTutorials.Any())
                {
                    await _writeLock.WaitAsync();
                    await db.Database.ExecuteSqlCommandAsync($"DELETE FROM Date Where CourseId IN ({string.Join(",", oldTutorials.Select(o => "'" + o.Id + "'"))})");
                    var selectedCourses = db.SelectedCourses.Where(p => oldTutorials.Any(o => o.Id == p.CourseId)).Include(s => s.Users).ThenInclude(u => u.User).ToList();
                    foreach (var selectedCourseUser in selectedCourses.SelectMany(s => s.Users))
                    {
                        await db.Database.ExecuteSqlCommandAsync($"DELETE FROM SelectedCourseUser Where UserId IN ({selectedCourseUser.User.Id}) And SelectedCourseId IN ({string.Join(",", selectedCourses.Select(s => "'" + s.Id + "'"))}) ");
                    }
                    await db.Database.ExecuteSqlCommandAsync($"DELETE FROM SelectedCourse Where CourseId IN ({string.Join(",", oldTutorials.Select(o => "'" + o.Id + "'"))})");

                    await db.Database.ExecuteSqlCommandAsync($"DELETE FROM Course Where Id IN ({string.Join(",", oldTutorials.Select(o => "'" + o.Id + "'"))})");
                    foreach (var old in oldTutorials)
                    {
                        course.ParsedTutorials.Remove(old);
                    }
                    _writeLock.Release();

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
                    HtmlDocument doc = await GetHtmlDocumentForCourse(t, db);
                    if (doc == null) return;

                    //Termine parsen
                    var dates = GetDates(doc, db).ToList();
                    await UpdateDatesInDatabase(t, dates, db);

                }
                catch
                {
                    //in case http request fails not the whole parsing run should fail
                }

            }
        }


        private async Task<HtmlDocument> GetHtmlDocumentForCourse(Course course, DatabaseContext db)
        {
            HtmlDocument doc = null;
            try
            {
                var response = await _client.GetAsync((BaseUrl + WebUtility.HtmlDecode(course.TrimmedUrl)));

                doc = new HtmlDocument();
                doc.Load(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
            }
            catch
            {
                try
                {
                    var response = await _client.GetAsync(BaseUrl + WebUtility.HtmlDecode(course.NewUrl));
                    doc = new HtmlDocument();
                    doc.Load(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
                    course.TrimmedUrl = course.NewUrl;
                    db.ChangeTracker.TrackObject(course);
                }
                catch
                {
                }
            }

            return doc;
        }
        static List<Date> GetDates(HtmlDocument doc, DatabaseContext db)
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

                        if (_timezone != null)
                        {
                            var tzOffset = _timezone.GetUtcOffset(date.DateTime);
                            date = new DateTimeOffset(date.DateTime, tzOffset);
                        }
                        else { PaulRepository.AddLog("Timezone not present", FatilityLevel.Critical, ""); }
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
            await _writeLock.WaitAsync();

            var difference = dates.Except(course.Dates, Date.StructuralComparer).ToList();
            var old = course.Dates.Except(dates, Date.StructuralComparer).ToList();


            if (difference.Any() && dates.Any())
            {
                difference.ForEach(d => d.CourseId = course.Id);
                foreach (var d in difference)
                {
                    db.Entry(d).State = EntityState.Added;
                }
                //db.Dates.AddRange(difference);
                course.DatesChanged = true;
            }

            if (old.Any() && dates.Any())
            {
                await db.Database.ExecuteSqlCommandAsync($"Delete from Date Where Id IN ({string.Join(",", old.Select(d => d.Id))})");
                //db.Dates.RemoveRange(old);
                course.DatesChanged = true;
            }

            _writeLock.Release();
        }

        public async Task UpdateExamDates(HtmlDocument doc, DatabaseContext db, Course course)
        {
            try
            {
                var list = new List<ExamDate>();
                var node = doc.GetElementbyId("contentlayoutleft");
                var tables = node.ChildNodes.Where(n => n.Name == "table");
                if (tables.Count() >= 5)
                {

                    var table = tables.ElementAt(4);
                    var trs = table.ChildNodes.Where(n => n.Name == "tr").Skip(1);
                    foreach (var tr in trs)
                    {
                        var dateString = tr.GetDescendantsByName("examDateTime").FirstOrDefault();
                        if (dateString != null)
                        {
                            var name = tr.GetDescendantsByName("examName").First().InnerText.TrimWhitespace();
                            var lastIndex = dateString.InnerText.LastIndexOf(' ');
                            var date = DateTimeOffset.Parse(dateString.InnerText.Substring(0, lastIndex).Replace("Mär", "Mar"), new CultureInfo("de-DE"));
                            if (_timezone != null)
                            {
                                var tzOffset = _timezone.GetUtcOffset(date.DateTime);
                                date = new DateTimeOffset(date.DateTime, tzOffset);
                            }
                            else { PaulRepository.AddLog("Timezone not present", FatilityLevel.Critical, ""); }

                            var time = dateString.InnerText.Substring(lastIndex, dateString.InnerText.Length - lastIndex);
                            var from = date.Add(TimeSpan.Parse(time.Split('-')[0]));
                            var toString = time.Split('-')[1];
                            DateTimeOffset to;
                            if (toString.Trim() != "24:00")
                            {
                                to = date.Add(TimeSpan.Parse(toString));
                            }
                            else
                            {
                                to = date.Add(new TimeSpan(23, 59, 59));
                            }

                            var instructor = tr.GetDescendantsByClass("tbdata")[3].InnerText.TrimWhitespace();
                            list.Add(new ExamDate() { From = from, To = to, Description = name, Instructor = instructor });
                        }
                    }

                    await _writeLock.WaitAsync();

                    var difference = list.Except(course.ExamDates).ToList();
                    var old = course.ExamDates.Except(list).ToList();


                    if (difference.Any() && list.Any())
                    {
                        difference.ForEach(d => d.CourseId = course.Id);
                        foreach (var d in difference)
                        {
                            db.Entry(d).State = EntityState.Added;
                        }
                    }

                    if (old.Any() && list.Any())
                    {
                        await db.Database.ExecuteSqlCommandAsync($"Delete from ExamDate Where Id IN ({string.Join(",", old.Select(d => d.Id))})");
                    }

                    _writeLock.Release();
                }
            }
            catch (Exception)
            {

            }
        }


        public async Task UpdateCategoryFilters(List<Course> allCourses, DatabaseContext context)
        {
            PaulRepository.AddLog("Update for category filters has started!", FatilityLevel.Normal, "Update category filters");
            var catalogues = (await PaulRepository.GetCourseCataloguesAsync(context));
            foreach (var cat in catalogues)
            {
                try
                {
                    await UpdateCategoryFiltersForCatalog(cat, allCourses, context);
                }
                catch (Exception e)
                {
                    PaulRepository.AddLog($"Updating Categories failed: {e}", FatilityLevel.Critical, "Nightly Update");
                }
            }

            PaulRepository.AddLog("Update for category filters completed!", FatilityLevel.Normal, "Update category filters");

        }

        private async Task UpdateCategoryFiltersForCatalog(CourseCatalog cat, List<Course> allCourses, DatabaseContext db)
        {
            var doc = await SendGetRequest(_categoryUrl);
            var navi = doc.GetElementbyId("pageTopNavi");
            var links = navi.Descendants().Where((d) => d.Name == "a" && d.Attributes.Any(a => a.Name == "class" && a.Value.Contains("depth_2")));
            var modifiedCatalogText = cat.ShortTitle.Replace("WS", "Winter").Replace("SS", "Sommer");
            if (links.Any(l => l.InnerText == modifiedCatalogText))
            {
                var url = links.First(l => l.InnerText == modifiedCatalogText).Attributes["href"].Value;
                doc = await SendGetRequest(BaseUrl + WebUtility.HtmlDecode(url));
                var nodes = GetNodesForCategories(doc);
                var parentCategories = await UpdateCategoriesInDatabase(db, null, nodes, doc, true, cat, allCourses);
                do
                {
                    var tasks = parentCategories.Keys.Select(node => UpdateCategoryForHtmlNode(db, node, parentCategories[node], cat, allCourses)).ToList();
                    parentCategories = (await Task.WhenAll(tasks)).SelectMany(r => r).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    await db.SaveChangesAsync();
                } while (parentCategories.Keys.Any());

            }


        }

        private async Task<Dictionary<HtmlNode, CategoryFilter>> UpdateCategoryForHtmlNode(DatabaseContext db, HtmlNode node, CategoryFilter category, CourseCatalog cat, List<Course> allCourses)
        {
            var dict = new Dictionary<HtmlNode, CategoryFilter>();
            try
            {
                var url = BaseUrl + WebUtility.HtmlDecode(node.Attributes["href"].Value);
                var doc = await SendGetRequest(url);
                var newNodes = GetNodesForCategories(doc);
                dict = await UpdateCategoriesInDatabase(db, category, newNodes, doc, false, cat, allCourses);
            }
            catch (Exception)
            {

            }
            return dict;
        }

        private async Task<Dictionary<HtmlNode, CategoryFilter>> UpdateCategoriesInDatabase(DatabaseContext db, CategoryFilter currentFilter, IEnumerable<HtmlNode> nodes, HtmlDocument doc, bool isTopLevel, CourseCatalog cat, List<Course> allCourses)
        {
            var dict = new Dictionary<HtmlNode, CategoryFilter>();
            List<CategoryFilter> topLevelCategories = null;
            foreach (var node in nodes)
            {
                var title = node.InnerText.Trim();
                if (isTopLevel)
                {
                    if (topLevelCategories == null) topLevelCategories = db.CategoryFilters.IncludeAll().ToList();
                    var category = topLevelCategories.FirstOrDefault(n => n.IsTopLevel && n.CourseCatalog.InternalID == cat.InternalID && n.Title == title);
                    if (category == null)
                    {
                        category = new CategoryFilter { Title = title, CourseCatalog = cat, IsTopLevel = isTopLevel };
                        db.Entry(category).State = EntityState.Added;
                    }

                    dict[node] = category;

                }
                else
                {
                    var filter = currentFilter.Subcategories.FirstOrDefault(c => c.Title == title);
                    if (filter == null)
                    {
                        //we found a new category
                        await _writeLock.WaitAsync();

                        filter = new CategoryFilter() { Title = title, IsTopLevel = isTopLevel, CourseCatalog = cat };
                        currentFilter?.Subcategories.Add(filter);
                        var entry = db.ChangeTracker.Entries().FirstOrDefault(e => e.Entity == currentFilter);
                        if (entry?.State != EntityState.Added) entry.State = EntityState.Modified;

                        db.Entry(filter).State = EntityState.Added;
                        _writeLock.Release();
                    }

                    dict[node] = filter;
                }

            }

            var courses = await GetCourseList(db, doc, cat, allCourses);

            if (courses.Any() && currentFilter != null)
            {
                foreach (var course in courses)
                {
                    await _writeLock.WaitAsync();

                    if (!currentFilter.ParsedCourses.Any(c => c.CourseId == course.Id))
                    {
                        var catCourse = new CategoryCourse() { Category = currentFilter, CourseId = course.Id };
                        db.Entry(catCourse).State = EntityState.Added;
                        currentFilter.ParsedCourses.Add(catCourse);
                    }
                    _writeLock.Release();

                }
            }

            return dict;
        }

        private List<HtmlNode> GetNodesForCategories(HtmlDocument doc)
        {
            var list = doc.GetElementbyId("auditRegistration_list");
            if (list == null) return new List<HtmlNode>();
            var li = list.ChildNodes.Where(n => n.Name == "li");
            return li.Where(l => l.FirstChild != null).Select(l => l.FirstChild).ToList();
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
