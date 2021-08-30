using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    class VFXSettingController : Controller, IPropertyRMProvider
    {
        IVFXSlotContainer m_Owner;
        public IVFXSlotContainer owner { get { return m_Owner; } }

        System.Type m_SettingType;

        string m_Name;

        public System.Type portType { get { return m_SettingType; } }

        public VFXViewController viewController { private set; get; }

        public void Init(VFXViewController viewController, IVFXSlotContainer owner, string name, System.Type type)
        {
            m_Owner = owner;
            m_Name = name;
            m_SettingType = type;
            this.viewController = viewController;
        }

        public string name
        {
            get { return m_Name; }
        }

        public object value
        {
            get
            {
                if (portType != null)
                {
                    return VFXConverter.ConvertTo(owner.GetSettingValue(name), portType);
                }
                else
                {
                    return null;
                }
            }

            set
            {
                m_Owner.SetSettingValue(name, VFXConverter.ConvertTo(value, portType));
            }
        }


        public string path
        {
            get { return name; }
        }

        public int depth
        {
            get { return 0; }
        }

        public bool expanded
        {
            get { return false; }
        }

        public virtual bool expandable
        {
            get { return false; }
        }
        bool IPropertyRMProvider.expandableIfShowsEverything { get { return false; } }


        IEnumerable<int> IPropertyRMProvider.filteredOutEnumerators { get { return (m_Owner as VFXModel).GetFilteredOutEnumerators(name); } }

        public virtual string iconName
        {
            get { return portType.Name; }
        }

        public bool editable
        {
            get { return true; }
        }

        public VFXPropertyAttributes attributes
        {
            get
            {
                return new VFXPropertyAttributes(customAttributes);
            }
        }

        public object[] customAttributes
        {
            get
            {
                var customAttributes = owner.GetSetting(path).field.GetCustomAttributes(true);
                return customAttributes;
            }
        }

        public VFXCoordinateSpace space
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool spaceableAndMasterOfSpace { get { return false; } }

        public bool IsSpaceInherited()
        {
            throw new NotImplementedException();
        }

        public void ExpandPath()
        {
        }

        public void RetractPath()
        {
        }

        public override void ApplyChanges()
        {
        }

        void IPropertyRMProvider.StartLiveModification() { viewController.errorRefresh = false; }
        void IPropertyRMProvider.EndLiveModification() { viewController.errorRefresh = true; }
    }
}
