using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    public class LoadVFXFromAssetBundle : MonoBehaviour
    {
        public static string s_AssetBundleName = "VFX_Bundle_Test";

        void Start()
        {
            var baseName = "vfx_in_assetbundle";
            var basePath = System.IO.Path.Combine(Application.streamingAssetsPath, s_AssetBundleName);
            var fullPath = System.IO.Path.Combine(basePath, baseName);
            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogError("Unable to find bundle at : " + fullPath);
            }

            AssetBundle assetBundle = null;
            AssetBundle[] bundles = Resources.FindObjectsOfTypeAll<AssetBundle>();
            foreach (var bundle in bundles)
            {
                if (bundle.name == baseName)
                {
                    //This asset bundle has been already loaded.
                    assetBundle = bundle;
                    break;
                }
            }

            if (assetBundle == null) //Not yet loaded
                assetBundle = AssetBundle.LoadFromFile(fullPath);

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
