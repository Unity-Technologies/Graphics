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

        public bool CheckGraphDataValid(GraphData graph, VFXContext refContext)
        {
            HashSet<string> unsupportedType = new HashSet<string>();
            unsupportedType.Clear();

            // Filter property list for any unsupported exposed shader properties.
            foreach (var property in graph.properties.Where(o => o.isExposed))
            {
                var invalid = GetUnsupportedShaderPropertyType().FirstOrDefault(o => o.Key == property.GetType());
                if (invalid.Key != null)
                {
                    unsupportedType.Add(invalid.Value);
                }
            }

            // VFX currently does not support the concept of exposed per-particle keywords.
            if (graph.keywords.Any(o => o.isExposed))
            {
                unsupportedType.Add("Keyword");
            }

            foreach (var unsupported in unsupportedType)
            {
                refContext.RegisterCompilationError(
                    "SGUnsupportedExposedType_" + unsupported.Replace(" ", string.Empty)
                    , VFXErrorType.Warning
                    , unsupported + " blackboard properties in Shader Graph are currently not supported in Visual Effect shaders.");
            }

            return true; //Not any critical issue which is preventing compilation
        }

        public virtual ShaderGraphBinder GetShaderGraphDescriptor(VFXContext context, VFXContextCompiledData data)
        {
            return new ShaderGraphBinder();
        }
    }
}
