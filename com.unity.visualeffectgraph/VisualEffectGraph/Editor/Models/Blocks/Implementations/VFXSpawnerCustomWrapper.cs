using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawner", autoRegister = false)]
    class VFXSpawnerCustomWrapper : VFXAbstractSpawner
    {
        [SerializeField]
        protected SerializableType m_customType;

        public void Init(Type customType)
        {
            m_customType = customType;
            var inputPropertiesType = customType.GetNestedType("InputProperties");
            var slots = GenerateSlotFromField(inputPropertiesType, VFXSlot.Direction.kInput);
            foreach (var slot in slots)
            {
                AddSlot(slot);
            }
        }

        public override sealed string name { get { return m_customType == null ? "" : ((Type)m_customType).Name; } }
        public override sealed Type customBehavior { get { return m_customType; } }
        public override sealed VFXSpawnerType spawnerType { get { return VFXSpawnerType.kCustomCallback; } }
    }
}
