using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace com.opentrigger.distributord
{
    public class QueueDistributor
    {
        private readonly MqttClient _client;
        public QueueDistributor(
            string connection = "tcp://127.0.0.1:1883",
            string rawTopic = "/opentrigger/raw/#", 
            string triggerTopic = "/opentrigger/signals/trigger", 
            string releaseTopic = "/opentrigger/signals/release", 
            int idleCycle = 500,
            string clientId = null,
            int verbosity = 0,
            int distance = 2000,
            int skip = 0,
            string[] excudeMacs = null
        )
        {
            var url = new Uri(connection);
            if(url.Scheme != "tcp") throw new InvalidProgramException($"Schema: {url.Scheme} is not supported");
            var port = url.Port >= 0 ? url.Port : 1883;
            _client = new MqttClient(url.Host, port, false, null, null, MqttSslProtocols.None);
            if (string.IsNullOrWhiteSpace(clientId)) clientId = Guid.NewGuid().ToString();
            _client.Connect(clientId); //TODO: parse authentication parameters
            _client.Subscribe(new[] {rawTopic}, new[] {MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE});

            var pf = new PacketFilter(skip, distance, verbosity);
            
            pf.OnTrigger = data =>
            {
                if(verbosity>0) Console.WriteLine($"TRIGGER: {data.UniqueIdentifier}");
                Publish(triggerTopic, data);
                if (verbosity >= 2) DebugPublish(triggerTopic, data);
            };

            pf.OnRelease = data =>
            {
                if (verbosity > 0) Console.WriteLine($"RELEASE: {data.UniqueIdentifier} Age:{DateTimeOffset.UtcNow - data.Timestamp}");
                Publish(releaseTopic, data);
                if (verbosity >= 2) DebugPublish(releaseTopic, data);
            };

            _client.MqttMsgPublishReceived += (sender, args) =>
            {
                byte[] bytes;
                try
                {
                    bytes = Encoding.UTF8.GetString(args.Message).ToBytes();
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Malformed Packet: {Encoding.UTF8.GetString(args.Message)}");
                    Console.WriteLine(exception);
                    return;
                }
                /*         HCI Event && LE Meta         */
                if (bytes[0] == 0x04 && bytes[1] == 0x03e)
                {
                    var mac = bytes.Skip(7).Take(6).Reverse().ToArray().ToHexString(":");
                    if (excudeMacs != null && excudeMacs.Contains(mac))
                    {
                        if(verbosity>=5)Console.WriteLine($"EXCLUDED: {mac}");
                    }
                    else
                    {
                        var data = new PacketData
                        {
                            Packet = bytes,
                            UniqueIdentifier = mac, /* TODO: use the right thing! */
                            OriginTopic = args.Topic
                        };
                        pf.Add(data);
                    }
                    
                }

            };

            while (_client.IsConnected)
            {
                System.Threading.Thread.Sleep(idleCycle);
                pf.Cleanup();
            }
        }

        private ushort Publish(string topic, object data) => _client.Publish(topic, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)));

        private void DebugPublish(string topic, object data)
        {
            Console.WriteLine("Publishing To: " + topic);
            Console.WriteLine(JsonConvert.SerializeObject(data,Formatting.Indented));
        }
    }


}
