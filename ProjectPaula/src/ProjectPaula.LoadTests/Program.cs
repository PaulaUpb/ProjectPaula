using Microsoft.AspNet.SignalR.Client;
using ProjectPaula.Hubs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.LoadTests
{
    class Program
    {
        private static readonly Random _random = new Random();

        private static readonly string[] _searchQueries = new[]
        {
            "plac", "GRA", "fundamental", "der", "die", "das", "test", "epik"
        };

        private static readonly string[] _courses = new[]
        {
            "357024079480661,L.079.05301", // GRA
            "357024079480661,L.079.05510", // XMLD
            "357024079480661,L.105.91100"  // MWW I (Mathe für Wiwis)
        };


        static void Main(string[] args)
        {
            var url = "http://localhost:5000";
            PerformLoadTestAsync(url).Wait();
            Console.ReadLine();
        }

        private static async Task PerformLoadTestAsync(string url)
        {
            var sw = Stopwatch.StartNew();

            var clientCount = 100;

            await Task.WhenAll(Enumerable.Range(0, clientCount).Select(i => SimulateUserAsync(i, url)));

            sw.Stop();
            Console.WriteLine("---");
            Console.WriteLine($"Simulation of {clientCount} clients took {sw.ElapsedMilliseconds}ms");
        }

        private static async Task SimulateUserAsync(int index, string url)
        {
            var sw = Stopwatch.StartNew();

            var userName = "User" + index;
            var connection = new HubConnection(url);
            var hubProxy = connection.CreateHubProxy(nameof(TimetableHub));
            await connection.Start();

            var connectionTime = sw.ElapsedMilliseconds;

            var scheduleId = await hubProxy.Invoke<string>(nameof(TimetableHub.CreateSchedule), userName, "357024079480661" /* WS1516 */);

            var searchCount = 5;// _random.Next(2, 10);

            for (var i = 0; i < searchCount; i++)
            {
                await hubProxy.Invoke(nameof(TimetableHub.SearchCourses), _searchQueries.Random());
            }

            var course = _courses.Random();
            await hubProxy.Invoke(nameof(TimetableHub.AddCourse), course);
            await hubProxy.Invoke(nameof(TimetableHub.RemoveUserFromCourse), course, true);

            await hubProxy.Invoke(nameof(TimetableHub.ExitSchedule));

            sw.Stop();
            var totalTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"{userName}: Connection time {connectionTime}ms, Total time {totalTime}ms");
        }
    }

    static class Extensions
    {
        private static readonly Random _random = new Random();

        public static T Random<T>(this IEnumerable<T> collection)
        {
            return collection.ElementAt(_random.Next(collection.Count()));
        }
    }
}
