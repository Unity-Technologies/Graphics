using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;
using UnityEditor.VFX.UI;
using UnityEngine.Serialization;

namespace UnityEditor.VFX
{
    enum VFXValueFilter
    {
        Default,
        Range,
        Enum
    }

    [ExcludeFromPreset]
    class VFXParameter : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXParameter()
        {
            m_ExposedName = "exposedName";
            m_Exposed = false;
            m_UICollapsed = false;
        }

        public static VFXParameter Duplicate(string copyName, VFXParameter source)
        {
            var newVfxParameter = (VFXParameter)ScriptableObject.CreateInstance(source.GetType());

            newVfxParameter.m_ExposedName = copyName;
            newVfxParameter.m_Exposed = source.m_Exposed;
            newVfxParameter.m_UICollapsed = source.m_UICollapsed;
            newVfxParameter.m_Order = source.m_Order + 1;
            newVfxParameter.m_Category = source.m_Category;
            newVfxParameter.m_Min = source.m_Min;
            newVfxParameter.m_Max = source.m_Max;
            newVfxParameter.m_IsOutput = source.m_IsOutput;
            newVfxParameter.m_EnumValues = source.m_EnumValues?.ToList();
            newVfxParameter.m_Tooltip = source.m_Tooltip;
            newVfxParameter.m_ValueFilter = source.m_ValueFilter;
            newVfxParameter.subgraphMode = source.subgraphMode;
            newVfxParameter.m_ValueExpr = source.m_ValueExpr;
            newVfxParameter.Init(source.type);

            if (!source.isOutput)
            {
                newVfxParameter.value = source.value;
            }

            return newVfxParameter;
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField, FormerlySerializedAs("m_exposedName")]
        private string m_ExposedName;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, FormerlySerializedAs("m_exposed")]
        private bool m_Exposed;
        [SerializeField]
        private int m_Order;
        [SerializeField]
        private string m_Category;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField]
        protected VFXSerializableObject m_Min;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField]
        protected VFXSerializableObject m_Max;

        [SerializeField]
        private bool m_IsOutput;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField]
        protected List<string> m_EnumValues;

        public List<string> enumValues
        {
            get { return m_EnumValues; }
            set
            {
                m_EnumValues = value;
                Invalidate(InvalidationCause.kSettingChanged);
            }
        }

        public object min
        {
            get { if (m_Min != null) return m_Min.Get(); else return null; }

            set
            {
                var invalidateCause = InvalidationCause.kParamChanged;

                if (m_Min == null || m_Min.type != type)
                {
                    m_Min = new VFXSerializableObject(type, value);
                    invalidateCause = InvalidationCause.kSettingChanged;
                }
                else
                    m_Min.Set(value);

                Invalidate(invalidateCause);
            }
        }
        public object max
        {
            get { if (m_Max != null) return m_Max.Get(); else return null; }

            set
            {
                var invalidateCause = InvalidationCause.kParamChanged;

                if (m_Max == null || m_Max.type != type)
                {
                    m_Max = new VFXSerializableObject(type, value);
                    invalidateCause = InvalidationCause.kSettingChanged;
                }
                else
                    m_Max.Set(value);

                Invalidate(invalidateCause);
            }
        }

        [SerializeField]
        VFXValueFilter m_ValueFilter;


        public VFXValueFilter valueFilter
        {
            get => m_ValueFilter;

            set
            {
                if (value != m_ValueFilter)
                {
                    m_ValueFilter = value;
                    switch (m_ValueFilter)
                    {
                        case VFXValueFilter.Default:
                            m_Max = m_Min = null;
                            m_EnumValues = null;
                            break;
                        case VFXValueFilter.Range:
                            m_Min = new VFXSerializableObject(type, this.value);
                            m_Max = new VFXSerializableObject(type, this.value);
                            m_EnumValues = null;
                            break;
                        case VFXValueFilter.Enum:
                            m_EnumValues = new List<string>();
                            m_EnumValues.Add("Zero");
                            m_EnumValues.Add("One");
                            m_Max = m_Min = null;
                            break;
                    }

                    Invalidate(InvalidationCause.kSettingChanged);
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                return m_IsOutput ? Enumerable.Repeat("m_Exposed", 1) : Enumerable.Empty<string>();
            }
        }


        public bool isOutput
        {
            get
            {
                return m_IsOutput;
            }

            set
            {
                if (m_IsOutput != value)
                {
                    m_IsOutput = value;

                    if (m_IsOutput)
                    {
                        var oldSlot = outputSlots[0];
                        var newSlot = VFXSlot.Create(new VFXProperty(oldSlot.property.type, "i"), VFXSlot.Direction.kInput);
                        newSlot.value = oldSlot.value;
                        oldSlot.UnlinkAll(true);
                        ReplaceSlot(oldSlot, newSlot);

                        if (m_Nodes != null && m_Nodes.Count > 1)
                        {
                            m_Nodes.RemoveRange(1, m_Nodes.Count - 2);
                        }
                        m_ExprSlots = null;
                        m_ValueExpr = null;
                        m_Exposed = false;
                    }
                    else
                    {
                        var oldSlot = inputSlots[0];
                        var newSlot = VFXSlot.Create(new VFXProperty(oldSlot.property.type, "o"), VFXSlot.Direction.kOutput);
                        newSlot.value = oldSlot.value;
                        oldSlot.UnlinkAll(true);
                        ReplaceSlot(oldSlot, newSlot);

                        ResetOutputValueExpression();
                    }
                }
            }
        }


        public void ResetOutputValueExpression()
        {
            Debug.Assert(!m_IsOutput);

            m_ExprSlots = outputSlots[0].GetVFXValueTypeSlots().ToArray();
            m_ValueExpr = m_ExprSlots.Select(t => t.DefaultExpression(valueMode)).ToArray();
        }

        public bool canHaveValueFilter
        {
            get
            {
                return !isOutput && (type == typeof(float) || type == typeof(int) || type == typeof(uint));
            }
        }

        [SerializeField]
        string m_Tooltip;

        public string tooltip
        {
            get
            {
                return m_Tooltip;
            }

            set
            {
                m_Tooltip = value;
                Invalidate(InvalidationCause.kUIChanged);
            }
        }

        [System.Serializable]
        public struct NodeLinkedSlot
        {
            public VFXSlot outputSlot; // some slot from the parameter
            public VFXSlot inputSlot;
        }

        [System.Serializable]
        public class Node
        {
            public Node(int id)
            {
                m_Id = id;
            }

            [SerializeField]
            private int m_Id;

            public int id { get { return m_Id; } }

            public List<NodeLinkedSlot> linkedSlots;
            public Vector2 position;
            public List<VFXSlot> expandedSlots;
            public bool expanded;


            //Should only be called by ValidateNodes if something very wrong happened with serialization
            internal void ChangeId(int newId)
            {
                m_Id = newId;
            }
        }

        [SerializeField]
        protected List<Node> m_Nodes;

        [NonSerialized]
        int m_IDCounter = 0;

        public string exposedName
        {
            get
            {
                return m_ExposedName;
            }
        }

        public bool exposed
        {
            get
            {
                return m_Exposed;
            }
        }

        public int order
        {
            get { return m_Order; }
            set
            {
                if (m_Order != value)
                {
                    m_Order = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public string category
        {
            get { return m_Category; }
            set
            {
                if (m_Category != value)
                {
                    m_Category = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        private void OnModified(VFXObject obj, bool uiChange)
        {
            if (!isOutput && (m_ExprSlots == null || m_ValueExpr == null))
            {
                ResetOutputValueExpression();
            }
        }

        public Type type
        {
            get
            {
                if (isOutput)
                {
                    return inputSlots[0].property.type;
                }
                else
                    return outputSlots[0].property.type;
            }
        }

        public object value
        {
            get
            {
                if (!isOutput)
                    return outputSlots[0].value;
                return null;
            }
            set
            {
                if (isOutput)
                    throw new System.InvalidOperationException("output parameters have no value");
                outputSlots[0].value = value;
            }
        }


        public ReadOnlyCollection<Node> nodes
        {
            get
            {
                if (m_Nodes == null)
                {
                    m_Nodes = new List<Node>();
                }
                return m_Nodes.AsReadOnly();
            }
        }


        public Node GetNode(int id)
        {
            return m_Nodes.FirstOrDefault(t => t.id == id);
        }

        protected override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            base.GenerateErrors(manager);

            var type = this.type;
            if (Deprecated.s_Types.Contains(type))
            {
                manager.RegisterError(
                    "DeprecatedTypeParameter",
                    VFXErrorType.Warning,
                    string.Format("The structure of the '{0}' has changed, the position property has been moved to a transform type. You should consider to recreate this parameter.", type.Name));
            }
        }

        protected sealed override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            base.OnInvalidate(model, cause);

            if (isOutput)
                return;
            if (cause == InvalidationCause.kSettingChanged)
            {
                var valueExpr = m_ExprSlots.Select(t => t.DefaultExpression(valueMode)).ToArray();
                bool valueExprChanged = true;
                if (m_ValueExpr.Length == valueExpr.Length)
                {
                    valueExprChanged = false;
                    for (int i = 0; i < m_ValueExpr.Length; ++i)
                    {
                        if (m_ValueExpr[i].ValueMode != valueExpr[i].ValueMode
                            || m_ValueExpr[i].valueType != valueExpr[i].valueType)
                        {
                            valueExprChanged = true;
                            break;
                        }
                    }
                }

                if (valueExprChanged)
                {
                    m_ValueExpr = valueExpr;
                    outputSlots[0].InvalidateExpressionTree();
                    Invalidate(InvalidationCause.kExpressionGraphChanged); // As we need to update exposed list event if not connected to a compilable context
                }
                /* TODO : Allow VisualEffectApi to update only exposed name */
                else if (exposed)
                {
                    Invalidate(InvalidationCause.kExpressionGraphChanged);
                }
            }

            if (cause == InvalidationCause.kParamChanged)
            {
                UpdateDefaultExpressionValue();
            }

            if (cause == InvalidationCause.kStructureChanged)
            {
                ResetOutputValueExpression();
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (isOutput)
                    return PropertiesFromSlotsOrDefaultFromClass(VFXSlot.Direction.kInput);
                return Enumerable.Empty<VFXPropertyWithValue>();
            }
        }
        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                if (!isOutput)
                    return PropertiesFromSlotsOrDefaultFromClass(VFXSlot.Direction.kOutput);
                return Enumerable.Empty<VFXPropertyWithValue>();
            }
        }

        public void Init(Type _type)
        {
            if (_type != null && outputSlots.Count == 0)
            {
                VFXSlot slot = VFXSlot.Create(new VFXProperty(_type, "o"), VFXSlot.Direction.kOutput);
                AddSlot(slot);

                if (!typeof(UnityEngine.Object).IsAssignableFrom(_type) && _type != typeof(GraphicsBuffer))
                    slot.value = System.Activator.CreateInstance(_type);
            }
            else
            {
                throw new InvalidOperationException("Cannot init VFXParameter");
            }

            ResetOutputValueExpression();
        }

        public override void OnEnable()
        {
            base.OnEnable();

            onModified += OnModified;
            if (!isOutput && outputSlots.Count > 0)
            {
                ResetOutputValueExpression();
            }

            if (m_Nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (m_IDCounter < node.id + 1)
                    {
                        m_IDCounter = node.id + 1;
                    }
                }
            }
        }

        Node NewNode()
        {
            return new Node(m_IDCounter++);
        }

        public int AddNode(Vector2 pos)
        {
            Node info = NewNode();

            info.position = pos;

            if (m_Nodes == null)
            {
                m_Nodes = new List<Node>();
            }

            m_Nodes.Add(info);

            Invalidate(InvalidationCause.kUIChanged);

            return info.id;
        }

        public void RemoveNode(Node info)
        {
            if (m_Nodes.Contains(info))
            {
                if (info.linkedSlots != null)
                {
                    foreach (var slots in info.linkedSlots)
                    {
                        slots.outputSlot.Unlink(slots.inputSlot);
                    }
                }
                m_Nodes.Remove(info);

                Invalidate(InvalidationCause.kUIChanged);
            }
        }

        public override void Sanitize(int version)
        {
            base.Sanitize(version);

            HashSet<int> usedIds = new HashSet<int>();

            if (m_Min != null && m_Min.type != null && m_ValueFilter == VFXValueFilter.Default)
                m_ValueFilter = VFXValueFilter.Range;

            if (m_Nodes != null)
            {
                foreach (var node in m_Nodes)
                {
                    if (usedIds.Contains(node.id))
                    {
                        node.ChangeId(m_IDCounter++);
                    }
                    usedIds.Add(node.id);
                }
            }
        }

        //AddNodeRange will take ownership of the Nodes instead of copying them
        public void AddNodeRange(IEnumerable<Node> infos)
        {
            foreach (var info in infos)
            {
                if (m_Nodes.Any(t => t.id == info.id))
                {
                    info.ChangeId(m_IDCounter++);
                }
                m_Nodes.Add(info);
            }

            Invalidate(InvalidationCause.kUIChanged);
        }

        //SetNodes will take ownership of the Nodes instead of copying them
        public void SetNodes(IEnumerable<Node> infos)
        {
            m_Nodes = infos.ToList();

            ValidateNodes();

            Invalidate(InvalidationCause.kUIChanged);
        }

        void GetAllLinks(List<NodeLinkedSlot> list, VFXSlot slot)
        {
            if (isOutput)
                list.AddRange(slot.LinkedSlots.Select(t => new NodeLinkedSlot() { outputSlot = t, inputSlot = slot }));
            else
                list.AddRange(slot.LinkedSlots.Select(t => new NodeLinkedSlot() { outputSlot = slot, inputSlot = t }));
            foreach (var child in slot.children)
            {
                GetAllLinks(list, child);
            }
        }

        void GetAllExpandedSlots(List<VFXSlot> list, VFXSlot slot)
        {
            if (!slot.collapsed)
                list.Add(slot);
            foreach (var child in slot.children)
            {
                GetAllExpandedSlots(list, child);
            }
        }

        public void ValidateNodes()
        {
            // Case of the old VFXParameter we create a new one on the same place with all the Links
            if (position != Vector2.zero && nodes.Count == 0)
            {
                CreateDefaultNode(position);
            }
            else
            {
                // the linked slot of the outSlot decides so make sure that all appear once and only once in all the nodes
                List<NodeLinkedSlot> links = new List<NodeLinkedSlot>();

                var targetSlot = isOutput ? inputSlots.FirstOrDefault() : outputSlots.FirstOrDefault();
                if (targetSlot == null)
                    return;

                GetAllLinks(links, targetSlot);
                HashSet<int> usedIds = new HashSet<int>();
                foreach (var info in nodes)
                {
                    // Check linkedSlots
                    if (info.linkedSlots == null)
                    {
                        info.linkedSlots = new List<NodeLinkedSlot>();
                    }
                    else
                    {
                        // first remove linkedSlots that are not existing
                        var intersect = info.linkedSlots.Intersect(links);
                        if (intersect.Count() != info.linkedSlots.Count())
                            info.linkedSlots = info.linkedSlots.Intersect(links).ToList();
                    }

                    //Check that all slots needed for
                    if (info.expandedSlots == null)
                    {
                        info.expandedSlots = new List<VFXSlot>();
                    }
                    else
                    {
                        if (info.expandedSlots.Any(t => t == null))
                            info.expandedSlots = info.expandedSlots.Where(t => t != null).ToList();
                    }

                    if (usedIds.Contains(info.id))
                    {
                        info.ChangeId(m_IDCounter++);
                    }
                    usedIds.Add(info.id);

                    foreach (var slot in info.linkedSlots)
                    {
                        links.Remove(slot);
                    }
                }
                // if there are some links in the output slots that are in not found in the infos, find or create a node for them.
                foreach (var link in links)
                {
                    Node newInfos = null;
                    if (nodes.Any())
                    {
                        //There are already some nodes, choose the closest one to restore the link
                        var refPosition = Vector2.zero;
                        object refOwner = link.inputSlot.owner;
                        while (refOwner is VFXModel model && refPosition == Vector2.zero)
                        {
                            refPosition = model is VFXBlock ? Vector2.zero : model.position;
                            refOwner = model.GetParent();
                        }
                        newInfos = nodes.OrderBy(o => (refPosition - o.position).SqrMagnitude()).First();
                    }
                    else
                    {
                        newInfos = NewNode();
                        m_Nodes.Add(newInfos);
                    }
                    if (newInfos.linkedSlots == null)
                        newInfos.linkedSlots = new List<NodeLinkedSlot>();
                    newInfos.linkedSlots.Add(link);
                    newInfos.expandedSlots = new List<VFXSlot>();
                }
            }
            position = Vector2.zero; // Set that as a marker that the parameter has been touched by the new code.
        }

        public void CreateDefaultNode(Vector2 position)
        {
            if (m_Nodes != null && m_Nodes.Count != 0)
            {
                Debug.LogError("CreateDefaultNode must only be called with an empty parameter");
                return;
            }
            var newInfos = NewNode();
            newInfos.position = position;

            var targetSlot = isOutput ? inputSlots[0] : outputSlots[0];

            newInfos.linkedSlots = new List<NodeLinkedSlot>();
            GetAllLinks(newInfos.linkedSlots, targetSlot);
            newInfos.expandedSlots = new List<VFXSlot>();
            GetAllExpandedSlots(newInfos.expandedSlots, targetSlot);
            if (m_Nodes == null)
            {
                m_Nodes = new List<Node>();
            }
            m_Nodes.Add(newInfos);
        }

        public bool subgraphMode
        {
            get; set;
        }

        public void UpdateDefaultExpressionValue()
        {
            if (!isOutput)
            {
                for (int i = 0; i < m_ExprSlots.Length; ++i)
                {
                    m_ValueExpr[i].SetContent(m_ExprSlots[i].value);
                }
            }
        }

        public override void UpdateOutputExpressions()
        {
            if (!isOutput)
            {
                for (int i = 0; i < m_ExprSlots.Length; ++i)
                {
                    m_ValueExpr[i].SetContent(m_ExprSlots[i].value);
                    if (!subgraphMode) // don't erase the expression in subgraph mode.
                        m_ExprSlots[i].SetExpression(m_ValueExpr[i]);
                }
            }
        }

        public override void OnCopyLinksOtherSlot(VFXSlot mySlot, VFXSlot prevOtherSlot, VFXSlot newOtherSlot)
        {
            foreach (var node in nodes)
            {
                if (node.linkedSlots != null)
                {
                    for (int i = 0; i < node.linkedSlots.Count; ++i)
                    {
                        if (node.linkedSlots[i].outputSlot == mySlot && node.linkedSlots[i].inputSlot == prevOtherSlot)
                        {
                            node.linkedSlots[i] = new NodeLinkedSlot() { outputSlot = mySlot, inputSlot = newOtherSlot };
                            return;
                        }
                    }
                }
            }
        }

        public override void OnCopyLinksMySlot(VFXSlot myPrevSlot, VFXSlot myNewSlot, VFXSlot otherSlot)
        {
            foreach (var node in nodes)
            {
                if (node.linkedSlots != null)
                {
                    for (int i = 0; i < node.linkedSlots.Count; ++i)
                    {
                        if (node.linkedSlots[i].outputSlot == myPrevSlot && node.linkedSlots[i].inputSlot == otherSlot)
                        {
                            node.linkedSlots[i] = new NodeLinkedSlot() { outputSlot = myNewSlot, inputSlot = otherSlot };
                            return;
                        }
                    }
                }
            }
        }

        private VFXValue.Mode valueMode
        {
            get
            {
                return exposed ? VFXValue.Mode.Variable : VFXValue.Mode.FoldableVariable;
            }
        }

        [NonSerialized]
        private VFXSlot[] m_ExprSlots;

        [NonSerialized]
        private VFXValue[] m_ValueExpr;
    }
}
