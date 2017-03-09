using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Graphing;


namespace UnityEditor.VFX
{
    abstract class VFXOperator : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        public VFXOperator()
        {
            var settingsType = GetPropertiesSettings();
            if (settingsType != null)
            {
                m_SettingsBuffer = System.Activator.CreateInstance(settingsType);
            }

            Invalidate(InvalidationCause.kParamChanged);
        }

        protected System.Type GetPropertiesSettings()
        {
            return GetType().GetNestedType("Settings");
        }

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

        virtual protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            return inputSlots.Select(o => o.expression).Where(e => e != null);
        }
    
        protected void SetOuputSlotFromExpression(IEnumerable<VFXExpression> inputExpression)
        {
            //Resize
            var inputExpressionArray = inputExpression.ToArray();
            if (inputExpressionArray.Length < outputSlots.Count())
            {
                while (outputSlots.Count() != inputExpressionArray.Length)
                {
                    RemoveSlot(outputSlots.Last(),false);
                }
            }
            else if (inputExpressionArray.Length > outputSlots.Count())
            {
                while (outputSlots.Count() != inputExpressionArray.Length)
                {
                    AddSlot(new VFXSlot(VFXSlot.Direction.kOutput),false);
                }
            }

            //Apply
            for (int iSlot = 0; iSlot < inputExpressionArray.Length; ++iSlot)
            {
                GetOutputSlot(iSlot).SetExpression(inputExpressionArray[iSlot]);
            }
        }

        protected abstract VFXExpression[] BuildExpression(VFXExpression[] inputExpression);

        virtual protected void OnOperatorInvalidate(VFXModel mode, InvalidationCause cause)
        {
            if (cause != InvalidationCause.kUIChanged)
            {
                var inputExpressions = GetInputExpressions();
                var ouputExpressions = BuildExpression(inputExpressions.ToArray());
                SetOuputSlotFromExpression(ouputExpressions);
            }
        }

        sealed override protected void OnInvalidate(VFXModel model,InvalidationCause cause)
        {
            OnOperatorInvalidate(model, cause);
            base.OnInvalidate(model, cause);
        }
    }
}
