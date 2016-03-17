using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace com.opentrigger.distributord
{
    public class RoundtripTime
    {
        private int Verbosity { get; }

        private readonly Random _rnd = new Random();

        public RoundtripTime(string server, int padding = 0, int verbosity = 0)
        {
            Verbosity = verbosity;
            _binaryFormatter = new BinaryFormatter();
            var client = new MqttClient(server);
            var clientId = Guid.NewGuid().ToString();
            var outChannel = "/opentrigger/rtt/" + clientId;
            var inChannels = new string[]
            {
                outChannel
            };

            client.Connect(clientId);
            client.Subscribe(inChannels, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            client.MqttMsgPublishReceived += (sender, eventArgs) =>
            {
                new Task(() => Incoming(eventArgs.Message)).Start();
            };

            while (true)
            {
                using (var ms = new MemoryStream())
                {
                    using (var ds = new DeflateStream(ms, CompressionMode.Compress, true))
                    {
                        _binaryFormatter.Serialize(ds, DateTime.UtcNow);
                        if (padding > 0)
                        {
                            var randomBytes = new byte[padding];
                            _rnd.NextBytes(randomBytes);
                            ds.Write(randomBytes,0,randomBytes.Length);

                        }
                    }
                    client.Publish(outChannel, ms.ToArray());
                }
                if (Interlocked.Increment(ref counter) > 10)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    Thread.Sleep(100);
                }
                
            }
        }

        private int counter = 0;
        private BinaryFormatter _binaryFormatter;
        private List<double> statistics = new List<double>();

        private void Incoming(byte[] msgBytes)
        {
            try
            {
                TimeSpan diff;
                using (var ms = new MemoryStream(msgBytes))
                {
                    using (var ds = new DeflateStream(ms, CompressionMode.Decompress, true))
                    {
                        var ts = (DateTime)_binaryFormatter.Deserialize(ds);
                        diff = DateTime.UtcNow - ts;
                    }
                }
                var cnt = Interlocked.Decrement(ref counter);
                if(Verbosity > 0) Console.WriteLine($"diff: {diff.TotalMilliseconds:000.0} inFlight:{cnt:000} size:{msgBytes.Length}");
                lock (statistics)
                {
                    statistics.Add(diff.TotalMilliseconds);
                    if (statistics.Count > 50)
                    {
                        Console.WriteLine($"avg: {statistics.Average():0000.0} inFlight:{cnt:000} size:{msgBytes.Length}");
                        statistics.RemoveRange(0, 10);
                    }
                }

            }
            catch(Exception ex)
            {
                // ignored
                Console.WriteLine($"ex: {ex}");
            }
        }
    }
}
