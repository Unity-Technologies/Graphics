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
            public abstract VFXExpression expression { get; }

            public Guid slotID = Guid.NewGuid();
        }

        public class VFXMitoSlotInput : VFXMitoSlot
        {
            public VFXOperator parent { get; private set; }
            public Guid parentSlotID { get; private set; }

            private object m_defaultValue;
            public object defaultValue { get { return m_defaultValue; } }

            public VFXMitoSlotInput(object defaultValue)
            {
                m_defaultValue = defaultValue;
            }

            public override VFXExpression expression
            {
                get
                {
                    if (parent != null)
                    {
                        return parent.OutputSlots.First(s => s.slotID == parentSlotID).expression;
                    }
                    if (m_defaultValue is float)
                    {
                        return new VFXValueFloat((float)m_defaultValue, true);
                    }
                    else if (m_defaultValue is Vector2)
                    {
                        return new VFXValueFloat2((Vector2)m_defaultValue, true);
                    }
                    else if (m_defaultValue is Vector3)
                    {
                        return new VFXValueFloat3((Vector3)m_defaultValue, true);
                    }
                    else if (m_defaultValue is Vector4)
                    {
                        return new VFXValueFloat4((Vector4)m_defaultValue, true);
                    }
                    else if (m_defaultValue is FloatN)
                    {
                        return (FloatN)m_defaultValue;
                    }
                    else if (m_defaultValue is AnimationCurve)
                    {
                        return new VFXValueCurve(m_defaultValue as AnimationCurve, true);
                    }

                    throw new NotImplementedException();
                }
            }

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
            private VFXExpression m_expression;

            public VFXMitoSlotOutput(VFXExpression expression)
            {
                m_expression = expression;
            }

            public override VFXExpression expression
            {
                get
                {
                    return m_expression;
                }
            }
        }

        protected abstract VFXExpression[] BuildExpression(VFXExpression[] inputExpression);

        public VFXOperator()
        {
            var propertyType = GetPropertiesType();
            if (propertyType != null)
            {
                m_PropertyBuffer = System.Activator.CreateInstance(propertyType);
                m_InputSlots = propertyType.GetFields().Where(o => !o.IsStatic).Select(o =>
                {
                    var value = o.GetValue(m_PropertyBuffer);
                    return new VFXMitoSlotInput(value);
                }).ToArray();
            }
            Invalidate(InvalidationCause.kParamChanged);
        }
        protected System.Type GetPropertiesType()
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

        virtual protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            return m_InputSlots.Select(o => o.expression).Where(e => e != null);
        }
    
        protected IEnumerable<VFXMitoSlotOutput> BuildOuputSlot(IEnumerable<VFXExpression> inputExpression)
        {
            return inputExpression.Select((o, i) => new VFXMitoSlotOutput(o)
            {
                slotID = i < m_OutputSlots.Length ? m_OutputSlots[i].slotID : Guid.NewGuid(),
            });
        }

        protected override void OnInvalidate(VFXModel model,InvalidationCause cause)
        {
            base.OnInvalidate(model,cause);
            if (cause == InvalidationCause.kParamChanged)
            {
                var inputExpressions = GetInputExpressions();
                var ouputExpressions = BuildExpression(inputExpressions.ToArray());
                OutputSlots = BuildOuputSlot(ouputExpressions).ToArray();
            }
        }
    }
}
