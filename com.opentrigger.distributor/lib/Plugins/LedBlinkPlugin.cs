using System;
using CoAP;
using CoAP.Log;

namespace com.opentrigger.distributord.Plugins
{
    // distributord --ledblink [aaaa::221:2eff:ff00:5d62]
    // put coap://[aaaa::221:2eff:ff00:5d62]/led/RGB -p 'r=22&g=33&b=11'

    class LedBlinkPlugin : IPlugin
    {
        public void Start(string[] cmdlineArgs)
        {
            var ledblinkIndex = Array.IndexOf(cmdlineArgs, "--ledblink");
            if(ledblinkIndex == -1) return;

            var payloadIndex = Array.IndexOf(cmdlineArgs, "--payload");
            var debug = Array.IndexOf(cmdlineArgs, "--debug") != -1;

            string payload;
            try
            {
                payload = payloadIndex != -1 ? cmdlineArgs[payloadIndex + 1] : "r=0&g=100&b=0&delay=20&times=5";
            }
            catch (Exception e)
            {
                throw new Exception("cant read payload from commandline parameters", e);
            }

            if (debug)
            {
                LogManager.Level = LogLevel.All;
                LogManager.Instance = new ConsoleLogManager();
            }
            else
            {
                LogManager.Level = LogLevel.None;
            }
            
            var jsonResponse = new JsonResponse();

            try
            {
                jsonResponse.UniqueIdentifier = cmdlineArgs[ledblinkIndex + 1];
                var uri = new Uri($"coap://{jsonResponse.UniqueIdentifier}/led/RGB");
                var timeout = 5000;

                var request = new Request(Method.PUT) { URI = uri, PayloadString = payload };
                request.Send();
                var response = request.WaitForResponse(timeout);
                if (response == null) throw new TimeoutException("green - no response");
                jsonResponse.Success = true;
            }
            catch (Exception e)
            {
                jsonResponse.Success = false;
                jsonResponse.Error = e.Message;
            }
            Console.WriteLine(jsonResponse.Serialize(false));
            Environment.Exit(jsonResponse.Success ? 0 : 1);
        }

        public class JsonResponse
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public string UniqueIdentifier { get; set; }
        }

    }
}
