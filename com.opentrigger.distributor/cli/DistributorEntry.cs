using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.opentrigger.distributord
{
    class DistributorEntry
    {
        static void Main(string[] args)
        {
            // 1 = Trigger/Release
            // 2 = Json
            // 3 = Take, Skip
            // 4 = Duplicate
            // 5 = Excluded
            int verbosity = args.Count(arg => (arg == "-v" || arg == "--verbose"));
            
            // --roundtrip 172.17.17.136 --verbose
            var roundtripIndex = Array.IndexOf(args,"--roundtrip");
            if (roundtripIndex != -1)
            {
                var paddingIndex = Array.IndexOf(args, "--padding");
                var padding = int.Parse(args.ElementAtOrDefault(paddingIndex + 1) ?? "0");
                var server = args.ElementAtOrDefault(roundtripIndex + 1) ?? "127.0.0.1";
                Console.Error.WriteLine($"! roundtrip:{server} padding:{padding} verbosity:{verbosity}");
                new RoundtripTime(server, padding, verbosity);
            }

            new QueueDistributor(connection: "tcp://172.17.17.136", verbosity:verbosity, excudeMacs:new []{ "54:60:09:39:F0:55" },skip:3);
        }
    }
}
