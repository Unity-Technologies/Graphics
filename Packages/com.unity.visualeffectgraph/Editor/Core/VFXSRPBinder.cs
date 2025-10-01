using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    abstract class VFXSRPBinder
    {
        public struct ShaderGraphBinder
        {
            public static readonly PragmaDescriptor kPragmaDescriptorNone = new() { value = "None" };
            public static readonly KeywordDescriptor kKeywordDescriptorNone = new() { referenceName = "None" };

            public StructCollection baseStructs;
            public FieldDescriptor[] varyingsAdditionalFields;
            public DependencyCollection fieldDependencies;
            public (PragmaDescriptor oldDesc, PragmaDescriptor newDesc)[] pragmasReplacement;
            public (KeywordDescriptor oldDesc, KeywordDescriptor newDesc)[] keywordsReplacement;
            public bool useFragInputs;
        }

        abstract public string templatePath { get; }
        virtual public string runtimePath { get { return templatePath; } } //optional different path for .hlsl included in runtime
        abstract public string SRPAssetTypeStr { get; }
        abstract public Type SRPOutputDataType { get; }

        public bool IsShaderVFXCompatible(ShaderGraphVfxAsset shaderGraph)
        {
            var path = AssetDatabase.GetAssetPath(shaderGraph);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null)
                return IsShaderVFXCompatible(shader);
            return false;
        }

        public abstract bool IsShaderVFXCompatible(Shader shader);
        public virtual void SetupMaterial(Material mat, bool hasMotionVector = false, bool hasShadowCasting = false, ShaderGraphVfxAsset shaderGraph = null) { }

        public virtual bool AllowMaterialOverride(ShaderGraphVfxAsset shaderGraph) => true;

        public virtual bool TryGetQueueOffset(ShaderGraphVfxAsset shaderGraph, VFXMaterialSerializedSettings materialSettings, out int queueOffset)
        {
            queueOffset = 0;
            return false;
        }

        public virtual bool TryGetCastShadowFromMaterial(ShaderGraphVfxAsset shaderGraph, VFXMaterialSerializedSettings materialSettings, out bool castShadow)
        {
            castShadow = false;
            return false;
        }

        public virtual VFXAbstractRenderedOutput.BlendMode GetBlendModeFromMaterial(ShaderGraphVfxAsset shaderGraph, VFXMaterialSerializedSettings materialSettings)
        {
            return VFXAbstractRenderedOutput.BlendMode.Opaque;
        }

        public virtual bool GetSupportsMotionVectorPerVertex(ShaderGraphVfxAsset shaderGraph, VFXMaterialSerializedSettings materialSettings) => true;

        public virtual bool TransparentMotionVectorEnabled(Material mat) => true;

        public virtual bool GetSupportsRayTracing() => false;

        public virtual string GetShaderName(ShaderGraphVfxAsset shaderGraph) => string.Empty;

        // List of shader properties that currently are not supported for exposure in VFX shaders (for all pipeline).
        private static readonly List<(Type type, string name)> s_BaseUnsupportedShaderPropertyTypes = new()
        {
            (typeof(VirtualTextureShaderProperty), "Virtual Texture")
        };

        public virtual IEnumerable<(Type type, string name)> GetUnsupportedShaderPropertyType()
        {
            return s_BaseUnsupportedShaderPropertyTypes;
        }

        public bool CheckGraphDataValid(GraphData graph, out List<string> errors)
        {
            bool valid = true;
            errors = new List<string>();
            var message = new StringBuilder();

            // Filter property list for any unsupported exposed shader properties.
            foreach (var property in graph.properties.Where(o => o.isExposed))
            {
                var unsupported = GetUnsupportedShaderPropertyType().FirstOrDefault(o => o.type == property.GetType());
                if (unsupported.type != null)
                {
                    errors.Add($"Shader Graph blackboard property of type '{unsupported.name}' is not currently supported in Visual Effect Graph");
                    message.Append(unsupported.name);
                    valid = false;
                }
            }

            if (!valid)
                Debug.LogError($"{message} blackboard properties in Shader Graph are not currently supported in Visual Effect Shaders.");

            return valid;
        }

        public virtual ShaderGraphBinder GetShaderGraphDescriptor(VFXContext context, VFXTaskCompiledData data)
        {
            return new ShaderGraphBinder();
        }

        public virtual IEnumerable<GraphicsDeviceType> GetSupportedGraphicDevices()
        {
            yield return GraphicsDeviceType.Direct3D11;
            yield return GraphicsDeviceType.OpenGLCore;
            yield return GraphicsDeviceType.OpenGLES3;
            yield return GraphicsDeviceType.Metal;
            yield return GraphicsDeviceType.Vulkan;
            yield return GraphicsDeviceType.XboxOne;
            yield return GraphicsDeviceType.GameCoreXboxOne;
            yield return GraphicsDeviceType.GameCoreXboxSeries;
            yield return GraphicsDeviceType.PlayStation4;
            yield return GraphicsDeviceType.PlayStation5;
            yield return GraphicsDeviceType.Switch;
            yield return GraphicsDeviceType.WebGPU;
        }
    }
}
