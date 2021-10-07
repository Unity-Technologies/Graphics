#if VFX_HAS_TIMELINE
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{

    [CustomTimelineEditor(typeof(VisualEffectControlPlayableAsset))]
    class VisualEffectControlPlayableAssetEditor : ClipEditor
    {
        public override void OnClipChanged(TimelineClip clip)
        {
            var behavior = clip.asset as VisualEffectControlPlayableAsset;
            if (behavior != null)
            {
                clip.displayName = "VFX (Play/Stop)"; //Can be customized with event name
            }
        }
    }
}
#endif
