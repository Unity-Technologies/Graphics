using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.UnitTests;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UIECursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.ShaderGraph.UnitTests
{
    static class ShaderGraphUITestHelpers
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

        public static void SendDeleteCommand(
            EditorWindow parentWindow,
            VisualElement elementToNotify)
        {
            var deleteCommand = new ExecuteCommandEvent();
            deleteCommand.SetNonPrivateProperty("commandName", "Delete");
            if (parentWindow is MaterialGraphEditWindow materialGraphEditWindow)
            {
                var graphView = materialGraphEditWindow.graphEditorView.graphView;
                graphView.InvokePrivateFunc("OnExecuteCommand", new object[]{ deleteCommand });
            }
        }

        public static void SendDuplicateCommand(EditorWindow parentWindow)
        {
            if (parentWindow is MaterialGraphEditWindow materialGraphEditWindow)
            {
                var graphView = materialGraphEditWindow.graphEditorView.graphView;
                graphView?.DuplicateSelection();
            }
        }

        public static void SendKeyEvent(EditorWindow parentWindow,
            VisualElement elementToNotify,
            EventType eventType = EventType.KeyDown,
            char keyboardCharacter = '\0',
            KeyCode keyCode = KeyCode.None,
            int pressCount = 1,
            EventModifiers eventModifiers = EventModifiers.None,
            Vector2 positionOffset = default)
        {
            var screenButtonPosition = GetScreenPosition(parentWindow, elementToNotify);
            var keyboardEvent = new Event()
            {
                type = eventType,
                mousePosition = screenButtonPosition + positionOffset,
                clickCount = pressCount,
                character = keyboardCharacter,
                keyCode = keyCode,
                modifiers = eventModifiers
            };
            parentWindow.SendEvent(keyboardEvent);
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
