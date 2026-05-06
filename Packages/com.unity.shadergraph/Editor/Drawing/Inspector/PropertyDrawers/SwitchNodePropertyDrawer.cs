using System;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(SwitchNode))]
    class SwitchNodeNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
    {
        private SwitchNode node;

        internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            node = nodeBase as SwitchNode;

            if (node.UpstreamFloatEnumProperty != null)
                parentElement.Add(new HelpBoxRow($"Enum cases found in property '{node.UpstreamFloatEnumProperty.displayName}'.", MessageType.Info));

            var intModeToggle = new Toggle("Floor Predicate") { value = node.m_floorPredicate };
            intModeToggle.OnToggleChanged((evt) =>
            {
                node.owner.owner.RegisterCompleteObjectUndo("Toggle Change");
                node.m_floorPredicate = evt.newValue;
                node.Dirty(ModificationScope.Node);
            });

            parentElement.Add(intModeToggle);

            // We are reading enum cases from a property, so we won't display the list view.
            if (node.UpstreamFloatEnumProperty != null)
                return;

            var listView = new ReorderableListView<SwitchNode.EntryCase>(node.m_cases, "Conditions");

            listView.OnNewItemCallback +=
                () => new SwitchNode.EntryCase { comparisonType = ComparisonType.Equal, threshold = node.m_cases.Count };

            listView.OnBeforeChangeCallback +=
                (ReorderableListView<SwitchNode.EntryCase>.ListActionType changeType) =>
                {
                    node.owner.owner.RegisterCompleteObjectUndo($"{changeType} Switch Condition");
                };

            listView.OnChangeCallback +=
                (ReorderableListView<SwitchNode.EntryCase>.ListActionType changeType) =>
                {
                    node.Dirty(ModificationScope.Topological);
                    node.UpdateNodeAfterDeserialization();
                    inspectorUpdateDelegate();
                };

            listView.DrawItemCallback += (Rect rect, int idx) =>
            {
                EditorGUI.BeginChangeCheck();
                var data = node.m_cases[idx];
                var trirect = TriRectSplit(rect, 0);
                EditorGUI.LabelField(trirect.a, $"Case {(char)(idx + 'A')} ");
                data.comparisonType = (ComparisonType)EditorGUI.EnumPopup(trirect.b, (ComparisonType)data.comparisonType);
                data.threshold = EditorGUI.DelayedFloatField(trirect.c, data.threshold);
                
                if (EditorGUI.EndChangeCheck())
                {
                    node.owner.owner.RegisterCompleteObjectUndo("switch Entry Value Change");
                    node.m_cases[idx] = data;
                    node.Dirty(ModificationScope.Node);
                    node.UpdateNodeAfterDeserialization();
                }
            };

            parentElement.Add(listView);
        }

        private static (Rect pad, Rect a, Rect b, Rect c) TriRectSplit(Rect rect, float padding)
        {
            var width = rect.width - padding;

            var P = rect.xMin;
            var A = P + padding;
            var B = rect.xMin + width / 3;
            var C = B + width / 3;
            var D = rect.xMax;

            Rect p = rect;
            Rect a = rect;
            Rect b = rect;
            Rect c = rect;

            p.xMin = P;
            p.xMax = A;
            a.xMin = A;
            a.xMax = B;
            b.xMin = B;
            b.xMax = C;
            c.xMin = C;
            c.xMax = D;

            return (p, a, b, c);
        }
    }
}
