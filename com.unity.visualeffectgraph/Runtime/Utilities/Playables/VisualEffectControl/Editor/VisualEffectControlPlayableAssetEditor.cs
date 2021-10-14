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
                clip.displayName = "VFX";
            }
        }

        public static void ShadowLabel(Rect rect, GUIContent content, GUIStyle style, Color textColor, Color shadowColor)
        {
            var shadowRect = rect;
            shadowRect.xMin += 2.0f;
            shadowRect.yMin += 2.0f;
            style.normal.textColor = shadowColor;
            style.hover.textColor = shadowColor;
            GUI.Label(shadowRect, content, style);

            style.normal.textColor = textColor;
            style.hover.textColor = textColor;
            GUI.Label(rect, content, style);
        }

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            return base.GetClipOptions(clip);
        }


        private GUIStyle fontStyle = GUIStyle.none;

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            base.DrawBackground(clip, region);

#if TODOPAUL
            var iconSize = new Vector2(16, 16); //Should be relative ?
            var playable = clip.asset as VisualEffectControlPlayableAsset;

            if (playable.events == null)
                return;

            foreach (var itEvent in playable.GetVirtualEvents())
            {
                var dt = Mathf.InverseLerp((float)region.startTime, (float)region.endTime, (float)itEvent.time);
                if (dt != Mathf.Clamp01(dt))
                    continue;

                var center = new Vector2(region.position.position.x + region.position.width * dt,
                    region.position.position.y + region.position.height * 0.5f);

                float color = 0.3f;
                var eventRect = new Rect(center - iconSize * new Vector2(1.0f, 0.0f), iconSize);
                EditorGUI.DrawRect(eventRect, Color.HSVToRGB(color, 1.0f, 1.0f));

                var textRect = new Rect(center + new Vector2(2, 0), iconSize);

                ShadowLabel(textRect,
                    new GUIContent(itEvent.name),
                    fontStyle,
                    Color.HSVToRGB(color, 1.0f, 1.0f),
                    Color.HSVToRGB(color, 1.0f, 0.1f));
            }
#endif
        }
    }
}
#endif
