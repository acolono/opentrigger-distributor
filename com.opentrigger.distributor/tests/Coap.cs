﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using com.opentrigger.distributord;
using CoAP;
using NUnit.Core;
using NUnit.Framework;

namespace com.opentrigger.tests
{
    [TestFixture]
    public class Coap
    {
        [Test]
        public void RoundTrip()
        {

            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var freePort = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();

            var listener = new CoapListener(new []{ freePort });
            listener.Start();

            var uri = new UriBuilder("coap://localhost/button") {Port = freePort};

            NameValueCollection data = null;
            listener.OnButtonData += d => { data = d; };
            listener.OnException += exception =>
            {
                Assert.Fail(exception.ToString());
            };

            var request = new Request(Method.POST)
            {
                URI = uri.Uri,
                PayloadString = "event=1&state=2"
            };
            request.Send();
            var response = request.WaitForResponse(1000);
            listener.Stop();
            
            Assert.IsNotNull(data);
            Assert.IsTrue(data.GetInt("event") == 1);
            Assert.IsTrue(data.GetInt("state") == 2);
            Assert.IsFalse(string.IsNullOrWhiteSpace(data.Get("source")));
            Console.WriteLine(data.Get("source"));
            var validResponseStatusCodes = new List<StatusCode>() { StatusCode.Valid, StatusCode.Content, StatusCode.Changed, StatusCode.Continue, StatusCode.Created, StatusCode.Deleted};
            Assert.IsTrue(validResponseStatusCodes.Contains(response.StatusCode));
        }
    }
}
