using System;
using System.Linq;
using uPLibrary.Networking.M2Mqtt;

namespace com.opentrigger.distributord
{
    public static class MqttClientFactory
    {
        public static MqttClient CreateConnectedClient(DistributorConfigBase config)
        {
            var uri = new UriBuilder(config.Connection);
            var validSchemas = new [] {"tcp", "mqtt"};
            if (!validSchemas.Contains(uri.Scheme)) throw new InvalidProgramException($"Scheme: {uri.Scheme} is not supported");
            var port = uri.Port > 0 ? uri.Port : 1883;
            var client = new MqttClient(uri.Host, port, false, null, null, MqttSslProtocols.None);
            if (string.IsNullOrWhiteSpace(config.ClientId)) config.ClientId = Guid.NewGuid().ToString();
            if (!string.IsNullOrWhiteSpace(uri.Uri.UserInfo))
            {
                var username = uri.UserName;
                var password = uri.Password;
                client.Connect(config.ClientId, username, password);
            }
            else
            {
                client.Connect(config.ClientId); 
            }
            return client;
        }
        
    }

}