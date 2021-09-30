#if VFX_HAS_TIMELINE
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.VFX
{
    // Represents the serialized data for a clip on the TextTrack
    [Serializable]
    public class VisualEffectControlPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        //[NoFoldOut]
        [NotKeyable] // NotKeyable used to prevent Timeline from making fields available for animation.
        public VisualEffectControlPlayableBehaviour template = new VisualEffectControlPlayableBehaviour();

        // Implementation of ITimelineClipAsset. This specifies the capabilities of this timeline clip inside the editor.
        public ClipCaps clipCaps
        {
            get { return ClipCaps.Blending; }
        }

        // Creates the playable that represents the instance of this clip.
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            // Using a template will clone the serialized values
            return ScriptPlayable<VisualEffectControlPlayableBehaviour>.Create(graph, template);
        }
    }
}
#endif
