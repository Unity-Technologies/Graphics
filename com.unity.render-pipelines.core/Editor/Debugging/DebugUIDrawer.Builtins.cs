using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [DebugUIDrawer(typeof(DebugUI.Value))]
    public sealed class DebugUIDrawerValue : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.Value>(widget);
            var rect = PrepareControlRect();
            EditorGUI.LabelField(rect, EditorGUIUtility.TrTextContent(w.displayName), EditorGUIUtility.TrTextContent(w.GetValue().ToString()));
            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.Button))]
    public sealed class DebugUIDrawerButton : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.Button>(widget);

            var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            if (GUI.Button(rect, w.displayName, EditorStyles.miniButton))
            {
                if (w.action != null)
                    w.action();
            }

            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.BoolField))]
    public sealed class DebugUIDrawerBoolField : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.BoolField>(widget);
            var s = Cast<DebugStateBool>(state);

            EditorGUI.BeginChangeCheck();

            var rect = PrepareControlRect();
            bool value = EditorGUI.Toggle(rect, EditorGUIUtility.TrTextContent(w.displayName), w.GetValue());

            if (EditorGUI.EndChangeCheck())
                Apply(w, s, value);

            return true;
        }
    }
    [DebugUIDrawer(typeof(DebugUI.HistoryBoolField))]
    public sealed class DebugUIDrawerHistoryBoolField : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.HistoryBoolField>(widget);
            var s = Cast<DebugStateBool>(state);

            EditorGUI.BeginChangeCheck();

            var rect = PrepareControlRect();
            var labelRect = rect;
            labelRect.width = EditorGUIUtility.labelWidth;
            const int oneValueWidth = 70;
            var valueRects = new Rect[w.historyDepth + 1];
            for(int i = 0; i < w.historyDepth + 1; i++)
            {
                valueRects[i] = rect;
                valueRects[i].x += EditorGUIUtility.labelWidth + i * oneValueWidth;
                valueRects[i].width = oneValueWidth;
            }
            EditorGUI.LabelField(labelRect, EditorGUIUtility.TrTextContent(w.displayName));
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0; //be at left of rects
            bool value = EditorGUI.Toggle(valueRects[0], w.GetValue());
            using (new EditorGUI.DisabledScope(true))
            {
                for (int i = 0; i < w.historyDepth; i++)
                    EditorGUI.Toggle(valueRects[i + 1], w.GetHistoryValue(i));
            }
            EditorGUI.indentLevel = indent;

            if (EditorGUI.EndChangeCheck())
                Apply(w, s, value);

            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.IntField))]
    public sealed class DebugUIDrawerIntField : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.IntField>(widget);
            var s = Cast<DebugStateInt>(state);

            EditorGUI.BeginChangeCheck();

            var rect = PrepareControlRect();
            int value = w.min != null && w.max != null
                ? EditorGUI.IntSlider(rect, EditorGUIUtility.TrTextContent(w.displayName), w.GetValue(), w.min(), w.max())
                : EditorGUI.IntField(rect, EditorGUIUtility.TrTextContent(w.displayName), w.GetValue());

            if (EditorGUI.EndChangeCheck())
                Apply(w, s, value);

            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.UIntField))]
    public sealed class DebugUIDrawerUIntField : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.UIntField>(widget);
            var s = Cast<DebugStateUInt>(state);

            EditorGUI.BeginChangeCheck();

            // No UIntField so we need to max to 0 ourselves or the value will wrap around
            var rect = PrepareControlRect();
            int tmp = w.min != null && w.max != null
                ? EditorGUI.IntSlider(rect, EditorGUIUtility.TrTextContent(w.displayName), Mathf.Max(0, (int)w.GetValue()), Mathf.Max(0, (int)w.min()), Mathf.Max(0, (int)w.max()))
                : EditorGUI.IntField(rect, EditorGUIUtility.TrTextContent(w.displayName), Mathf.Max(0, (int)w.GetValue()));

            uint value = (uint)Mathf.Max(0, tmp);

            if (EditorGUI.EndChangeCheck())
                Apply(w, s, value);

            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.FloatField))]
    public sealed class DebugUIDrawerFloatField : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.FloatField>(widget);
            var s = Cast<DebugStateFloat>(state);

            EditorGUI.BeginChangeCheck();

            var rect = PrepareControlRect();
            float value = w.min != null && w.max != null
                ? EditorGUI.Slider(rect, EditorGUIUtility.TrTextContent(w.displayName), w.GetValue(), w.min(), w.max())
                : EditorGUI.FloatField(rect, EditorGUIUtility.TrTextContent(w.displayName), w.GetValue());

            if (EditorGUI.EndChangeCheck())
                Apply(w, s, value);

            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.EnumField))]
    public sealed class DebugUIDrawerEnumField : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.EnumField>(widget);
            var s = Cast<DebugStateInt>(state);

            if (w.indexes == null)
                w.InitIndexes();

            EditorGUI.BeginChangeCheck();

            int index = -1;
            int value = w.GetValue();
            if (w.enumNames == null || w.enumValues == null)
            {
                EditorGUILayout.LabelField("Can't draw an empty enumeration.");
            }
            else
            {
                var rect = PrepareControlRect();

                index = w.currentIndex;

                // Fallback just in case, we may be handling sub/sectionned enums here
                if (index < 0)
                    index = 0;

                index = EditorGUI.IntPopup(rect, EditorGUIUtility.TrTextContent(w.displayName), index, w.enumNames, w.indexes);
                value = w.enumValues[index];
            }

            if (EditorGUI.EndChangeCheck())
            {
                Apply(w, s, value);
                if (index > -1)
                    w.currentIndex = index;
            }

            return true;
        }
    }
    [DebugUIDrawer(typeof(DebugUI.HistoryEnumField))]
    public sealed class DebugUIDrawerHistoryEnumField : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.HistoryEnumField>(widget);
            var s = Cast<DebugStateInt>(state);

            if (w.indexes == null)
                w.InitIndexes();

            EditorGUI.BeginChangeCheck();

            int index = -1;
            int value = w.GetValue();
            if (w.enumNames == null || w.enumValues == null)
            {
                EditorGUILayout.LabelField("Can't draw an empty enumeration.");
            }
            else
            {
                var rect = PrepareControlRect();
                index = w.currentIndex;

                // Fallback just in case, we may be handling sub/sectionned enums here
                if (index < 0)
                    index = 0;

                var labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                const int oneValueWidth = 70;
                var valueRects = new Rect[w.historyDepth + 1];
                for (int i = 0; i < w.historyDepth + 1; i++)
                {
                    valueRects[i] = rect;
                    valueRects[i].x += EditorGUIUtility.labelWidth + i * oneValueWidth;
                    valueRects[i].width = oneValueWidth;
                }
                EditorGUI.LabelField(labelRect, EditorGUIUtility.TrTextContent(w.displayName));
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0; //be at left of rects
                index = EditorGUI.IntPopup(valueRects[0], index, w.enumNames, w.indexes);
                value = w.enumValues[index];
                using (new EditorGUI.DisabledScope(true))
                {
                    for (int i = 0; i < w.historyDepth; i++)
                        EditorGUI.IntPopup(valueRects[i + 1], w.GetHistoryValue(i), w.enumNames, w.indexes);
                }
                EditorGUI.indentLevel = indent;
            }

            if (EditorGUI.EndChangeCheck())
            {
                Apply(w, s, value);
                if (index > -1)
                    w.currentIndex = index;
            }

            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.BitField))]
    public sealed class DebugUIDrawerBitField : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.BitField>(widget);
            var s = Cast<DebugStateFlags>(state);

            EditorGUI.BeginChangeCheck();
            Enum value = w.GetValue();
            var rect = PrepareControlRect();
            value = EditorGUI.EnumFlagsField(rect, EditorGUIUtility.TrTextContent(w.displayName), value);

            if (EditorGUI.EndChangeCheck())
                Apply(w, s, value);

            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.Foldout))]
    public sealed class DebugUIDrawerFoldout : DebugUIDrawer
    {
        public override void Begin(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.Foldout>(widget);
            var s = Cast<DebugStateBool>(state);

            EditorGUI.BeginChangeCheck();

            Rect rect = PrepareControlRect();
            bool value = EditorGUI.Foldout(rect, w.GetValue(), EditorGUIUtility.TrTextContent(w.displayName), true);

            Rect drawRect = GUILayoutUtility.GetLastRect();
            if (w.columnLabels != null && value)
            {
                const int oneColumnWidth = 70;
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0; //be at left of rects
                for (int i = 0; i < w.columnLabels.Length; i++)
                {
                    var columnRect = drawRect;
                    columnRect.x += EditorGUIUtility.labelWidth + i * oneColumnWidth;
                    columnRect.width = oneColumnWidth;
                    EditorGUI.LabelField(columnRect, w.columnLabels[i] ?? "", EditorStyles.miniBoldLabel);
                }
                EditorGUI.indentLevel = indent;
            }

            if (EditorGUI.EndChangeCheck())
                Apply(w, s, value);

            EditorGUI.indentLevel++;
        }

        public override bool OnGUI(DebugUI.Widget node, DebugState state)
        {
            var s = Cast<DebugStateBool>(state);
            return s.value;
        }

        public override void End(DebugUI.Widget node, DebugState state)
        {
            EditorGUI.indentLevel--;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.ColorField))]
    public sealed class DebugUIDrawerColorField : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.ColorField>(widget);
            var s = Cast<DebugStateColor>(state);

            EditorGUI.BeginChangeCheck();

            var rect = PrepareControlRect();
            var value = EditorGUI.ColorField(rect, EditorGUIUtility.TrTextContent(w.displayName), w.GetValue(), w.showPicker, w.showAlpha, w.hdr);

            if (EditorGUI.EndChangeCheck())
                Apply(w, s, value);

            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.Vector2Field))]
    public sealed class DebugUIDrawerVector2Field : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.Vector2Field>(widget);
            var s = Cast<DebugStateVector2>(state);

            EditorGUI.BeginChangeCheck();

            var value = EditorGUILayout.Vector2Field(w.displayName, w.GetValue());

            if (EditorGUI.EndChangeCheck())
                Apply(w, s, value);

            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.Vector3Field))]
    public sealed class DebugUIDrawerVector3Field : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.Vector3Field>(widget);
            var s = Cast<DebugStateVector3>(state);

            EditorGUI.BeginChangeCheck();

            var value = EditorGUILayout.Vector3Field(w.displayName, w.GetValue());

            if (EditorGUI.EndChangeCheck())
                Apply(w, s, value);

            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.Vector4Field))]
    public sealed class DebugUIDrawerVector4Field : DebugUIDrawer
    {
        public override bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            var w = Cast<DebugUI.Vector4Field>(widget);
            var s = Cast<DebugStateVector4>(state);

            EditorGUI.BeginChangeCheck();

            var value = EditorGUILayout.Vector4Field(w.displayName, w.GetValue());

            if (EditorGUI.EndChangeCheck())
                Apply(w, s, value);

            return true;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.Container))]
    public sealed class DebugUIDrawerContainer : DebugUIDrawer
    {
        public override void Begin(DebugUI.Widget widget, DebugState state)
        {
            if (!string.IsNullOrEmpty(widget.displayName))
                EditorGUILayout.LabelField(widget.displayName, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
        }

        public override void End(DebugUI.Widget widget, DebugState state)
        {
            EditorGUI.indentLevel--;
        }
    }

    [DebugUIDrawer(typeof(DebugUI.HBox))]
    public sealed class DebugUIDrawerHBox : DebugUIDrawer
    {
        public override void Begin(DebugUI.Widget widget, DebugState state)
        {
            EditorGUILayout.BeginHorizontal();
        }

        public override void End(DebugUI.Widget widget, DebugState state)
        {
            EditorGUILayout.EndHorizontal();
        }
    }

    [DebugUIDrawer(typeof(DebugUI.VBox))]
    public sealed class DebugUIDrawerVBox : DebugUIDrawer
    {
        public override void Begin(DebugUI.Widget widget, DebugState state)
        {
            EditorGUILayout.BeginVertical();
        }

        public override void End(DebugUI.Widget widget, DebugState state)
        {
            EditorGUILayout.EndVertical();
        }
    }
}
