using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;

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
        private VFXSlotContainerController m_SourceNode;

        public VFXSlotContainerController sourceNode
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
                if (model.GetExpression() == null)
                    return "Empty";
                return base.name;
            }
        }

        IDataWatchHandle m_MasterSlotHandle;

        public Type portType { get; set; }

        public VFXDataAnchorController(VFXSlot model, VFXSlotContainerController sourceNode, bool hidden) : base(model)
        {
            m_SourceNode = sourceNode;
            m_Hidden = hidden;
            m_Collapsed = model.collapsed;

            portType = model.property.type;

            if (model.GetMasterSlot() != null && model.GetMasterSlot() != model)
            {
                m_MasterSlotHandle = DataWatchService.sharedInstance.AddWatch(model.GetMasterSlot(), MasterSlotChanged);
            }
        }

        void MasterSlotChanged(UnityEngine.Object obj)
        {
            ModelChanged(obj);
        }

        bool m_Collapsed;

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            if (model.collapsed != m_Collapsed)
            {
                m_Collapsed = model.collapsed;
                UpdateHiddenRecursive(m_Hidden, true);
            }
            UpdateInfos();

            sourceNode.viewController.DataEdgesMightHaveChanged();
            NotifyChange(AnyThing);
        }

        public override void OnDisable()
        {
            if (m_MasterSlotHandle != null)
            {
                DataWatchService.sharedInstance.RemoveWatch(m_MasterSlotHandle);
            }
            base.OnDisable();
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
                foreach (var element in model.children.Select(t => ports.First(u => u.model == t)))
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

        public object value
        {
            get
            {
                if (portType != null)
                {
                    if (!editable)
                    {
                        VFXViewController controller = m_SourceNode.viewController;

                        if (controller.CanGetEvaluatedContent(model))
                        {
                            return VFXConverter.ConvertTo(controller.GetEvaluatedContent(model), portType);
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
        }

        public virtual void Disconnect(VFXEdgeController edgeController)
        {
            m_Connections.Remove(edgeController as VFXDataEdgeController);
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

        public int depth
        {
            get { return model.depth; }
        }

        public virtual bool expandable
        {
            get { return VFXContextSlotContainerController.IsTypeExpandable(portType); }
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

        public bool expandedSelf
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

        public bool editable
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
                        if (slot.LinkedSlots.Count() > 0)
                        {
                            editable = false;
                            break;
                        }
                        slot = slot.GetParent();
                    }


                    foreach (VFXSlot child in model.children)
                    {
                        if (child.LinkedSlots.Count() > 0)
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
            model.value = value;
        }

        public void ExpandPath()
        {
            model.collapsed = false;
            if (typeof(ISpaceable).IsAssignableFrom(model.property.type) && model.children.Count() == 1)
            {
                model.children.First().collapsed = model.collapsed;
            }
        }

        public void RetractPath()
        {
            model.collapsed = true;
            if (typeof(ISpaceable).IsAssignableFrom(model.property.type) && model.children.Count() == 1)
            {
                model.children.First().collapsed = model.collapsed;
            }
        }

        public void DrawGizmo(VFXComponent component)
        {
            VFXValueGizmo.Draw(this, component);
        }
    }
}
