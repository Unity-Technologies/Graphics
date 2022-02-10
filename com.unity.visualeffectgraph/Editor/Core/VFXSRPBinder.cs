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
        private static readonly List<(Type type, string name, bool error)> s_BaseUnsupportedShaderPropertyTypes = new List<(Type type, string name, bool error)>()
        {
            (typeof(VirtualTextureShaderProperty), "Virtual Texture", true),
            (typeof(GradientShaderProperty), "Gradient", false)
        };

        public virtual IEnumerable<(Type type, string name, bool error)> GetUnsupportedShaderPropertyType()
        {
            return s_BaseUnsupportedShaderPropertyTypes;
        }

        public bool CheckGraphDataValid(GraphData graph, VFXContext refContext)
        {
            bool valid = true;

            // Filter property list for any unsupported exposed shader properties.
            foreach (var property in graph.properties.Where(o => o.isExposed))
            {
                var unsupported = GetUnsupportedShaderPropertyType().FirstOrDefault(o => o.type == property.GetType());
                if (unsupported.type != null)
                {
                    valid = valid && !unsupported.error;
                    refContext.RegisterCompilationError(
                        "SGUnsupportedExposedType_" + unsupported.name.Replace(" ", string.Empty)
                        , unsupported.error ? VFXErrorType.Error : VFXErrorType.Warning
                        , unsupported.name
                          + " blackboard properties in Shader Graph are currently not supported in Visual Effect shaders."
                          + (unsupported.error ? " The Effect Output won't compile." : string.Empty));
                }
            }

            // VFX currently does not support the concept of exposed per-particle keywords.
            if (graph.keywords.Any(o => o.isExposed))
            {
                refContext.RegisterCompilationError(
                    "SGUnsupportedExposedType_Keword"
                    , VFXErrorType.Warning
                    , "Exposed Keyword properties in Shader Graph are currently not supported in Visual Effect shaders.");
            }

            return valid;
        }

        public virtual ShaderGraphBinder GetShaderGraphDescriptor(VFXContext context, VFXContextCompiledData data)
        {
            return new ShaderGraphBinder();
        }
    }
}
