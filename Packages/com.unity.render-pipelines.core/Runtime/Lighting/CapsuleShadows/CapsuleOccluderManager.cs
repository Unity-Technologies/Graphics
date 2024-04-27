using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public class CapsuleOccluderManager
    {
        static private CapsuleOccluderManager s_Instance = null;

        private List<CapsuleOccluder> m_Occluders = null;
        private List<CapsuleOccluder> m_IgnoredOccluders = null;

        public bool maxOccludersReached => m_IgnoredOccluders != null && m_IgnoredOccluders.Count != 0;
        public List<CapsuleOccluder> occluders => m_Occluders;
        public void AddIgnoredOccluder(CapsuleOccluder occluder)
        {
            if (!m_IgnoredOccluders.Contains(occluder))
            {
                m_IgnoredOccluders.Add(occluder);
            }
        }

        public bool IsOccluderIgnored(CapsuleOccluder occluder)
        {
            if (!maxOccludersReached)
            {
                return false;
            }

            return m_IgnoredOccluders.Contains(occluder);
        }

        public bool ContainsIgnoredOccluders(List<CapsuleOccluder> occluders)
        {
            int ignoredCount = 0;
            foreach (CapsuleOccluder occluder in occluders)
            {
                if (IsOccluderIgnored(occluder))
                {
                    ignoredCount++;
                }

                if (ignoredCount != 0)
                {
                    return true;
                }
            }

            return false;
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
            m_IgnoredOccluders = new();
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
