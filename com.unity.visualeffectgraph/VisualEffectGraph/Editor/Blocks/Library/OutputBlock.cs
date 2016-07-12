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

    class VFXBlockFaceCameraPlane : VFXBlockType
    {
        public VFXBlockFaceCameraPlane()
        {
            Name = "Face Camera Plane";
            Icon = "Position";
            Category = "Orientation";
            CompatibleContexts = VFXContextDesc.Type.kTypeOutput;

            Add(new VFXAttribute(CommonAttrib.Front, true));
            Add(new VFXAttribute(CommonAttrib.Side, true));
            Add(new VFXAttribute(CommonAttrib.Up, true));

            Source = @"
float4x4 cameraMat = VFXCameraMatrix();
front = -VFXCameraLook();
side = cameraMat[0].xyz;
up = cameraMat[1].xyz;";
        }
    }

    class VFXBlockFaceCameraPosition : VFXBlockType
    {
        public VFXBlockFaceCameraPosition()
        {
            Name = "Face Camera Position";
            Icon = "Position";
            Category = "Orientation";
            CompatibleContexts = VFXContextDesc.Type.kTypeOutput;

            Add(new VFXAttribute(CommonAttrib.Front, true));
            Add(new VFXAttribute(CommonAttrib.Side, true));
            Add(new VFXAttribute(CommonAttrib.Up, true));
            Add(new VFXAttribute(CommonAttrib.Position, false));

            Source = @"
front = normalize(VFXCameraPos() - position);
side = normalize(cross(front,VFXCameraMatrix()[1].xyz));
up = cross(side,front);";
        }
    }

    class VFXBlockLookAtPosition : VFXBlockType
    {
        public VFXBlockLookAtPosition()
        {
            Name = "Look At Position";
            Icon = "Position";
            Category = "Orientation";
            CompatibleContexts = VFXContextDesc.Type.kTypeOutput;

            Add(new VFXAttribute(CommonAttrib.Front, true));
            Add(new VFXAttribute(CommonAttrib.Side, true));
            Add(new VFXAttribute(CommonAttrib.Up, true));
            Add(new VFXAttribute(CommonAttrib.Position, false));

            Add(VFXProperty.Create<VFXPositionType>("Position"));

            Source = @"
front = normalize(Position - position);
side = normalize(cross(front,VFXCameraMatrix()[1].xyz));
up = cross(side,front);";
        }
    }

    class VFXBlockFixedOrientation : VFXBlockType
    {
        public VFXBlockFixedOrientation()
        {
            Name = "Fixed Orientation";
            Icon = "Position";
            Category = "Orientation";
            CompatibleContexts = VFXContextDesc.Type.kTypeOutput;

            Add(new VFXAttribute(CommonAttrib.Front, true));
            Add(new VFXAttribute(CommonAttrib.Side, true));
            Add(new VFXAttribute(CommonAttrib.Up, true));

            Add(new VFXProperty(new VFXDirectionType(new Vector3(0.0f,0.0f,1.0f)), "Front"));
            Add(new VFXProperty(new VFXDirectionType(new Vector3(0.0f,1.0f,0.0f)),"Up"));

            Source = @"
front = Front;
side = normalize(cross(front,Up));
up = cross(side,front);";
        }
    }

    class VFXBlockFixedAxis : VFXBlockType
    {
        public VFXBlockFixedAxis()
        {
            Name = "Axis Constrained Orientation";
            Icon = "Position";
            Category = "Orientation";
            CompatibleContexts = VFXContextDesc.Type.kTypeOutput;

            Add(new VFXAttribute(CommonAttrib.Front, true));
            Add(new VFXAttribute(CommonAttrib.Side, true));
            Add(new VFXAttribute(CommonAttrib.Up, true));
            Add(new VFXAttribute(CommonAttrib.Position, false));

            Add(VFXProperty.Create<VFXDirectionType>("Axis"));

            Source = @"
up = Axis;
front = VFXCameraPos() - position;
side = normalize(cross(front,up));
front = cross(up,side);";
        }
    }

    class VFXBlockOrientAlongVelocity : VFXBlockType
    {
        public VFXBlockOrientAlongVelocity()
        {
            Name = "Orient Along Velocity";
            Icon = "Position";
            Category = "Orientation";
            CompatibleContexts = VFXContextDesc.Type.kTypeOutput;

            Add(new VFXAttribute(CommonAttrib.Front, true));
            Add(new VFXAttribute(CommonAttrib.Side, true));
            Add(new VFXAttribute(CommonAttrib.Up, true));
            Add(new VFXAttribute(CommonAttrib.Velocity, false));
            Add(new VFXAttribute(CommonAttrib.Position, false));

            Source = @"
up = normalize(velocity);
front = VFXCameraPos() - position;
side = normalize(cross(front,up));
front = cross(up,side);";
        }
    }
}

