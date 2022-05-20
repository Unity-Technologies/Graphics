using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// ShaderGraphContext manages and exposes the Context stored in the
    /// ShaderGraphRegisrty.
    /// </summary>
    public class ShaderGraphContext : IContextDescriptor
    {
        public IEnumerable<IContextDescriptor.ContextEntry> GetEntries()
        {
            return new List<IContextDescriptor.ContextEntry>()
                {
                    new ()
                    {
                        fieldName = "BaseColor",
                        primitive = GraphType.Primitive.Float,
                        precision = GraphType.Precision.Single,
                        height = GraphType.Height.One,
                        length = GraphType.Length.Three,
                    },
                    new ()
                    {
                        fieldName = "NormalTS",
                        primitive = GraphType.Primitive.Float,
                        precision = GraphType.Precision.Single,
                        height = GraphType.Height.One,
                        length = GraphType.Length.Three,
                        // Expected default Tangent Space Normal is (0, 0, 1)
                        initialValue = new Matrix4x4(
                                            new Vector4(0, 0, 1, 0),
                                            Vector4.zero,
                                            Vector4.zero,
                                            Vector4.zero)
                    },
                    new ()
                    {
                        fieldName = "Emission",
                        primitive = GraphType.Primitive.Float,
                        precision = GraphType.Precision.Single,
                        height = GraphType.Height.One,
                        length = GraphType.Length.Three,
                    },
                };
        }

        public RegistryFlags GetRegistryFlags()
        {
            return RegistryFlags.Base;
        }

        public RegistryKey GetRegistryKey()
        {
            // Defines the name of the context node on the graph
            return new RegistryKey() { Name = "DefaultContextDescriptor", Version = 1 };
        }
    }
}
