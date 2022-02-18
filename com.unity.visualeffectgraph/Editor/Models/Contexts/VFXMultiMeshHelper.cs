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
        private const string indirectIndexName = "indirectIndex";
        public const string lodName = "lodValues";
        private const string bufferName = "indirectBuffer";
        private static readonly Vector4 lodFactors = new Vector4(8.0f, 4.0f, 2.0f, 0.1f);

        private static string GetId(uint meshCount, int index)
        {
            if (meshCount > 1)
                return index.ToString();
            return string.Empty;
        }

        public static IEnumerable<VFXPropertyWithValue> GetInputProperties(uint meshCount, VFXOutputUpdate.Features features)
        {
            for (int i = 0; i < meshCount; ++i)
            {
                string id = GetId(meshCount, i);

                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Mesh), meshName + id, new TooltipAttribute("Specifies the mesh" + id + " used to render the particle.")), VFXResources.defaultResources.mesh);
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), maskName + id, new TooltipAttribute("Defines a bitmask to control which submeshes are rendered for mesh" + id + "."), new BitFieldAttribute()), 0xffffffff);
            }

            if (VFXOutputUpdate.HasFeature(features, VFXOutputUpdate.Features.LOD))
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector4), lodName, new TooltipAttribute("Specifies the minimum screen ratio for a LOD mesh to be used (e.g. a value of 25 means the mesh has to occupy 25% of the screen on 1 dimension).")), lodFactors);

            if (VFXOutputUpdate.HasFeature(features, VFXOutputUpdate.Features.LOD)
                || VFXOutputUpdate.HasFeature(features, VFXOutputUpdate.Features.FrustumCulling))
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "radiusScale", new MinAttribute(0.0f), new TooltipAttribute("Specifies a scale to apply to the radius of the bounding sphere used for LOD and frustum culling. By default the bounding sphere is encompassing a mesh bounding box of side 1.")), 1.0f);
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
                else if (m.name == indirectIndexName)
                    yield return new VFXMapping(indirectIndexName, m.index + index);
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
