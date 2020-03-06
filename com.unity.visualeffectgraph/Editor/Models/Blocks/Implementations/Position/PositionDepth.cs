using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionDepth : VFXBlock
    {
		public enum PositionMode
		{
			Random,
			Sequential,
			Custom,
		}

        public enum CullMode
        {
            None,
            FarPlane,
            Range,
        }
		
        public class InputProperties
        {   [Tooltip("Sets a scale multiplier to the depth value. Values above 1 push particles further back, values lower than 1 pull them closer to the camera.")]
            public float ZMultiplier = 1.0f;
        }

        public class SequentialInputProperties
        {
            [Tooltip("Sets the space between sequentially-placed particles. Lower numbers produce a denser placement.")]
            public uint GridStep = 1;
        }
		
		public class CustomInputProperties
		{
            [Range(0.0f, 1.0f), Tooltip("Sets the UV coordinates with which to sample the depth buffer.")]
            public Vector2 UVSpawn;
		}

        public class RangeInputProperties
        {
            [Range(0.0f,1.0f), Tooltip("Sets the depth range within which to spawn particles. Particles outside of this range are culled.")]
            public Vector2 DepthRange = new Vector2(0.0f,1.0f);
        }

        [VFXSetting, Tooltip("Specifies which Camera to use to project particles onto its depth. Can use the camera tagged 'Main', or a custom camera.")]
        public CameraMode camera;

        [VFXSetting, Tooltip("Specifies how particles are positioned on the screen. They can be placed sequentially in an even grid, randomly, or with a custom UV position.")]
		public PositionMode mode;

        [VFXSetting, Tooltip("Specifies how to determine whether the particle should be alive. A particle can be culled when it is projected on the far camera plane, between a specific range, or culling can be disabled.")]
        public CullMode cullMode;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, particles inherit the color from the color buffer.")]
        public bool inheritSceneColor = false;

        public override string name { get { return "Position (Depth)"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.Init; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Write);

                if (inheritSceneColor)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Write);
				
                if (mode == PositionMode.Sequential)
                    yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
                else if (mode == PositionMode.Random)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);

                if (cullMode != CullMode.None)
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);   
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var inputs = Enumerable.Empty<VFXPropertyWithValue>();
                if (camera == CameraMode.Custom)
                    inputs = inputs.Concat(PropertiesFromType(typeof(CameraHelper.CameraProperties)));
                inputs = inputs.Concat(PropertiesFromType("InputProperties"));
                if (mode == PositionMode.Sequential)
                    inputs = inputs.Concat(PropertiesFromType("SequentialInputProperties"));
				else if (mode == PositionMode.Custom)
					inputs = inputs.Concat(PropertiesFromType("CustomInputProperties"));
                if (cullMode == CullMode.Range)
                    inputs = inputs.Concat(PropertiesFromType("RangeInputProperties"));
                return inputs;
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var expressions = CameraHelper.AddCameraExpressions(GetExpressionsFromSlots(this),camera);

                CameraMatricesExpressions camMat = CameraHelper.GetMatricesExpressions(expressions, ((VFXDataParticle)GetData()).space);

                // Filter unused expressions
                expressions = expressions.Where(t =>
                    t.name != "Camera_fieldOfView" &&
                    t.name != "Camera_aspectRatio" &&
                    t.name != "Camera_nearPlane" &&
                    t.name != "Camera_farPlane" &&
                    t.name != "Camera_transform" &&
                    (inheritSceneColor || t.name != "Camera_colorBuffer"));

                foreach (var input in expressions)
                    yield return input;

                var clipToVFX = new VFXExpressionTransformMatrix(camMat.ViewToVFX.exp, camMat.ClipToView.exp);
                yield return new VFXNamedExpression(clipToVFX, "ClipToVFX");
            }
        }

        public override string source
        {
            get
            {
                string source = "";              

				switch(mode)
				{
					case PositionMode.Random:
						source += @"
float2 uvs = RAND2;
";
					break;
					
					case PositionMode.Sequential:
						source += @"
// Pixel perfect spawn
uint2 sSize = Camera_pixelDimensions / GridStep;
uint nbPixels = sSize.x * sSize.y;
uint id = particleId % nbPixels;
uint2 ids = uint2(id % sSize.x,id / sSize.x) * GridStep + (GridStep >> 1);
float2 uvs = (ids + 0.5f) / Camera_pixelDimensions;
";
					break;
					
					case PositionMode.Custom:
						source += @"
float2 uvs = UVSpawn;
";
					break;
				}

                source += @"
float2 projpos = uvs * 2.0f - 1.0f;
				
float depth = LoadTexture(Camera_depthBuffer,int4(uvs*Camera_pixelDimensions, 0, 0)).r;
#if UNITY_REVERSED_Z
depth = 1.0f - depth; // reversed z
#endif";

                if (cullMode == CullMode.FarPlane)
                    source += @"
// cull on far plane
if (depth >= 1.0f - VFX_EPSILON)
{
    alive = false;
    return;
}
                ";

                if (cullMode == CullMode.Range)
                    source += @"
// filter based on depth
if (depth < DepthRange.x || depth > DepthRange.y)
{
    alive = false;
    return;
}
";
            source += @"
float4 clipPos = float4(projpos,depth * ZMultiplier * 2.0f - 1.0f,1.0f);
float4 vfxPos = mul(ClipToVFX,clipPos);
position = vfxPos.xyz / vfxPos.w;
";

                if (inheritSceneColor)
                    source += @"
color = LoadTexture(Camera_colorBuffer,int4(uvs*Camera_pixelDimensions, 0, 0)).rgb;
";

                return source;
            }
        }

    }
}
