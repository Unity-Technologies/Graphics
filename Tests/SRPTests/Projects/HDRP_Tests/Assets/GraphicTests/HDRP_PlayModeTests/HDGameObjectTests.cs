using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using System.Collections;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;

public class HDGameObjectTests
{
    [UnityTest]
    public IEnumerator ReflectionProbeCustomTextureRelease()
    {
#if UNITY_EDITOR
        var customTextureAsset = AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.unity.testing.hdrp/Textures/CubeMap_01.exr");

        var go = new GameObject();
        {
            var probe = go.AddComponent<PlanarReflectionProbe>();
            probe.mode = ProbeSettings.Mode.Custom;
            probe.customTexture = customTextureAsset;
            probe.mode = ProbeSettings.Mode.Baked;

            // Check the switching to custom and baked to baked results in a released asset reference.
            Assert.IsNull(probe.customTexture);

            probe.mode = ProbeSettings.Mode.Custom;

            // And also check that switching back to custom will restore the reference if the asset exists.
            Assert.IsTrue(probe.customTexture == customTextureAsset);
        }
        CoreUtils.Destroy(go);
#endif

        yield break;
    }
}
