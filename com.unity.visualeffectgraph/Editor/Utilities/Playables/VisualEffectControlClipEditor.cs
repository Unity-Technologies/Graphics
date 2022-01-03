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
        public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
        {
            base.OnCreate(track, copiedFrom);
            if (copiedFrom == null && track.isEmpty)
            {
                track.CreateClip<VisualEffectControlClip>();
            }
        }

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
            shadowRect.xMin += Style.kShadowDistance;
            shadowRect.yMin += Style.kShadowDistance;
            shadowRect.width += Style.kShadowDistance;
            shadowRect.height += Style.kShadowDistance;
            GUI.Label(shadowRect, content, shadowStyle);
            GUI.Label(rect, content, textStyle);
        }

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            return base.GetClipOptions(clip);
        }

        static readonly bool use2xMarker = false;

        static class Content
        {
            public static GUIContent scrubbingDisabled = new GUIContent("Scrubbing Disabled");
            public static GUIContent singleEventIcon;
            public static GUIContent clipEnterIcon;
            public static GUIContent clipExitIcon;

            static Content()
            {
                Texture2D singleEventTexture, clipEnterTexture, clipExitTexture;
                if (use2xMarker)
                {
                    singleEventTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/marker_Single2x.png");
                    clipEnterTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/marker_In2x.png");
                    clipExitTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/marker_Out2x.png");
                }
                else
                {
                    singleEventTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/marker_Single1x.png");
                    clipEnterTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/marker_In1x.png");
                    clipExitTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.visualeffectgraph/Editor/UIResources/VFX/marker_Out1x.png");
                }

                singleEventIcon = new GUIContent(singleEventTexture);
                clipEnterIcon = new GUIContent(clipEnterTexture);
                clipExitIcon = new GUIContent(clipExitTexture);
            }
        }

        static class Style
        {
            public static GUIStyle eventLeftAlign = new GUIStyle(GUI.skin.GetStyle("Label"));
            public static GUIStyle eventRightAlign = new GUIStyle(GUI.skin.GetStyle("Label"));
            public static GUIStyle eventLeftAlignShadow = new GUIStyle(GUI.skin.GetStyle("Label"));
            public static GUIStyle eventRightAlignShadow = new GUIStyle(GUI.skin.GetStyle("Label"));

            public static readonly Color kDarkGlobalBackground = new Color32(81, 86, 94, 255);
            public static readonly Color kDarkScrubbingBackground = new Color32(64, 68, 74, 255);
            public static readonly Color kLightGlobalBackground = new Color32(108, 110, 113, 255);
            public static readonly Color kLightScrubbingBackground = new Color32(136, 141, 148, 255);

            public static GUIStyle scrubbingDisabled = new GUIStyle(GUI.skin.GetStyle("Label"));
            public static GUIStyle scrubbingDisabledShadow = new GUIStyle(GUI.skin.GetStyle("Label"));

            public static readonly float kIconScale = 1.0f;

            public static readonly float kMinimalBarHeight = 2.0f;
            public static readonly float kBarPadding = 1.0f;
            public static readonly float kSingleEventWidth = use2xMarker ? 2.0f : 1.0f;

            private static readonly bool useBiggerFont = false;

            private static readonly float kLineHeight = useBiggerFont ? 16.0f : 14.0f;
            public static readonly float kScrubbingBarHeight = kLineHeight;
            public static readonly float kEventNameHeight = kLineHeight;

            public static readonly int kFontSize = useBiggerFont ? 12 : 10;
            public static readonly float kShadowDistance = useBiggerFont ? 0.5f : 1.0f;

            static Style()
            {
                Color white = new Color32(229, 229, 229, 255);

                eventLeftAlign.normal.textColor = white;
                eventRightAlign.normal.textColor = white;
                scrubbingDisabled.normal.textColor = white;
                eventLeftAlign.hover.textColor = white;
                eventRightAlign.hover.textColor = white;
                scrubbingDisabled.hover.textColor = new Color32(209, 210, 211, 255);

                eventLeftAlign.alignment = eventLeftAlignShadow.alignment = TextAnchor.UpperLeft;
                eventRightAlign.alignment = eventRightAlignShadow.alignment = TextAnchor.UpperRight;

                var shadowColor = Color.black;
                eventLeftAlignShadow.normal.textColor = shadowColor;
                eventLeftAlignShadow.hover.textColor = shadowColor;
                eventRightAlignShadow.normal.textColor = shadowColor;
                eventRightAlignShadow.hover.textColor = shadowColor;
                scrubbingDisabledShadow.normal.textColor = shadowColor;
                scrubbingDisabledShadow.hover.textColor = shadowColor;

                eventLeftAlign.fontSize
                    = eventRightAlign.fontSize
                    = eventLeftAlignShadow.fontSize
                    = eventRightAlignShadow.fontSize
                    = scrubbingDisabled.fontSize
                    = scrubbingDisabledShadow.fontSize
                    = kFontSize;

                scrubbingDisabled.alignment = scrubbingDisabledShadow.alignment = TextAnchor.UpperCenter;
                scrubbingDisabled.fontStyle = scrubbingDisabledShadow.fontStyle = FontStyle.Bold;
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
                    return Style.eventLeftAlign;
                return Style.eventRightAlign;
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
                    return Style.eventLeftAlignShadow;
                return Style.eventRightAlignShadow;
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

        private static double InverseLerp(double a, double b, double value)
        {
            return (value - a) / (b - a);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                value = min;
            if (value > max)
                value = max;
            return value;
        }

        List<ClipEventBar> m_ClipEventBars = new List<ClipEventBar>();
        private List<ClipEventBar> ComputeBarClipEvent(IEnumerable<VisualEffectPlayableSerializedEvent> clipEvents, out uint rowCount)
        {
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
        private List<EventAreaItem> ComputeEventItemArea(ClipBackgroundRegion baseRegion, double maxClipTime, IEnumerable<VisualEffectPlayableSerializedEvent> allEvents, uint clipEventCount)
        {
            m_TextEventAreaRequested.Clear();
            m_TextEventArea.Clear();

            int index = 0;

            m_TextEventAreaRequested.Clear();
            foreach (var itEvent in allEvents)
            {
                var timeAfterClamp = Clamp(itEvent.time, 0.0f, maxClipTime);
                var relativeTime = InverseLerp(baseRegion.startTime, baseRegion.endTime, timeAfterClamp);

                var currentType = IconType.SingleEvent;
                if (index < clipEventCount)
                    currentType = index % 2 == 0 ? IconType.ClipEnter : IconType.ClipExit;

                var color = new Color(itEvent.editorColor.r, itEvent.editorColor.g, itEvent.editorColor.b);

                GUIContent iconContent = null;
                switch (currentType)
                {
                    case IconType.ClipEnter: iconContent = Content.clipEnterIcon; break;
                    case IconType.ClipExit: iconContent = Content.clipExitIcon; break;
                    case IconType.SingleEvent: iconContent = Content.singleEventIcon; break;
                }

                var iconOffset = 0.0f;
                switch (currentType)
                {
                    case IconType.ClipEnter:
                        iconOffset = 0.0f;
                        break;

                    case IconType.ClipExit:
                        iconOffset = -iconContent.image.width * Style.kIconScale;
                        break;

                    case IconType.SingleEvent:
                        iconOffset = -iconContent.image.width * Style.kIconScale * 0.5f;
                        break;
                }
                var iconArea = new Rect(
                    baseRegion.position.width * (float)relativeTime + iconOffset,
                    (Style.kEventNameHeight - (iconContent.image.height * Style.kIconScale)) * 0.5f,
                    iconContent.image.width * Style.kIconScale,
                    iconContent.image.height * Style.kIconScale);

                var icon = new EventAreaItem(iconArea, currentType, iconContent, color);
                m_TextEventAreaRequested.Add(icon);

                iconArea.y = 0.0f;
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

            //Resolve text overlapping
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
            var playable = clip.asset as VisualEffectControlClip;
            if (playable.clipEvents == null || playable.singleEvents == null)
                return;

            var clipEvents = VFXTimeSpaceHelper.GetEventNormalizedSpace(PlayableTimeSpace.AfterClipStart, playable, true);
            var singleEvents = VFXTimeSpaceHelper.GetEventNormalizedSpace(PlayableTimeSpace.AfterClipStart, playable, false);
            var allEvents = clipEvents.Concat(singleEvents);

            //Precompute overlapping data
            var clipEventBars = ComputeBarClipEvent(clipEvents, out var rowCount);
            var eventAreaItems = ComputeEventItemArea(region, clip.duration, allEvents, (uint)clipEvents.Count());

            //Compute region
            var clipBarHeight = rowCount * (Style.kMinimalBarHeight + Style.kBarPadding * 2.0f);
            var eventNameHeight = Style.kEventNameHeight;
            var scrubbingHeight = playable.scrubbing ? 0.0f : Style.kScrubbingBarHeight;

            var initialAvailableHeight = region.position.height;
            var remainingHeight = initialAvailableHeight - (clipBarHeight + scrubbingHeight + eventNameHeight);
            if (remainingHeight < 0)
                remainingHeight = 0.0f;
            clipBarHeight += remainingHeight;

            eventNameHeight = Mathf.Min(eventNameHeight, initialAvailableHeight - clipBarHeight);
            scrubbingHeight = Mathf.Min(scrubbingHeight, initialAvailableHeight - clipBarHeight - eventNameHeight);

            var barRegion = new Rect(region.position.position, new Vector2(region.position.width, clipBarHeight));
            var eventRegion = new Rect(region.position.position + new Vector2(0, clipBarHeight), new Vector2(region.position.width, eventNameHeight));
            var scrubbingRegion = new Rect(region.position.position + new Vector2(0, clipBarHeight + eventNameHeight), new Vector2(region.position.width, scrubbingHeight));

            //Draw custom background
            EditorGUI.DrawRect(region.position, EditorGUIUtility.isProSkin ? Style.kDarkGlobalBackground : Style.kLightGlobalBackground);

            if (!playable.scrubbing)
            {
                EditorGUI.DrawRect(scrubbingRegion, EditorGUIUtility.isProSkin ? Style.kDarkScrubbingBackground : Style.kLightScrubbingBackground);
                ShadowLabel(scrubbingRegion, Content.scrubbingDisabled, Style.scrubbingDisabled, Style.scrubbingDisabledShadow);
            }

            //Draw Clip Bar
            var rowHeight = clipBarHeight / (float)rowCount;
            foreach (var bar in clipEventBars)
            {
                var relativeStart = InverseLerp(region.startTime, region.endTime, bar.start);
                var relativeStop = InverseLerp(region.startTime, region.endTime, bar.end);

                var startRange = region.position.width * Mathf.Clamp01((float)relativeStart);
                var endRange = region.position.width * Mathf.Clamp01((float)relativeStop);

                var rect = new Rect(
                    barRegion.x + startRange,
                    barRegion.y + rowHeight * bar.rowIndex + Style.kBarPadding,
                    endRange - startRange,
                    rowHeight - Style.kBarPadding * 2);
                EditorGUI.DrawRect(rect, bar.color);
            }

            //Draw Text Event
            foreach (var item in eventAreaItems)
            {
                var drawRect = item.drawRect;
                drawRect.position += eventRegion.position;

                if (item.text)
                {
                    drawRect.height = eventRegion.height;
                    ShadowLabel(drawRect,
                        item.content,
                        item.textStyle,
                        item.textShadowStyle);
                }
                else
                {
                    var currentType = item.currentType;
                    var baseColor = item.color;

                    if (currentType == IconType.SingleEvent)
                    {
                        //Exception, drawing a kSingleEventWidth px line from here to begin of clip
                        var sourceArea = drawRect;

                        float triangleGap = 0.0f;
                        var middlePosition = sourceArea.position.x + sourceArea.width * 0.5f;
                        var leftRect = new Rect(middlePosition - Style.kSingleEventWidth * triangleGap - Content.clipExitIcon.image.width * Style.kIconScale,
                                                sourceArea.y,
                                                Content.clipExitIcon.image.width * Style.kIconScale,
                                                Content.clipExitIcon.image.height * Style.kIconScale);

                        var rightRect = new Rect(middlePosition + Style.kSingleEventWidth * triangleGap,
                                                sourceArea.y,
                                                Content.clipEnterIcon.image.width * Style.kIconScale,
                                                Content.clipEnterIcon.image.height * Style.kIconScale);

                        GUI.DrawTexture(leftRect, Content.clipExitIcon.image, ScaleMode.StretchToFill, true, 1.0f, baseColor, 0, 0);
                        GUI.DrawTexture(rightRect, Content.clipEnterIcon.image, ScaleMode.StretchToFill, true, 1.0f, baseColor, 0, 0);

                        var middleLineRect = new Rect(middlePosition - Style.kSingleEventWidth * 0.5f,
                                                        0.0f,
                                                        Style.kSingleEventWidth,
                                                        sourceArea.position.y + sourceArea.height);
                        EditorGUI.DrawRect(middleLineRect, baseColor);
                    }
                    else
                    {
                        GUI.DrawTexture(drawRect, item.content.image, ScaleMode.StretchToFill, true, 1.0f, baseColor, 0, 0);
                    }
                }
            }
            base.DrawBackground(clip, region);
        }
    }
}
#endif
