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

        public static void ShadowLabel(Rect rect, GUIContent content, GUIStyle textStyle, GUIStyle shadowStyle)
        {
            var shadowRect = rect;
            shadowRect.xMin += 2.0f;
            shadowRect.yMin += 2.0f;
            shadowRect.width += 2.0f;
            shadowRect.height += 2.0f;
            GUI.Label(shadowRect, content, shadowStyle);
            GUI.Label(rect, content, textStyle);
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
            public static GUIStyle centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            public static GUIStyle eventLeftAlignStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            public static GUIStyle eventRightAlignStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            public static GUIStyle eventLeftAlignShadowStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            public static GUIStyle eventRightAlignShadowStyle = new GUIStyle(GUI.skin.GetStyle("Label"));

            public static GUIStyle scrubbingDisabled = new GUIStyle(GUI.skin.GetStyle("Label"));
            public static readonly Color kGlobalBackground = new Color32(81, 86, 94, 255);
            public static readonly float kScrubbingBarHeight = 14.0f;
            public static readonly Color kScrubbingBackgroundColor = new Color32(64, 68, 74, 255);

            static Style()
            {
                centeredStyle.alignment = TextAnchor.UpperCenter;
                eventLeftAlignStyle.alignment = eventLeftAlignShadowStyle.alignment = TextAnchor.MiddleLeft;
                eventRightAlignStyle.alignment = eventRightAlignShadowStyle.alignment = TextAnchor.MiddleRight;

                var shadowColor = Color.black;
                eventLeftAlignShadowStyle.normal.textColor = shadowColor;
                eventLeftAlignShadowStyle.hover.textColor = shadowColor;
                eventRightAlignShadowStyle.normal.textColor = shadowColor;
                eventRightAlignShadowStyle.hover.textColor = shadowColor;

                scrubbingDisabled.alignment = TextAnchor.UpperCenter;
                scrubbingDisabled.fontStyle = FontStyle.Bold;
                scrubbingDisabled.fontSize = 12;
            }
        }

        struct ClipEventBar
        {
            public Color color;
            public float start;
            public float end;
            public uint rowIndex;
        }
        List<ClipEventBar> m_ClipEventBars = new List<ClipEventBar>();

        struct EventAreaItem
        {
            static GUIStyle GetTextGUIStyle(IconType type)
            {
                if (type != IconType.ClipExit)
                    return Style.eventLeftAlignStyle;
                return Style.eventRightAlignStyle;
            }

            public GUIStyle textStyle
            {
                get
                {
                    return GetTextGUIStyle(currentType);
                }
            }

            static GUIStyle GetTextShadowGUIStyle(IconType type)
            {
                if (type != IconType.ClipExit)
                    return Style.eventLeftAlignShadowStyle;
                return Style.eventRightAlignShadowStyle;
            }

            public GUIStyle textShadowStyle
            {
                get
                {
                    return GetTextShadowGUIStyle(currentType);
                }
            }

            public EventAreaItem(Rect iconDrawRect, IconType currentType, string text)
            {
                this.content = new GUIContent(text);
                this.color = Color.white;
                var textWidth = ComputeTextWidth(content, GetTextGUIStyle(currentType));

                var rectSize = new Vector2(textWidth, iconDrawRect.height);
                if (currentType != IconType.ClipExit)
                {
                    this.drawRect = new Rect(iconDrawRect.position + new Vector2(iconDrawRect.width, 0), rectSize);
                }
                else
                {
                    this.drawRect = new Rect(iconDrawRect.position - new Vector2(textWidth, 0), rectSize);
                }

                this.text = true;
                this.currentType = currentType;
                this.start = this.end = 0.0f;
                RecomputeRange();
            }

            public EventAreaItem(Rect iconDrawRect, IconType currentType, GUIContent icon, Color color)
            {
                this.content = icon;
                this.color = color;
                this.drawRect = iconDrawRect;
                this.text = false;
                this.currentType = currentType;
                this.start = this.end = 0.0f;
                RecomputeRange();
            }

            public readonly IconType currentType;
            public readonly bool text;
            public readonly Rect drawRect;
            public readonly Color color;

            public GUIContent content;
            public float start;
            public float end;

            static float ComputeTextWidth(GUIContent content, GUIStyle style)
            {
                var textSize = Vector2.zero;
                if (!string.IsNullOrEmpty(content.text))
                    textSize = style.CalcSize(content);
                return textSize.x;
            }

            public void RecomputeRange()
            {
                if (text)
                {
                    var textWidth = ComputeTextWidth(content, textStyle);
                    if (currentType != IconType.ClipExit)
                    {
                        start = drawRect.position.x;
                        end = start + textWidth;
                    }
                    else
                    {
                        start = drawRect.position.x + drawRect.width - textWidth;
                        end = drawRect.position.x + drawRect.width;
                    }
                }
                else
                {
                    start = drawRect.position.x;
                    end = drawRect.position.x + drawRect.width;
                }
            }
        }
        List<EventAreaItem> m_TextEventAreaRequested = new List<EventAreaItem>();
        List<EventAreaItem> m_TextEventArea = new List<EventAreaItem>();

        bool AvailableEmplacement(EventAreaItem current, IEnumerable<EventAreaItem> currentZones)
        {
            return !currentZones.Any(o =>
            {
                if (!(current.end <= o.start || current.start >= o.end))
                    return true;
                return false;
            });
        }

        bool AvailableEmplacement(ClipEventBar current, IEnumerable<ClipEventBar> currentBars)
        {
            return !currentBars.Any(o =>
            {
                if (o.rowIndex == current.rowIndex)
                {
                    if (!(current.end < o.start || current.start > o.end))
                        return true;
                }
                return false;
            });
        }

        enum IconType
        {
            ClipEnter,
            ClipExit,
            SingleEvent
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

            //TODOPAUL: Can use directly percentage space
            var clipEvents = VFXTimeSpaceHelper.GetEventNormalizedSpace(PlayableTimeSpace.AfterClipStart, playable, true);
            var singleEvents = VFXTimeSpaceHelper.GetEventNormalizedSpace(PlayableTimeSpace.AfterClipStart, playable, false);
            var allEvents = clipEvents.Concat(singleEvents);
            var clipEventsCount = clipEvents.Count();
            int index = 0;
            var eventNameHeight = 14.0f;
            var iconSize = new Vector2(eventNameHeight, eventNameHeight);

            m_TextEventAreaRequested.Clear();
            foreach (var itEvent in allEvents)
            {
                var relativeTime = InverseLerp(region.startTime, region.endTime, itEvent.time);

                var iconArea = new Rect(
                    currentRect.position.x + currentRect.width * (float)relativeTime - iconSize.x * 0.5f,
                    currentRect.height - eventNameHeight,
                    iconSize.x,
                    iconSize.y);

                var currentType = IconType.SingleEvent;
                if (index < clipEventsCount)
                    currentType = index % 2 == 0 ? IconType.ClipEnter : IconType.ClipExit;

                var color = new Color(itEvent.editorColor.r, itEvent.editorColor.g, itEvent.editorColor.b);
                var icon = new EventAreaItem(iconArea, currentType, new GUIContent() /* TODOPAUL the icon png will be selected here */, color);
                m_TextEventAreaRequested.Add(icon);

                var text = new EventAreaItem(iconArea, currentType, (string)itEvent.name);
                m_TextEventAreaRequested.Add(text);

                index++;
            }

            //Resolve text overlapping
            m_TextEventArea.Clear();

            //Putting all icon
            foreach (var request in m_TextEventAreaRequested)
            {
                var candidate = request;
                if (!candidate.text)
                {
                    m_TextEventArea.Add(candidate);
                }
            }

            //Resolve text
            foreach (var request in m_TextEventAreaRequested)
            {
                var candidate = request;
                if (candidate.text)
                {
                    //Trimming text content until it fits
                    while (!string.IsNullOrEmpty(candidate.content.text))
                    {
                        if (AvailableEmplacement(candidate, m_TextEventArea))
                            break;

                        var newName = candidate.content.text;
                        if (newName.Length > 2)
                            newName = newName.Substring(0, newName.Length - 2) + "…";
                        else
                            newName = string.Empty;
                        candidate.content = new GUIContent(newName);
                        candidate.RecomputeRange();
                    }

                    if (string.IsNullOrEmpty(candidate.content.text))
                        continue; //Avoid putting empty range

                    m_TextEventArea.Add(candidate);
                }
            }

            //Render remaining text
            foreach (var item in m_TextEventArea)
            {
                if (item.text)
                {
                    ShadowLabel(item.drawRect,
                        item.content,
                        item.textStyle,
                        item.textShadowStyle);
                }
                else
                {
                    //Place holder for icons
                    {
                        var iconArea = item.drawRect;
                        var currentType = item.currentType;
                        var baseColor = item.color;
                        EditorGUI.DrawRect(iconArea, new Color(baseColor.r, baseColor.g, baseColor.b, 0.2f));

                        if (currentType == IconType.SingleEvent)
                        {
                            EditorGUI.DrawRect(new Rect(
                                iconArea.position.x + iconArea.width * 0.45f,
                                iconArea.position.y,
                                iconArea.width * 0.1f,
                                iconArea.height), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                iconArea.position.x + iconArea.width * 0.3f,
                                iconArea.position.y + iconArea.height * 0.25f,
                                iconArea.width * 0.4f,
                                iconArea.height * 0.75f), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                iconArea.position.x + iconArea.width * 0.2f,
                                iconArea.position.y + iconArea.height * 0.5f,
                                iconArea.width * 0.6f,
                                iconArea.height * 0.5f), baseColor);
                        }
                        else if (currentType == IconType.ClipEnter)
                        {
                            EditorGUI.DrawRect(new Rect(
                                iconArea.position.x + iconArea.width * 0.5f,
                                iconArea.position.y,
                                iconArea.width * 0.05f,
                                iconArea.height), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                iconArea.position.x + iconArea.width * 0.5f,
                                iconArea.position.y + iconArea.height * 0.25f,
                                iconArea.width * 0.2f,
                                iconArea.height * 0.75f), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                iconArea.position.x + iconArea.width * 0.5f,
                                iconArea.position.y + iconArea.height * 0.5f,
                                iconArea.width * 0.3f,
                                iconArea.height * 0.5f), baseColor);
                        }
                        else if (currentType == IconType.ClipExit)
                        {
                            EditorGUI.DrawRect(new Rect(
                                iconArea.position.x + iconArea.width * 0.45f,
                                iconArea.position.y,
                                iconArea.width * 0.05f,
                                iconArea.height), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                iconArea.position.x + iconArea.width * 0.3f,
                                iconArea.position.y + iconArea.height * 0.25f,
                                iconArea.width * 0.2f,
                                iconArea.height * 0.75f), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                iconArea.position.x + iconArea.width * 0.2f,
                                iconArea.position.y + iconArea.height * 0.5f,
                                iconArea.width * 0.3f,
                                iconArea.height * 0.5f), baseColor);
                        }
                    }
                }
            }

            currentRect.height -= eventNameHeight + 2; //TODOPAUL

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
                        rowIndex = 0u,
                        color = new Color(enter.editorColor.r, enter.editorColor.g, enter.editorColor.b)
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

                EditorGUI.DrawRect(rect, bar.color);
            }

        }
    }
}
#endif
