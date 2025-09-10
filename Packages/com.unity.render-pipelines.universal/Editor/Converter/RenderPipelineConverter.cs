using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering.Converter;

[assembly: InternalsVisibleTo("PPv2URPConverters")]
[assembly: InternalsVisibleTo("Unity.2D.PixelPerfect.Editor")]
namespace UnityEditor.Rendering.Universal
{
    internal interface IRenderPipelineConverter
    {
        bool isEnabled { get; }
    }

    [Serializable]
    internal class RenderPipelineConverterAssetItem
    {
        public string assetPath { get; }
        public string guid { get; }

        public RenderPipelineConverterAssetItem(string id)
        {
            if (!GlobalObjectId.TryParse(id, out var gid))
                throw new ArgumentException(nameof(id), $"Unable to perform GlobalObjectId.TryParse with the given id {id}");

            assetPath = AssetDatabase.GUIDToAssetPath(gid.assetGUID);
            guid = gid.ToString();
        }

        public RenderPipelineConverterAssetItem(GlobalObjectId gid, string assetPath)
        {
            if (!AssetDatabase.AssetPathExists(assetPath))
                throw new ArgumentException(nameof(assetPath), $"{assetPath} does not exist");

            this.assetPath = assetPath;
            guid = gid.ToString();
        }

        public UnityEngine.Object LoadObject()
        {
            UnityEngine.Object obj = null;

            if (GlobalObjectId.TryParse(guid, out var globalId))
            {
                // Try loading the object
                // TODO: Upcoming changes to GlobalObjectIdentifierToObjectSlow will allow it
                //       to return direct references to prefabs and their children.
                //       Once that change happens there are several items which should be adjusted.
                obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);

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
                        obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                    }
                }
            }

            return obj;
        }

        public void OnClicked()
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath));
        }
    }

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
