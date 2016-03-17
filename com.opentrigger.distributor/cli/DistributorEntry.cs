using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace com.opentrigger.distributord
{
    class DistributorEntry
    {
        static void Main(string[] args)
        {
            // 1 = Trigger/Release
            // 2 = Json
            // 3 = Take, Skip, PublishFails
            // 4 = Duplicate
            // 5 = Excluded
            int verbosity = 2 + args.Count(arg => arg == "-v" || arg == "--verbose") - args.Count(arg => arg == "-q" || arg == "--quiet");

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

            DistributorConfig config;

            var connection = string.Intern("tcp://pi3.fritz.box");
            var includeMacs_TokenCube = new[] { "F1:11:11:11:10:12" };
            var includeMacs_WhiteButtons = new[] { "0C:F3:EE:00:2D:00", "0C:F3:EE:00:2E:7D" };

            var excludeMacs = new[] {"54:60:09:39:F0:55"}; // annoying things...
            int skip = 0;

            config = new DistributorConfig {
                QueueDistributorConfigs = new[]{
                    new QueueDistributorConfig {
                        Connection = connection,
                        Skip = skip,
                        IncludedMacs = includeMacs_WhiteButtons,
                        //ExcludedMacs = excludeMacs,
                    },
                    new QueueDistributorConfig {
                        Connection = connection,
                        Skip = skip,
                        ExcludedMacs = excludeMacs,
                        UniqueIdentifier = UniqueIdentifier.MacAndTokenCubePir,
                        Distance = 200,
                    },
                    new QueueDistributorConfig {
                        Connection = connection,
                        Skip = skip,
                        UniqueIdentifier = UniqueIdentifier.MacAndAdvertisingData,
                        ReleaseTopic = null,
                        TriggerTopic = "/opentrigger/signals/sensors",
                        Distance = 0,
                        //IncludedMacs = includeMacs_TokenCube,
                        ExcludedMacs = excludeMacs,
                    },
                },
                Verbosity = verbosity,
                RunParallel = true,
            };
            
            var distributors = config.QueueDistributorConfigs.Select(queueDistributorConfig => new QueueDistributor(queueDistributorConfig,config.Verbosity)).ToList();
            if (config.RunParallel) while (true)
            {
                Parallel.ForEach(distributors, d => d.Distribute());
                Thread.Sleep(config.IdleCycle);
            }
            else while (true)
            {
                distributors.ForEach(d=>d.Distribute());
                Thread.Sleep(config.IdleCycle);
            }
        }
    }
}
