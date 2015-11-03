using CodeComb.HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPaul.Model
{
    class PaulParser
    {
        private HttpClient _client;
        private const string _searchUrl = "https://paul.uni-paderborn.de/scripts/mgrqispi.dll?APPNAME=CampusNet&PRGNAME=ACTION&ARGUMENTS=-A6grKs5PHq2rFF2cazDrKQT4oecxio0CjK9Y7W9Jd3DdiHke0Qf8QZdI4tyCkNAXXLn5WwUf1J-8nbwl3GO3wniMX-TGs97==";
        private const string _baseUrl = "https://paul.uni-paderborn.de/";


        public PaulParser()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Accept-Language", "de-DE,de;q=0.8,en-US;q=0.5,en;q=0.3");
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

            return await _client.PostAsync(_searchUrl, new StringContent(par));
        }



        public async Task<List<Course>> GetCourseSearchDataAsync(CourseCatalogue catalogue, string search)
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
                var c = new Course()
                {
                    Name = name,
                    Docent = td.ChildNodes.Where(ch => ch.Name == "#text").Skip(1).First().InnerText.Trim('\r', '\t', '\n', ' '),
                    Url = td.ChildNodes.First(ch => ch.Name == "a").Attributes["href"].Value,
                    Catalogue = catalogue,
                    Id = $"{catalogue.InternalID},{id}"
                };
                list.Add(c);
            }

            return list;
        }

        public async Task GetCourseDetailAsync(Course course)
        {
            var response = await _client.GetAsync((_baseUrl + WebUtility.HtmlDecode(course.Url)));

            HtmlDocument doc = new HtmlDocument();
            doc.Load(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
            //Termine parsen

            course.Dates = GetDates(doc);
            //Regelmäßige Termine herausfinden
            course.RegularDates = course.Dates.GetRegularDates().SelectMany(x => new List<Date>() { x.Key }).ToList();

            //Verbundene Veranstaltungen parsen
            var divs = doc.DocumentNode.GetDescendantsByClass("dl-ul-listview");
            var courses = divs.FirstOrDefault(l => l.InnerHtml.Contains("Veranstaltung anzeigen"))?.ChildNodes.Where(l => l.Name == "li" && l.InnerHtml.Contains("Veranstaltung anzeigen"));
            if (courses != null)
            {
                foreach (var c in courses)
                {
                    course.ConnectedCourses = new List<Course>();
                    var text = c.Descendants().First(n => n.Name == "strong")?.InnerText;
                    var name = text.Split(new char[] { ' ' }, 2)[1];
                    var id = text.Split(new char[] { ' ' }, 2)[0];
                    var url = c.Descendants().First(n => n.Name == "a")?.Attributes["href"].Value;
                    var docent = c.Descendants().Where(n => n.Name == "p").Skip(2).First().InnerText;
                    course.ConnectedCourses.Add(new Course() { Name = name, Url = url, Catalogue = course.Catalogue, Id = $"{course.Catalogue.InternalID},{id}" });
                }
            }

            //Gruppen parsen
            var groups = divs.FirstOrDefault(l => l.InnerHtml.Contains("Kleingruppe anzeigen"))?.ChildNodes.Where(l => l.Name == "li");
            if (groups != null)
            {
                course.Tutorials = new List<Tutorial>();
                foreach (var group in groups)
                {
                    var name = group.Descendants().First(n => n.Name == "strong")?.InnerText;
                    var url = group.Descendants().First(n => n.Name == "a")?.Attributes["href"].Value;
                    Tutorial t = new Tutorial() { Name = name };

                    var res = await _client.GetAsync((_baseUrl + WebUtility.HtmlDecode(url)));

                    HtmlDocument d = new HtmlDocument();
                    d.Load(await res.Content.ReadAsStreamAsync(), Encoding.UTF8);
                    //Termine parsen
                    var dates = GetDates(d);
                    t.Dates = dates.Except(dates.Intersect(course.Dates)).ToList();
                    t.RegularDates = t.Dates.GetRegularDates().SelectMany(x => new List<Date>() { x.Key }).ToList();
                    course.Tutorials.Add(t);
                }
            }
        }

        static List<Date> GetDates(HtmlDocument doc)
        {
            var list = new List<Date>();
            var table = doc.DocumentNode.GetDescendantsByClass("tb list")[1];
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
                    var room = tr.GetDescendantsByName("appointmentRooms").First().InnerText;
                    var instructor = tr.GetDescendantsByName("appointmentInstructors").First().InnerText.Trim('\r', '\t', '\n', ' ');
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

        public static IList<IGrouping<Date, Date>> GetRegularDates(this IList<Date> list)
        {
            return list.GroupBy(d => d, new DateComparer()).ToList();
        }
    }
}
