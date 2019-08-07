using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    partial class SplatNode
    {
        private const int kDefaultValueFieldWidth = 180;
        private const int kListFieldSpace = 5;
        private ReorderableList m_ReorderableList;

        private (MaterialSlot inputSlot, MaterialSlot outputSlot) CreateInputOutputSlot(int splatSlotIndex)
        {
            var inputSlot = new DynamicVectorMaterialSlot(splatSlotIndex * 2 + kSplatInputSlotIdStart, m_SplatSlotNames[splatSlotIndex], $"Input{splatSlotIndex}", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment);
            var outputSlot = new DynamicVectorMaterialSlot(splatSlotIndex * 2 + kSplatInputSlotIdStart + 1, m_SplatSlotNames[splatSlotIndex], $"Output{splatSlotIndex}", SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment);
            return (inputSlot, outputSlot);
        }

        private void AddBlendWeightAndCondtionSlots()
        {
            AddSlot(new Vector1MaterialSlot(kBlendWeightInputSlotId, "Blend Weight", "BlendWeight", SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
            if (m_Conditional)
                AddSlot(new SplatConditionsInputMaterialSlot(kConditionInputSlotId, "Sample Condition", "Condition", kBlendWeightInputSlotId));
        }

        private void CreateSplatSlots(IReadOnlyList<MaterialSlot> oldInputSlots, IReadOnlyList<MaterialSlot> oldOutputSlots)
        {
            for (int i = 0; i < m_SplatSlotNames.Count; ++i)
            {
                var (newInputSlot, newOutputSlot) = CreateInputOutputSlot(i);
                if (oldInputSlots != null)
                    newInputSlot.CopyValuesFrom(oldInputSlots[i]);
                if (oldOutputSlots != null)
                    newOutputSlot.CopyValuesFrom(oldOutputSlots[i]);
                AddSlot(newInputSlot);
                AddSlot(newOutputSlot);
            }
        }

        private List<(IEnumerable<IEdge> inputEdges, IEnumerable<IEdge> outputEdges)> SaveSplatSlotEdges()
        {
            var edges = new List<(IEnumerable<IEdge> inputEdges, IEnumerable<IEdge> outputEdges)>();
            for (int i = 0; i < m_SplatSlotNames.Count; ++i)
            {
                var inputSlot = new SlotReference(guid, i * 2 + kSplatInputSlotIdStart);
                var outputSlot = new SlotReference(guid, i * 2 + kSplatInputSlotIdStart + 1);
                edges.Add((owner.GetEdges(inputSlot), owner.GetEdges(outputSlot)));
            }
            return edges;
        }

        private void MoveBlendWeightSlotsToLast()
        {
            var edges = owner.GetEdges(guid, kBlendWeightInputSlotId, kConditionInputSlotId);
            AddBlendWeightAndCondtionSlots();
            foreach (var edge in edges)
                owner.Connect(edge.outputSlot, edge.inputSlot);
        }

        private void RecreateSplatSlots(IReadOnlyList<MaterialSlot> oldInputSlots, IReadOnlyList<MaterialSlot> oldOutputSlots,
            IReadOnlyList<(IEnumerable<IEdge> inputEdges, IEnumerable<IEdge> outputEdges)> oldEdges, IReadOnlyList<int> newIndexToOldMapping)
        {
            RemoveSlotsNameNotMatching(new[] { kBlendWeightInputSlotId, kConditionInputSlotId }, true);
            CreateSplatSlots(oldInputSlots, oldOutputSlots);

            for (int i = 0; i < m_SplatSlotNames.Count; ++i)
            {
                var oi = newIndexToOldMapping[i];
                foreach (var edge in oldEdges[oi].inputEdges)
                {
                    var inputSlotRef = new SlotReference(guid, i * 2 + kSplatInputSlotIdStart);
                    owner.Connect(edge.outputSlot, inputSlotRef);
                }
                foreach (var edge in oldEdges[oi].outputEdges)
                {
                    var outputSlotRef = new SlotReference(guid, i * 2 + kSplatInputSlotIdStart + 1);
                    owner.Connect(outputSlotRef, edge.inputSlot);
                }
            }

            MoveBlendWeightSlotsToLast();
        }

        public VisualElement CreateSettingsElement()
        {
            var ps = new PropertySheet();

            ps.Add(new PropertyRow(new Label("Conditional")), row =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Conditional;
                    toggle.OnToggleChanged(ChangeConditional);
                });
            });

            m_ReorderableList = new ReorderableList(m_SplatSlotNames, typeof(string), true, true, true, true);

            m_ReorderableList.drawHeaderCallback = rect =>
            {
                var nameRect = m_Conditional ? new Rect(rect.x, rect.y, rect.width - kDefaultValueFieldWidth, EditorGUIUtility.singleLineHeight) : rect;
                EditorGUI.LabelField(nameRect, "Name");
                if (m_Conditional)
                {
                    var defaultValueRect = new Rect(nameRect.xMax + kListFieldSpace, rect.y, rect.width - nameRect.width - kListFieldSpace * 2, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(defaultValueRect, "Default Value");
                }
            };

            m_ReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                EditorGUI.BeginChangeCheck();
                var nameRect = m_Conditional ? new Rect(rect.x, rect.y, rect.width - kDefaultValueFieldWidth, EditorGUIUtility.singleLineHeight) : rect;
                int id = GUIUtility.GetControlID("NameField".GetHashCode(), FocusType.Keyboard, nameRect);
                var name = m_SplatSlotNames[index] = EditorGUI.DelayedTextField(nameRect, GUIContent.none, id, m_SplatSlotNames[index], EditorStyles.label);
                if (EditorGUI.EndChangeCheck())
                {
                    var inputSplatSlots = EnumerateSplatInputSlots().ToList();
                    var outputSplatSlots = EnumerateSplatOutputSlots().ToList();
                    var splatEdges = SaveSplatSlotEdges();

                    RecreateSplatSlots(inputSplatSlots, outputSplatSlots, splatEdges, Enumerable.Range(0, inputSplatSlots.Count).ToList());
                }

                if (GUIUtility.keyboardControl == id)
                    m_ReorderableList.index = index;

                if (m_Conditional)
                {
                    EditorGUI.BeginChangeCheck();
                    var defaultValueRect = new Rect(nameRect.xMax + kListFieldSpace, rect.y, rect.width - nameRect.width - kListFieldSpace * 2, EditorGUIUtility.singleLineHeight);
                    int idVec4 = GUIUtility.GetControlID("DefaultValueField".GetHashCode(), FocusType.Keyboard, defaultValueRect);
                    var splatSlot = FindInputSlot<DynamicVectorMaterialSlot>(kSplatInputSlotIdStart + index * 2);
                    var xyzw = new[] { splatSlot.value.x, splatSlot.value.y, splatSlot.value.z, splatSlot.value.w };
                    EditorGUI.MultiFloatField(defaultValueRect, new[] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z"), new GUIContent("W") }, xyzw);
                    if (EditorGUI.EndChangeCheck())
                    {
                        splatSlot.value = new Vector4(xyzw[0], xyzw[1], xyzw[2], xyzw[3]);
                        Dirty(ModificationScope.Node); // update the default values on the slot
                    }

                    // TODO: keyboardControl never equals idVec4.
                    if (GUIUtility.keyboardControl == idVec4)
                        m_ReorderableList.index = index;
                }
            };

            m_ReorderableList.onAddCallback += list =>
            {
                owner.owner.RegisterCompleteObjectUndo("Add Splat slot");

                var name = "New";
                int counter = 0;
                while (m_SplatSlotNames.Contains(name))
                    name = $"New {counter++}";

                m_SplatSlotNames.Add(name);
                var (newInputSlot, newOutputSlot) = CreateInputOutputSlot(m_SplatSlotNames.Count - 1);
                AddSlot(newInputSlot);
                AddSlot(newOutputSlot);

                MoveBlendWeightSlotsToLast();

                // Select the new slot, then validate the node
                list.index = m_SplatSlotNames.Count - 1;
            };

            m_ReorderableList.onRemoveCallback += list =>
            {
                owner.owner.RegisterCompleteObjectUndo("Remove Splat slot");

                var inputSplatSlots = EnumerateSplatInputSlots().ToList();
                var outputSplatSlots = EnumerateSplatOutputSlots().ToList();
                var splatEdges = SaveSplatSlotEdges();

                m_SplatSlotNames.RemoveAt(list.index);
                inputSplatSlots.RemoveAt(list.index);
                outputSplatSlots.RemoveAt(list.index);

                var newIndexToOldMapping = Enumerable.Range(0, inputSplatSlots.Count + 1).ToList();
                newIndexToOldMapping.RemoveAt(list.index);

                RecreateSplatSlots(inputSplatSlots, outputSplatSlots, splatEdges, newIndexToOldMapping);
            };

            m_ReorderableList.onReorderCallbackWithDetails += (list, oldIndex, newIndex) =>
            {
                owner.owner.RegisterCompleteObjectUndo("Reorder Splat slot");

                var inputSplatSlots = EnumerateSplatInputSlots().ToList();
                var outputSplatSlots = EnumerateSplatOutputSlots().ToList();
                var splatEdges = SaveSplatSlotEdges();

                var newIndexToOldMapping = Enumerable.Range(0, inputSplatSlots.Count).ToList();
                newIndexToOldMapping.RemoveAt(oldIndex);
                newIndexToOldMapping.Insert(newIndex, oldIndex);

                RecreateSplatSlots(inputSplatSlots, outputSplatSlots, splatEdges, newIndexToOldMapping);
            };

            ps.Add(new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                m_ReorderableList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                    ValidateNode();
            }));

            return ps;
        }

        void ChangeConditional(ChangeEvent<bool> evt)
        {
            owner.owner.RegisterCompleteObjectUndo("Conditional Change");
            m_Conditional = evt.newValue;
            if (m_Conditional)
                AddSlot(new SplatConditionsInputMaterialSlot(kConditionInputSlotId, "Sample Condition", "Condition", kBlendWeightInputSlotId));
            else
                RemoveSlot(kConditionInputSlotId);
            owner?.ClearErrorsForNode(this);
            ValidateNode();
        }
    }
}
