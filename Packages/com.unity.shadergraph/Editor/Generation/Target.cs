using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable, GenerationAPI] // TODO: Public
    internal abstract class Target : JsonObject
    {
        public string displayName { get; set; }
        public bool isHidden { get; set; }
        internal virtual bool ignoreCustomInterpolators => true;
        internal virtual int padCustomInterpolatorLimit => 4;
        internal virtual bool prefersSpritePreview => false;
        public abstract bool IsActive();
        public abstract void Setup(ref TargetSetupContext context);
        public abstract void GetFields(ref TargetFieldContext context);
        public abstract void GetActiveBlocks(ref TargetActiveBlockContext context);
        public abstract void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo);
        public virtual void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode) { }
        public virtual void ProcessPreviewMaterial(Material material) { }
        public virtual object saveContext => null;
        public virtual bool IsNodeAllowedByTarget(Type nodeType)
        {
            NeverAllowedByTargetAttribute never = NodeClassCache.GetAttributeOnNodeType<NeverAllowedByTargetAttribute>(nodeType);
            return never == null;
        }

        public virtual bool DerivativeModificationCallback(
                out string dstGraphFunctions,
                out string dstGraphPixel,
                out bool[] adjustedUvDerivs,
                string primaryShaderName,
                string passName,
                string propStr,
                string surfaceDescStr,
                string graphFuncStr,
                string graphPixelStr,
                List<string> customFuncs,
                bool applyEmulatedDerivatives)
        {
            dstGraphFunctions = "";
            dstGraphPixel = "";
            adjustedUvDerivs = new bool[Enum.GetValues(typeof(UVChannel)).Length];
            return false;
        }

        // think this is not called by anyone anymore, leaving it to avoid changing client code
        public abstract bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline);

        virtual public SubTarget activeSubTarget { get; set; }
    }
}
