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
            while (outputSlots.Count > 0)
            {
                RemoveSlot(outputSlots[0]);
            }
            m_attributeName = attributeName;
            var exp = VFXAttributeExpression.Find(m_attributeName);
            AddSlot(VFXSlot.Create(new VFXProperty(VFXExpression.TypeToType(exp.ValueType), "o"), VFXSlot.Direction.kOutput));
            outputSlots[0].SetExpression(exp);
        }
    }
}