using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CoAP;
using CoAP.Log;
using CoAP.Net;
using uPLibrary.Networking.M2Mqtt;

namespace com.opentrigger.distributord
{

    class ConsoleLogManager : ILogManager
    {
        public ConsoleLogManager(System.IO.TextWriter textWriter = null)
        {
            _logger = new TextWriterLogger(textWriter ?? Console.Error);
        }

        private readonly ILogger _logger;

        public ILogger GetLogger(Type type) => _logger;

        public ILogger GetLogger(string name) => _logger;
    }

    public class CoapData : PacketBase
    {
        public CoapPacket Packet { get; set; }
    }

    public class CoapReleaseData : CoapData
    {
        public int Age { get; set; }
    }

    public class CoapPacket
    {
        public int EventId { get; set; }
        public ButtonConfiguration CoapButtonConfiguration { get; set; }
    }

    public class CoapButtonStatus
    {
        public string Uri { get; set; }
        public bool Working { get; set; }
        public DateTimeOffset? LastSeen { get; set; }
        public DateTimeOffset? LastDown { get; set; }
        public int? LastEventId { get; set; }
        public ButtonConfiguration Config { get; set; }
        public Request CoapRequest { get; set; }
        public DateTimeOffset? LastRetry { get; set; }
    }

    public class CoapDistributor : IDistributor
    {
        private MqttClient _mqttClient;
        private readonly List<CoapButtonStatus> _buttonStates = new List<CoapButtonStatus>();
        private readonly CoapDistributorConfig _config;
        private readonly int _verbosity;

        public CoapDistributor(CoapDistributorConfig config, int verbosity)
        {
            _config = config;
            _verbosity = verbosity;
            Setup();
        }

        private void Setup()
        {
            _mqttClient = MqttClientFactory.CreateConnectedClient(_config);
            if (_verbosity > 0)
            {
                switch (_verbosity)
                {
                    case 1: LogManager.Level = LogLevel.Fatal; break;
                    case 2: LogManager.Level = LogLevel.Error; break;
                    case 3: LogManager.Level = LogLevel.Warning; break;
                    case 4: LogManager.Level = LogLevel.Info; break;
                    case 5: LogManager.Level = LogLevel.Debug; break;
                    default: LogManager.Level = LogLevel.All; break;
                }
                LogManager.Level = LogLevel.All;
                LogManager.Instance = new ConsoleLogManager();
            }

            //((CoapConfig) EndPointManager.Default.Config).NotificationReregistrationBackoff = 1000*60*60;

            // TODO: Try paralell with multiple buttons
            foreach (var buttonConfiguration in _config.ButtonConfigurations)
            {
                SetupButton(buttonConfiguration);
            }
        }

        private void SetupButton(ButtonConfiguration buttonConfig)
        {

            var request = new Request(Method.GET) { URI = buttonConfig.BuildButtonUri()};
            request.TimedOut += TimeoutHandler;
            request.Rejected += TimeoutHandler;
            request.Respond += ResponseHandler;
            request.MarkObserve();

            lock (_buttonStates)
            {
                _buttonStates.Add(new CoapButtonStatus
                {
                    Uri = request.URI.ToString(),
                    Config = buttonConfig,
                    CoapRequest = request,
                    Working = false,
                });

                //TODO: Move!
                //var initBlinkUrl = buttonConfig.BuildButtonUri();
                //if (initBlinkUrl != null && !string.IsNullOrWhiteSpace(buttonConfig.InitLedPayload))
                //{
                //    var initBlink = new Request(Method.PUT)
                //    {
                //        URI = initBlinkUrl,
                //        PayloadString = buttonConfig.InitLedPayload
                //    };
                //    initBlink.Send();
                //}   
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

        private void ResponseHandler(object o, ResponseEventArgs e)
        {
            
            
            if(e.Response.Duplicate) return; /* only handles coap duplicates */

            // Parse: EVENT 0 STATE 0
            var match = Regex.Match(e.Message.PayloadString, @"^EVENT\s(\S+)\sSTATE\s([01])");
            if (!match.Success)
            {
                if(_verbosity>0) Console.WriteLine($"Unable to parse payload: {e.Message.PayloadString}");
                return;
            }
            var eventId = int.Parse(match.Groups[1].Value);
            var state = int.Parse(match.Groups[2].Value) == 1;
            

            lock (_buttonStates)
            {
                var req = (Request)o;
                var status = _buttonStates.SingleOrDefault(s => s.Uri == req.URI.ToString());
                if (status == null) return;
                status.LastSeen = DateTimeOffset.UtcNow;
                status.Working = true;
                if(status.LastEventId.HasValue && status.LastEventId == eventId) return; /* duplicate */
                status.LastEventId = eventId;
                TimeSpan? age = null;

                if (state)
                {
                    //UP
                    if(!status.LastDown.HasValue) return; /* lingering/duplicate, etc... */
                    age = DateTimeOffset.UtcNow - status.LastDown.Value;
                    status.LastDown = null;

                    var data = new CoapReleaseData
                    {
                        Origin = req.URI.GetLeftPart(UriPartial.Authority),
                        UniqueIdentifier = req.URI.ToString(),
                        Timestamp = DateTimeOffset.UtcNow,
                        Age = (int)age.Value.TotalMilliseconds,
                        Packet = new CoapPacket
                        {
                            CoapButtonConfiguration = status.Config,
                            EventId = eventId
                        }
                    };
                    Publish(_config.ReleaseTopic, data);

                    var ackUrl = status.Config.BuildLedUri();
                    if (ackUrl != null && !string.IsNullOrWhiteSpace(status.Config.AckLedPayload))
                    {
                        var initBlink = new Request(Method.PUT)
                        {
                            URI = ackUrl,
                            PayloadString = status.Config.AckLedPayload,
                        };
                        initBlink.Send();
                    }

                }
                else
                {
                    //DOWN
                    status.LastDown = DateTimeOffset.UtcNow;

                    var data = new CoapData
                    {
                        Origin = req.URI.GetLeftPart(UriPartial.Authority),
                        UniqueIdentifier = req.URI.ToString(),
                        Timestamp = DateTimeOffset.UtcNow,
                        Packet = new CoapPacket
                        {
                            CoapButtonConfiguration = status.Config,
                            EventId = eventId
                        }
                    };
                    Publish(_config.TriggerTopic, data);
                }

                var origin = req.URI.GetLeftPart(UriPartial.Authority);
                var uid = req.URI.ToString();
                if(_verbosity > 0) Console.WriteLine($"eventId={eventId}, state={(state?"Up":"Down")}, age={age}, origin={origin}, uid={uid}");
                //Console.WriteLine(_buttonStates.Serialize());
            }
        }

        private void TimeoutHandler(object o, EventArgs e)
        {
            var r = (Request) o;
            lock (_buttonStates)
            {
                var status = _buttonStates.SingleOrDefault(b => b.Uri == r.URI.ToString());
                if (status != null)
                {
                    status.Working = false;
                    if(_verbosity>0) Console.WriteLine($"Button went dead: " + status.Uri);
                }
            }
        }

        public void Distribute()
        {
            lock (_buttonStates)
            {
                var defunktButtons = _buttonStates.Where(b => !b.Working || !b.LastSeen.HasValue || (DateTimeOffset.UtcNow - b.LastSeen.Value).TotalSeconds > 60).ToList();
                foreach (var defunktButton in defunktButtons)
                {
                    defunktButton.Working = false;
                    if (!defunktButton.LastRetry.HasValue || (DateTimeOffset.UtcNow - defunktButton.LastRetry.Value).TotalSeconds > 60)
                    {
                        if (_verbosity > 0) Console.WriteLine($"Trying to reconnect to button: " + defunktButton.Uri);
                        defunktButton.CoapRequest.Send();
                        defunktButton.LastRetry = DateTimeOffset.UtcNow;
                    }
                }
            }
        }
    }
}
