using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Bridge
{
    class AnimEditorOverlay
    {
        public TimelineState state;

        TimeCursorManipulator m_PlayHeadCursor;
        public Color PlayHeadColor { get; set; }

        Rect m_Rect;
        Rect m_ContentRect;

        public void OnGUI(Rect timeRect, Rect contentRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            m_Rect = timeRect;
            m_ContentRect = contentRect;

            GUI.BeginClip(timeRect);
            m_PlayHeadCursor.OnGUI(m_Rect, m_Rect.xMin + TimeToPixel(state.CurrentTime));

            var iconContent = EditorGUIUtility.IconContent("Animation.EventMarker");
            var position = new Rect(m_Rect.xMin + TimeToPixel(state.CurrentTime) - iconContent.image.width / 2.0f, 2, 16, 30);
            GUI.Box(position, iconContent, GUIStyle.none);
            GUI.EndClip();
        }

        public void HandleEvents()
        {
            if (m_PlayHeadCursor == null)
            {
                m_PlayHeadCursor = new TimeCursorManipulator(AnimationWindowStyles.playHead) { drawHead = false };
                m_PlayHeadCursor.headColor = PlayHeadColor;
                m_PlayHeadCursor.lineColor = PlayHeadColor;
                m_PlayHeadCursor.alignment = TimeCursorManipulator.Alignment.Left;
                m_PlayHeadCursor.onStartDrag += (manipulator, evt) => OnDragPlayHead(evt);
                m_PlayHeadCursor.onDrag += (manipulator, evt) => OnDragPlayHead(evt);
            }

            m_PlayHeadCursor.HandleEvents();
        }

        bool OnDragPlayHead(Event evt)
        {
            state.CurrentTime = MousePositionToTime(evt);
            return true;
        }

        float MousePositionToTime(Event evt)
        {
            float width = m_ContentRect.width;
            float time = Mathf.Max(evt.mousePosition.x / width * state.VisibleTimeSpan + state.MinVisibleTime, 0);
            time = TimelineState.SnapToFrame(time, TimelineState.SnapMode.SnapToFrame);
            return time;
        }

        float TimeToPixel(float time)
        {
            return state.TimeToPixel(time);
        }
    }
}
