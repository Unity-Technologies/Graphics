using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXType(VFXTypeAttribute.Usage.ExcludeFromProperty)]
    struct GPUEvent
    {
        /* expected emptiness */
    };

    [VFXHelpURL("Context-GPUEvent")]
    [VFXInfo(name = "GPU Event", category = "#1Event")]
    class VFXBasicGPUEvent : VFXContext
    {
        public VFXBasicGPUEvent() : base(VFXContextType.SpawnerGPU, VFXDataType.None, VFXDataType.SpawnEvent) { }
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
