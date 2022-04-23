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
        ISerializedGraphAsset m_Asset;
        IGraphTemplate m_Template;

        public void SetUp(ICommandTarget target, ISerializedGraphAsset asset, IGraphTemplate template)
        {
            m_CommandTarget = target;
            m_Template = template;
            m_Asset = asset;
        }

        internal void CreateAndLoadAsset(string pathName)
        {
            m_Asset.CreateGraph(m_Template.StencilType);
            m_Template?.InitBasicGraph(m_Asset.GraphModel);

            m_Asset.CreateFile(pathName);
            m_Asset.Save();
            m_Asset = m_Asset.Import();

            m_CommandTarget?.Dispatch(new LoadGraphCommand(m_Asset.GraphModel));
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
    public static class GraphAssetCreationHelpers
    {
        public static void CreateInProjectWindow<TGraphAssetType>(IGraphTemplate template, ICommandTarget target, string path)
            where TGraphAssetType : ScriptableObject, ISerializedGraphAsset
        {
            var asset = ScriptableObject.CreateInstance<TGraphAssetType>();

            var endAction = ScriptableObject.CreateInstance<DoCreateAsset>();
            endAction.SetUp(target, asset, template);

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                asset.GetInstanceID(),
                endAction,
                $"{path}/{template.DefaultAssetName}.{template.GraphFileExtension}",
                AssetPreview.GetMiniThumbnail(asset),
                null);
        }

        /// <summary>
        /// Creates a graph asset.
        /// </summary>
        /// <param name="graphAssetType">The graph asset type.</param>
        /// <param name="stencilType">The type of the stencil.</param>
        /// <param name="name">The name of the graph.</param>
        /// <param name="assetPath">The asset path of the graph.</param>
        /// <param name="graphTemplate">The template of the graph.</param>
        /// <returns>The created graph asset.</returns>
        public static IGraphAsset CreateGraphAsset(Type graphAssetType, Type stencilType, string name, string assetPath,
            IGraphTemplate graphTemplate = null)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(graphAssetType) ||
                !typeof(IGraphAsset).IsAssignableFrom(graphAssetType))
                return null;

            var graphAsset = ScriptableObject.CreateInstance(graphAssetType) as IGraphAsset;

            if (graphAsset as Object != null)
            {
                graphAsset.Name = name;

                graphAsset.CreateGraph(stencilType);
                graphTemplate?.InitBasicGraph(graphAsset.GraphModel);
            }

            if (graphAsset is ISerializedGraphAsset serializedAsset)
            {
                serializedAsset.CreateFile(assetPath);
                serializedAsset.Save();
                graphAsset = serializedAsset.Import();
            }

            return graphAsset;
        }

        /// <summary>
        /// Creates a graph asset using a prompt box.
        /// <param name="graphAssetType">The graph asset type.</param>
        /// <param name="template">The template of the graph.</param>
        /// <param name="title">The title of the file. It will be part of the asset path.</param>
        /// <param name="prompt">The message in the prompt box.</param>
        /// <returns>The created graph asset.</returns>
        /// </summary>
        public static IGraphAsset PromptToCreateGraphAsset(Type graphAssetType, IGraphTemplate template, string title, string prompt)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(graphAssetType) ||
                !typeof(IGraphAsset).IsAssignableFrom(graphAssetType))
                return null;

            var path = EditorUtility.SaveFilePanelInProject(title, template.DefaultAssetName, template.GraphFileExtension, prompt);

            if (path.Length != 0)
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var asset = CreateGraphAsset(graphAssetType, template.StencilType, fileName, path, template);
                return asset;
            }

            return null;
        }
    }
}
