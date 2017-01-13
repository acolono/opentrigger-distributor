using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoAP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace com.opentrigger.distributord.Plugins
{
    // distributord --ledblink [aaaa::221:2eff:ff00:5d62]
    // put coap://[aaaa::221:2eff:ff00:5d62]/led/RGB -p 'r=22&g=33&b=11'

    class LedBlinkPlugin : IPlugin
    {
        public void Start(string[] cmdlineArgs)
        {
            if(cmdlineArgs.Length != 2) return;
            var argIndex = Array.IndexOf(cmdlineArgs, "--ledblink");
            if(argIndex == -1) return;

            var jsonResponse = new JsonResponse();

            try
            {
                jsonResponse.UniqueIdentifier = cmdlineArgs[argIndex + 1];
                var uri = new Uri($"coap://{jsonResponse.UniqueIdentifier}/led/RGB");
                var timeout = 5000;

                var greenRequest = new Request(Method.PUT) { URI = uri, PayloadString = "r=0&g=255&b=0" };
                var offRequest = new Request(Method.PUT) { URI = uri, PayloadString = "r=0&g=0&b=0" };
                greenRequest.Send();
                var greenresponse = greenRequest.WaitForResponse(timeout);
                if (greenresponse == null) throw new TimeoutException("green - no response");
                var offresponse = offRequest.Send().WaitForResponse(timeout);
                if (offresponse == null) throw new TimeoutException("off - no response");
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
