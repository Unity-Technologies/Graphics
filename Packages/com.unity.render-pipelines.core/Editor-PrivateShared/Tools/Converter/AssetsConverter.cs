using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering.Converter
{
    [Serializable]
    internal abstract class AssetsConverter : IRenderPipelineConverter
    {
        protected abstract List<(string query, string description)> contextSearchQueriesAndIds { get; }
        public abstract bool isEnabled { get; }
        public abstract string isDisabledMessage { get; }

        internal List<RenderPipelineConverterAssetItem> assets = new();

        public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
        {
            assets.Clear();

            var searchQueries = contextSearchQueriesAndIds;
            if (searchQueries == null || searchQueries.Count == 0)
            {
                onScanFinish?.Invoke(new List<IRenderPipelineConverterItem>());
                return;
            }

            var pooledDict = UnityEngine.Pool.DictionaryPool<string, List<RenderPipelineConverterAssetItem>>.Get(out var assetsByDescription);
            bool dictionaryReleased = false;

            try
            {
                void OnSearchFinish()
                {
                    try
                    {
                        var organizedList = CategorizeResults(assets, assetsByDescription);
                        onScanFinish?.Invoke(organizedList);
                    }
                    finally
                    {
                        if (!dictionaryReleased)
                        {
                            UnityEngine.Pool.DictionaryPool<string, List<RenderPipelineConverterAssetItem>>.Release(assetsByDescription);
                            dictionaryReleased = true;
                        }
                    }
                }

                var processedIds = new HashSet<string>();
                var perGroupProcessedIds = new Dictionary<string, HashSet<string>>();
                var assetLookup = new Dictionary<string, RenderPipelineConverterAssetItem>();

                SearchServiceUtils.RunQueuedSearch
                (
                    SearchServiceUtils.IndexingOptions.DeepSearch,
                    searchQueries,
                    (item, description) =>
                    {
                        var newAssetItem = CreateAssetItem(item);
                        if (newAssetItem == null)
                            return;

                        var gidString = newAssetItem.GlobalObjectId;

                        string groupName = GetGroupNameFromDescription(description);

                        // Check if this group has already processed this object
                        if (!perGroupProcessedIds.ContainsKey(groupName))
                            perGroupProcessedIds[groupName] = new HashSet<string>();

                        if (!perGroupProcessedIds[groupName].Add(gidString))
                            return;

                        RenderPipelineConverterAssetItem assetItem;

                        // Add to the global assets list only once
                        bool isNewAsset = processedIds.Add(gidString);
                        if (isNewAsset)
                        {
                            assetItem = newAssetItem;
                            assets.Add(assetItem);
                            assetLookup[gidString] = assetItem;
                        }
                        else
                        {
                            assetItem = assetLookup[gidString];
                        }

                        // Add the asset item to the group
                        if (!assetsByDescription.ContainsKey(groupName))
                            assetsByDescription[groupName] = new List<RenderPipelineConverterAssetItem>();

                        assetsByDescription[groupName].Add(assetItem);
                    },
                    OnSearchFinish
                );
            }
            catch
            {
                if (!dictionaryReleased)
                {
                    UnityEngine.Pool.DictionaryPool<string, List<RenderPipelineConverterAssetItem>>.Release(assetsByDescription);
                    dictionaryReleased = true;
                }
                throw;
            }
        }

        internal static RenderPipelineConverterAssetItem CreateAssetItem(SearchItem item)
        {
            var unityObject = item.ToObject();

            if (unityObject == null)
                return CreateUnloadedSceneObjectItem(item);

            GameObject go = null;

            if (unityObject is GameObject gameObject)
                go = gameObject;
            else if (unityObject is Component component)
                go = component.gameObject;
            else
                return null;

            var gid = GlobalObjectId.GetGlobalObjectIdSlow(go);

            if (gid.identifierType == GlobalObjectIdentifierType.ImportedAsset)
            {
                return new RenderPipelineConverterAssetItem(gid.ToString())
                {
                    name = $"{unityObject.name} (Prefab)",
                    info = AssetDatabase.GetAssetPath(unityObject),
                };
            }

            return new RenderPipelineConverterAssetItem(gid.ToString())
            {
                name = GetSceneObjectName(UnityEditor.Search.SearchUtils.GetTransformPath(go.transform)),
                info = go.scene.path,
            };
        }

        // Named with the path in the hierarchy, the only name Search has for objects in closed scenes. GameObject
        // names can contain '/', so the object's own name can't be taken from it.
        internal static string GetSceneObjectName(string transformPath)
        {
            var path = transformPath.StartsWith('/') ? transformPath.Substring(1) : transformPath;
            return $"{path} (SceneObject)";
        }

        // Returns null unless the id is of an object in a scene that is not loaded
        internal static string GetUnloadedScenePath(string globalObjectId)
        {
            if (!GlobalObjectId.TryParse(globalObjectId, out var gid) || gid.identifierType != GlobalObjectIdentifierType.SceneObject)
                return null;

            var scenePath = AssetDatabase.GUIDToAssetPath(gid.assetGUID);
            if (string.IsNullOrEmpty(scenePath) || SceneManager.GetSceneByPath(scenePath).isLoaded)
                return null;

            return scenePath;
        }

        // A scene object only loads while its scene is open, so build the item from the search result and let
        // LoadObject open the scene at Convert time
        static RenderPipelineConverterAssetItem CreateUnloadedSceneObjectItem(SearchItem item)
        {
            var scenePath = GetUnloadedScenePath(item.id);
            if (scenePath == null)
                return null;

            // Search labels scene objects with their path in the hierarchy
            return new RenderPipelineConverterAssetItem(item.id)
            {
                name = GetSceneObjectName(item.GetLabel(item.context, true) ?? string.Empty),
                info = scenePath,
            };
        }

        /// <summary>
        /// Categorizes scan results into a tree structure.
        /// Override this method to customize how items are organized.
        /// Default implementation returns a flat list.
        /// </summary>
        /// <param name="assets">All assets found during scan</param>
        /// <param name="assetsByDescription">Assets grouped by their search description</param>
        /// <returns>Organized list of converter items</returns>
        protected virtual List<IRenderPipelineConverterItem> CategorizeResults(
            List<RenderPipelineConverterAssetItem> assets,
            Dictionary<string, List<RenderPipelineConverterAssetItem>> assetsByDescription)
        {
            if (assets == null || assets.Count == 0)
                return new List<IRenderPipelineConverterItem>();

            // Default: Return flat list
            var flatList = new List<IRenderPipelineConverterItem>(assets.Count);
            foreach (var asset in assets)
                flatList.Add(asset);

            return flatList;
        }

        /// <summary>
        /// Extracts the group name from the search description.
        /// Override this to customize how group names are extracted.
        /// Default implementation removes " is being referenced" suffix.
        /// Note: This string is coupled to the format used in ReadonlyMaterialConverter.GetMaterialSearchList()
        /// </summary>
        protected virtual string GetGroupNameFromDescription(string description)
        {
            return description.Replace(" is being referenced", "");
        }

        public virtual void BeforeConvert()
        {
            m_ScenesOpenedForConversion?.Clear();
        }

        protected abstract Status ConvertObject(UnityEngine.Object obj, StringBuilder message);

        public Status Convert(IRenderPipelineConverterItem item, out string message)
        {
            var assetItem = item as RenderPipelineConverterAssetItem;

            if (assetItem == null)
            {
                message = "Item is not a valid asset for conversion.";
                return Status.Error;
            }

            var scenePath = GetUnloadedScenePath(assetItem.GlobalObjectId);
            if (scenePath != null)
                (m_ScenesOpenedForConversion ??= new HashSet<string>()).Add(scenePath);

            var obj = assetItem.LoadObject();

            if (obj == null)
            {
                message = $"Failed to load {assetItem.name} Global ID {assetItem.GlobalObjectId} Asset Path {assetItem.assetPath}";
                return Status.Error;
            }

            var errorString = new StringBuilder();

            var status = ConvertObject(obj, errorString);
            SaveSceneOpenedForConversion(obj);
            message = errorString.ToString();
            return status;
        }

        // Scenes opened to convert their objects are closed again without saving when the next one opens, or when
        // the conversion returns to the scene it started from. Only tracked for the current conversion, so that a
        // later one leaves saving them to the user.
        [NonSerialized]
        HashSet<string> m_ScenesOpenedForConversion;

        void SaveSceneOpenedForConversion(UnityEngine.Object obj)
        {
            var scene = obj switch
            {
                GameObject go => go.scene,
                Component component => component.gameObject.scene,
                _ => default
            };

            if (scene.IsValid() && scene.isDirty && m_ScenesOpenedForConversion != null && m_ScenesOpenedForConversion.Contains(scene.path))
                EditorSceneManager.SaveScene(scene);
        }

        public virtual void AfterConvert()
        {
            m_ScenesOpenedForConversion?.Clear();
        }
    }
}
