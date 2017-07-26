using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXAttributeParameter : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        [SerializeField]
        private string m_attributeName;

        public string attributeName
        {
            get
            {
                return m_attributeName;
            }
        }

        public VFXAttributeParameter()
        {
        }

        public override void OnEnable()
        {
            base.OnEnable();
            if (!string.IsNullOrEmpty(m_attributeName))
            {
                Init(m_attributeName);
            }
        }

        public void Init(string attributeName)
        {
            m_attributeName = attributeName;
            var attribute = VFXAttribute.Find(m_attributeName);
            if (outputSlots.Count == 0)
            {
                AddSlot(VFXSlot.Create(new VFXProperty(VFXExpression.TypeToType(attribute.type), "o"), VFXSlot.Direction.kOutput));
            }
            var expression = new VFXAttributeExpression(attribute);
            outputSlots[0].SetExpression(expression);
        }
    }
}
