using System;
using System.Collections.Generic;
using System.Text;

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
            void OnSearchFinish()
            {
                var returnList = new List<IRenderPipelineConverterItem>(assets.Count);
                foreach (var asset in assets)
                    returnList.Add(asset);
                onScanFinish?.Invoke(returnList);
            }

            SearchServiceUtils.RunQueuedSearch
            (
                SearchServiceUtils.IndexingOptions.DeepSearch,
                contextSearchQueriesAndIds,
                (item, description) =>
                {
                    var assetItem = new RenderPipelineConverterAssetItem(item.id)
                    {
                        info = description
                    };
                    assets.Add(assetItem);
                },
                OnSearchFinish
            );
        }

        public virtual void BeforeConvert() { }

        protected abstract Status ConvertObject(UnityEngine.Object obj, StringBuilder message);

        public Status Convert(IRenderPipelineConverterItem item, out string message)
        {
            var assetItem = item as RenderPipelineConverterAssetItem;

            var obj = assetItem.LoadObject();

            if (obj == null)
            {
                message = $"Failed to load {assetItem.name} Global ID {assetItem.guid} Asset Path {assetItem.assetPath}";
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
