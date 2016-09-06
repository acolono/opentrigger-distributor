using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace com.opentrigger.distributord
{
    class SimpleclientJson
    {
        [Newtonsoft.Json.JsonProperty("m")]
        public string Mac { get; set; }
        [Newtonsoft.Json.JsonProperty("e")]
        public FlicClickType Event { get; set; }
        [Newtonsoft.Json.JsonProperty("i")]
        public int EventId { get; set; }
    }

    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    enum FlicClickType
    {
        ButtonDown,
        ButtonUp,
        ButtonClick,
        ButtonSingleClick,
        ButtonDoubleClick,
        ButtonHold
    }

    public class FlicData : PacketBase
    {
    }

    public class FlicReleaseData : FlicData
    {
        public int Age { get; set; }
    }

    class FlicButtonInfo
    {
        public string Mac { get; set; }
        public DateTimeOffset? LastDown { get; set; }
    }

    public class FlicDistributor : IDistributor
    {
        private readonly FlicDistributorConfig _config;
        private int _verbosity;
        private readonly MqttClient _mqttClient;
        private readonly List<FlicButtonInfo> _buttonInfos = new List<FlicButtonInfo>();


        public FlicDistributor(FlicDistributorConfig config, int verbosity)
        {
            _config = config;
            _verbosity = verbosity;
            _mqttClient = MqttClientFactory.CreateConnectedClient(_config);
            _mqttClient.Subscribe(_config.RawFlicTopics.ToArray(), _config.QosLevels.Select(qosLevel => (byte)qosLevel).ToArray());
            _mqttClient.MqttMsgPublishReceived += RawFlicMsgHandler;
        }

        private void RawFlicMsgHandler(object s, MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message);
            var json = message.Deserialize<SimpleclientJson>();
            EventType eventType;

            switch (json.Event)
            {
                case FlicClickType.ButtonDown: eventType = EventType.Trigger; break;
                case FlicClickType.ButtonUp: eventType = EventType.Release; break;
                default:
                    return; // only Up and Down Events are supported
            }

            FlicReleaseData pubMsg;
            lock (_buttonInfos)
            {
                var buttonInfo = _buttonInfos.FirstOrDefault(b => b.Mac == json.Mac);
                if (buttonInfo == null)
                {
                    buttonInfo = new FlicButtonInfo {Mac = json.Mac};
                    _buttonInfos.Add(buttonInfo);
                }
                pubMsg = new FlicReleaseData
                {
                    UniqueIdentifier = json.Mac, Timestamp = DateTimeOffset.UtcNow,
                    Origin = e.Topic, EventType = eventType,
                };

                if (buttonInfo.LastDown.HasValue && eventType == EventType.Release)
                {
                    pubMsg.Age = (int) (DateTimeOffset.UtcNow - buttonInfo.LastDown.Value).TotalMilliseconds;
                    buttonInfo.LastDown = null;
                }
                else if (eventType == EventType.Trigger)
                {
                    buttonInfo.LastDown = DateTimeOffset.UtcNow;
                }
                else
                {
                    //duplicate/etc...
                    return;
                }

                switch (eventType)
                {
                    case EventType.Trigger:
                        PublishTrigger(pubMsg);
                        break;
                    case EventType.Release:
                        PublishRelease(pubMsg);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void PublishTrigger(FlicReleaseData data)
        {
            var flicData = new FlicData
            {
                EventType = data.EventType,
                Origin = data.Origin,
                Timestamp = data.Timestamp,
                UniqueIdentifier = data.UniqueIdentifier,
            };
            Publish(_config.TriggerTopic, flicData);
        }

        private void PublishRelease(FlicReleaseData data)
        {
            Publish(_config.ReleaseTopic, data);
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
                    formatedData = data.Serialize(true);
                    break;
            }
            if (string.IsNullOrWhiteSpace(formatedData))
            {
                if (_verbosity >= 3) Console.WriteLine($"Failed to publish as {_config.PublishFormat}");
                return;
            }
            _mqttClient.Publish(topic, Encoding.UTF8.GetBytes(formatedData));
            if (_verbosity < 2) return;
            Console.WriteLine("Publishing To: " + topic);
            Console.WriteLine(data.Serialize());
        }

        public void Distribute()
        {
/*no loop, event driven...*/
        }
    }
}
