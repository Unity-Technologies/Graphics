using System;
using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class DoCreateAsset : EndNameEditAction
    {
        ICommandTarget m_CommandTarget;
        IGraphAssetModel m_AssetModel;
        IGraphTemplate m_Template;

        public void SetUp(ICommandTarget target, IGraphAssetModel assetModel, IGraphTemplate template)
        {
            m_CommandTarget = target;
            m_Template = template;
            m_AssetModel = assetModel;
        }

        internal void CreateAndLoadAsset(string pathName)
        {
            AssetDatabase.CreateAsset(m_AssetModel as Object, AssetDatabase.GenerateUniqueAssetPath(pathName));
            m_AssetModel.CreateGraph(Path.GetFileNameWithoutExtension(pathName), m_Template.StencilType);
            m_Template?.InitBasicGraph(m_AssetModel.GraphModel);

            m_CommandTarget?.Dispatch(new LoadGraphAssetCommand(m_AssetModel));
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            CreateAndLoadAsset(pathName);
        }

        public override void Cancelled(int instanceId, string pathName, string resourceFile)
        {
            Selection.activeObject = null;
        }
    }

    /// <summary>
    /// Helper methods to create graph assets.
    /// </summary>
    /// <typeparam name="TGraphAssetModelType">The type of graph asset model.</typeparam>
    public static class GraphAssetCreationHelpers<TGraphAssetModelType>
        where TGraphAssetModelType : ScriptableObject, IGraphAssetModel
    {
        public static TGraphAssetModelType CreateInMemoryGraphAsset(Type stencilType, string name,
            IGraphTemplate graphTemplate = null)
        {
            return CreateGraphAsset(stencilType, name, null, graphTemplate);
        }

        public static TGraphAssetModelType CreateGraphAsset(Type stencilType, string name, string assetPath,
            IGraphTemplate graphTemplate = null)
        {
            return (TGraphAssetModelType)GraphAssetCreationHelpers.CreateGraphAsset(typeof(TGraphAssetModelType), stencilType, name, assetPath, graphTemplate);
        }

        public static IGraphAssetModel PromptToCreate(IGraphTemplate template, string title, string prompt, string assetExtension)
        {
            return GraphAssetCreationHelpers.PromptToCreate(typeof(TGraphAssetModelType), template, title, prompt, assetExtension);
        }

        public static void CreateInProjectWindow(IGraphTemplate template, ICommandTarget target, string path)
        {
            var asset = ScriptableObject.CreateInstance<TGraphAssetModelType>();

            var endAction = ScriptableObject.CreateInstance<DoCreateAsset>();
            endAction.SetUp(target, asset, template);

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                asset.GetInstanceID(),
                endAction,
                $"{path}/{template.DefaultAssetName}.asset",
                AssetPreview.GetMiniThumbnail(asset),
                null);
        }
    }

    /// <summary>
    /// Helper methods to create graph assets with /graphAssetModelType/.
    /// </summary>
    public static class GraphAssetCreationHelpers
    {
        /// <summary>
        /// Creates a graph asset.
        /// </summary>
        /// <param name="graphAssetModelType">The graph asset model type.</param>
        /// <param name="stencilType">The type of the stencil.</param>
        /// <param name="name">The name of the graph.</param>
        /// <param name="assetPath">The asset path of the graph.</param>
        /// <param name="graphTemplate">The template of the graph.</param>
        /// <returns>The created graph asset.</returns>
        public static IGraphAssetModel CreateGraphAsset(Type graphAssetModelType, Type stencilType, string name, string assetPath,
            IGraphTemplate graphTemplate = null)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(graphAssetModelType) || !typeof(IGraphAssetModel).IsAssignableFrom(graphAssetModelType))
                return null;

            var graphAssetModel = IGraphAssetModelHelper.Create(name, assetPath, graphAssetModelType);
            if (graphAssetModel != null)
            {
                graphAssetModel.CreateGraph(name, stencilType, assetPath != null);
                graphTemplate?.InitBasicGraph(graphAssetModel.GraphModel);

                AssetDatabase.SaveAssets();
            }

            return graphAssetModel;
        }

        /// <summary>
        /// Creates a graph asset using a prompt box.
        /// <param name="graphAssetModelType">The graph asset model type.</param>
        /// <param name="template">The template of the graph.</param>
        /// <param name="title">The title of the file. It will be part of the asset path.</param>
        /// <param name="prompt">The message in the prompt box.</param>
        /// <param name="assetExtension">The asset extension.</param>
        /// <returns>The created graph asset.</returns>
        /// </summary>
        public static IGraphAssetModel PromptToCreate(Type graphAssetModelType, IGraphTemplate template, string title, string prompt, string assetExtension)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(graphAssetModelType) || !typeof(IGraphAssetModel).IsAssignableFrom(graphAssetModelType))
                return null;

            var path = EditorUtility.SaveFilePanelInProject(title, template.DefaultAssetName, assetExtension, prompt);

            if (path.Length != 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                return CreateGraphAsset(graphAssetModelType, template.StencilType, fileName, path, template);
            }

            return null;
        }
    }
}
