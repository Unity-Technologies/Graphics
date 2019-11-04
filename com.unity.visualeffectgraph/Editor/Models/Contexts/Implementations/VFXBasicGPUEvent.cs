using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXType]
    struct GPUEvent
    {
        /* expected emptiness */
    };

    [VFXInfo(experimental = true)]
    class VFXBasicGPUEvent : VFXContext
    {
        public VFXBasicGPUEvent() : base(VFXContextType.SpawnerGPU, VFXDataType.None, VFXDataType.SpawnEvent) {}
        public override string name { get { return "GPUEvent"; } }

        public class InputProperties
        {
            public GPUEvent evt = new GPUEvent();
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            return new VFXExpressionMapper();
        }

        public override bool CanBeCompiled()
        {
            return outputContexts.Any(c => c.CanBeCompiled());
        }
    }
}
