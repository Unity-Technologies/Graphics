using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using Object = System.Object;
using System.Reflection;

namespace UnityEditor.VFX
{
    abstract class VFXSRPBinder
    {
        public struct ShaderGraphBinder
        {
            public static readonly PragmaDescriptor kPragmaDescriptorNone = new() { value = "None" };

            public StructCollection structs;
            public DependencyCollection fieldDependencies;
            public (PragmaDescriptor oldDesc, PragmaDescriptor newDesc)[] pragmasReplacement;
            public bool useFragInputs;
        }

        abstract public string templatePath { get; }
        virtual public string runtimePath { get { return templatePath; } } //optional different path for .hlsl included in runtime
        abstract public string SRPAssetTypeStr { get; }
        abstract public Type SRPOutputDataType { get; }

        public virtual void SetupMaterial(Material mat, bool hasMotionVector = false, bool hasShadowCasting = false, ShaderGraphVfxAsset shaderGraph = null) { }

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

        public virtual string GetShaderName(ShaderGraphVfxAsset shaderGraph) => string.Empty;

        // List of shader properties that currently are not supported for exposure in VFX shaders (for all pipeline).
        private static readonly Dictionary<Type, string> s_BaseUnsupportedShaderPropertyTypes = new Dictionary<Type, string>()
        {
            { typeof(VirtualTextureShaderProperty),   "Virtual Texture"   },
            { typeof(GradientShaderProperty),         "Gradient"          }
        };

        public virtual IEnumerable<KeyValuePair<Type, string>> GetUnsupportedShaderPropertyType()
        {
            return s_BaseUnsupportedShaderPropertyTypes;
        }

        public bool IsGraphDataValid(GraphData graph)
        {
            var valid = true;
            var warnings = new List<string>();

            var unsupportedShaderPropertyTypes = GetUnsupportedShaderPropertyType().ToDictionary(a => a.Key, b => b.Value);
            // Filter property list for any unsupported shader properties.
            foreach (var property in graph.properties)
            {
                if (unsupportedShaderPropertyTypes.ContainsKey(property.GetType()))
                {
                    warnings.Add(unsupportedShaderPropertyTypes[property.GetType()]);
                    valid = false;
                }
            }

            // VFX currently does not support the concept of per-particle keywords.
            if (graph.keywords.Any())
            {
                warnings.Add("Keyword");
                valid = false;
            }

            if (!valid)
                Debug.LogWarning($"({String.Join(", ", warnings)}) blackboard properties in Shader Graph are currently not supported in Visual Effect shaders. Falling back to default generation path.");

            return valid;
        }

        public virtual ShaderGraphBinder GetShaderGraphDescriptor(VFXContext context, VFXContextCompiledData data)
        {
            return new ShaderGraphBinder();
        }
    }
}
