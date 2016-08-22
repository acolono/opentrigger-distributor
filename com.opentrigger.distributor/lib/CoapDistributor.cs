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

    public class CoapButtonStatus
    {
        public string Uri { get; set; }
        public bool Working { get; set; }
        public DateTimeOffset? LastSeen { get; set; }
        public DateTimeOffset? LastDown { get; set; }
        public int? LastEventId { get; set; }
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
            foreach (var buttonUri in _config.ButtonUris)
            {
                SetupButton(buttonUri);
            }
        }

        private void SetupButton(string buttonUri)
        {
            var request = new Request(Method.GET) { URI = new Uri(buttonUri)};
            //((CoapConfig) request.EndPoint.Config).NotificationReregistrationBackoff = 1000*60*60;
            //request.MaxAge = 60*60;

            var statusHandler = new EventHandler<ResponseEventArgs>((o,e) =>
            {
                var r = (Request) o;
                var uri = r.URI.ToString();
                lock (_buttonStates)
                {
                    var status = _buttonStates.SingleOrDefault(b => b.Uri == uri);
                    if (status == null)
                    {
                        status = new CoapButtonStatus { Uri = uri };
                        _buttonStates.Add(status);
                    }
                    status.Working = true;
                    status.LastSeen = DateTimeOffset.Now;
                }

            });
            //request.Respond += statusHandler;
            request.MarkObserve();
            request.Send();
            var resp = request.WaitForResponse(60000);
            if (resp != null) statusHandler(request, null);

            lock (_buttonStates)
            {
                if(_buttonStates.Any(s=>s.Uri == request.URI.ToString() && s.Working))
                {
                    request.TimedOut += TimeoutHandler;
                    request.Respond += statusHandler;
                    request.Respond += ResponseHandler;
                }
                else
                {
                    if(_verbosity > 0) Console.WriteLine($"Button not responding, registration failed: {request.URI}");
                }
            }
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
                status.LastSeen = DateTimeOffset.Now;
                status.Working = true;
                if(status.LastEventId.HasValue && status.LastEventId == eventId) return; /* duplicate */
                status.LastEventId = eventId;
                TimeSpan? age = null;

                if (state)
                {
                    //UP
                    if(!status.LastDown.HasValue) return; /* lingering/duplicate, etc... */
                    age = DateTimeOffset.Now - status.LastDown.Value;
                    status.LastDown = null;
                }
                else
                {
                    //DOWN
                    status.LastDown = DateTimeOffset.Now;
                }

                var origin = req.URI.GetLeftPart(UriPartial.Authority);
                var uid = req.URI.ToString();

                Console.WriteLine($"eventId={eventId}, state={state}, age={age}, origin={origin}, uid={uid}");
                Console.WriteLine(_buttonStates.Serialize());
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
            // TODO: Try to recover non working buttons
        }
    }
}
