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
            int result = dependencyName.CompareTo(other.dependencyName);
            if (result == 0)
                result = shaderName.CompareTo(other.shaderName);
            return result;
        }
    }

    internal struct ShaderCustomEditor : IComparable<ShaderCustomEditor>
    {
        public string shaderGUI;
        public string renderPipelineAssetType;

        public int CompareTo(ShaderCustomEditor other)
        {
            int result = renderPipelineAssetType.CompareTo(other.renderPipelineAssetType);
            if (result == 0)
                result = shaderGUI.CompareTo(other.shaderGUI);
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

        // if set, this subshader is intended to be placed not in the main shader result, but in an additional shader
        // the string is used as a postfix on the shader name (i.e. "shaderName-additionalShaderID")
        public string additionalShaderID;

        // these are per-shader settings, that get passed up to the shader level
        // and merged with the same settings from other subshaders
        public List<ShaderDependency> shaderDependencies;

        // if null, it will use the global custom editors provided via TargetSetupContext.AddCustomEditorForRenderPipeline
        public List<ShaderCustomEditor> shaderCustomEditors;

        // old CustomEditor controll... TODO: anyone use it???  Built-in is the only one who uses it..
        // the rest of the pipelines all use the customEditorForRenderPipeline path.

        // if null, it will use the global defaultShaderGUI provided via TargetSetupContext.SetDefaultShaderGUI
        public string defaultShaderGUI;
    }
}
