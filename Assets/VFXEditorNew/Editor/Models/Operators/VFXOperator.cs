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
            public abstract VFXExpression expression { get; set; }

            private Guid m_slotID = Guid.NewGuid();
            public Guid slotID { get { return m_slotID; } }
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
                set
                {
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
                parent = _parent;
                parentSlotID = _slotID;
                var slotParent = parent.OutputSlots.First(o => o.slotID == parentSlotID);
                slotParent.AddChild(current, slotID);
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

            public override VFXExpression expression
            {
                get
                {
                    return m_expression;
                }
                set
                {
                    m_expression = value;
                }
            }
        }
        /*end draft slot class waiting for real slot implementation*/

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
            else
            {
                m_SerializableSettings = SerializationHelper.nullElement;
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (!string.IsNullOrEmpty(m_SerializableSettings.JSONnodeData))
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
                    if (m_SettingsBuffer != null && value != null)
                    {
                        if (value.GetType() != m_SettingsBuffer.GetType())
                        {
                            throw new Exception(string.Format("Settings is assigned with invalid type, expected : {0} given : {1}", m_SettingsBuffer.GetType(), value.GetType()));
                        }
                    }
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
    
        protected void SetOuputSlotFromExpression(IEnumerable<VFXExpression> inputExpression)
        {
            //Resize
            var inputExpressionArray = inputExpression.ToArray();
            if (inputExpressionArray.Length < OutputSlots.Length)
            {
                m_OutputSlots = m_OutputSlots.Take(inputExpressionArray.Length).ToArray();
            }
            else if (inputExpressionArray.Length > OutputSlots.Length)
            {
                var slotList = m_OutputSlots.ToList();
                while (slotList.Count != inputExpressionArray.Length)
                {
                    slotList.Add(new VFXMitoSlotOutput());
                }
                m_OutputSlots = slotList.ToArray();
            }

            //Apply
            for (int iSlot = 0; iSlot < inputExpressionArray.Length; ++iSlot)
            {
                m_OutputSlots[iSlot].expression = inputExpressionArray[iSlot];
            }
        }

        public void ConnectInput(Guid slotID, VFXOperator parentOperator, Guid parentSlotID)
        {
            var slot = InputSlots.First(s => s.slotID == slotID);
            if (slot.CanConnect(parentOperator, parentSlotID))
            {
                slot.Connect(this, parentOperator, parentSlotID);
                Invalidate(InvalidationCause.kParamChanged);
            }
        }

        public void DisconnectInput(Guid slotID)
        {
            var slot = InputSlots.First(s => s.slotID == slotID);
            slot.Disconnect();
            Invalidate(InvalidationCause.kParamChanged);
        }

        public void DisconnectAllInputs()
        {
            if (InputSlots.Length > 0)
            {
                foreach (var slot in InputSlots)
                {
                    slot.Disconnect();
                }
                Invalidate(InvalidationCause.kParamChanged);
            }
        }

        protected abstract VFXExpression[] BuildExpression(VFXExpression[] inputExpression);

        virtual protected void OnOperatorInvalidate(VFXModel mode, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kParamChanged)
            {
                var inputExpressions = GetInputExpressions();
                var ouputExpressions = BuildExpression(inputExpressions.ToArray());
                SetOuputSlotFromExpression(ouputExpressions);
            }
        }

        sealed override protected void OnInvalidate(VFXModel model,InvalidationCause cause)
        {
            var allConnectedChildModel = OutputSlots.SelectMany(o => o.children.Select(c => c.model)).Distinct().ToArray();
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
