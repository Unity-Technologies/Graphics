using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.LWRP
{
    public struct BrunetonParameters
    {
        public float m_mieScattering;
        public float m_raleightScattering;
        public float m_ozoneDensity;
        public float m_phase;
        public float m_fogAmount;
        public float m_sunSize;
        public float m_sunEdge;
        public float m_exposure;

        static public BrunetonParameters MakeDefault()
        {
            BrunetonParameters ret;
            ret.m_mieScattering = 1.0f;
            ret.m_raleightScattering = 1.0f;
            ret.m_ozoneDensity = 1.0f;
            ret.m_phase = 0.8f;
            ret.m_fogAmount = 1.0f;
            ret.m_sunSize = 1.0f;
            ret.m_sunEdge = 1.0f;
            ret.m_exposure = 10.0f;
            return ret;
        }

        /// This ignores m_fogAmount, m_sunSize, m_sunEdge as changing them do not trigger a recomputation of the look-up textures.
        static public bool IsEquals(ref BrunetonParameters a, ref BrunetonParameters b)
        {
            return a.m_mieScattering == b.m_mieScattering
                && a.m_raleightScattering == b.m_raleightScattering
                && a.m_ozoneDensity == b.m_ozoneDensity
                && a.m_phase == b.m_phase
                && a.m_sunSize == b.m_sunSize; // sunSize indirectly scales SunAngularRadius
        }
    };
}