using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace ProjectPaula
{
    public class Program
    {

        /// <summary>
        /// Enum-like class containing type-safe flags the program
        /// can be started with.
        /// </summary>
        sealed class Flag
        {
            /// <summary>
            /// Flag indicating that Kestrel should listen on 0.0.0.0:80.
            /// </summary>
            public static readonly Flag ListenWeb = new Flag("listen-web");

            private static readonly Flag[] Flags = { ListenWeb };

            private readonly string _commandLineParam;

            private Flag(string commandLineParam)
            {
                _commandLineParam = commandLineParam;
            }

            public static Flag ByCommandLineParam(string param)
            {
                return Flags.First(flag => flag._commandLineParam.Equals(param));
            }

            public static IEnumerable<Flag> FromCommandLineParams(IEnumerable<string> pars)
            {
                return pars.Select(par => Flags.FirstOrDefault(flag => par.Equals(flag._commandLineParam)))
                    .Where(it => it != null);
            }
        }

        public static void Main(string[] args)
        {
            var flags = Flag.FromCommandLineParams(args);

            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();

            if (flags.Contains(Flag.ListenWeb))
            {
                hostBuilder.UseUrls("http://0.0.0.0:80");
            }


            var host = hostBuilder.Build();
            host.Run();
        }
    }
}
