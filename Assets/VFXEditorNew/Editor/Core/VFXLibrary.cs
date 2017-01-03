using System;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    static class VFXLibrary
    {
        public static VFXContextDesc GetContext(string id)      { LoadIfNeeded(); return m_ContextDescs[id]; }
        public static IEnumerable<VFXContextDesc> GetContexts() { LoadIfNeeded(); return m_ContextDescs.Values; }

        public static void LoadIfNeeded()
        {
            if (m_Loaded)
                return;

            lock (m_Lock)
            {
                if (!m_Loaded)
                    Load();
            }
        }
        
        public static void Load()
        {
            lock(m_Lock)
            {
                LoadContextDescs();
                m_Loaded = true;
            }
        }

        private static void LoadContextDescs()
        {
            m_ContextDescs = new Dictionary<string, VFXContextDesc>(); // Copy On Write
        }

        private static volatile Dictionary<string, VFXContextDesc> m_ContextDescs;

        private static Object m_Lock = new Object();
        private static volatile bool m_Loaded = false;
    }
}
