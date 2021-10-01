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

        public double clipStart { get; set; }
        public double clipEnd { get; set; }
        public double easeIn { get; set; }
        public double easeOut { get; set; }

        // Creates the playable that represents the instance of this clip.
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<VisualEffectControlPlayableBehaviour>.Create(graph, template);
            playable.GetBehaviour().clipStart = clipStart;
            playable.GetBehaviour().clipEnd = clipEnd;
            playable.GetBehaviour().easeIn = easeIn;
            playable.GetBehaviour().easeOut = easeOut;
            return playable;
        }
    }
}
#endif
