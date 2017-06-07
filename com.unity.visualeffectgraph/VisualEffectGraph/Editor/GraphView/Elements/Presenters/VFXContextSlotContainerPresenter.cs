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

            base.position = new Rect(model.position,Vector2.one);

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
        }

        protected void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model as IVFXSlotContainer == slotContainer && cause == VFXModel.InvalidationCause.kStructureChanged)
            {
                var inputs = inputAnchors;
                m_InputAnchors.Clear();
                List<NodeAnchorPresenter> newAnchors = new List<NodeAnchorPresenter>();

                UpdateSlots(newAnchors, slotContainer.inputSlots,true, true);
                m_InputAnchors = newAnchors;
                newAnchors = new List<NodeAnchorPresenter>();
                UpdateSlots(newAnchors, slotContainer.outputSlots,true, false);
                m_OutputAnchors = newAnchors;
            }
        }

        void UpdateSlots(List<NodeAnchorPresenter> newAnchors, IEnumerable<VFXSlot> slotList, bool expanded,bool input)
        {
            foreach (VFXSlot slot in slotList)
            {
                VFXDataAnchorPresenter propPresenter = GetPropertyPresenter(slot,input);

                if (propPresenter == null)
                {
                    propPresenter = AddDataAnchor(slot,input);
                }
                newAnchors.Add(propPresenter);

                propPresenter.UpdateInfos(expanded);

                UpdateSlots(newAnchors, slot.children, expanded && slot.expanded,input);
            }
        }
        public VFXDataAnchorPresenter GetPropertyPresenter(VFXSlot slot,bool input)
        {
            VFXDataAnchorPresenter result = null;

            if( input )
                result = inputAnchors.Cast<VFXDataAnchorPresenter>().Where(t=> t.model == slot ).FirstOrDefault();
            else
                result = outputAnchors.Cast<VFXDataAnchorPresenter>().Where(t => t.model == slot).FirstOrDefault();

            return result;
        }
        protected virtual VFXDataAnchorPresenter AddDataAnchor(VFXSlot slot,bool input)
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
    class VFXContextSlotContainerPresenter : VFXSlotContainerPresenter
    {

        protected override VFXDataAnchorPresenter AddDataAnchor(VFXSlot slot,bool input)
        {
            if( input )
            {
                VFXContextDataInputAnchorPresenter anchorPresenter = CreateInstance<VFXContextDataInputAnchorPresenter>();
                anchorPresenter.Init(slot, this);
                contextPresenter.viewPresenter.RegisterDataAnchorPresenter(anchorPresenter);

                return anchorPresenter;
            }
            return null;
        }

        public void Init(VFXModel model, VFXContextPresenter contextPresenter)
        {
            m_ContextPresenter = contextPresenter;
            base.Init(model,contextPresenter.viewPresenter);

            //TODO unregister when the block is destroyed
            (m_Model as VFXModel).onInvalidateDelegate += OnInvalidate;
        }

        public VFXContextPresenter contextPresenter
        {
            get { return m_ContextPresenter; }
        }

        public static bool IsTypeExpandable(System.Type type)
        {
            return !type.IsPrimitive && !typeof(Object).IsAssignableFrom(type) && type != typeof(AnimationCurve) && !type.IsEnum && type != typeof(Gradient);
        }

        static bool ShouldSkipLevel(Type type)
        {
            return typeof(Spaceable).IsAssignableFrom(type) && type.GetFields().Length == 2; // spaceable having only one member plus their space member.
        }

        bool ShouldIgnoreMember(Type type, FieldInfo field)
        {
            return typeof(Spaceable).IsAssignableFrom(type) && field.Name == "space";
        }

        protected VFXContextPresenter m_ContextPresenter;
    }
}
