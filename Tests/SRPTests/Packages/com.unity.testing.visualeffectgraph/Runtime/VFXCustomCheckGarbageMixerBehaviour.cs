#if VFX_HAS_TIMELINE
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    //Warning: This class is only used for editor test purpose
    public class VFXCustomCheckGarbageMixerBehaviour : PlayableBehaviour
    {
        private static readonly int kForceGarbageID = Shader.PropertyToID("forceGarbage");

        VisualEffect m_ReferenceVFX;
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (m_ReferenceVFX == null)
            {
                m_ReferenceVFX = Resources.FindObjectsOfTypeAll<VisualEffect>().FirstOrDefault();
            }

            if (m_ReferenceVFX.GetBool(kForceGarbageID))
            {
                var garbage = "";
                for (int i = 0; i < 128; ++i)
                    garbage += i + ", ";
                if (garbage.Length < 64)
                    Debug.Log("Won't hit.");
            }
        }

        public override void OnPlayableCreate(Playable playable)
        {
        }

        public override void OnPlayableDestroy(Playable playable)
        {
        }
    }
}
#endif
