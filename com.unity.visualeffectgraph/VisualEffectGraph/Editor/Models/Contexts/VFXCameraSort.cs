using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.Block;

namespace UnityEditor.VFX
{
    class VFXCameraSort : VFXContext
    {
        public VFXCameraSort() : base(VFXContextType.kUpdate, VFXDataType.kParticle, VFXDataType.kParticle) {}
        public override string name { get { return "CameraSort"; } }
        public override string codeGeneratorTemplate { get { return "VFXShaders/VFXCameraSort"; } }
        public override bool codeGeneratorCompute { get { return true; } }
        public override VFXTaskType taskType { get { return VFXTaskType.Update; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
            }
        }
    }
}
