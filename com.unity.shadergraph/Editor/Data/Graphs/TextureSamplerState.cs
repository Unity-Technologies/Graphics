using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class TextureSamplerState
    {
        public enum FilterMode
        {
            Linear,
            Point,
            Trilinear
        }

        public enum WrapMode
        {
            Repeat,
            Clamp,
            Mirror,
            MirrorOnce
        }

        public enum Anisotropic
        {
            None,
            x2,
            x4,
            x8,
            x16
        }

        [SerializeField] private FilterMode m_filter = FilterMode.Linear;

        public FilterMode filter
        {
            get { return m_filter; }
            set
            {
                if (m_filter == value)
                    return;

                m_filter = value;
            }
        }

        [SerializeField] private WrapMode m_wrap = WrapMode.Repeat;

        public WrapMode wrap
        {
            get { return m_wrap; }
            set
            {
                if (m_wrap == value)
                    return;

                m_wrap = value;
            }
        }

        [SerializeField] private Anisotropic m_anisotropic = Anisotropic.None;

        public Anisotropic anisotropic
        {
            get { return m_anisotropic; }
            set
            {
                if (m_anisotropic == value)
                    return;

                m_anisotropic = value;
            }
        }

        static string GetAnisoString(Anisotropic aniso)
        {
            switch (aniso)
            {
                default:
                case Anisotropic.None:
                    return String.Empty;
                case Anisotropic.x2:
                    return "_Aniso2";
                case Anisotropic.x4:
                    return "_Aniso4";
                case Anisotropic.x8:
                    return "_Aniso8";
                case Anisotropic.x16:
                    return "_Aniso16";
            }
        }

        public static string BuildSamplerStateName(FilterMode filter, WrapMode wrap, Anisotropic aniso)
        {
            var anisoMode = GetAnisoString(aniso);
            return $"SamplerState_{filter}_{wrap}{anisoMode}";
        }

        public string defaultPropertyName => BuildSamplerStateName(filter, wrap, anisotropic);
    }
}
