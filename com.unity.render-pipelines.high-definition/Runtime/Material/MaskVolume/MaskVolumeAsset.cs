using System;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.HighDefinition.VolumeGlobalUniqueIDUtils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    [PreferBinarySerialization]
    internal class MaskVolumeAsset : ScriptableObject
    {
        [Serializable]
        internal enum AssetVersion
        {
            First,
            // Add new version here and they will automatically be the Current one
            Max,
            Current = Max - 1
        }

        [SerializeField] protected internal int m_Version = (int)AssetVersion.Current;
        internal int Version => m_Version;

        [SerializeField] internal MaskVolumePayload payload = MaskVolumePayload.zero;

        [SerializeField] internal int resolutionX;
        [SerializeField] internal int resolutionY;
        [SerializeField] internal int resolutionZ;

        [SerializeField] internal VolumeGlobalUniqueID globalUniqueID;

        internal bool IsDataAssigned()
        {
            return !MaskVolumePayload.IsEmpty(ref payload);
        }

        internal VolumeGlobalUniqueID GetID()
        {
            return (globalUniqueID == VolumeGlobalUniqueID.zero) ? new VolumeGlobalUniqueID(0, 0, 0, (ulong)unchecked((uint)GetInstanceID()), 0) : globalUniqueID;
        }

#if UNITY_EDITOR
        // Debug only: Uncomment out if you want to manually create a mask volume asset and type in data into the inspector.
        // This is not a user facing workflow we are supporting.
        // [UnityEditor.MenuItem("Assets/Create/Experimental/Mask Volume", false, 204)]
        // protected static void CreateAssetFromMenu()
        // {
        //     CreateAsset();
        // }

        internal static string GetFileName(VolumeGlobalUniqueID globalUniqueID)
        {
            string assetName = "MaskVolumeData";

            String assetFileName;
            String assetPath;

            String scenePath = SceneManagement.SceneManager.GetActiveScene().path;
            String sceneDir = System.IO.Path.GetDirectoryName(scenePath);
            String sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            assetPath = System.IO.Path.Combine(sceneDir, sceneName);

            if (!UnityEditor.AssetDatabase.IsValidFolder(assetPath))
                UnityEditor.AssetDatabase.CreateFolder(sceneDir, sceneName);

            assetFileName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(string.Format("{0}-{1}{2}", assetName, globalUniqueID.ToString(), ".asset"));

            assetFileName = System.IO.Path.Combine(assetPath, assetFileName);

            return assetFileName;
        }

        internal static MaskVolumeAsset CreateAsset(VolumeGlobalUniqueID globalUniqueID)
        {
            MaskVolumeAsset asset = CreateInstance<MaskVolumeAsset>();
            asset.globalUniqueID = globalUniqueID;
            EditorUtility.SetDirty(asset);

            string assetFileName = GetFileName(globalUniqueID);

            UnityEditor.AssetDatabase.CreateAsset(asset, assetFileName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return asset;
        }

        [ContextMenu("Reserialize All")]
        void ReserializeAll()
        {
            const string k_ProgressBarTitle = "Reserializing all Mask Volume assets";
            EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Searching assets", 1f / 6f);
            var assetGuids = AssetDatabase.FindAssets("t:" + nameof(MaskVolumeAsset));
            EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Loading assets", 3f / 6f);
            for (int i = 0; i < assetGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<MaskVolumeAsset>(path);
                EditorUtility.SetDirty(asset);
            }
            EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Saving assets", 5f / 6f);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }
#endif
    }
}
