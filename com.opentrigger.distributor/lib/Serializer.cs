using Newtonsoft.Json;

namespace com.opentrigger.distributord
{
    public static class Serializer
    {
        public static T Deserialize<T>(this string jsonString) => JsonConvert.DeserializeObject<T>(jsonString);

        public static string Serialize<T>(this T objectToSerialize, bool Indented = true) => JsonConvert.SerializeObject(objectToSerialize,Indented ? Formatting.Indented : Formatting.None);
    }
}
