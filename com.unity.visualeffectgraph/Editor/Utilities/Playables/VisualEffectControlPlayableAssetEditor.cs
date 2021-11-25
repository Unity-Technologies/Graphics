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
            public static readonly float kEventNameHeight = 14.0f;
            public static readonly Color kScrubbingBackgroundColor = new Color32(64, 68, 74, 255);

            public static readonly float kMinimalBarHeight = 2.0f;
            public static readonly float kBarPadding = 2.0f;


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

        enum IconType
        {
            ClipEnter,
            ClipExit,
            SingleEvent
        }

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


        static bool AvailableEmplacement(EventAreaItem current, IEnumerable<EventAreaItem> currentZones)
        {
            return !currentZones.Any(o =>
            {
                if (!(current.end <= o.start || current.start >= o.end))
                    return true;
                return false;
            });
        }

        static bool AvailableEmplacement(ClipEventBar current, IEnumerable<ClipEventBar> currentBars)
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

        List<ClipEventBar> m_ClipEventBars = new List<ClipEventBar>();
        private List<ClipEventBar> ComputeBarClipEvent(IEnumerable<VisualEffectPlayableSerializedEvent> clipEvents, out uint rowCount)
        {
            //Precompute draw data
            m_ClipEventBars.Clear();
            rowCount = 1;
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
            return m_ClipEventBars;
        }

        List<EventAreaItem> m_TextEventAreaRequested = new List<EventAreaItem>();
        List<EventAreaItem> m_TextEventArea = new List<EventAreaItem>();
        private List<EventAreaItem> ComputeEventName(Rect baseRegion, IEnumerable<VisualEffectPlayableSerializedEvent> allEvents, uint clipEventCount)
        {
            m_TextEventAreaRequested.Clear();
            m_TextEventArea.Clear();

            int index = 0;
            var iconSize = new Vector2(Style.kEventNameHeight, Style.kEventNameHeight);

            m_TextEventAreaRequested.Clear();
            foreach (var itEvent in allEvents)
            {
                var relativeTime = Mathf.Clamp01((float)itEvent.time / 100.0f);

                var iconArea = new Rect(
                    baseRegion.position.x + baseRegion.width * (float)relativeTime - iconSize.x * 0.5f,
                    0.0f,
                    iconSize.x,
                    iconSize.y);

                var currentType = IconType.SingleEvent;
                if (index < clipEventCount)
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
            return m_TextEventArea;
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            base.DrawBackground(clip, region);
            var playable = clip.asset as VisualEffectControlClip;
            if (playable.clipEvents == null || playable.singleEvents == null)
                return;

            var clipEvents = VFXTimeSpaceHelper.GetEventNormalizedSpace(PlayableTimeSpace.Percentage, playable, true);
            var singleEvents = VFXTimeSpaceHelper.GetEventNormalizedSpace(PlayableTimeSpace.Percentage, playable, false);
            var allEvents = clipEvents.Concat(singleEvents);

            //Precompute overlapping data
            var clipEventBars = ComputeBarClipEvent(clipEvents, out var rowCount);
            var eventAreaName = ComputeEventName(region.position, allEvents, (uint)clipEvents.Count());

            //Compute region
            var clipBarHeight = rowCount * (Style.kMinimalBarHeight + Style.kBarPadding * 2.0f);
            var eventNameHeight = Style.kEventNameHeight;
            var scrubbingHeight = playable.scrubbing ? 0.0f : Style.kScrubbingBarHeight;

            var remainingHeight = region.position.height - (clipBarHeight + scrubbingHeight + eventNameHeight);
            if (remainingHeight < 0)
                remainingHeight = 0.0f;
            clipBarHeight += remainingHeight;

            var barRegion = new Rect(region.position.position, new Vector2(region.position.width, clipBarHeight));
            var eventRegion = new Rect(region.position.position + new Vector2(0, clipBarHeight), new Vector2(region.position.width, eventNameHeight));
            var scrubbingRegion = new Rect(region.position.position + new Vector2(0, clipBarHeight + eventNameHeight), new Vector2(region.position.width, scrubbingHeight));

            //Draw custom background
            EditorGUI.DrawRect(region.position, Style.kGlobalBackground);

            if (!playable.scrubbing)
            {
                EditorGUI.DrawRect(scrubbingRegion, Style.kScrubbingBackgroundColor);
                GUI.Label(scrubbingRegion, Content.scrubbingDisabled, Style.scrubbingDisabled);
            }

            //Draw Clip Bar
            var rowHeight = clipBarHeight / (float)rowCount;
            foreach (var bar in clipEventBars)
            {
                var relativeStart = bar.start / 100.0f;
                var relativeStop = bar.end / 100.0f;

                var startRange = region.position.width * Mathf.Clamp01((float)relativeStart);
                var endRange = region.position.width * Mathf.Clamp01((float)relativeStop);

                var rect = new Rect(
                    barRegion.x + startRange,
                    barRegion.y + rowHeight * (rowCount - bar.rowIndex - 1) + Style.kBarPadding,
                    endRange - startRange,
                    rowHeight - Style.kBarPadding);
                EditorGUI.DrawRect(rect, bar.color);
            }

            //Draw Text Event
            foreach (var item in eventAreaName)
            {
                var drawRect = item.drawRect;
                drawRect.position += eventRegion.position;

                if (item.text)
                {
                    ShadowLabel(drawRect,
                        item.content,
                        item.textStyle,
                        item.textShadowStyle);
                }
                else
                {
                    //Place holder for icons
                    {
                        var currentType = item.currentType;
                        var baseColor = item.color;
                        EditorGUI.DrawRect(drawRect, new Color(baseColor.r, baseColor.g, baseColor.b, 0.2f));

                        if (currentType == IconType.SingleEvent)
                        {
                            /* exception, drawing a 2px line from here to begin */
                            EditorGUI.DrawRect(new Rect(
                                drawRect.position.x + drawRect.width * 0.5f - 0.5f,
                                0.0f,
                                1.0f,
                                clipBarHeight + eventNameHeight), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                drawRect.position.x + drawRect.width * 0.45f,
                                drawRect.position.y,
                                drawRect.width * 0.1f,
                                drawRect.height), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                drawRect.position.x + drawRect.width * 0.3f,
                                drawRect.position.y + drawRect.height * 0.25f,
                                drawRect.width * 0.4f,
                                drawRect.height * 0.75f), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                drawRect.position.x + drawRect.width * 0.2f,
                                drawRect.position.y + drawRect.height * 0.5f,
                                drawRect.width * 0.6f,
                                drawRect.height * 0.5f), baseColor);
                        }
                        else if (currentType == IconType.ClipEnter)
                        {
                            EditorGUI.DrawRect(new Rect(
                                drawRect.position.x + drawRect.width * 0.5f,
                                drawRect.position.y,
                                drawRect.width * 0.05f,
                                drawRect.height), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                drawRect.position.x + drawRect.width * 0.5f,
                                drawRect.position.y + drawRect.height * 0.25f,
                                drawRect.width * 0.2f,
                                drawRect.height * 0.75f), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                drawRect.position.x + drawRect.width * 0.5f,
                                drawRect.position.y + drawRect.height * 0.5f,
                                drawRect.width * 0.3f,
                                drawRect.height * 0.5f), baseColor);
                        }
                        else if (currentType == IconType.ClipExit)
                        {
                            EditorGUI.DrawRect(new Rect(
                                drawRect.position.x + drawRect.width * 0.45f,
                                drawRect.position.y,
                                drawRect.width * 0.05f,
                                drawRect.height), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                drawRect.position.x + drawRect.width * 0.3f,
                                drawRect.position.y + drawRect.height * 0.25f,
                                drawRect.width * 0.2f,
                                drawRect.height * 0.75f), baseColor);

                            EditorGUI.DrawRect(new Rect(
                                drawRect.position.x + drawRect.width * 0.2f,
                                drawRect.position.y + drawRect.height * 0.5f,
                                drawRect.width * 0.3f,
                                drawRect.height * 0.5f), baseColor);
                        }
                    }
                }
            }
        }
    }
}
#endif
