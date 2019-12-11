using CodeComb.HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using ProjectPaula.DAL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectPaula.Model.PaulParser
{
    /// <summary>
    /// This class is used for all parsing that is related to the PAUL website. This includes parsing the information for courses as well as parsing category filters.
    /// </summary>
    class PaulParser
    {
        private readonly HttpClient _client;
        public const string BaseUrl = "https://paul.uni-paderborn.de/";
        private const string _searchUrl = "https://paul.uni-paderborn.de/scripts/mgrqispi.dll?APPNAME=CampusNet&PRGNAME=ACTION&ARGUMENTS=-Ap7M9FqkfZaGFsKUSCWM2rhdYTMKOKb9tXehxOp-pZxLyxePFW7q0q13qvsLDMLK5Wyep9zDay2lPd28PpwaHklhhvK1Hqyfs2Vw5X2HcDJwXLRfWYstyeFxd0MDxDWIEbez2dM6ZgYJsEk~hyZh2J42pmtFg";
        private const string _dllUrl = "https://paul.uni-paderborn.de/scripts/mgrqispi.dll";
        private const string _categoryUrl = "https://paul.uni-paderborn.de/scripts/mgrqispi.dll?APPNAME=CampusNet&PRGNAME=EXTERNALPAGES&ARGUMENTS=-N000000000000001,-N000442,-Avvz";
        private static TimeZoneInfo _timezone;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _requestSemaphore = new SemaphoreSlim(10);
        private readonly Dictionary<CourseCatalog, ISet<string>> _seenCourseIdsByCatalog = new Dictionary<CourseCatalog, ISet<string>>();

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

        /// <summary>
        /// This method checks PAUL for the course catalogs that are available. It parses PAUL's search website and considers the entries in the dropdown that contain the name "Vorlesungsverzeichnis"
        /// </summary>
        /// <returns>List of course catalogs</returns>
        public async Task<IEnumerable<CourseCatalog>> GetAvailableCourseCatalogs()
        {
            var doc = new HtmlDocument();
            doc.Load(await _client.GetStreamAsync(_searchUrl), Encoding.UTF8);
            var catalogue = doc.GetElementbyId("course_catalogue");

            if (catalogue == null)
            {
                PaulRepository.AddLog("No course catalogs could be found! Maybe the search url has changed?", FatalityLevel.Critical, "");
                throw new ArgumentException("No course catalogs could be found in PAUL!", nameof(_searchUrl));
            }

            var options = catalogue.Descendants().Where(c => c.Name == "option" && c.Attributes.Any(a => a.Name == "title" && a.Value.Contains("Vorlesungsverzeichnis")));
            return options.Select(n => new CourseCatalog { InternalID = n.Attributes["value"].Value, Title = n.Attributes["title"].Value });
        }

        /// <summary>
        /// This helper method is used to send a search POST request to PAUL. The resulting website is expected to be the list of courses
        /// </summary>
        /// <param name="couseCatalogueId">Id of the course catalog that should be searched</param>
        /// <param name="search">Optional search string</param>
        /// <param name="logo">Logo string that determines if only courses with logo should be searched. Default value is 0</param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> SendPostRequest(string couseCatalogueId, string search, string logo = "0")
        {
            var doc = new HtmlDocument();
            doc.Load(await _client.GetStreamAsync(_searchUrl), Encoding.UTF8);
            var input = doc.DocumentNode.GetDescendantsByName("ARGS_SEARCHCOURSE").FirstOrDefault();
            var token = input?.GetAttributeValue("value", "");

            var par = $"APPNAME=CampusNet&PRGNAME=ACTION&ARGUMENTS=ARGS_SEARCHCOURSE&ARGS_SEARCHCOURSE={token}&menuid=000443&submit_search=Suche&course_catalogue={couseCatalogueId}&course_catalogue_section=0&faculty=0&course_type=0&course_number=&course_name=&course_short_name=&with_logo={logo}&module_number=&module_name=&instructor_firstname=&instructor_surname=&free_text={WebUtility.UrlEncode(search)}";

            return await _client.PostAsync(_dllUrl, new StringContent(par));
        }

        /// <summary>
        /// Small helper method that sends a GET request and returns an HtmlDocument for parsing
        /// </summary>
        /// <param name="url">Url of website that should be parsed</param>
        /// <returns>Loaded HtmlDocument</returns>
        private async Task<HtmlDocument> SendGetRequest(string url)
        {
            await _requestSemaphore.WaitAsync();
            var response = await _client.GetAsync(url);
            _requestSemaphore.Release();
            var doc = new HtmlDocument();
            doc.Load(await response.Content.ReadAsStreamAsync());
            return doc;
        }

        /// <summary>
        /// Updates all courses in the database for a given course catalog
        /// </summary>
        /// <param name="catalog">Catalog for which the courses should be updated</param>
        /// <param name="allCourses">List of courses that have already been parsed (from database)</param>
        /// <param name="db">Database context</param>
        public async Task UpdateCoursesInCourseCatalog(CourseCatalog catalog, List<Course> allCourses, DatabaseContext db)
        {
            var counter = 1;
            try
            {
                PaulRepository.AddLog($"Update for {catalog.ShortTitle} started!", FatalityLevel.Normal, "");

                var courseList = allCourses.Where(co => co.Catalogue.InternalID == catalog.InternalID && !co.IsTutorial).ToList();
                //ensure that every course has the right instance of the course catalog so that we don't get a tracking exception
                courseList.ForEach(course => course.Catalogue = catalog);

                var messages = await Task.WhenAll(new[] { "1", "2" }.Select(useLogo => SendPostRequest(catalog.InternalID, "", useLogo)));

                foreach (var message in messages)
                {
                    var document = new HtmlDocument();
                    document.Load(await message.Content.ReadAsStreamAsync());
                    var pageResult = GetPageSearchResult(document, counter);
                    if (pageResult.HasCourses)
                    {
                        await GetCourseList(db, document, catalog, courseList, updateUrls: true);
                    }


                    while (pageResult.LinksToNextPages.Count > 0)
                    {
                        var docs = await Task.WhenAll(pageResult.LinksToNextPages.Select(s => SendGetRequest(BaseUrl + s)));

                        //Getting course list for at most 3 pages
                        var courses = await Task.WhenAll(docs.Select(d => GetCourseList(db, d, catalog, courseList, updateUrls: true)));
                        counter += pageResult.LinksToNextPages.Count;
                        pageResult = GetPageSearchResult(docs.Last(), counter);
                    }
                }

                await UpdateCoursesInDatabase(db, courseList, catalog);
                PaulRepository.AddLog($"Update for {catalog.ShortTitle} completed!", FatalityLevel.Normal, "");

            }
            catch (DbUpdateConcurrencyException e)
            {
                //db.ChangeTracker.Entries().First(entry => entry.Equals(e)).State == EntityState.Detached;
                var str = new StringBuilder();
                foreach (var entry in e.Entries)
                {
                    str.AppendLine("Entry involved: " + entry.Entity + " Type: " + entry.Entity.GetType().Name);
                }

                PaulRepository.AddLog($"DbUpdateConcurrency failure: {e} in {catalog.Title} at round {counter}", FatalityLevel.Critical, "Nightly Update");
                PaulRepository.AddLog($"DbUpdateConcurrency failure: {str} in {catalog.Title}", FatalityLevel.Critical, "Nightly Update");
            }
            catch (Exception e)
            {
                PaulRepository.AddLog("Update failure: " + e + " in " + catalog.Title, FatalityLevel.Critical, "Nightly Update");
            }
        }

        /// <summary>
        /// This method is used to update the courses of the course catalog in small steps (to not overwelm the server)  
        /// </summary>
        /// <param name="db">Database context</param>
        /// <param name="courseList">List of already existing courses (from database)</param>
        /// <param name="c">Course catalog</param>
        /// <returns></returns>
        private async Task UpdateCoursesInDatabase(DatabaseContext db, List<Course> courseList, CourseCatalog c)
        {
            int counter = 0;
            int stepCount = 80;
            var stepCourses = courseList.Take(stepCount);
            var modifiableList = new List<Course>(courseList);
            while (stepCourses.Any())
            {
                counter += stepCourses.Count();
                //Get details for all courses
                await Task.WhenAll(stepCourses.Select(course => GetCourseDetailAsync(course, db, modifiableList, c)));
                await Task.WhenAll(stepCourses.Select(course => GetTutorialDetailAsync(course, db)));

                //Get details for connected courses
                var connectedCourses = stepCourses.SelectMany(s => s.ParsedConnectedCourses).Distinct();
                await Task.WhenAll(connectedCourses.Select(course => GetCourseDetailAsync(course, db, modifiableList, c, true)));
                await Task.WhenAll(connectedCourses.Select(course => GetTutorialDetailAsync(course, db)));
                await db.SaveChangesAsync();
                PaulRepository.AddLog($"Completed parsing of {counter}/{courseList.Count} courses", FatalityLevel.Normal, "");
                stepCourses = courseList.Skip(counter).Take(stepCount);
            }

        }

        /// <summary>
        /// Assumes mutually-exclusive access to _seenCourseIdsByCatalog. Returns true
        /// iff the ID was not seen before for this catalog.
        /// </summary>
        /// <param name="catalog"></param>
        /// <param name="courseId"></param>
        /// <returns></returns>
        private bool AddSeenCourseId(CourseCatalog catalog, string courseId)
        {
            if (!_seenCourseIdsByCatalog.ContainsKey(catalog))
            {
                _seenCourseIdsByCatalog[catalog] = new HashSet<string>();
            }

            return _seenCourseIdsByCatalog[catalog].Add(courseId);
        }

        /// <summary>
        /// This method parses the website using the given HtmlDocument and returns a list of courses
        /// </summary>
        /// <param name="db">Database context</param>
        /// <param name="doc">Html Document</param>
        /// <param name="catalog">Course catalog</param>
        /// <param name="courses">Existing courses</param>
        /// <param name="allowMultipleIdPasses">Determines if the id is passed more than once. This is useful if courses have the same id</param>
        /// <param name="updateUrls">Determines if the urls of courses should be updated</param>
        /// <returns></returns>
        private async Task<List<Course>> GetCourseList(DatabaseContext db, HtmlDocument doc, CourseCatalog catalog, List<Course> courses, bool allowMultipleIdPasses = false, bool updateUrls = false)
        {
            var list = new List<Course>();
            var data = doc.DocumentNode.Descendants().Where((d) => d.Name == "tr" && d.Attributes.Any(a => a.Name == "class" && a.Value == "tbdata"));

            foreach (var tr in data)
            {
                try
                {
                    var td = tr.ChildNodes.Where(ch => ch.Name == "td").Skip(1).First();
                    var text = td.ChildNodes.First(ch => ch.Name == "a").InnerText;
                    var name = text.Split(new[] { ' ' }, 2)[1];
                    var id = text.Split(new[] { ' ' }, 2)[0];
                    var url = td.ChildNodes.First(ch => ch.Name == "a").Attributes["href"].Value;
                    var docent = td.ChildNodes.Where(ch => ch.Name == "#text").Skip(1).First().InnerText.Trim('\r', '\t', '\n', ' ');
                    var trimmedUrl = WebUtility.HtmlDecode(url);

                    await _writeLock.WaitAsync();
                    if (!allowMultipleIdPasses && !AddSeenCourseId(catalog, id))
                    {
                        _writeLock.Release();
                        continue;
                    }

                    Course c = courses.FirstOrDefault(course => course.Id == $"{catalog.InternalID},{id}");
                    if (c == null)
                    {
                        c = new Course
                        {
                            Name = name,
                            Docent = docent,
                            TrimmedUrl = url,
                            Catalogue = catalog,
                            Id = $"{catalog.InternalID},{id}",
                            InternalCourseID = id
                        };
                        //db.Courses.Add(c);
                        db.Entry(c).State = EntityState.Added;
                        courses.Add(c);
                        list.Add(c);
                    }
                    else
                    {

                        var changed = false;
                        if (c.TrimmedUrl != null && trimmedUrl != null && updateUrls)
                        {
                            var relevantTrimmedUrl = c.TrimmedUrl.Substring(0, c.TrimmedUrl.LastIndexOf(','));
                            var relevantNewUrl = trimmedUrl.Substring(0, trimmedUrl.LastIndexOf(','));

                            if (relevantNewUrl != relevantTrimmedUrl)
                            {
                                c.NewUrl = trimmedUrl;
                                c.TrimmedUrl = trimmedUrl;
                                changed = true;
                            }
                        }
                        if (c.Docent != docent)
                        {
                            c.Docent = docent;
                            changed = true;
                        }

                        if (!name.Equals(c.Name))
                        {
                            c.Name = name;
                            changed = true;
                        }

                        if (changed)
                        {
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

        /// <summary>
        /// Parses the search result page and returns a <see cref="PageSearchResult"/> that contains important information like the links to the next pages
        /// </summary>
        /// <param name="doc">Html Document</param>
        /// <param name="number">This number represents the current status of the parsing run. It is used to determine which next page links to return.</param>
        /// <returns></returns>
        private PageSearchResult GetPageSearchResult(HtmlDocument doc, int number)
        {
            var navi = doc.GetElementbyId("searchCourseListPageNavi");
            if (navi == null) return PageSearchResult.Empty;

            var next = navi.ChildNodes.Where(c => c.Name == "a").SkipWhile(h => h.InnerText != number.ToString());

            var result = new PageSearchResult
            {
                HasCourses = CheckDocumentForCourses(doc),
                LinksToNextPages = next.Skip(1).Take(Math.Min(3, next.Count() - 1)).Select(h => WebUtility.HtmlDecode(h.Attributes["href"].Value)).ToList()
            };

            return result;
        }

        /// <summary>
        /// Checks if a website (search result page) contains has any courses
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private bool CheckDocumentForCourses(HtmlDocument doc)
        {
            var trs = doc.DocumentNode.Descendants().Where((d) => d.Name == "tr" && d.Attributes.Any(a => a.Name == "class" && a.Value == "tbdata"));
            return trs.Any();
        }

        /// <summary>
        /// This method updates the (more detailed) properties of a given course such as dates, connected courses, description etc.
        /// </summary>
        /// <param name="course">Course for which the information should be updated</param>
        /// <param name="db">Database context</param>
        /// <param name="list">List of existing courses</param>
        /// <param name="catalog">Course catalog</param>
        /// <param name="isConnectedCourse">Determines if the current parsing happens for a connected course (is used to prevent cirular parsing)</param>
        /// <returns></returns>
        public async Task GetCourseDetailAsync(Course course, DatabaseContext db, List<Course> list, CourseCatalog catalog, bool isConnectedCourse = false)
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

            try
            {
                //Termine parsen
                var dates = GetDates(doc, db).ToList();
                await UpdateDatesInDatabase(course, dates, db);
                await UpdateExamDates(doc, db, course);
            }
            catch
            {
                //if the updating of dates fails, not the whole update should crash
                PaulRepository.AddLog($"Date parsing failed for course {course.CourseId}", FatalityLevel.Error, "Date parsing");
            }

            //Verbundene Veranstaltungen parsen
            var divs = doc.DocumentNode.GetDescendantsByClass("dl-ul-listview");
            var courses = divs.FirstOrDefault(l => l.InnerHtml.Contains("Veranstaltung anzeigen"))?.ChildNodes.Where(l => l.Name == "li" && l.InnerHtml.Contains("Veranstaltung anzeigen"));
            if (courses != null)
            {
                foreach (var c in courses)
                {
                    var text = c.Descendants().First(n => n.Name == "strong")?.InnerText;
                    var name = text.Split(new[] { ' ' }, 2)[1];
                    var id = text.Split(new[] { ' ' }, 2)[0];
                    var url = c.Descendants().First(n => n.Name == "a")?.Attributes["href"].Value;
                    var docent = c.Descendants().Where(n => n.Name == "p").Skip(2).First().InnerText;

                    await _writeLock.WaitAsync();
                    Course c2 = list.FirstOrDefault(co => co.Id == $"{course.Catalogue.InternalID},{id}");
                    if (c2 == null)
                    {
                        c2 = new Course { Name = name, TrimmedUrl = url, Catalogue = course.Catalogue, Id = $"{course.Catalogue.InternalID},{id}" };
                        //db.Courses.Add(c2);
                        db.Entry(c2).State = EntityState.Added;
                        list.Add(c2);

                    }

                    //prevent that two separate threads add the connected courses

                    if (course.Id != c2.Id &&
                        !course.ParsedConnectedCourses.Any(co => co.Id == c2.Id) &&
                        !c2.ParsedConnectedCourses.Any(co => co.Id == course.Id))
                    {
                        var con1 = new ConnectedCourse { CourseId = course.Id, CourseId2 = c2.Id };
                        course.ParsedConnectedCourses.Add(c2);
                        db.ConnectedCourses.Add(con1);

                        var con2 = new ConnectedCourse { CourseId = c2.Id, CourseId2 = course.Id };
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
                    return new Course { Id = course.Id + $",{name}", Name = name, TrimmedUrl = url, CourseId = course.Id, IsTutorial = true, Catalogue = catalog };
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
                        var entry = db.Entry(t);
                        if (entry.State != EntityState.Added)
                        {
                            entry.State = EntityState.Added;
                        }

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

        /// <summary>
        /// Gets detailed information for a tutorial (which is also a course). Most important are the dates of the tutorial
        /// </summary>
        /// <param name="c">Course (which is a tutorial)</param>
        /// <param name="db">Database context</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the HtmlDocument for a course object (also handles fallback if the URL has changed)
        /// </summary>
        /// <param name="course">Relevant course</param>
        /// <param name="db">Database context</param>
        /// <returns>HtmlDocument of website</returns>
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
        /// <summary>
        /// This method is used to parse the database from a given website
        /// </summary>
        /// <param name="doc">HtmlDocument to parse</param>
        /// <param name="db">Database context</param>
        /// <returns></returns>
        static List<Date> GetDates(HtmlDocument doc, DatabaseContext db)
        {
            var list = new List<Date>();
            var tables = doc.DocumentNode.GetDescendantsByClass("tb list rw-table rw-all");
            var table = tables.FirstOrDefault(t => t.ChildNodes.Any(n => n.InnerText == "Termine"));
            if (table == null) return list;
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
                        else { PaulRepository.AddLog("Timezone not present", FatalityLevel.Critical, ""); }
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
                        list.Add(new Date { From = from, To = to, Room = room, Instructor = instructor });
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// This method updates the dates in the database for a given course
        /// </summary>
        /// <param name="course">Course</param>
        /// <param name="dates">List of new dates</param>
        /// <param name="db">Database context</param>
        /// <returns></returns>
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

        /// <summary>
        /// Update the exam dates in the database for a given course
        /// </summary>
        /// <param name="doc">HtmlDocment used for parsing</param>
        /// <param name="db">Database context</param>
        /// <param name="course">Relevant course</param>
        /// <returns></returns>
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
                            else { PaulRepository.AddLog("Timezone not present", FatalityLevel.Critical, ""); }

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
                            list.Add(new ExamDate { From = from, To = to, Description = name, Instructor = instructor });
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

        /// <summary>
        /// Used to update all category filters for every course catalog
        /// </summary>
        /// <param name="allCourses">List of existing courses</param>
        /// <param name="context">Database context</param>
        /// <returns></returns>
        public async Task UpdateCategoryFilters(List<Course> allCourses, DatabaseContext context)
        {
            PaulRepository.AddLog("Update for category filters has started!", FatalityLevel.Normal, "Update category filters");
            var catalogues = (await PaulRepository.GetCourseCataloguesAsync(context));
            foreach (var cat in catalogues)
            {
                try
                {
                    PaulRepository.AddLog($"Update for course catalog {cat.ShortTitle} has started!", FatalityLevel.Normal, "");
                    await UpdateCategoryFiltersForCatalog(cat, allCourses, context);
                    PaulRepository.AddLog($"Update for course catalog {cat.ShortTitle} completed!", FatalityLevel.Normal, "");
                }
                catch (Exception e)
                {
                    PaulRepository.AddLog($"Updating Categories failed: {e}", FatalityLevel.Critical, "Nightly Update");
                }
            }

            PaulRepository.AddLog("Update for category filters completed!", FatalityLevel.Normal, "Update category filters");

        }

        /// <summary>
        /// Upadtes the category filters for a given course catalog
        /// </summary>
        /// <param name="cat">Course catalog</param>
        /// <param name="allCourses">List of existing courses</param>
        /// <param name="db">Database context</param>
        /// <returns></returns>
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
                    foreach (var category in parentCategories.Select(e => e.Value)) PaulRepository.AddLog($"Currently at filter {category.Title}", FatalityLevel.Verbose, "");
                    var tasks = parentCategories.Keys.Select(node => UpdateCategoryForHtmlNode(db, node, parentCategories[node], cat, allCourses)).ToList();
                    parentCategories = (await Task.WhenAll(tasks)).SelectMany(r => r).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    await db.SaveChangesAsync();
                } while (parentCategories.Keys.Any());

            }
        }

        /// <summary>
        /// Extracts the category filters from a given html node and updates the database 
        /// </summary>
        /// <param name="db">Database context</param>
        /// <param name="node">Current Html node</param>
        /// <param name="category">Current category filter</param>
        /// <param name="cat">Course catalog</param>
        /// <param name="allCourses">List of existing courses</param>
        /// <returns></returns>
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

        /// <summary>
        /// Updates the category filters in the database by comparing new filters with the existing filters
        /// </summary>
        /// <param name="db">Database context</param>
        /// <param name="currentFilter">Current category filter</param>
        /// <param name="nodes">New html nodes (basically new filters)</param>
        /// <param name="doc">Current Html document</param>
        /// <param name="isTopLevel">Determines if we are in the first run (so at the top level of the "tree")</param>
        /// <param name="cat">Course catalog</param>
        /// <param name="allCourses">List of existing courses</param>
        /// <returns></returns>
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

                        filter = new CategoryFilter { Title = title, IsTopLevel = isTopLevel, CourseCatalog = cat };
                        currentFilter?.Subcategories.Add(filter);
                        var entry = db.ChangeTracker.Entries().FirstOrDefault(e => e.Entity == currentFilter);
                        if (entry?.State != EntityState.Added) entry.State = EntityState.Modified;

                        db.Entry(filter).State = EntityState.Added;
                        _writeLock.Release();
                    }

                    dict[node] = filter;
                }

            }

            var courses = await GetCourseList(db, doc, cat, allCourses, allowMultipleIdPasses: true);

            if (courses.Any() && currentFilter != null)
            {
                foreach (var course in courses)
                {
                    await _writeLock.WaitAsync();

                    if (!currentFilter.ParsedCourses.Any(c => c.CourseId == course.Id))
                    {
                        var catCourse = new CategoryCourse { Category = currentFilter, CourseId = course.Id };
                        db.Entry(catCourse).State = EntityState.Added;
                        currentFilter.ParsedCourses.Add(catCourse);
                    }
                    _writeLock.Release();

                }
            }

            return dict;
        }

        /// <summary>
        /// Return a list of HtmlNodes that contain relevant information for the category filters
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
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
        /// <summary>
        /// This extension method returns all descendant html nodes that have a class attribute with the given name
        /// </summary>
        /// <param name="node">Html node</param>
        /// <param name="c">Name of the class attribute</param>
        /// <returns></returns>
        public static List<HtmlNode> GetDescendantsByClass(this HtmlNode node, string c)
        {
            return node.Descendants().Where((d) => d.Attributes.Any(a => a.Name == "class" && a.Value == c)).ToList();
        }

        /// <summary>
        /// This extension method returns all descendant html nodes that have  the given name
        /// </summary>
        /// <param name="node">Html node</param>
        /// <param name="c">Name of the class attribute</param>
        /// <returns></returns>
        public static List<HtmlNode> GetDescendantsByName(this HtmlNode node, string n)
        {
            return node.Descendants().Where((d) => d.Attributes.Any(a => a.Name == "name" && a.Value == n)).ToList();
        }

        public static string TrimWhitespace(this string s)
        {
            return s.Trim('\r', '\t', '\n', ' ');
        }

    }
}
