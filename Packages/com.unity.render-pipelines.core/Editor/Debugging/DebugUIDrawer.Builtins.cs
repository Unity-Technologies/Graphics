using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Builtin Drawer for Value Debug Items.
    /// </summary>
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
            EditorGUI.LabelField(rect, label, EditorGUIUtility.TrTextContent(field.FormatString(field.GetValue())));
        }
    }

    /// <summary>
    /// Builtin Drawer for ValueTuple Debug Items.
    /// </summary>
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
            EditorGUI.PrefixLabel(rect, label);

            // Following layout should match DebugUIDrawerFoldout to make column labels align
            Rect drawRect = GUILayoutUtility.GetLastRect();

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0; //be at left of rects
            for (int i = 0; i < field.numElements; i++)
            {
                var columnRect = drawRect;
                columnRect.x += EditorGUIUtility.labelWidth + i * DebugWindow.Styles.foldoutColumnWidth;
                columnRect.width = DebugWindow.Styles.foldoutColumnWidth;
                var value = field.values[i].GetValue();

                var style = EditorStyles.label;
                if (Convert.ToSingle(value) == 0)
                    style = DebugWindow.Styles.labelWithZeroValueStyle;

                EditorGUI.LabelField(columnRect, field.values[i].FormatString(value), style);
            }
            EditorGUI.indentLevel = indent;
        }
    }

    /// <summary>
    /// Builtin Drawer for ProgressBarValue Debug Items.
    /// </summary>
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
            var progressBarRect = EditorGUI.PrefixLabel(rect, label);
            float value = (float)field.GetValue();
            EditorGUI.ProgressBar(progressBarRect, value, field.FormatString(value));
        }
    }

    /// <summary>
    /// Builtin Drawer for Button Debug Items.
    /// </summary>
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
            rect = EditorGUI.IndentedRect(rect);
            if (GUI.Button(rect, label, EditorStyles.miniButton))
            {
                if (field.action != null)
                    field.action();
            }
        }
    }

    /// <summary>
    /// Builtin Drawer for Boolean Debug Items.
    /// </summary>
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
            return EditorGUI.Toggle(rect, label, field.GetValue());
        }
    }

    /// <summary>
    /// Builtin Drawer for History Boolean Debug Items.
    /// </summary>
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
            var labelRect = rect;
            labelRect.width = EditorGUIUtility.labelWidth;
            const int oneValueWidth = 70;
            var valueRects = new Rect[field.historyDepth + 1];
            for (int i = 0; i < field.historyDepth + 1; i++)
            {
                valueRects[i] = rect;
                valueRects[i].x += EditorGUIUtility.labelWidth + i * oneValueWidth;
                valueRects[i].width = oneValueWidth;
            }
            EditorGUI.LabelField(labelRect, label);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0; //be at left of rects
            bool value = EditorGUI.Toggle(valueRects[0], field.GetValue());
            using (new EditorGUI.DisabledScope(true))
            {
                for (int i = 0; i < field.historyDepth; i++)
                    EditorGUI.Toggle(valueRects[i + 1], field.GetHistoryValue(i));
            }
            EditorGUI.indentLevel = indent;
            return value;
        }
    }

    /// <summary>
    /// Builtin Drawer for Integer Debug Items.
    /// </summary>
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
            return field.min != null && field.max != null
                ? EditorGUI.IntSlider(rect, label, field.GetValue(), field.min(), field.max())
                : EditorGUI.IntField(rect, label, field.GetValue());
        }
    }

    /// <summary>
    /// Builtin Drawer for Unsigned Integer Debug Items.
    /// </summary>
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
            // No UIntField so we need to max to 0 ourselves or the value will wrap around
            int tmp = field.min != null && field.max != null
                ? EditorGUI.IntSlider(rect, label, Mathf.Max(0, (int)field.GetValue()), Mathf.Max(0, (int)field.min()), Mathf.Max(0, (int)field.max()))
                : EditorGUI.IntField(rect, label, Mathf.Max(0, (int)field.GetValue()));

            return (uint)Mathf.Max(0, tmp);
        }
    }

    /// <summary>
    /// Builtin Drawer for Float Debug Items.
    /// </summary>
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
            return field.min != null && field.max != null
                ? EditorGUI.Slider(rect, label, field.GetValue(), field.min(), field.max())
                : EditorGUI.FloatField(rect, label, field.GetValue());
        }
    }

    /// <summary>
    /// Builtin Drawer for Enum Debug Items.
    /// </summary>
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
            int index = Mathf.Max(0, field.currentIndex); // Fallback just in case, we may be handling sub/sectioned enums here
            int value = field.GetValue();

            if (field.enumNames == null || field.enumValues == null)
            {
                EditorGUI.LabelField(rect, label, "Can't draw an empty enumeration.");
            }
            else if (field.enumNames.Length != field.enumValues.Length)
            {
                EditorGUI.LabelField(rect, label, "Invalid data");
            }
            else
            {
                index = EditorGUI.IntPopup(rect, label, index, field.enumNames, field.indexes);
                value = field.enumValues[index];
            }

            return value;
        }
    }

    /// <summary>
    /// Builtin Drawer for Object Popup Fields Items.
    /// </summary>
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
            var selectedValue = field.GetValue();

            rect = EditorGUI.PrefixLabel(rect, label);

            var elements = field.getObjects();
            if (elements?.Any() ?? false)
            {
                var elementsArrayNames = elements.Select(e => e.name).ToArray();
                var elementsArrayIndices = Enumerable.Range(0, elementsArrayNames.Length).ToArray();
                var selectedIndex = selectedValue != null ? Array.IndexOf(elementsArrayNames, selectedValue.name) : 0;
                var newSelectedIndex = EditorGUI.IntPopup(rect, selectedIndex, elementsArrayNames, elementsArrayIndices);
                if (selectedIndex != newSelectedIndex)
                    selectedValue = elements.ElementAt(newSelectedIndex);
            }
            else
            {
                EditorGUI.LabelField(rect, "Can't draw an empty enumeration.");
            }

            return selectedValue;
        }
    }

    /// <summary>
    /// Builtin Drawer for History Enum Debug Items.
    /// </summary>
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
            int index = -1;
            int value = field.GetValue();
            if (field.enumNames == null || field.enumValues == null)
            {
                EditorGUILayout.LabelField("Can't draw an empty enumeration.");
            }
            else
            {
                index = field.currentIndex;

                // Fallback just in case, we may be handling sub/sectionned enums here
                if (index < 0)
                    index = 0;

                var labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                const int oneValueWidth = 70;
                var valueRects = new Rect[field.historyDepth + 1];
                for (int i = 0; i < field.historyDepth + 1; i++)
                {
                    valueRects[i] = rect;
                    valueRects[i].x += EditorGUIUtility.labelWidth + i * oneValueWidth;
                    valueRects[i].width = oneValueWidth;
                }
                EditorGUI.LabelField(labelRect, label);
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0; //be at left of rects
                index = EditorGUI.IntPopup(valueRects[0], index, field.enumNames, field.indexes);
                value = field.enumValues[index];
                using (new EditorGUI.DisabledScope(true))
                {
                    for (int i = 0; i < field.historyDepth; i++)
                        EditorGUI.IntPopup(valueRects[i + 1], field.GetHistoryValue(i), field.enumNames, field.indexes);
                }
                EditorGUI.indentLevel = indent;

                value = field.enumValues[index];
            }

            return value;
        }
    }

    /// <summary>
    /// Builtin Drawer for Bitfield Debug Items.
    /// </summary>
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
            Enum value = field.GetValue();

            // Skip first element (with value 0) because EditorGUI.MaskField adds a 'Nothing' field anyway
            var enumNames = new string[field.enumNames.Length - 1];
            for (int i = 0; i < enumNames.Length; i++)
                enumNames[i] = field.enumNames[i + 1].text;
            var index = EditorGUI.MaskField(rect, label, (int)Convert.ToInt32(value), enumNames);
            value = Enum.Parse(value.GetType(), index.ToString()) as Enum;

            return value;
        }
    }

    /// <summary>
    /// Builtin Drawer for Foldout Debug Items.
    /// </summary>
    [DebugUIDrawer(typeof(DebugUI.Foldout))]
    public sealed class DebugUIDrawerFoldout : DebugUIDrawer
    {
        const int k_HeaderVerticalMargin = 2;

        /// <summary>
        /// Implement this to execute processing before UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void Begin(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.Foldout>(widget);
            var s = Cast<DebugStateBool>(state);

            var title = EditorGUIUtility.TrTextContent(w.displayName, w.tooltip);

            Action<GenericMenu> fillContextMenuAction = null;

            if (w.contextMenuItems != null)
            {
                fillContextMenuAction = menu =>
                {
                    foreach (var item in w.contextMenuItems)
                    {
                        menu.AddItem(EditorGUIUtility.TrTextContent(item.displayName), false, () => item.action.Invoke());
                    }
                };
            }

            bool previousValue = (bool)w.GetValue();
            bool value = CoreEditorUtils.DrawHeaderFoldout(title, previousValue, isTitleHeader: w.isHeader, customMenuContextAction: fillContextMenuAction);

            if (previousValue != value)
                Apply(w, s, value);

            Rect drawRect = GUILayoutUtility.GetLastRect();
            if (w.columnLabels != null && value)
            {
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0; //be at left of rects

                if (w.isHeader) // display column labels on a separate row for header-styled foldouts
                {
                    drawRect = GUILayoutUtility.GetRect(1f, 1f, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
                    drawRect.x -= EditorGUIUtility.labelWidth / 2;
                }

                for (int i = 0; i < w.columnLabels.Length; i++)
                {
                    var columnRect = drawRect;
                    columnRect.x += EditorGUIUtility.labelWidth + i * DebugWindow.Styles.foldoutColumnWidth;
                    columnRect.width = DebugWindow.Styles.foldoutColumnWidth;
                    string label = w.columnLabels[i] ?? "";
                    string tooltip = w.columnTooltips?.ElementAtOrDefault(i) ?? "";
                    EditorGUI.LabelField(columnRect, EditorGUIUtility.TrTextContent(label, tooltip), EditorStyles.miniBoldLabel);
                }
                EditorGUI.indentLevel = indent;
            }

            EditorGUI.indentLevel++;
        }

        /// <summary>
        /// OnGUI implementation for Foldout DebugUIDrawer.
        /// </summary>
        /// <param name="widget">DebugUI Widget.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        /// <returns>The state of the widget.</returns>
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.Foldout>(widget);
            return w.opened;
        }

        /// <summary>
        /// End implementation for Foldout DebugUIDrawer.
        /// </summary>
        /// <param name="widget">DebugUI Widget.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void End(DebugUI.Widget widget, DebugState state)
        {
            EditorGUI.indentLevel--;
            var w = Cast<DebugUI.Foldout>(widget);
            if (w.isHeader)
                GUILayout.Space(k_HeaderVerticalMargin);
        }
    }

    /// <summary>
    /// Builtin Drawer for Color Debug Items.
    /// </summary>
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
            return EditorGUI.ColorField(rect, label, field.GetValue(), field.showPicker, field.showAlpha, field.hdr);
        }
    }

    /// <summary>
    /// Builtin Drawer for Vector2 Debug Items.
    /// </summary>
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
            return EditorGUILayout.Vector2Field(label, field.GetValue());
        }
    }

    /// <summary>
    /// Builtin Drawer for Vector3 Debug Items.
    /// </summary>
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
            return EditorGUILayout.Vector3Field(label, field.GetValue());
        }
    }

    /// <summary>
    /// Builtin Drawer for Vector4 Debug Items.
    /// </summary>
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
            return EditorGUILayout.Vector4Field(label, field.GetValue());
        }
    }

    /// <summary>
    /// Builtin Drawer for <see cref="DebugUI.ObjectField"/> items.
    /// </summary>
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
            return EditorGUI.ObjectField(rect, label, field.GetValue(), field.type, true);
        }
    }

    /// <summary>
    /// Builtin Drawer for <see cref="DebugUI.ObjectListField"/> Items.
    /// </summary>
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
            var w = Cast<DebugUI.ObjectListField>(widget);
            var objects = w.GetValue();

            float height = Math.Max(objects != null ? objects.Length : 0, 1) * DebugWindow.Styles.singleRowHeight;
            var rect = PrepareControlRect(height);

            rect = EditorGUI.PrefixLabel(rect, EditorGUIUtility.TrTextContent(widget.displayName));

            EditorGUI.BeginChangeCheck();
            DoObjectList(rect, w, objects);
            if (EditorGUI.EndChangeCheck())
                Apply(w, state, objects);

            return true;
        }

        internal static void DoObjectList(Rect rect, DebugUI.ObjectListField widget, UnityEngine.Object[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                EditorGUI.LabelField(rect, GUIContent.none, EditorGUIUtility.TrTextContent("Empty"));
                return;
            }

            rect.height = EditorGUIUtility.singleLineHeight;
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i] = EditorGUI.ObjectField(rect, GUIContent.none, objects[i], widget.type, true);
                rect.y += DebugWindow.Styles.singleRowHeight;
            }
        }
    }

    /// <summary>
    /// Builtin Drawer for MessageBox Items.
    /// </summary>
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
            var w = Cast<DebugUI.MessageBox>(widget);

            var type = w.style switch
            {
                DebugUI.MessageBox.Style.Info => MessageType.Info,
                DebugUI.MessageBox.Style.Warning => MessageType.Warning,
                DebugUI.MessageBox.Style.Error => MessageType.Error,
                _ => MessageType.None
            };

            EditorGUILayout.HelpBox(w.message, type);

            return true;
        }
    }

    /// <summary>
    /// Builtin Drawer for Container Debug Items.
    /// </summary>
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
            var w = Cast<DebugUI.Container>(widget);
            if (!w.hideDisplayName)
                EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent(widget.displayName, widget.tooltip), EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
        }

        /// <summary>
        /// Implement this to execute processing after UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void End(DebugUI.Widget widget, DebugState state)
        {
            EditorGUI.indentLevel--;
        }
    }

    /// <summary>
    /// Builtin Drawer for Horizontal Box Debug Items.
    /// </summary>
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
            EditorGUILayout.BeginHorizontal();
        }
        /// <summary>
        /// Implement this to execute processing after UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void End(DebugUI.Widget widget, DebugState state)
        {
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Builtin Drawer for Vertical Box Debug Items.
    /// </summary>
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
            EditorGUILayout.BeginVertical();
        }

        /// <summary>
        /// Implement this to execute processing after UI rendering.
        /// </summary>
        /// <param name="widget">Widget that is going to be rendered.</param>
        /// <param name="state">Debug State associated with the Debug Item.</param>
        public override void End(DebugUI.Widget widget, DebugState state)
        {
            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// Builtin Drawer for Table Debug Items.
    /// </summary>
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
            const float k_ScrollBarHeight = 15;

            var w = Cast<DebugUI.Table>(widget);
            var header = w.Header;
            var visible = header.state.visibleColumns;

            float contentHeight = 0.0f;
            foreach (DebugUI.Table.Row row in w.children)
                contentHeight += row != null ? GetRowHeight(row, visible) : EditorGUIUtility.singleLineHeight;

            // Put some space before the array
            PrepareControlRect(EditorGUIUtility.singleLineHeight * 0.5f);

            // Draw an outline around the table
            var rect = EditorGUI.IndentedRect(PrepareControlRect(header.height + contentHeight + k_ScrollBarHeight));
            rect = DrawOutline(rect);

            // Compute rects
            var headerRect = new Rect(rect.x, rect.y, rect.width, header.height);
            var contentRect = new Rect(rect.x, headerRect.yMax, rect.width, rect.height - headerRect.height);
            var viewRect = new Rect(contentRect.x, contentRect.y, header.state.widthOfAllVisibleColumns, contentRect.height);
            var rowRect = contentRect;
            viewRect.height -= k_ScrollBarHeight;

            // Show header
            header.OnGUI(headerRect, Mathf.Max(w.scroll.x, 0f));

            // Show array content
            w.scroll = GUI.BeginScrollView(contentRect, w.scroll, viewRect);
            {
                var columns = header.state.columns;
                for (int r = 0; r < w.children.Count; r++)
                {
                    var row = Cast<DebugUI.Container>(w.children[r]);
                    rowRect.x = contentRect.x;
                    rowRect.width = columns[0].width;
                    rowRect.height = (row is DebugUI.Table.Row tableRow) ? GetRowHeight(tableRow, visible) : EditorGUIUtility.singleLineHeight;

                    rowRect.xMin += 2;
                    rowRect.xMax -= 2;
                    EditorGUI.LabelField(rowRect, GUIContent.none, EditorGUIUtility.TrTextContent(row.displayName), DebugWindow.Styles.centeredLeft);
                    rowRect.xMin -= 2;
                    rowRect.xMax += 2;

                    using (new EditorGUI.DisabledScope(w.isReadOnly))
                    {
                        for (int c = 1; c < visible.Length; c++)
                        {
                            rowRect.x += rowRect.width;
                            rowRect.width = columns[visible[c]].width;
                            if (!row.isHidden)
                                DisplayChild(rowRect, row.children[visible[c] - 1]);
                        }
                        rowRect.y += rowRect.height;
                    }
                }
            }
            GUI.EndScrollView(false);

            return false;
        }

        internal float GetRowHeight(DebugUI.Table.Row row, int[] visibleColumns)
        {
            float height = EditorGUIUtility.singleLineHeight;
            for (int c = 1; c < visibleColumns.Length; c++)
            {
                var child = row.children[visibleColumns[c] - 1] as DebugUI.ObjectListField;
                if (child == null || child.GetValue() == null)
                    continue;
                height = Mathf.Max(height, child.GetValue().Length * DebugWindow.Styles.singleRowHeight);
            }
            return height;
        }

        internal Rect DrawOutline(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
                return rect;

            float size = 1.0f;
            var color = EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f);

            Color orgColor = GUI.color;
            GUI.color = GUI.color * color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - size, rect.width, size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - size, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);

            GUI.color = orgColor;
            return new Rect(rect.x + size, rect.y + size, rect.width - 2 * size, rect.height - 2 * size);
        }

        internal void DisplayChild(Rect rect, DebugUI.Widget child)
        {
            rect.xMin += 2;
            rect.xMax -= 2;

            if (child.isHidden)
            {
                EditorGUI.LabelField(rect, "-");
            }
            else
            {
                if (child.GetType() == typeof(DebugUI.Value))
                {
                    var widget = Cast<DebugUI.Value>(child);
                    EditorGUI.LabelField(rect, GUIContent.none, EditorGUIUtility.TrTextContent(widget.GetValue().ToString()));
                }
                else if (child.GetType() == typeof(DebugUI.ColorField))
                {
                    var widget = Cast<DebugUI.ColorField>(child);
                    EditorGUI.ColorField(rect, GUIContent.none, widget.GetValue(), false, widget.showAlpha, widget.hdr);
                }
                else if (child.GetType() == typeof(DebugUI.BoolField))
                {
                    var widget = Cast<DebugUI.BoolField>(child);
                    EditorGUI.Toggle(rect, GUIContent.none, widget.GetValue());
                }
                else if (child.GetType() == typeof(DebugUI.ObjectField))
                {
                    var widget = Cast<DebugUI.ObjectField>(child);
                    EditorGUI.ObjectField(rect, GUIContent.none, widget.GetValue(), widget.type, true);
                }
                else if (child.GetType() == typeof(DebugUI.ObjectListField))
                {
                    var widget = Cast<DebugUI.ObjectListField>(child);
                    DebugUIDrawerObjectListField.DoObjectList(rect, widget, widget.GetValue());
                }
            }
        }
    }
}
