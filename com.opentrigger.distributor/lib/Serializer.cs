using System.Collections.Generic;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace com.opentrigger.distributord
{
    public static class Serializer
    {
        public static T Deserialize<T>(this string jsonString) => JsonConvert.DeserializeObject<T>(jsonString);

        public static string Serialize<T>(this T objectToSerialize, bool Indented = true) => JsonConvert.SerializeObject(objectToSerialize,Indented ? Formatting.Indented : Formatting.None);

        public static IDictionary<string, string> ToDictionary(this NameValueCollection col)
        {
            IDictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var k in col.AllKeys)
            {
                dict.Add(k, col[k]);
            }
            return dict;
        }
    }
}
