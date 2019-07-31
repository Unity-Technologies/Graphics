using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    partial class SplatNode
    {
        private ReorderableList m_ReorderableList;

        private (MaterialSlot inputSlot, MaterialSlot outputSlot) CreateInputOutputSlot(int splatSlotIndex)
        {
            var inputSlot = new DynamicVectorMaterialSlot(splatSlotIndex * 2 + 1, m_SplatSlotNames[splatSlotIndex], $"Input{splatSlotIndex}", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment);
            var outputSlot = new DynamicVectorMaterialSlot(splatSlotIndex * 2 + 2, m_SplatSlotNames[splatSlotIndex], $"Output{splatSlotIndex}", SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment);
            return (inputSlot, outputSlot);
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
                var inputSlot = new SlotReference(guid, i * 2 + 1);
                var outputSlot = new SlotReference(guid, i * 2 + 2);
                edges.Add((owner.GetEdges(inputSlot), owner.GetEdges(outputSlot)));
            }
            return edges;
        }

        private void MoveBlendWeightSlotToLast()
        {
            var blendWeightEdges = owner.GetEdges(new SlotReference(guid, kBlendWeightInputSlotId));
            AddSlot(FindInputSlot<ISlot>(kBlendWeightInputSlotId));
            foreach (var edge in blendWeightEdges)
                owner.Connect(edge.outputSlot, edge.inputSlot);
        }

        private void RecreateSplatSlots(IReadOnlyList<MaterialSlot> oldInputSlots, IReadOnlyList<MaterialSlot> oldOutputSlots,
            IReadOnlyList<(IEnumerable<IEdge> inputEdges, IEnumerable<IEdge> outputEdges)> oldEdges, IReadOnlyList<int> newIndexToOldMapping)
        {
            RemoveSlotsNameNotMatching(new[] { kBlendWeightInputSlotId }, true);
            CreateSplatSlots(oldInputSlots, oldOutputSlots);

            for (int i = 0; i < m_SplatSlotNames.Count; ++i)
            {
                var oi = newIndexToOldMapping[i];
                foreach (var edge in oldEdges[oi].inputEdges)
                {
                    var inputSlotRef = new SlotReference(guid, i * 2 + 1);
                    owner.Connect(edge.outputSlot, inputSlotRef);
                }
                foreach (var edge in oldEdges[oi].outputEdges)
                {
                    var outputSlotRef = new SlotReference(guid, i * 2 + 2);
                    owner.Connect(outputSlotRef, edge.inputSlot);
                }
            }

            MoveBlendWeightSlotToLast();
        }

        public VisualElement CreateSettingsElement()
        {
            m_ReorderableList = new ReorderableList(m_SplatSlotNames, typeof(string), true, true, true, true);

            m_ReorderableList.drawHeaderCallback = rect =>
            {
                var labelRect = new Rect(rect.x, rect.y, rect.width - 10, rect.height);
                EditorGUI.LabelField(labelRect, "Slots");
            };

            m_ReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                EditorGUI.BeginChangeCheck();

                int id = GUIUtility.GetControlID("NameField".GetHashCode(), FocusType.Keyboard, rect);
                var name = m_SplatSlotNames[index] = EditorGUI.DelayedTextField(rect, GUIContent.none, id, m_SplatSlotNames[index], EditorStyles.label);
                if (GUIUtility.keyboardControl == id)
                    m_ReorderableList.index = index;

                if (EditorGUI.EndChangeCheck())
                {
                    var inputSplatSlots = EnumerateSplatInputSlots().ToList();
                    var outputSplatSlots = EnumerateSplatOutputSlots().ToList();
                    var splatEdges = SaveSplatSlotEdges();

                    RecreateSplatSlots(inputSplatSlots, outputSplatSlots, splatEdges, Enumerable.Range(0, inputSplatSlots.Count).ToList());
                    ValidateNode();
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

                MoveBlendWeightSlotToLast();

                // Select the new slot, then validate the node
                list.index = m_SplatSlotNames.Count - 1;
                ValidateNode();
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
                ValidateNode();
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
                ValidateNode();
            };

            var ps = new PropertySheet();
            ps.Add(new IMGUIContainer(m_ReorderableList.DoLayoutList));
            return ps;
        }
    }
}
