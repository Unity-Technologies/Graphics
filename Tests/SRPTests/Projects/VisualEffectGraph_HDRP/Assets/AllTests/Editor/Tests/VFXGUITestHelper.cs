using System.Collections;
using System.Reflection;

using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.Test
{
    public static class VFXGUITestHelper
    {
        public static void SendDoubleClick(VisualElement element, int clickCount)
        {
            var clickEvent = MouseDownEvent.GetPooled();
            var clickCountProperty = clickEvent.GetType().GetProperty(nameof(clickEvent.clickCount), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(clickCountProperty);
            clickCountProperty.SetValue(clickEvent, clickCount);

            var buttonProperty = clickEvent.GetType().GetProperty(nameof(clickEvent.button), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(buttonProperty);
            buttonProperty.SetValue(clickEvent, (int)MouseButton.LeftMouse);

            var targetProperty = clickEvent.GetType().GetProperty(nameof(clickEvent.target), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(targetProperty);
            targetProperty.SetValue(clickEvent, element);

            var currentTargetProperty = clickEvent.GetType().GetProperty(nameof(clickEvent.currentTarget), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(currentTargetProperty);
            currentTargetProperty.SetValue(clickEvent, element);

            element.SendEvent(clickEvent);
        }

        public static void SendKeyDown(VisualElement element, KeyCode keyCode)
        {
            var keyDownEvent = KeyDownEvent.GetPooled((char)keyCode, keyCode, EventModifiers.None);
            element.SendEvent(keyDownEvent);
        }

        public static IEnumerator SendKeyDown(VisualElement element, string text)
        {
            foreach (var character in text)
            {
                var keyDownEvent = KeyDownEvent.GetPooled(character, (KeyCode)character, EventModifiers.None);
                element.SendEvent(keyDownEvent);
                yield return null;
            }
        }
    }
}
