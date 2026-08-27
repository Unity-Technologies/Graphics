using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Shaders.UITK.Sample
{
    /// <summary>
    /// Per-element shader-property driver for a <see cref="VisualElement"/>'s material.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each element is given its own <see cref="Material"/> clone, created lazily from the
    /// fallback material the first time a property is set. The clone is assigned to the
    /// element's <c>style.unityMaterial</c> exactly once; thereafter every Set call mutates a
    /// property on that same clone in place and calls <see cref="VisualElement.MarkDirtyRepaint"/>.
    /// </para>
    /// <para>
    /// This mirrors UGUI's <c>IMaterialModifier</c> pattern (clone once, <c>SetFloat</c> each
    /// frame) and is allocation-free per call, so a property can safely be driven every frame
    /// for animation. The renderer keeps the live <see cref="Material"/> reference and reads its
    /// properties fresh on each repaint, so the in-place mutation is picked up without
    /// reassigning the material.
    /// </para>
    /// <para>
    /// The clone is stored on the element itself (its <c>style.unityMaterial</c>) and read back
    /// from there, so no external cache is kept. All bindings on the same element therefore share
    /// the one clone: stacking <see cref="ElementSizeBinding"/>,
    /// <see cref="InteractiveElementStateBinding"/>, etc. composes correctly, each writing its own
    /// properties on the shared clone.
    /// </para>
    /// <para>
    /// The clone's lifetime follows the element's panel membership: it is destroyed when the
    /// element detaches from its panel (a rebuild discards the old tree), and lazily recreated
    /// and reassigned if a detached element is reused and driven again.
    /// </para>
    /// <para>
    /// A Set call made while no material is available (every binding so far constructed with a
    /// null fallback) is dropped, not deferred — provide the material before, or with, the
    /// first write.
    /// </para>
    /// </remarks>
    public static class UIShaderBinding
    {
        public static void SetFloat(VisualElement element, Material fallbackMaterial, string name, float value)
        {
            var material = GetOrCreate(element, fallbackMaterial);
            if (material == null)
                return;
            material.SetFloat(name, value);
            element.MarkDirtyRepaint();
        }

        public static void SetVector(VisualElement element, Material fallbackMaterial, string name, Vector4 value)
        {
            var material = GetOrCreate(element, fallbackMaterial);
            if (material == null)
                return;
            material.SetVector(name, value);
            element.MarkDirtyRepaint();
        }

        public static void SetColor(VisualElement element, Material fallbackMaterial, string name, Color value)
        {
            var material = GetOrCreate(element, fallbackMaterial);
            if (material == null)
                return;
            material.SetColor(name, value);
            element.MarkDirtyRepaint();
        }

        public static void SetTexture(VisualElement element, Material fallbackMaterial, string name, Texture value)
        {
            var material = GetOrCreate(element, fallbackMaterial);
            if (material == null)
                return;
            material.SetTexture(name, value);
            element.MarkDirtyRepaint();
        }

        // Cloned materials carry this suffix so the helper only ever reuses or destroys the
        // clones it owns — never a material assigned to the element by other code.
        const string k_CloneSuffix = " (UITK binding clone)";

        static Material GetOrCreate(VisualElement element, Material fallbackMaterial)
        {
            // The element's inline unityMaterial IS the per-element store: read our clone back
            // from it, so no static cache is needed and every binding on the element shares it.
            var current = element.style.unityMaterial.value.material;
            if (current != null && current.name.EndsWith(k_CloneSuffix))
                return current;

            // No clone yet, or a material this helper does not own: clone the existing material
            // (to keep its look) or the fallback, and take ownership. Cloning means writes never
            // mutate a material that may be shared with other elements. Assigned exactly once
            // here; later Set calls read this same clone back and mutate it in place.
            var source = current != null ? current : fallbackMaterial;
            if (source == null)
                return null;
            var clone = new Material(source) { name = source.name + k_CloneSuffix };
            element.style.unityMaterial = new MaterialDefinition(clone);

            // Destroy the clone when the element leaves the panel. The native Material is not
            // garbage-collected, so without this every panel rebuild would leak one clone per
            // bound element. Self-unregisters so a re-attached element gets a fresh clone.
            element.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            return clone;
        }

        static void OnDetach(DetachFromPanelEvent evt)
        {
            if (evt.target is not VisualElement element)
                return;
            element.UnregisterCallback<DetachFromPanelEvent>(OnDetach);

            // Only destroy (and clear) a clone this helper owns — never a material set elsewhere.
            var material = element.style.unityMaterial.value.material;
            if (material == null || !material.name.EndsWith(k_CloneSuffix))
                return;

            if (Application.isPlaying)
                Object.Destroy(material);
            else
                Object.DestroyImmediate(material);

            // Clear so a re-attached element renders with the default UI material (not a
            // destroyed one) until the next Set recreates the clone.
            element.style.unityMaterial = StyleKeyword.Null;
        }
    }
}
