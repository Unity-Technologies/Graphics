using System;
using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXSlotContainerPresenter : VFXLinkablePresenter
    {
        public override IVFXSlotContainer slotContainer { get { return m_Model; } }
        protected new void OnEnable()
        {
        }

        protected VFXContextDataInputAnchorPresenter AddDataAnchor(VFXSlot slot)
        {
            VFXContextDataInputAnchorPresenter anchorPresenter = CreateInstance<VFXContextDataInputAnchorPresenter>();
            anchorPresenter.Init(slotContainer as VFXModel, slot, this);
            ContextPresenter.ViewPresenter.RegisterDataAnchorPresenter(anchorPresenter);

            return anchorPresenter;
        }

        public void Init(IVFXSlotContainer model, VFXContextPresenter contextPresenter)
        {
            m_Model = model;
            if (m_Model == null)
            {
                Debug.LogError("Model must not be null");
            }
            m_ContextPresenter = contextPresenter;
            base.Init(contextPresenter.ViewPresenter);

            //TODO unregister when the block is destroyed
            (m_Model as VFXModel).onInvalidateDelegate += OnInvalidate;


            object settings = m_Model.settings;
            if( settings != null )
            {
                m_Settings = new VFXSettingPresenter[settings.GetType().GetFields().Length];
                int cpt = 0;
                foreach (var member in settings.GetType().GetFields())
                {
                    VFXSettingPresenter settingPresenter = VFXSettingPresenter.CreateInstance<VFXSettingPresenter>();

                    settingPresenter.Init(this.m_Model, member.Name, member.FieldType);
                    m_Settings[cpt++] = settingPresenter;
                }
            }

            OnInvalidate((m_Model as VFXModel), VFXModel.InvalidationCause.kStructureChanged);
        }

        protected void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model is IVFXSlotContainer && cause == VFXModel.InvalidationCause.kStructureChanged)
            {
                var inputs = inputAnchors;
                inputAnchors.Clear();
                Dictionary<VFXSlot, VFXContextDataInputAnchorPresenter> newAnchors = new Dictionary<VFXSlot, VFXContextDataInputAnchorPresenter>();

                IVFXSlotContainer block = m_Model as IVFXSlotContainer;
                UpdateSlots(newAnchors, block.inputSlots, true);
                m_Anchors = newAnchors;
            }
        }

        void UpdateSlots(Dictionary<VFXSlot, VFXContextDataInputAnchorPresenter> newAnchors , IEnumerable<VFXSlot> slotList, bool expanded)
        {
            foreach (VFXSlot slot in slotList)
            {
                VFXContextDataInputAnchorPresenter propPresenter = GetPropertyPresenter(slot);

                if (propPresenter == null)
                {
                    propPresenter = AddDataAnchor(slot);
                }
                newAnchors[slot] = propPresenter;

                propPresenter.UpdateInfos(expanded);
                inputAnchors.Add(propPresenter);

                UpdateSlots(newAnchors, slot.children, expanded && slot.expanded);
            }
        }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, m_Model as VFXModel};
        }

        public VFXContextPresenter ContextPresenter
        {
            get { return m_ContextPresenter; }
        }

        public VFXContextDataInputAnchorPresenter GetPropertyPresenter(VFXSlot slot)
        {
            VFXContextDataInputAnchorPresenter result = null;

            m_Anchors.TryGetValue(slot, out result);

            return result;
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

        public override IEnumerable<GraphElementPresenter> allChildren
        {
            get
            {
                foreach (var kv in m_Anchors)
                {
                    yield return kv.Value;
                }
                foreach(var v in m_Settings)
                {
                    yield return v;
                }
            }
        }

        public IEnumerable<VFXSettingPresenter> settings
        {
            get { return m_Settings; }
        }

        public IEnumerable<VFXContextDataInputAnchorPresenter> anchors
        {
            get { return m_Anchors.Values; }
        }

        [SerializeField]
        private Dictionary<VFXSlot, VFXContextDataInputAnchorPresenter> m_Anchors = new Dictionary<VFXSlot, VFXContextDataInputAnchorPresenter>();

        [SerializeField]
        private VFXSettingPresenter[] m_Settings;

        [SerializeField]
        private IVFXSlotContainer m_Model;


        protected VFXContextPresenter m_ContextPresenter;
    }
}
