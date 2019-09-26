using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Velocity", experimental = true)]
    class VelocitySpeed : VelocityBase
    {
        public override string name { get { return string.Format(base.name, "Change Speed"); } }
        protected override bool altersDirection { get { return false; } }

        public override string source
        {
            get
            {
                string outSource = speedComputeString + "\n";
                outSource += string.Format(velocityComposeFormatString, "direction * speed");
                return outSource;
            }
        }
    }
}
