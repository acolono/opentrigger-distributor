using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.opentrigger.distributord;

namespace com.opentrigger.cli.distributord
{
    class DefaultConfig
    {
        public DistributorConfig GetDefaultDebugDistributorConfig()
        {
            var connection = string.Intern("mqtt://pi3");
            var includeMacs_TokenCube = new[] { "F1:11:11:11:10:12", "CA:9F:0A:00:66:20" };
            var includeMacs_WhiteButtons = new[] { "0C:F3:EE:00:2D:00", "0C:F3:EE:00:2E:7D" };

            var excludeMacs = new[] { "54:60:09:39:F0:55" }; // annoying things...
            int skip = 0;

            var config = new DistributorConfig
            {
                QueueDistributorConfigs = new[]{
                    new QueueDistributorConfig {
                        Connection = connection,
                        Skip = skip,
                        IncludedMacs = includeMacs_WhiteButtons,
                        ExcludedMacs = excludeMacs,
                    },
                    new QueueDistributorConfig {
                        Connection = connection,
                        Skip = skip,
                        IncludedMacs = includeMacs_TokenCube,
                        UniqueIdentifier = UniqueIdentifier.Nrf51MacAndTokenCubePir,
                        Distance = 100,
                    },
                    //new QueueDistributorConfig {
                    //    Connection = connection,
                    //    Skip = skip,
                    //    UniqueIdentifier = UniqueIdentifier.MacAndAdvertisingData,
                    //    ReleaseTopic = null,
                    //    TriggerTopic = "/opentrigger/signals/sensors",
                    //    Distance = 0,
                    //    IncludedMacs = includeMacs_TokenCube,
                    //    ExcludedMacs = excludeMacs,
                    //},
                },

                //CoapDistributorConfigs = new []
                //{
                //    new CoapDistributorConfig
                //    {
                //        Connection = connection,
                //        ButtonConfigurations = new []
                //        {
                //            new ButtonConfiguration { BaseUri = "coap://pi3:5683/" },
                //        }
                //    },
                //},
                FlicDistributorConfigs = new[]
                {
                    new FlicDistributorConfig
                    {
                        Connection = connection,
                    }
                },

                CoapServerDistributorConfigs = new CoapServerDistributorConfig[]
                {
                  new CoapServerDistributorConfig
                  {
                      Connection = connection,
                  }  
                },
                RunParallel = true,
                Verbosity = 3
            };
            return config;
        }
    }
}
