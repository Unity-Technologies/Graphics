using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    [SetUpFixture]
    // Since GraphView tests rely on some global state related to UIElements mouse capture
    // Here we make sure to disable input events on the whole editor UI to avoid other interactions
    // from interfering with the tests being run
    class GraphViewTestEnvironment
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            SetDisableInputEventsOnAllWindows(true);
            MouseCaptureController.ReleaseMouse();
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            SetDisableInputEventsOnAllWindows(false);
        }

        static void SetDisableInputEventsOnAllWindows(bool value)
        {
            if (InternalEditorUtility.isHumanControllingUs == false)
                return;

            foreach (var otherWindow in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                ChangeInputEvents(otherWindow, value);
            }
        }

        static void ChangeInputEvents(EditorWindow window, bool value)
        {
            try
            {
                typeof(EditorWindow).GetProperty("disableInputEvents", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(window, value);
            }
            catch
            {
                Debug.LogWarning("Unable to disableInputEvents");
            }
        }
    }
}
