using System;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class RenderPipelineSettings : ScriptableSingleton<RenderPipelineSettings>
    {
        [SerializeField]
        GlobalFrameSettings m_globalFrameSettings;

        static public GlobalFrameSettings GetGlobalFrameSettings()
        {
            if (m_globalFrameSettings == null)
            {
                m_globalFrameSettings = new GlobalFrameSettings();
            }
            return instance.m_globalFrameSettings;
        }
    }
}
