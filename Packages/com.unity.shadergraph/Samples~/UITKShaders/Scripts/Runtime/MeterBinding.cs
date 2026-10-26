using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Shaders.UITK.Sample
{
    /// <summary>
    /// Manipulator that exposes a single <see cref="Value"/> property in [0, 1] and writes it
    /// into the <c>_MeterValue</c> float shader property. Drive it from game code (health,
    /// shield, mana) by setting <see cref="Value"/>.
    /// </summary>
    public sealed class MeterBinding : Manipulator
    {
        const string k_MeterValueProperty = "_MeterValue";

        readonly Material m_FallbackMaterial;
        float m_Value;

        public MeterBinding() : this(null) {}

        public MeterBinding(Material fallbackMaterial)
        {
            m_FallbackMaterial = fallbackMaterial;
        }

        public float Value
        {
            get => m_Value;
            set
            {
                m_Value = Mathf.Clamp01(value);
                if (target != null)
                    Write();
            }
        }

        protected override void RegisterCallbacksOnTarget()
        {
            Write();
        }

        protected override void UnregisterCallbacksFromTarget() {}

        void Write()
        {
            UIShaderBinding.SetFloat(target, m_FallbackMaterial, k_MeterValueProperty, m_Value);
        }
    }
}
