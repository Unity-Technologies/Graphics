using System;
using System.IO;
using UnityEngine.Serialization;
using Unity.IO.LowLevel.Unsafe;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    // A StreamableAsset is an asset that is converted to a Streaming Asset for builds.
    // assetGUID is used in editor to handle the asset and streamableAssetPath is updated at build time and is used at runtime.
    [Serializable]
    [Scripting.APIUpdating.MovedFrom(false, "UnityEngine.Rendering", "Unity.RenderPipelines.Core.Runtime", "ProbeVolumeBakingSet.StreamableAsset")]
    class ProbeVolumeStreamableAsset
    {
        [Serializable]
        [Scripting.APIUpdating.MovedFrom(false, "UnityEngine.Rendering", "Unity.RenderPipelines.Core.Runtime", "ProbeVolumeBakingSet.StreamableAsset.StreamableCellDesc")]
        public struct StreamableCellDesc
        {
            public int offset; // Offset of the cell within the file.
            public int elementCount; // Number of elements in the cell (can be data chunks, bricks, debug info, etc)
        }

        [SerializeField] [FormerlySerializedAs("assetGUID")] string m_AssetGUID = ""; // In the editor, allows us to load the asset through the AssetDatabase.
        [SerializeField] [FormerlySerializedAs("streamableAssetPath")]string m_StreamableAssetPath = ""; // At runtime, path of the asset within the StreamingAssets data folder.
        [SerializeField] [FormerlySerializedAs("elementSize")]int m_ElementSize; // Size of an element. Can be a data chunk, a brick, etc.
        [SerializeField] [FormerlySerializedAs("streamableCellDescs")] SerializedDictionary<int, StreamableCellDesc> m_StreamableCellDescs = new SerializedDictionary<int, StreamableCellDesc>();
        [SerializeField] TextAsset m_Asset;

        public string assetGUID { get => m_AssetGUID; }
        public TextAsset asset { get => m_Asset; }
        public int elementSize { get => m_ElementSize; }
        public SerializedDictionary<int, StreamableCellDesc> streamableCellDescs { get => m_StreamableCellDescs; }

        string m_FinalAssetPath;

        FileHandle m_AssetFileHandle;

        public ProbeVolumeStreamableAsset(string apvStreamingAssetsPath, SerializedDictionary<int, StreamableCellDesc> cellDescs, int elementSize, string bakingSetGUID, string assetGUID)
        {
            m_AssetGUID = assetGUID;
            m_StreamableCellDescs = cellDescs;
            m_ElementSize = elementSize;
            m_StreamableAssetPath = Path.Combine(Path.Combine(apvStreamingAssetsPath, bakingSetGUID), m_AssetGUID + ".bytes");
#if UNITY_EDITOR
            EnsureAssetLoaded();
#endif
        }

        internal void RefreshAssetPath()
        {
#if UNITY_EDITOR
            m_FinalAssetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
#else
            m_FinalAssetPath = Path.Combine(Application.streamingAssetsPath, m_StreamableAssetPath);
#endif
        }

        public string GetAssetPath()
        {
            // Avoid GCAlloc every frame this is called.
            if (string.IsNullOrEmpty(m_FinalAssetPath))
                RefreshAssetPath();

            return m_FinalAssetPath;
        }

        internal bool HasValidAssetReference()
        {
            return m_Asset != null && m_Asset.bytes != null;
        }

        unsafe public bool FileExists()
        {
#if UNITY_EDITOR
            if (HasValidAssetReference())
                return true;
            if (File.Exists(GetAssetPath()))
                return true;
            // File may not exist if it was moved, refresh path in this case
            RefreshAssetPath();
            return File.Exists(GetAssetPath());
#else
            // When not using streaming assets, this reference should always be valid.
            if (m_Asset != null)
                return true;

            FileInfoResult result;
            AsyncReadManager.GetFileInfo(GetAssetPath(), &result).JobHandle.Complete();
            return result.FileState == FileState.Exists;
#endif
        }

#if UNITY_EDITOR
        public void RenameAsset(string newName)
        {
            AssetDatabase.RenameAsset(AssetDatabase.GUIDToAssetPath(m_AssetGUID), newName);
            m_FinalAssetPath = "";
        }

        // Ensures that the asset is referenced via Unity's serialization layer.
        public void EnsureAssetLoaded()
        {
            m_Asset = AssetDatabase.LoadAssetAtPath<TextAsset>(GetAssetPath());
        }

        // Temporarily clear the asset reference. Used to prevent serialization of the asset when we are using the StreamingAssets codepath.
        public void ClearAssetReferenceForBuild()
        {
            m_Asset = null;
        }
#endif

        public long GetFileSize()
        {
            return new FileInfo(GetAssetPath()).Length;
        }

        public bool IsOpen()
        {
            return m_AssetFileHandle.IsValid();
        }

        public FileHandle OpenFile()
        {
            if (m_AssetFileHandle.IsValid())
                return m_AssetFileHandle;

            m_AssetFileHandle = AsyncReadManager.OpenFileAsync(GetAssetPath());
            return m_AssetFileHandle;
        }

        public void CloseFile()
        {
            if (m_AssetFileHandle.IsValid() && m_AssetFileHandle.JobHandle.IsCompleted)
                m_AssetFileHandle.Close();

            m_AssetFileHandle = default(FileHandle);
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(m_AssetGUID);
        }

        public void Dispose()
        {
            if (m_AssetFileHandle.IsValid())
            {
                m_AssetFileHandle.Close().Complete();
                m_AssetFileHandle = default(FileHandle);
            }
        }
    }

}
