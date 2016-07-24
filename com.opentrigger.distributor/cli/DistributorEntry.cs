using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.opentrigger.cli.distributord;

namespace com.opentrigger.distributord
{
    class DistributorEntry
    {
        static void Main(string[] args)
        {
            // 0 = Quiet
            // 1 = Trigger/Release, print config
            // 2 = Json
            // 3 = Take, Skip, PublishFails
            // 4 = Duplicates
            // 5 = Excluded

            int verbosity = 0 + args.Count(arg => arg == "-v" || arg == "--verbose") - args.Count(arg => arg == "-q" || arg == "--quiet");

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

            var config = new DefaultConfig().GetDefaultDistributorConfig();

            var configIndex = Array.IndexOf(args, "-c");
            if (configIndex == -1) configIndex = Array.IndexOf(args, "--config");
            if (configIndex != -1)
            {
                try
                {
                    var configFile = args.ElementAtOrDefault(configIndex + 1);
                    if(string.IsNullOrWhiteSpace(configFile)) throw new Exception("Invalid config file name");
                    if(!System.IO.File.Exists(configFile)) throw new Exception("Missing config file - " + configFile);
                    var configContent = System.IO.File.ReadAllText(configFile, Encoding.UTF8);
                    if(string.IsNullOrWhiteSpace(configContent)) throw new Exception("Empty config file - " + configFile);
                    config = configContent.Deserialize<DistributorConfig>();
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("Error loading configuration!");
                    throw;
                }
                
            }

            config.Verbosity += verbosity;

            if(config.Verbosity > 0) Console.WriteLine(config.Serialize());

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
