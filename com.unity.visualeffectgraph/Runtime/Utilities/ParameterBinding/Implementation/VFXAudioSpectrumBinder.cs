#if VFX_HAS_AUDIO
using System;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX Audio Spectrum Binder")]
    [VFXBinder("Audio/Audio Spectrum to AttributeMap")]
    public class VFXAudioSpectrumBinder : VFXBinderBase
    {
        public enum AudioSourceMode
        {
            AudioSource,
            AudioListener
        }

        public string CountParameter { get { return (string)m_CountParameter; } set { m_CountParameter = value; } }
        [VFXParameterBinding("System.UInt32"), SerializeField]
        protected ExposedParameter m_CountParameter = "Count";

        public string TextureParameter { get { return (string)m_TextureParameter; } set { m_TextureParameter = value; } }
        [VFXParameterBinding("UnityEngine.Texture2D"), SerializeField]
        protected ExposedParameter m_TextureParameter = "SpectrumTexture";

        public FFTWindow FFTWindow = FFTWindow.BlackmanHarris;
        public uint Samples = 64;
        public AudioSourceMode Mode = AudioSourceMode.AudioSource;
        public AudioSource AudioSource;

        private Texture2D m_Texture;
        private float[] m_AudioCache;
        private Color[] m_ColorCache;

        public override bool IsValid(VisualEffect component)
        {
            bool mode = (Mode == AudioSourceMode.AudioSource ? AudioSource != null : true);
            bool texture = component.HasTexture(TextureParameter);
            bool count = component.HasUInt(CountParameter);

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
            component.SetTexture(TextureParameter, m_Texture);
            component.SetUInt(CountParameter, Samples);
        }

        public override string ToString()
        {
            return string.Format("Audio Spectrum : '{0} samples' -> {1}", m_CountParameter, (Mode == AudioSourceMode.AudioSource ? "AudioSource" : "AudioListener"));
        }
    }
}
#endif
