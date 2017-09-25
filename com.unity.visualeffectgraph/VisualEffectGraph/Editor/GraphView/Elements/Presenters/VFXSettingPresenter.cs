using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    class VFXSettingPresenter : GraphElementPresenter, IPropertyRMProvider
    {
        [SerializeField]
        IVFXSlotContainer m_Owner;
        public IVFXSlotContainer owner { get { return m_Owner; } }

        System.Type m_SettingType;

        public System.Type anchorType { get { return m_SettingType; } }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, m_Owner as UnityEngine.Object };
        }

        public void Init(IVFXSlotContainer owner, string name, System.Type type)
        {
            m_Owner = owner;
            this.name = name;
            m_SettingType = type;
        }

        public object value
        {
            get
            {
                if (anchorType != null)
                {
                    return VFXConverter.ConvertTo(owner.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner), anchorType);
                }
                else
                {
                    return null;
                }
            }

            set
            {
                m_Owner.SetSettingValue(name, VFXConverter.ConvertTo(value, anchorType));
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

        public virtual string iconName
        {
            get { return anchorType.Name; }
        }

        public bool editable
        {
            get { return true; }
        }

        public VFXPropertyAttribute[] attributes
        {
            get
            {
                return VFXPropertyAttribute.Create(customAttributes);
            }
        }

        public object[] customAttributes
        {
            get
            {
                var customAttributes = owner.GetType().GetField(path, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetCustomAttributes(true);
                return customAttributes;
            }
        }

        public void ExpandPath()
        {
        }

        public void RetractPath()
        {
        }
    }
}
