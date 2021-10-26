#if VFX_HAS_TIMELINE
using System;
using System.Linq;
using System.Collections.Generic;
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
                clip.displayName = "VFX";
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

        private static double InverseLerp(double a, double b, double value)
        {
            return (value - a) / (b - a);
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            base.DrawBackground(clip, region);
            var playable = clip.asset as VisualEffectControlPlayableAsset;
            if (playable.clipEvents == null || playable.singleEvents == null)
                return;

            var iconSize = new Vector2(8, 8);

            var clipEvents = VFXTimeSpaceHelper.GetEventNormalizedSpace(VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart, playable, true);
            using (var iterator = clipEvents.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    var enter = iterator.Current;
                    iterator.MoveNext();
                    var exit = iterator.Current;

                    var relativeStart = InverseLerp(region.startTime, region.endTime, enter.time);
                    var relativeStop = InverseLerp(region.startTime, region.endTime, exit.time);

                    var startRange = region.position.width * Mathf.Clamp01((float)relativeStart);
                    var endRange = region.position.width * Mathf.Clamp01((float)relativeStop);

                    var rect = new Rect(
                        region.position.x + startRange,
                        region.position.y + 0.14f * region.position.height,
                        endRange - startRange,
                        region.position.y + 0.19f * region.position.height);

                    float hue = 0.5f;
                    var color = Color.HSVToRGB(hue, 1.0f, 1.0f);
                    color.a = 0.5f;
                    EditorGUI.DrawRect(rect, color);
                }
            }

            var singleEvents = VFXTimeSpaceHelper.GetEventNormalizedSpace(VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart, playable, false);
            var allEvents = clipEvents.Concat(singleEvents);
            var clipEventsCount = clipEvents.Count();
            int index = 0;
            foreach (var itEvent in allEvents)
            {
                var relativeTime = InverseLerp(region.startTime, region.endTime, itEvent.time);
                var center = new Vector2(region.position.position.x + region.position.width * (float)relativeTime,
                    region.position.position.y + region.position.height * 0.5f);

                float color = index < clipEventsCount ? 0.5f : 0.3f;
                var eventRect = new Rect(center - iconSize * new Vector2(1.0f, -0.5f), iconSize);
                EditorGUI.DrawRect(eventRect, Color.HSVToRGB(color, 1.0f, 1.0f));

                var textRect = new Rect(center + new Vector2(2.0f, 0), iconSize);
                ShadowLabel(textRect,
                    new GUIContent((string)itEvent.name),
                    fontStyle,
                    Color.HSVToRGB(color, 1.0f, 1.0f),
                    Color.HSVToRGB(color, 1.0f, 0.1f));

                index++;
            }
        }
    }
}
#endif
