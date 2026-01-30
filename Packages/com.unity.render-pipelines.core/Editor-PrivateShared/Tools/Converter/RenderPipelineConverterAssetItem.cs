using System;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor.Rendering.Converter
{
    [Serializable]
    internal class RenderPipelineConverterAssetItem : IRenderPipelineConverterItem
    {
        [SerializeField]
        private string m_AssetPath;

        public string assetPath
        {
            get => m_AssetPath;
            protected set => m_AssetPath = value;
        }

        [SerializeField]
        private string m_GUID;

        public string guid
        {
            get => m_GUID;
            protected set => m_GUID = value;
        }

        [SerializeField]
        private string m_GlobalObjectId;

        public string GlobalObjectId => m_GlobalObjectId;

        public string name => System.IO.Path.GetFileNameWithoutExtension(assetPath);

        [SerializeField]
        private string m_Info;

        public string info
        {
            get => m_Info;
            set => m_Info = value;
        }

        public bool isEnabled { get; set; } = true;
        public string isDisabledMessage { get; set; } = string.Empty;

        public Texture2D icon
        {
            get
            {
                if (!string.IsNullOrEmpty(assetPath))
                {
                    // Get the cached icon without loading the asset
                    var icon = AssetDatabase.GetCachedIcon(assetPath);
                    if (icon != null)
                        return icon as Texture2D;
                }

                return null;
            }
        }

        public RenderPipelineConverterAssetItem(string id)
        {
            if (!UnityEditor.GlobalObjectId.TryParse(id, out var gid))
                throw new ArgumentException(nameof(id), $"Unable to perform GlobalObjectId.TryParse with the given id {id}");

            m_AssetPath = AssetDatabase.GUIDToAssetPath(gid.assetGUID);
            m_GlobalObjectId = gid.ToString();
            m_GUID = gid.assetGUID.ToString();
        }

        public RenderPipelineConverterAssetItem(GlobalObjectId gid, string assetPath)
        {
            if (!AssetDatabase.AssetPathExists(assetPath))
                throw new ArgumentException(nameof(assetPath), $"{assetPath} does not exist");

            m_AssetPath = assetPath;
            m_GlobalObjectId = gid.ToString();
            m_GUID = AssetDatabase.AssetPathToGUID(assetPath);
        }

        public UnityEngine.Object LoadObject()
        {
            UnityEngine.Object obj = null;

            if (UnityEditor.GlobalObjectId.TryParse(GlobalObjectId, out var globalId))
            {
                // Try loading the object
                // TODO: Upcoming changes to GlobalObjectIdentifierToObjectSlow will allow it
                //       to return direct references to prefabs and their children.
                //       Once that change happens there are several items which should be adjusted.
                obj = UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);

                // If the object was not loaded, it is probably part of an unopened scene or prefab;
                // if so, then the solution is to first load the scene here.
                var objIsInSceneOrPrefab = globalId.identifierType == 2; // 2 is IdentifierType.kSceneObject
                if (!obj &&
                    objIsInSceneOrPrefab)
                {
                    // Open the Containing Scene Asset or Prefab in the Hierarchy so the Object can be manipulated
                    var mainAssetPath = AssetDatabase.GUIDToAssetPath(globalId.assetGUID);
                    var mainAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(mainAssetPath);
                    AssetDatabase.OpenAsset(mainAsset);

                    // If a prefab stage was opened, then mainAsset is the root of the
                    // prefab that contains the target object, so reference that for now,
                    // until GlobalObjectIdentifierToObjectSlow is updated
                    if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                    {
                        obj = mainAsset;
                    }

                    // Reload object if it is still null (because it's in a previously unopened scene)
                    if (!obj)
                    {
                        obj = UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                    }
                }
            }

            return obj;
        }

        public void OnClicked()
        {
            var obj = LoadObject();
            if (obj != null)
                EditorGUIUtility.PingObject(obj);
        }
    }
}
