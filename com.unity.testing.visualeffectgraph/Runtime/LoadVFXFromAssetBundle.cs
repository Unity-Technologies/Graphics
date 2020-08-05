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
            var assetBundlePath = System.IO.File.ReadAllLines(Application.streamingAssetsPath + "/AssetBundlePath.txt");
            if (assetBundlePath.Length < 1)
            {
                Debug.LogError("Unable to find bundle AssetBundlePath.txt");
                return;
            }

            var basePath = assetBundlePath[0];
            var fullPath = System.IO.Path.Combine(basePath, "vfx_in_assetbundle");
            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogError("Unable to find bundle at : " + fullPath);
            }

            var assetBundle = AssetBundle.LoadFromFile(fullPath);
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
