using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable, GenerationAPI] // TODO: Public
    internal abstract class Target
    {
        public string displayName { get; set; }
        public bool isHidden { get; set; }
        public abstract bool IsActive();
        public abstract void Setup(ref TargetSetupContext context);
        public abstract void GetFields(ref TargetFieldContext context);
        public abstract void GetActiveBlocks(ref TargetActiveBlockContext context);
        public abstract void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange);

        public virtual void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode) { }
        public virtual void ProcessPreviewMaterial(Material material) { }
    }
}
