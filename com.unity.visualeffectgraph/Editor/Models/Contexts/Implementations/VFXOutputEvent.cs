using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOutputEvent : VFXContext
    {
        public VFXOutputEvent() : base(VFXContextType.OutputEvent, VFXDataType.SpawnEvent, VFXDataType.OutputEvent)
        {
        }

        protected override int outputFlowCount => 0;

        public override string name => "Output Event";

        public override bool CanBeCompiled()
        {
            var anyInputContextPlugged = inputContexts.Any();
            return anyInputContextPlugged;
        }
    }
}
