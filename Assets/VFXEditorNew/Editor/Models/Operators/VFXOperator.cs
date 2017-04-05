using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    abstract class VFXOperator : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXOperator()
        {
        }

        public override void OnEnable()
        {
            base.OnEnable();
            var settingsType = GetPropertiesSettings();
            if (settingsType != null && m_SettingsBuffer == null)
            {
                m_SettingsBuffer = System.Activator.CreateInstance(settingsType);
            }

            if (outputSlots.Count == 0)
            {
                Debug.Log("UPDATE OUTPUTS in OnEnable !!!!!!! ?!");
                UpdateOutputs();
            }
            //Invalidate(InvalidationCause.kParamChanged);
        }

        protected Type GetPropertiesSettings()
        {
            return GetType().GetNestedType("Settings");
        }

        private object m_SettingsBuffer;

        [SerializeField]
        private SerializationHelper.JSONSerializedElement m_SerializableSettings;

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            if (m_SettingsBuffer != null)
            {
                m_SerializableSettings = SerializationHelper.Serialize(m_SettingsBuffer);
            }
            else
            {
                m_SerializableSettings.Clear();
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (!m_SerializableSettings.Empty)
            {
                m_SettingsBuffer = SerializationHelper.Deserialize<object>(m_SerializableSettings, null);
            }
            m_SerializableSettings.Clear();
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
                    Invalidate(InvalidationCause.kStructureChanged);
                }
            }
        }

        virtual protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            return inputSlots.Select(o => o.GetExpression()).Where(e => e != null);
        }

        private static void CopyLink(VFXSlot from, VFXSlot to)
        {
            for (int iLink = 0; iLink < from.LinkedSlots.Count; ++iLink)
            {
                var slot = from.LinkedSlots[iLink];
                to.Link(slot);
            }

            var fromChild = from.children.ToArray();
            var toChild = to.children.ToArray();
            fromChild = fromChild.Take(toChild.Length).ToArray();
            toChild = toChild.Take(fromChild.Length).ToArray();
            for (int iChild = 0; iChild < toChild.Length; ++iChild)
            {
                CopyLink(fromChild[iChild], toChild[iChild]);
            }
        }

        private Queue<VFXExpression[]> outputExpressionQueue = new Queue<VFXExpression[]>();

        private void DequeueOutputSlotFromExpression()
        {
            var outputExpressionArray = outputExpressionQueue.First();
            Debug.Log("********************* " + +outputExpressionArray.Length + " " + outputSlots.Count);

            //Check change
            bool bOuputputLayoutChanged = false;
            if (outputExpressionArray.Length != outputSlots.Count())
            {
                bOuputputLayoutChanged = true;
            }
            else
            {
                for (int iSlot = 0; iSlot < outputExpressionArray.Length; ++iSlot)
                {
                    var slot = GetOutputSlot(iSlot);
                    var expression = outputExpressionArray[iSlot];
                    if (slot.property.type != VFXExpression.TypeToType(expression.ValueType))
                    {
                        bOuputputLayoutChanged = true;
                        break;
                    }
                }
            }

            if (bOuputputLayoutChanged)
            {
                var slotToRemove = outputSlots.ToArray();

                for (int iSlot = 0; iSlot < outputExpressionArray.Length; ++iSlot)
                {
                    var expression = outputExpressionArray[iSlot];
                    AddSlot(VFXSlot.Create(new VFXProperty(VFXExpression.TypeToType(expression.ValueType), "o"), VFXSlot.Direction.kOutput));
                    if (iSlot < slotToRemove.Length)
                    {
                        CopyLink(slotToRemove[iSlot], outputSlots.Last());
                    }
                }

                foreach (var slot in slotToRemove)
                {
                    RemoveSlot(slot);
                    slot.UnlinkAll(false);
                }
            }

            //Apply
            for (int iSlot = 0; iSlot < outputExpressionArray.Length; ++iSlot)
            {
                GetOutputSlot(iSlot).SetExpression(outputExpressionArray[iSlot]);
            }

            outputExpressionQueue.Dequeue();
        }

        protected void SetOuputSlotFromExpression(IEnumerable<VFXExpression> outputExpression)
        {
            var outputExpressionArray = outputExpression.ToArray();
            outputExpressionQueue.Enqueue(outputExpressionArray);
            
            if (outputExpressionQueue.Count > 1)
                return;
            
            // Dequeue
            while (outputExpressionQueue.Count > 0)
                DequeueOutputSlotFromExpression();
        }

        protected abstract VFXExpression[] BuildExpression(VFXExpression[] inputExpression);

        virtual protected void OnOperatorInvalidate(VFXModel mode, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kConnectionChanged)
                UpdateOutputs();
        }

        sealed override protected void OnInvalidate(VFXModel model,InvalidationCause cause)
        {
            OnOperatorInvalidate(model, cause);
            base.OnInvalidate(model, cause);
        }

        public override void UpdateOutputs()
        {
            Debug.Log("------------------------------- UPDATE OUTPUTS FOR " + GetType().Name + "\n" +System.Environment.StackTrace.ToString());
            var inputExpressions = GetInputExpressions();
            var ouputExpressions = BuildExpression(inputExpressions.ToArray());
            SetOuputSlotFromExpression(ouputExpressions);
        }
    }
}
