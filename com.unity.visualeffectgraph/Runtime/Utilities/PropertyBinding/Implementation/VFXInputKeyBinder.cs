using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Input Key Press Binder")]
    [VFXBinder("Input/Key")]
    class VFXInputKeyBinder : VFXBinderBase
    {
        public string KeyProperty { get { return (string)m_KeyProperty; } set { m_KeyProperty = value; } }

        [VFXPropertyBinding("System.Boolean"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_KeyParameter")]
        protected ExposedProperty m_KeyProperty = "KeyDown";

        public string KeySmoothProperty { get { return (string)m_KeySmoothProperty; } set { m_KeySmoothProperty = value; } }

        [VFXPropertyBinding("System.Single"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_KeySmoothParameter")]
        protected ExposedProperty m_KeySmoothProperty = "KeySmooth";

        public KeyCode Key = KeyCode.Space;
        public float SmoothSpeed = 2.0f;
        public bool UseKeySmooth = true;

#if ENABLE_LEGACY_INPUT_MANAGER
        float m_CachedSmoothValue = 0.0f;
#endif

        public override bool IsValid(VisualEffect component)
        {
            return component.HasBool(m_KeyProperty) && (UseKeySmooth ? component.HasFloat(m_KeySmoothProperty) : true);
        }

        private void Start()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (UseKeySmooth)
            {
                m_CachedSmoothValue = Input.GetKeyDown(Key) ? 1.0f : 0.0f;
            }
#endif
        }

        public override void UpdateBinding(VisualEffect component)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            bool press = Input.GetKey(Key);

            component.SetBool(m_KeyProperty, press);
            if (UseKeySmooth)
            {
                m_CachedSmoothValue += SmoothSpeed * Time.deltaTime * (press ? 1.0f : -1.0f);
                m_CachedSmoothValue = Mathf.Clamp01(m_CachedSmoothValue);
                component.SetFloat(m_KeySmoothProperty, m_CachedSmoothValue);
            }
#endif
        }

        public override string ToString()
        {
            return string.Format("Key: '{0}' -> {1}", m_KeySmoothProperty, Key.ToString());
        }
    }
}
