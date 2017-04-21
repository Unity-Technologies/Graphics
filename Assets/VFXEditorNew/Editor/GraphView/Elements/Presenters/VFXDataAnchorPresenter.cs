using System;
using System.Collections.Generic;
using UnityEngine;
using UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXDataAnchorPresenter : NodeAnchorPresenter
    {
        [SerializeField]
        VFXModel m_Owner;
        public VFXModel Owner { get { return m_Owner; } }

        [SerializeField]
        private VFXSlot m_Model;
        public VFXSlot model { get { return m_Model; } }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, m_Model };
        }


        private VFXLinkablePresenter m_SourceNode;

        public VFXLinkablePresenter sourceNode
        {
            get
            {
                return m_SourceNode;
            }
        }

        public void Init(VFXModel owner,VFXSlot model, VFXLinkablePresenter nodePresenter)
        {
            m_Owner = owner;
            m_Model = model;
            m_SourceNode = nodePresenter;

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

        public object value
        {
            get { return model.value; }
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
            get { return false; }
        }

        [SerializeField]
        private bool m_Hidden;

        public bool hidden
        {
            get{

                return m_Hidden;
            }
        }
        public bool editable
        {
            get
            {
                bool editable = true;

                VFXSlot slot = model;
                while(slot != null )
                {
                    if( slot.LinkedSlots.Count > 0)
                    {
                        editable = false;
                        break;
                    }
                    slot = slot.GetParent();
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
            Undo.RecordObject(model, "Expand path");
            model.expanded = true;
            model.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }

        public void RetractPath()
        {
            Undo.RecordObject(model, "Retract path");
            model.expanded = false;
            model.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }
    }

}
