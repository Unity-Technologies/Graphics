using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    interface IVFXAnchorController
    {
        void Connect(VFXEdgeController edgeController);
        void Disconnect(VFXEdgeController edgeController);

        Direction direction {get; }
    }

    abstract class VFXDataAnchorController : VFXController<VFXSlot>, IVFXAnchorController, IPropertyRMProvider, IValueController
    {
        private VFXNodeController m_SourceNode;

        public VFXNodeController sourceNode
        {
            get
            {
                return m_SourceNode;
            }
        }

        public override string name
        {
            get
            {
                return base.name;
            }
        }

        VFXSlot m_MasterSlot;

        public Type portType { get; set; }

        public VFXDataAnchorController(VFXSlot model, VFXNodeController sourceNode, bool hidden) : base(sourceNode.viewController, model)
        {
            m_SourceNode = sourceNode;
            m_Hidden = hidden;
            m_Expanded = expandedSelf;

            if (model != null)
            {
                portType = model.property.type;

                if (model.GetMasterSlot() != null && model.GetMasterSlot() != model)
                {
                    m_MasterSlot = model.GetMasterSlot();

                    viewController.RegisterNotification(m_MasterSlot, MasterSlotChanged);
                }
                ModelChanged(model);
            }
        }

        void MasterSlotChanged()
        {
            if (m_MasterSlot == null)
                return;
            ModelChanged(m_MasterSlot);
        }

        bool m_Expanded;

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            Profiler.BeginSample("VFXDataAnchorController.ModelChanged");
            if (expandedSelf != m_Expanded)
            {
                m_Expanded = expandedSelf;
                UpdateHiddenRecursive(m_Hidden, true);
            }
            Profiler.BeginSample("VFXDataAnchorController.ModelChanged:UpdateInfos");
            UpdateInfos();
            Profiler.EndSample();

            sourceNode.DataEdgesMightHaveChanged();

            Profiler.BeginSample("VFXDataAnchorController.NotifyChange");
            NotifyChange(AnyThing);
            Profiler.EndSample();
            Profiler.EndSample();
        }

        public override void OnDisable()
        {
            if (!object.ReferenceEquals(m_MasterSlot, null))
            {
                viewController.UnRegisterNotification(m_MasterSlot, MasterSlotChanged);
                m_MasterSlot = null;
            }
            base.OnDisable();
        }

        public virtual bool HasLink()
        {
            return model.HasLink();
        }

        public virtual bool CanLink(VFXDataAnchorController controller)
        {
            if (controller.model != null)
            {
                if (model.CanLink(controller.model) && controller.model.CanLink(model))
                {
                    return true;
                }
                return sourceNode.CouldLink(this, controller);
            }

            return controller.CanLink(this);
        }

        public virtual VFXParameter.NodeLinkedSlot CreateLinkTo(VFXDataAnchorController output)
        {
            var slotOutput = output != null ? output.model : null;
            var slotInput = model;
            sourceNode.WillCreateLink(ref slotInput, ref slotOutput);

            if (slotInput != null && slotOutput != null && slotInput.Link(slotOutput))
            {
                return new VFXParameter.NodeLinkedSlot() {inputSlot = slotInput, outputSlot = slotOutput};
            }

            return new VFXParameter.NodeLinkedSlot();
        }

        public class Change
        {
            public const int hidden = 1;
        }

        private void UpdateHiddenRecursive(bool parentCollapsed, bool firstLevel)
        {
            bool changed = m_Hidden != parentCollapsed;
            if (changed || firstLevel)
            {
                m_Hidden = parentCollapsed;

                var ports = (direction == Direction.Input) ? m_SourceNode.inputPorts : m_SourceNode.outputPorts;

                var children = model.children;

                if (typeof(ISpaceable).IsAssignableFrom(model.property.type) && model.children.Count() == 1)
                {
                    children = children.First().children;
                }

                foreach (var element in children.Select(t => ports.First(u => u.model == t)))
                {
                    element.UpdateHiddenRecursive(m_Hidden || !expandedSelf, false);
                }
                if (changed && !firstLevel) //Do not notify on first level as it will be done by the called
                    NotifyChange((int)Change.hidden);
            }
        }

        VFXPropertyAttribute[] m_Attributes;

        public virtual void UpdateInfos()
        {
            bool sameAttributes = (m_Attributes == null && model.property.attributes == null) || (m_Attributes != null && model.property.attributes != null && Enumerable.SequenceEqual(m_Attributes, model.property.attributes));

            if (model.property.type != portType || !sameAttributes)
            {
                portType = model.property.type;
                m_Attributes = model.property.attributes;
            }
        }

        public bool indeterminate
        {
            get
            {
                return !m_SourceNode.viewController.CanGetEvaluatedContent(model);
            }
        }

        public virtual object value
        {
            get
            {
                if (portType != null)
                {
                    if (!editable)
                    {
                        VFXViewController nodeController = m_SourceNode.viewController;

                        try
                        {
                            if (nodeController.CanGetEvaluatedContent(model))
                            {
                                return VFXConverter.ConvertTo(nodeController.GetEvaluatedContent(model), portType);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError("Trying to get the value from expressions threw." + e.Message + " In anchor : " + name + " from node :" + sourceNode.title);
                        }
                    }

                    return VFXConverter.ConvertTo(model.value, portType);
                }
                else
                {
                    return null;
                }
            }

            set { SetPropertyValue(VFXConverter.ConvertTo(value, portType)); }
        }


        List<VFXDataEdgeController> m_Connections = new List<VFXDataEdgeController>();

        public virtual void Connect(VFXEdgeController edgeController)
        {
            m_Connections.Add(edgeController as VFXDataEdgeController);
            RefreshGizmo();
        }

        public virtual void Disconnect(VFXEdgeController edgeController)
        {
            m_Connections.Remove(edgeController as VFXDataEdgeController);
            RefreshGizmo();
        }

        public bool connected
        {
            get { return m_Connections.Count > 0; }
        }

        public IEnumerable<VFXDataEdgeController> connections { get { return m_Connections; } }

        public abstract Direction direction { get; }
        public Orientation orientation { get { return Orientation.Horizontal; } }

        public string path
        {
            get { return model.path; }
        }

        public object[] customAttributes
        {
            get
            {
                return new object[] {};
            }
        }

        public VFXPropertyAttribute[] attributes
        {
            get { return m_Attributes; }
        }

        public virtual int depth
        {
            get
            {
                int depth = model.depth;
                if (depth > 0)
                {
                    if (SlotShouldSkipFirstLevel(model.GetMasterSlot()))
                    {
                        --depth;
                    }
                }
                return depth;
            }
        }

        public virtual bool expandable
        {
            get { return VFXContextController.IsTypeExpandable(portType); }
        }

        public virtual string iconName
        {
            get { return portType.Name; }
        }

        private bool m_Hidden;

        public bool expandedInHierachy
        {
            get
            {
                return !m_Hidden || connected;
            }
        }

        public virtual bool expandedSelf
        {
            get
            {
                return !model.collapsed;
            }
        }

        bool IPropertyRMProvider.expanded
        {
            get { return expandedSelf; }
        }

        public virtual bool editable
        {
            get
            {
                if (direction == Direction.Output)
                    return true;
                bool editable = m_SourceNode.enabled;

                if (editable)
                {
                    VFXSlot slot = model;
                    while (slot != null)
                    {
                        if (slot.HasLink())
                        {
                            editable = false;
                            break;
                        }
                        slot = slot.GetParent();
                    }


                    foreach (VFXSlot child in model.children)
                    {
                        if (child.HasLink())
                        {
                            editable = false;
                        }
                    }
                }

                return editable;
            }
        }

        public void SetPropertyValue(object value)
        {
            Undo.RecordObject(model.GetMasterSlot(), "VFXSlotValue"); // The slot value is stored on the master slot, not necessarly my own slot
            model.value = value;
        }

        public static bool SlotShouldSkipFirstLevel(VFXSlot slot)
        {
            return typeof(ISpaceable).IsAssignableFrom(slot.property.type) && slot.children.Count() == 1;
        }

        public virtual void ExpandPath()
        {
            model.collapsed = false;
            if (SlotShouldSkipFirstLevel(model))
            {
                model.children.First().collapsed = model.collapsed;
            }
        }

        public virtual void RetractPath()
        {
            model.collapsed = true;
            if (SlotShouldSkipFirstLevel(model))
            {
                model.children.First().collapsed = model.collapsed;
            }
        }

        void RefreshGizmo()
        {
            if( m_GizmoContext != null)m_GizmoContext.Unprepare();

            if( ! model.IsMasterSlot())
            {
                var parentController = sourceNode.inputPorts.FirstOrDefault(t=>t.model == model.GetParent());
                if( parentController != null)
                    parentController.RefreshGizmo();
            }
        }

        VFXDataAnchorGizmoContext m_GizmoContext;

        public void DrawGizmo(VisualEffect component)
        {
            if(m_GizmoContext == null)
            {
                m_GizmoContext = new VFXDataAnchorGizmoContext(this);
            }
            VFXValueGizmo.Draw(new VFXDataAnchorGizmoContext(this), component);
        }
    }

    class VFXUpcommingDataAnchorController : VFXDataAnchorController
    {
        public VFXUpcommingDataAnchorController(VFXNodeController sourceNode, bool hidden) : base(null, sourceNode, hidden)
        {
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }

        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }


        public override bool editable
        {
            get {return true; }
        }
        public override bool expandedSelf
        {
            get
            {
                return false;
            }
        }
        public override bool expandable
        {
            get {return false; }
        }
        public override bool HasLink()
        {
            return false;
        }

        public override void UpdateInfos()
        {
        }

        public override object value
        {
            get
            {
                return null;
            }
            set
            {
            }
        }
        public override int depth
        {
            get
            {
                return 0;
            }
        }
        public override string name
        {
            get
            {
                return "";
            }
        }
        public override bool CanLink(VFXDataAnchorController controller)
        {
            var op = (sourceNode as VFXCascadedOperatorController);

            if (op == null)
                return false;

            return op.model.GetBestAffinityType(controller.model.property.type) != null;
        }

        public new VFXCascadedOperatorController sourceNode
        {
            get { return base.sourceNode as VFXCascadedOperatorController; }
        }

        public override VFXParameter.NodeLinkedSlot CreateLinkTo(VFXDataAnchorController output)
        {
            var slotOutput = output != null ? output.model : null;

            VFXOperatorNumericCascadedUnifiedNew op = sourceNode.model;

            op.AddOperand(op.GetBestAffinityType(output.model.property.type));

            var slotInput = op.GetInputSlot(op.GetNbInputSlots() - 1);
            if (slotInput != null && slotOutput != null && slotInput.Link(slotOutput))
            {
                return new VFXParameter.NodeLinkedSlot() {inputSlot = slotInput, outputSlot = slotOutput};
            }

            return new VFXParameter.NodeLinkedSlot();
        }
    }

    public class VFXDataAnchorGizmoContext : VFXValueGizmo.Context
    {
        // Provider
        internal VFXDataAnchorGizmoContext(VFXDataAnchorController controller)
        {
            m_Controller = controller;
        }

        VFXDataAnchorController m_Controller;

        public override Type portType
        {
            get {return m_Controller.portType; }
        }



        List<object> stack= new List<object>();
        public override object value 
        {
            get
            {
                stack.Clear();
                int stackSize = stack.Count;
                foreach(var action in m_ValueBuilder)
                {
                    action(stack);
                    stackSize = stack.Count;
                }

                return stack.First();
            }
        }

        List<Action<List<object>>> m_ValueBuilder = new List<Action<List<object>>>();

        protected override void InternalPrepare()
        {
            var type = m_Controller.portType;

            if (!type.IsValueType)
            {
                Debug.LogError("No support for class types in Gizmos");
                return;
            }
            m_ReadOnlyMembers.Clear();
            m_ValueBuilder.Clear();

            bool valueSet = false;
            m_ValueBuilder.Add(o=>o.Add(m_Controller.value));
            m_FullReadOnly = false;

            if (m_Controller.viewController.CanGetEvaluatedContent(m_Controller.model))// this is for Vector type that the system knows how to compute
            {
                valueSet = true;
                m_FullReadOnly = true;
            }
            else if (m_Controller.model.HasLink(false))
            {
                m_Indeterminate = true;
                return;
            }
            else if (m_Controller.model.HasLink(true))
            {
                BuildValue( m_Controller.model, "", valueSet);
            }
        }


        void BuildValue(VFXSlot slot, string memberPath, bool valueSet)
        {
            foreach (var field in slot.property.type.GetFields())
            {
                VFXSlot subSlot = slot.children.FirstOrDefault<VFXSlot>(t => t.name == field.Name);

                if (subSlot != null)
                {
                    string subMemberPath = field.Name;
                    if (memberPath.Length > 0)
                    {
                        subMemberPath = memberPath + separator + subMemberPath;
                    }
                    bool subValueSet = false;
                    if (!valueSet)
                    {
                        subValueSet = false;


                        if (m_Controller.viewController.CanGetEvaluatedContent(subSlot))
                        {
                            m_ValueBuilder.Add(o=>o.Add(m_Controller.viewController.GetEvaluatedContent(subSlot)));
                            subValueSet = true;
                        }
                        else if( slot.HasLink(false))
                        {
                            m_Indeterminate = true;
                            return;
                        }
                        else
                        {
                            m_ValueBuilder.Add(o=>o.Add(subSlot.value));

                            BuildValue(subSlot, subMemberPath, false);
                            if( m_Indeterminate) return;
                        }
                        /*
                        m_ValueBuilder.Add(o=>
                            {
                                int target = o.Count-2;
                                int member = o.Count-1;
                                field.SetValue(o[target], o[member]);
                            }
                        );*/
                        m_ValueBuilder.Add(o=>field.SetValue(o[o.Count-2], o[ o.Count-1]));
                        m_ValueBuilder.Add(o=>o.RemoveAt(o.Count-1));
                    }

                    if (subSlot.HasLink(false))
                    {
                        m_ReadOnlyMembers.Add(subMemberPath);
                    }
                    else if (subSlot.HasLink(true))
                    {
                        if (m_Controller.viewController.CanGetEvaluatedContent(subSlot))
                        // for the moment we can edit only part of a position or rotation so mark it as read only if one of the children has a link
                        {
                            m_ReadOnlyMembers.Add(subMemberPath);
                        }
                        else if (subValueSet || valueSet)
                        {
                            BuildValue( subSlot, subMemberPath, true);
                            if( m_Indeterminate) return;
                        }
                    }
                }
            }
        }

        public override void SetMemberValue(string memberPath, object value)
        {
            if (string.IsNullOrEmpty(memberPath))
            {
                m_Controller.value = value;
                return;
            }

            SetSubMemberValue(memberPath, m_Controller.model, value);
        }

        void SetSubMemberValue(string memberPath, VFXSlot slot, object value)
        {
            int index = memberPath.IndexOf(separator);

            if (index == -1)
            {
                VFXSlot subSlot = slot.children.FirstOrDefault(t => t.name == memberPath);
                if (subSlot != null)
                {
                    m_Controller.sourceNode.inputPorts.First(t => t.model == subSlot).value = value;
                }
            }
            else
            {
                string memberName = memberPath.Substring(0, index);

                VFXSlot subSlot = slot.children.FirstOrDefault(t => t.name == memberName);
                if (subSlot != null)
                {
                    SetSubMemberValue(memberPath.Substring(index + 1), subSlot, value);
                }
            }
        }
    }
}
