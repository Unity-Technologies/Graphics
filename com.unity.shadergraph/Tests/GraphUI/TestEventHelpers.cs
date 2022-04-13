using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEditor.ShaderGraph.GraphUI;
using UIECursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    static class TestEventHelpers
    {
        public static void TestAllElements(VisualElement container, Action<VisualElement> test)
        {
            test(container);
            for (int i = 0; i < container.hierarchy.childCount; ++i)
            {
                var element = container.hierarchy[i];
                TestAllElements(element, test);
            }
        }

        public static void SendMouseEvent(EditorWindow parentWindow,
            VisualElement elementToNotify,
            EventType eventType = EventType.MouseDown,
            MouseButton mouseButton = MouseButton.LeftMouse,
            int clickCount = 1,
            EventModifiers eventModifiers = EventModifiers.None,
            Vector2 positionOffset = default)
        {
            var screenButtonPosition = GetScreenPosition(parentWindow, elementToNotify);
            var mouseEvent = new Event
            {
                type = eventType,
                mousePosition = screenButtonPosition + positionOffset,
                clickCount = clickCount,
                button = (int)mouseButton,
                modifiers = eventModifiers
            };
            parentWindow.SendEvent(mouseEvent);
        }

        // TODO: Reimplement for GTF windows
        public static void SendDeleteCommand(
            EditorWindow parentWindow,
            VisualElement elementToNotify)
        {
            //var deleteCommand = new ExecuteCommandEvent();
            //deleteCommand.SetNonPrivateProperty("commandName", "Delete");
            //if (parentWindow is ShaderGraphEditorWindow editorWindow)
            //{
            //    var graphView = materialGraphEditWindow.graphEditorView.graphView;
            //    graphView.InvokePrivateFunc("OnExecuteCommand", new object[]{ deleteCommand });
            //}
        }

        // TODO: Reimplement for GTF windows
        public static void SendDuplicateCommand(EditorWindow parentWindow)
        {
            if (parentWindow is ShaderGraphEditorWindow editorWindow)
            {
                //var graphView = materialGraphEditWindow.graphEditorView.graphView;
                //graphView?.DuplicateSelection();
            }
        }

        public static void SendKeyDownEvent(
            EditorWindow parentWindow,
            string keyChar,
            EventModifiers eventModifiers = EventModifiers.None)
        {
            // In Unity, key down are sent twice: once with keycode, once with character.

            // Builds event with correct keyCode
            var keyEvent = Event.KeyboardEvent(keyChar);
            keyEvent.type = EventType.KeyDown;
            keyEvent.modifiers = eventModifiers;
            parentWindow.SendEvent(keyEvent);

            keyEvent.character = keyChar.ToCharArray()[0];
            parentWindow.SendEvent(keyEvent);
        }

        public static void SendKeyDownEvent(
            EditorWindow parentWindow,
            KeyCode key = KeyCode.None,
            EventModifiers eventModifiers = EventModifiers.None,
            bool sendTwice = true)
        {
            // In Unity, key down are sent twice: once with keycode, once with character.

            parentWindow.SendEvent(
                new Event
                {
                    type = EventType.KeyDown,
                    keyCode = key,
                    modifiers = eventModifiers
                });

            if(sendTwice)
                parentWindow.SendEvent(
                    new Event
                    {
                        type = EventType.KeyDown,
                        character = (char)key,
                        modifiers = eventModifiers
                    });
        }

        public static void SendKeyUpEvent(
            EditorWindow parentWindow,
            KeyCode key = KeyCode.None,
            EventModifiers eventModifiers = EventModifiers.None)
        {
            parentWindow.SendEvent(
                new Event
                {
                    type = EventType.KeyUp,
                    keyCode = key,
                    modifiers = eventModifiers
                });
        }

        public static Vector2 GetScreenPosition(EditorWindow parentWindow, VisualElement visualElement)
        {
            // WorldBound is the "global" xposition of this element, relative to the top-left corner of this editor window
            var screenPosition = visualElement.worldBound.position;
            // EditorWindow.position is the top-left position of the window in desktop-space
            //screenPosition = (screenPosition + parentWindow.position.position);
            // To account for 4k screens with virtual coordinates, need to be multiply by EditorGUI.pixelsPerPoint to get actual desktop pixels.
            //screenPosition *= EditorGUIUtility.pixelsPerPoint;
            return screenPosition;
        }

    }
}
