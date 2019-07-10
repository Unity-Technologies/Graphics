using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.HighDefinition
{
    public enum XRDebugMode
    {
        None,
        Composite,
    }

    static class XRDebugMenu
    {
        public static XRDebugMode debugMode { get; set; }
        public static bool displayCompositeBorders;
        public static bool animateCompositeTiles;

        static GUIContent[] debugModeStrings = null;
        static int[] debugModeValues = null;

        public static void Init()
        {
            debugModeValues = (int[])Enum.GetValues(typeof(XRDebugMode));
            debugModeStrings = Enum.GetNames(typeof(XRDebugMode))
                .Select(t => new GUIContent(t))
                .ToArray();
        }

        public static void Reset()
        {
            debugMode = XRDebugMode.None;
            displayCompositeBorders = false;
            animateCompositeTiles = false;
        }

        public static void AddWidgets(List<DebugUI.Widget> widgetList, Action<DebugUI.Field<int>, int> RefreshCallback)
        {
            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "XR Debug Mode", getter = () => (int)debugMode, setter = value => debugMode = (XRDebugMode)value, enumNames = debugModeStrings, enumValues = debugModeValues, getIndex = () => (int)debugMode, setIndex = value => debugMode = (XRDebugMode)value, onValueChanged = RefreshCallback },
            });

            if (debugMode == XRDebugMode.Composite)
            {
                widgetList.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.BoolField { displayName = "Display borders", getter = () => displayCompositeBorders, setter = value => displayCompositeBorders = value },
                        new DebugUI.BoolField { displayName = "Animate tiles",   getter = () => animateCompositeTiles, setter = value => animateCompositeTiles = value }
                    }
                });
            }
        }
    }
}