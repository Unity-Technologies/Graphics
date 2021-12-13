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

        bool reetrant = false;
        protected internal override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);

            //Our system cannot retrieve the actual space with subgraph blocks (yet)
            //Forcing all input slot in VFXCoordinateSpace.None for user consistency
            if (!spaceable && !reetrant)
            {
                var notUndefinedSpace = children.SelectMany(o => o.inputSlots).Where(o => o.space != VFXCoordinateSpace.None);
                if (notUndefinedSpace.Any())
                {
                    reetrant = true;
                    foreach (var slot in notUndefinedSpace)
                        slot.space = VFXCoordinateSpace.None;
                    reetrant = false;
                }
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
