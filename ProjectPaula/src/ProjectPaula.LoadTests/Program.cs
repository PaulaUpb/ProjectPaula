using Microsoft.AspNet.SignalR.Client;
using ProjectPaula.Hubs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectPaula.LoadTests
{
    class Program
    {
        private static readonly Random Random = new Random();

        private static readonly string[] SearchQueries = new[]
        {
            "plac", "GRA", "fundamental", "der", "die", "das", "test", "epik"
        };

        private static readonly string[] Courses = new[]
        {
            "357024079480661,L.079.05301", // GRA
            "357024079480661,L.079.05510", // XMLD
            "357024079480661,L.105.91100"  // MWW I (Mathe für Wiwis)
        };

        private const int RunsPerUser = 5;
        private const double AverageTimeBetweenClicksMs = 5000;
        private const int ClientCount = 1000;
        private const int SearchCount = 5;
        private const string Url = "http://localhost:5000";
        private const int ClientInitWaitTimeMs = 200;

        /// <summary>
        /// Compute a random exponentially distributed
        /// wait time with parameter lambda. E[X] = 1/lambda,
        /// Var(X) = 1/lambda^2.
        /// </summary>
        /// <param name="lambda"></param>
        /// <returns></returns>
        private static double ComputeExponentialWaitTime(double lambda)
            => -Math.Log(1 - Random.NextDouble()) / lambda;

        private static int _activeClients = 0;

        private static async Task<long> WaitRandomTime()
        {
            var waitTime = (long)ComputeExponentialWaitTime(1 / AverageTimeBetweenClicksMs);
            await Task.Delay(TimeSpan.FromMilliseconds(waitTime));
            return waitTime;
        }


        static void Main(string[] args)
        {
            PerformLoadTestAsync(Url).Wait();
            Console.ReadLine();
        }

        private static async Task PerformLoadTestAsync(string url)
        {
            var sw = Stopwatch.StartNew();

            await Task.WhenAll(Enumerable.Range(0, ClientCount).Select(i => SimulateUserAsync(i, url)));

            sw.Stop();
            Console.WriteLine("---");
            Console.WriteLine($"Simulation of {ClientCount} clients took {sw.ElapsedMilliseconds}ms");
        }

        private static async Task SimulateUserAsync(int index, string url)
        {
            var userName = "User" + index;

            await Task.Delay(TimeSpan.FromMilliseconds(ClientInitWaitTimeMs * index));
            var nowActiveClients = Interlocked.Increment(ref _activeClients);
            Console.WriteLine($"{userName} started working, ActiveClients={nowActiveClients}.");

            var sw = Stopwatch.StartNew();

            try
            {
                var connection = new HubConnection(url);
                var hubProxy = connection.CreateHubProxy(nameof(TimetableHub));
                await connection.Start();

                var connectionTime = sw.ElapsedMilliseconds;

                var scheduleId =
                    await
                        hubProxy.Invoke<string>(nameof(TimetableHub.CreateSchedule), userName, "357024079480661"
                            /* WS1516 */);

                var operationsPerRun = SearchCount + 2;
                long totalWaitTime = 0;

                for (var run = 1; run < RunsPerUser + 1; run++)
                {
                    for (var i = 0; i < SearchCount; i++)
                    {
                        Interlocked.Add(ref totalWaitTime, await WaitRandomTime());
                        await hubProxy.Invoke(nameof(TimetableHub.SearchCourses), SearchQueries.Random());
                    }

                    var course = Courses.Random();
                    Interlocked.Add(ref totalWaitTime, await WaitRandomTime());
                    await hubProxy.Invoke(nameof(TimetableHub.AddCourse), course);
                    Interlocked.Add(ref totalWaitTime, await WaitRandomTime());
                    await hubProxy.Invoke(nameof(TimetableHub.RemoveUserFromCourse), course, true);

                }

                await hubProxy.Invoke(nameof(TimetableHub.ExitSchedule));

                sw.Stop();
                var totalTime = sw.ElapsedMilliseconds;
                var averageResponseTime = (totalTime - totalWaitTime) / (RunsPerUser * (double)operationsPerRun);
                Console.WriteLine($"{userName} finished: Connection time {connectionTime}ms, " +
                                  $"Average time per run {totalTime / RunsPerUser}ms, " +
                                  $"Average response time {averageResponseTime}ms");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{userName}: Failed due to {e}");
            }
            finally
            {
                Interlocked.Decrement(ref _activeClients);
            }
        }
    }

    static class Extensions
    {
        private static readonly Random _random = new Random();

        public static T Random<T>(this ICollection<T> collection)
        {
            return collection.ElementAt(_random.Next(collection.Count()));
        }
    }
}
