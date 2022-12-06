#if HDRP_HAS_TIMELINE
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.Rendering.HighDefinition
{
    [TrackColor(0.0f, 0.6f, 0.8f)]
    [TrackClipType(typeof(WaterPlayableAsset))]
    [TrackBindingType(typeof(WaterSurface))]
    public class WaterSurfaceTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (var clip in GetClips())
            {
                if (clip.asset is WaterPlayableAsset playableAsset)
                {
                    playableAsset.clipStart = clip.start;
                    playableAsset.clipEnd = clip.end;
                }
#if UNITY_EDITOR
                else
                {
                    Debug.LogErrorFormat("Unexpected clip type : {0} in timeline '{1}'", clip, UnityEditor.AssetDatabase.GetAssetPath(timelineAsset));
                }
#endif
            }
            return ScriptPlayable<WaterSurfaceBehaviour>.Create(graph, inputCount);
        }

#if UNITY_EDITOR
        protected override void OnCreateClip(TimelineClip clip)
        {
            base.OnCreateClip(clip);
            if (clip.asset is WaterPlayableAsset playableAsset)
            {
                playableAsset.clipStart = clip.start;
                playableAsset.clipEnd = clip.end;
            }
            else
            {
                throw new InvalidOperationException("Unexpected clip added : " + clip.asset);
            }
        }
#endif
    }
}
#endif
