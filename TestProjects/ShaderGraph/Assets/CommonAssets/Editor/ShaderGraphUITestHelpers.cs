using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.ShaderGraph.UnitTests;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UIECursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.ShaderGraph.UnitTests
{
    static class ShaderGraphUITestHelpers
    {
        private static readonly MethodInfo CreateEventMethodInfo = null;
        static ShaderGraphUITestHelpers()
        {
            // Get reference to UIElements assembly
            Assembly uiElementAssembly = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName().ToString();
                if (assemblyName.Contains("UnityEngine.UIElementsModule"))
                {
                    uiElementAssembly = assembly;
                }
            }

            // Get specific class that is used for UI event generation, as it is currently marked as internal and is inaccessible directly
            Type uiElementsUtilityType = uiElementAssembly?.GetType("UnityEngine.UIElements.UIElementsUtility");

            // Cache the method info for this function to be used through application lifetime
            CreateEventMethodInfo = uiElementsUtilityType?.GetMethod("CreateEvent",
                BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[] {typeof(Event), typeof(EventType)},
                null);
        }

        private static EventBase CreateEvent(Event evt)
        {
            return (EventBase)CreateEventMethodInfo?.Invoke(null,  new object[]{evt, evt.rawType});
        }

        public static EventBase MakeEvent(EventType type)
        {
            var evt = new Event() { type = type };
            return CreateEvent(evt);
        }

        public static EventBase MakeEvent(EventType type, Vector2 position)
        {
            var evt = new Event() { type = type, mousePosition = position };
            return CreateEvent(evt);
        }

        public static EventBase MakeKeyEvent(EventType type, KeyCode code, char character = '\0', EventModifiers modifiers = EventModifiers.None)
        {
            var evt = new Event() { type = type, keyCode = code, character = character, modifiers = modifiers};
            return CreateEvent(evt);
        }

        public static EventBase MakeMouseMoveEvent(Vector2 mousePosition, MouseButton button = MouseButton.LeftMouse, EventModifiers modifiers = EventModifiers.None, int clickCount = 1)
        {
            var evt = new Event() { type = EventType.MouseMove, mousePosition = mousePosition, button = (int)button, modifiers = modifiers, clickCount = clickCount};
            return CreateEvent(evt);
        }

        public static EventBase MakeMouseEvent(EventType type, Vector2 position, MouseButton button = MouseButton.LeftMouse, EventModifiers modifiers = EventModifiers.None, int clickCount = 1)
        {
            var evt = new Event() { type = type, mousePosition = position, button = (int)button, modifiers = modifiers, clickCount = clickCount};
            return CreateEvent(evt);
        }

        public static EventBase MakeScrollWheelEvent(Vector2 delta, Vector2 position)
        {
            var evt = new Event
            {
                type = EventType.ScrollWheel,
                delta = delta,
                mousePosition = position
            };

            return CreateEvent(evt);
        }

        public static EventBase MakeCommandEvent(EventType type, string command)
        {
            var evt = new Event() { type = type, commandName = command };
            return CreateEvent(evt);
        }

        public static void TestAllElements(VisualElement container, Action<VisualElement> test)
        {
            test(container);
            for (int i = 0; i < container.hierarchy.childCount; ++i)
            {
                var element = container.hierarchy[i];
                TestAllElements(element, test);
            }
        }

        public static void SendMouseEventToVisualElement(
            VisualElement elementToNotify,
            EventType eventType,
            MouseButton mouseButton = MouseButton.LeftMouse,
            Vector2 eventPositionOffset = default)
        {
            var screenButtonPosition = GetScreenPosition(elementToNotify);
            // Apply offset if any was specified
            screenButtonPosition += eventPositionOffset;
            using var evt = ShaderGraphUITestHelpers.MakeMouseEvent(eventType, screenButtonPosition, mouseButton);
            evt.target = elementToNotify;
            elementToNotify.SendEvent(evt);
        }

        public static Vector2 GetScreenPosition(VisualElement visualElement)
        {
            // WorldBound is the "global" xposition of this element, relative to the top-left corner of this editor window
            var screenPosition = visualElement.worldBound.position;
            // EditorWindow.position is the top-left position of the window in desktop-space
            screenPosition = (screenPosition + EditorWindow.focusedWindow.position.position);
            // To account for 4k screens with virtual coordinates, need to be multiply by EditorGUI.pixelsPerPoint to get actual desktop pixels.
            //actualScreenPosition *= EditorGUIUtility.pixelsPerPoint;
            return screenPosition;
        }

    }
}
