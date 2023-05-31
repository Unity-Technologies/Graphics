#if VFX_HAS_TIMELINE
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEditor.VFX.Test
{
    //Warning: This class is only used for editor test purpose
    [TrackColor(0.855f, 0.1f, 0.87f)]
    [TrackClipType(typeof(VFXCustomCheckGarbageAsset))]
    public class VFXCustomCheckGarbageTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<VFXCustomCheckGarbageMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
#endif
