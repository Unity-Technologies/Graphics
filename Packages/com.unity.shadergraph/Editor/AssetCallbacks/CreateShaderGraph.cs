using System;
using System.IO;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    static class CreateShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/Blank Shader Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateBlankShaderGraph()
        {
            CreateFromTemplate(null, string.Empty);
        }

        [MenuItem("Assets/Create/Shader Graph/From Template...")]
        public static void CreateFromTemplate()
        {
            CreateFromTemplate(null);
        }

        /// <summary>
        /// Prompts the user to create a new Shader Graph from a Template.
        /// /// The Template Browser opens if <paramref name="templateSourcePath"/> is null.
        /// </summary>
        /// <param name="callback">Callback to execute with the created shader graph asset.</param>
        /// <param name="templateSourcePath">Shader Graph template file. Use string.Empty for Blank Shader Graph.</param>
        /// <param name="filename">New Shader Graph filename.</param>
        internal static void CreateFromTemplate(Action<string> callback = null, string templateSourcePath = null, string filename = null)
        {
            if (!string.IsNullOrEmpty(templateSourcePath))
            {
                if (!AssetDatabase.AssetPathExists(templateSourcePath))
                {
                    Debug.LogError($"Shader Graph Template File missing at path: {templateSourcePath}.");
                    return;
                }

                if (Path.GetExtension(templateSourcePath) != $".{ShaderGraphImporter.Extension}")
                {
                    Debug.LogError($"Shader Graph Template File {templateSourcePath} is not a Shader Graph asset.");
                    return;
                }
            }

            bool projectWindowIsVisible = EditorWindow.HasOpenInstances<ProjectBrowser>();

            NewGraphFromTemplateAction action = ScriptableObject.CreateInstance<NewGraphFromTemplateAction>();

            if (callback != null)
                action.Callback = callback;

            if (!string.IsNullOrEmpty(filename))
                action.Filename = filename;

            ShaderGraphTemplateHelper shaderGraphTemplateHelper = new ShaderGraphTemplateHelper();

            if (templateSourcePath == null)
            {
                GraphViewTemplateWindow.ShowCreateFromTemplate(shaderGraphTemplateHelper, action.CreateAndRenameGraphFromTemplate, showSaveDialog: !projectWindowIsVisible);
            }
            else
            {
                if (shaderGraphTemplateHelper.TryGetTemplate(templateSourcePath, out GraphViewTemplateDescriptor graphViewTemplate))
                    shaderGraphTemplateHelper.RaiseTemplateUsed(graphViewTemplate);

                if (projectWindowIsVisible)
                {
                    action.CreateAndRenameGraphFromTemplate(templateSourcePath, null);
                }
                else
                {
                    string graphDestinationPath = shaderGraphTemplateHelper.saveFileDialogHelper.OpenSaveFileDialog();
                    if (!string.IsNullOrEmpty(graphDestinationPath))
                        action.CreateAndRenameGraphFromTemplate(templateSourcePath, graphDestinationPath);
                }
            }
        }

        /// <summary>
        /// Prompts the user to create a new Shader Graph from a Template and associated Material.
        /// The Template Browser opens if <paramref name="templateSourcePath"/> is null.
        /// </summary>
        /// <param name="callback">Callback to execute with the created material.</param>
        /// <param name="templateSourcePath">Shader Graph template file. Use string.Empty for Blank Shader Graph.</param>
        /// /// <param name="filename">New Shader Graph filename.</param>
        internal static void CreateGraphAndMaterialFromTemplate(Action<Material> callback = null, string templateSourcePath = null, string filename = null)
        {
            CreateFromTemplate((template) =>
            {
                if (!string.IsNullOrEmpty(template))
                {
                    Material material;
                    if (ShaderGraphPreferences.GetOrPromptGraphTemplateWorkflow() == ShaderGraphPreferences.GraphTemplateWorkflow.MaterialVariant)
                    {
                        var srcMaterial = AssetDatabase.LoadAssetAtPath<Material>(template);
                        material = new Material(srcMaterial);
                        material.parent = srcMaterial;
                    }
                    else
                    {
                        var shader = AssetDatabase.LoadAssetAtPath<Shader>(template);
                        material = new Material(shader);
                    }

                    var materialPath = Path.Combine(Path.GetDirectoryName(template), Path.GetFileNameWithoutExtension(template) + ".mat");
                    AssetDatabase.CreateAsset(material, materialPath);
                    AssetDatabase.SaveAssetIfDirty(material);
                    AssetDatabase.Refresh(ImportAssetOptions.Default);

                    callback?.Invoke(material);
                }
            },
            templateSourcePath,
            filename);
        }
    }
}
