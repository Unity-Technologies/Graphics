using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    //[VFXInfo(category = "Implicit")] //Only used in implicit block
    class BackupOldPosition : VFXBlock
    {
        public override string name { get { return "Backup Old Position"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.Update; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.OldPosition, VFXAttributeMode.Write);
            }
        }

        public override string source
        {
            get
            {
                return "oldPosition = position;";
            }
        }
    }
}
