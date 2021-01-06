using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Rendering.MaterialVariants;

namespace UnityEditor.Rendering
{
    public abstract class SRPShaderGUI : ShaderGUI
    {
        protected MaterialVariant[] variants;

        /// <summary>
        /// Unity calls this function when you assign a new shader to the material.
        /// </summary>
        /// <param name="material">The current material.</param>
        /// <param name="oldShader">The shader the material currently uses.</param>
        /// <param name="newShader">The new shader to assign to the material.</param>
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            var variant = MaterialVariant.GetMaterialVariantFromObject(material);
            if (variant)
                variant.SetParent(newShader);
        }

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
