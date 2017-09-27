using System;
using UnityEngine;
using UnityEngine.VFX;

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
            InitSlotsFromProperties(inputProperties, VFXSlot.Direction.kInput);
        }

        public override sealed string name { get { return m_customType == null ? "" : ((Type)m_customType).Name; } }
        public override sealed Type customBehavior { get { return m_customType; } }
        public override sealed VFXTaskType spawnerType { get { return VFXTaskType.kSpawnerCustomCallback; } }
    }
}
