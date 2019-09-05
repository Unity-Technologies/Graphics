#if VFX_HAS_TIMELINE
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.VFX;

[Serializable]
class VisualEffectActivationClip : PlayableAsset, ITimelineClipAsset
{
    public VisualEffectActivationBehaviour activationBehavior = new VisualEffectActivationBehaviour();

    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<VisualEffectActivationBehaviour>.Create(graph, activationBehavior);
        /*VisualEffectActivationBehaviour clone =*/ playable.GetBehaviour();
        return playable;
    }
}
#endif
