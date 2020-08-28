using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetSetupContext
    {
        public List<SubShaderDescriptor> subShaders { get; private set; }
        public List<string> assetDependencyPaths { get; private set; }
        public List<(string shaderGUI, string renderPipelineType)> customEditorForRenderPipelines { get; private set; }
        public string defaultShaderGUI { get; private set; }

        public TargetSetupContext()
        {
            subShaders = new List<SubShaderDescriptor>();
            assetDependencyPaths = new List<string>();
            customEditorForRenderPipelines = new List<(string shaderGUI, string renderPipelineType)>();
        }

        public void AddSubShader(SubShaderDescriptor subShader)
        {
            subShaders.Add(subShader);
        }

        public void AddAssetDependencyPath(string path)
        {
            assetDependencyPaths.Add(path);
        }

        public void SetDefaultShaderGUI(string defaultShaderGUI)
        {
            this.defaultShaderGUI = defaultShaderGUI;
        }

        public void AddRenderPipelineCustomEditor(string shaderGUI, string renderPipelineType)
        {
            this.customEditorForRenderPipelines.Add((shaderGUI, renderPipelineType));
        }
    }
}
