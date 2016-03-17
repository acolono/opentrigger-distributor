using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace com.opentrigger.distributord
{
    [JsonObject(MemberSerialization.OptOut)]
    public class QueueDistributorConfig
    {

        public string Connection { get; set; } = "tcp://127.0.0.1:1883";
        public IEnumerable<QosLevel> QosLevels { get; set; } = new[] { QosLevel.AtMostOnce };
        public IEnumerable<string> RawHexTopics { get; set; } = new []{"/opentrigger/rawhex/#"};
        public string TriggerTopic { get; set; } = "/opentrigger/signals/trigger";
        public string ReleaseTopic { get; set; } = "/opentrigger/signals/release";
        public PublishFormat PublishFormat { get; set; } = PublishFormat.Json;
        public string ClientId { get; set; } = null;
        public int Distance { get; set; } = 2000;
        public int Skip { get; set; } = 1;
        public UniqueIdentifier UniqueIdentifier { get; set; } = UniqueIdentifier.Mac;
        public IEnumerable<string> ExcludedMacs { get; set; } = null;
        public IEnumerable<string> IncludedMacs { get; set; } = null;

    }

    [JsonConverter(typeof (StringEnumConverter))]
    public enum PublishFormat
    {
        Json,
        JsonPretty,
        HexString,
        //TODO: Bson,Binary,?
    }

    [JsonConverter(typeof (StringEnumConverter))]
    public enum UniqueIdentifier
    {
        Mac,
        MacAndAdvertisingData,
        MacAndTokenCubePir,

    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum QosLevel
    {
        AtMostOnce = MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
        AtLeastOnce = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
        ExactlyOnce = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class DistributorConfig
    {
        public IEnumerable<QueueDistributorConfig> QueueDistributorConfigs { get; set; }
        public int Verbosity { get; set; } = 1;
        public int IdleCycle { get; set; } = 500;
        public bool RunParallel { get; set; } = false;
    }
}
