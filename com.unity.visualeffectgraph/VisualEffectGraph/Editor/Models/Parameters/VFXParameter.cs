using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    class VFXParameter : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXParameter()
        {
            m_exposedName = "exposedName";
            m_exposed = false;
            m_UICollapsed = false;
        }

        [VFXSetting, SerializeField]
        private string m_exposedName;
        [VFXSetting, SerializeField]
        private bool m_exposed;
        [VFXSetting, SerializeField]
        private int m_order;
        [VFXSetting, SerializeField]
        public VFXSerializableObject m_Min;
        [VFXSetting, SerializeField]
        public VFXSerializableObject m_Max;

        public string exposedName
        {
            get
            {
                return m_exposedName;
            }
        }

        public bool exposed
        {
            get
            {
                return m_exposed;
            }
        }

        public int order
        {
            get { return m_order; }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                yield return "m_order";
                yield return "m_Min";
                yield return "m_Max";
            }
        }

        public Type type
        {
            get { return outputSlots[0].property.type; }
        }

        public object value
        {
            get { return outputSlots[0].value; }
            set { outputSlots[0].value = value; }
        }

        protected sealed override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kSettingChanged)
            {
                Invalidate(InvalidationCause.kExpressionGraphChanged);
            }
            if (cause == InvalidationCause.kParamChanged)
            {
                if (m_ExprSlots != null)
                {
                    for (int i = 0; i < m_ExprSlots.Length; ++i)
                    {
                        m_ValueExpr[i].SetContent(m_ExprSlots[i].value);
                    }
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties { get { return PropertiesFromSlotsOrDefaultFromClass(VFXSlot.Direction.kOutput); } }

        public void Init(Type _type)
        {
            if (_type != null && outputSlots.Count == 0)
            {
                VFXSlot slot = VFXSlot.Create(new VFXProperty(_type, "o"), VFXSlot.Direction.kOutput);
                AddSlot(slot);

                if (!typeof(UnityEngine.Object).IsAssignableFrom(_type))
                    slot.value = System.Activator.CreateInstance(_type);
            }
            else
            {
                throw new InvalidOperationException("Cannot init VFXParameter");
            }
            m_ExprSlots = outputSlots[0].GetExpressionSlots().ToArray();
            m_ValueExpr = m_ExprSlots.Select(t => t.DefaultExpression()).ToArray();
        }

        public override void OnEnable()
        {
            base.OnEnable();

            Debug.Log("VFXParameter.OnEnable");
            if (outputSlots.Count != 0)
            {
                Debug.Log("VFXParameter.OnEnable with outputslot");
                m_ExprSlots = outputSlots[0].GetExpressionSlots().ToArray();
                m_ValueExpr = m_ExprSlots.Select(t => t.DefaultExpression()).ToArray();
            }
        }

        public override void UpdateOutputExpressions()
        {
            if (m_ExprSlots != null)
            {
                for (int i = 0; i < m_ExprSlots.Length; ++i)
                {
                    m_ValueExpr[i].SetContent(m_ExprSlots[i].value);
                    m_ExprSlots[i].SetExpression(m_ValueExpr[i]);
                }
            }
        }

        [NonSerialized]
        private VFXSlot[] m_ExprSlots;

        private VFXValue[] m_ValueExpr;
    }
}
