#if HDRP_HAS_TIMELINE
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.Rendering.HighDefinition
{
    public class WaterPlayableAsset : PlayableAsset
    {
        // This is required to propagate the data at the creation of the track to the playable behavior
        public double clipStart = 0.0;
        public double clipEnd = 0.0;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<WaterSurfacePlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            // Propagate the clip start and end to the playable behavior
            behaviour.clipStart = clipStart;
            behaviour.clipEnd = clipEnd;
            return playable;
        }
    }
}
#endif
