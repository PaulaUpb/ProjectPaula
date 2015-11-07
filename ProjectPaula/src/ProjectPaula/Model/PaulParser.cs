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
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace ProjectPaula.Model
{
    class PaulParser
    {
        private HttpClient _client;
        private const string _searchUrl = "https://paul.uni-paderborn.de/scripts/mgrqispi.dll?APPNAME=CampusNet&PRGNAME=ACTION&ARGUMENTS=-A6grKs5PHq2rFF2cazDrKQT4oecxio0CjK9Y7W9Jd3DdiHke0Qf8QZdI4tyCkNAXXLn5WwUf1J-8nbwl3GO3wniMX-TGs97==";
        private const string _dllUrl = "https://paul.uni-paderborn.de/scripts/mgrqispi.dll";
        private const string _baseUrl = "https://paul.uni-paderborn.de/";


        public PaulParser()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Accept-Language", "de-DE,de;q=0.8,en-US;q=0.5,en;q=0.3");
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            _client.DefaultRequestHeaders.Add("Accept", "text/html, application/xhtml+xml, image/jxr, */*");
            _client.DefaultRequestHeaders.Remove("Expect");
        }

        public async Task<IEnumerable<CourseCatalogue>> GetAvailabeCourseCatalogues()
        {
            HtmlDocument doc = new HtmlDocument();
            doc.Load(await _client.GetStreamAsync(_searchUrl), Encoding.UTF8);
            var catalogue = doc.GetElementbyId("course_catalogue");
            var options = catalogue.Descendants().Where(c => c.Name == "option" && c.Attributes.Any(a => a.Name == "title" && a.Value.Contains("Vorlesungsverzeichnis")));
            return options.Select(n => new CourseCatalogue() { InternalID = n.Attributes["value"].Value, Title = n.Attributes["title"].Value });
        }

        private async Task<HttpResponseMessage> SendPostRequest(string couseCatalogue, string search)
        {
            var par = $"APPNAME=CampusNet&PRGNAME=ACTION&ARGUMENTS=ARGS_SEARCHCOURSE&ARGS_SEARCHCOURSE=6grKs5PHq2rFF2cazDrKQT4oecxio0CjK9Y7W9Jd3DdiHke0Qf8QZdI4tyCkNAXXLn5WwUf1J-8nbwl3GO3wniMX-TGs97%3D%3D&sessionno=000000000000001&menuid=000443&submit_search=Suche&course_catalogue={couseCatalogue}&course_catalogue_section=0&faculty=0&course_type=0&course_number=&course_name=&course_short_name=&with_logo=0&module_number=&module_name=&instructor_firstname=&instructor_surname=&free_text={search}";

            return await _client.PostAsync(_dllUrl, new StringContent(par));
        }



        public async Task<List<Course>> GetCourseSearchDataAsync(CourseCatalogue catalogue, string search, DatabaseContext db)
        {
            var message = await SendPostRequest(catalogue.InternalID, search);
            HtmlDocument doc = new HtmlDocument();
            doc.Load(await message.Content.ReadAsStreamAsync(), Encoding.UTF8);
            var list = new List<Course>();
            var data = doc.DocumentNode.Descendants().Where((d) => d.Name == "tr" && d.Attributes.Any(a => a.Name == "class" && a.Value == "tbdata"));


            foreach (var tr in data)
            {
                var td = tr.ChildNodes.Where(ch => ch.Name == "td").Skip(1).First();
                var text = td.ChildNodes.First(ch => ch.Name == "a").InnerText;
                var name = text.Split(new char[] { ' ' }, 2)[1];
                var id = text.Split(new char[] { ' ' }, 2)[0];
                var c = db.Courses.Include(d => d.ConnectedCoursesInternal).Include(d => d.Dates).Include(d => d.Catalogue).Include(d => d.Tutorials).ToList().LocalChanges(db).FirstOrDefault(course => course.Id == $"{catalogue.InternalID},{id}");

                if (c == null)
                {
                    c = new Course()
                    {
                        Name = name,
                        Docent = td.ChildNodes.Where(ch => ch.Name == "#text").Skip(1).First().InnerText.Trim('\r', '\t', '\n', ' '),
                        Url = td.ChildNodes.First(ch => ch.Name == "a").Attributes["href"].Value,
                        Catalogue = catalogue,
                        Id = $"{catalogue.InternalID},{id}"
                    };
                    db.Courses.Add(c);
                }
                list.Add(c);
            }

            return list;
        }

        public async Task GetCourseDetailAsync(Course course, DatabaseContext db)
        {
            var response = await _client.GetAsync((_baseUrl + WebUtility.HtmlDecode(course.Url)));

            HtmlDocument doc = new HtmlDocument();
            doc.Load(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
            //Termine parsen
            var dates = GetDates(doc);
            course.Dates.AddRange(dates.Except(course.Dates));
            db.Dates.RemoveRange(course.Dates.Except(dates));
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
                    var c2 = db.Courses.Include(d => d.ConnectedCoursesInternal).Include(d => d.Catalogue).Include(d => d.Tutorials).Include(d => d.Dates).ToList().LocalChanges(db).FirstOrDefault(co => co.Id == $"{course.Catalogue.InternalID},{id}");
                    if (c2 == null)
                    {
                        c2 = new Course() { Name = name, Url = url, Catalogue = course.Catalogue, Id = $"{course.Catalogue.InternalID},{id}" };
                        db.Courses.Add(c2);

                    }
                    if (!course.ConnectedCoursesInternal.Any(co => co.CourseId2 == c2.Id))
                    {
                        var con1 = new ConnectedCourse() { CourseId2 = c2.Id };
                        course.ConnectedCoursesInternal.Add(con1);
                        var con2 = new ConnectedCourse() { CourseId2 = course.Id };
                        c2.ConnectedCoursesInternal.Add(con2);
                        db.ConnectedCourses.Add(con1); db.ConnectedCourses.Add(con2);
                    }
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
                    var contained = false;
                    Tutorial t;
                    if (course.Tutorials.Any(tut => tut.Name == name))
                    {
                        t = course.Tutorials.First(tut => tut.Name == name);
                        contained = true;
                    }
                    else
                    {
                        t = new Tutorial() { Name = name, Course = course };
                    }

                    var res = await _client.GetAsync((_baseUrl + WebUtility.HtmlDecode(url)));

                    HtmlDocument d = new HtmlDocument();
                    d.Load(await res.Content.ReadAsStreamAsync(), Encoding.UTF8);

                    //Termine parsen
                    var dates2 = GetDates(d).Except(course.Dates);
                    t.Dates.AddRange(dates2.Except(t.Dates));
                    db.Dates.RemoveRange(t.Dates.Except(dates2));


                    if (!contained)
                    {
                        course.Tutorials.Add(t);
                        db.Tutorials.Add(t);
                    }
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
                    var date = DateTime.Parse(tr.GetDescendantsByName("appointmentDate").First().InnerText.Trim('*'), new CultureInfo("de-DE"));
                    var fromNode = tr.GetDescendantsByName("appointmentTimeFrom").First();
                    var toNode = tr.GetDescendantsByName("appointmentDateTo").First();
                    var from = date.Add(TimeSpan.Parse(fromNode.InnerText));
                    var to = date.Add(TimeSpan.Parse(toNode.InnerText));
                    var room = tr.GetDescendantsByName("appointmentRooms").FirstOrDefault()?.InnerText;
                    var instructor = tr.GetDescendantsByName("appointmentInstructors").First().InnerText.Trim('\r', '\t', '\n', ' ');
                    list.Add(new Date() { From = from, To = to, Room = room, Instructor = instructor });
                }
            }

            return list;
        }
    }
    static partial class ExtensionMethods
    {
        public static List<CodeComb.HtmlAgilityPack.HtmlNode> GetDescendantsByClass(this CodeComb.HtmlAgilityPack.HtmlNode node, string c)
        {
            return node.Descendants().Where((d) => d.Attributes.Any(a => a.Name == "class" && a.Value == c)).ToList();
        }

        public static List<CodeComb.HtmlAgilityPack.HtmlNode> GetDescendantsByName(this CodeComb.HtmlAgilityPack.HtmlNode node, string n)
        {
            return node.Descendants().Where((d) => d.Attributes.Any(a => a.Name == "name" && a.Value == n)).ToList();
        }

    }
}
