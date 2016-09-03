using System;

namespace com.opentrigger.distributord
{
    public static class ConfigExtensions
    {
        public static Uri BuildButtonUri(this ButtonConfiguration buttonConfig) => buttonConfig.BuildUri(buttonConfig.ButtonPath);
        public static Uri BuildLedUri(this ButtonConfiguration buttonConfig) => buttonConfig.BuildUri(buttonConfig.LedPath);

        private static Uri BuildUri(this ButtonConfiguration buttonConfig, string path)
        {
            if (string.IsNullOrWhiteSpace(buttonConfig.BaseUri)) return null;
            var ub = new UriBuilder(buttonConfig.BaseUri) { Path = path };
            return ub.Uri;
        }
    }
}