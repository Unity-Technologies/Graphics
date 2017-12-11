using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    interface IVFXAnchorPresenter
    {
        void Connect(VFXEdgeController edgePresenter);
        void Disconnect(VFXEdgeController edgePresenter);
    }

    abstract class VFXDataAnchorPresenter : VFXController<VFXSlot>, IVFXAnchorPresenter, IPropertyRMProvider, IValuePresenter
    {
        private VFXSlotContainerPresenter m_SourceNode;

        public VFXSlotContainerPresenter sourceNode
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

        public void Init(VFXSlot model, VFXSlotContainerPresenter scPresenter, bool hidden)
        {
            base.Init(model);
            m_SourceNode = scPresenter;
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

        [SerializeField]
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
                        VFXViewPresenter presenter = m_SourceNode.viewPresenter;

                        if (presenter.CanGetEvaluatedContent(model))
                        {
                            return VFXConverter.ConvertTo(presenter.GetEvaluatedContent(model), portType);
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


        List<VFXDataEdgePresenter> m_Connections = new List<VFXDataEdgePresenter>();

        public virtual void Connect(VFXEdgeController edgePresenter)
        {
            m_Connections.Add(edgePresenter as VFXDataEdgePresenter);
        }

        public virtual void Disconnect(VFXEdgeController edgePresenter)
        {
            m_Connections.Remove(edgePresenter as VFXDataEdgePresenter);
        }

        public bool connected
        {
            get { return m_Connections.Count > 0; }
        }

        public IEnumerable<VFXDataEdgePresenter> connections { get { return m_Connections; } }

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
            get { return VFXContextSlotContainerPresenter.IsTypeExpandable(portType); }
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
