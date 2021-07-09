using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetSetupContext
    {
        public List<SubShaderDescriptor> subShaders { get; private set; }
        public List<(string shaderGUI, string renderPipelineAssetType)> customEditorForRenderPipelines { get; private set; }
        public AssetCollection assetCollection { get; private set; }
        public string defaultShaderGUI { get; private set; }
        public List<(string dependencyName, string shaderName)> shaderDependencies { get; private set; }
        public PragmaCollection globalPragmas { get; private set; }

        // pass a HashSet to the constructor to have it gather asset dependency GUIDs
        public TargetSetupContext(AssetCollection assetCollection = null, PragmaCollection globalPragmaCollection = null)
        {
            subShaders = new List<SubShaderDescriptor>();
            shaderDependencies = new List<(string dependencyName, string shaderName)>();
            if (globalPragmaCollection != null)
                globalPragmas = globalPragmaCollection;
            else
                globalPragmas = new PragmaCollection();
            this.customEditorForRenderPipelines = new List<(string shaderGUI, string renderPipelineAssetType)>();
            this.assetCollection = assetCollection;
        }

        public void AddSubShader(SubShaderDescriptor subShader)
        {
            subShaders.Add(subShader);
        }

        public void AddAssetDependency(GUID guid, AssetCollection.Flags flags)
        {
            assetCollection?.AddAssetDependency(guid, flags);
        }

        public void SetDefaultShaderGUI(string defaultShaderGUI)
        {
            this.defaultShaderGUI = defaultShaderGUI;
        }

        public void AddShaderDependency(string dependencyName, string shaderName)
        {
            shaderDependencies.Add((dependencyName, shaderName));
        }

        public void AddGlobalPragma(PragmaDescriptor pd)
        {
            globalPragmas?.Add(pd);
        }

        public void AddCustomEditorForRenderPipeline(string shaderGUI, Type renderPipelineAssetType)
        {
            this.customEditorForRenderPipelines.Add((shaderGUI, renderPipelineAssetType.FullName));
        }

        public bool HasCustomEditorForRenderPipeline(Type renderPipelineAssetType)
            => this.customEditorForRenderPipelines.Any(c => c.renderPipelineAssetType == renderPipelineAssetType.FullName);
    }
}
