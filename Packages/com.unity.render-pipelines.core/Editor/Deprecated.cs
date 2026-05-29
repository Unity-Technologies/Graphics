using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Attribute specifying wich type of Debug Item should this drawer be used with.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
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
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
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
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
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
            }
        }
    }

    /// <summary>
    /// Common class to help drawing widgets
    /// </summary>
    /// <typeparam name="TWidget">The widget</typeparam>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
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

    /// <summary>
    /// Serialized state of a Debug Item.
    /// </summary>
    [Serializable]
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    public abstract class DebugState : ScriptableObject
    {
        /// <summary>
        /// Path of the Debug Item.
        /// </summary>
        [SerializeField]
        protected string m_QueryPath;

        // We need this to keep track of the state modified in the current frame.
        // This helps reduces the cost of re-applying states to original widgets and is also needed
        // when two states point to the same value (e.g. when using split enums like HDRP does for
        // the `fullscreenDebugMode`.
        internal static DebugState m_CurrentDirtyState;

        /// <summary>
        /// Path of the Debug Item.
        /// </summary>
        public string queryPath
        {
            get { return m_QueryPath; }
            internal set { m_QueryPath = value; }
        }

        /// <summary>
        /// Returns the value of the Debug Item.
        /// </summary>
        /// <returns>Value of the Debug Item.</returns>
        public abstract object GetValue();

        /// <summary>
        /// Set the value of the Debug Item.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="field">Debug Item field.</param>
        public abstract void SetValue(object value, DebugUI.IValueField field);

        /// <summary>
        /// OnEnable implementation.
        /// </summary>
        public virtual void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }
    }

    /// <summary>
    /// Generic serialized state of a Debug Item.
    /// </summary>
    /// <typeparam name="T">The type of the Debug Item.</typeparam>
    [Serializable]
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    public class DebugState<T> : DebugState
    {
        /// <summary>
        /// Value of the Debug Item.
        /// </summary>
        [SerializeField]
        protected T m_Value;

        /// <summary>
        /// Value of the Debug Item
        /// </summary>
        public virtual T value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        /// <summary>
        /// Returns the value of the Debug Item.
        /// </summary>
        /// <returns>Value of the Debug Item.</returns>
        public override object GetValue()
        {
            return value;
        }

        /// <summary>
        /// Set the value of the Debug Item.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="field">Debug Item field.</param>
        public override void SetValue(object value, DebugUI.IValueField field)
        {
            this.value = (T)field.ValidateValue(value);
        }

        /// <summary>
        /// Returns the hash code of the Debug Item.
        /// </summary>
        /// <returns>Hash code of the Debug Item</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;
                hash = hash * 23 + (m_QueryPath != null ? m_QueryPath.GetHashCode() : 0);
                if (value != null)
                    hash = hash * 23 + value.GetHashCode();

                return hash;
            }
        }
    }

    /// <summary>
    /// Attribute specifying which types should be save as this Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    public sealed class DebugStateAttribute : Attribute
    {
        internal readonly Type[] types;

        /// <summary>
        /// Debug State Attribute constructor
        /// </summary>
        /// <param name="types">List of types of the Debug State.</param>
        public DebugStateAttribute(params Type[] types)
        {
            this.types = types;
        }
    }

    // Builtins
    /// <summary>
    /// Boolean Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.BoolField), typeof(DebugUI.Foldout), typeof(DebugUI.HistoryBoolField))]
    public sealed class DebugStateBool : DebugState<bool> { }

    /// <summary>
    /// Enums Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.EnumField), typeof(DebugUI.HistoryEnumField))]
    public sealed class DebugStateEnum : DebugState<int>
    {
    }

    /// <summary>
    /// Integer Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.IntField))]
    public sealed class DebugStateInt : DebugState<int> { }

    /// <summary>
    /// Object Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.ObjectPopupField), typeof(DebugUI.CameraSelector), typeof(DebugUI.ObjectField))]
    public sealed class DebugStateObject : DebugState<UnityEngine.Object>
    {
    }

    /// <summary>
    /// Flags Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.BitField))]
    public sealed class DebugStateFlags : DebugState<Enum>
    {
    }

    /// <summary>
    /// Unsigned Integer Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.UIntField))]
    public sealed class DebugStateUInt : DebugState<uint> { }

    /// <summary>
    /// Rendering layer mask state.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.RenderingLayerField))]
    public sealed class DebugStateRenderingLayer : DebugState<RenderingLayerMask> { }

    /// <summary>
    /// Float Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.FloatField))]
    public sealed class DebugStateFloat : DebugState<float> { }

    /// <summary>
    /// Color Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.ColorField))]
    public sealed class DebugStateColor : DebugState<Color> { }

    /// <summary>
    /// Vector2 Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.Vector2Field))]
    public sealed class DebugStateVector2 : DebugState<Vector2> { }

    /// <summary>
    /// Vector3 Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.Vector3Field))]
    public sealed class DebugStateVector3 : DebugState<Vector3> { }

    /// <summary>
    /// Vector4 Debug State.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [Serializable, DebugState(typeof(DebugUI.Vector4Field))]
    public sealed class DebugStateVector4 : DebugState<Vector4> { }

    /// <summary>
    /// Builtin Drawer for Value Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.Value))]
    public sealed class DebugUIDrawerValue : DebugUIWidgetDrawer<DebugUI.Value>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The widget</param>
        protected override void DoGUI(Rect rect, GUIContent label, DebugUI.Value field)
        {
        }
    }

    /// <summary>
    /// Builtin Drawer for ValueTuple Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.ValueTuple))]
    public sealed class DebugUIDrawerValueTuple : DebugUIWidgetDrawer<DebugUI.ValueTuple>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The widget</param>
        protected override void DoGUI(Rect rect, GUIContent label, DebugUI.ValueTuple field)
        {
        }
    }

    /// <summary>
    /// Builtin Drawer for ProgressBarValue Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.ProgressBarValue))]
    public sealed class DebugUIDrawerProgressBarValue : DebugUIWidgetDrawer<DebugUI.ProgressBarValue>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The widget</param>
        protected override void DoGUI(Rect rect, GUIContent label, DebugUI.ProgressBarValue field)
        {
        }
    }

    /// <summary>
    /// Builtin Drawer for Button Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.Button))]
    public sealed class DebugUIDrawerButton : DebugUIWidgetDrawer<DebugUI.Button>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The widget</param>
        protected override void DoGUI(Rect rect, GUIContent label, DebugUI.Button field)
        {
        }
    }

    /// <summary>
    /// Builtin Drawer for Boolean Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.BoolField))]
    public sealed class DebugUIDrawerBoolField : DebugUIFieldDrawer<bool, DebugUI.BoolField, DebugStateBool>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The value</returns>
        protected override bool DoGUI(Rect rect, GUIContent label, DebugUI.BoolField field, DebugStateBool state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for History Boolean Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.HistoryBoolField))]
    public sealed class DebugUIDrawerHistoryBoolField : DebugUIFieldDrawer<bool, DebugUI.HistoryBoolField, DebugStateBool>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override bool DoGUI(Rect rect, GUIContent label, DebugUI.HistoryBoolField field, DebugStateBool state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Integer Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.IntField))]
    public sealed class DebugUIDrawerIntField : DebugUIFieldDrawer<int, DebugUI.IntField, DebugStateInt>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override int DoGUI(Rect rect, GUIContent label, DebugUI.IntField field, DebugStateInt state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Unsigned Integer Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.UIntField))]
    public sealed class DebugUIDrawerUIntField : DebugUIFieldDrawer<uint, DebugUI.UIntField, DebugStateUInt>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override uint DoGUI(Rect rect, GUIContent label, DebugUI.UIntField field, DebugStateUInt state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Float Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.FloatField))]
    public sealed class DebugUIDrawerFloatField : DebugUIFieldDrawer<float, DebugUI.FloatField, DebugStateFloat>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override float DoGUI(Rect rect, GUIContent label, DebugUI.FloatField field, DebugStateFloat state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Enum Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.EnumField))]
    public sealed class DebugUIDrawerEnumField : DebugUIFieldDrawer<int, DebugUI.EnumField, DebugStateEnum>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override int DoGUI(Rect rect, GUIContent label, DebugUI.EnumField field, DebugStateEnum state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Object Popup Fields Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.ObjectPopupField))]
    public sealed class DebugUIDrawerObjectPopupField : DebugUIFieldDrawer<UnityEngine.Object, DebugUI.ObjectPopupField, DebugStateObject>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override UnityEngine.Object DoGUI(Rect rect, GUIContent label, DebugUI.ObjectPopupField field, DebugStateObject state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for History Enum Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.HistoryEnumField))]
    public sealed class DebugUIDrawerHistoryEnumField : DebugUIFieldDrawer<int, DebugUI.HistoryEnumField, DebugStateEnum>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override int DoGUI(Rect rect, GUIContent label, DebugUI.HistoryEnumField field, DebugStateEnum state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Bitfield Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.BitField))]
    public sealed class DebugUIDrawerBitField : DebugUIFieldDrawer<Enum, DebugUI.BitField, DebugStateFlags>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override Enum DoGUI(Rect rect, GUIContent label, DebugUI.BitField field, DebugStateFlags state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Maskfield Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.RenderingLayerField))]
    public sealed class DebugUIDrawerRenderingLayerField : DebugUIFieldDrawer<RenderingLayerMask, DebugUI.RenderingLayerField, DebugStateRenderingLayer>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override RenderingLayerMask DoGUI(Rect rect, GUIContent label, DebugUI.RenderingLayerField field, DebugStateRenderingLayer state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Foldout Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.Foldout))]
    public sealed class DebugUIDrawerFoldout : DebugUIDrawer
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
        /// OnGUI implementation for Foldout DebugUIDrawer.
        /// </summary>
        /// <param name="widget">DebugUI Widget.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        /// <returns>The state of the widget.</returns>
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            return default;
        }

        /// <summary>
        /// End implementation for Foldout DebugUIDrawer.
        /// </summary>
        /// <param name="widget">DebugUI Widget.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void End(DebugUI.Widget widget, DebugState state)
        {
        }
    }

    /// <summary>
    /// Builtin Drawer for Color Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.ColorField))]
    public sealed class DebugUIDrawerColorField : DebugUIFieldDrawer<Color, DebugUI.ColorField, DebugStateColor>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override Color DoGUI(Rect rect, GUIContent label, DebugUI.ColorField field, DebugStateColor state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Vector2 Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.Vector2Field))]
    public sealed class DebugUIDrawerVector2Field : DebugUIFieldDrawer<Vector2, DebugUI.Vector2Field, DebugStateVector2>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override Vector2 DoGUI(Rect rect, GUIContent label, DebugUI.Vector2Field field, DebugStateVector2 state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Vector3 Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.Vector3Field))]
    public sealed class DebugUIDrawerVector3Field : DebugUIFieldDrawer<Vector3, DebugUI.Vector3Field, DebugStateVector3>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override Vector3 DoGUI(Rect rect, GUIContent label, DebugUI.Vector3Field field, DebugStateVector3 state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Vector4 Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.Vector4Field))]
    public sealed class DebugUIDrawerVector4Field : DebugUIFieldDrawer<Vector4, DebugUI.Vector4Field, DebugStateVector4>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override Vector4 DoGUI(Rect rect, GUIContent label, DebugUI.Vector4Field field, DebugStateVector4 state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for <see cref="DebugUI.ObjectField"/> items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.ObjectField))]
    public sealed class DebugUIDrawerObjectField : DebugUIFieldDrawer<UnityEngine.Object, DebugUI.ObjectField, DebugStateObject>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override UnityEngine.Object DoGUI(Rect rect, GUIContent label, DebugUI.ObjectField field, DebugStateObject state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for <see cref="DebugUI.ObjectListField"/> Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.ObjectListField))]
    public sealed class DebugUIDrawerObjectListField : DebugUIDrawer
    {
        /// <summary>
        /// Implement this to execute UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        /// <returns>Returns the state of the widget.</returns>
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for MessageBox Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.MessageBox))]
    public sealed class DebugUIDrawerMessageBox : DebugUIDrawer
    {
        /// <summary>
        /// Implement this to execute UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        /// <returns>Returns the state of the widget.</returns>
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            return default;
        }
    }

    /// <summary>
    /// Builtin Drawer for Container Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.Container))]
    public sealed class DebugUIDrawerContainer : DebugUIDrawer
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
        /// Implement this to execute processing after UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void End(DebugUI.Widget widget, DebugState state)
        {
        }
    }

    /// <summary>
    /// Builtin Drawer for Horizontal Box Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.HBox))]
    public sealed class DebugUIDrawerHBox : DebugUIDrawer
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
        /// Implement this to execute processing after UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void End(DebugUI.Widget widget, DebugState state)
        {
        }
    }

    /// <summary>
    /// Builtin Drawer for Vertical Box Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.VBox))]
    public sealed class DebugUIDrawerVBox : DebugUIDrawer
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
        /// Implement this to execute processing after UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void End(DebugUI.Widget widget, DebugState state)
        {
        }
    }

    /// <summary>
    /// Builtin Drawer for Table Debug Items.
    /// </summary>
    [Obsolete("This class is no longer used. #from(6000.5) #breakingFrom(6000.6)", true)]
    [DebugUIDrawer(typeof(DebugUI.Table))]
    public sealed class DebugUIDrawerTable : DebugUIDrawer
    {
        /// <summary>
        /// Implement this to execute UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        /// <returns>Returns the state of the widget.</returns>
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            return default;
        }
    }

    class LegacyStyles
    {
    }

    public partial class MaterialUpgrader
    {
        /// <summary>
        /// Material Upgrader dialog text.
        /// </summary>
        [Obsolete("DialogText has been deprecated. #from(6000.3)")]
        public static class DialogText
        {
            /// <summary>Material Upgrader title.</summary>
            public static readonly string title = "Material Upgrader";
            /// <summary>Material Upgrader proceed.</summary>
            public static readonly string proceed = "Proceed";
            /// <summary>Material Upgrader Ok.</summary>
            public static readonly string ok = "OK";
            /// <summary>Material Upgrader cancel.</summary>
            public static readonly string cancel = "Cancel";
            /// <summary>Material Upgrader no selection message.</summary>
            public static readonly string noSelectionMessage = "You must select at least one material.";
            /// <summary>Material Upgrader project backup message.</summary>
            public static readonly string projectBackMessage = "Make sure to have a project backup before proceeding.";
        }

        /// <summary>
        /// Checking if project folder contains any materials that are not using built-in shaders.
        /// </summary>
        /// <param name="upgraders">List if MaterialUpgraders</param>
        /// <returns>Returns true if at least one material uses a non-built-in shader (ignores Hidden, HDRP and Shader Graph Shaders)</returns>
        [Obsolete("Please directly use ProjectContainsNonAutomaticUpgradePath now. #from(6000.3)")]
        public static bool ProjectFolderContainsNonBuiltinMaterials(List<MaterialUpgrader> upgraders)
        {
            string[] pathsWhiteList = new[]
            {
                "Hidden/",
                "HDRP/",
                "Shader Graphs/"
            };

            foreach (var material in AssetDatabaseHelper.FindAssets<Material>(".mat"))
            {
                if (material.shader.name.ContainsAny(pathsWhiteList))
                    continue;

                if (!IsMaterialUpgradable(upgraders, material))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Callback method that will be called when the Global Preferences for Additional Properties is changed
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [Obsolete("This attribute is not handled anymore. Use Advanced Properties. #from(6000.0)")]
    public sealed class SetAdditionalPropertiesVisibilityAttribute : Attribute
    {
    }

    /// <summary>
    /// This attribute tells the <see cref="VolumeComponentEditor"/> class which type of
    /// <see cref="VolumeComponent"/> it is an editor for. It is used to associate a custom editor
    /// with a specific volume component, enabling the editor to handle its custom properties and settings.
    /// </summary>
    /// <remarks>
    /// When creating a custom editor for a <see cref="VolumeComponent"/>, this attribute must be applied
    /// to the editor class to ensure it targets the appropriate component. This functionality has been deprecated,
    /// and developers are encouraged to use the <see cref="CustomEditor"/> attribute instead for newer versions.
    ///
    /// The attribute specifies which <see cref="VolumeComponent"/> type the editor class is responsible for.
    /// Typically, it is used in conjunction with custom editor UI drawing and handling logic for the specified volume component.
    /// This provides a way for developers to create custom editing tools for their volume components in the Unity Inspector.
    ///
    /// Since Unity 2022.2, this functionality has been replaced by the <see cref="CustomEditor"/> attribute, and as such,
    /// it is advised to update any existing custom editors to use the newer approach.
    /// </remarks>
    /// <seealso cref="VolumeComponentEditor"/>
    /// <seealso cref="CustomEditor"/>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [Obsolete("VolumeComponentEditor property has been deprecated. Please use CustomEditor. #from(2022.2)")]
    public sealed class VolumeComponentEditorAttribute : CustomEditor
    {
        /// <summary>
        /// A type derived from <see cref="VolumeComponent"/> that this editor is responsible for.
        /// </summary>
        /// <remarks>
        /// This field holds the type of the volume component that the editor class will handle.
        /// The type should be a subclass of <see cref="VolumeComponent"/> and is used to associate the editor
        /// with the specific component type.
        /// </remarks>
        public readonly Type componentType;

        /// <summary>
        /// Creates a new <see cref="VolumeComponentEditorAttribute"/> instance.
        /// </summary>
        /// <param name="componentType">A type derived from <see cref="VolumeComponent"/> that the editor is responsible for.</param>
        /// <remarks>
        /// This constructor initializes the attribute with the component type that the editor will target.
        /// The component type is a subclass of <see cref="VolumeComponent"/> and provides the necessary
        /// context for the editor class to function properly within the Unity Editor.
        /// </remarks>
        public VolumeComponentEditorAttribute(Type componentType)
            : base(componentType, true)
        {
            this.componentType = componentType;
        }
    }


    /// <summary>
    /// Interface that should be used with [ScriptableRenderPipelineExtension(type))] attribute to dispatch ContextualMenu calls on the different SRPs
    /// </summary>
    /// <typeparam name="T">This must be a component that require AdditionalData in your SRP</typeparam>
    [Obsolete("The menu items are handled automatically for components with the AdditionalComponentData attribute. #from(2022.2)")]
    public interface IRemoveAdditionalDataContextualMenu<T>
        where T : Component
    {
        /// <summary>
        /// Remove the given component
        /// </summary>
        /// <param name="component">The component to remove</param>
        /// <param name="dependencies">Dependencies.</param>
        void RemoveComponent(T component, IEnumerable<Component> dependencies);
    }

    public static partial class RenderPipelineGlobalSettingsUI
    {
        /// <summary>A collection of GUIContent for use in the inspector</summary>
        [Obsolete("Use ShaderStrippingSettings instead. #from(2023.2).")]
        public static class Styles
        {
            /// <summary>
            /// Global label width
            /// </summary>
            public const int labelWidth = 250;

            /// <summary>
            /// Shader Stripping
            /// </summary>
            public static readonly GUIContent shaderStrippingSettingsLabel = EditorGUIUtility.TrTextContent("Shader Stripping", "Shader Stripping settings");

            /// <summary>
            /// Shader Variant Log Level
            /// </summary>
            public static readonly GUIContent shaderVariantLogLevelLabel = EditorGUIUtility.TrTextContent("Shader Variant Log Level", "Controls the level of logging of shader variant information outputted during the build process. Information appears in the Unity Console when the build finishes.");

            /// <summary>
            /// Export Shader Variants
            /// </summary>
            public static readonly GUIContent exportShaderVariantsLabel = EditorGUIUtility.TrTextContent("Export Shader Variants", "Controls whether to output shader variant information to a file.");

            /// <summary>
            /// Stripping Of Rendering Debugger Shader Variants is enabled
            /// </summary>
            public static readonly GUIContent stripRuntimeDebugShadersLabel = EditorGUIUtility.TrTextContent("Strip Runtime Debug Shaders", "When enabled, all debug display shader variants are removed when you build for the Unity Player. This decreases build time, but disables some features of Rendering Debugger in Player builds.");
        }

        /// <summary>
        /// Draws the shader stripping settinsg
        /// </summary>
        /// <param name="serialized">The serialized global settings</param>
        /// <param name="owner">The owner editor</param>
        /// <param name="additionalShaderStrippingSettings">Pass another drawer if you want to specify additional shader stripping settings</param>
        [Obsolete("Use ShaderStrippingSettings instead. #from(2023.2).")]
        public static void DrawShaderStrippingSettings(ISerializedRenderPipelineGlobalSettings serialized, Editor owner, CoreEditorDrawer<ISerializedRenderPipelineGlobalSettings>.IDrawer additionalShaderStrippingSettings = null)
        {
            CoreEditorUtils.DrawSectionHeader(Styles.shaderStrippingSettingsLabel);

            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            EditorGUILayout.Space();
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.shaderVariantLogLevel, Styles.shaderVariantLogLevelLabel);
                EditorGUILayout.PropertyField(serialized.exportShaderVariants, Styles.exportShaderVariantsLabel);
                EditorGUILayout.PropertyField(serialized.stripDebugVariants, Styles.stripRuntimeDebugShadersLabel);

                additionalShaderStrippingSettings?.Draw(serialized, owner);
            }
            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = oldWidth;
        }
    }

    /// <summary>
    /// Public interface for handling a serialized object of <see cref="UnityEngine.Rendering.RenderPipelineGlobalSettings"/>
    /// </summary>
    [Obsolete("Use ShaderStrippingSettings instead. #from(2023.2).")]
    public interface ISerializedRenderPipelineGlobalSettings
    {
        /// <summary>
        /// The <see cref="SerializedObject"/>
        /// </summary>
        SerializedObject serializedObject { get; }

        /// <summary>
        /// The shader variant log level
        /// </summary>
        SerializedProperty shaderVariantLogLevel { get; }

        /// <summary>
        /// If the shader variants needs to be exported
        /// </summary>
        SerializedProperty exportShaderVariants { get; }

        /// <summary>
        /// If the Runtime Rendering Debugger Debug Variants should be stripped
        /// </summary>
        SerializedProperty stripDebugVariants { get => null; }
    }

    public sealed partial class DefaultVolumeProfileEditor
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseEditor">Editor that displays the content of this class</param>
        /// <param name="profile">VolumeProfile to display</param>
        [Obsolete("Use DefaultVolumeProfileEditor(VolumeProfile, SerializedObject) instead. #from(2023.3)")]
        public DefaultVolumeProfileEditor(Editor baseEditor, VolumeProfile profile)
        {
            m_Profile = profile;
            m_TargetSerializedObject = baseEditor.serializedObject;
        }
    }

    public abstract partial class DefaultVolumeProfileSettingsPropertyDrawer
    {
        /// <summary>
        /// Context menu implementation for Default Volume Profile.
        /// </summary>
        /// <typeparam name="TSetting">Default Volume Profile Settings type</typeparam>
        /// <typeparam name="TRenderPipeline">Render Pipeline type</typeparam>
        [Obsolete("Use DefaultVolumeProfileSettingsPropertyDrawer<T>.DefaultVolumeProfileSettingsContextMenu2<TSetting, TRenderPipeline> instead #from(6000.0)")]
        public abstract class DefaultVolumeProfileSettingsContextMenu<TSetting, TRenderPipeline> : IRenderPipelineGraphicsSettingsContextMenu<TSetting>
            where TSetting : class, IDefaultVolumeProfileSettings
            where TRenderPipeline : RenderPipeline
        {
            /// <summary>
            /// Path where new Default Volume Profile will be created.
            /// </summary>
            [Obsolete("Not used anymore. #from(6000.0)")]
            protected abstract string defaultVolumeProfilePath { get; }

            [Obsolete("Not used anymore. #from(6000.0)")]
            void IRenderPipelineGraphicsSettingsContextMenu<TSetting>.PopulateContextMenu(TSetting setting, PropertyDrawer property, ref GenericMenu menu){ }
        }
    }

    /// <summary>
    /// Builtin Drawer for Maskfield Debug Items.
    /// </summary>
    [DebugUIDrawer(typeof(DebugUI.MaskField))]
    [Obsolete("DebugUI.MaskField has been deprecated and is not longer supported, please use BitField instead. #from(6000.2)", true)]
    public sealed class DebugUIDrawerMaskField : DebugUIFieldDrawer<uint, DebugUI.MaskField, DebugStateUInt>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override uint DoGUI(Rect rect, GUIContent label, DebugUI.MaskField field, DebugStateUInt state)
        {
            return default;
        }
    }

    /// <summary>
    /// Interface to add additional gizmo renders for a <see cref="IVolume"/>
    /// </summary>
    [Obsolete("IVolumeAdditionalGizmo is no longer used. #from(6000.4)", false)]
    public interface IVolumeAdditionalGizmo
    {
        /// <summary>
        /// The type that overrides this additional gizmo
        /// </summary>
        Type type { get; }

        /// <summary>
        /// Additional gizmo draw for <see cref="BoxCollider"/>
        /// </summary>
        /// <param name="scr">The <see cref="IVolume"/></param>
        /// <param name="c">The <see cref="BoxCollider"/></param>
        void OnBoxColliderDraw(IVolume scr, BoxCollider c);

        /// <summary>
        /// Additional gizmo draw for <see cref="SphereCollider"/>
        /// </summary>
        /// <param name="scr">The <see cref="IVolume"/></param>
        /// <param name="c">The <see cref="SphereCollider"/></param>
        void OnSphereColliderDraw(IVolume scr, SphereCollider c);

        /// <summary>
        /// Additional gizmo draw for <see cref="MeshCollider"/>
        /// </summary>
        /// <param name="scr">The <see cref="IVolume"/></param>
        /// <param name="c">The <see cref="MeshCollider"/></param>
        void OnMeshColliderDraw(IVolume scr, MeshCollider c);
    }
}
