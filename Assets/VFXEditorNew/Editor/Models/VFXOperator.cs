using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEngine;

using Type = System.Type;

namespace UnityEditor.VFX
{
    abstract class VFXOperator : VFXModel
    {
        /*draft slot class waiting for real slot implementation*/
        public abstract class VFXMitoSlot
        {
            public Type type;
            public Guid slotID = Guid.NewGuid();

            public string name = "";
        }

        public class VFXMitoSlotInput : VFXMitoSlot
        {
            public VFXOperator parent { get; private set; }
            public Guid parentSlotID { get; private set; }

            public void Disconnect()
            {
                parent = null;
                parentSlotID = Guid.Empty;
            }

            public void Connect(VFXOperator _parent, Guid _slotID)
            {
                parent = _parent;
                parentSlotID = _slotID;
            }
        }

        public class VFXMitoSlotOutput : VFXMitoSlot
        {
            public VFXExpression expression;
        }

        protected abstract VFXExpression[] BuildExpression(VFXExpression[] inputExpression);

        public VFXOperator()
        {
            System.Type propertyType = GetPropertiesType();
            if (propertyType != null)
            {
                m_PropertyBuffer = System.Activator.CreateInstance(propertyType);
                m_InputSlots = propertyType.GetFields().Select(o =>
                {
                    return new VFXMitoSlotInput()
                    {
                        type = o.FieldType,
                        name = o.Name,
                    };
                }).ToArray();
            }            
            OnInvalidate(InvalidationCause.kParamChanged);
        }
        private System.Type GetPropertiesType()
        {
            return GetType().GetNestedType("Properties");
        }

        private VFXMitoSlotInput[] m_InputSlots = new VFXMitoSlotInput[] { };
        private VFXMitoSlotOutput[] m_OutputSlots = new VFXMitoSlotOutput[] { };
        private object m_PropertyBuffer;

        public VFXMitoSlotInput[] InputSlots
        {
            get
            {
                return m_InputSlots;
            }

            protected set
            {
                m_InputSlots = value;
            }
        }

        public VFXMitoSlotOutput[] OutputSlots
        {
            get
            {
                return m_OutputSlots;
            }

            protected set
            {
                m_OutputSlots = value;
            }

        }

        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return false;
        }

        static private VFXMitoSlotOutput GetExpression(object defaultProperties, string name)
        {
            var fieldInfo = defaultProperties.GetType().GetField(name);
            var value = fieldInfo.GetValue(defaultProperties);

            VFXExpression expression = null;
            if (value is float)
            {
                expression = new VFXValueFloat((float)value, true);

            }
            else if(value is AnimationCurve)
            {
                expression = new VFXValueCurve(value as AnimationCurve, true);
            }

            if (expression == null)
                throw new NotImplementedException();

            return new VFXMitoSlotOutput()
            {
                expression = expression,
                type = value.GetType()
            };
        }

        protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            return m_InputSlots.Select(o =>
            {
                VFXExpression expression = new VFXValueFloat(0, true);
                if (o.parent != null)
                {
                    expression = o.parent.OutputSlots.First(s => s.slotID == o.parentSlotID).expression;
                }
                else if (o.name != null)
                {
                    expression = GetExpression(m_PropertyBuffer, o.name).expression;
                }
                return expression;
            });
        }
    
        protected IEnumerable<VFXMitoSlotOutput> BuildOuputSlot(IEnumerable<VFXExpression> inputExpression)
        {
            return inputExpression.Select((o, i) => new VFXMitoSlotOutput()
            {
                slotID = i < m_OutputSlots.Length ? m_OutputSlots[i].slotID : Guid.NewGuid(),
                expression = o,
                type = VFXExpression.TypeToType(o.ValueType),
            });
        }

        protected override void OnInvalidate(InvalidationCause cause)
        {
            base.OnInvalidate(cause);

            IEnumerable<VFXExpression> inputExpressions = GetInputExpressions();
            var ouputExpressions = BuildExpression(inputExpressions.ToArray());
            OutputSlots = BuildOuputSlot(ouputExpressions).ToArray();
        }

        public Vector2 Position
        {
            get { return m_UIPosition; }
            set { m_UIPosition = value; }
        }

        [SerializeField]
        private Vector2 m_UIPosition;
    }
}
