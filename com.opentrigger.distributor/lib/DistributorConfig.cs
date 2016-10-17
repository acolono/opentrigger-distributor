using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace com.opentrigger.distributord
{
    [JsonObject(MemberSerialization.OptOut)]
    public class DistributorConfigBase
    {
        public string Connection { get; set; } = "tcp://127.0.0.1:1883";
        public IEnumerable<QosLevel> QosLevels { get; set; } = new[] { QosLevel.AtMostOnce };
        public string TriggerTopic { get; set; } = "/opentrigger/signals/trigger";
        public string ReleaseTopic { get; set; } = "/opentrigger/signals/release";
        public PublishFormat PublishFormat { get; set; } = PublishFormat.JsonPretty;
        public string ClientId { get; set; }
    }
    [JsonObject(MemberSerialization.OptOut)]
    public class FlicDistributorConfig : DistributorConfigBase
    {
        public IEnumerable<string> RawFlicTopics { get; set; } = new[] { "/opentrigger/rawflic" };
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class QueueDistributorConfig : DistributorConfigBase
    {
        public IEnumerable<string> RawHexTopics { get; set; } = new []{"/opentrigger/rawhex/#"};
        public int Distance { get; set; } = 500;
        public int Skip { get; set; } = 0;
        public UniqueIdentifier UniqueIdentifier { get; set; } = UniqueIdentifier.Mac;
        public IEnumerable<string> ExcludedMacs { get; set; } = null;
        public IEnumerable<string> IncludedMacs { get; set; } = null;
    }

    [JsonConverter(typeof (StringEnumConverter))]
    public enum PublishFormat
    {
        Json,
        JsonPretty,
        HexString, /* convert for wireshark like: echo $LINE| xxd -r -p | od -Ax -tx1 -v */
        //TODO: Bson,Binary,?
    }

    [JsonConverter(typeof (StringEnumConverter))]
    public enum EventType
    {
        Trigger,
        Release,
    }

    [JsonConverter(typeof (StringEnumConverter))]
    public enum UniqueIdentifier
    {
        Mac,
        MacAndAdvertisingData,
        MacAndTokenCubePir,
        Nrf51MacAndTokenCubePir,
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
        public IEnumerable<CoapDistributorConfig> CoapDistributorConfigs { get; set; }
        public IEnumerable<FlicDistributorConfig> FlicDistributorConfigs { get; set; }
        public IEnumerable<CoapServerDistributorConfig> CoapServerDistributorConfigs { get; set; }
        public int Verbosity { get; set; } = 1;
        public int IdleCycle { get; set; } = 500;
        public bool RunParallel { get; set; } = false;
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class ButtonConfiguration
    {
        public string BaseUri { get; set; }
        public string ButtonPath { get; set; } = "s/button";
        public string LedPath { get; set; } = "s/led";
        public string InitLedPayload { get; set; } = "mode=blink&times=5&delay=30";
        public string AckLedPayload { get; set; } = "mode=blink";
        public double? KeepaliveRequestInterval { get; set; }
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class CoapDistributorConfig : DistributorConfigBase
    {
        public IEnumerable<ButtonConfiguration> ButtonConfigurations;
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class CoapServerDistributorConfig : DistributorConfigBase
    {
        public IEnumerable<int> Port { get; set; } = CoapListener.DefaultPorts;
        public string Origin { get; set; } = null;
        public int CleanupMaxAge { get; set; } = 3000;
    }
}
