using System;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeVolumeAsset : ScriptableObject
    {
        [Serializable]
        internal enum AssetVersion
        {
            First,
            AddProbeVolumesAtlasEncodingModes,
            PV2,
            Max,
            Current = Max - 1
        }

        [SerializeField] protected internal int m_Version = (int)AssetVersion.Current;
        [SerializeField] public int Version { get => m_Version; }
        [SerializeField] public SphericalHarmonicsL1[] probes;
        [SerializeField] public ProbeBrickIndex.Brick[] bricks;

#if UNITY_EDITOR
        // Debug only: Uncomment out if you want to manually create a probe volume asset and type in data into the inspector.
        // This is not a user facing workflow we are supporting.
        // [UnityEditor.MenuItem("Assets/Create/Experimental/Probe Volume", false, 204)]
        // protected static void CreateAssetFromMenu()
        // {
        //     CreateAsset();
        // }

        internal static string GetFileName(int id = -1)
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
                String sceneDir = System.IO.Path.GetDirectoryName(scenePath);
                String sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                assetPath = System.IO.Path.Combine(sceneDir, sceneName);

                if (!UnityEditor.AssetDatabase.IsValidFolder(assetPath))
                    UnityEditor.AssetDatabase.CreateFolder(sceneDir, sceneName);

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
