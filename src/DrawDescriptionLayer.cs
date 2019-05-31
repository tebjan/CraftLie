using FeralTic.DX11.Geometry;
using SlimDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using Newtonsoft.Json;
using System.IO;
using Ceras;
using Newtonsoft.Json.Bson;
using Polenter.Serialization;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using Newtonsoft.Json.Linq;
using Polenter.Serialization.Advanced;

namespace CraftLie
{
    public class DrawDescriptionLayer
    {
        public static readonly DrawDescriptionLayer Default = new DrawDescriptionLayer(
            GetDefaultDrawDescription().ToList(),
            GetDefaultSpritesDescriptor().ToList(),
            GetDefaultTextDescriptor().ToList());

        public readonly IReadOnlyList<DrawGeometryDescription> GeometryDescriptions;
        public readonly IReadOnlyList<DrawTextDescription> TextDescriptions;
        public readonly IReadOnlyList<DrawSpritesDescription> SpritesDescriptions;

        public DrawDescriptionLayer() 
            : this(new List<DrawGeometryDescription>(),
            new List<DrawSpritesDescription>(),
            new List<DrawTextDescription>())
        {
        }
       
        public DrawDescriptionLayer(IReadOnlyList<DrawGeometryDescription> geometries, IReadOnlyList<DrawSpritesDescription> sprites, IReadOnlyList<DrawTextDescription> texts)
        {
            GeometryDescriptions = geometries;
            SpritesDescriptions = sprites;
            TextDescriptions = texts;
        }

        public static DrawDescriptionLayer Concat(DrawDescriptionLayer input, DrawDescriptionLayer input2)
        {
            return new DrawDescriptionLayer(
                input.GeometryDescriptions.Concat(input2.GeometryDescriptions).ToList(),
                input.SpritesDescriptions.Concat(input2.SpritesDescriptions).ToList(),
                input.TextDescriptions.Concat(input2.TextDescriptions).ToList());
        }

        public static DrawDescriptionLayer Unite(IEnumerable<DrawDescriptionLayer> input)
        {
            return new DrawDescriptionLayer(
                input.SelectMany(d => d.GeometryDescriptions).ToList(),
                input.SelectMany(d => d.SpritesDescriptions).ToList(),
                input.SelectMany(d => d.TextDescriptions).ToList());
        }

        public DrawDescriptionLayer DeepCopy()
        {
            return new DrawDescriptionLayer(
                DeepCopyGeometries(),
                DeepCopySprites(),
                DeepCopyTexts());
        }


        private IReadOnlyList<DrawGeometryDescription> DeepCopyGeometries()
        {
            var result = new List<DrawGeometryDescription>();
            foreach (var e in GeometryDescriptions)
            {
                result.Add(e.DeepCopy());
            }
            return result;
        }

        private IReadOnlyList<DrawSpritesDescription> DeepCopySprites()
        {
            return Default.SpritesDescriptions;
        }

        private IReadOnlyList<DrawTextDescription> DeepCopyTexts()
        {
            var result = new List<DrawTextDescription>();
            foreach(var e in TextDescriptions)
            {
                result.Add(e.DeepCopy());
            }
            return result;
        }

        public static IEnumerable<DrawGeometryDescription> GetDefaultDrawDescription()
        {
            yield return DrawGeometryDescription.Default;
        }

        public static IEnumerable<DrawTextDescription> GetDefaultTextDescriptor()
        {
            yield return DrawTextDescription.Default;
        }

        public static IEnumerable<DrawSpritesDescription> GetDefaultSpritesDescriptor()
        {
            yield return DrawSpritesDescription.Default;
        }

        public static byte[] SerializeCeras(DrawDescriptionLayer layer)
        {
            using (var stream = new MemoryStream())
            {
                var s = new CerasSerializer();
                return s.Serialize(layer);
            }
        }

        public static byte[] SerializeSharp(DrawDescriptionLayer layer)
        {
            using (var ms = new MemoryStream())
            {
                var s = new SharpSerializer(true);
                s.Serialize(layer.GeometryDescriptions.ToList(), ms);
                return ms.ToArray();
            }
        }

        public static DrawDescriptionLayer DeserializeSharp(byte[] layer)
        {
            using (var ms = new MemoryStream(layer))
            {
                var s = new SharpSerializer(true);
                var o = s.Deserialize(ms);
                return new DrawDescriptionLayer((List<DrawGeometryDescription>)o, new List<DrawSpritesDescription>(), new List<DrawTextDescription>());
            }
        }

        static JsonSerializerSettings js = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            ContractResolver = ShouldSerializeContractResolver.Instance,
            TypeNameHandling = TypeNameHandling.None
        };

        public static byte[] SerializeBson(DrawDescriptionLayer layer)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BsonWriter(ms))
                {
                    JsonSerializer serializer = JsonSerializer.Create(js);
                    serializer.Serialize(writer, layer);
                    return ms.ToArray();
                }
            }
        }



        public static byte[] SerializeJson(DrawDescriptionLayer layer)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(layer, js));
        }

        static MemoryStream Writer = new MemoryStream();
        static StreamWriter writer = new StreamWriter(Writer, Encoding.UTF8);
        static JsonTextWriter jsonWriter = new JsonTextWriter(writer);
        public static byte[] SerializeJson2(DrawDescriptionLayer layer, out int length)
        {
            Writer.SetLength(0);
            JsonSerializer ser = JsonSerializer.Create(js);
            ser.Serialize(jsonWriter, layer);
            jsonWriter.Flush();
            length = (int)Writer.Length;
            return Writer.GetBuffer();
        }

        public static DrawDescriptionLayer DeserializeJson(byte[] layer)
        {
            //js.Converters.Add(new GeometryDescriptorConverter());
            return JsonConvert.DeserializeObject<DrawDescriptionLayer>(Encoding.UTF8.GetString(layer));
        }

        public static DrawDescriptionLayer DeserializeJson3(byte[] layer)
        {
            using (var s = new MemoryStream(layer))
            using (var sr = new StreamReader(s, Encoding.UTF8))
            using (var reader = new JsonTextReader(sr))
            {
                JsonSerializer serializer = JsonSerializer.CreateDefault();
                return serializer.Deserialize<DrawDescriptionLayer>(reader);
            }
        }

        public static DrawDescriptionLayer DeserializeJson2(string layer)
        {
            using (var sr = new StringReader(layer))
            using (var reader = new FixedJsonTextReader(sr))
            {
                var settings = new JsonSerializerSettings
                {
                    Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        Debug.WriteLine(args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true;
                    },
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                };

                settings.Converters.Add(new Vector3Converter());
                settings.Converters.Add(new BoxConverter());

                return  JsonSerializer.Create(settings).Deserialize<DrawDescriptionLayer>(reader);
            }
        }
    }

    public class ShouldSerializeContractResolver : DefaultContractResolver
    {
        public static readonly ShouldSerializeContractResolver Instance = new ShouldSerializeContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.DeclaringType == typeof(SharpDX.Matrix))
            {
                property.ShouldSerialize =
                    instance =>
                    {
                        return member.MemberType == MemberTypes.Field;
                    };
            }

            return property;
        }
    }

    public class Vector3Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new Vector2(1, 1);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class BoxConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Box);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new Quad();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class FixedJsonTextReader : JsonTextReader
    {
        public FixedJsonTextReader(TextReader reader) : base(reader) { }

        public override string ReadAsString()
        {
            try
            {
                return base.ReadAsString();
            }
            catch (JsonReaderException)
            {
                if (TokenType == JsonToken.PropertyName)
                    SetToken(JsonToken.None);
                throw;
            }
        }
    }

    public class BaseSpecifiedConcreteClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(GeometryDescriptor).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null; // pretend TableSortRuleConvert is not specified (thus avoiding a stack overflow)
            return base.ResolveContractConverter(objectType);
        }
    }

    public class GeometryDescriptorConverter : JsonConverter
    {
        static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new BaseSpecifiedConcreteClassConverter() };

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(GeometryDescriptor);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return new TextDescriptor();

            var jo = JObject.Load(reader);
            var type = jo["PrimitiveType"].Value<PrimitiveType>();
            switch (type)
            {
                case PrimitiveType.Quad:
                    return JsonConvert.DeserializeObject<QuadDescriptor>(jo.ToString(), SpecifiedSubclassConversion);
                    break;
                case PrimitiveType.RoundQuad:
                    break;
                case PrimitiveType.Box:
                    break;
                case PrimitiveType.Disc:
                    break;
                case PrimitiveType.Polygon:
                    return JsonConvert.DeserializeObject<PolygonDescriptor>(jo.ToString(), SpecifiedSubclassConversion);
                    break;
                case PrimitiveType.Sphere:
                    break;
                case PrimitiveType.Cylinder:
                    break;
                case PrimitiveType.Tube:
                    break;
                case PrimitiveType.Line:
                    return JsonConvert.DeserializeObject<LineDescriptor>(jo.ToString(), SpecifiedSubclassConversion);
                    break;
                case PrimitiveType.MeshJoin:
                    break;
                case PrimitiveType.Sprites:
                    break;
                case PrimitiveType.Text:
                    return JsonConvert.DeserializeObject<TextDescriptor>(jo.ToString(), SpecifiedSubclassConversion);
                    break;
                default:
                    break;
            }
            throw new NotImplementedException();
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException(); // won't be called because CanWrite returns false
        }
    }
}
