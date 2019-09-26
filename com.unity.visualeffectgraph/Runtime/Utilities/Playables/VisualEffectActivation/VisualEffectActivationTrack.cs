#if VFX_HAS_TIMELINE
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.VFX;

[TrackColor(0.5990566f, 0.9038978f, 1f)]
[TrackClipType(typeof(VisualEffectActivationClip))]
[TrackBindingType(typeof(VisualEffect))]
class VisualEffectActivationTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<VisualEffectActivationMixerBehaviour>.Create(graph, inputCount);
    }
}
#endif
