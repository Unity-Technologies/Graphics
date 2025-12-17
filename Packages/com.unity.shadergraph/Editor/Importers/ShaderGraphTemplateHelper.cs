using System;
using System.IO;

using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering.ShaderGraph;

namespace UnityEditor.ShaderGraph
{
    class ShaderGraphTemplateHelper : ITemplateHelper
    {
        const string k_TemplateBasePath = "Packages/com.unity.shadergraph/GraphTemplates";
        const string k_BuiltInTemplatePath = k_TemplateBasePath + "/Default";

        class SaveFileDialog : GraphViewTemplateWindow.ISaveFileDialogHelper
        {
            public string OpenSaveFileDialog()
            {
                return EditorUtility.SaveFilePanelInProject("", "New Shader Graph", "shadergraph", "Create new Shader Graph");
            }
        }

        public static string ShaderGraphToolKey => "ShaderGraph";

        public string toolKey => ShaderGraphToolKey;
        public string packageInfoName => "Shader Graph";
        public string learningSampleName => string.Empty;
        public string templateWindowDocUrl => Documentation.GetPageLink("index");
        public string builtInTemplatePath => k_BuiltInTemplatePath;
        public string builtInCategory => "Default Shader Graph Templates";
        public Type assetType => typeof(Shader);
        public string createNewAssetTitle => "Create new Shader Graph Asset";
        public string insertTemplateTitle => "Insert a template into current Shader Graph Asset";
        public string emptyTemplateGuid => null;
        public string customTemplateIcon => "Packages/com.unity.shadergraph/Editor/Resources/Icons/sg_graph_icon@2x.png";
        public bool showPackageIndexingBanner { get; set; } = true;

        public GraphViewTemplateWindow.ISaveFileDialogHelper saveFileDialogHelper { get; set; } = new SaveFileDialog();

        public void RaiseImportSampleDependencies(PackageManager.PackageInfo packageInfo, PackageManager.UI.Sample sample) { }

        public void RaiseTemplateUsed(GraphViewTemplateDescriptor usedTemplate) =>
            ShaderGraphAnalytics.SendShaderGraphTemplateEvent(usedTemplate);

        public bool TryGetTemplate(string assetPath, out GraphViewTemplateDescriptor graphViewTemplate) => TryGetTemplateStatic(assetPath, out graphViewTemplate);
        internal static bool TryGetTemplateStatic(string assetPath, out GraphViewTemplateDescriptor graphViewTemplate)
        {
            if (FileUtilities.TryGetImporter(assetPath, out var importer))
            {
                var template = importer.Template;
                if (importer.UseAsTemplate)
                {
                    graphViewTemplate = new GraphViewTemplateDescriptor(ShaderGraphToolKey)
                    {
                        name = template.name,
                        category = template.category,
                        description = template.description,
                        icon = template.icon,
                        thumbnail = template.thumbnail,
                    };
                    return true;
                }
            }
            graphViewTemplate = default;
            return false;
        }

        public bool TrySetTemplate(string assetPath, GraphViewTemplateDescriptor graphViewTemplate)
        {
            if (FileUtilities.TryGetImporter(assetPath, out var importer))
            {
                importer.UseAsTemplate = true;

                var template = new ShaderGraphTemplate
                {
                    name = graphViewTemplate.name,
                    category = graphViewTemplate.category,
                    description = graphViewTemplate.description,
                    icon = graphViewTemplate.icon,
                    thumbnail = graphViewTemplate.thumbnail,
                };
                importer.Template = template;
                return true;
            }
            return false;
        }

        public ITemplateSorter[] GetTemplateSorter() => Array.Empty<ITemplateSorter>();
        public SearchProposition[] GetSearchPropositions() => Array.Empty<SearchProposition>();
    }
}
