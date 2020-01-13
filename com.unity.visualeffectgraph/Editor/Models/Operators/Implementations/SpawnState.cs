using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Spawn")]
    class SpawnState : VFXOperator
    {
        public override string libraryName => "Spawn Context State";

        public override string name => "Spawn State";

        public class OutputProperties
        {
            [Tooltip("This boolean indicates if a new loop has just started.")]
            public bool NewLoop;

            [Tooltip("The current state of VFXSpawnerState.")]
            public uint LoopState;

            [Tooltip("The current index of loop.")]
            public int LoopIndex;

            [Tooltip("The current (frame relative) Spawn count.")]
            public float SpawnCount;

            [Tooltip("The current delta time.")]
            public float SpawnDeltaTime;

            [Tooltip("The accumulated delta time since the last Play event.")]
            public float SpawnTotalTime;

            [Tooltip("The duration of the looping state.")]
            public float LoopDuration;

            [Tooltip("The current loop count.")]
            public int LoopCount;

            [Tooltip("The current delay time that the VFXSpawner waits for after it finishes a loop.")]
            public float DelayBeforeLoop;

            [Tooltip("The current delay time that the VFXSpawner waits for after it finishes a loop.")]
            public float DelayAfterLoop;
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[]
            {
                VFXSpawnerStateExpression.NewLoop,
                VFXSpawnerStateExpression.LoopState,
                VFXSpawnerStateExpression.LoopIndex,
                VFXSpawnerStateExpression.SpawnCount,
                VFXSpawnerStateExpression.DeltaTime,
                VFXSpawnerStateExpression.TotalTime,
                VFXSpawnerStateExpression.LoopDuration,
                VFXSpawnerStateExpression.LoopCount,
                VFXSpawnerStateExpression.DelayBeforeLoop,
                VFXSpawnerStateExpression.DelayAfterLoop,
            };
        }
    }
}
