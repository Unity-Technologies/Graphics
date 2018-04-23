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

    abstract class VFXDataAnchorController : VFXController<VFXSlot>, IVFXAnchorController, IPropertyRMProvider
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


        class ProfilerScope : System.IDisposable
        {
            public ProfilerScope(string name)
            {
                UnityEngine.Profiling.Profiler.BeginSample(name);
            }

            void System.IDisposable.Dispose()
            {
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        public virtual object value
        {
            get
            {
                using (var scope = new ProfilerScope("VFXDataAnchorController.value.get"))
                {
                    if (portType != null)
                    {
                        if (!editable)
                        {
                            VFXViewController nodeController = m_SourceNode.viewController;

                            try
                            {
                                Profiler.BeginSample("GetEvaluatedContent");
                                var evaluatedValue = nodeController.GetEvaluatedContent(model);
                                Profiler.EndSample();
                                if (evaluatedValue != null)
                                {
                                    return VFXConverter.ConvertTo(evaluatedValue, portType);
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

        public bool m_Editable = true;

        public void UpdateEditable()
        {
            m_Editable = true;
            if (direction == Direction.Output)
                return;
            VFXSlot slot = model;
            if (slot.HasLink(true))
            {
                m_Editable = false;
                return;
            }

            while (slot != null)
            {
                if (slot.HasLink())
                {
                    m_Editable = false;
                    return;
                }
                slot = slot.GetParent();
            }
        }

        public virtual bool editable
        {
            get
            {
                return m_SourceNode.enabled && m_Editable;
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
            if (m_GizmoContext != null) m_GizmoContext.Unprepare();

            if (!model.IsMasterSlot())
            {
                var parentController = sourceNode.inputPorts.FirstOrDefault(t => t.model == model.GetParent());
                if (parentController != null)
                {
                    parentController.RefreshGizmo();
                }
                else if (model.GetParent()) // Try with grand parent for Vector3 spacable types
                {
                    parentController = sourceNode.inputPorts.FirstOrDefault(t => t.model == model.GetParent().GetParent());
                    if (parentController != null)
                    {
                        parentController.RefreshGizmo();
                    }
                }
            }
        }

        VFXDataAnchorGizmoContext m_GizmoContext;

        public void DrawGizmo(VisualEffect component)
        {
            if (m_GizmoContext == null)
            {
                m_GizmoContext = new VFXDataAnchorGizmoContext(this);
            }
            VFXGizmoUtility.Draw(m_GizmoContext, component);
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

    public class VFXDataAnchorGizmoContext : VFXGizmoUtility.Context
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


        List<object> stack = new List<object>();
        public override object value
        {
            get
            {
                stack.Clear();
                int stackSize = stack.Count;
                foreach (var action in m_ValueBuilder)
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
            m_ValueBuilder.Clear();
            m_ValueBuilder.Add(o => o.Add(m_Controller.value));

            if (!m_Controller.viewController.CanGetEvaluatedContent(m_Controller.model))
            {
                if (m_Controller.model.HasLink(false))
                {
                    if (VFXTypeUtility.GetComponentCount(m_Controller.model) != 0)
                    {
                        m_Indeterminate = true;
                        return;
                    }
                    m_FullReadOnly = true;
                }
                BuildValue(m_Controller.model);
            }
        }

        void BuildValue(VFXSlot slot)
        {
            foreach (var field in slot.property.type.GetFields())
            {
                VFXSlot subSlot = slot.children.FirstOrDefault<VFXSlot>(t => t.name == field.Name);

                if (subSlot != null)
                {
                    if (m_Controller.viewController.CanGetEvaluatedContent(subSlot))
                    {
                        m_ValueBuilder.Add(o => o.Add(m_Controller.viewController.GetEvaluatedContent(subSlot)));
                    }
                    else if (subSlot.HasLink(false) && VFXTypeUtility.GetComponentCount(subSlot) != 0) // replace by is VFXType
                    {
                        m_Indeterminate = true;
                        return;
                    }
                    else
                    {
                        m_ValueBuilder.Add(o => o.Add(subSlot.value));
                        BuildValue(subSlot);
                        if (m_Indeterminate) return;
                    }
                    m_ValueBuilder.Add(o => field.SetValue(o[o.Count - 2], o[o.Count - 1]));
                    m_ValueBuilder.Add(o => o.RemoveAt(o.Count - 1));
                }
            }
        }

        public override VFXGizmo.IProperty<T> RegisterProperty<T>(string member)
        {
            object result;
            if (m_PropertyCache.TryGetValue(member, out result))
            {
                if (result is VFXGizmo.IProperty<T> )
                    return result as VFXGizmo.IProperty<T>;
                else
                    return VFXGizmoUtility.NullProperty<T>.defaultProperty;
            }
            var controller = GetMemberController(member);

            if (controller != null && controller.portType == typeof(T))
            {
                return new VFXGizmoUtility.Property<T>(controller, !controller.model.HasLink(true));
            }

            return VFXGizmoUtility.NullProperty<T>.defaultProperty;
        }

        VFXDataAnchorController GetMemberController(string memberPath)
        {
            if (string.IsNullOrEmpty(memberPath))
            {
                return m_Controller;
            }

            return GetSubMemberController(memberPath, m_Controller.model);
        }

        VFXDataAnchorController GetSubMemberController(string memberPath, VFXSlot slot)
        {
            int index = memberPath.IndexOf(separator);

            if (index == -1)
            {
                VFXSlot subSlot = slot.children.FirstOrDefault(t => t.name == memberPath);
                if (subSlot != null)
                {
                    var subController = m_Controller.sourceNode.inputPorts.FirstOrDefault(t => t.model == subSlot);
                    return subController;
                }
                return null;
            }
            else
            {
                string memberName = memberPath.Substring(0, index);

                VFXSlot subSlot = slot.children.FirstOrDefault(t => t.name == memberName);
                if (subSlot != null)
                {
                    return GetSubMemberController(memberPath.Substring(index + 1), subSlot);
                }
                return null;
            }
        }

        protected bool m_FullReadOnly;
        protected HashSet<string> m_ReadOnlyMembers = new HashSet<string>();

        bool IsMemberEditable(string memberPath)
        {
            if (m_Indeterminate) return false;
            if (m_FullReadOnly) return false;
            while (true)
            {
                if (m_ReadOnlyMembers.Contains(memberPath))
                    return false;
                int index = memberPath.LastIndexOf(separator);
                if (index == -1)
                    return true;

                memberPath = memberPath.Substring(0, index);
            }
        }
    }
}
