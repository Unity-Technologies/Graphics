using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderFoundry
{
    internal class UniformBufferCollection
    {
        internal class UniformBufferObject
        {
            readonly public UniformDataSource DataSource;
            readonly public string BufferName;

            public ShaderBuilder Builder = new ShaderBuilder();

            internal UniformBufferObject(UniformDataSource dataSource, string bufferName)
            {
                DataSource = dataSource;
                BufferName = bufferName;
            }

            public void AddDeclaration(ShaderBuilder outBuilder)
            {
                if (DataSource != UniformDataSource.Global)
                {
                    outBuilder.AddLine($"CBUFFER_START({BufferName})");
                    outBuilder.Add(Builder.ToString());
                    outBuilder.AddLine("CBUFFER_END");
                }
                else
                    outBuilder.AddLine(Builder.ToString());
            }
        }

        Dictionary<string, UniformBufferObject> namedBufferMap = new Dictionary<string, UniformBufferObject>();
        List<UniformBufferObject> buffers = new List<UniformBufferObject>();

        public IEnumerable<UniformBufferObject> Buffers => buffers;

        public UniformBufferObject FindOrCreateBuffer(UniformDataSource dataSource, string customBufferName)
        {
            string bufferName = GetBufferName(ref dataSource, customBufferName);

            // If the buffer doesn't exist, create it
            if (!namedBufferMap.TryGetValue(bufferName, out var buffer))
            {
                buffer = new UniformBufferObject(dataSource, bufferName);
                namedBufferMap.Add(bufferName, buffer);
                buffers.Add(buffer);
            }
            return buffer;
        }

        string GetBufferName(ref UniformDataSource dataSource, string customBufferName)
        {
            switch (dataSource)
            {
                case UniformDataSource.None:
                    return string.Empty;
                case UniformDataSource.Global:
                    return string.Empty;
                case UniformDataSource.PerMaterial:
                    return "UnityPerMaterial";
                case UniformDataSource.PerInstance:
                    return "UnityPerMaterial";
                case UniformDataSource.Custom:
                {
                    // If the data source is a custom buffer but no name was provided,
                    // report an error and fallback to the global buffer. The callers
                    // should have always validated in advance, but to prevent having
                    // to check for nulls on the outside a bunch this will fallback to something reasonable.
                    if (string.IsNullOrEmpty(customBufferName))
                    {
                        Debug.LogError($"Data source {UniformDataSource.Custom} cannot have an empty buffer name.");
                        dataSource = UniformDataSource.Global;
                        return string.Empty;
                    }
                    return customBufferName;
                }
            }
            return string.Empty;
        }

        public void AddDeclarations(ShaderBuilder outBuilder)
        {
            foreach (var buffer in Buffers)
            {
                buffer.AddDeclaration(outBuilder);
                outBuilder.NewLine();
            }
        }
    }
}
