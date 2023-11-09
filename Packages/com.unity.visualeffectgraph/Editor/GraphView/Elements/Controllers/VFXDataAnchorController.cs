using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine.Profiling;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    interface IVFXAnchorController
    {
        void Connect(VFXEdgeController edgeController);
        void Disconnect(VFXEdgeController edgeController);
        Direction direction { get; }
    }

    abstract class VFXDataAnchorController : VFXController<VFXSlot>, IVFXAnchorController, IPropertyRMProvider, IGizmoable, IGizmoError
    {
        private VFXNodeController m_SourceNode;
        private int m_expressionHashCode;

        public VFXNodeController sourceNode
        {
            get
            {
                return m_SourceNode;
            }
        }

        public VFXCoordinateSpace space
        {
            get
            {
                return model.space;
            }
            set
            {
                model.space = value;
            }
        }

        public bool spaceableAndMasterOfSpace
        {
            get
            {
                return model.spaceable && model.IsMasterSlot();
            }
        }

        public bool IsSpaceInherited()
        {
            return model.IsSpaceInherited();
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


        IEnumerable<int> IPropertyRMProvider.filteredOutEnumerators { get { return null; } }

        public Type storageType
        {
            get
            {
                if (typeof(Texture).IsAssignableFrom(portType))
                {
                    return typeof(Texture);
                }

                return portType;
            }
        }

        public VFXDataAnchorController(VFXSlot model, VFXNodeController sourceNode, bool hidden) : base(sourceNode.viewController, model)
        {
            m_SourceNode = sourceNode;
            m_Hidden = hidden;
            m_Expanded = expandedSelf;

            if (model != null)
            {
                isSubgraphActivation = sourceNode.model is VFXSubgraphBlock && model.name == VFXBlock.activationSlotName;

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

            // This method is called every time a value change in the expression which is way to often
            // Currently we only want to refresh the gizmo when the expression change (especially when space or "can evaluate" change)
            // That's why we cache the expression hash code
            if (m_GizmoContext != null)
            {
                HashSet<VFXExpression> expressions = new HashSet<VFXExpression>();
                model.GetExpressions(expressions);

                var currentExpressionHashCode = UIUtilities.GetHashCode(expressions);
                if (currentExpressionHashCode != m_expressionHashCode)
                {
                    RefreshGizmo();
                    m_expressionHashCode = currentExpressionHashCode;
                }
            }

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

        // Used to hide activation slot and forbid linking
        public bool isSubgraphActivation { get; }

        public virtual bool HasLink()
        {
            return model.HasLink();
        }

        public class CanLinkCache
        {
            internal HashSet<IVFXSlotContainer> localChildrenOperator = new HashSet<IVFXSlotContainer>();
            internal HashSet<IVFXSlotContainer> localParentOperator = new HashSet<IVFXSlotContainer>();
        }

        public bool CanLinkToNode(VFXNodeController nodeController, CanLinkCache cache)
        {
            if (isSubgraphActivation)
                return false;

            if (nodeController == sourceNode)
                return false;

            if (cache == null)
                cache = new CanLinkCache();

            cache.localChildrenOperator.Clear();
            cache.localParentOperator.Clear();

            bool result;
            if (direction != Direction.Input)
            {
                VFXViewController.CollectAncestorOperator(sourceNode.slotContainer, cache.localParentOperator);
                result = !cache.localParentOperator.Contains(nodeController.slotContainer);
            }
            else
            {
                VFXViewController.CollectDescendantOperator(sourceNode.slotContainer, cache.localChildrenOperator);
                result = !cache.localChildrenOperator.Contains(nodeController.slotContainer);
            }

            return result;
        }

        public virtual bool CanLink(VFXDataAnchorController controller, CanLinkCache cache = null)
        {
            if (isSubgraphActivation)
                return false;

            if (controller.model != null)
            {
                if (model.CanLink(controller.model) && controller.model.CanLink(model))
                {
                    if (!CanLinkToNode(controller.sourceNode, cache))
                        return false;

                    return true;
                }
                return sourceNode.CouldLink(this, controller, cache);
            }

            return controller.CanLink(this, cache);
        }

        public virtual VFXParameter.NodeLinkedSlot CreateLinkTo(VFXDataAnchorController output, bool revertTypeConstraint = false)
        {
            var slotOutput = output != null ? output.model : null;
            var slotInput = model;
            sourceNode.WillCreateLink(ref slotInput, ref slotOutput, revertTypeConstraint);

            if (slotInput != null && slotOutput != null && slotInput.Link(slotOutput))
            {
                return new VFXParameter.NodeLinkedSlot() { inputSlot = slotInput, outputSlot = slotOutput };
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

                if (model.spaceable && model.children.Count() == 1)
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

        VFXPropertyAttributes m_Attributes;

        public virtual void UpdateInfos()
        {
            portType = model.property.type;
            m_Attributes = model.property.attributes;
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
                            Profiler.BeginSample("GetEvaluatedContent");
                            var evaluatedValue = nodeController.GetEvaluatedContent(model);
                            Profiler.EndSample();
                            if (evaluatedValue != null)
                            {
                                if (typeof(UnityObject).IsAssignableFrom(storageType))
                                {
                                    int instanceID = (int)evaluatedValue;
                                    return VFXConverter.ConvertTo(EditorUtility.InstanceIDToObject(instanceID), storageType);
                                }
                                else
                                    return VFXConverter.ConvertTo(evaluatedValue, storageType);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError("Trying to get the value from expressions threw." + e.Message + " In anchor : " + name + " from node :" + sourceNode.title);
                        }
                    }
                    return VFXConverter.ConvertTo(model.value, storageType);
                }
                else
                {
                    return null;
                }
            }

            set { SetPropertyValue(VFXConverter.ConvertTo(value, storageType)); }
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
                return new object[] { };
            }
        }

        public VFXPropertyAttributes attributes
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
        bool IPropertyRMProvider.expandableIfShowsEverything { get { return true; } }

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
            if (!slot || slot.HasLink(true))
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
                return m_Editable;
            }
        }

        private void SetPropertyValue(object value)
        {
            if (model.value != value)
            {
                Undo.RecordObject(model.GetMasterSlot(), "VFX port value (" + model.GetMasterSlot().value?.ToString() ?? "null" + ")"); // The slot value is stored on the master slot, not necessarily my own slot
                model.value = value;
            }
        }

        public static bool SlotShouldSkipFirstLevel(VFXSlot slot)
        {
            return slot is VFXSlotEncapsulated;
        }

        public virtual void ExpandPath()
        {
            if (model == null || !model.collapsed)
                return;

            Undo.RecordObject(model, "Expand port");
            model.collapsed = false;
            if (SlotShouldSkipFirstLevel(model))
            {
                model.children.First().collapsed = model.collapsed;
            }
        }

        public virtual void RetractPath()
        {
            if (model == null || model.collapsed)
                return;

            Undo.RecordObject(model, "Collapse port");
            model.collapsed = true;
            if (SlotShouldSkipFirstLevel(model))
            {
                model.children.First().collapsed = model.collapsed;
            }
        }

        void RefreshGizmo()
        {
            if (m_GizmoContext != null) m_GizmoContext.Unprepare();
            if (model == null || model.IsMasterSlot()) return;

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

        public Bounds GetGizmoBounds(VisualEffect component)
        {
            if (m_GizmoContext != null)
            {
                return VFXGizmoUtility.GetGizmoBounds(m_GizmoContext, component);
            }

            return new Bounds();
        }

        public GizmoError GetGizmoError(VisualEffect component)
        {
            if (!VFXGizmoUtility.HasGizmo(portType))
                return GizmoError.None;
            CreateGizmoContextIfNeeded();

            return VFXGizmoUtility.CollectGizmoError(m_GizmoContext, component);
        }

        VFXDataAnchorGizmoContext m_GizmoContext;

        public void DrawGizmo(VisualEffect component)
        {
            if (VFXGizmoUtility.HasGizmo(portType))
            {
                CreateGizmoContextIfNeeded();
                VFXGizmoUtility.Draw(m_GizmoContext, component);
            }
        }

        void CreateGizmoContextIfNeeded()
        {
            if (m_GizmoContext == null)
            {
                m_GizmoContext = new VFXDataAnchorGizmoContext(this);
            }
        }

        void IPropertyRMProvider.StartLiveModification()
        {
            sourceNode.viewController.errorRefresh = false;
        }

        void IPropertyRMProvider.EndLiveModification()
        {
            sourceNode.viewController.errorRefresh = true;
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
            get { return true; }
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
            get { return false; }
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
        public override bool CanLink(VFXDataAnchorController controller, CanLinkCache cache = null)
        {
            if (isSubgraphActivation)
                return false;

            var op = (sourceNode as VFXCascadedOperatorController);

            if (op == null)
                return false;

            if (controller is VFXUpcommingDataAnchorController)
                return false;

            if (!CanLinkToNode(controller.sourceNode, cache))
                return false;

            return op.model.GetBestAffinityType(controller.model.property.type) != null;
        }

        public new VFXCascadedOperatorController sourceNode
        {
            get { return base.sourceNode as VFXCascadedOperatorController; }
        }

        public override VFXParameter.NodeLinkedSlot CreateLinkTo(VFXDataAnchorController output, bool revertTypeConstraint = false)
        {
            var slotOutput = output != null ? output.model : null;

            VFXOperatorNumericCascadedUnified op = sourceNode.model;

            op.AddOperand(op.GetBestAffinityType(output.model.property.type));

            var slotInput = op.GetInputSlot(op.GetNbInputSlots() - 1);
            if (slotInput != null && slotOutput != null && slotInput.Link(slotOutput))
            {
                return new VFXParameter.NodeLinkedSlot() { inputSlot = slotInput, outputSlot = slotOutput };
            }

            return new VFXParameter.NodeLinkedSlot();
        }
    }

    class VFXDataAnchorGizmoContext : VFXGizmoUtility.Context
    {
        // Provider
        internal VFXDataAnchorGizmoContext(VFXDataAnchorController controller)
        {
            m_Controller = controller;
        }

        VFXDataAnchorController m_Controller;

        public override Type portType
        {
            get { return m_Controller.portType; }
        }

        List<object> stack = new List<object>();
        public override object value
        {
            get
            {
                // If the vfxwindow is hidden then Update will not be called, which in turn will not recompile the expression graph. so try recompiling it now
                m_Controller.viewController.RecompileExpressionGraphIfNeeded();
                stack.Clear();
                foreach (var action in m_ValueBuilder)
                {
                    action(stack);
                }

                return stack.First();
            }
        }
        public override VFXCoordinateSpace space
        {
            get
            {
                return m_Controller.space;
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
                        m_Error |= GizmoError.HasLinkIndeterminate;
                        return;
                    }
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
                    object result = null;
                    if (subSlot.HasLink(true) && m_Controller.viewController.CanGetEvaluatedContent(subSlot) && (result = m_Controller.viewController.GetEvaluatedContent(subSlot)) != null)
                    {
                        m_ValueBuilder.Add(o => o.Add(m_Controller.viewController.GetEvaluatedContent(subSlot)));
                    }
                    else if (subSlot.HasLink(false) && VFXTypeUtility.GetComponentCount(subSlot) != 0) // replace by is VFXType
                    {
                        m_Error |= GizmoError.HasLinkIndeterminate;
                        return;
                    }
                    else
                    {
                        m_ValueBuilder.Add(o => o.Add(subSlot.value));
                        BuildValue(subSlot);
                        if (m_Error != GizmoError.None)
                            return;
                    }
                    m_ValueBuilder.Add(o =>
                    {
                        var newValue = o[o.Count - 1];
                        if (newValue != null)
                        {
                            var target = o[o.Count - 2];

                            if (field.FieldType != newValue.GetType())
                            {
                                if (!VFXConverter.TryConvertTo(newValue, field.FieldType, out var convertedValue))
                                    throw new InvalidOperationException($"VFXDataAnchorGizmo is failing to convert from {newValue.GetType()} to {field.FieldType}");
                                newValue = convertedValue;
                            }

                            field.SetValue(target, newValue);
                        }
                    });
                    m_ValueBuilder.Add(o => o.RemoveAt(o.Count - 1));
                }
            }
        }

        public override VFXGizmo.IProperty<T> RegisterProperty<T>(string member)
        {
            object result;
            if (m_PropertyCache.TryGetValue(member, out result))
            {
                if (result is VFXGizmo.IProperty<T>)
                    return result as VFXGizmo.IProperty<T>;
                else
                    return VFXGizmoUtility.NullProperty<T>.defaultProperty;
            }
            var controller = GetMemberController(member);

            if (controller != null && controller.portType == typeof(T))
            {
                bool readOnly = false;
                var slot = controller.model;
                if (slot.HasLink(true))
                    readOnly = true;
                else
                {
                    slot = slot.GetParent();
                    while (slot != null)
                    {
                        if (slot.HasLink(false))
                        {
                            readOnly = true;
                            break;
                        }
                        slot = slot.GetParent();
                    }
                }


                return new VFXGizmoUtility.Property<T>(controller, !readOnly);
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
    }
}
