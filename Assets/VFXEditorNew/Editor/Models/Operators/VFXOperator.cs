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
                m_SettingsBuffer = new VFXSerializableObject(settingsType);
            }

            if (outputSlots.Count == 0)
                UpdateOutputs();
        }

        protected Type GetPropertiesSettings()
        {
            return GetType().GetNestedType("Settings");
        }

        [SerializeField]
        private VFXSerializableObject m_SettingsBuffer;

        public object settings
        {
            get
            {
                return m_SettingsBuffer == null ? null : m_SettingsBuffer.Get();
            }
            set
            {
                if (m_SettingsBuffer != null)
                {
                    m_SettingsBuffer.Set(value);
                    UpdateOutputs(); // TODOPAUL: (Julien) This should be handled in a more generic way: Handle settings change via virtual dispatch as behaviour depends on operator
                }
            }
        }

        virtual protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            return inputSlots.Select(o => o.GetExpression()).Where(e => e != null);
        }

        private static void CopyLink(VFXSlot from, VFXSlot to)
        {
            var linkedSlots = from.LinkedSlots.ToArray();
            for (int iLink = 0; iLink < linkedSlots.Length; ++iLink)
            {
                to.Link(linkedSlots[iLink]);
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

        virtual protected void OnInputConnectionsChanged()
        {
            UpdateOutputs();
        }

        sealed override protected void OnInvalidate(VFXModel model,InvalidationCause cause)
        {
            if (cause == InvalidationCause.kConnectionChanged) // Connection changed is only triggered for
                OnInputConnectionsChanged();
            base.OnInvalidate(model, cause);
        }

        public override void UpdateOutputs()
        {
            var inputExpressions = GetInputExpressions();
            var ouputExpressions = BuildExpression(inputExpressions.ToArray());
            SetOuputSlotFromExpression(ouputExpressions);
        }
    }
}
