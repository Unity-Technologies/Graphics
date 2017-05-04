using System;
using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXBlockPresenter : VFXLinkablePresenter
    {
        public override IVFXSlotContainer slotContainer { get { return m_Model; } }
        protected new void OnEnable()
		{
			capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Movable;

            // Most initialization will be done in Init
		}

        VFXBlockDataInputAnchorPresenter AddDataAnchor(VFXSlot slot)
        {
            VFXBlockDataInputAnchorPresenter anchorPresenter = CreateInstance<VFXBlockDataInputAnchorPresenter>();
            anchorPresenter.Init(Model, slot, this);
            ContextPresenter.ViewPresenter.RegisterDataAnchorPresenter(anchorPresenter);

            return anchorPresenter;
        }

        public void Init(VFXBlock model,VFXContextPresenter contextPresenter)
        {
            m_Model = model;
            m_ContextPresenter = contextPresenter;


            //TODO unregister when the block is destroyed
            model.onInvalidateDelegate += OnInvalidate;

            OnInvalidate(model, VFXModel.InvalidationCause.kParamChanged);
        }

        private void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if( model is VFXBlock && cause == VFXModel.InvalidationCause.kStructureChanged)
            {
                var inputs = inputAnchors;
                inputAnchors.Clear();
                Dictionary<VFXSlot, VFXBlockDataInputAnchorPresenter> newAnchors = new Dictionary<VFXSlot, VFXBlockDataInputAnchorPresenter>();

                VFXBlock block = model as VFXBlock;
                UpdateSlots(newAnchors, block.inputSlots,true);
                m_Anchors = newAnchors;
            }
        }

        void UpdateSlots(Dictionary<VFXSlot, VFXBlockDataInputAnchorPresenter> newAnchors , IEnumerable<VFXSlot> slotList,bool expanded)
        {
            foreach (VFXSlot slot in slotList)
            {
                VFXBlockDataInputAnchorPresenter propPresenter = GetPropertyPresenter(slot);

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

        public VFXBlock Model
        {
            get { return m_Model; }
        }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, m_Model };
        }

        public VFXContextPresenter ContextPresenter
        {
            get { return m_ContextPresenter; }
        }

        public int index
        {
            get { return m_ContextPresenter.blockPresenters.FindIndex(t=>t == this); }
        }

        public VFXBlockDataInputAnchorPresenter GetPropertyPresenter(VFXSlot slot)
        {
            VFXBlockDataInputAnchorPresenter result = null;

            m_Anchors.TryGetValue(slot, out result);

            return result;
        }

        [SerializeField]
        public struct PropertyInfo
        {
            public string name;
            public object value;
            public System.Type type;
            public bool expandable;
            public bool expanded;
            public int depth;
            public string parentPath;

            public string path { get { return !string.IsNullOrEmpty(parentPath)?parentPath + "." + name : name; } }
        }

        

        public static bool IsTypeExpandable(System.Type type)
        {
            return !type.IsPrimitive && !typeof(Object).IsAssignableFrom(type) && type != typeof(AnimationCurve) && ! type.IsEnum && type != typeof(Gradient);
        }



        bool ShouldSkipLevel(Type type)
        {
            return typeof(Spaceable).IsAssignableFrom(type) && type.GetFields().Length == 2; // spaceable having only one member plus their space member.
        }


        bool ShouldIgnoreMember(Type type, FieldInfo field)
        {
            return typeof(Spaceable).IsAssignableFrom(type) && field.Name == "space";
        }

        private IEnumerable<PropertyInfo> GetProperties(Type type, object value, string prefix, int depth)
        {
            if (type == null)
                yield break;

            FieldInfo[] infos = type.GetFields(BindingFlags.Public|BindingFlags.Instance);


            if( ShouldSkipLevel(type) )
            {
                foreach (var field in infos)
                {
                    if( ShouldIgnoreMember(type,field))
                        continue;

                    object fieldValue = field.GetValue(value);
                    string fieldPath = string.IsNullOrEmpty(prefix)? field.Name:prefix + "." + field.Name;

                    foreach (var subField in GetProperties(field.FieldType, fieldValue, fieldPath, depth + 1))
                    {
                        yield return subField;
                    }
                }
                yield break;
            }

            foreach (var field in infos)
            {
                if( ShouldIgnoreMember(type,field))
                    continue;

                object fieldValue = field.GetValue(value);


                string fieldPath = string.IsNullOrEmpty(prefix)? field.Name:prefix + "." + field.Name;
                bool expanded = m_Model.IsPathExpanded(fieldPath);

                yield return new PropertyInfo()
                {
                    name = field.Name,
                    value = fieldValue,
                    type = field.FieldType,
                    expandable = IsTypeExpandable(field.FieldType),
                    expanded = expanded,
                    depth = depth,
                    parentPath = prefix
                };
                if (expanded)
                {
                    foreach (var subField in GetProperties(field.FieldType, fieldValue, fieldPath, depth + 1))
                    {
                        yield return subField;
                    }
                }
            }
        }



        public override IEnumerable<GraphElementPresenter> allChildren
        {
            get {
                foreach (var kv in m_Anchors)
                {
                    yield return kv.Value;
                }
            }
        }
        [SerializeField]
        bool m_DirtyHack;//because serialization doesn't work with the below dictionary

        [SerializeField]
        private Dictionary<VFXSlot, VFXBlockDataInputAnchorPresenter> m_Anchors = new Dictionary<VFXSlot, VFXBlockDataInputAnchorPresenter>();

        [SerializeField]
        private VFXBlock m_Model;


        VFXContextPresenter m_ContextPresenter;
    }
}
