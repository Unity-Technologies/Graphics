using System;
using System.Text;
using UnityEditor.Rendering.Analytics;
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
            if (o == null) return null;

            if (o is T casted)
                return casted;

            StringBuilder info = new StringBuilder("Cast Exception:");
            switch (o)
            {
                case DebugUI.Widget value:
                    info.AppendLine($"Query Path : {value.queryPath}");
                    break;
                case DebugState state:
                    info.AppendLine($"Query Path : {state.queryPath}");
                    break;
            }
            info.AppendLine($"Object to Cast Type : {o.GetType().AssemblyQualifiedName}");
            info.AppendLine($"Target Cast Type : {typeof(T).AssemblyQualifiedName}");

            throw new InvalidCastException(info.ToString());
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
            Undo.RegisterCompleteObjectUndo(state, $"Modified Value '{state.queryPath}'");
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

    /// <summary>
    /// Common class to help drawing fields
    /// </summary>
    /// <typeparam name="TValue">The internal value of the field</typeparam>
    /// <typeparam name="TField">The type of the field widget</typeparam>
    /// <typeparam name="TState">The state of the field</typeparam>
    public abstract class DebugUIFieldDrawer<TValue, TField, TState> : DebugUIDrawer
        where TField : DebugUI.Field<TValue>
        where TState : DebugState
    {
        private TValue value { get; set; }

        /// <summary>
        /// Implement this to execute processing before UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void Begin(DebugUI.Widget widget, DebugState state)
        {
            EditorGUI.BeginChangeCheck();
        }

        /// <summary>
        /// Implement this to execute UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        /// <returns>Returns the state of the widget.</returns>
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            value = DoGUI(
                PrepareControlRect(),
                EditorGUIUtility.TrTextContent(widget.displayName, widget.tooltip),
                Cast<TField>(widget),
                Cast<TState>(state)
            );

            return true;
        }

        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected abstract TValue DoGUI(Rect rect, GUIContent label, TField field, TState state);

        /// <summary>
        /// Implement this to execute processing after UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void End(DebugUI.Widget widget, DebugState state)
        {
            if (EditorGUI.EndChangeCheck())
            {
                var w = Cast<TField>(widget);
                var s = Cast<TState>(state);
                Apply(w, s, value);

                DebugManagerWidgetUsedAnalytic.Send(widget.queryPath, value);
            }
        }
    }

    /// <summary>
    /// Common class to help drawing widgets
    /// </summary>
    /// <typeparam name="TWidget">The widget</typeparam>
    public abstract class DebugUIWidgetDrawer<TWidget> : DebugUIDrawer
        where TWidget : DebugUI.Widget
    {
        /// <summary>
        /// Implement this to execute processing before UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void Begin(DebugUI.Widget widget, DebugState state)
        {
        }

        /// <summary>
        /// Implement this to execute UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        /// <returns>Returns the state of the widget.</returns>
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            DoGUI(
                PrepareControlRect(),
                EditorGUIUtility.TrTextContent(widget.displayName, widget.tooltip),
                Cast<TWidget>(widget)
            );

            return true;
        }

        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="w">The widget</param>
        protected abstract void DoGUI(Rect rect, GUIContent label, TWidget w);

        /// <summary>
        /// Implement this to execute processing after UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void End(DebugUI.Widget widget, DebugState state)
        {
        }
    }
}
