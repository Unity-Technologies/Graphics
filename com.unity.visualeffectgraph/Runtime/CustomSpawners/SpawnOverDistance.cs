using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEngine.VFX
{
    class SpawnOverDistance : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
            public Vector3 Position = Vector3.zero;
            public float RatePerUnit = 10.0f;
            public float VelocityThreshold = 50.0f;
            public bool ClampToOne = false;
        }

        private Vector3 m_OldPosition;

        static private readonly int positionPropertyId = Shader.PropertyToID("Position");
        static private readonly int ratePerUnitPropertyId = Shader.PropertyToID("RatePerUnit");
        static private readonly int velocityThresholdPropertyId = Shader.PropertyToID("VelocityThreshold");
        static private readonly int clampToOnePropertyId = Shader.PropertyToID("ClampToOne");

        static private readonly int positionAttributeId = Shader.PropertyToID("position");
        static private readonly int oldPositionAttributeId = Shader.PropertyToID("oldPosition");

        public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            m_OldPosition = vfxValues.GetVector3(positionPropertyId);
        }

        public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            if (!state.playing || state.deltaTime == 0) return;

            float threshold = vfxValues.GetFloat(velocityThresholdPropertyId);

            Vector3 pos = vfxValues.GetVector3(positionPropertyId);
            float dist = Vector3.Magnitude(m_OldPosition - pos);
            if (threshold <= 0.0f || dist < threshold * state.deltaTime)
            {
                float count = dist * vfxValues.GetFloat(ratePerUnitPropertyId);
                if (vfxValues.GetBool(clampToOnePropertyId))
                    count = Mathf.Min(count, 1.0f);
                state.spawnCount += count;

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
