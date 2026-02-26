using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    class DataDescriptionWriterRegistry
    {
        Dictionary<System.Type, IDataDescriptionWriter> m_Writers = new();

        public void Register(IDataDescriptionWriter writer)
        {
            var type = writer.DataDescriptionType;
            Debug.Assert(!m_Writers.ContainsKey(type));
            m_Writers.Add(type, writer);
        }

        public bool TryGetDataDescriptionWriter(IDataDescription dataDescription, out IDataDescriptionWriter dataDescriptionWriter)
        {
            return m_Writers.TryGetValue(dataDescription.GetType(), out dataDescriptionWriter);
        }

        public string GetSubdataName(DataView dataView, IDataKey subDataKey)
        {
            return m_Writers.TryGetValue(dataView.DataDescription.GetType(), out var writer) ? writer.GetSubdataName(subDataKey) : null;
        }

        public string GetSubdataTypeName(DataView dataView, IDataKey subDataKey)
        {
            return m_Writers.TryGetValue(dataView.DataDescription.GetType(), out var writer) ? writer.GetSubdataTypeName(subDataKey) : null;
        }

        public string FindDynamicDataTypeName(DataView dataView)
        {
            if (dataView.Parent.HasValue)
            {
                return FindDynamicDataTypeName(dataView.Parent.Value) + GetSubdataTypeName(dataView.Parent.Value, dataView.SubDataKey);
            }
            else
            {
                return dataView.DataContainer.Name;
            }
        }

        public string FindDataTypeName(DataView dataView)
        {
            if (TryGetDataDescriptionWriter(dataView.DataDescription, out var _))
            {
                return FindDynamicDataTypeName(dataView);
            }
            else if (dataView.DataDescription is ValueData valueData)
            {
                if (typeof(Texture).IsAssignableFrom(valueData.Type)) // TODO: Binding resources directly, instead of declaring and initializing
                    return $"{HlslCodeHelper.GetTypeName(valueData.Type)}";
                else
                    return $"static {HlslCodeHelper.GetTypeName(valueData.Type)}";
            }
            else
            {
                return "UnknownType";
            }
        }
    }
}
