using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockCameraFade : VFXBlockType
    {
        public VFXBlockCameraFade()
        {
            Name = "Camera Fade";
            Icon = "Alpha";
            Category = "Output";
            CompatibleContexts = VFXContextDesc.Type.kTypeOutput;

            Add(new VFXProperty(new VFXFloat2Type(new Vector2(0.0f,1.0f)),"FadeDistances"));

            Add(new VFXAttribute(CommonAttrib.Alpha, true));
            Add(new VFXAttribute(CommonAttrib.Position, false));

            Source = @"
float planeDist = VFXNearPlaneDist(position);
float camFade = smoothstep(FadeDistances.x,FadeDistances.y,planeDist);
alpha *= camFade;";
        }
    }
}
