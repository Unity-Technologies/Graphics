using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    abstract class UniversalSurfaceSubTarget : UniversalSubTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("0cf019105201bc541bfc8a3ea9cceb2c"); // UniversalSurfaceSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);
        }
    }
}
