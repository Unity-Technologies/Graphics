using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    class CapsuleOccluderManager
    {
        static private CapsuleOccluderManager s_Instance = null;

        private List<CapsuleOccluder> m_Occluders = null;

        internal List<CapsuleOccluder> occluders
        {
            get
            {
                return m_Occluders;
            }
        }

        public static CapsuleOccluderManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new CapsuleOccluderManager();
                }
                return s_Instance;
            }
        }

        private CapsuleOccluderManager()
        {
            m_Occluders = new List<CapsuleOccluder>();
        }

        public void RegisterCapsule(CapsuleOccluder occluder)
        {
            m_Occluders.Add(occluder);
        }

        public void DeregisterCapsule(CapsuleOccluder occluder)
        {
            m_Occluders.Remove(occluder);
        }
    }
}
