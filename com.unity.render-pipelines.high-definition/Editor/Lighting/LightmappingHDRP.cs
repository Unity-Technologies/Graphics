using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    public static class LightmappingHDRP
    {
        public static Exception BakeProbe(HDProbe probe, string path, int textureSize = -1)
        {
            // Check arguments
            if (probe == null || probe.Equals(null)) return new ArgumentNullException(nameof(probe));
            if (string.IsNullOrEmpty(path))
                return new ArgumentException($"{nameof(path)} must not be empty or null.");

            // We force RGBAHalf as we don't support 11-11-10 textures (only RT)
            var probeFormat = GraphicsFormat.R16G16B16A16_SFloat;
            switch (probe)
            {
                case HDAdditionalReflectionData reflectionProbe:
                {
                    // Get the texture size from the probe
                    if (textureSize == -1)
                        textureSize = (int) probe.resolution;

                    // Render and write
                    var cubeRT = HDRenderUtilities.CreateReflectionProbeRenderTarget(textureSize, probeFormat);
                    HDBakedReflectionSystem.RenderAndWriteToFile(probe, path, cubeRT, null);
                    cubeRT.Release();

                    // Import asset at target location
                    AssetDatabase.ImportAsset(path);
                    HDBakedReflectionSystem.ImportAssetAt(probe, path);

                    // Assign to the probe the baked texture
                    var bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                    probe.SetTexture(ProbeSettings.Mode.Baked, bakedTexture);

                    // Mark probe as dirty
                    EditorUtility.SetDirty(probe);

                    return null;
                }
                case PlanarReflectionProbe planarProbe:
                    return new Exception("Planar reflection probe baking is not supported.");
                default: return new Exception($"Cannot handle probe type: {probe.GetType()}");
            }
        }
    }
}
