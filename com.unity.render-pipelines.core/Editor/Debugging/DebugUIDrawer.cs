using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Attribute specifying wich type of Debug Item should this drawer be used with.
    /// </summary>
    public class DebugUIDrawerAttribute : Attribute
    {
        internal readonly Type type;

        /// <summary>
        /// Constructor for DebugUIDraw Attribute
        /// </summary>
        /// <param name="type">Type of Debug Item this draw should be used with.</param>
        public DebugUIDrawerAttribute(Type type)
        {
            this.type = type;
        }
    }

    /// <summary>
    /// Debug Item Drawer
    /// </summary>
    public class DebugUIDrawer
    {
        /// <summary>
        /// Cast into the proper type.
        /// </summary>
        /// <typeparam name="T">Type of the drawer</typeparam>
        /// <param name="o">Object to be cast</param>
        /// <returns>Returns o cast to type T</returns>
        protected T Cast<T>(object o)
            where T : class
        {
            var casted = o as T;

            if (casted == null)
            {
                string typeName = o == null ? "null" : o.GetType().ToString();
                throw new InvalidOperationException("Can't cast " + typeName + " to " + typeof(T));
            }

            return casted;
        }

        /// <summary>
        /// Implement this to execute processing before UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public virtual void Begin(DebugUI.Widget widget, DebugState state)
        { }

        /// <summary>
        /// Implement this to execute UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        /// <returns>Returns the state of the widget.</returns>
        public virtual bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            return true;
        }

        /// <summary>
        /// Implement this to execute processing after UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public virtual void End(DebugUI.Widget widget, DebugState state)
        { }

        /// <summary>
        /// Applies a value to the widget and the Debug State of the Debug Item.
        /// </summary>
        /// <param name="widget">Debug Item widget.</param>
        /// <param name="state">Debug State associated with the Debug Item</param>
        /// <param name="value">Input value.</param>
        protected void Apply(DebugUI.IValueField widget, DebugState state, object value)
        {
            Undo.RegisterCompleteObjectUndo(state, "Debug Property Change");
            state.SetValue(value, widget);
            widget.SetValue(value);
            EditorUtility.SetDirty(state);
            DebugState.m_CurrentDirtyState = state;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>
        /// Prepares the rendering Rect of the Drawer.
        /// </summary>
        /// <param name="height">Height of the rect.</param>
        /// <param name="fullWidth">Whether to reserve full width for the element.</param>
        /// <returns>Appropriate Rect for drawing.</returns>
        protected Rect PrepareControlRect(float height = -1, bool fullWidth = false)
        {
            if (height < 0)
                height = EditorGUIUtility.singleLineHeight;
            var rect = GUILayoutUtility.GetRect(1f, 1f, height, height);

            const float paddingLeft = 4f;
            rect.width -= paddingLeft;
            rect.xMin += paddingLeft;

            EditorGUIUtility.labelWidth = fullWidth ? rect.width : rect.width / 2f;

            return rect;
        }
    }
}
