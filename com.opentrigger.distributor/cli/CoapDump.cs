using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.opentrigger.distributord;

namespace com.opentrigger.cli.distributord
{
    internal static class CoapDump
    {

        public static void SetupCoapDump(this string[] args)
        {
            var coapdumpIndex = Array.IndexOf(args, "--coapdump");
            if(coapdumpIndex == -1) return;

            var l = new CoapListener();

            l.OnButtonData += data =>
            {
                IDictionary<string, string> dict = new Dictionary<string, string>();
                foreach (var k in data.AllKeys)
                {
                    dict.Add(k, data[k]);
                }
                Console.WriteLine(dict.Serialize());
            };

            l.OnException += exception =>
            {
                Console.WriteLine($"Ex: {exception.ToString()}");
            };

            l.Start();

            while (true) System.Threading.Thread.Sleep(500);
        }
    }
}
