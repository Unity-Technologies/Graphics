using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    #pragma warning disable 0659
    sealed class VFXSpawnerStateExpression : VFXExpression
    {
        public static readonly VFXExpression NewLoop = new VFXSpawnerStateExpression(VFXExpressionOperation.SpawnerStateNewLoop);
        public static readonly VFXExpression LoopState = new VFXSpawnerStateExpression(VFXExpressionOperation.SpawnerStateLoopState);
        public static readonly VFXExpression SpawnCount = new VFXSpawnerStateExpression(VFXExpressionOperation.SpawnerStateSpawnCount);
        public static readonly VFXExpression DeltaTime = new VFXSpawnerStateExpression(VFXExpressionOperation.SpawnerStateDeltaTime);
        public static readonly VFXExpression TotalTime = new VFXSpawnerStateExpression(VFXExpressionOperation.SpawnerStateTotalTime);
        public static readonly VFXExpression DelayBeforeLoop = new VFXSpawnerStateExpression(VFXExpressionOperation.SpawnerStateDelayBeforeLoop);
        public static readonly VFXExpression LoopDuration = new VFXSpawnerStateExpression(VFXExpressionOperation.SpawnerStateLoopDuration);
        public static readonly VFXExpression DelayAfterLoop = new VFXSpawnerStateExpression(VFXExpressionOperation.SpawnerStateDelayAfterLoop);
        public static readonly VFXExpression LoopIndex = new VFXSpawnerStateExpression(VFXExpressionOperation.SpawnerStateLoopIndex);
        public static readonly VFXExpression LoopCount = new VFXSpawnerStateExpression(VFXExpressionOperation.SpawnerStateLoopCount);

        private static readonly VFXExpression[] AllExpressions = VFXReflectionHelper.CollectStaticReadOnlyExpression<VFXExpression>(typeof(VFXBuiltInExpression));
        public static readonly VFXExpressionOperation[] All = AllExpressions.Select(e => e.operation).ToArray();

        private VFXExpressionOperation m_Operation;
        private VFXSpawnerStateExpression(VFXExpressionOperation op)
            : base(Flags.InvalidOnGPU | Flags.PerSpawn)
        {
            m_Operation = op;
        }

        public sealed override VFXExpressionOperation operation => m_Operation;

        public override bool Equals(object obj)
        {
            if (!(obj is VFXSpawnerStateExpression))
                return false;

            var other = (VFXSpawnerStateExpression)obj;
            return operation == other.operation;
        }

        protected override int GetInnerHashCode()
        {
            return operation.GetHashCode();
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }
    }
    #pragma warning restore 0659
}
