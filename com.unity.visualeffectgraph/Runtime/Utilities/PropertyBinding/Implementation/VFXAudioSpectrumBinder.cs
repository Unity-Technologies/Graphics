#if VFX_HAS_AUDIO
using System;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Audio Spectrum Binder")]
    [VFXBinder("Audio/Audio Spectrum to AttributeMap")]
    class VFXAudioSpectrumBinder : VFXBinderBase
    {
        public enum AudioSourceMode
        {
            AudioSource,
            AudioListener
        }

        public string CountProperty { get { return (string)m_CountProperty; } set { m_CountProperty = value; } }
        [VFXPropertyBinding("System.UInt32"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_CountParameter")]
        protected ExposedProperty m_CountProperty = "Count";

        public string TextureProperty { get { return (string)m_TextureProperty; } set { m_TextureProperty = value; } }
        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_TextureParameter")]
        protected ExposedProperty m_TextureProperty = "SpectrumTexture";

        public FFTWindow FFTWindow = FFTWindow.BlackmanHarris;
        public uint Samples = 64;
        public AudioSourceMode Mode = AudioSourceMode.AudioSource;
        public AudioSource AudioSource = null;

        private Texture2D m_Texture;
        private float[] m_AudioCache;
        private Color[] m_ColorCache;

        public override bool IsValid(VisualEffect component)
        {
            bool mode = (Mode == AudioSourceMode.AudioSource ? AudioSource != null : true);
            bool texture = component.HasTexture(TextureProperty);
            bool count = component.HasUInt(CountProperty);

            return mode && texture && count;
        }

        void UpdateTexture()
        {
            if (m_Texture == null || m_Texture.width != Samples)
            {
                m_Texture = new Texture2D((int)Samples, 1, TextureFormat.RFloat, false);
                m_AudioCache = new float[Samples];
                m_ColorCache = new Color[Samples];
            }

            if (Mode == AudioSourceMode.AudioListener)
                AudioListener.GetSpectrumData(m_AudioCache, 0, FFTWindow);
            else if (Mode == AudioSourceMode.AudioSource)
                AudioSource.GetSpectrumData(m_AudioCache, 0, FFTWindow);
            else throw new NotImplementedException();

            for (int i = 0; i < Samples; i++)
            {
                m_ColorCache[i] = new Color(m_AudioCache[i], 0, 0, 0);
            }

            m_Texture.SetPixels(m_ColorCache);
            m_Texture.name = "AudioSpectrum" + Samples;
            m_Texture.Apply();
        }

        public override void UpdateBinding(VisualEffect component)
        {
            UpdateTexture();
            component.SetTexture(TextureProperty, m_Texture);
            component.SetUInt(CountProperty, Samples);
        }

        public override string ToString()
        {
            return string.Format("Audio Spectrum : '{0} samples' -> {1}", m_CountProperty, (Mode == AudioSourceMode.AudioSource ? "AudioSource" : "AudioListener"));
        }
    }
}
#endif
