using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class SerializableTextureUpgrader : JsonUpgrader<Texture>
    {
        [Serializable]
        class TextureHelper
        {
#pragma warning disable 649
            public Texture texture;
#pragma warning restore 649
        }

        [Serializable]
        class SerializableTexture
        {
            [JsonProperty("m_SerializedTexture")]
            public string serializedTexture;

            [JsonProperty("m_Guid")]
            public string guid = default;
        }

        public override Texture ReadJson(JsonReader reader, Type objectType, Texture existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var st = serializer.Deserialize<SerializableTexture>(reader);
            var texture = (Texture)null;
            if (!string.IsNullOrEmpty(st.serializedTexture))
            {
                var textureHelper = new TextureHelper();
                EditorJsonUtility.FromJsonOverwrite(st.serializedTexture, textureHelper);
                st.serializedTexture = null;
                texture = textureHelper.texture;
            }
            else if (!string.IsNullOrEmpty(st.guid))
            {
                texture = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(st.guid));
            }

            return texture != null ? texture : null;
        }
    }
}
