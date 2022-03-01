//#define ENABLE_EVENT_HELPER_TRACE

using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class TestEventHelpers
    {
        readonly EditorWindow m_Window;

        public TestEventHelpers(EditorWindow window)
        {
            m_Window = window;
        }

        public const EventModifiers multiSelectModifier =
#if UNITY_EDITOR_OSX
            EventModifiers.Command;
#else
            EventModifiers.Control;
#endif

        //-----------------------------------------------------------
        // MouseDown Event Helpers
        //-----------------------------------------------------------
        public void MouseDownEvent(Vector2 point, int count, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENT_HELPER_TRACE
            Debug.Log("MouseDown: [" + eventModifiers + "][" + mouseButton + "] @" + point);
#endif
            m_Window.SendEvent(
                new Event
                {
                    type = EventType.MouseDown,
                    mousePosition = point,
                    clickCount = count,
                    button = (int)mouseButton,
                    modifiers = eventModifiers
                });
        }

        public void MouseDownEvent(Vector2 point, MouseButton mouseButton = MouseButton.LeftMouse,
            EventModifiers eventModifiers = EventModifiers.None)
        {
            MouseDownEvent(point, 1, mouseButton, eventModifiers);
        }

        public void MouseDownEvent(VisualElement element, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
            MouseDownEvent(element.worldBound.center, mouseButton, eventModifiers);
        }

        //-----------------------------------------------------------
        // MouseUp Event Helpers
        //-----------------------------------------------------------
        public void MouseUpEvent(Vector2 point, int count, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENT_HELPER_TRACE
            Debug.Log("MouseUp: [" + eventModifiers + "][" + mouseButton + "] @" + point);
#endif

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.MouseUp,
                    mousePosition = point,
                    clickCount = count,
                    button = (int)mouseButton,
                    modifiers = eventModifiers
                });
        }

        public void MouseUpEvent(Vector2 point, MouseButton mouseButton = MouseButton.LeftMouse,
            EventModifiers eventModifiers = EventModifiers.None)
        {
            MouseUpEvent(point, 1, mouseButton, eventModifiers);
        }

        public void MouseUpEvent(VisualElement element, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
            MouseUpEvent(element.worldBound.center, mouseButton, eventModifiers);
        }

        //-----------------------------------------------------------
        // MouseDown + Up Event Helpers (aka Click)
        //-----------------------------------------------------------
        public void MouseClickEvent(Vector2 point, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
            MouseDownEvent(point, 1, mouseButton, eventModifiers);
            MouseUpEvent(point, 1, mouseButton, eventModifiers);
        }

        //-----------------------------------------------------------
        // MouseDrag Event Helpers
        //-----------------------------------------------------------
        public void MouseDragEvent(Vector2 start, Vector2 end, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENT_HELPER_TRACE
            Debug.Log("MouseDrag: [" + eventModifiers + "][" + mouseButton + "] @" + start + " -> " + end);
#endif

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.MouseDrag,
                    mousePosition = end,
                    button = (int)mouseButton,
                    delta = end - start,
                    modifiers = eventModifiers
                });
        }

        //-----------------------------------------------------------
        // MouseMove Event Helpers
        //-----------------------------------------------------------
        public void MouseMoveEvent(Vector2 start, Vector2 end, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENT_HELPER_TRACE
            Debug.Log("MouseMove: [" + eventModifiers + "][" + mouseButton + "] @" + start + " -> " + end);
#endif

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.MouseMove,
                    mousePosition = end,
                    button = (int)mouseButton,
                    delta = end - start,
                    modifiers = eventModifiers
                });
        }

        public void MouseDragEvent(VisualElement start, VisualElement end, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
            MouseDragEvent(start.worldBound.center, end.worldBound.center, mouseButton, eventModifiers);
        }

        //-----------------------------------------------------------
        // ScrollWheel Event Helpers
        //-----------------------------------------------------------
        public void ScrollWheelEvent(float scrollDelta, Vector2 mousePosition, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENT_HELPER_TRACE
            Debug.Log("ScrollWheel: [" + eventModifiers + "] @" + mousePosition + " delta:" + scrollDelta);
#endif

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.ScrollWheel,
                    modifiers = eventModifiers,
                    mousePosition = mousePosition,
                    delta = new Vector2(scrollDelta, scrollDelta)
                });
        }

        //-----------------------------------------------------------
        // Keyboard Event Helpers
        //-----------------------------------------------------------
        public void KeyDownEvent(KeyCode key, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENT_HELPER_TRACE
            Debug.Log("KeyDown: [" + eventModifiers + "][" + key + "]");
#endif

            // In Unity, key down are sent twice: once with keycode, once with character.

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.KeyDown,
                    keyCode = key,
                    modifiers = eventModifiers
                });

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.KeyDown,
                    character = (char)key,
                    modifiers = eventModifiers
                });
        }

        public void KeyUpEvent(KeyCode key, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENT_HELPER_TRACE
            Debug.Log("KeyUp: [" + eventModifiers + "][" + key + "]");
#endif

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.KeyUp,
                    character = (char)key,
                    keyCode = key,
                    modifiers = eventModifiers
                });
        }

        public void SendTabEvent(EventModifiers eventModifiers = EventModifiers.None)
        {
            // This is the event sequence we get when user presses tab.
            m_Window.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Tab, modifiers = eventModifiers });
            m_Window.SendEvent(new Event { type = EventType.KeyDown, character = '\t', modifiers = eventModifiers });
            m_Window.SendEvent(new Event { type = EventType.KeyUp, keyCode = KeyCode.Tab, modifiers = eventModifiers });
        }

        //-----------------------------------------------------------
        // Layout Event Helpers
        //-----------------------------------------------------------
        public void LayoutEvent()
        {
#if ENABLE_EVENT_HELPER_TRACE
            Debug.Log("Layout Event");
#endif
            m_Window.SendEvent(new Event { type = EventType.Layout });
        }

        //-----------------------------------------------------------
        // Repaint Event Helpers
        //-----------------------------------------------------------
        public void RepaintEvent()
        {
#if ENABLE_EVENT_HELPER_TRACE
            Debug.Log("Repaint Event");
#endif
            if (!ApplicationIsHumanControllingUs() && Application.platform == RuntimePlatform.WindowsEditor)
            {
                LogAssert.Expect(LogType.Assert, new Regex("device.IsInsideFrame"));
            }
            m_Window.SendEvent(new Event { type = EventType.Repaint });
        }

        static bool ApplicationIsHumanControllingUs()
        {
            try
            {
                return (bool?)typeof(Application).GetProperty("isHumanControllingUs",
                    BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) ?? false;
            }
            catch
            {
                Debug.LogWarning("Unable to disableInputEvents");
            }
            return false;
        }

        //-----------------------------------------------------------
        // Clicking Helpers
        //-----------------------------------------------------------
        public void Click(Vector2 point, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None, int clickCount = 1)
        {
            MouseDownEvent(point, clickCount, mouseButton, eventModifiers);
            MouseUpEvent(point, clickCount, mouseButton, eventModifiers);
        }

        public void Click(float x, float y, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
            Click(new Vector2(x, y), mouseButton, eventModifiers);
        }

        public void Click(VisualElement element, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None, int clickCount = 1)
        {
            Click(element.worldBound.center, mouseButton, eventModifiers, clickCount);
        }

        //-----------------------------------------------------------
        // Dragging Helpers
        //-----------------------------------------------------------
        public void Drag(Vector2 start, Vector2 translation, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
            DragTo(start, start + translation, mouseButton, eventModifiers);
        }

        public void Drag(VisualElement element, Vector2 translation, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
            Drag(element.worldBound.center, translation, mouseButton, eventModifiers);
        }

        public void DragTo(VisualElement element, VisualElement target, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
            DragTo(element.worldBound.center, target.worldBound.center, mouseButton, eventModifiers);
        }

        public void DragTo(Vector2 start, Vector2 end, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None, int steps = 1)
        {
            MouseDownEvent(start, mouseButton, eventModifiers);
            Vector2 increment = (end - start) / steps;
            for (int i = 0; i < steps; i++)
            {
                MouseDragEvent(start + i * increment, start + (i + 1) * increment, mouseButton, eventModifiers);
            }
            MouseUpEvent(end, mouseButton, eventModifiers);
        }

        public void DragToNoRelease(Vector2 start, Vector2 end, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None, int steps = 1)
        {
            MouseDownEvent(start, mouseButton, eventModifiers);
            Vector2 increment = (end - start) / steps;
            for (int i = 0; i < steps; i++)
            {
                MouseDragEvent(start + i * increment, start + (i + 1) * increment, mouseButton, eventModifiers);
            }
        }

        //-----------------------------------------------------------
        // KeyPressed Helpers
        //-----------------------------------------------------------

        public void KeyPressed(KeyCode key, EventModifiers eventModifiers = EventModifiers.None)
        {
            KeyDownEvent(key, eventModifiers);
            KeyUpEvent(key, eventModifiers);
        }

        public void Type(string text)
        {
            foreach (var c in text)
            {
                KeyPressed((KeyCode)c);
            }
        }

        //-----------------------------------------------------------
        // ValidateCommand Helpers
        //-----------------------------------------------------------
        public bool ValidateCommand(string command)
        {
#if ENABLE_EVENT_HELPER_TRACE
            Debug.Log("ValidateCommand: [" + command + "]");
#endif

            return m_Window.SendEvent(new Event { type = EventType.ValidateCommand, commandName = command });
        }

        //-----------------------------------------------------------
        // ExecuteCommand Helpers
        //-----------------------------------------------------------
        public bool ExecuteCommand(string command)
        {
#if ENABLE_EVENT_HELPER_TRACE
            Debug.Log("ExecuteCommand: [" + command + "]");
#endif

            return m_Window.SendEvent(new Event { type = EventType.ExecuteCommand, commandName = command });
        }
    }
}
