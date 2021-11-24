#if VFX_HAS_TIMELINE
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [CustomTimelineEditor(typeof(VisualEffectControlTrack))]
    class VisualEffectControlTrackEditor : TrackEditor
    {
        public override TrackDrawOptions GetTrackOptions(TrackAsset track, UnityEngine.Object binding)
        {
            var options = base.GetTrackOptions(track, binding);
            if (VisualEffectControlErrorHelper.instance.AnyError())
            {
                var conflicts = VisualEffectControlErrorHelper.instance.GetConflictingControlTrack()
                    .SelectMany(o => o)
                    .Any(o =>
                    {
                        return o.GetTrack() == track;
                    });

                var scrubbing = VisualEffectControlErrorHelper.instance.GetMaxScrubbingWarnings()
                    .Any(o =>
                    {
                        return o.controller.GetTrack() == track;
                    });

                if (conflicts || scrubbing)
                {
                    var baseMessage = new StringBuilder("This track is generating a warning about ");

                    if (conflicts && scrubbing)
                    {
                        baseMessage.Append("a conflict and maximum scrubbing time reached ");
                    }

                    if (conflicts)
                        baseMessage.Append("conflict(s)");

                    if (scrubbing)
                    {
                        if (conflicts)
                            baseMessage.Append(" and ");
                        baseMessage.Append("maximum scrubbing time reached");
                    }

                    baseMessage.Append(".");
                    baseMessage.AppendLine();
                    baseMessage.Append("More information in overlay in scene view.");
                    options.errorText = L10n.Tr(baseMessage.ToString());
                }
            }
            return options;
        }
    }

    [CustomTimelineEditor(typeof(VisualEffectControlClip))]
    class VisualEffectControlClipEditor : ClipEditor
    {
        public override void OnClipChanged(TimelineClip clip)
        {
            var behavior = clip.asset as VisualEffectControlClip;
            if (behavior != null)
                clip.displayName = " ";
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

        private static double InverseLerp(double a, double b, double value)
        {
            return (value - a) / (b - a);
        }

        static class Content
        {
            public static GUIContent scrubbingDisabled = new GUIContent("Scrubbing Disabled");
        }

        static class Style
        {
            public static readonly GUIStyle noneStyle = GUIStyle.none; //Remove in the end
            public static GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
            public static GUIStyle scrubbingDisabled = GUI.skin.GetStyle("Label");
            public static readonly Color kGlobalBackground = new Color32(81, 86, 94, 255);
            public static readonly float kScrubbingBarHeight = 14.0f;
            public static readonly Color kScrubbingBackgroundColor = new Color32(64, 68, 74, 255);

            static Style()
            {
                centeredStyle.alignment = TextAnchor.UpperCenter;

                scrubbingDisabled.alignment = TextAnchor.UpperCenter;
                scrubbingDisabled.fontStyle = FontStyle.Bold;
                scrubbingDisabled.fontSize = 12;
            }
        }

        struct ClipEventBar
        {
            public float start;
            public float end;
            public uint rowIndex;
        }
        List<ClipEventBar> m_ClipEventBars = new List<ClipEventBar>();

        bool AvailableEmplacement(ClipEventBar current, IEnumerable<ClipEventBar> currentBars)
        {
            var barInRow = currentBars.Where(o => o.rowIndex == current.rowIndex);
            return !currentBars.Any(o =>
            {
                if (o.rowIndex == current.rowIndex)
                {
                    if (Mathf.Min(o.start, current.start) <= Mathf.Min(o.end, current.end))
                        return true;
                }
                return false;
            });
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            base.DrawBackground(clip, region);
            var playable = clip.asset as VisualEffectControlClip;
            if (playable.clipEvents == null || playable.singleEvents == null)
                return;

            //Draw custom background
            var currentRect = region.position;
            EditorGUI.DrawRect(currentRect, Style.kGlobalBackground);

            if (!playable.scrubbing)
            {
                var scrubbingRect = new Rect(
                    currentRect.x,
                    currentRect.height - Style.kScrubbingBarHeight,
                    currentRect.width,
                    Style.kScrubbingBarHeight);
                EditorGUI.DrawRect(scrubbingRect, Style.kScrubbingBackgroundColor);

                currentRect.height -= Style.kScrubbingBarHeight;

                GUI.Label(scrubbingRect, Content.scrubbingDisabled, Style.scrubbingDisabled);
            }

            var clipEvents = VFXTimeSpaceHelper.GetEventNormalizedSpace(VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart, playable, true);
            var singleEvents = VFXTimeSpaceHelper.GetEventNormalizedSpace(VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart, playable, false);
            var allEvents = clipEvents.Concat(singleEvents);
            var clipEventsCount = clipEvents.Count();
            int index = 0;
            var iconSize = new Vector2(8, 8);
            var eventNameHeight = 14.0f;
            foreach (var itEvent in allEvents)
            {
                var relativeTime = InverseLerp(region.startTime, region.endTime, itEvent.time);
                var center = new Vector2(currentRect.position.x + currentRect.width * (float)relativeTime,
                    currentRect.height - eventNameHeight);

                float color = index < clipEventsCount ? 0.5f : 0.3f;
                var eventRect = new Rect(center - iconSize * new Vector2(1.0f, -0.5f), iconSize);
                EditorGUI.DrawRect(eventRect, Color.HSVToRGB(color, 1.0f, 1.0f));

                var textRect = new Rect(center + new Vector2(2.0f, 0), iconSize);
                ShadowLabel(textRect,
                    new GUIContent((string)itEvent.name),
                    Style.noneStyle,
                    Color.HSVToRGB(color, 1.0f, 1.0f),
                    Color.HSVToRGB(color, 1.0f, 0.1f));

                index++;
            }
            currentRect.height -= eventNameHeight; //TODOPAUL

            //TODO Comment
            m_ClipEventBars.Clear();
            uint rowCount = 1u;
            using (var iterator = clipEvents.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    var enter = iterator.Current;
                    iterator.MoveNext();
                    var exit = iterator.Current;

                    var candidate = new ClipEventBar()
                    {
                        start = (float)enter.time,
                        end = (float)exit.time,
                        rowIndex = 0u
                    };

                    while (!AvailableEmplacement(candidate, m_ClipEventBars))
                        candidate.rowIndex++;

                    if (candidate.rowIndex >= rowCount)
                        rowCount = candidate.rowIndex + 1;

                    m_ClipEventBars.Add(candidate);
                }
            }

            var padding = 2f;
            var rowHeight = currentRect.height / (float)rowCount;
            foreach (var bar in m_ClipEventBars)
            {
                var relativeStart = InverseLerp(region.startTime, region.endTime, bar.start);
                var relativeStop = InverseLerp(region.startTime, region.endTime, bar.end);

                var startRange = region.position.width * Mathf.Clamp01((float)relativeStart);
                var endRange = region.position.width * Mathf.Clamp01((float)relativeStop);

                var rect = new Rect(
                    currentRect.x + startRange,
                    currentRect.y + rowHeight * (rowCount - bar.rowIndex - 1) + padding,
                    endRange - startRange,
                    rowHeight - padding);

                float hue = 0.5f;
                var color = Color.HSVToRGB(hue, 1.0f, 1.0f);
                color.a = 0.5f;
                EditorGUI.DrawRect(rect, color);
            }

        }
    }
}
#endif
