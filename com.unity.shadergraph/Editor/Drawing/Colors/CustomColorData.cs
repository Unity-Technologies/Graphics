using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Drawing.Colors
{
    [Serializable]
    class SerializableUserColor
    {
        public string Key = String.Empty;
        public Color Value = Color.black;

        public SerializableUserColor(KeyValuePair<string, Color> pair) { Key = pair.Key; Value = pair.Value; }
        
        // Empty constructor required by serialization system
        public SerializableUserColor() {  }
    }

    [Serializable]
    public class CustomColorData : ISerializationCallbackReceiver
    {
        Dictionary<string, Color> m_CustomColors = new Dictionary<string, Color>();
        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableColors = new List<SerializationHelper.JSONSerializedElement>();

        public bool TryGetColor(string provider, out Color color)
        {
            return m_CustomColors.TryGetValue(provider, out color);
        }
        
        public void Set(string provider, Color color)
        {
            m_CustomColors[provider] = color;
        }

        public void Remove(string provider)
        {
            m_CustomColors.Remove(provider);
        }

        public void OnBeforeSerialize()
        {
            m_SerializableColors.Clear();
            foreach (var customColorKvp in m_CustomColors)
            {
                m_SerializableColors.Add(SerializationHelper.Serialize(new SerializableUserColor(customColorKvp)));
            }
        }

        public void OnAfterDeserialize()
        {
            List<SerializableUserColor> colors = SerializationHelper.Deserialize<SerializableUserColor>(m_SerializableColors, null);
            foreach (var colorPair in colors)
            {
                m_CustomColors.Add(colorPair.Key, colorPair.Value);
            }
        }
    }
}
