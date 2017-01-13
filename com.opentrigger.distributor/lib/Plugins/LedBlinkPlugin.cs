using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoAP;

namespace com.opentrigger.distributord.Plugins
{
    // distributord --ledblink [aaaa::221:2eff:ff00:5d62]
    // put coap://[aaaa::221:2eff:ff00:5d62]/led/RGB -p 'r=22&g=33&b=11'

    class LedBlinkPlugin : IPlugin
    {
        public void Start(string[] cmdlineArgs)
        {
            if(cmdlineArgs.Length != 2) return;
            var argIndex = Array.IndexOf(cmdlineArgs, "--blinkled");
            if(argIndex == -1) return;
            var addr = cmdlineArgs[argIndex + 1];
            var uri = new Uri($"coap://{addr}/led/RGB");
            var timeout = 5000;

            var greenRequest = new Request(Method.PUT) {URI = uri, PayloadString = "r=0&g=255&b=0" };
            var offRequest = new Request(Method.PUT) {URI = uri, PayloadString = "r=0&g=0&b=0" };
            greenRequest.Send();
            var greenresponse = greenRequest.WaitForResponse(timeout);
            if(greenresponse == null) throw new TimeoutException("no response");
            offRequest.Send().WaitForResponse(timeout);
        }
    }
}
