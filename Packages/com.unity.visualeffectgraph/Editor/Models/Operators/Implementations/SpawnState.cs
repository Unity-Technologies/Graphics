using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-SpawnState")]
    [VFXInfo(name = "Spawn Context State", category = "Spawn")]
    class SpawnState : VFXOperator
    {
        public override string name => "Spawn State";

        public class OutputProperties
        {
            [Tooltip("Outputs ‘true’ if a new loop has just started. Otherwise, outputs ‘false’.")]
            public bool NewLoop;

            [Tooltip("Outputs the current loop state. This can be ‘0’ when not looping, ‘1’ when delaying before a loop, ‘2’ when looping, or ‘3’ when delaying after a loop.")]
            public uint LoopState;

            [Tooltip("Outputs the current index of the loop. This number is incremented for each new loop.")]
            public int LoopIndex;

            [Tooltip("Outputs the number of particles spawned in the current frame.")]
            public float SpawnCount;

            [Tooltip("Outputs the current delta time. This value can be modified by a custom spawner.")]
            public float SpawnDeltaTime;

            [Tooltip("Outputs the accumulated time in seconds since the last Play event.")]
            public float SpawnTotalTime;

            [Tooltip("Outputs the loop duration specified in the spawn context.")]
            public float LoopDuration;

            [Tooltip("Outputs the loop count specified in the spawn context.")]
            public int LoopCount;

            [Tooltip("Outputs the delay time the VFXSpawner waits for before starting a new loop. This value is specified in the spawn context.")]
            public float DelayBeforeLoop;

            [Tooltip("Outputs the delay time the VFXSpawner waits for after it finishes a loop. This value is specified in the spawn context.")]
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
