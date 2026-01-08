using System;
using System.IO;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor.Search;
using UnityEngine.VFX;

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

        const string k_EmptyTemplateRelativePath = "/Editor/Templates/Empty.vfx";

        string m_EmptyTemplateGuid;

        public static string VFXGraphToolKey => "VFXGraph";
        public string toolKey => VFXGraphToolKey;
        public string packageInfoName => VisualEffectGraphPackageInfo.name;
        public string learningSampleName => "Learning Templates";
        public string templateWindowDocUrl =>Documentation.GetPageLink("Templates-window");
        public string builtInTemplatePath => VisualEffectAssetEditorUtility.templatePath;
        public string builtInCategory => "Default VFX Graph Templates";
        public Type assetType => typeof(VisualEffectAsset);

        public string emptyTemplateGuid
        {
            get
            {
                if (string.IsNullOrEmpty(m_EmptyTemplateGuid))
                {
                    m_EmptyTemplateGuid = FindEmptyTemplateDescriptor();
                }

                return m_EmptyTemplateGuid;
            }
        }

        public string createNewAssetTitle => "Create new VFX Asset";
        public string insertTemplateTitle => "Insert a template into current VFX Asset";
        public string customTemplateIcon => $"{VisualEffectGraphPackageInfo.assetPackagePath}/Editor/Templates/UI/CustomVFXGraph@256.png";

        public bool showPackageIndexingBanner
        {
            get => VFXViewPreference.showPackageIndexingBanner;
            set => VFXViewPreference.showPackageIndexingBanner = value;
        }

        public GraphViewTemplateWindow.ISaveFileDialogHelper saveFileDialogHelper { get; set; } = new SaveFileDialog();

        public static void ImportSampleDependencies(PackageManager.PackageInfo packageInfo, PackageManager.UI.Sample sample)
        {
            try
            {
                var sampleDependencyImporterType = typeof(Rendering.DebugState).Assembly.GetType("SampleDependencyImporter");
                var instanceProperty = sampleDependencyImporterType.GetProperty("instance", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                var importerInstance = instanceProperty.GetValue(null);
                var importSampleDependenciesMethod = sampleDependencyImporterType.GetMethod(
                    "ImportSampleDependencies",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(PackageManager.PackageInfo), typeof(PackageManager.UI.Sample) },
                    null);
                importSampleDependenciesMethod.Invoke(importerInstance, new object[] { packageInfo, sample });
            }
            catch (Exception e)
            {
                Debug.LogError("ImportSampleDependencies unexpected failure, SampleDependencyImporter might have been changed or has been moved.");
                Debug.LogException(e);
            }
        }

        public void RaiseImportSampleDependencies(PackageManager.PackageInfo packageInfo, PackageManager.UI.Sample sample)
        {
            ImportSampleDependencies(packageInfo, sample);
        }

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

        public SearchProposition[] GetSearchPropositions() => Array.Empty<SearchProposition>();

        public ITemplateSorter[] GetTemplateSorter() => Array.Empty<ITemplateSorter>();

        internal static bool TryGetTemplateStatic(string vfxPath, out GraphViewTemplateDescriptor graphViewTemplate)
        {
            // Can happen because the search engine sometimes finds prefabs
            if (!vfxPath.EndsWith(VisualEffectResource.Extension))
            {
                graphViewTemplate = default;
                return false;
            }

            if (AssetImporter.GetAtPath(vfxPath) is VisualEffectImporter { useAsTemplateProperty: true, templateProperty: var nativeTemplate })
            {
                graphViewTemplate = new GraphViewTemplateDescriptor(VFXGraphToolKey)
                {
                    name = nativeTemplate.name,
                    category = nativeTemplate.category,
                    description = nativeTemplate.description,
                    icon = nativeTemplate.icon,
                    thumbnail = nativeTemplate.thumbnail,
                    order = nativeTemplate.order,
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
                importer.useAsTemplateProperty = true;
                importer.templateProperty = nativeTemplate;
                return true;
            }

            return false;
        }

        string FindEmptyTemplateDescriptor()
        {
            return AssetDatabase.GUIDFromAssetPath(VisualEffectGraphPackageInfo.assetPackagePath + k_EmptyTemplateRelativePath).ToString();
        }
    }
}
