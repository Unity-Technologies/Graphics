using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Graphing;


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
                        var slot = parent.OutputSlots.FirstOrDefault(s => s.slotID == parentSlotID);
                        return slot.expression; //shouldn't be null at this stage
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

            public bool CanConnect(VFXOperator _parent, Guid _slotID)
            {
                var slot = _parent.OutputSlots.FirstOrDefault(s => s.slotID == _slotID);
                if (slot != null)
                {
                    var fromType = VFXExpression.TypeToType(slot.expression.ValueType);
                    var toType = m_defaultValue.GetType();
                    if (fromType.IsAssignableFrom(toType))
                    {
                        return true;
                    }

                    if (toType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                        .Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == fromType)
                        .Any(mi =>
                        {
                            var pi = mi.GetParameters().FirstOrDefault();
                            return pi != null && pi.ParameterType == toType;
                        }))
                    {
                        return true;
                    }
                }
                return false;
            }

            public void Disconnect()
            {
                if (parent != null)
                {
                    var slotParent = parent.OutputSlots.FirstOrDefault(o => o.slotID == parentSlotID);
                    if (slotParent != null)
                    {
                        slotParent.RemoveChild(slotID);
                    }
                }

                parent = null;
                parentSlotID = Guid.Empty;
            }

            public void Connect(VFXOperator current, VFXOperator _parent, Guid _slotID)
            {
                if (CanConnect(_parent, _slotID))
                {
                    parent = _parent;
                    parentSlotID = _slotID;
                    var slotParent = parent.OutputSlots.First(o => o.slotID == parentSlotID);
                    slotParent.AddChild(current, slotID);
                }
            }
        }

        public class VFXMitoSlotOutput : VFXMitoSlot
        {
            private VFXExpression m_expression;

            public class MitoChildInfo
            {
                public VFXOperator model;
                public Guid slotID;
            }
            public List<MitoChildInfo> children = new List<MitoChildInfo>();

            public void AddChild(VFXOperator _child, Guid _slotID)
            {
                RemoveChild(_slotID);
                children.Add(new MitoChildInfo()
                {
                    model = _child,
                    slotID = _slotID
                });
            }

            public void RemoveChild(Guid _slotID)
            {
                var entry = children.FirstOrDefault(o => o.slotID == _slotID);
                if (entry != null)
                {
                    children.Remove(entry);
                }
            }

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
        /*end draft slot class waiting for real slot implementation*/

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

            var settingsType = GetPropertiesSettings();
            if (settingsType != null)
            {
                m_SettingsBuffer = System.Activator.CreateInstance(settingsType);
            }

            Invalidate(InvalidationCause.kParamChanged);
        }
        protected System.Type GetPropertiesType()
        {
            return GetType().GetNestedType("Properties");
        }

        protected System.Type GetPropertiesSettings()
        {
            return GetType().GetNestedType("Settings");
        }

        private VFXMitoSlotInput[] m_InputSlots = new VFXMitoSlotInput[] { };
        private VFXMitoSlotOutput[] m_OutputSlots = new VFXMitoSlotOutput[] { };
        private object m_PropertyBuffer;
        private object m_SettingsBuffer;

        [SerializeField]
        private SerializationHelper.JSONSerializedElement m_SerializableSettings;

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            if (settings != null)
            {
                m_SerializableSettings = SerializationHelper.Serialize(settings);
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (!m_SerializableSettings.Equals(SerializationHelper.nullElement))
            {
                settings = SerializationHelper.Deserialize<object>(m_SerializableSettings, null);
            }
            m_SerializableSettings = SerializationHelper.nullElement;
        }

        public object settings
        {
            get
            {
                return m_SettingsBuffer;
            }
            set
            {
                if (m_SettingsBuffer != value)
                {
                    m_SettingsBuffer = value;
                    Invalidate(InvalidationCause.kParamChanged);
                }
            }
        }
        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return false;
        }

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

        virtual protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            return m_InputSlots.Select(o => o.expression).Where(e => e != null);
        }
    
        protected IEnumerable<VFXMitoSlotOutput> BuildOuputSlot(IEnumerable<VFXExpression> inputExpression)
        {
            return inputExpression.Select((o, i) => new VFXMitoSlotOutput(o)
            {
                slotID = i < m_OutputSlots.Length ? m_OutputSlots[i].slotID : Guid.NewGuid(),
                children = i < m_OutputSlots.Length ? m_OutputSlots[i].children : new List<VFXMitoSlotOutput.MitoChildInfo>()
            });
        }

        virtual protected void OnOperatorInvalidate(VFXModel mode, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kParamChanged)
            {
                var inputExpressions = GetInputExpressions();
                var ouputExpressions = BuildExpression(inputExpressions.ToArray());
                OutputSlots = BuildOuputSlot(ouputExpressions).ToArray();
            }
        }

        sealed override protected void OnInvalidate(VFXModel model,InvalidationCause cause)
        {
            var allConnectedChildModel = OutputSlots.SelectMany(o => o.children.Select(c => c.model)).Distinct();
            if (cause == InvalidationCause.kParamChanged)
            {
                foreach (var slot in InputSlots)
                {
                    if (slot.parent != null && !slot.CanConnect(slot.parent, slot.parentSlotID))
                    {
                        slot.Disconnect();
                    }
                }
            }

            OnOperatorInvalidate(model, cause);
            base.OnInvalidate(model, cause);
            foreach (var slot in allConnectedChildModel)
            {
                slot.Invalidate(cause);
            }
        }
    }
}
