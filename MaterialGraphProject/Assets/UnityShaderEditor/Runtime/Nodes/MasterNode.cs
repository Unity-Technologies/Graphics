using System;
using System.Collections.Generic;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class MasterNode : AbstractMaterialNode
    {
        protected override bool generateDefaultInputs { get { return false; } }

        public override IEnumerable<ISlot> GetInputsWithNoConnection()
        {
            return new List<ISlot>();
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override bool allowedInSubGraph
        {
            get { return false; }
        }

        public virtual bool has3DPreview()
        {
            return true;
        }
    }
}
