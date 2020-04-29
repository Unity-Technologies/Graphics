using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using System;

namespace UnityEditor.VFX
{
    interface IVFXMultiMeshOutput
    {
        uint meshCount { get; }
    }

    static class VFXMultiMeshHelper
    {
        private const string meshName = "mesh";
        private const string maskName = "subMeshMask";
        private const string lodName = "lodValue";
        private const string bufferName = "indirectBuffer";
        private static readonly float[] lodFactors = new float[4] { 0.1f, 4.0f, 8.0f, 12.0f };

        private static string GetId(uint meshCount, int index)
        {
            if (meshCount > 1)
                return index.ToString();
            return string.Empty;
        }

        public static IEnumerable<VFXPropertyWithValue> GetInputProperties(uint meshCount, bool lod)
        {
            for (int i = 0; i < meshCount; ++i)
            {
                string id = GetId(meshCount, i);

                if (lod)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), lodName + id, new TooltipAttribute("Specifies the screen ratio at which mesh" + id + " is used.")), lodFactors[meshCount - i - 1]);
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Mesh), meshName + id, new TooltipAttribute("Specifies the mesh" + id + " used to render the particle.")), VFXResources.defaultResources.mesh);
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), maskName + id, new TooltipAttribute("Defines a bitmask to control which submeshes are rendered for mesh" + id + "."), new BitFieldAttribute()), 0xffffffff);
            }
        }

        public static IEnumerable<string> GetLODExpressionNames(uint meshCount)
        {
            for (int i = 0; i < meshCount; ++i)
            {
                string id = GetId(meshCount, i);
                yield return lodName + id;
            }
        }

        public static IEnumerable<string> GetCPUExpressionNames(uint meshCount)
        {
            for (int i = 0; i < meshCount; ++i)
            {
                string id = GetId(meshCount, i);

                yield return meshName + id;
                yield return maskName + id;
            }
        }

        public static IEnumerable<VFXMapping> PatchCPUMapping(IEnumerable<VFXMapping> mappings, uint meshCount, int index)
        {
            string id = GetId(meshCount, index);
            foreach (var m in mappings)
            {
                if (m.name.StartsWith(meshName))
                {
                    if (m.name == meshName + id)
                        yield return new VFXMapping(meshName, m.index);
                }    
                else if (m.name.StartsWith(maskName))
                {
                    if (m.name == maskName + id)
                        yield return new VFXMapping(maskName, m.index);
                }      
                else
                    yield return m;
            }
        }

        public static IEnumerable<VFXMapping> PatchBufferMapping(IEnumerable<VFXMapping> mappings, int bufferIndex)
        {
            foreach (var m in mappings)
            {
                if (m.name == bufferName)
                    yield return new VFXMapping(bufferName, m.index + bufferIndex);
                else
                    yield return m;
            }
        }
    }
}
