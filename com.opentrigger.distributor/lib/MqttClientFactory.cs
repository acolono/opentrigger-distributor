using System;
using System.Linq;
using uPLibrary.Networking.M2Mqtt;

namespace com.opentrigger.distributord
{
    public static class MqttClientFactory
    {
        public static MqttClient CreateConnectedClient(DistributorConfigBase config)
        {

            var url = new Uri(config.Connection);
            var validSchemas = new [] {"tcp", "mqtt"};
            if (!validSchemas.Contains(url.Scheme)) throw new InvalidProgramException($"Schema: {url.Scheme} is not supported");
            var port = url.Port > 0 ? url.Port : 1883;
            var client = new MqttClient(url.Host, port, false, null, null, MqttSslProtocols.None);
            if (string.IsNullOrWhiteSpace(config.ClientId)) config.ClientId = Guid.NewGuid().ToString();
            if (!string.IsNullOrWhiteSpace(url.UserInfo))
            {
                var username = url.GetUsername();
                var password = url.GetPassword();
                client.Connect(config.ClientId, username, password);
            }
            else
            {
                client.Connect(config.ClientId); 
            }
            return client;
        }
        
    }

    public static class UriExtensions
    {
        public static string GetUsername(this Uri uri)
        {
            var items = uri.UserInfo.Split(':');
            return items.Length > 0 ? items[0] : string.Empty;
        }

        public static string GetPassword(this Uri uri)
        {
            var items = uri.UserInfo.Split(':');
            return items.Length > 1 ? items[1] : string.Empty;
        }
    }

}