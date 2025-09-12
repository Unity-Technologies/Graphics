using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Rendering.Converter;

namespace UnityEditor.Rendering.Universal
{
    internal abstract class RenderPipelineAssetsConverter : RenderPipelineConverter
    {
        protected abstract List<(string query, string description)> contextSearchQueriesAndIds { get; }

        protected abstract Status ConvertObject(UnityEngine.Object obj, StringBuilder message);

        internal List<RenderPipelineConverterAssetItem> assets = new();

        public override void OnInitialize(InitializeConverterContext ctx, Action callback)
        {
            SearchServiceUtils.RunQueuedSearch
            (
                SearchServiceUtils.IndexingOptions.DeepSearch,
                contextSearchQueriesAndIds,
                (item, description) =>
                {
                    var assetItem = new RenderPipelineConverterAssetItem(item.id);
                    assets.Add(assetItem);

                    var itemDescriptor = new ConverterItemDescriptor()
                    {
                        name = assetItem.assetPath,
                        info = description,
                    };

                    ctx.AddAssetToConvert(itemDescriptor);
                },
                callback
            );
        }

        public override void OnRun(ref RunItemContext ctx)
        {
            var errorString = new StringBuilder();
            var obj = LoadObject(ref ctx, errorString);
            if (obj == null)
            {
                ctx.didFail = true;
                ctx.info = errorString.ToString();
                return;
            }

            var status = ConvertObject(obj, errorString);
            if (status == Status.Error)
            {
                ctx.didFail = true;
                ctx.info = errorString.ToString();
                return;
            }
        }

        public override void OnClicked(int index)
        {
            assets[index].OnClicked();
        }

        private UnityEngine.Object LoadObject(ref RunItemContext ctx, StringBuilder sb)
        {
            var item = assets[ctx.item.index];
            var obj = item.LoadObject();

            if (obj == null)
                sb.AppendLine($"Failed to load {ctx.item.descriptor.info} Global ID {item.guid} Asset Path {item.assetPath}");

            return obj;
        }
    }
}
