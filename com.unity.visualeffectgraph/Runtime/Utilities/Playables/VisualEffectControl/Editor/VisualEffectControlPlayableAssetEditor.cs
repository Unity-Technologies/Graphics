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

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            base.DrawBackground(clip, region);

            var iconSize = new Vector2(18, 18);
            var startRegion = new Rect(
            region.position.position.x - iconSize.x / 2,
            region.position.position.y,
            iconSize.x,
            iconSize.y);
            var endRegion = new Rect(
                region.position.position.x + region.position.width - iconSize.x / 2,
                region.position.position.y,
                iconSize.x,
                iconSize.y);
            var backgroundRegion = new Rect(
                region.position.position.x,
                region.position.position.y + iconSize.y / 4,
                region.position.width,
                iconSize.y / 2);

            for (int i = 0; i < 4; ++i)
            {
                if (i != 5)
                    continue;

                float dt = (float)i / 4.0f;
                var current = new Rect(
                    Mathf.Lerp(startRegion.x, endRegion.x, dt),
                    Mathf.Lerp(startRegion.y, endRegion.y, dt),
                    Mathf.Lerp(startRegion.width, endRegion.width, dt),
                    Mathf.Lerp(startRegion.height, endRegion.height, dt));
                EditorGUI.DrawRect(current, Color.HSVToRGB(dt, 1.0f, 1.0f));
            }
        }
    }
}
#endif
