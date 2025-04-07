using System;

using UnityEditor.Experimental.GraphView;

namespace UnityEditor.VFX
{
    /// <summary>
    /// This class provides all useful information for the GraphView template window
    /// It also allows to read or write template information from/to a VFX asset
    /// </summary>
    class VFXTemplateHelperInternal : ITemplateHelper
    {
        class SaveFileDialog : GraphViewTemplateWindow.ISaveFileDialogHelper
        {
            public string OpenSaveFileDialog()
            {
                return EditorUtility.SaveFilePanelInProject("", "New VFX", "vfx", "Create new VisualEffect Graph");
            }
        }

        public string packageInfoName => VisualEffectGraphPackageInfo.name;
        public string learningSampleName => "Learning Templates";
        public string templateWindowDocUrl =>Documentation.GetPageLink("Templates-window");
        public string builtInTemplatePath => VisualEffectAssetEditorUtility.templatePath;
        public string builtInCategory => "Default VFX Graph Templates";
        public string assetType => "VisualEffectAsset";
        public string emptyTemplateName => "Empty VFX";
        public string emptyTemplateDescription => "Create a completely empty VFX asset";
        public string lastSelectedGuidKey => "VFXTemplateWindow.LastSelectedGuid";
        public string createNewAssetTitle => "Create new VFX Asset";
        public string insertTemplateTitle => "Insert a template into current VFX Asset";
        public string emptyTemplateIconPath => $"{VisualEffectGraphPackageInfo.assetPackagePath}/Editor/Templates/UI/EmptyTemplate@256.png";
        public string emptyTemplateScreenshotPath => $"{VisualEffectGraphPackageInfo.assetPackagePath}/Editor/Templates/UI/3d_Empty.png";
        public string customTemplateIcon => $"{VisualEffectGraphPackageInfo.assetPackagePath}/Editor/Templates/UI/CustomVFXGraph@256.png";
        public GraphViewTemplateWindow.ISaveFileDialogHelper saveFileDialogHelper { get; set; } = new SaveFileDialog();


        /// <summary>
        /// This method is called each time a template is used.
        /// This is the good place to implement analytics
        /// </summary>
        /// <param name="usedTemplate">Template that has just been used</param>
        public void RaiseTemplateUsed(GraphViewTemplateDescriptor usedTemplate)
        {
            // For legal reason we should only monitor built-in templates usage
            if (string.Compare(usedTemplate.category, builtInCategory, StringComparison.OrdinalIgnoreCase) == 0)
            {
                VFXAnalytics.GetInstance().OnSystemTemplateCreated(usedTemplate.name);
            }
        }

        /// <summary>
        /// This method gets template information for any Visual Effect asset.
        /// </summary>
        /// <param name="vfxPath">The path to a Visual Effect asset.</param>
        /// <param name="graphViewTemplate">The structure that contains template information.</param>
        /// <returns>Returns true if the Visual Effect asset has template information, otherwise it returns false.</returns>
        public bool TryGetTemplate(string vfxPath, out GraphViewTemplateDescriptor graphViewTemplate) => TryGetTemplateStatic(vfxPath, out graphViewTemplate);

        /// <summary>
        /// This method creates or updates a Visual Effect asset template.
        /// </summary>
        /// <param name="vfxPath">The path to the existing Visual Effect asset.</param>
        /// <param name="graphViewTemplate">The structure that contains all template information.</param>
        /// <returns>Returns true if the template is created, otherwise it returns false.</returns>
        public bool TrySetTemplate(string vfxPath, GraphViewTemplateDescriptor graphViewTemplate) => TrySetTemplateStatic(vfxPath, graphViewTemplate);

        internal static bool TryGetTemplateStatic(string vfxPath, out GraphViewTemplateDescriptor graphViewTemplate)
        {
            var importer = (VisualEffectImporter)AssetImporter.GetAtPath(vfxPath);
            var nativeTemplate = importer.templateProperty;

            if (!string.IsNullOrEmpty(nativeTemplate.name))
            {
                graphViewTemplate = new GraphViewTemplateDescriptor
                {
                    name = nativeTemplate.name,
                    category = nativeTemplate.category,
                    description = nativeTemplate.description,
                    icon = nativeTemplate.icon,
                    thumbnail = nativeTemplate.thumbnail,
                };

                return true;
            }

            graphViewTemplate = default;
            return false;
        }

        internal static bool TrySetTemplateStatic(string vfxPath, GraphViewTemplateDescriptor graphViewTemplate)
        {
            if (string.IsNullOrEmpty(vfxPath))
                return false;

            if (AssetDatabase.AssetPathExists(vfxPath))
            {
                var importer = (VisualEffectImporter)AssetImporter.GetAtPath(vfxPath);
                var nativeTemplate = new VFXTemplate
                {
                    name = graphViewTemplate.name,
                    category = graphViewTemplate.category,
                    description = graphViewTemplate.description,
                    icon = graphViewTemplate.icon,
                    thumbnail = graphViewTemplate.thumbnail,
                };
                importer.templateProperty = nativeTemplate;
                return true;
            }

            return false;
        }
    }
}
