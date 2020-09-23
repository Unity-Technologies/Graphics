#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
    #define USE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif

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

#if USE_INPUT_SYSTEM
        public Key InputSystemKey  = InputSystem.Key.Space;
#else
        public KeyCode Key = KeyCode.Space;
#endif
        public float SmoothSpeed = 2.0f;
        public bool UseKeySmooth = true;

        float m_CachedSmoothValue = 0.0f;

        public override bool IsValid(VisualEffect component)
        {
            return component.HasBool(m_KeyProperty) && (UseKeySmooth ? component.HasFloat(m_KeySmoothProperty) : true);
        }

        private void Start()
        {
            if (UseKeySmooth)
            {
#if USE_INPUT_SYSTEM
                if (Keyboard.current != null)
                    m_CachedSmoothValue = Keyboard.current[InputSystemKey].isPressed ? 1.0f : 0.0f;
#else
                m_CachedSmoothValue = Input.GetKeyDown(Key) ? 1.0f : 0.0f;
#endif
            }
        }

        public override void UpdateBinding(VisualEffect component)
        {
#if USE_INPUT_SYSTEM
            bool press = Keyboard.current != null ? Keyboard.current[InputSystemKey].isPressed : false;
#else
            bool press = Input.GetKey(Key);
#endif
            component.SetBool(m_KeyProperty, press);
            if (UseKeySmooth)
            {
                m_CachedSmoothValue += SmoothSpeed * Time.deltaTime * (press ? 1.0f : -1.0f);
                m_CachedSmoothValue = Mathf.Clamp01(m_CachedSmoothValue);
                component.SetFloat(m_KeySmoothProperty, m_CachedSmoothValue);
            }
        }

        public override string ToString()
        {
#if USE_INPUT_SYSTEM
            return string.Format("Key: '{0}' -> {1}", m_KeySmoothProperty, InputSystemKey.ToString());
#else
            return string.Format("Key: '{0}' -> {1}", m_KeySmoothProperty, Key.ToString());
#endif
        }
    }
}
