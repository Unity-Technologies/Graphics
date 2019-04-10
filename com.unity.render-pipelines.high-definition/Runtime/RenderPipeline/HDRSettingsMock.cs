using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;



namespace UnityEngine.Experimental.Rendering.HDPipeline
{

    // !!!!!!!!!!!IMPORTANT!!!!!!!!! All of this is a mock, nothing to be used in any way in a final build.

    public class HDROutputSettings
    {
        private static HDROutputSettings s_Instance = new HDROutputSettings();
        public static HDROutputSettings instance { get { return s_Instance; } }

        static private bool ACTIVE = false;
        private static RTHandleSystem.RTHandle m_UITarget = null;


        static public RTHandleSystem.RTHandle uiTarget()
        {
            if(m_UITarget == null)
            {
                m_UITarget = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, useDynamicScale: true, name: "UIBuffer");
            }
            return m_UITarget;
        }

        public static bool active()
        {
            return ACTIVE;
        }

        public static GraphicsFormat format()
        {
            if(ACTIVE)
            {
                return GraphicsFormat.R16G16B16A16_SFloat;
            }
            else
            {
                return GraphicsFormat.R8G8B8A8_UNorm;
            }
        }

        public static ColorGamut gamut()
        {
            // What gamut does Dolby uses? I must assume 2020? Or P3? 
            if (ACTIVE)
            {
                return ColorGamut.HDR10;
            }
            else
            {
                return ColorGamut.sRGB;
            }
        }
    }

}
