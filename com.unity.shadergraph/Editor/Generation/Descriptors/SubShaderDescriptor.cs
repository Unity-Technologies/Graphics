using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    internal struct ShaderDependency : IComparable<ShaderDependency>
    {
        public string dependencyName;
        public string shaderName;

        public int CompareTo(ShaderDependency other)
        {
            int result = string.CompareOrdinal(dependencyName, other.dependencyName);
            if (result == 0)
                result = string.CompareOrdinal(shaderName, other.shaderName);
            return result;
        }
    }

    internal struct ShaderCustomEditor : IComparable<ShaderCustomEditor>
    {
        public string shaderGUI;
        public string renderPipelineAssetType;

        public int CompareTo(ShaderCustomEditor other)
        {
            int result = string.CompareOrdinal(renderPipelineAssetType, other.renderPipelineAssetType);
            if (result == 0)
                result = string.CompareOrdinal(shaderGUI, other.shaderGUI);
            return result;
        }
    }

    [GenerationAPI]
    internal struct SubShaderDescriptor
    {
        public string pipelineTag;
        public string customTags;
        public string renderType;
        public string renderQueue;
        public bool generatesPreview;
        public PassCollection passes;
        public List<string> usePassList;

        // if set, this subshader is intended to be placed not in the primary shader result, but in an additional shader.
        // the name of the additional shader is specified by this string,
        // with "{Name}" replaced by the name of the primary shader
        public string additionalShaderID;

        // these are per-shader settings, that get passed up to the shader level
        // and merged with the same settings from other subshaders
        public List<ShaderDependency> shaderDependencies;

        // if null, it will use the global custom editors provided via TargetSetupContext.AddCustomEditorForRenderPipeline
        public List<ShaderCustomEditor> shaderCustomEditors;

        // if null, it will use the global defaultShaderGUI provided via TargetSetupContext.SetDefaultShaderGUI
        // NOTE: only Builtin Target currently uses this value
        public string shaderCustomEditor;

        // if null, the default shadergraph fallback shader is used
        public string shaderFallback;
    }
}
