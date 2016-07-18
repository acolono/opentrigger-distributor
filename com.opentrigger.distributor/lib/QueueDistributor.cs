using System;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;

namespace com.opentrigger.distributord
{
    public delegate bool ByteFilter(byte[] packetBytes);

    public class QueueDistributor
    {
        private readonly MqttClient _client;
        private readonly QueueDistributorConfig _config;
        public ByteFilter ByteFilter = p => p[0] == 0x04 && p[1] == 0x3e; /* HCI Event && LE Meta */
        private readonly PacketFilter _packetFilter;
        private readonly int _verbosity = 0;
        public QueueDistributor(QueueDistributorConfig config, int verbosity = 0)
        {
            _verbosity = verbosity;
            _config = config;
            var url = new Uri(_config.Connection);
            if(url.Scheme != "tcp") throw new InvalidProgramException($"Schema: {url.Scheme} is not supported");
            var port = url.Port > 0 ? url.Port : 1883;
            _client = new MqttClient(url.Host, port, false, null, null, MqttSslProtocols.None);
            if (string.IsNullOrWhiteSpace(_config.ClientId)) config.ClientId = Guid.NewGuid().ToString();
            _client.Connect(_config.ClientId); //TODO: parse authentication parameters
            _client.Subscribe(_config.RawHexTopics.ToArray(), _config.QosLevels.Select(qosLevel => (byte)qosLevel).ToArray());

            _packetFilter = new PacketFilter(_config.Skip, _config.Distance, _verbosity);
            
            _packetFilter.OnTrigger = data =>
            {
                if (_verbosity > 0) Console.WriteLine($"TRIGGER: {data.UniqueIdentifier}");
                Publish(_config.TriggerTopic, data);
            };

            _packetFilter.OnRelease = data =>
            {
                if (_verbosity > 0) Console.WriteLine($"RELEASE: {data.UniqueIdentifier} Age:{DateTimeOffset.UtcNow - data.Timestamp}");
                Publish(_config.ReleaseTopic, data);
            };

            _client.MqttMsgPublishReceived += (sender, args) =>
            {
                BtleDecoded btData = null;
                try
                {
                    var message = Encoding.UTF8.GetString(args.Message);
                    if (message[0] != '{')
                    {
                        // hex stream
                        btData = BtleDecoder.Decode(message);
                    }
                    else
                    {
                        //json
                        var jsonMessage = message.Deserialize<PacketData>();
                        btData = jsonMessage.Packet;
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Malformed Packet: {Encoding.UTF8.GetString(args.Message)}");
                    Console.WriteLine(exception);
                    return;
                }
                
                if (ByteFilter(btData.RawData))
                {
                    if (_config.ExcludedMacs != null && _config.ExcludedMacs.Contains(btData.Mac))
                    {
                        if(_verbosity>=5)Console.WriteLine($"EXCLUDED: {btData.Mac}");
                    }
                    else
                    {
                        if (_config.IncludedMacs == null || _config.IncludedMacs.Contains(btData.Mac))
                        {
                            var data = new PacketData
                            {
                                Packet = btData,
                                OriginTopic = args.Topic
                            };
                            switch (_config.UniqueIdentifier)
                            {
                                case UniqueIdentifier.Mac:
                                    data.UniqueIdentifier = btData.Mac;
                                    break;

                                case UniqueIdentifier.MacAndAdvertisingData:
                                    if (btData?.AdvertisingData != null && btData.AdvertisingData.Length > 0)
                                    {
                                        data.UniqueIdentifier = btData.Mac + "-" + btData.AdvertisingData.ToHexString();
                                    }
                                    break;
                                case UniqueIdentifier.MacAndTokenCubePir:
                                    if (btData?.ManufacturerSpecific?.SensorData != null)
                                    {
                                        var sensorData = btData.ManufacturerSpecific.SensorData;
                                        if (sensorData.ContainsKey("PIR"))
                                        {
                                            var pirString = sensorData["PIR"];
                                            bool pir;
                                            if (!string.IsNullOrWhiteSpace(pirString) && bool.TryParse(pirString, out pir) && pir)
                                            {
                                                data.UniqueIdentifier = btData.Mac;
                                            }
                                        }
                                    }
                                    break;
                                case UniqueIdentifier.Nrf51MacAndTokenCubePir:
                                    if (btData?.ManufacturerSpecific?.SensorData != null)
                                    {
                                        var sensorData = btData.ManufacturerSpecific.SensorData;
                                        if (sensorData.ContainsKey("PIR") && sensorData.ContainsKey("EventId"))
                                        {
                                            var pirString = sensorData["PIR"];
                                            var eventIdString = sensorData["EventId"];
                                            int eventId;
                                            bool pir;
                                            if (!string.IsNullOrWhiteSpace(pirString) && bool.TryParse(pirString, out pir) && pir && !string.IsNullOrWhiteSpace(eventIdString) && int.TryParse(eventIdString, out eventId))
                                            {
                                                data.UniqueIdentifier = $"{btData.Mac}-{eventId:X2}";
                                            }
                                        }
                                    }
                                    break;
                            }
                            if(data?.UniqueIdentifier != null) _packetFilter.Add(data);
                        }
                    }
                    
                }

            };
        }

        public void Distribute()
        {
            _packetFilter.WorkCycle();
            if (!_client.IsConnected)
            {
                throw new WebException("Client not connected");
            }
        }


        private void Publish(string topic, object data)
        {
            if (string.IsNullOrWhiteSpace(topic)) return;
            string formatedData = null;
            switch (_config.PublishFormat)
            {
                case PublishFormat.Json:
                    formatedData = data.Serialize(false);
                    break;
                case PublishFormat.JsonPretty:
                    formatedData = data.Serialize();
                    break;
                case PublishFormat.HexString:
                    formatedData = (data as PacketData)?.Packet.RawData.ToHexString();
                    break;
            }
            if (string.IsNullOrWhiteSpace(formatedData))
            {
                if(_verbosity >= 3) Console.WriteLine($"Failed to publish as {_config.PublishFormat}");
                return;
            }
            _client.Publish(topic, Encoding.UTF8.GetBytes(formatedData));
            if (_verbosity < 2) return;
            Console.WriteLine("Publishing To: " + topic);
            Console.WriteLine(data.Serialize());
        }
    }
}
