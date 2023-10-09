using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;

namespace UnityEditor.VFX
{
    class VFXBlockSubgraphContext : VFXContext
    {
        public enum ContextType
        {
            Spawner = VFXContextType.Spawner,
            Init = VFXContextType.Init,
            Update = VFXContextType.Update,
            Output = VFXContextType.Output,

            InitAndUpdate = Init | Update,
            InitAndUpdateAndOutput = Init | Update | Output,
            UpdateAndOutput = Update | Output
        }

        public VFXBlockSubgraphContext() : base(VFXContextType.None, VFXDataType.None, VFXDataType.None)
        {
        }

        protected override int inputFlowCount { get { return 0; } }

        public sealed override string name { get { return "Block Subgraph"; } }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                yield break;
            }
        }

        [VFXSetting, SerializeField]
        ContextType m_SuitableContexts = ContextType.InitAndUpdateAndOutput;

        public VFXContextType compatibleContextType
        {
            get
            {
                return (VFXContextType)m_SuitableContexts;
            }
        }
        public override VFXDataType ownedType
        {
            get
            {
                return (m_SuitableContexts == ContextType.Spawner) ? VFXDataType.SpawnEvent : VFXDataType.Particle;
            }
        }
        public override bool spaceable
        {
            get
            {
                return false;
            }
        }

        protected internal override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);
            //Called from VFXSlot.InvalidateExpressionTree, can be triggered from a space change, need to refresh block warning
            if (cause == InvalidationCause.kExpressionInvalidated)
            {
                model.RefreshErrors();
            }
        }

        public override bool Accept(VFXBlock block, int index = -1)
        {
            return ((block.compatibleContexts & compatibleContextType) == compatibleContextType);
        }

        public override bool CanBeCompiled()
        {
            return false;
        }
    }
}
