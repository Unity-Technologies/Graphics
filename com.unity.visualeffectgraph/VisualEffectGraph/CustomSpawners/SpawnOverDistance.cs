using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    public class SpawnOverDistance : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
            public Vector3 Position;
            public float RatePerUnit = 10.0f;
            public float VelocityThreshold = 50.0f;
        }

        private Vector3 m_OldPosition;

        static private readonly int positionPropertyId = Shader.PropertyToID("Position");
        static private readonly int ratePerUnitPropertyId = Shader.PropertyToID("RatePerUnit");
        static private readonly int velocityThresholdPropertyId = Shader.PropertyToID("VelocityThreshold");

        static private readonly int positionAttributeId = Shader.PropertyToID("position");
        static private readonly int oldPositionAttributeId = Shader.PropertyToID("oldPosition");

        public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            m_OldPosition = vfxValues.GetVector3(positionPropertyId);
        }

        public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            if (!state.playing || state.deltaTime == 0) return;

            Vector3 pos = vfxValues.GetVector3(positionPropertyId);
            float distance = Vector3.Distance(m_OldPosition, pos);
            if (distance < vfxValues.GetFloat(velocityThresholdPropertyId) * state.deltaTime)
            {
                state.spawnCount += distance * vfxValues.GetFloat(ratePerUnitPropertyId);

                state.vfxEventAttribute.SetVector3(oldPositionAttributeId, m_OldPosition);
                state.vfxEventAttribute.SetVector3(positionAttributeId, pos);
            }
            m_OldPosition = pos;
        }

        public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
