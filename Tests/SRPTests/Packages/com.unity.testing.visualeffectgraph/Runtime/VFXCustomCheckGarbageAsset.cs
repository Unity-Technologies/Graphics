#if VFX_HAS_TIMELINE
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEditor.VFX.Test
{
    //Warning: This class is only used for editor test purpose
    [Serializable]
    public class VFXCustomCheckGarbageAsset : PlayableAsset, ITimelineClipAsset
    {
        public VFXCustomCheckGarbageBehaviour template = new VFXCustomCheckGarbageBehaviour();

        public ClipCaps clipCaps
        {
            get { return ClipCaps.Extrapolation | ClipCaps.Blending; }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<VFXCustomCheckGarbageBehaviour>.Create(graph, template);
        }
    }
}
#endif
