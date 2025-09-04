using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.Rendering.Converter;

[assembly: InternalsVisibleTo("PPv2URPConverters")]
[assembly: InternalsVisibleTo("Unity.2D.PixelPerfect.Editor")]
namespace UnityEditor.Rendering.Universal
{
    // Might need to change this name before making it public
    internal abstract class RenderPipelineConverter : IRenderPipelineConverter
    {
        /// <summary>
        /// Name of the converter.
        /// </summary>
        public abstract string name { get; }

        /// <summary>
        /// The information when hovering over the converter.
        /// </summary>
        public abstract string info { get; }

        private bool m_Enabled = true;
        /// <summary>
        /// A check if the converter is enabled or not. Can be used to do a check if prerequisites are met to have it enabled or disabled.
        /// </summary>
        public virtual bool isEnabled { get => m_Enabled; set => m_Enabled = value; }
        public virtual string isDisabledMessage { get; set; }

        /// <summary>
        /// The message if the converter is disabled. This will be shown in the UI when hovering over the disabled converter.
        /// </summary>
        public virtual string isDisabledWarningMessage => string.Empty;

        /// <summary>
        /// A priority of the converter. The lower the number (can be negative), the earlier it will be executed. Can be used to make sure that a converter runs before another converter.
        /// </summary>
        public virtual int priority => 0;

        /// <summary>
        /// A check to see if the converter needs to create the index.
        /// This will only need to be set to true if the converter is using search api, and search queries.
        /// If set to true the converter framework will create the indexer and remove it after all search queries are done.
        /// </summary>
        public virtual bool needsIndexing => false;

        /// <summary>
        /// This method getting triggered when clicking the listview item in the UI.
        /// </summary>
        public virtual void OnClicked(int index)
        {
        }

        // This is so that we can have different segment in our UI, example Unity converters, your custom converters etc..
        // This is not implemented yet
        public virtual string category { get; }

        // This is in which drop down item the converter belongs to.
        // Not properly implemented yet
        public abstract Type container { get; }
        

        /// <summary>
        /// This runs when initializing the converter. To gather data for the UI and also for the converter if needed.
        /// </summary>
        /// <param name="context">The context that will be used to initialize data for the converter.</param>
        public abstract void OnInitialize(InitializeConverterContext context, Action callback);

        /// <summary>
        /// The method that will be run before Run method if needed.
        /// </summary>
        public virtual void OnPreRun()
        {
        }

        /// <summary>
        /// The method that will be run when converting the assets.
        /// </summary>
        /// <param name="context">The context that will be used when executing converter.</param>
        public abstract void OnRun(ref RunItemContext context);

        /// <summary>
        /// The method that will be run after the converters are done if needed.
        /// </summary>
        public virtual void OnPostRun()
        {
        }

        // Temporary solution until all converters are moved to new API
        class RenderPipelineConverterItem : IRenderPipelineConverterItem
        {
            public string name { get; set; }
            public string info { get; set; }
            public int index { get; set; }
            public ConverterItemDescriptor descriptor { get; set; }
            public Action onClicked { get; set; }

            public bool isEnabled { get; set; }
            public string isDisabledMessage { get; set; }

            public void OnClicked()
            {
                onClicked?.Invoke();
            }
        }

        public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
        {
            InitializeConverterContext ctx = new() { items = new() };

            void TranslateOldAPIToNewAPI()
            {
                // Temporary solution until all converters are moved to new API
                var listItems = new List<IRenderPipelineConverterItem>();
                foreach (var i in ctx.items)
                {
                    var item = new RenderPipelineConverterItem()
                    {
                        name = i.name,
                        info = i.info,
                        index = ctx.items.IndexOf(i),
                        descriptor = i,
                        isEnabled = string.IsNullOrEmpty(i.warningMessage),
                        isDisabledMessage = i.warningMessage,
                        onClicked = () => OnClicked(ctx.items.IndexOf(i))
                    };

                    listItems.Add(item);
                }
                onScanFinish?.Invoke(listItems);
            }

            OnInitialize(ctx, TranslateOldAPIToNewAPI);
        }

        public void BeforeConvert()
        {
            OnPreRun();
        }

        public Status Convert(IRenderPipelineConverterItem item, out string message)
        {
            // Temporary solution until all converters are moved to new API
            RenderPipelineConverterItem rpItem = item as RenderPipelineConverterItem;
            var itemToConvertInfo = new ConverterItemInfo()
            {
                index = rpItem.index,
                descriptor = rpItem.descriptor,
            };
            var ctx = new RunItemContext(itemToConvertInfo);
            OnRun(ref ctx);

            if (ctx.didFail)
            {
                message = ctx.info;
                return Status.Error;
            }

            message = string.Empty;
            return Status.Success;
        }

        public void AfterConvert()
        {
            OnPostRun();
        }
    }
}
