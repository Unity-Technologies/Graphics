using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // TODO (Brett) Make this make sense for SG.
    /**
     * WARNING -- This code was copied from the MathBookMainToolbar example.
     *            It is likely that it does not do what we want.
     */
    public class ShaderGraphMainToolbar : MainToolbar
    {
        public ShaderGraphMainToolbar(BaseGraphTool graphTool, GraphView graphView)
            : base(graphTool, graphView) { }

        protected override void BuildOptionMenu(GenericMenu menu)
        {
            base.BuildOptionMenu(menu);
            var preferences = GraphTool?.Preferences;
            GUIContent CreateTextContent(string content)
            {
                return new GUIContent(content);
            }
            void MenuItem(string title, bool value, GenericMenu.MenuFunction onToggle)
                => menu.AddItem(CreateTextContent(title), value, onToggle);
            void MenuToggle(string title, BoolPref k, Action callback = null)
            {
                if (preferences != null)
                    MenuItem(title, preferences.GetBool(k), () =>
                    {
                        preferences.ToggleBool(k);
                        callback?.Invoke();
                    });
            }
            menu.AddSeparator("");
            MenuToggle("Auto Itemize Constants", BoolPref.AutoItemizeConstants);
            MenuToggle("Auto Itemize Variables", BoolPref.AutoItemizeVariables);
        }
    }
}
