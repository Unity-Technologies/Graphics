using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    abstract class BuiltInSurfaceSubTarget : BuiltInSubTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("4393f232312587b4dab35c21ea5bb5d9"); // BuiltInSurfaceSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);
        }
    }
}
