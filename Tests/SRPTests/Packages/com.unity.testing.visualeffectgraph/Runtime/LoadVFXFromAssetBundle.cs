using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Testing.VisualEffectGraph
{
    public static class AssetBundleHelper
    {
        public static readonly string kAssetBundleRoot = "VFX_Bundle_Root";

        private static List<AssetBundle> s_AssetBundles = new List<AssetBundle>();

        public static AssetBundle Load(string name)
        {
            var basePath = System.IO.Path.Combine(Application.streamingAssetsPath, kAssetBundleRoot);
            var fullPath = System.IO.Path.Combine(basePath, name);
            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogError("Unable to find bundle at : " + fullPath);
                return null;
            }

            foreach (var bundle in s_AssetBundles)
            {
                if (bundle.name == name)
                {
                    return bundle;
                }
            }
            var assetBundle = AssetBundle.LoadFromFile(fullPath);
            if (assetBundle == null)
            {
                Debug.LogError("Unable to load bundle at : " + fullPath);
            }

            s_AssetBundles.Add(assetBundle);
            return assetBundle;
        }

        public static void Unload(AssetBundle bundle)
        {
            var index = s_AssetBundles.FindIndex(o => o == bundle);
            if (index == -1)
            {
                Debug.LogError("Unable to unload bundle : " + bundle);
                return;
            }

            bundle.Unload(true);
            s_AssetBundles.RemoveAt(index);
        }
    }

    public class LoadVFXFromAssetBundle : MonoBehaviour
    {
        private GameObject m_VFX;
        private AssetBundle m_AssetBundle;

        void Start()
        {
            m_AssetBundle = AssetBundleHelper.Load("vfx_in_assetbundle");

            var asset = m_AssetBundle.LoadAsset("Packages/com.unity.testing.visualeffectgraph/AssetBundle/VFX_In_AssetBundle.prefab") as GameObject;
            m_VFX = Instantiate(asset);
            if (m_VFX == null || m_VFX.GetComponent<VisualEffect>() == null)
            {
                Debug.LogError("Unable to load VFX_In_AssetBundle");
            }
        }

        void OnDisable()
        {
            if (m_VFX)
                Destroy(m_VFX);

            if (m_AssetBundle)
                AssetBundleHelper.Unload(m_AssetBundle);
        }
    }
}
