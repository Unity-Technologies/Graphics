using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class ProjectOnDepth : VFXBlock
    {
		public enum PositionMode
		{
			Random,
			Sequential,
			Custom,
		}
		
        public class InputProperties
        {
            public CameraType Camera;
            public float ZMultiplier = 0.99f;
            public Texture2D DepthBuffer;
        }

        public class SceneColorInputProperties
        {
            public Texture2D ColorBuffer;
        }

        public class SequentialInputProperties
        {
            public uint GridStep = 1;
        }
		
		public class CustomInputProperties
		{
			public Vector2 UVSpawn;
		}

		[VFXSetting]
		public PositionMode mode;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public bool cullOnFarPlane = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public bool inheritSceneColor = false;

        public override string name { get { return "Project On Depth"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInit; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
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
				
                if (cullOnFarPlane)
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);   
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var inputs = PropertiesFromType("InputProperties");
                if (inheritSceneColor)
                    inputs = inputs.Concat(PropertiesFromType("SceneColorInputProperties"));
                if (mode == PositionMode.Sequential)
                    inputs = inputs.Concat(PropertiesFromType("SequentialInputProperties"));
				else if (mode == PositionMode.Custom)
					inputs = inputs.Concat(PropertiesFromType("CustomInputProperties"));
                return inputs;
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var expressions = GetExpressionsFromSlots(this);
                foreach (var input in expressions)
                {
                    yield return input;
                }

                var fov = expressions.First(e => e.name == "Camera_fieldOfView");
                var aspect = expressions.First(e => e.name == "Camera_aspectRatio");
                var near = expressions.First(e => e.name == "Camera_nearPlane");
                var far = expressions.First(e => e.name == "Camera_farPlane");
                var cameraMatrix = expressions.First(e => e.name == "Camera_transform");

                var invPerspMat = new VFXExpressionInverseMatrix(VFXOperatorUtility.GetPerspectiveMatrix(fov.exp,aspect.exp,near.exp,far.exp));
                var clipToWorld = invPerspMat;// new VFXExpressionTransformMatrix(invPerspMat, cameraMatrix.exp);

                yield return new VFXNamedExpression(clipToWorld, "clipToWorld");

                if (((VFXDataParticle)GetData()).space == VFXCoordinateSpace.Local)
                    yield return new VFXNamedExpression(VFXBuiltInExpression.WorldToLocal, "worldToLocal");
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
uint2 ids = uvs * Camera_pixelDimensions;
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
float2 uvs = saturate(UVSpawn);
uint2 ids = uvs * Camera_pixelDimensions;
";
					break;
				}

                source += @"
float2 projpos = uvs * 2.0f - 1.0f;
				
float n = Camera_nearPlane;
float f = Camera_farPlane;

// TODO We should compute the clipToWorld directly in expressions
float4x4 camToWorld = transpose(Camera_transform);
				
float PlaneHalfHeight = tan(Camera_fieldOfView * 0.5f) * n;
float3 PlaneRight = camToWorld[0].xyz * PlaneHalfHeight * Camera_aspectRatio;
float3 PlaneUp = camToWorld[1].xyz * PlaneHalfHeight;

float3 camFront = camToWorld[2].xyz;
float3 camPos = camToWorld[3].xyz;
float3 PlanePos = camPos + camFront * n;

float depth = DepthBuffer.t[ids];

float linearEyeDepth = n * f / (depth * (f - n) + n) - n;

float3 worldPos = (PlaneRight * projpos.x) + (PlaneUp * projpos.y) + PlanePos;
float3 dir = normalize(worldPos - camPos);
position = worldPos + dir * ZMultiplier * linearEyeDepth / dot(dir,camFront);


float4 worldPos2 = mul(clipToWorld,float4(projpos,depth,1.0f));
position = worldPos2.xyz / worldPos2.w;





#if VFX_LOCAL_SPACE
//position = mul(worldToLocal,float4(position,1.0f)).xyz;
#endif
";

                if (cullOnFarPlane)
                    source += @"
// cull on far plane
if (linearEyeDepth >= (1.0f - VFX_EPSILON) * (f - n))
   alive = false;
";

                if (inheritSceneColor)
                    source += @"
color = ColorBuffer.t[ids].rgb;
";

                return source;
            }
        }

    }
}
