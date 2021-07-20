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
        }

        abstract public string templatePath { get; }
        virtual public string runtimePath { get { return templatePath; } } //optional different path for .hlsl included in runtime
        abstract public string SRPAssetTypeStr { get; }
        abstract public Type SRPOutputDataType { get; }

        public virtual void SetupMaterial(Material mat, bool hasMotionVector = false, bool hasShadowCasting = false, ShaderGraphVfxAsset shaderGraph = null) {}

        public virtual VFXAbstractRenderedOutput.BlendMode GetBlendModeFromMaterial(VFXMaterialSerializedSettings materialSettings)
        {
            return VFXAbstractRenderedOutput.BlendMode.Opaque;
        }

        public virtual bool TransparentMotionVectorEnabled(Material mat) => true;

        public virtual string GetShaderName(ShaderGraphVfxAsset shaderGraph) => string.Empty;

        public virtual bool IsGraphDataValid(GraphData graph) => false;

        public virtual ShaderGraphBinder GetShaderGraphDescriptor(VFXContext context, VFXContextCompiledData data)
        {
            return new ShaderGraphBinder();
        }
    }
}
