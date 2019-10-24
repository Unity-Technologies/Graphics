using System;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeVolumeAsset : ScriptableObject
    {
        public enum AssetVersion
        {
            First,
            // Add new version here and they will automatically be the Current one
            Max,
            Current = Max - 1
        }

        protected int m_Version = (int)AssetVersion.First;
        public int Version { get => m_Version; }

        public SphericalHarmonicsL1[] data = null;

        public int resolutionX;
        public int resolutionY;
        public int resolutionZ;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/ProbeVolume", false, 204)]
        protected static void CreateAssetFromMenu()
        {
            CreateAsset();
        }

        public static string GetFileName(int id = -1)
        {
            string assetName = "ProbeVolumeData";

            String assetFileName;
            String assetPath;

            if (id == -1)
            {
                assetPath = "Assets";
                assetFileName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(assetName + ".asset");
            }
            else
            {
                String scenePath = SceneManagement.SceneManager.GetActiveScene().path;
                assetPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(scenePath), System.IO.Path.GetFileNameWithoutExtension(scenePath));
                assetFileName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(assetName + id + ".asset");
            }

            assetFileName = System.IO.Path.Combine(assetPath, assetFileName);

            return assetFileName;
        }

        public static ProbeVolumeAsset CreateAsset(int id = -1)
        {
            ProbeVolumeAsset asset = ScriptableObject.CreateInstance<ProbeVolumeAsset>();
            string assetFileName = GetFileName(id);

            UnityEditor.AssetDatabase.CreateAsset(asset, assetFileName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return asset;
        }
#endif
    }
}
