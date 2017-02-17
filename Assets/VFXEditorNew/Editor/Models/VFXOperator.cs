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
        }

        public class VFXMitoSlotInput : VFXMitoSlot
        {
            public string name = null;

            public VFXOperator parent = null;
            public Guid parentSlotID = new Guid();
        }

        public class VFXMitoSlotOutput : VFXMitoSlot
        {
            public VFXExpression expression;
        }

        protected abstract ModeFlags Flags { get; }
        protected abstract VFXExpression[] BuildExpression(VFXExpression[] inputExpression);

        private bool cascadable { get { return (Flags & ModeFlags.kCascadable) != 0; } }

        [Flags]
        protected enum ModeFlags
        {
            None = 0,
            kBasicFloatOperation = 1 << 0,  //Automatically cast to biggest floatN format (sin(float3) => return float3)
            kCascadable = 1 << 1,           //allow implicit stacking (add, mul, substract, ...)

            kUnaryFloatOperator = kBasicFloatOperation,
            kBinaryFloatOperator = kBasicFloatOperation | kCascadable,
            kTernaryFloatOperator = kBasicFloatOperation,
        }

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
        }

        public VFXMitoSlotOutput[] OutputSlots
        {
            get
            {
                return m_OutputSlots;
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
            if (value is float)
            {
                return new VFXMitoSlotOutput()
                {
                    expression = new VFXValueFloat((float)value, true),
                    type = value.GetType()
                };
            }
            throw new NotImplementedException();
        }

        protected override void OnInvalidate(InvalidationCause cause)
        {
            base.OnInvalidate(cause);

            VFXExpression[] resExpression = null;
            if (cascadable)
            {
                var inputSlots = m_InputSlots.ToList();

                //Remove useless unplugged slot (ensuring there is at least 2 slots)
                var slotIndex = 0;
                while (inputSlots.Count > 2 && slotIndex < inputSlots.Count)
                {
                    var currentSlot = inputSlots[slotIndex];
                    if (currentSlot.parent == null)
                    {
                        inputSlots.RemoveAt(slotIndex);
                    }
                    else
                    {
                        ++slotIndex;
                    }
                }

                var lastElement = inputSlots.Last();
                if (lastElement.parent != null)
                {
                    //Add new available slot element
                    inputSlots.Add(new VFXMitoSlotInput()
                    {
                        name = m_PropertyBuffer.GetType().GetFields().First().Name,
                        type = lastElement.type
                    });
                }

                m_InputSlots = inputSlots.ToArray();
            }

            IEnumerable<VFXExpression> expressions = m_InputSlots.Select(o =>
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

            if (cascadable)
            {
                var stackInputExpression = new Stack<VFXExpression>(expressions.Reverse());
                while (stackInputExpression.Count > 1)
                {
                    var a = stackInputExpression.Pop();
                    var b = stackInputExpression.Pop();
                    var compose = BuildExpression(new[] { a, b })[0];
                    stackInputExpression.Push(compose);
                }
                resExpression = stackInputExpression.ToArray();
            }
            else
            {
                resExpression = BuildExpression(expressions.ToArray());
            }

            var outpoutSlots = resExpression.Select((o, i) => new VFXMitoSlotOutput()
            {
                slotID = i < m_OutputSlots.Length ? m_OutputSlots[i].slotID : Guid.NewGuid(),
                expression = o,
                type = VFXExpression.TypeToType(o.ValueType),
            });
            m_OutputSlots = outpoutSlots.ToArray();
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
