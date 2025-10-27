using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetSetupContext
    {
        public List<SubShaderDescriptor> subShaders { get; private set; }

        public KernelCollection kernels { get; private set; }
        public AssetCollection assetCollection { get; private set; }

        // these are data that are now stored in the subshaders.
        // but for backwards compatibility with the existing Targets,
        // we store the values provided directly by the Target,
        // and apply them to all of the subShaders provided by the Target (that don't have their own setting)
        // the Targets are free to switch to specifying these values per SubShaderDescriptor instead,
        // if they want to specify different values for each subshader.
        private List<ShaderCustomEditor> customEditorForRenderPipelines;
        private string defaultShaderGUI;

        // assetCollection is used to gather asset dependencies
        public TargetSetupContext(AssetCollection assetCollection = null)
        {
            subShaders = new List<SubShaderDescriptor>();
            kernels = new KernelCollection();
            this.assetCollection = assetCollection;
        }

        public void SetupFinalize()
        {
            // copy custom editors to each subshader, if they don't have their own specification
            if (subShaders == null)
                return;

            for (int i = 0; i < subShaders.Count; i++)
            {
                var subShader = subShaders[i];

                if ((subShader.shaderCustomEditors == null) && (customEditorForRenderPipelines != null))
                    subShader.shaderCustomEditors = new List<ShaderCustomEditor>(customEditorForRenderPipelines);

                if (subShader.shaderCustomEditor == null)
                    subShader.shaderCustomEditor = defaultShaderGUI;

                subShaders[i] = subShader; // yay C# structs
            }
        }

        public void AddSubShader(SubShaderDescriptor subShader)
        {
            subShaders.Add(subShader);
        }

        public void AddKernel(KernelDescriptor kernel)
        {
            kernels.Add(kernel);
        }

        public void AddAssetDependency(GUID guid, AssetCollection.Flags flags)
        {
            assetCollection?.AddAssetDependency(guid, flags);
        }

        public void SetDefaultShaderGUI(string defaultShaderGUI)
        {
            this.defaultShaderGUI = defaultShaderGUI;
        }

        public void AddCustomEditorForRenderPipeline(string shaderGUI, Type renderPipelineAssetType)
            => AddCustomEditorForRenderPipeline(shaderGUI, renderPipelineAssetType.FullName);

        public void AddCustomEditorForRenderPipeline(string shaderGUI, string renderPipelineAssetTypeFullName)
        {
            if (customEditorForRenderPipelines == null)
                customEditorForRenderPipelines = new List<ShaderCustomEditor>();

            customEditorForRenderPipelines.Add(
                new ShaderCustomEditor()
                {
                    shaderGUI = shaderGUI,
                    renderPipelineAssetType = renderPipelineAssetTypeFullName
                });
        }

        public bool HasCustomEditorForRenderPipeline<RPT>()
            => HasCustomEditorForRenderPipeline(typeof(RPT).FullName);

        public bool HasCustomEditorForRenderPipeline(Type renderPipelineAssetType)
            => HasCustomEditorForRenderPipeline(renderPipelineAssetType.FullName);

        public bool HasCustomEditorForRenderPipeline(string renderPipelineAssetTypeFullName)
            => customEditorForRenderPipelines?.Any(c => c.renderPipelineAssetType == renderPipelineAssetTypeFullName) ?? false;
    }
}
