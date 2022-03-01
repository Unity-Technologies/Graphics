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

        internal void CreateAndLoadAsset(string pathName, GraphAssetType graphAssetType = GraphAssetType.AssetGraph)
        {
            AssetDatabase.CreateAsset(m_AssetModel as Object, AssetDatabase.GenerateUniqueAssetPath(pathName));
            m_AssetModel.CreateGraph(Path.GetFileNameWithoutExtension(pathName), m_Template.StencilType, graphAssetType: graphAssetType);
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
        public static IGraphAssetModel CreateInMemoryGraphAsset(Type stencilType, string name,
            IGraphTemplate graphTemplate = null, GraphAssetType graphAssetType = GraphAssetType.AssetGraph)
        {
            return CreateGraphAsset(stencilType, name, null, graphTemplate, graphAssetType);
        }

        public static IGraphAssetModel CreateGraphAsset(Type stencilType, string name, string assetPath,
            IGraphTemplate graphTemplate = null, GraphAssetType graphAssetType = GraphAssetType.AssetGraph)
        {
            IGraphAssetModel graphAssetModel;

            graphAssetModel = IGraphAssetModelHelper.Create(name, assetPath, typeof(TGraphAssetModelType));
            graphAssetModel.CreateGraph(name, stencilType, assetPath != null, graphAssetType);
            graphTemplate?.InitBasicGraph(graphAssetModel.GraphModel);

            AssetDatabase.SaveAssets();

            return graphAssetModel;
        }

        public static IGraphAssetModel PromptToCreate(IGraphTemplate template, string title, string prompt, string assetExtension, GraphAssetType graphAssetType = GraphAssetType.AssetGraph)
        {
            var path = EditorUtility.SaveFilePanelInProject(title, template.DefaultAssetName, assetExtension, prompt);

            if (path.Length != 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                return CreateGraphAsset(template.StencilType, fileName, path, template, graphAssetType);
            }

            return null;
        }

        public static void CreateInProjectWindow(IGraphTemplate template, ICommandTarget target, string path, GraphAssetType graphAssetType = GraphAssetType.AssetGraph)
        {
            var asset = ScriptableObject.CreateInstance<TGraphAssetModelType>();
            asset.GraphAssetType = graphAssetType;

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
}
