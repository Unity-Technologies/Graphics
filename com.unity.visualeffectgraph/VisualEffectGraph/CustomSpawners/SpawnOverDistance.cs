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

        private Vector3 oldPosition;

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            oldPosition = vfxValues.GetVector3("Position");
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            Vector3 pos = vfxValues.GetVector3("Position");
            float distance = Vector3.Distance(oldPosition, pos);
            if (distance < vfxValues.GetFloat("VelocityThreshold") * state.deltaTime)
            {
                state.spawnCount += distance * vfxValues.GetFloat("RatePerUnit");

                state.vfxEventAttribute.SetVector3("oldPosition", oldPosition);
                state.vfxEventAttribute.SetVector3("position", pos);
            }
            oldPosition = vfxValues.GetVector3("Position");
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
