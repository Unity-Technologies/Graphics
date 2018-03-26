using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;
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

        IDataWatchHandle m_MasterSlotHandle;

        public Type portType { get; set; }

        public VFXDataAnchorController(VFXSlot model, VFXNodeController sourceNode, bool hidden) : base(model)
        {
            m_SourceNode = sourceNode;
            m_Hidden = hidden;
            m_Expanded = expandedSelf;

            portType = model.property.type;

            if (model.GetMasterSlot() != null && model.GetMasterSlot() != model)
            {
                m_MasterSlotHandle = DataWatchService.sharedInstance.AddWatch(model.GetMasterSlot(), MasterSlotChanged);
            }
            ModelChanged(model);
        }

        void MasterSlotChanged(UnityEngine.Object obj)
        {
            if (m_MasterSlotHandle == null)
                return;
            ModelChanged(obj);
        }

        bool m_Expanded;

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            if (expandedSelf != m_Expanded)
            {
                m_Expanded = expandedSelf;
                UpdateHiddenRecursive(m_Hidden, true);
            }
            UpdateInfos();

            sourceNode.DataEdgesMightHaveChanged();
            NotifyChange(AnyThing);
        }

        public override void OnDisable()
        {
            if (m_MasterSlotHandle != null)
            {
                DataWatchService.sharedInstance.RemoveWatch(m_MasterSlotHandle);
                m_MasterSlotHandle = null;
            }
            base.OnDisable();
        }

        public bool HasLink()
        {
            return model.HasLink();
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

        public object value
        {
            get
            {
                if (portType != null)
                {
                    if (!editable)
                    {
                        VFXViewController controller = m_SourceNode.viewController;

                        try
                        {
                            if (controller.CanGetEvaluatedContent(model))
                            {
                                return VFXConverter.ConvertTo(controller.GetEvaluatedContent(model), portType);
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

        public void DrawGizmo(VisualEffect component)
        {
            VFXValueGizmo.Draw(this, component);
        }
    }
}
