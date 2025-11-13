using System;

namespace UnityEngine.Rendering.Universal
{
    internal abstract class PostProcessPass : ScriptableRenderPass, IDisposable
    {
        VolumeStack m_VolumeStackOverride;

        public VolumeStack volumeStack
        {
            get{
                if (m_VolumeStackOverride == null)
                {
                    return VolumeManager.instance.stack;
                }
                else
                {
                    return m_VolumeStackOverride;
                }
            }
        }

        public VolumeStack volumeStackOverride
        {
            set { m_VolumeStackOverride = value; }
        }

        public abstract void Dispose();
    }
}
