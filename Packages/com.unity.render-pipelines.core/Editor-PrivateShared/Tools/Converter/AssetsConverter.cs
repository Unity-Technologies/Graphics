using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
                    contextSearchQueriesAndIds,
                    (item, description) =>
                    {
                        var unityObject = item.ToObject();

                        if (unityObject == null)
                            return;

                        GameObject go = null;

                        if (unityObject is GameObject gameObject)
                            go = gameObject;
                        else if (unityObject is Component component)
                            go = component.gameObject;
                        else
                            return;

                        var gid = GlobalObjectId.GetGlobalObjectIdSlow(go);
                        var gidString = gid.ToString();

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
                            int type = gid.identifierType;

                            assetItem = new RenderPipelineConverterAssetItem(gidString)
                            {
                                name = $"{unityObject.name} ({(type == GlobalObjectIdentifierType.ImportedAsset ? "Prefab" : "SceneObject")})",
                                info = type == GlobalObjectIdentifierType.ImportedAsset ? AssetDatabase.GetAssetPath(unityObject) : go.scene.path,
                            };

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

        public virtual void BeforeConvert() { }

        protected abstract Status ConvertObject(UnityEngine.Object obj, StringBuilder message);

        public Status Convert(IRenderPipelineConverterItem item, out string message)
        {
            var assetItem = item as RenderPipelineConverterAssetItem;

            if (assetItem == null)
            {
                message = "Item is not a valid asset for conversion.";
                return Status.Error;
            }

            var obj = assetItem.LoadObject();

            if (obj == null)
            {
                message = $"Failed to load {assetItem.name} Global ID {assetItem.GlobalObjectId} Asset Path {assetItem.assetPath}";
                return Status.Error;
            }

            var errorString = new StringBuilder();

            var status = ConvertObject(obj, errorString);
            message = errorString.ToString();
            return status;
        }

        public virtual void AfterConvert() { }
    }
}
