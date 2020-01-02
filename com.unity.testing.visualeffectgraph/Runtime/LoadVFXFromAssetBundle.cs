using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    public class LoadVFXFromAssetBundle : MonoBehaviour
    {
        void Start()
        {
            var basePath = "C:/build/output/Unity-Technologies/ScriptableRenderPipeline/TestProjects/VisualEffectGraph_LWRP/test-results/";
            var path = System.IO.Path.Combine(basePath, "VFX_Bundle_Test");
            var assetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(path, "vfx_in_assetbundle"));

            var asset = assetBundle.LoadAsset("Packages/com.unity.testing.visualeffectgraph/AssetBundle/VFX_In_AssetBundle.prefab") as GameObject;
            var vfx = Instantiate(asset).GetComponent<VisualEffect>();
            if (vfx == null)
            {
                Debug.LogError("Unable to load VFX_In_AssetBundle");
            }
        }

        void Update()
        {

        }
    }
}
