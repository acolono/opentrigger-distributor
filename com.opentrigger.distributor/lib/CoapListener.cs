using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;
using CoAP;
using CoAP.Net;
using CoAP.Server;
using CoAP.Server.Resources;

namespace com.opentrigger.distributord
{
    public delegate void ButtonDataReceivedEvent(NameValueCollection data);
    public delegate void ExceptionEvent(Exception exception);

    class ButtonEventCoapResource : Resource
    {
        public ButtonEventCoapResource(string name) : base(name)
        {
            Attributes.Title = "POST new button events here";
            Attributes.AddInterfaceDescription("ButtonEvent");
        }

        public event ButtonDataReceivedEvent OnPostData;
        public event ExceptionEvent OnException;

        protected override void DoPost(CoapExchange exchange)
        {
            try
            {
                var source = ((IPEndPoint)exchange.Request.Source);
                var isIPv6 = source.Address.AddressFamily == AddressFamily.InterNetworkV6;
                var ip = source.Address.ToString();
                if (isIPv6) ip = $"[{ip}]";
                var query = HttpUtility.ParseQueryString(exchange.Request.PayloadString);
                query.Add("source", ip);

                OnPostData?.Invoke(query);
                exchange.Respond(StatusCode.Valid);
            }
            catch (Exception exception)
            {
                OnException?.Invoke(exception);
                exchange.Respond(StatusCode.InternalServerError, exception.ToString());
            }
        }

        protected override void DoPut(CoapExchange exchange) {DoPost(exchange);}
    }

    public class CoapListener
    {
        public static readonly IEnumerable<int> DefaultPorts = new []{5683};
        public event ButtonDataReceivedEvent OnButtonData;
        public event ExceptionEvent OnException;
        private CoapServer Server { get; set; }

        public CoapListener()
            : this(DefaultPorts)
        { }

        public CoapListener(IEnumerable<int> ports)
        {
            var coapPorts = ports.ToArray();
            Server = new CoapServer();
            ((CoapConfig)Server.Config).Deduplicator = "Noop";
            foreach (var coapPort in coapPorts)
            {
                Server.AddEndPoint(IPAddress.Any, coapPort);
                Server.AddEndPoint(IPAddress.IPv6Any, coapPort);
                Server.AddEndPoint(IPAddress.Parse("::1"), coapPort);
            }
            
            var buttonResorce = new ButtonEventCoapResource("button");
            buttonResorce.OnPostData += ButtonResorceOnOnPostData;
            buttonResorce.OnException += ButtonResorceOnOnException;
            Server.Add(buttonResorce);
        }

        private void ButtonResorceOnOnException(Exception exception)
        {
            OnException?.Invoke(exception);
        }

        private void ButtonResorceOnOnPostData(NameValueCollection data)
        {
            OnButtonData?.Invoke(data);
        }

        public void Start()
        {
            Server.Start();
        }

        public void Stop()
        {
            Server.Stop();
        }
    }

    public static class NameValueCollectionExt
    {
        public static int? GetInt(this NameValueCollection collection, string key)
        {
            var i = collection.Get(key);
            if (i != null)
            {
                int rval;
                if (int.TryParse(i, out rval)) return rval;
            }
            return null;
        }
    }
}
