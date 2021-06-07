using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditorInternal;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal class ReorderableSlotListView : VisualElement
    {
        int m_SelectedIndex = -1;
        SlotType m_SlotType;
        GUIStyle m_LabelStyle;
        IMGUIContainer m_Container;
        AbstractMaterialNode m_Node;
        ReorderableList m_ReorderableList;
        bool m_AllowBareResources;

        internal delegate void ListRecreatedDelegate();
        ListRecreatedDelegate m_OnListRecreatedCallback = new ListRecreatedDelegate(() => {});

        string label => string.Format("{0}s", m_SlotType.ToString());

        public ReorderableList.AddCallbackDelegate OnAddCallback
        {
            get => m_ReorderableList?.onAddCallback;
            set => m_ReorderableList.onAddCallback = value;
        }

        public ReorderableList.RemoveCallbackDelegate OnRemoveCallback
        {
            get => m_ReorderableList.onRemoveCallback;
            set => m_ReorderableList.onRemoveCallback = value;
        }

        public ListRecreatedDelegate OnListRecreatedCallback
        {
            get => m_OnListRecreatedCallback;
            set => m_OnListRecreatedCallback = value;
        }

        public Func<ConcreteSlotValueTypePopupName, bool> AllowedTypeCallback;

        internal ReorderableSlotListView(AbstractMaterialNode node, SlotType slotType, bool allowBareResources)
        {
            m_AllowBareResources = allowBareResources;
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ReorderableSlotListView"));
            m_Node = node;
            m_SlotType = slotType;
            m_Container = new IMGUIContainer(() => OnGUIHandler()) { name = "ListContainer" };
            Add(m_Container);
            RecreateList();
            AddCallbacks();
        }

        internal void RecreateList()
        {
            // Get slots based on type
            List<MaterialSlot> slots = new List<MaterialSlot>();
            if (m_SlotType == SlotType.Input)
                m_Node.GetInputSlots(slots);
            else
                m_Node.GetOutputSlots(slots);

            // Create reorderable list from IDs
            List<int> slotIDs = slots.Select(s => s.id).ToList();
            m_ReorderableList = new ReorderableList(slotIDs, typeof(int), true, true, true, true);
            m_OnListRecreatedCallback();
        }

        private void OnGUIHandler()
        {
            if (m_ReorderableList == null)
            {
                RecreateList();
                AddCallbacks();
            }

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                m_ReorderableList.index = m_SelectedIndex;
                m_ReorderableList.DoLayoutList();

                if (changeCheckScope.changed)
                    m_Node.Dirty(ModificationScope.Node);
            }
        }

        private void AddCallbacks()
        {
            m_ReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                var labelRect = new Rect(rect.x, rect.y, rect.width - 10, rect.height);
                EditorGUI.LabelField(labelRect, label);
            };

            // Draw Element
            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Slot is guaranteed to exist in this UI state
                MaterialSlot oldSlot = m_Node.FindSlot<MaterialSlot>((int)m_ReorderableList.list[index]);

                EditorGUI.BeginChangeCheck();

                var displayName = EditorGUI.DelayedTextField(new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), oldSlot.RawDisplayName(), EditorStyles.label);

                ConcreteSlotValueTypePopupName concreteValueTypePopupOrig =
                    oldSlot.concreteValueType.ToConcreteSlotValueTypePopupName(oldSlot.bareResource);

                ConcreteSlotValueTypePopupName concreteValueTypePopupNew = (ConcreteSlotValueTypePopupName)EditorGUI.EnumPopup(
                    new Rect(rect.x + rect.width / 2, rect.y, rect.width - rect.width / 2, EditorGUIUtility.singleLineHeight),
                    GUIContent.none,
                    concreteValueTypePopupOrig,
                    e =>
                    {
                        ConcreteSlotValueTypePopupName csvtpn = (ConcreteSlotValueTypePopupName)e;
                        csvtpn.ToConcreteSlotValueType(out bool isBareResource);
                        if (isBareResource && !m_AllowBareResources)
                            return false;
                        return AllowedTypeCallback?.Invoke(csvtpn) ?? true;
                    }
                );

                if (EditorGUI.EndChangeCheck())
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Modify Port");

                    displayName = NodeUtils.ConvertToValidHLSLIdentifier(displayName);

                    if (displayName != oldSlot.RawDisplayName())
                    {
                        using (var tempSlots = PooledList<MaterialSlot>.Get())
                        {
                            m_Node.GetSlots(tempSlots);

                            // deduplicate against other slot shaderOutputNames
                            displayName = GraphUtil.DeduplicateName(tempSlots.Where(p => p.id != oldSlot.id).Select(p => p.shaderOutputName), "{0}_{1}", displayName);
                        }
                    }

                    ConcreteSlotValueType concreteValueType = concreteValueTypePopupNew.ToConcreteSlotValueType(out bool isBareResource);

                    // Because the type may have changed, we can't (always) just modify the existing slot.  So create a new one and replace it.
                    var newSlot = MaterialSlot.CreateMaterialSlot(concreteValueType.ToSlotValueType(), oldSlot.id, displayName, displayName, m_SlotType, Vector4.zero);
                    newSlot.CopyValuesFrom(oldSlot);
                    m_Node.AddSlot(newSlot, false);
                    newSlot.bareResource = isBareResource;

                    List<int> orderedSlotIds = new List<int>();
                    if (m_SlotType == SlotType.Input)
                    {
                        orderedSlotIds.AddRange(m_ReorderableList.list.OfType<int>());

                        List<MaterialSlot> slots = new List<MaterialSlot>();
                        m_Node.GetOutputSlots(slots);
                        orderedSlotIds.AddRange(slots.Select(s => s.id));
                    }
                    else
                    {
                        List<MaterialSlot> slots = new List<MaterialSlot>();
                        m_Node.GetInputSlots(slots);
                        orderedSlotIds.AddRange(slots.Select(s => s.id));

                        orderedSlotIds.AddRange(m_ReorderableList.list.OfType<int>());
                    }

                    m_Node.SetSlotOrder(orderedSlotIds);

                    RecreateList();
                    m_Node.ValidateNode();
                }
            };

            // Element height
            m_ReorderableList.elementHeightCallback = (int indexer) =>
            {
                return m_ReorderableList.elementHeight;
            };

            // Add callback delegates
            m_ReorderableList.onSelectCallback += SelectEntry;
            m_ReorderableList.onAddCallback += AddEntry;
            m_ReorderableList.onRemoveCallback += RemoveEntry;
            m_ReorderableList.onReorderCallback += ReorderEntries;
        }

        private void SelectEntry(ReorderableList list)
        {
            m_SelectedIndex = list.index;
        }

        private void AddEntry(ReorderableList list)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Add Port");
            // Need to get all current slots to get the next valid ID
            List<MaterialSlot> slots = new List<MaterialSlot>();
            m_Node.GetSlots(slots);
            int[] slotIDs = slots.Select(s => s.id).OrderByDescending(s => s).ToArray();
            int newSlotID = slotIDs.Length > 0 ? slotIDs[0] + 1 : 0;

            string name = NodeUtils.GetDuplicateSafeNameForSlot(m_Node, newSlotID, "New");

            // Create a new slot and add it
            var newSlot = MaterialSlot.CreateMaterialSlot(SlotValueType.Vector1, newSlotID, name, NodeUtils.GetHLSLSafeName(name), m_SlotType, Vector4.zero);
            m_Node.AddSlot(newSlot);

            // Select the new slot, then validate the node
            m_SelectedIndex = list.list.Count - 1;
            m_Node.ValidateNode();
        }

        private void RemoveEntry(ReorderableList list)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Remove Port");

            // Remove the slot from the node
            m_SelectedIndex = list.index;
            m_Node.RemoveSlot((int)m_ReorderableList.list[m_SelectedIndex]);

            // Then remove it from the list
            // Need to do this order to preserve the list indicies for previous step
            ReorderableList.defaultBehaviours.DoRemoveButton(list);

            // Validate
            m_Node.ValidateNode();
        }

        private void ReorderEntries(ReorderableList list)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Reorder Ports");

            // Get all the current slots
            List<MaterialSlot> slots = new List<MaterialSlot>();
            if (m_SlotType == SlotType.Input)
                m_Node.GetInputSlots<MaterialSlot>(slots);
            else
                m_Node.GetOutputSlots<MaterialSlot>(slots);

            // Store the edges
            Dictionary<MaterialSlot, List<IEdge>> edgeDict = new Dictionary<MaterialSlot, List<IEdge>>();
            foreach (MaterialSlot slot in slots)
                edgeDict.Add(slot, (List<IEdge>)slot.owner.owner.GetEdges(slot.slotReference));

            // Get reorder slots so need to remove them all then re-add
            foreach (MaterialSlot slot in slots)
                m_Node.RemoveSlot(slot.id);

            // Order them by their slot ID
            slots = slots.OrderBy(s => s.id).ToList();

            // Now add the slots back based on the list order
            // For each list entry get the slot with that ID
            for (int i = 0; i < list.list.Count; i++)
            {
                var currentSlot = slots.Where(s => s.id == (int)list.list[i]).FirstOrDefault();
                m_Node.AddSlot(currentSlot);
            }

            // Reconnect the edges
            foreach (KeyValuePair<MaterialSlot, List<IEdge>> entry in edgeDict)
            {
                foreach (IEdge edge in entry.Value)
                {
                    m_Node.owner.Connect(edge.outputSlot, edge.inputSlot);
                }
            }

            RecreateList();
            m_Node.ValidateNode();
        }
    }
}
