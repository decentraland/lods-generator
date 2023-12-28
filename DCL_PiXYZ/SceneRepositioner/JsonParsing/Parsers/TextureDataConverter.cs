// unset:none
using System;
using DCL_PiXYZ.SceneRepositioner.SceneBuilder.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DCL_PiXYZ.SceneRepositioner.JsonParsing.Parsers
{
    public class TextureDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            (objectType == typeof(Texture));

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            Texture texture = new Texture();
            texture.src = jsonObject["texture"]["src"].Value<string>();
            return texture;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

