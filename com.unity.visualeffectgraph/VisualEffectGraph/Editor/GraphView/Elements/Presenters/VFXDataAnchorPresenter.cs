using System;
using System.Collections.Generic;
using UnityEngine;
using UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXDataAnchorPresenter : NodeAnchorPresenter, IPropertyRMProvider
    {
        [SerializeField]
        private VFXSlot m_Model;
        public VFXSlot model { get { return m_Model; } }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, m_Model };
        }

        private VFXSlotContainerPresenter m_SourceNode;

        public VFXSlotContainerPresenter sourceNode
        {
            get
            {
                return m_SourceNode;
            }
        }

        public void Init(VFXSlot model, VFXSlotContainerPresenter scPresenter)
        {
            m_Model = model;
            m_SourceNode = scPresenter;

            anchorType = model.property.type;
            name = model.property.name;

            UpdateHidden();

            m_Model.onInvalidateDelegate += OnInvalidate;
        }

        void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            UpdateHidden();
        }

        private void UpdateHidden()
        {
            m_Hidden = false;


            VFXSlot parent = m_Model.GetParent();
            while (parent != null)
            {
                if (!parent.expanded)
                {
                    m_Hidden = true;
                    break;
                }
                parent = parent.GetParent();
            }
        }

        public virtual void UpdateInfos(bool expanded)
        {
            anchorType = model.property.type;
        }

        public object value
        {
            get
            {
                if (anchorType != null)
                {
                    if (!editable)
                    {
                        VFXViewPresenter presenter = m_SourceNode.viewPresenter;

                        if (presenter.CanGetEvaluatedContent(model))
                        {
                            return VFXConverter.ConvertTo(presenter.GetEvaluatedContent(model), anchorType);
                        }
                    }

                    return VFXConverter.ConvertTo(model.value, anchorType);
                }
                else
                {
                    return null;
                }
            }

            set { SetPropertyValue(VFXConverter.ConvertTo(value, anchorType)); }
        }


        public override void Connect(EdgePresenter edgePresenter)
        {
            base.Connect(edgePresenter);
        }

        public string path
        {
            get { return model.path; }
        }

        public int depth
        {
            get { return model.depth; }
        }

        public bool expanded
        {
            get { return model.expanded; }
        }

        public virtual bool expandable
        {
            get { return VFXContextSlotContainerPresenter.IsTypeExpandable(anchorType); }
        }

        public virtual string iconName
        {
            get { return anchorType.Name; }
        }

        [SerializeField]
        private bool m_Hidden;

        public override bool collapsed
        {
            get
            {
                return m_Hidden && !connected;
            }
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
                        if (slot.LinkedSlots.Count > 0)
                        {
                            editable = false;
                            break;
                        }
                        slot = slot.GetParent();
                    }


                    foreach (VFXSlot child in model.children)
                    {
                        if (child.LinkedSlots.Count > 0)
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
            Undo.RecordObject(model, "Change Value");
            model.value = value;
        }

        public void ExpandPath()
        {
            Undo.RecordObject(model, "Expand path");
            model.expanded = true;
            model.Invalidate(VFXModel.InvalidationCause.kParamExpanded);
        }

        public void RetractPath()
        {
            Undo.RecordObject(model, "Retract path");
            model.expanded = false;
            model.Invalidate(VFXModel.InvalidationCause.kParamExpanded);
        }
    }
}
