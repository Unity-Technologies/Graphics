using System;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class RenderPipelineSettings : Singleton<RenderPipelineSettings>
    {
        [SerializeField]
        GlobalFrameSettings m_globalFrameSettings;

        static public GlobalFrameSettings GetGlobalFrameSettings()
        {
            if (instance.m_globalFrameSettings == null)
            {
                instance.m_globalFrameSettings = new GlobalFrameSettings();
            }

            return instance.m_globalFrameSettings;
        }
    }
}
