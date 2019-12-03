using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Spawn")]
    class SpawnerState : VFXOperator
    {
        public override string libraryName => "Spawn Context State";

        public override string name => "Spaw Context State";

        public class OutputProperties
        {
            [Tooltip("This boolean indicates if a new loop has just started.")]
            public bool NewLoop;

            [Tooltip("The current state of VFXSpawnerState.")]
            public uint LoopState;

            [Tooltip("The current Spawn count.")]
            public float SpawnCount;

            [Tooltip("The current delta time.")]
            public float SpawnDeltaTime;

            [Tooltip("The accumulated delta time since the last Play event.")]
            public float SpawnTotalTime;

            [Tooltip("The current delay time that the VFXSpawner waits for after it finishes a loop.")]
            public float DelayBeforeLoop;

            [Tooltip("The duration of the looping state.")]
            public float LoopDuration;

            [Tooltip("The current delay time that the VFXSpawner waits for after it finishes a loop.")]
            public float DelayAfterLoop;

            [Tooltip("The current index of loop.")]
            public int LoopIndex;

            [Tooltip("The current loop count.")]
            public int LoopCount;
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[]
            {
                VFXSpawnerStateExpression.NewLoop,
                VFXSpawnerStateExpression.LoopState,
                VFXSpawnerStateExpression.SpawnCount,
                VFXSpawnerStateExpression.DeltaTime,
                VFXSpawnerStateExpression.TotalTime,
                VFXSpawnerStateExpression.DelayBeforeLoop,
                VFXSpawnerStateExpression.LoopDuration,
                VFXSpawnerStateExpression.DelayAfterLoop,
                VFXSpawnerStateExpression.LoopIndex,
                VFXSpawnerStateExpression.LoopCount
            };
        }
    }
}
