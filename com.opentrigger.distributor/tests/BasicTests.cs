using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using com.opentrigger.distributord;
using NUnit.Framework;

namespace com.opentrigger.tests
{
    [TestFixture]
    public class BasicTests
    {
        [Test]
        public void HexStrings()
        {
            Assert.IsTrue("FFAA".ToBytes().ToHexString() == "FFAA");
            Assert.IsTrue("".ToBytes().Length == 0);
            Assert.IsTrue(new byte[] {}.ToHexString() == "");
            Assert.AreEqual("0011".ToBytes(),new byte[] {0x00,0x11});
            Assert.AreEqual("0011 22xxaa".ToBytes(true),new byte[] {0x00,0x11,0x22,0xAA});

        }

        [Test]
        public void Config()
        {
            var config = new DistributorConfig();
            config.QueueDistributorConfigs = new List<QueueDistributorConfig> {new QueueDistributorConfig()};
            config.CoapDistributorConfigs = new List<CoapDistributorConfig> {new CoapDistributorConfig()};
            config.FlicDistributorConfigs = new List<FlicDistributorConfig> {new FlicDistributorConfig()};
            config.CoapServerDistributorConfigs = new List<CoapServerDistributorConfig> {new CoapServerDistributorConfig()};
            var json = config.Serialize().Deserialize<DistributorConfig>().Serialize();
            var obj = json.Deserialize<DistributorConfig>();
            Debug.WriteLine(json);
            Assert.IsTrue(obj.QueueDistributorConfigs.Any());
            Assert.IsTrue(obj.QueueDistributorConfigs.First().Connection == config.QueueDistributorConfigs.First().Connection);
            System.IO.File.WriteAllText("defaultConfig.json",json,Encoding.UTF8);
        }

        [Test]
        public void debugConfig()
        {
            var connection = string.Intern("tcp://172.17.17.136");

            var config = new DistributorConfig
            {
                QueueDistributorConfigs = new[]{
                    new QueueDistributorConfig {
                        Connection = connection,
                        Skip = 1,
                        ExcludedMacs = new[] {"54:60:09:39:F0:55"}
                    },
                    new QueueDistributorConfig {
                        Connection = connection,
                        Skip = 1,
                        UniqueIdentifier = UniqueIdentifier.MacAndAdvertisingData,
                        ReleaseTopic = null,
                        TriggerTopic = "/opentrigger/signals/sensors",
                        PublishFormat = PublishFormat.JsonPretty
                    },
                },
                Verbosity = 4
            };
            System.IO.File.WriteAllText("debugConfig.json", config.Serialize(), Encoding.UTF8);
        }

        [Test]
        public void PacketFilterPerformance()
        {
            for (int i = 0; i < 5; i++)
            {
                PacketFilter();
            }
        }
        public void PacketFilter()
        {
            int eventCounter = -20;
            var pf = new PacketFilter(5, 100)
            {
                OnRelease = data => { Debug.WriteLine($"RELEASE: {data.UniqueIdentifier} {data.Age}"); eventCounter++; },
                OnTrigger = data => { /* Debug.WriteLine($"TRIGGER: {data.UniqueIdentifier}"); */ eventCounter++; },
                OnLogentry = line => { /* Debug.WriteLine(line); */ },
            };
            var uid = Guid.NewGuid().ToString().Replace("-", "");
            for (int j = 0; j < 10; j++)
            for (int i = 0; i < 50; i++)
            {
                pf.Add(new PacketData
                {
                    Origin = $"/{uid}/{j}/xyz",
                    UniqueIdentifier = $"{uid}-{j}",
                    Packet = new BtleDecoded()
                });
                Thread.Sleep(5);
                pf.WorkCycle();
            }
            for (int i = 0; i < 50 ; i++) { Thread.Sleep(5); pf.WorkCycle(); }
            
            Debug.WriteLine($"eventCounter: {eventCounter}");
            Assert.IsTrue(eventCounter == 0);

        }
    }
}
