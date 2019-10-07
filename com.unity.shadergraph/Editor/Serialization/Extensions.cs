using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace UnityEditor.ShaderGraph.Serialization
{
    static class Extensions
    {
        public static JsonProperty Clone(this JsonProperty property)
        {
            return new JsonProperty
            {
                AttributeProvider = property.AttributeProvider,
                Converter = property.Converter,
                DeclaringType = property.DeclaringType,
                DefaultValue = property.DefaultValue,
                DefaultValueHandling = property.DefaultValueHandling,
                GetIsSpecified = property.GetIsSpecified,
                HasMemberAttribute = property.HasMemberAttribute,
                Ignored = property.Ignored,
                IsReference = property.IsReference,
                ItemConverter = property.ItemConverter,
                ItemIsReference = property.ItemIsReference,
                ItemReferenceLoopHandling = property.ItemReferenceLoopHandling,
                ItemTypeNameHandling = property.ItemTypeNameHandling,
                NullValueHandling = property.NullValueHandling,
                ObjectCreationHandling = property.ObjectCreationHandling,
                Order = property.Order,
                PropertyName = property.PropertyName,
                PropertyType = property.PropertyType,
                Readable = property.Readable,
                ReferenceLoopHandling = property.ReferenceLoopHandling,
                Required = property.Required,
                SetIsSpecified = property.SetIsSpecified,
                ShouldDeserialize = property.ShouldDeserialize,
                ShouldSerialize = property.ShouldSerialize,
                TypeNameHandling = property.TypeNameHandling,
                UnderlyingName = property.UnderlyingName,
                ValueProvider = property.ValueProvider,
                Writable = property.Writable
            };
        }
    }
}
