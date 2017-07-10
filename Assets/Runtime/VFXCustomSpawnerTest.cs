using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    public class VFXCustomSpawnerTest : VFXSpawnerFunction
    {
        static public float s_SpawnCount = 101.0f;
        static public float s_LifeTime = 17.0f;

        public class InputProperties
        {
            public float totalTime = 8;
        }

        public override void OnStart(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
            state.spawnCount = s_SpawnCount;
            state.totalTime = vfxValues.GetFloat("totalTime");
            state.vfxEventAttribute.SetFloat("lifeTime", s_LifeTime);
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
        }
    }
}
