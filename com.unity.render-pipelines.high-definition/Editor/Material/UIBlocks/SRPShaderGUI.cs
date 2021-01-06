using System;
using System.Linq;
using Unity.Assets.MaterialVariant.Editor;

namespace UnityEditor.Rendering.HighDefinition
{
    public abstract class SRPShaderGUI : ShaderGUI
    {
        protected MaterialVariant[] variants;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            variants = MaterialVariant.GetMaterialVariantsFor(materialEditor);
        }

        public bool IsPropertyBlockedInAncestorsForAnyVariant(MaterialProperty property)
        {
            return variants != null && variants.Any(o => o.IsPropertyBlockedInAncestors(property.name));
        }

        public MaterialPropertyScope CreateOverrideScopeFor(MaterialProperty property, bool forceMode = false)
                => new MaterialPropertyScope(new MaterialProperty[] { property }, variants, forceMode);

        public MaterialPropertyScope CreateOverrideScopeFor(MaterialProperty[] properties, bool forceMode = false)
            => new MaterialPropertyScope(properties, variants, forceMode);

        public MaterialPropertyScope CreateOverrideScopeFor(params MaterialProperty[] properties)
            => new MaterialPropertyScope(properties, variants, false);

        public MaterialRenderQueueScope CreateRenderQueueOverrideScope(Func<int> valueGetter)
            => new MaterialRenderQueueScope(variants, valueGetter);

        public MaterialNonDrawnPropertyScope<T> CreateNonDrawnOverrideScope<T>(string propertyName, T value)
            where T : struct
            => new MaterialNonDrawnPropertyScope<T>(propertyName, value, variants);
    }
}
