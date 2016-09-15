using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using CoAP;
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
                var source = ((IPEndPoint)exchange.Request.Source).Address.ToString();
                var query = HttpUtility.ParseQueryString(exchange.Request.PayloadString);
                query.Add("source", source);

                OnPostData?.Invoke(query);

                var sb = new StringBuilder();

                foreach (var q in query.AllKeys)
                {
                    sb.AppendLine($"{q}: '{query.Get(q)}'");
                }
                Console.Write(sb.ToString());
                exchange.Respond(StatusCode.Content, sb.ToString());
            }
            catch (Exception exception)
            {
                OnException?.Invoke(exception);
                exchange.Respond(StatusCode.InternalServerError, exception.ToString());
            }
        }
    }

    public class CoapListener
    {
        public event ButtonDataReceivedEvent OnButtonData;
        public event ExceptionEvent OnException;
        public readonly int[] Ports;
        private CoapServer Server { get; set; }

        public CoapListener(IEnumerable<int> ports )
        {
            Ports = ports.ToArray();
            Server = new CoapServer(Ports);
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
}
