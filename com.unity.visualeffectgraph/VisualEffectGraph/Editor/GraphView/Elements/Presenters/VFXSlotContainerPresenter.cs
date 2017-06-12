using System;
using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXSlotContainerPresenter : NodePresenter
    {
        public VFXModel model { get { return m_Model; } }
        public VFXViewPresenter viewPresenter { get { return m_ViewPresenter; } }

        public override Rect position
        {
            get
            {
                return base.position;
            }

            set
            {
                base.position = value;
                Undo.RecordObject(model, "Position");
                model.position = position.position;
            }
        }
        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, m_Model };
        }

        public virtual void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            m_Model = model;
            m_ViewPresenter = viewPresenter;

            base.position = new Rect(model.position, Vector2.one);

            object settings = slotContainer.settings;
            if (settings != null)
            {
                m_Settings = new VFXSettingPresenter[settings.GetType().GetFields().Length];
                int cpt = 0;
                foreach (var member in settings.GetType().GetFields())
                {
                    VFXSettingPresenter settingPresenter = VFXSettingPresenter.CreateInstance<VFXSettingPresenter>();

                    settingPresenter.Init(this.slotContainer, member.Name, member.FieldType);
                    m_Settings[cpt++] = settingPresenter;
                }
            }
            OnInvalidate(m_Model, VFXModel.InvalidationCause.kStructureChanged);
            //TODO unregister when the block is destroyed
            m_Model.onInvalidateDelegate += OnInvalidate;
        }

        protected void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model as IVFXSlotContainer == slotContainer && cause == VFXModel.InvalidationCause.kStructureChanged)
            {
                var inputs = inputAnchors;
                m_InputAnchors.Clear();
                List<NodeAnchorPresenter> newAnchors = new List<NodeAnchorPresenter>();

                UpdateSlots(newAnchors, slotContainer.inputSlots, true, true);

                foreach (var anchor in inputAnchors.Except(newAnchors).Cast<VFXDataAnchorPresenter>())
                {
                    viewPresenter.UnregisterDataAnchorPresenter(anchor);
                }
                m_InputAnchors = newAnchors;
                newAnchors = new List<NodeAnchorPresenter>();
                UpdateSlots(newAnchors, slotContainer.outputSlots, true, false);

                foreach (var anchor in outputAnchors.Except(newAnchors).Cast<VFXDataAnchorPresenter>())
                {
                    viewPresenter.UnregisterDataAnchorPresenter(anchor);
                }
                m_OutputAnchors = newAnchors;
            }
        }

        void UpdateSlots(List<NodeAnchorPresenter> newAnchors, IEnumerable<VFXSlot> slotList, bool expanded, bool input)
        {
            foreach (VFXSlot slot in slotList.ToArray())
            {
                VFXDataAnchorPresenter propPresenter = GetPropertyPresenter(slot, input);

                if (propPresenter == null)
                {
                    propPresenter = AddDataAnchor(slot, input);
                }
                newAnchors.Add(propPresenter);
                viewPresenter.RegisterDataAnchorPresenter(propPresenter);

                propPresenter.UpdateInfos(expanded);

                UpdateSlots(newAnchors, slot.children, expanded && slot.expanded, input);
            }
        }

        public VFXDataAnchorPresenter GetPropertyPresenter(VFXSlot slot, bool input)
        {
            VFXDataAnchorPresenter result = null;

            if (input)
                result = inputAnchors.Cast<VFXDataAnchorPresenter>().Where(t => t.model == slot).FirstOrDefault();
            else
                result = outputAnchors.Cast<VFXDataAnchorPresenter>().Where(t => t.model == slot).FirstOrDefault();

            return result;
        }

        protected virtual VFXDataAnchorPresenter AddDataAnchor(VFXSlot slot, bool input)
        {
            return null;
        }

        public IVFXSlotContainer slotContainer { get { return m_Model as IVFXSlotContainer; } }

        public IEnumerable<VFXSettingPresenter> settings
        {
            get { return m_Settings; }
        }

        public virtual bool enabled
        {
            get { return true; }
        }

        public bool expanded
        {
            get
            {
                return slotContainer.expanded;
            }

            set
            {
                if (value != slotContainer.expanded)
                {
                    Undo.RecordObject(slotContainer as UnityEngine.Object, "Collapse");
                    slotContainer.expanded = value;
                }
            }
        }

        [SerializeField]
        protected VFXModel         m_Model;

        [SerializeField]
        private VFXSettingPresenter[] m_Settings;

        protected VFXViewPresenter m_ViewPresenter;
    }
}
