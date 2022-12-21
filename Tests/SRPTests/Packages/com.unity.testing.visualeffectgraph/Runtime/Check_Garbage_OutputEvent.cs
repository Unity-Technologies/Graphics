using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.VFX.Test
{
    public class Check_Garbage_OutputEvent : MonoBehaviour
    {
        public Light m_Light;
        public VisualEffect m_VFX;

        void Start()
        {
            m_VFX.outputEventReceived += OutputEventReceived;
        }

        private static readonly int kForceGarbageID = Shader.PropertyToID("forceGarbage");
        private static readonly int kColorID = Shader.PropertyToID("color");
        private static readonly int kCustomEventID = Shader.PropertyToID("custom_output_event");
        private void OutputEventReceived(VFXOutputEventArgs obj)
        {
            if (m_VFX.GetBool(kForceGarbageID))
            {
                var garbage = "";
                for (int i = 0; i < 1024; ++i)
                    garbage += i + ", ";
                if (garbage.Length < 512)
                    Debug.Log("Won't hit.");
            }
            else
            {
                if (obj.nameId == kCustomEventID)
                {
                    var color = obj.eventAttribute.GetVector3(kColorID);
                    m_Light.color = new Color(color.x, color.y, color.z);
                }
            }
        }
    }
}
