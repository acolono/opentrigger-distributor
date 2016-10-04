using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using uPLibrary.Networking.M2Mqtt;

namespace com.opentrigger.distributord
{

    public class CoapServerTrigger : PacketBase
    {
        public int EventId { get; set; }
    }
    public class CoapServerRelease : CoapServerTrigger
    {
        public int Age { get; set; }
    }

    public class CoapServerDistributor : IDistributor
    {
        private readonly CoapServerDistributorConfig _config;
        private readonly int _verbosity;
        private MqttClient _mqttClient;
        private CoapListener _listener;

        private readonly ConcurrentQueue<CoapServerIncoming> _incomingQueue = new ConcurrentQueue<CoapServerIncoming>();
        private class CoapServerIncoming
        {
            public string Source { get; set; }
            public int EventId { get; set; }
            public bool ButtonState { get; set; }
            public DateTimeOffset Timestamp { get; set; }
        }

        public CoapServerDistributor(CoapServerDistributorConfig config, int verbosity)
        {
            _config = config;
            _verbosity = verbosity;
            Setup();
        }

        private void Setup()
        {
            _mqttClient = MqttClientFactory.CreateConnectedClient(_config);
            _listener = new CoapListener(_config.Port);
            _listener.OnButtonData += OnButtonData;
            _listener.OnException += OnException;
            _listener.Start();
        }

        private void OnException(Exception exception)
        {
            if(_verbosity>0) Console.WriteLine(exception.ToString());
        }

        private void OnButtonData(NameValueCollection data)
        {
            try
            {
                var n = new CoapServerIncoming
                {
                    ButtonState = data.GetInt("state").Value == 1,
                    EventId = data.GetInt("event").Value,
                    Source = data.Get("source"),
                    Timestamp = DateTimeOffset.UtcNow,
                };
                if (string.IsNullOrWhiteSpace(n.Source)) throw new InvalidOperationException("source missing");
                _incomingQueue.Enqueue(n);
            }
            catch (Exception ex)
            {
                if (_verbosity > 0)
                {
                    Console.WriteLine($"Malformed Packet: {data.ToDictionary().Serialize()} Ext:{ex.Message} ");
                }
            }

            Dequeue(1);
        }

        private class ButtonState : CoapServerIncoming
        {
            public ButtonState(CoapServerIncoming incoming = null)
            {
                if(incoming == null) return;
                ButtonState = incoming.ButtonState;
                EventId = incoming.EventId;
                Source = incoming.Source;
                Timestamp = incoming.Timestamp;
            }
        }

        private readonly List<ButtonState> _buttonStates = new List<ButtonState>();

        private void Dequeue(int limit)
        {
            while (limit-- > 0)
            {
                CoapServerIncoming workload;
                if(!_incomingQueue.TryDequeue(out workload)) break;

                lock (_buttonStates)
                {
                    if (!workload.ButtonState)
                    {
                        //DOWN
                        var isDuplicate = _buttonStates.Any(s => s.ButtonState == workload.ButtonState && s.Source == workload.Source);
                        if (!isDuplicate)
                        {
                            var trigger = new CoapServerTrigger
                            {
                                EventType = EventType.Trigger,
                                Timestamp = workload.Timestamp,
                                Origin = _config.Origin,
                                UniqueIdentifier = workload.Source,
                                EventId = workload.EventId,
                            };
                            _buttonStates.Add(new ButtonState(workload));
                            Publish(_config.TriggerTopic, trigger);
                        }
                    }
                    else
                    {
                        //UP
                        var matchingDown = _buttonStates.SingleOrDefault(b => b.Source == workload.Source);
                        if (matchingDown != null)
                        {
                            var age = workload.Timestamp - matchingDown.Timestamp;
                            var release = new CoapServerRelease
                            {
                                EventType = EventType.Release,
                                Timestamp = workload.Timestamp,
                                Origin = _config.Origin,
                                UniqueIdentifier = workload.Source,
                                Age = (int)age.TotalMilliseconds,
                                EventId = matchingDown.EventId,
                            };
                            _buttonStates.RemoveAll(b => b.Source == workload.Source);
                            Publish(_config.ReleaseTopic, release);
                        }
                        else
                        {
                            if(_verbosity >= 3) Console.WriteLine("Up event w/o matching Down:" + workload.Serialize());
                        }

                    }
                }
            }
        }

        private void Publish<T>(string topic, T data) where T:PacketBase
        {
            if (string.IsNullOrWhiteSpace(topic)) return;
            string formatedData = null;
            switch (_config.PublishFormat)
            {
                case PublishFormat.Json:
                    formatedData = data.Serialize(false);
                    break;
                case PublishFormat.JsonPretty:
                    formatedData = data.Serialize(true);
                    break;
            }
            if (string.IsNullOrWhiteSpace(formatedData))
            {
                if (_verbosity >= 3) Console.WriteLine($"Failed to publish as {_config.PublishFormat}");
                return;
            }
            var qos = (byte)_config.QosLevels.First();
            _mqttClient.Publish(topic, Encoding.UTF8.GetBytes(formatedData), qos, false);
            if (_verbosity < 2) return;
            Console.WriteLine("Publishing To: " + topic);
            Console.WriteLine(data.Serialize());
        }


        public void Distribute()
        {
            Dequeue(16);
            Cleanup();
        }

        private void Cleanup()
        {
            lock (_buttonStates)
            {
                _buttonStates.RemoveAll(b =>
                {
                    var age = DateTimeOffset.UtcNow - b.Timestamp;
                    var toOld = age.TotalMinutes > _config.CleanupMaxAge;
                    if (toOld && _verbosity >= 3)
                    {
                        Console.WriteLine($"Droping {toOld} seconds (to)old package: {b.Serialize()}");
                    }
                    return toOld;
                });
            }
        }
    }
}
