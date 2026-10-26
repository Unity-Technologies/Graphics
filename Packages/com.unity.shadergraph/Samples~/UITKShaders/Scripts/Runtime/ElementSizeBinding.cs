using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Shaders.UITK.Sample
{
    /// <summary>
    /// Manipulator that writes the target <see cref="VisualElement"/>'s content rect size into
    /// the <c>_RectSize</c> Vector4 shader property (width, height, 0, 0) on every layout change.
    /// Attach it to elements whose shader needs to know the rendered size to remain
    /// aspect-ratio-correct (rounded rectangles, fixed-radius corners, etc.).
    /// </summary>
    public sealed class ElementSizeBinding : Manipulator
    {
        const string k_RectSizeProperty = "_RectSize";

        readonly Material m_FallbackMaterial;

        public ElementSizeBinding() : this(null) {}

        public ElementSizeBinding(Material fallbackMaterial)
        {
            m_FallbackMaterial = fallbackMaterial;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            UpdateSize();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent evt) => UpdateSize();

        void UpdateSize()
        {
            var rect = target.contentRect;
            UIShaderBinding.SetVector(target, m_FallbackMaterial, k_RectSizeProperty,
                new Vector4(rect.width, rect.height, 0f, 0f));
        }
    }
}
