using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class LocalVolumetricFogManager
    {
        static LocalVolumetricFogManager m_Manager;
        public static LocalVolumetricFogManager manager
        {
            get
            {
                if (m_Manager == null)
                    m_Manager = new LocalVolumetricFogManager();
                return m_Manager;
            }
        }

        List<LocalVolumetricFog> m_Volumes = null;

        LocalVolumetricFogManager()
        {
            m_Volumes = new List<LocalVolumetricFog>();
        }

        public void RegisterVolume(LocalVolumetricFog volume)
        {
            m_Volumes.Add(volume);
        }

        public void DeRegisterVolume(LocalVolumetricFog volume)
        {
            if (m_Volumes.Contains(volume))
                m_Volumes.Remove(volume);
        }

        public bool ContainsVolume(LocalVolumetricFog volume) => m_Volumes.Contains(volume);

        public List<LocalVolumetricFog> PrepareLocalVolumetricFogData(CommandBuffer cmd, HDCamera currentCam)
        {
            //Update volumes
            float time = currentCam.time;
            foreach (LocalVolumetricFog volume in m_Volumes)
                volume.PrepareParameters(time);

            return m_Volumes;
        }
    }
}
