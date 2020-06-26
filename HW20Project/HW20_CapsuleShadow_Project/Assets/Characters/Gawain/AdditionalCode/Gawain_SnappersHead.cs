using UnityEngine;
using Unity.DemoTeam.DigitalHuman;
using Unity.Collections.LowLevel.Unsafe;// for UnsafeUtilityEx.AsRef<T>

//using static Unity.DemoTeam.DigitalHuman.SnappersHeadDefinitionMath;

namespace Unity.DemoTeam.DigitalHuman.Sample
{
//	using SnappersControllers = Gawain_SnappersControllers<SnappersController>;
//	using SnappersBlendShapes = Gawain_SnappersBlendShapes<float>;

//	public class Gawain_SnappersHead : SnappersHeadDefinition
//	{
//		public override InstanceData CreateInstanceData(Mesh sourceMesh, Transform sourceRig, Warnings warnings)
//		{
//			return CreateInstanceData<SnappersControllers, SnappersBlendShapes>(sourceMesh, sourceRig, warnings);
//		}

//#pragma warning disable 0219
//		// --- ResolveControllers
//		public override unsafe void ResolveControllers(void* ptrSnappersControllers, void* ptrSnappersBlendShapes, void* ptrSnappersShaderParam)
//		{
//			ResolveControllers(
//				ref UnsafeUtilityEx.AsRef<SnappersControllers>(ptrSnappersControllers),
//				ref UnsafeUtilityEx.AsRef<SnappersBlendShapes>(ptrSnappersBlendShapes),
//				ref UnsafeUtilityEx.AsRef<SnappersShaderParam>(ptrSnappersShaderParam)
//			);
//		}
//		public void ResolveControllers(ref SnappersControllers Head_controller, ref SnappersBlendShapes Head_blendShape, ref SnappersShaderParam SkinShader)
//		{
//			// this segment generated from 'Controllers_Limits'
//			{
//				Head_controller.Neck_cntr.translateX  = clamp(Head_controller.Neck_cntr.translateX, 0.0f, 2.5f);
//				Head_controller.Neck_cntr.translateY  = clamp(Head_controller.Neck_cntr.translateY, -2.5f, 0.0f);
//				Head_controller.Neck_cntr.translateZ  = clamp(Head_controller.Neck_cntr.translateZ, -2.5f, 2.5f);
//				Head_controller.Head_cntr.translateX  = clamp(Head_controller.Head_cntr.translateX, 0.0f, 0.0f);
//				Head_controller.Head_cntr.translateY  = clamp(Head_controller.Head_cntr.translateY, 0.0f, 2.5f);
//				Head_controller.Head_cntr.translateZ  = clamp(Head_controller.Head_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.Brow_cntr.translateX  = clamp(Head_controller.Brow_cntr.translateX, -2.5f, 0.0f);
//				Head_controller.Brow_cntr.translateY  = clamp(Head_controller.Brow_cntr.translateY, 0.0f, 2.5f);
//				Head_controller.Brow_cntr.translateZ  = clamp(Head_controller.Brow_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.BrowOut_R_cntr.translateX  = clamp(Head_controller.BrowOut_R_cntr.translateX, 0.0f, 0.0f);
//				Head_controller.BrowOut_R_cntr.translateY  = clamp(Head_controller.BrowOut_R_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.BrowOut_R_cntr.translateZ  = clamp(Head_controller.BrowOut_R_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.BrowIn_R_cntr.translateX  = clamp(Head_controller.BrowIn_R_cntr.translateX, -2.5f, 0.0f);
//				Head_controller.BrowIn_R_cntr.translateY  = clamp(Head_controller.BrowIn_R_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.BrowIn_R_cntr.translateZ  = clamp(Head_controller.BrowIn_R_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.BrowIn_L_cntr.translateX  = clamp(Head_controller.BrowIn_L_cntr.translateX, -2.5f, 0.0f);
//				Head_controller.BrowIn_L_cntr.translateY  = clamp(Head_controller.BrowIn_L_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.BrowIn_L_cntr.translateZ  = clamp(Head_controller.BrowIn_L_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.BrowOut_L_cntr.translateX  = clamp(Head_controller.BrowOut_L_cntr.translateX, 0.0f, 0.0f);
//				Head_controller.BrowOut_L_cntr.translateY  = clamp(Head_controller.BrowOut_L_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.BrowOut_L_cntr.translateZ  = clamp(Head_controller.BrowOut_L_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.EyeSqz_R_cntr.translateX  = clamp(Head_controller.EyeSqz_R_cntr.translateX, -2.5f, 0.0f);
//				Head_controller.EyeSqz_R_cntr.translateY  = clamp(Head_controller.EyeSqz_R_cntr.translateY, -2.5f, 0.0f);
//				Head_controller.EyeSqz_R_cntr.translateZ  = clamp(Head_controller.EyeSqz_R_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.EyeSqz_L_cntr.translateX  = clamp(Head_controller.EyeSqz_L_cntr.translateX, -2.5f, 0.0f);
//				Head_controller.EyeSqz_L_cntr.translateY  = clamp(Head_controller.EyeSqz_L_cntr.translateY, -2.5f, 0.0f);
//				Head_controller.EyeSqz_L_cntr.translateZ  = clamp(Head_controller.EyeSqz_L_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.UprLid_R_cntr.translateX  = clamp(Head_controller.UprLid_R_cntr.translateX, -2.5f, 0.0f);
//				Head_controller.UprLid_R_cntr.translateY  = clamp(Head_controller.UprLid_R_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.UprLid_R_cntr.translateZ  = clamp(Head_controller.UprLid_R_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.UprLid_L_cntr.translateX  = clamp(Head_controller.UprLid_L_cntr.translateX, -2.5f, 0.0f);
//				Head_controller.UprLid_L_cntr.translateY  = clamp(Head_controller.UprLid_L_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.UprLid_L_cntr.translateZ  = clamp(Head_controller.UprLid_L_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.LwrLid_R_cntr.translateX  = clamp(Head_controller.LwrLid_R_cntr.translateX, 0.0f, 0.0f);
//				Head_controller.LwrLid_R_cntr.translateY  = clamp(Head_controller.LwrLid_R_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.LwrLid_R_cntr.translateZ  = clamp(Head_controller.LwrLid_R_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.LwrLid_L_cntr.translateX  = clamp(Head_controller.LwrLid_L_cntr.translateX, 0.0f, 0.0f);
//				Head_controller.LwrLid_L_cntr.translateY  = clamp(Head_controller.LwrLid_L_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.LwrLid_L_cntr.translateZ  = clamp(Head_controller.LwrLid_L_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.Cheek_R_2_cntr.translateX  = clamp(Head_controller.Cheek_R_2_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.Cheek_R_2_cntr.translateY  = clamp(Head_controller.Cheek_R_2_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.Cheek_R_2_cntr.translateZ  = clamp(Head_controller.Cheek_R_2_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.Cheek_L_2_cntr.translateX  = clamp(Head_controller.Cheek_L_2_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.Cheek_L_2_cntr.translateY  = clamp(Head_controller.Cheek_L_2_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.Cheek_L_2_cntr.translateZ  = clamp(Head_controller.Cheek_L_2_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.Nose_R_cntr.translateX  = clamp(Head_controller.Nose_R_cntr.translateX, 0.0f, 0.0f);
//				Head_controller.Nose_R_cntr.translateY  = clamp(Head_controller.Nose_R_cntr.translateY, 0.0f, 2.5f);
//				Head_controller.Nose_R_cntr.translateZ  = clamp(Head_controller.Nose_R_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.Nose_L_cntr.translateX  = clamp(Head_controller.Nose_L_cntr.translateX, 0.0f, 0.0f);
//				Head_controller.Nose_L_cntr.translateY  = clamp(Head_controller.Nose_L_cntr.translateY, 0.0f, 2.5f);
//				Head_controller.Nose_L_cntr.translateZ  = clamp(Head_controller.Nose_L_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.Cheek_R_cntr.translateX  = clamp(Head_controller.Cheek_R_cntr.translateX, 0.0f, 0.0f);
//				Head_controller.Cheek_R_cntr.translateY  = clamp(Head_controller.Cheek_R_cntr.translateY, 0.0f, 2.5f);
//				Head_controller.Cheek_R_cntr.translateZ  = clamp(Head_controller.Cheek_R_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.Cheek_L_cntr.translateX  = clamp(Head_controller.Cheek_L_cntr.translateX, 0.0f, 0.0f);
//				Head_controller.Cheek_L_cntr.translateY  = clamp(Head_controller.Cheek_L_cntr.translateY, 0.0f, 2.5f);
//				Head_controller.Cheek_L_cntr.translateZ  = clamp(Head_controller.Cheek_L_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.Nose_cntr.translateX  = clamp(Head_controller.Nose_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.Nose_cntr.translateY  = clamp(Head_controller.Nose_cntr.translateY, 0.0f, 0.0f);
//				Head_controller.Nose_cntr.translateZ  = clamp(Head_controller.Nose_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.UprLip_2_cntr.translateX  = clamp(Head_controller.UprLip_2_cntr.translateX, -2.5f, 0.0f);
//				Head_controller.UprLip_2_cntr.translateY  = clamp(Head_controller.UprLip_2_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.UprLip_2_cntr.translateZ  = clamp(Head_controller.UprLip_2_cntr.translateZ, -2.5f, 0.0f);
//				Head_controller.UprLip_R_2_cntr.translateX  = clamp(Head_controller.UprLip_R_2_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.UprLip_R_2_cntr.translateY  = clamp(Head_controller.UprLip_R_2_cntr.translateY, 0.0f, 2.5f);
//				Head_controller.UprLip_R_2_cntr.translateZ  = clamp(Head_controller.UprLip_R_2_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.UprLip_L_2_cntr.translateX  = clamp(Head_controller.UprLip_L_2_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.UprLip_L_2_cntr.translateY  = clamp(Head_controller.UprLip_L_2_cntr.translateY, 0.0f, 2.5f);
//				Head_controller.UprLip_L_2_cntr.translateZ  = clamp(Head_controller.UprLip_L_2_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.UprLip_R_cntr.translateX  = clamp(Head_controller.UprLip_R_cntr.translateX, -2.5f, 0.0f);
//				Head_controller.UprLip_R_cntr.translateY  = clamp(Head_controller.UprLip_R_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.UprLip_R_cntr.translateZ  = clamp(Head_controller.UprLip_R_cntr.translateZ, -2.5f, 2.5f);
//				Head_controller.UprLip_L_cntr.translateX  = clamp(Head_controller.UprLip_L_cntr.translateX, -2.5f, 0.0f);
//				Head_controller.UprLip_L_cntr.translateY  = clamp(Head_controller.UprLip_L_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.UprLip_L_cntr.translateZ  = clamp(Head_controller.UprLip_L_cntr.translateZ, -2.5f, 2.5f);
//				Head_controller.UprLip_cntr.translateX  = clamp(Head_controller.UprLip_cntr.translateX, 0.0f, 2.5f);
//				Head_controller.UprLip_cntr.translateY  = clamp(Head_controller.UprLip_cntr.translateY, 0.0f, 2.5f);
//				Head_controller.UprLip_cntr.translateZ  = clamp(Head_controller.UprLip_cntr.translateZ, -2.5f, 2.5f);
//				Head_controller.Mouth_cntr.translateX  = clamp(Head_controller.Mouth_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.Mouth_cntr.translateY  = clamp(Head_controller.Mouth_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.Mouth_cntr.translateZ  = clamp(Head_controller.Mouth_cntr.translateZ, -2.5f, 2.5f);
//				Head_controller.Crnr_R_2_cntr.translateX  = clamp(Head_controller.Crnr_R_2_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.Crnr_R_2_cntr.translateY  = clamp(Head_controller.Crnr_R_2_cntr.translateY, -2.5f, 0.0f);
//				Head_controller.Crnr_R_2_cntr.translateZ  = clamp(Head_controller.Crnr_R_2_cntr.translateZ, -2.5f, 0.0f);
//				Head_controller.Crnr_L_2_cntr.translateX  = clamp(Head_controller.Crnr_L_2_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.Crnr_L_2_cntr.translateY  = clamp(Head_controller.Crnr_L_2_cntr.translateY, -2.5f, 0.0f);
//				Head_controller.Crnr_L_2_cntr.translateZ  = clamp(Head_controller.Crnr_L_2_cntr.translateZ, -2.5f, 0.0f);
//				Head_controller.Crnr_R_cntr.translateX  = clamp(Head_controller.Crnr_R_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.Crnr_R_cntr.translateY  = clamp(Head_controller.Crnr_R_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.Crnr_R_cntr.translateZ  = clamp(Head_controller.Crnr_R_cntr.translateZ, -2.5f, 0.0f);
//				Head_controller.Crnr_L_cntr.translateX  = clamp(Head_controller.Crnr_L_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.Crnr_L_cntr.translateY  = clamp(Head_controller.Crnr_L_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.Crnr_L_cntr.translateZ  = clamp(Head_controller.Crnr_L_cntr.translateZ, -2.5f, 0.0f);
//				Head_controller.LwrLip_R_cntr.translateX  = clamp(Head_controller.LwrLip_R_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.LwrLip_R_cntr.translateY  = clamp(Head_controller.LwrLip_R_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.LwrLip_R_cntr.translateZ  = clamp(Head_controller.LwrLip_R_cntr.translateZ, -2.5f, 2.5f);
//				Head_controller.LwrLip_L_cntr.translateX  = clamp(Head_controller.LwrLip_L_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.LwrLip_L_cntr.translateY  = clamp(Head_controller.LwrLip_L_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.LwrLip_L_cntr.translateZ  = clamp(Head_controller.LwrLip_L_cntr.translateZ, -2.5f, 2.5f);
//				Head_controller.LwrLip_cntr.translateX  = clamp(Head_controller.LwrLip_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.LwrLip_cntr.translateY  = clamp(Head_controller.LwrLip_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.LwrLip_cntr.translateZ  = clamp(Head_controller.LwrLip_cntr.translateZ, -2.5f, 2.5f);
//				Head_controller.Chin_R_cntr.translateX  = clamp(Head_controller.Chin_R_cntr.translateX, 0.0f, 2.5f);
//				Head_controller.Chin_R_cntr.translateY  = clamp(Head_controller.Chin_R_cntr.translateY, -2.5f, 0.0f);
//				Head_controller.Chin_R_cntr.translateZ  = clamp(Head_controller.Chin_R_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.Chin_L_cntr.translateX  = clamp(Head_controller.Chin_L_cntr.translateX, 0.0f, 2.5f);
//				Head_controller.Chin_L_cntr.translateY  = clamp(Head_controller.Chin_L_cntr.translateY, -2.5f, 0.0f);
//				Head_controller.Chin_L_cntr.translateZ  = clamp(Head_controller.Chin_L_cntr.translateZ, 0.0f, 0.0f);
//				Head_controller.Chin_cntr.translateX  = clamp(Head_controller.Chin_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.Chin_cntr.translateY  = clamp(Head_controller.Chin_cntr.translateY, -2.5f, 2.5f);
//				Head_controller.Chin_cntr.translateZ  = clamp(Head_controller.Chin_cntr.translateZ, -2.5f, 0.0f);
//				Head_controller.Jaw_cntr.translateX  = clamp(Head_controller.Jaw_cntr.translateX, -2.5f, 2.5f);
//				Head_controller.Jaw_cntr.translateY  = clamp(Head_controller.Jaw_cntr.translateY, -2.5f, 0.0f);
//				Head_controller.Jaw_cntr.translateZ  = clamp(Head_controller.Jaw_cntr.translateZ, -2.5f, 2.5f);
//			}
//			// this segment generated from 'Controllers_Constraints'
//			{
//				Head_controller.Tongue_cntr_p.translateX = -Head_controller.Tongue_cntr.translateX;
//				Head_controller.Tongue_cntr_p.translateY = -Head_controller.Tongue_cntr.translateY;
//				Head_controller.Tongue_cntr_p.translateZ = -Head_controller.Tongue_cntr.translateZ;
//				Head_controller.Tongue_curl_cntr_p.translateX = -Head_controller.Tongue_curl_cntr.translateX;
//				Head_controller.Tongue_curl_cntr_p.translateY = -Head_controller.Tongue_curl_cntr.translateY;
//				Head_controller.Tongue_curl_cntr_p.translateZ = -Head_controller.Tongue_curl_cntr.translateZ;
//				Head_controller.Teeth_cntr_p.translateX = -Head_controller.Teeth_cntr.translateX;
//				Head_controller.Teeth_cntr_p.translateY = -Head_controller.Teeth_cntr.translateY;
//				Head_controller.Teeth_cntr_p.translateZ = -Head_controller.Teeth_cntr.translateZ;
//				Head_controller.Jaw_cntr_p.translateX = -Head_controller.Jaw_cntr.translateX;
//				Head_controller.Jaw_cntr_p.translateY = -Head_controller.Jaw_cntr.translateY;
//				Head_controller.Jaw_cntr_p.translateZ = -Head_controller.Jaw_cntr.translateZ;
//				Head_controller.Chin_cntr_p.translateX = -Head_controller.Chin_cntr.translateX;
//				Head_controller.Chin_cntr_p.translateY = -Head_controller.Chin_cntr.translateY;
//				Head_controller.Chin_cntr_p.translateZ = -Head_controller.Chin_cntr.translateZ;
//				Head_controller.Neck_cntr_p.translateX = -Head_controller.Neck_cntr.translateX;
//				Head_controller.Neck_cntr_p.translateY = -Head_controller.Neck_cntr.translateY;
//				Head_controller.Neck_cntr_p.translateZ = -Head_controller.Neck_cntr.translateZ;
//				Head_controller.Chin_L_cntr_p.translateX = -Head_controller.Chin_L_cntr.translateX;
//				Head_controller.Chin_L_cntr_p.translateY = -Head_controller.Chin_L_cntr.translateY;
//				Head_controller.Chin_L_cntr_p.translateZ = -Head_controller.Chin_L_cntr.translateZ;
//				Head_controller.Chin_R_cntr_p.translateX = -Head_controller.Chin_R_cntr.translateX;
//				Head_controller.Chin_R_cntr_p.translateY = -Head_controller.Chin_R_cntr.translateY;
//				Head_controller.Chin_R_cntr_p.translateZ = -Head_controller.Chin_R_cntr.translateZ;
//				Head_controller.LwrLip_R_cntr_p.translateX = -Head_controller.LwrLip_R_cntr.translateX;
//				Head_controller.LwrLip_R_cntr_p.translateY = -Head_controller.LwrLip_R_cntr.translateY;
//				Head_controller.LwrLip_R_cntr_p.translateZ = -Head_controller.LwrLip_R_cntr.translateZ;
//				Head_controller.LwrLip_L_cntr_p.translateX = -Head_controller.LwrLip_L_cntr.translateX;
//				Head_controller.LwrLip_L_cntr_p.translateY = -Head_controller.LwrLip_L_cntr.translateY;
//				Head_controller.LwrLip_L_cntr_p.translateZ = -Head_controller.LwrLip_L_cntr.translateZ;
//				Head_controller.LwrLip_cntr_p.translateX = -Head_controller.LwrLip_cntr.translateX;
//				Head_controller.LwrLip_cntr_p.translateY = -Head_controller.LwrLip_cntr.translateY;
//				Head_controller.LwrLip_cntr_p.translateZ = -Head_controller.LwrLip_cntr.translateZ;
//				Head_controller.Mouth_cntr_p.translateX = -Head_controller.Mouth_cntr.translateX;
//				Head_controller.Mouth_cntr_p.translateY = -Head_controller.Mouth_cntr.translateY;
//				Head_controller.Mouth_cntr_p.translateZ = -Head_controller.Mouth_cntr.translateZ;
//				Head_controller.Crnr_L_cntr_p.translateX = -Head_controller.Crnr_L_cntr.translateX;
//				Head_controller.Crnr_L_cntr_p.translateY = -Head_controller.Crnr_L_cntr.translateY;
//				Head_controller.Crnr_L_cntr_p.translateZ = -Head_controller.Crnr_L_cntr.translateZ;
//				Head_controller.Crnr_R_cntr_p.translateX = -Head_controller.Crnr_R_cntr.translateX;
//				Head_controller.Crnr_R_cntr_p.translateY = -Head_controller.Crnr_R_cntr.translateY;
//				Head_controller.Crnr_R_cntr_p.translateZ = -Head_controller.Crnr_R_cntr.translateZ;
//				Head_controller.UprLip_L_cntr_p.translateX = -Head_controller.UprLip_L_cntr.translateX;
//				Head_controller.UprLip_L_cntr_p.translateY = -Head_controller.UprLip_L_cntr.translateY;
//				Head_controller.UprLip_L_cntr_p.translateZ = -Head_controller.UprLip_L_cntr.translateZ;
//				Head_controller.UprLip_R_cntr_p.translateX = -Head_controller.UprLip_R_cntr.translateX;
//				Head_controller.UprLip_R_cntr_p.translateY = -Head_controller.UprLip_R_cntr.translateY;
//				Head_controller.UprLip_R_cntr_p.translateZ = -Head_controller.UprLip_R_cntr.translateZ;
//				Head_controller.UprLip_cntr_p.translateX = -Head_controller.UprLip_cntr.translateX;
//				Head_controller.UprLip_cntr_p.translateY = -Head_controller.UprLip_cntr.translateY;
//				Head_controller.UprLip_cntr_p.translateZ = -Head_controller.UprLip_cntr.translateZ;
//				Head_controller.Crnr_L_2_cntr_p.translateX = -Head_controller.Crnr_L_2_cntr.translateX;
//				Head_controller.Crnr_L_2_cntr_p.translateY = -Head_controller.Crnr_L_2_cntr.translateY;
//				Head_controller.Crnr_L_2_cntr_p.translateZ = -Head_controller.Crnr_L_2_cntr.translateZ;
//				Head_controller.Crnr_R_2_cntr_p.translateX = -Head_controller.Crnr_R_2_cntr.translateX;
//				Head_controller.Crnr_R_2_cntr_p.translateY = -Head_controller.Crnr_R_2_cntr.translateY;
//				Head_controller.Crnr_R_2_cntr_p.translateZ = -Head_controller.Crnr_R_2_cntr.translateZ;
//				Head_controller.Cheek_L_cntr_p.translateX = -Head_controller.Cheek_L_cntr.translateX;
//				Head_controller.Cheek_L_cntr_p.translateY = -Head_controller.Cheek_L_cntr.translateY;
//				Head_controller.Cheek_L_cntr_p.translateZ = -Head_controller.Cheek_L_cntr.translateZ;
//				Head_controller.Cheek_R_cntr_p.translateX = -Head_controller.Cheek_R_cntr.translateX;
//				Head_controller.Cheek_R_cntr_p.translateY = -Head_controller.Cheek_R_cntr.translateY;
//				Head_controller.Cheek_R_cntr_p.translateZ = -Head_controller.Cheek_R_cntr.translateZ;
//				Head_controller.UprLip_2_cntr_p.translateX = -Head_controller.UprLip_2_cntr.translateX;
//				Head_controller.UprLip_2_cntr_p.translateY = -Head_controller.UprLip_2_cntr.translateY;
//				Head_controller.UprLip_2_cntr_p.translateZ = -Head_controller.UprLip_2_cntr.translateZ;
//				Head_controller.UprLip_L_2_cntr_p.translateX = -Head_controller.UprLip_L_2_cntr.translateX;
//				Head_controller.UprLip_L_2_cntr_p.translateY = -Head_controller.UprLip_L_2_cntr.translateY;
//				Head_controller.UprLip_L_2_cntr_p.translateZ = -Head_controller.UprLip_L_2_cntr.translateZ;
//				Head_controller.UprLip_R_2_cntr_p.translateX = -Head_controller.UprLip_R_2_cntr.translateX;
//				Head_controller.UprLip_R_2_cntr_p.translateY = -Head_controller.UprLip_R_2_cntr.translateY;
//				Head_controller.UprLip_R_2_cntr_p.translateZ = -Head_controller.UprLip_R_2_cntr.translateZ;
//				Head_controller.Nose_cntr_p.translateX = -Head_controller.Nose_cntr.translateX;
//				Head_controller.Nose_cntr_p.translateY = -Head_controller.Nose_cntr.translateY;
//				Head_controller.Nose_cntr_p.translateZ = -Head_controller.Nose_cntr.translateZ;
//				Head_controller.Nose_R_cntr_p.translateX = -Head_controller.Nose_R_cntr.translateX;
//				Head_controller.Nose_R_cntr_p.translateY = -Head_controller.Nose_R_cntr.translateY;
//				Head_controller.Nose_R_cntr_p.translateZ = -Head_controller.Nose_R_cntr.translateZ;
//				Head_controller.Nose_L_cntr_p.translateX = -Head_controller.Nose_L_cntr.translateX;
//				Head_controller.Nose_L_cntr_p.translateY = -Head_controller.Nose_L_cntr.translateY;
//				Head_controller.Nose_L_cntr_p.translateZ = -Head_controller.Nose_L_cntr.translateZ;
//				Head_controller.Cheek_R_2_cntr_p.translateX = -Head_controller.Cheek_R_2_cntr.translateX;
//				Head_controller.Cheek_R_2_cntr_p.translateY = -Head_controller.Cheek_R_2_cntr.translateY;
//				Head_controller.Cheek_R_2_cntr_p.translateZ = -Head_controller.Cheek_R_2_cntr.translateZ;
//				Head_controller.Cheek_L_2_cntr_p.translateX = -Head_controller.Cheek_L_2_cntr.translateX;
//				Head_controller.Cheek_L_2_cntr_p.translateY = -Head_controller.Cheek_L_2_cntr.translateY;
//				Head_controller.Cheek_L_2_cntr_p.translateZ = -Head_controller.Cheek_L_2_cntr.translateZ;
//				Head_controller.LwrLid_L_cntr_p.translateX = -Head_controller.LwrLid_L_cntr.translateX;
//				Head_controller.LwrLid_L_cntr_p.translateY = -Head_controller.LwrLid_L_cntr.translateY;
//				Head_controller.LwrLid_L_cntr_p.translateZ = -Head_controller.LwrLid_L_cntr.translateZ;
//				Head_controller.LwrLid_R_cntr_p.translateX = -Head_controller.LwrLid_R_cntr.translateX;
//				Head_controller.LwrLid_R_cntr_p.translateY = -Head_controller.LwrLid_R_cntr.translateY;
//				Head_controller.LwrLid_R_cntr_p.translateZ = -Head_controller.LwrLid_R_cntr.translateZ;
//				Head_controller.UprLid_L_cntr_p.translateX = -Head_controller.UprLid_L_cntr.translateX;
//				Head_controller.UprLid_L_cntr_p.translateY = -Head_controller.UprLid_L_cntr.translateY;
//				Head_controller.UprLid_L_cntr_p.translateZ = -Head_controller.UprLid_L_cntr.translateZ;
//				Head_controller.UprLid_R_cntr_p.translateX = -Head_controller.UprLid_R_cntr.translateX;
//				Head_controller.UprLid_R_cntr_p.translateY = -Head_controller.UprLid_R_cntr.translateY;
//				Head_controller.UprLid_R_cntr_p.translateZ = -Head_controller.UprLid_R_cntr.translateZ;
//				Head_controller.EyeSqz_L_cntr_p.translateX = -Head_controller.EyeSqz_L_cntr.translateX;
//				Head_controller.EyeSqz_L_cntr_p.translateY = -Head_controller.EyeSqz_L_cntr.translateY;
//				Head_controller.EyeSqz_L_cntr_p.translateZ = -Head_controller.EyeSqz_L_cntr.translateZ;
//				Head_controller.EyeSqz_R_cntr_p.translateX = -Head_controller.EyeSqz_R_cntr.translateX;
//				Head_controller.EyeSqz_R_cntr_p.translateY = -Head_controller.EyeSqz_R_cntr.translateY;
//				Head_controller.EyeSqz_R_cntr_p.translateZ = -Head_controller.EyeSqz_R_cntr.translateZ;
//				Head_controller.BrowIn_L_cntr_p.translateX = -Head_controller.BrowIn_L_cntr.translateX;
//				Head_controller.BrowIn_L_cntr_p.translateY = -Head_controller.BrowIn_L_cntr.translateY;
//				Head_controller.BrowIn_L_cntr_p.translateZ = -Head_controller.BrowIn_L_cntr.translateZ;
//				Head_controller.BrowIn_R_cntr_p.translateX = -Head_controller.BrowIn_R_cntr.translateX;
//				Head_controller.BrowIn_R_cntr_p.translateY = -Head_controller.BrowIn_R_cntr.translateY;
//				Head_controller.BrowIn_R_cntr_p.translateZ = -Head_controller.BrowIn_R_cntr.translateZ;
//				Head_controller.BrowOut_L_cntr_p.translateX = -Head_controller.BrowOut_L_cntr.translateX;
//				Head_controller.BrowOut_L_cntr_p.translateY = -Head_controller.BrowOut_L_cntr.translateY;
//				Head_controller.BrowOut_L_cntr_p.translateZ = -Head_controller.BrowOut_L_cntr.translateZ;
//				Head_controller.BrowOut_R_cntr_p.translateX = -Head_controller.BrowOut_R_cntr.translateX;
//				Head_controller.BrowOut_R_cntr_p.translateY = -Head_controller.BrowOut_R_cntr.translateY;
//				Head_controller.BrowOut_R_cntr_p.translateZ = -Head_controller.BrowOut_R_cntr.translateZ;
//				Head_controller.Brow_cntr_p.translateX = -Head_controller.Brow_cntr.translateX;
//				Head_controller.Brow_cntr_p.translateY = -Head_controller.Brow_cntr.translateY;
//				Head_controller.Brow_cntr_p.translateZ = -Head_controller.Brow_cntr.translateZ;
//				Head_controller.Head_cntr_p.translateX = -Head_controller.Head_cntr.translateX;
//				Head_controller.Head_cntr_p.translateY = -Head_controller.Head_cntr.translateY;
//				Head_controller.Head_cntr_p.translateZ = -Head_controller.Head_cntr.translateZ;
//				Head_controller.UprLip_cntr_adj_p.translateX = -Head_controller.UprLip_cntr_adj.translateX;
//				Head_controller.UprLip_cntr_adj_p.translateY = -Head_controller.UprLip_cntr_adj.translateY;
//				Head_controller.UprLip_cntr_adj_p.translateZ = -Head_controller.UprLip_cntr_adj.translateZ;
//				Head_controller.UprLip_cntr_L_adj_p.translateX = -Head_controller.UprLip_cntr_L_adj.translateX;
//				Head_controller.UprLip_cntr_L_adj_p.translateY = -Head_controller.UprLip_cntr_L_adj.translateY;
//				Head_controller.UprLip_cntr_L_adj_p.translateZ = -Head_controller.UprLip_cntr_L_adj.translateZ;
//				Head_controller.UprLip_cntr_R_adj_p.translateX = -Head_controller.UprLip_cntr_R_adj.translateX;
//				Head_controller.UprLip_cntr_R_adj_p.translateY = -Head_controller.UprLip_cntr_R_adj.translateY;
//				Head_controller.UprLip_cntr_R_adj_p.translateZ = -Head_controller.UprLip_cntr_R_adj.translateZ;
//				Head_controller.Corner_cntr_L_adj_p.translateX = -Head_controller.Corner_cntr_L_adj.translateX;
//				Head_controller.Corner_cntr_L_adj_p.translateY = -Head_controller.Corner_cntr_L_adj.translateY;
//				Head_controller.Corner_cntr_L_adj_p.translateZ = -Head_controller.Corner_cntr_L_adj.translateZ;
//				Head_controller.Corner_cntr_R_adj_p.translateX = -Head_controller.Corner_cntr_R_adj.translateX;
//				Head_controller.Corner_cntr_R_adj_p.translateY = -Head_controller.Corner_cntr_R_adj.translateY;
//				Head_controller.Corner_cntr_R_adj_p.translateZ = -Head_controller.Corner_cntr_R_adj.translateZ;
//				Head_controller.LwrLip_cntr_adj_p.translateX = -Head_controller.LwrLip_cntr_adj.translateX;
//				Head_controller.LwrLip_cntr_adj_p.translateY = -Head_controller.LwrLip_cntr_adj.translateY;
//				Head_controller.LwrLip_cntr_adj_p.translateZ = -Head_controller.LwrLip_cntr_adj.translateZ;
//				Head_controller.LwrLip_cntr_L_adj_p.translateX = -Head_controller.LwrLip_cntr_L_adj.translateX;
//				Head_controller.LwrLip_cntr_L_adj_p.translateY = -Head_controller.LwrLip_cntr_L_adj.translateY;
//				Head_controller.LwrLip_cntr_L_adj_p.translateZ = -Head_controller.LwrLip_cntr_L_adj.translateZ;
//				Head_controller.LwrLip_cntr_R_adj_p.translateX = -Head_controller.LwrLip_cntr_R_adj.translateX;
//				Head_controller.LwrLip_cntr_R_adj_p.translateY = -Head_controller.LwrLip_cntr_R_adj.translateY;
//				Head_controller.LwrLip_cntr_R_adj_p.translateZ = -Head_controller.LwrLip_cntr_R_adj.translateZ;
//				Head_controller.UprLip_cntr_adj_p.scaleX = 1/Head_controller.UprLip_cntr_adj.scaleX;
//				Head_controller.UprLip_cntr_adj_p.scaleY = 1/Head_controller.UprLip_cntr_adj.scaleY;
//				Head_controller.UprLip_cntr_adj_p.scaleZ = 1/Head_controller.UprLip_cntr_adj.scaleZ;
//				Head_controller.UprLip_cntr_L_adj_p.scaleX = 1/Head_controller.UprLip_cntr_L_adj.scaleX;
//				Head_controller.UprLip_cntr_L_adj_p.scaleY = 1/Head_controller.UprLip_cntr_L_adj.scaleY;
//				Head_controller.UprLip_cntr_L_adj_p.scaleZ = 1/Head_controller.UprLip_cntr_L_adj.scaleZ;
//				Head_controller.UprLip_cntr_R_adj_p.scaleX = 1/Head_controller.UprLip_cntr_R_adj.scaleX;
//				Head_controller.UprLip_cntr_R_adj_p.scaleY = 1/Head_controller.UprLip_cntr_R_adj.scaleY;
//				Head_controller.UprLip_cntr_R_adj_p.scaleZ = 1/Head_controller.UprLip_cntr_R_adj.scaleZ;
//				Head_controller.Corner_cntr_L_adj_p.scaleX = 1/Head_controller.Corner_cntr_L_adj.scaleX;
//				Head_controller.Corner_cntr_L_adj_p.scaleY = 1/Head_controller.Corner_cntr_L_adj.scaleY;
//				Head_controller.Corner_cntr_L_adj_p.scaleZ = 1/Head_controller.Corner_cntr_L_adj.scaleZ;
//				Head_controller.Corner_cntr_R_adj_p.scaleX = 1/Head_controller.Corner_cntr_R_adj.scaleX;
//				Head_controller.Corner_cntr_R_adj_p.scaleY = 1/Head_controller.Corner_cntr_R_adj.scaleY;
//				Head_controller.Corner_cntr_R_adj_p.scaleZ = 1/Head_controller.Corner_cntr_R_adj.scaleZ;
//				Head_controller.LwrLip_cntr_adj_p.scaleX = 1/Head_controller.LwrLip_cntr_adj.scaleX;
//				Head_controller.LwrLip_cntr_adj_p.scaleY = 1/Head_controller.LwrLip_cntr_adj.scaleY;
//				Head_controller.LwrLip_cntr_adj_p.scaleZ = 1/Head_controller.LwrLip_cntr_adj.scaleZ;
//				Head_controller.LwrLip_cntr_L_adj_p.scaleX = 1/Head_controller.LwrLip_cntr_L_adj.scaleX;
//				Head_controller.LwrLip_cntr_L_adj_p.scaleY = 1/Head_controller.LwrLip_cntr_L_adj.scaleY;
//				Head_controller.LwrLip_cntr_L_adj_p.scaleZ = 1/Head_controller.LwrLip_cntr_L_adj.scaleZ;
//				Head_controller.LwrLip_cntr_R_adj_p.scaleX = 1/Head_controller.LwrLip_cntr_R_adj.scaleX;
//				Head_controller.LwrLip_cntr_R_adj_p.scaleY = 1/Head_controller.LwrLip_cntr_R_adj.scaleY;
//				Head_controller.LwrLip_cntr_R_adj_p.scaleZ = 1/Head_controller.LwrLip_cntr_R_adj.scaleZ;
//				Head_controller.UprLid_1_cntr_L_adj_p.translateX = -Head_controller.UprLid_1_cntr_L_adj.translateX;
//				Head_controller.UprLid_1_cntr_L_adj_p.translateY = -Head_controller.UprLid_1_cntr_L_adj.translateY;
//				Head_controller.UprLid_1_cntr_L_adj_p.translateZ = -Head_controller.UprLid_1_cntr_L_adj.translateZ;
//				Head_controller.UprLid_2_cntr_L_adj_p.translateX = -Head_controller.UprLid_2_cntr_L_adj.translateX;
//				Head_controller.UprLid_2_cntr_L_adj_p.translateY = -Head_controller.UprLid_2_cntr_L_adj.translateY;
//				Head_controller.UprLid_2_cntr_L_adj_p.translateZ = -Head_controller.UprLid_2_cntr_L_adj.translateZ;
//				Head_controller.UprLid_3_cntr_L_adj_p.translateX = -Head_controller.UprLid_3_cntr_L_adj.translateX;
//				Head_controller.UprLid_3_cntr_L_adj_p.translateY = -Head_controller.UprLid_3_cntr_L_adj.translateY;
//				Head_controller.UprLid_3_cntr_L_adj_p.translateZ = -Head_controller.UprLid_3_cntr_L_adj.translateZ;
//				Head_controller.UprLid_1_cntr_R_adj_p.translateX = -Head_controller.UprLid_1_cntr_R_adj.translateX;
//				Head_controller.UprLid_1_cntr_R_adj_p.translateY = -Head_controller.UprLid_1_cntr_R_adj.translateY;
//				Head_controller.UprLid_1_cntr_R_adj_p.translateZ = -Head_controller.UprLid_1_cntr_R_adj.translateZ;
//				Head_controller.UprLid_2_cntr_R_adj_p.translateX = -Head_controller.UprLid_2_cntr_R_adj.translateX;
//				Head_controller.UprLid_2_cntr_R_adj_p.translateY = -Head_controller.UprLid_2_cntr_R_adj.translateY;
//				Head_controller.UprLid_2_cntr_R_adj_p.translateZ = -Head_controller.UprLid_2_cntr_R_adj.translateZ;
//				Head_controller.UprLid_3_cntr_R_adj_p.translateX = -Head_controller.UprLid_3_cntr_R_adj.translateX;
//				Head_controller.UprLid_3_cntr_R_adj_p.translateY = -Head_controller.UprLid_3_cntr_R_adj.translateY;
//				Head_controller.UprLid_3_cntr_R_adj_p.translateZ = -Head_controller.UprLid_3_cntr_R_adj.translateZ;
//				Head_controller.LwrLid_1_cntr_L_adj_p.translateX = -Head_controller.LwrLid_1_cntr_L_adj.translateX;
//				Head_controller.LwrLid_1_cntr_L_adj_p.translateY = -Head_controller.LwrLid_1_cntr_L_adj.translateY;
//				Head_controller.LwrLid_1_cntr_L_adj_p.translateZ = -Head_controller.LwrLid_1_cntr_L_adj.translateZ;
//				Head_controller.LwrLid_2_cntr_L_adj_p.translateX = -Head_controller.LwrLid_2_cntr_L_adj.translateX;
//				Head_controller.LwrLid_2_cntr_L_adj_p.translateY = -Head_controller.LwrLid_2_cntr_L_adj.translateY;
//				Head_controller.LwrLid_2_cntr_L_adj_p.translateZ = -Head_controller.LwrLid_2_cntr_L_adj.translateZ;
//				Head_controller.LwrLid_3_cntr_L_adj_p.translateX = -Head_controller.LwrLid_3_cntr_L_adj.translateX;
//				Head_controller.LwrLid_3_cntr_L_adj_p.translateY = -Head_controller.LwrLid_3_cntr_L_adj.translateY;
//				Head_controller.LwrLid_3_cntr_L_adj_p.translateZ = -Head_controller.LwrLid_3_cntr_L_adj.translateZ;
//				Head_controller.LwrLid_1_cntr_R_adj_p.translateX = -Head_controller.LwrLid_1_cntr_R_adj.translateX;
//				Head_controller.LwrLid_1_cntr_R_adj_p.translateY = -Head_controller.LwrLid_1_cntr_R_adj.translateY;
//				Head_controller.LwrLid_1_cntr_R_adj_p.translateZ = -Head_controller.LwrLid_1_cntr_R_adj.translateZ;
//				Head_controller.LwrLid_2_cntr_R_adj_p.translateX = -Head_controller.LwrLid_2_cntr_R_adj.translateX;
//				Head_controller.LwrLid_2_cntr_R_adj_p.translateY = -Head_controller.LwrLid_2_cntr_R_adj.translateY;
//				Head_controller.LwrLid_2_cntr_R_adj_p.translateZ = -Head_controller.LwrLid_2_cntr_R_adj.translateZ;
//				Head_controller.LwrLid_3_cntr_R_adj_p.translateX = -Head_controller.LwrLid_3_cntr_R_adj.translateX;
//				Head_controller.LwrLid_3_cntr_R_adj_p.translateY = -Head_controller.LwrLid_3_cntr_R_adj.translateY;
//				Head_controller.LwrLid_3_cntr_R_adj_p.translateZ = -Head_controller.LwrLid_3_cntr_R_adj.translateZ;
//				Head_controller.Brow_1_cntr_L_adj_p.translateX = -Head_controller.Brow_1_cntr_L_adj.translateX;
//				Head_controller.Brow_1_cntr_L_adj_p.translateY = -Head_controller.Brow_1_cntr_L_adj.translateY;
//				Head_controller.Brow_1_cntr_L_adj_p.translateZ = -Head_controller.Brow_1_cntr_L_adj.translateZ;
//				Head_controller.Brow_2_cntr_L_adj_p.translateX = -Head_controller.Brow_2_cntr_L_adj.translateX;
//				Head_controller.Brow_2_cntr_L_adj_p.translateY = -Head_controller.Brow_2_cntr_L_adj.translateY;
//				Head_controller.Brow_2_cntr_L_adj_p.translateZ = -Head_controller.Brow_2_cntr_L_adj.translateZ;
//				Head_controller.Brow_3_cntr_L_adj_p.translateX = -Head_controller.Brow_3_cntr_L_adj.translateX;
//				Head_controller.Brow_3_cntr_L_adj_p.translateY = -Head_controller.Brow_3_cntr_L_adj.translateY;
//				Head_controller.Brow_3_cntr_L_adj_p.translateZ = -Head_controller.Brow_3_cntr_L_adj.translateZ;
//				Head_controller.Brow_1_cntr_R_adj_p.translateX = -Head_controller.Brow_1_cntr_R_adj.translateX;
//				Head_controller.Brow_1_cntr_R_adj_p.translateY = -Head_controller.Brow_1_cntr_R_adj.translateY;
//				Head_controller.Brow_1_cntr_R_adj_p.translateZ = -Head_controller.Brow_1_cntr_R_adj.translateZ;
//				Head_controller.Brow_2_cntr_R_adj_p.translateX = -Head_controller.Brow_2_cntr_R_adj.translateX;
//				Head_controller.Brow_2_cntr_R_adj_p.translateY = -Head_controller.Brow_2_cntr_R_adj.translateY;
//				Head_controller.Brow_2_cntr_R_adj_p.translateZ = -Head_controller.Brow_2_cntr_R_adj.translateZ;
//				Head_controller.Brow_3_cntr_R_adj_p.translateX = -Head_controller.Brow_3_cntr_R_adj.translateX;
//				Head_controller.Brow_3_cntr_R_adj_p.translateY = -Head_controller.Brow_3_cntr_R_adj.translateY;
//				Head_controller.Brow_3_cntr_R_adj_p.translateZ = -Head_controller.Brow_3_cntr_R_adj.translateZ;
//			}
//		}

//		// --- ResolveBlendShapes
//		public override unsafe void ResolveBlendShapes(void* ptrSnappersControllers, void* ptrSnappersBlendShapes, void* ptrSnappersShaderParam)
//		{
//			ResolveBlendShapes(
//				ref UnsafeUtilityEx.AsRef<SnappersControllers>(ptrSnappersControllers),
//				ref UnsafeUtilityEx.AsRef<SnappersBlendShapes>(ptrSnappersBlendShapes),
//				ref UnsafeUtilityEx.AsRef<SnappersShaderParam>(ptrSnappersShaderParam)
//			);
//		}
//		public void ResolveBlendShapes(ref SnappersControllers Head_controller, ref SnappersBlendShapes Head_blendShape, ref SnappersShaderParam SkinShader)
//		{
//			// this segment generated from 'Linears_controller'
//			{
//				//-----------------------------------------------Neck_cntr----------------------------------------------------------
//				Head_blendShape.neck_blow = max(0,Head_controller.Neck_cntr.translateZ/2.5f);
//				Head_blendShape.neck_muscle = max(0,Head_controller.Neck_cntr.translateX/2.5f);
//				Head_blendShape.neck_slide = max(0,-Head_controller.Neck_cntr.translateZ/2.5f);
//				Head_blendShape.swallow_1 = linstep(0,0.2f,max(0,-Head_controller.Neck_cntr.translateY/2.5f));
//				Head_blendShape.swallow_2 = linstep(0.2f,0.4f,max(0,-Head_controller.Neck_cntr.translateY/2.5f));
//				Head_blendShape.swallow_3 = linstep(0.4f,0.6f,max(0,-Head_controller.Neck_cntr.translateY/2.5f));
//				Head_blendShape.swallow_4 = linstep(0.6f,0.8f,max(0,-Head_controller.Neck_cntr.translateY/2.5f));
//				Head_blendShape.swallow_5 = linstep(0.8f,1.0f,max(0,-Head_controller.Neck_cntr.translateY/2.5f));
//				//------------------------------------------------------------------------------------------------------------------
//				//-----------------------------------------------Jaw_cntr-----------------------------------------------------------
//				Head_blendShape.jaw_drop = hermite(0,0.0f,4,-4,max(0,-Head_controller.Jaw_cntr.translateY/2.5f));
//				Head_blendShape.jaw_fwd = max(0,Head_controller.Jaw_cntr.translateZ/2.5f);
//				Head_blendShape.jaw_back = max(0,-Head_controller.Jaw_cntr.translateZ/2.5f);
//				//------------------------------------------------------------------------------------------------------------------
//				//------------------------------------------------Chin_cntr---------------------------------------------------------
//				Head_blendShape.chin_dn = max(0,-Head_controller.Chin_cntr.translateY/2.5f);
//				Head_blendShape.chin_tension = max(0,-Head_controller.Chin_cntr.translateX/2.5f);
//				Head_blendShape.lwr_lip_vol = max(0,-Head_controller.Chin_cntr.translateZ/2.5f);
//				Head_blendShape.jaw_clench = max(0,Head_controller.Chin_cntr.translateX/2.5f);
//				//------------------------------------------------------------------------------------------------------------------
//				//-------------------------------------------Chin_L_cntr/Chin_R_cntr------------------------------------------------
//				Head_blendShape.neck_tension_1 = max(0,Head_controller.Chin_L_cntr.translateX/2.5f);
//				Head_blendShape.neck_tension_2 = max(0,Head_controller.Chin_R_cntr.translateX/2.5f);
//				//------------------------------------------------------------------------------------------------------------------
//				//------------------------------------------------H/O/Kiss----------------------------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//-----------------------------------------------LwrLip_cntr--------------------------------------------------------
//				Head_blendShape.lwr_lip_fwd = max(0,Head_controller.LwrLip_cntr.translateZ/2.5f);
//				Head_blendShape.lwr_lip_bk = max(0,-Head_controller.LwrLip_cntr.translateZ/2.5f);
//				//------------------------------------------------------------------------------------------------------------------
//				//------------------------------------------------Mouth_cntr--------------------------------------------------------
//				Head_blendShape.lip_in_p = max(0,-Head_controller.Mouth_cntr.translateZ/2.5f);
//				Head_blendShape.mouth_dn = max(0,-Head_controller.Mouth_cntr.translateY/2.5f);
//				Head_blendShape.mouth_up = max(0,Head_controller.Mouth_cntr.translateY/2.5f);
//				//------------------------------------------------------------------------------------------------------------------
//				//--------------------------------------------Crnr_L_cntr/Crnr_R_cntr-----------------------------------------------
//				Head_blendShape.tight_1 = max(0,-Head_controller.Crnr_L_2_cntr.translateX/2.5f);
//				Head_blendShape.tight_2 = max(0,-Head_controller.Crnr_R_2_cntr.translateX/2.5f);
//				Head_blendShape.wide_1 = max(0,Head_controller.Crnr_L_cntr.translateX/2.5f);
//				Head_blendShape.wide_2 = max(0,Head_controller.Crnr_R_cntr.translateX/2.5f);
//				Head_blendShape.sticky_lips_1_1 = linstep(0,0.2f,max(0,-Head_controller.Crnr_L_cntr.translateZ/2.5f));
//				Head_blendShape.sticky_lips_2_1 = linstep(0.2f,0.4f,max(0,-Head_controller.Crnr_L_cntr.translateZ/2.5f));
//				Head_blendShape.sticky_lips_3_1 = linstep(0.4f,0.6f,max(0,-Head_controller.Crnr_L_cntr.translateZ/2.5f));
//				Head_blendShape.sticky_lips_4_1 = linstep(0.6f,0.8f,max(0,-Head_controller.Crnr_L_cntr.translateZ/2.5f));
//				Head_blendShape.sticky_lips_5_1 = linstep(0.8f,1.0f,max(0,-Head_controller.Crnr_L_cntr.translateZ/2.5f));
//				Head_blendShape.sticky_lips_1_2 = linstep(0,0.2f,max(0,-Head_controller.Crnr_R_cntr.translateZ/2.5f));
//				Head_blendShape.sticky_lips_2_2 = linstep(0.2f,0.4f,max(0,-Head_controller.Crnr_R_cntr.translateZ/2.5f));
//				Head_blendShape.sticky_lips_3_2 = linstep(0.4f,0.6f,max(0,-Head_controller.Crnr_R_cntr.translateZ/2.5f));
//				Head_blendShape.sticky_lips_4_2 = linstep(0.6f,0.8f,max(0,-Head_controller.Crnr_R_cntr.translateZ/2.5f));
//				Head_blendShape.sticky_lips_5_2 = linstep(0.8f,1.0f,max(0,-Head_controller.Crnr_R_cntr.translateZ/2.5f));
//				//------------------------------------------------------------------------------------------------------------------
//				//--------------------------------------------Crnr_L_2_cntr/Crnr_R_2_cntr-------------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//------------------------------------------------UprLip_cntr-------------------------------------------------------
//				Head_blendShape.upr_lip_fwd = max(0,Head_controller.UprLip_cntr.translateZ/2.5f);
//				Head_blendShape.upr_lip_bk = max(0,-Head_controller.UprLip_cntr.translateZ/2.5f);
//				Head_blendShape.o_wide_1 = max(0,Head_controller.UprLip_cntr.translateX/2.5f) * Head_blendShape.o_1;
//				Head_blendShape.o_wide_2 = max(0,Head_controller.UprLip_cntr.translateX/2.5f) * Head_blendShape.o_3;
//				Head_blendShape.o_wide_3 = max(0,Head_controller.UprLip_cntr.translateX/2.5f) * Head_blendShape.o_4;
//				Head_blendShape.o_wide_4 = max(0,Head_controller.UprLip_cntr.translateX/2.5f) * Head_blendShape.o_6;
//				Head_blendShape.funnel_wide_1 = max(0,Head_controller.UprLip_cntr.translateX/2.5f) * Head_blendShape.funnel_1;
//				Head_blendShape.funnel_wide_2 = max(0,Head_controller.UprLip_cntr.translateX/2.5f) * Head_blendShape.funnel_3;
//				Head_blendShape.funnel_wide_3 = max(0,Head_controller.UprLip_cntr.translateX/2.5f) * Head_blendShape.funnel_4;
//				Head_blendShape.funnel_wide_4 = max(0,Head_controller.UprLip_cntr.translateX/2.5f) * Head_blendShape.funnel_6;
//				//------------------------------------------------------------------------------------------------------------------
//				//---------------------------------------------UprLip_L_cntr\UprLip_R_cntr------------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//----------------------------------------------UprLip_2_cntr-------------------------------------------------------
//				Head_blendShape.upr_lip_vol = max(0,-Head_controller.UprLip_2_cntr.translateZ/2.5f);
//				//------------------------------------------------------------------------------------------------------------------
//				//-----------------------------------------UprLip_L_2_cntr\UprLip_R_2_cntr------------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//--------------------------------------------LwrLip_L_cntr/LwrLip_R_cntr-------------------------------------------
//				float _close_l = max(0,Head_controller.LwrLip_L_cntr.translateY/2.5f);
//				float _close_r = max(0,Head_controller.LwrLip_R_cntr.translateY/2.5f);
//				Head_blendShape.lip_lock_open_1 = _close_l * Head_blendShape.jaw_open_1;
//				Head_blendShape.lip_lock_open_2 = _close_r * Head_blendShape.jaw_open_6;
//				Head_blendShape.lip_lock_drop_1 = hermite(0,0.0f,4,-4,Head_blendShape.lip_lock_open_1);
//				Head_blendShape.lip_lock_drop_2 = hermite(0,0.0f,4,-4,Head_blendShape.lip_lock_open_2);
//				Head_blendShape.lip_lock_fwd_1 = _close_l * Head_blendShape.jaw_fwd;
//				Head_blendShape.lip_lock_fwd_2 = _close_r * Head_blendShape.jaw_fwd;
//				Head_blendShape.lip_lock_back_1 = _close_l * Head_blendShape.jaw_back;
//				Head_blendShape.lip_lock_back_2 = _close_r * Head_blendShape.jaw_back;
//				Head_blendShape.lip_lock_l_1 = _close_l * Head_blendShape.jaw_l;
//				Head_blendShape.lip_lock_l_2 = _close_r * Head_blendShape.jaw_l;
//				Head_blendShape.lip_lock_r_1 = _close_l * Head_blendShape.jaw_r;
//				Head_blendShape.lip_lock_r_2 = _close_r * Head_blendShape.jaw_r;
//				Head_blendShape.sneer_close_1 = _close_l * Head_blendShape.sneer_2;
//				Head_blendShape.sneer_close_2 = _close_r * Head_blendShape.sneer_6;
//				Head_blendShape.disgust_close_1 = _close_l * Head_blendShape.disgust_2;
//				Head_blendShape.disgust_close_2 = _close_r * Head_blendShape.disgust_6;
//				Head_blendShape.nl_deep_close_1 = _close_l * Head_blendShape.nl_deep_2;
//				Head_blendShape.nl_deep_close_2 = _close_r * Head_blendShape.nl_deep_6;
//				Head_blendShape.funnel_close_1 = _close_l * max(Head_blendShape.funnel_1,Head_blendShape.funnel_3);
//				Head_blendShape.funnel_close_2 = _close_r * max(Head_blendShape.funnel_4,Head_blendShape.funnel_6);
//				Head_blendShape.stretch_close_1 = _close_l * Head_blendShape.stretch_1 ;
//				Head_blendShape.stretch_close_2 = _close_r * Head_blendShape.stretch_3 ;
//				Head_blendShape.smile_close_1 = _close_l * Head_blendShape.smile_2 ;
//				Head_blendShape.smile_close_2 = _close_r * Head_blendShape.smile_7 ;
//				//------------------------------------------------------------------------------------------------------------------
//				//-------------------------------------------------Nose_cntr--------------------------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//--------------------------------------------Nose_L_cntr\Nose_R_cntr-----------------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//--------------------------------------------Cheek_L_2_cntr\Cheek_R_2_cntr-----------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//--------------------------------------------Cheek_L_cntr\Cheek_R_cntr---------------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//---------------------------------------------LwrLid_L_cntr\LwrLid_R_cntr------------------------------------------
//				Head_blendShape.lwr_lid_up_1 = max(0,Head_controller.LwrLid_L_cntr.translateY/2.5f);
//				Head_blendShape.lwr_lid_up_2 = max(0,Head_controller.LwrLid_R_cntr.translateY/2.5f);
//				Head_blendShape.lwr_lid_dn_1 = max(0,-Head_controller.LwrLid_L_cntr.translateY/2.5f);
//				Head_blendShape.lwr_lid_dn_2 = max(0,-Head_controller.LwrLid_R_cntr.translateY/2.5f);
//				//------------------------------------------------------------------------------------------------------------------
//				//----------------------------------------------UprLid_L_cntr\UprLid_R_cntr-----------------------------------------
//				Head_blendShape.upr_lid_up_1 = max(0,Head_controller.UprLid_L_cntr.translateY/2.5f);
//				Head_blendShape.upr_lid_up_2 = max(0,Head_controller.UprLid_R_cntr.translateY/2.5f);
//				//------------------------------------------------------------------------------------------------------------------
//				//---------------------------------------------EyeSqz_L_cntr\EyeSqz_R_cntr------------------------------------------
//				Head_blendShape.eye_sqz_open_1 = Head_blendShape.eye_sqz_2 * (1-Head_blendShape.eye_blink_2);
//				Head_blendShape.eye_sqz_open_2 = Head_blendShape.eye_sqz_5 * (1-Head_blendShape.eye_blink_4);
//				//------------------------------------------------------------------------------------------------------------------
//				//-----------------------------------------------------Brow_cntr----------------------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//----------------------------------------------BrowIn_L_cntr\BrowIn_R_cntr-----------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//-----------------------------------------------BrowOut_L_cntr\BrowOut_R_cntr--------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//-------------------------------------------------------Head_cntr--------------------------------------------------
//				Head_blendShape.head_skin_slide = max(0,Head_controller.Head_cntr.translateY/2.5f);
//				//------------------------------------------------------------------------------------------------------------------
//				//---------------------------------------------------------Drops----------------------------------------------------
//				Head_blendShape.neck_tns_dr_1 = Head_blendShape.jaw_open_1 * Head_blendShape.neck_tension_1;
//				Head_blendShape.neck_tns_dr_2 = Head_blendShape.jaw_open_6 * Head_blendShape.neck_tension_2;
//				Head_blendShape.smile_drop_1 = Head_blendShape.smile_3 * Head_blendShape.jaw_open_2;
//				Head_blendShape.smile_drop_2 = Head_blendShape.smile_8 * Head_blendShape.jaw_open_7;
//				Head_blendShape.smile_close_d_1 = Head_blendShape.smile_close_1 * Head_blendShape.jaw_open_1;
//				Head_blendShape.smile_close_d_2 = Head_blendShape.smile_close_1 * Head_blendShape.jaw_open_6;
//				Head_blendShape.stretch_close_d_1 = Head_blendShape.stretch_close_1 * Head_blendShape.jaw_open_1;
//				Head_blendShape.stretch_close_d_2 = Head_blendShape.stretch_close_1 * Head_blendShape.jaw_open_6;
//				Head_blendShape.stretch_drop_1 = Head_blendShape.stretch_1 * Head_blendShape.jaw_open_2;
//				Head_blendShape.stretch_drop_2 = Head_blendShape.stretch_3 * Head_blendShape.jaw_open_7;
//				Head_blendShape.stretch_close_d_1 = Head_blendShape.stretch_drop_1 * Head_blendShape.stretch_close_1;
//				Head_blendShape.stretch_close_d_2 = Head_blendShape.stretch_drop_2 * Head_blendShape.stretch_close_2;
//				//------------------------------------------------------------------------------------------------------------------
//				//----------------------------------------------------------Eyes----------------------------------------------------
//				//------------------------------------------------------------------------------------------------------------------
//				//---------------------------------------------------------Eyes/Blinks----------------------------------------------
//				Head_blendShape.eye_blink_b_1 = hermite(0,0.0f,4,-4,max(0,Head_blendShape.eye_blink_2));
//				Head_blendShape.eye_blink_b_2 = hermite(0,0.0f,4,-4,max(0,Head_blendShape.eye_blink_4));
//				Head_blendShape.eye_look_dn_b_1 = hermite(0,0.0f,4,-4,max(0,Head_blendShape.eye_look_dn_2));
//				Head_blendShape.eye_look_dn_b_2 = hermite(0,0.0f,4,-4,max(0,Head_blendShape.eye_look_dn_4));
//				Head_blendShape.brows_up_blink_1 = Head_blendShape.brows_up_1 * max(Head_blendShape.eye_blink_2,Head_blendShape.eye_look_dn_2);
//				Head_blendShape.brows_up_blink_2 = Head_blendShape.brows_up_3 * max(Head_blendShape.eye_blink_2,Head_blendShape.eye_look_dn_2);
//				Head_blendShape.brows_dn_blink_1 = Head_blendShape.eye_blink_2 * Head_blendShape.brows_dn_in_2;
//				Head_blendShape.brows_dn_blink_2 = Head_blendShape.eye_blink_4 * Head_blendShape.brows_dn_in_4;
//				Head_blendShape.browssqzup_blink_1 = Head_blendShape.eye_blink_2 * Head_blendShape.brows_sqz_up_2;
//				Head_blendShape.browssqzup_blink_2 = Head_blendShape.eye_blink_4 * Head_blendShape.brows_sqz_up_4;
//				Head_blendShape.browssqz_blink_1 = Head_blendShape.eye_blink_2 * Head_blendShape.brows_sqz_2;
//				Head_blendShape.browssqz_blink_2 = Head_blendShape.eye_blink_4 * Head_blendShape.brows_sqz_4;
//				//------------------------------------------------------------------------------------------------------------------
//				//-------------------------------------------------------Correctives------------------------------------------------
//				Head_blendShape.nl_deep_l = max(0,Head_blendShape.nl_deep_2 - Head_blendShape.nl_deep_6);
//				Head_blendShape.nl_deep_r = max(0,Head_blendShape.nl_deep_6 - Head_blendShape.nl_deep_2);
//				Head_blendShape.nl_deep_l_c = min(Head_blendShape.nl_deep_l,Head_blendShape.nl_deep_close_1);
//				Head_blendShape.nl_deep_r_c = min(Head_blendShape.nl_deep_r,Head_blendShape.nl_deep_close_2);
//				Head_blendShape.smile_l = max(0,Head_blendShape.smile_2 - Head_blendShape.smile_7);
//				Head_blendShape.smile_r = max(0,Head_blendShape.smile_7 - Head_blendShape.smile_2);
//				Head_blendShape.smile_l_c = min(Head_blendShape.smile_l,Head_blendShape.smile_close_1);
//				Head_blendShape.smile_r_c = min(Head_blendShape.smile_r,Head_blendShape.smile_close_2);
//				Head_blendShape.stretch_l = max(0,Head_blendShape.stretch_2 - Head_blendShape.stretch_4);
//				Head_blendShape.stretch_r = max(0,Head_blendShape.stretch_4 - Head_blendShape.stretch_2);
//				Head_blendShape.stretch_l_c = min(Head_blendShape.stretch_l,Head_blendShape.stretch_close_1);
//				Head_blendShape.stretch_r_c = min(Head_blendShape.stretch_r,Head_blendShape.stretch_close_2);
//				Head_blendShape.eye_sqz_sneer_1 = Head_blendShape.eye_sqz_1 * Head_blendShape.sneer_3;
//				Head_blendShape.eye_sqz_sneer_2 = Head_blendShape.eye_sqz_2 * Head_blendShape.sneer_7;
//				Head_blendShape.lip_in_b_1 = hermite(0,0.0f,4,-4,max(0,Head_blendShape.lip_in_1));
//				Head_blendShape.lip_in_b_2 = hermite(0,0.0f,4,-4,max(0,Head_blendShape.lip_in_3));
//				Head_blendShape.lip_in_b_3 = hermite(0,0.0f,4,-4,max(0,Head_blendShape.lip_in_4));
//				Head_blendShape.lip_in_b_4 = hermite(0,0.0f,4,-4,max(0,Head_blendShape.lip_in_6));
//				//------------------------------------------------------------------------------------------------------------------
//			}
//			// this segment generated from 'Mouth_controller'
//			{
//				float _sneer_l = max(0,Head_controller.Nose_L_cntr.translateY/2.5f); 
//				float _disgust_l = max(0,Head_controller.UprLip_L_2_cntr.translateY/2.5f);
//				float _nl_deep_l = max(0,Head_controller.UprLip_L_2_cntr.translateX/2.5f);
//				float _corner_up_l = max(0,Head_controller.Crnr_L_cntr.translateY/2.5f);
//				float _smile_l = max(0,Head_controller.Crnr_L_2_cntr.translateX/2.5f);
//				float _dimple_l = max(0,-Head_controller.Crnr_L_2_cntr.translateZ/2.5f);
//				float _stretch_l = max(0,-Head_controller.Chin_L_cntr.translateY/2.5f);
//				float _frown_l = max(0,-Head_controller.Crnr_L_2_cntr.translateY/2.5f);
//				float _lwr_lip_dn_l = max(0,-Head_controller.LwrLip_L_cntr.translateY/2.5f);
//				float _chin_raise_up_l = max(0,Head_controller.Chin_cntr.translateY/2.5f);
//				float _chin_raise_dn_l = max(0,Head_controller.Chin_cntr.translateY/2.5f);
//				float _lwr_lip_l_l = max(0,Head_controller.LwrLip_cntr.translateX/2.5f);
//				float _lwr_lip_r_l = max(0,-Head_controller.LwrLip_cntr.translateX/2.5f);
//				float _funnel_up_l = max(0,Head_controller.UprLip_L_cntr.translateZ/2.5f);
//				float _funnel_dn_l = max(0,Head_controller.LwrLip_L_cntr.translateZ/2.5f);
//				float _funnel_l = max(_funnel_up_l,_funnel_dn_l);
//				float _o_up_l = max(0,-Head_controller.UprLip_L_cntr.translateX/2.5f);
//				float _o_dn_l = max(0,-Head_controller.LwrLip_L_cntr.translateX/2.5f);
//				float _o_l = max(_o_up_l,_o_dn_l);
//				float _pucker_up_l = max(0,-Head_controller.Crnr_L_cntr.translateX/2.5f);
//				float _pucker_dn_l = max(0,-Head_controller.Crnr_L_cntr.translateX/2.5f);
//				float _pucker_l = max(_pucker_up_l,_pucker_dn_l);
//				float _lip_in_up_l = max(0,-Head_controller.UprLip_L_cntr.translateZ/2.5f);
//				float _lip_in_dn_l = max(0,-Head_controller.LwrLip_L_cntr.translateZ/2.5f);
//				float _lip_in_l = max(_lip_in_up_l,_lip_in_dn_l);
//				float _tension_up_l = max(0,-Head_controller.UprLip_2_cntr.translateX/2.5f);
//				float _tension_dn_l = max(0,-Head_controller.UprLip_2_cntr.translateX/2.5f);
//				float _tension_l = max(_tension_up_l,_tension_dn_l);
//				float _press_up_l = max(0,-Head_controller.UprLip_2_cntr.translateY/2.5f);
//				float _press_dn_l = max(0,-Head_controller.UprLip_2_cntr.translateY/2.5f);
//				float _press_l = max(_press_up_l,_press_dn_l);
//				float _puff_up_l = max(0,Head_controller.Cheek_L_2_cntr.translateX/2.5f);
//				float _puff_dn_l = max(0,Head_controller.Cheek_L_2_cntr.translateX/2.5f);
//				float _puff_l = max(_puff_up_l,_puff_dn_l);
//				float _suck_up_l = max(0,-Head_controller.Cheek_L_2_cntr.translateX/2.5f);
//				float _suck_dn_l = max(0,-Head_controller.Cheek_L_2_cntr.translateX/2.5f);
//				float _suck_l = max(_suck_up_l,_suck_dn_l);
//				float _upr_lip_dn_up_l = max(0,-Head_controller.UprLip_L_cntr.translateY/2.5f);
//				float _upr_lip_dn_dn_l = max(0,-Head_controller.UprLip_L_cntr.translateY/2.5f);
//				float _lip_lock_open_l = max(0, Head_controller.LwrLip_L_cntr.translateY/2.5f)*max(0,-Head_controller.Jaw_cntr.translateY/2.5f);
//				float _sneer_r = max(0,Head_controller.Nose_R_cntr.translateY/2.5f);
//				float _disgust_r = max(0,Head_controller.UprLip_R_2_cntr.translateY/2.5f);
//				float _nl_deep_r = max(0,Head_controller.UprLip_R_2_cntr.translateX/2.5f);
//				float _corner_up_r = max(0,Head_controller.Crnr_R_cntr.translateY/2.5f);
//				float _smile_r = max(0,Head_controller.Crnr_R_2_cntr.translateX/2.5f);
//				float _dimple_r = max(0,-Head_controller.Crnr_R_2_cntr.translateZ/2.5f);
//				float _stretch_r = max(0,-Head_controller.Chin_R_cntr.translateY/2.5f);
//				float _frown_r = max(0,-Head_controller.Crnr_R_2_cntr.translateY/2.5f);
//				float _lwr_lip_dn_r = max(0,-Head_controller.LwrLip_R_cntr.translateY/2.5f);
//				float _chin_raise_up_r = max(0,Head_controller.Chin_cntr.translateY/2.5f);
//				float _chin_raise_dn_r = max(0,Head_controller.Chin_cntr.translateY/2.5f);
//				float _lwr_lip_l_r = max(0,Head_controller.LwrLip_cntr.translateX/2.5f);
//				float _lwr_lip_r_r = max(0,-Head_controller.LwrLip_cntr.translateX/2.5f);
//				float _funnel_up_r = max(0,Head_controller.UprLip_R_cntr.translateZ/2.5f);
//				float _funnel_dn_r = max(0,Head_controller.LwrLip_R_cntr.translateZ/2.5f);
//				float _funnel_r = max(_funnel_up_r,_funnel_dn_r);
//				float _o_up_r = max(0,-Head_controller.UprLip_R_cntr.translateX/2.5f);
//				float _o_dn_r = max(0,-Head_controller.LwrLip_R_cntr.translateX/2.5f);
//				float _o_r = max(_o_up_r,_o_dn_r);
//				float _pucker_up_r = max(0,-Head_controller.Crnr_R_cntr.translateX/2.5f);
//				float _pucker_dn_r = max(0,-Head_controller.Crnr_R_cntr.translateX/2.5f);
//				float _pucker_r = max(_pucker_up_r,_pucker_dn_r);
//				float _lip_in_up_r = max(0,-Head_controller.UprLip_R_cntr.translateZ/2.5f);
//				float _lip_in_dn_r = max(0,-Head_controller.LwrLip_R_cntr.translateZ/2.5f);
//				float _lip_in_r = max(_lip_in_up_r,_lip_in_dn_r);
//				float _tension_up_r = max(0,-Head_controller.UprLip_2_cntr.translateX/2.5f);
//				float _tension_dn_r = max(0,-Head_controller.UprLip_2_cntr.translateX/2.5f);
//				float _tension_r = max(_tension_up_r,_tension_dn_r);
//				float _press_up_r = max(0,-Head_controller.UprLip_2_cntr.translateY/2.5f);
//				float _press_dn_r = max(0,-Head_controller.UprLip_2_cntr.translateY/2.5f);
//				float _press_r = max(_press_up_r,_press_dn_r);
//				float _puff_up_r = max(0,Head_controller.Cheek_R_2_cntr.translateX/2.5f);
//				float _puff_dn_r = max(0,Head_controller.Cheek_R_2_cntr.translateX/2.5f);
//				float _puff_r = max(_puff_up_r,_puff_dn_r);
//				float _suck_up_r = max(0,-Head_controller.Cheek_R_2_cntr.translateX/2.5f);
//				float _suck_dn_r = max(0,-Head_controller.Cheek_R_2_cntr.translateX/2.5f);
//				float _suck_r = max(_suck_up_r,_suck_dn_r);
//				float _upr_lip_dn_up_r = max(0,-Head_controller.UprLip_R_cntr.translateY/2.5f);
//				float _upr_lip_dn_dn_r = max(0,-Head_controller.UprLip_R_cntr.translateY/2.5f);
//				float _lip_lock_open_r = max(0, Head_controller.LwrLip_R_cntr.translateY/2.5f)*max(0,-Head_controller.Jaw_cntr.translateY/2.5f);
//				float _jaw_drop = 0;//max(0,-Head_controller.Jaw_cntr.ty);
//				float _jaw_open = max(0,-Head_controller.Jaw_cntr.translateY/2.5f);
//				float _jaw_l = max(0,Head_controller.Jaw_cntr.translateX/2.5f);
//				float _jaw_r = max(0,-Head_controller.Jaw_cntr.translateX/2.5f);
//				float _jaw_back = max(0,-Head_controller.Jaw_cntr.translateZ/2.5f);
//				float _jaw_fwd = max(0,Head_controller.Jaw_cntr.translateZ/2.5f);
//				float _mouth_l = max(0,Head_controller.Mouth_cntr.translateX/2.5f);
//				float _mouth_r = max(0,-Head_controller.Mouth_cntr.translateX/2.5f);
//				float _nose_close = max(0,-Head_controller.Nose_cntr.translateX/2.5f);
//				float _nose_open = max(0,Head_controller.Nose_cntr.translateX/2.5f);
//				//Head_blendShape.jaw_open = _jaw_open;
//				Head_blendShape.jaw_l = _jaw_l;
//				Head_blendShape.jaw_r = _jaw_r;
//				Head_blendShape.mouth_l = _mouth_l;
//				Head_blendShape.mouth_r = _mouth_r;
//				Head_blendShape.nose_close = _nose_close;
//				Head_blendShape.nose_open = _nose_open;
//				Head_blendShape.sneer_1 = _sneer_l*(1-_nl_deep_l*0.5583f)*(1-_smile_l*0.975f)*(1-_dimple_l*0.0625f)*(1-_funnel_dn_l*0.1958f)*(1-_o_dn_l*0.4667f)*(1-_pucker_dn_l*0.3417f)*(1-_tension_dn_l*0.1f)*(1-_lip_in_dn_l*0.625f)*(1-_upr_lip_dn_dn_l*0.3375f)*(1-_mouth_l*0.2583f)*(1-_mouth_r*0.2333f)*(1-_jaw_open*0.1875f);
//				Head_blendShape.sneer_2 = _sneer_l*(1-_disgust_l*0.4f)*(1-_nl_deep_l*0.6292f)*(1-_smile_l*0.6875f)*(1-_dimple_l*0.025f)*(1-_frown_l*0.1333f)*(1-_funnel_up_l*0.1917f)*(1-_o_up_l*0.4417f)*(1-_pucker_up_l*0.2875f)*(1-_tension_up_l*0.2f)*(1-_press_up_l*0.1f)*(1-_lip_in_up_l*0.8667f)*(1-_upr_lip_dn_up_l*0.725f)*(1-_mouth_r*0.1208f)*(1-_jaw_open*0.1208f);
//				Head_blendShape.sneer_3 = _sneer_l*(1-_nl_deep_l*0.2583f)*(1-_smile_l*0.1542f)*(1-_o_up_l*0.1667f)*(1-_pucker_up_l*0.2375f)*(1-_tension_up_l*0.1292f)*(1-_lip_in_up_l*0.3542f)*(1-_upr_lip_dn_up_l*0.3917f)*(1-_mouth_l*0.1083f)*(1-_lip_lock_open_l*0.4292f);
//				Head_blendShape.sneer_4 = _sneer_l*(1-_disgust_l*0.3125f)*(1-_nl_deep_l*0.2625f)*(1-_smile_l*0.4833f)*(1-_dimple_l*0.2208f)*(1-_lip_in_up_l*0.3167f)*(1-_upr_lip_dn_up_l*0.425f)*(1-_mouth_l*0.1292f)*(1-_jaw_open*0.4167f)*(1-_lip_lock_open_l*0.4625f);
//				Head_blendShape.sneer_5 = _sneer_r*(1-_nl_deep_r*0.5583f)*(1-_smile_r*0.975f)*(1-_dimple_r*0.0625f)*(1-_funnel_dn_r*0.1958f)*(1-_o_dn_r*0.4667f)*(1-_pucker_dn_r*0.3417f)*(1-_tension_dn_r*0.1f)*(1-_lip_in_dn_r*0.625f)*(1-_upr_lip_dn_dn_r*0.3375f)*(1-_mouth_l*0.2583f)*(1-_mouth_r*0.2333f)*(1-_jaw_open*0.1875f);
//				Head_blendShape.sneer_6 = _sneer_r*(1-_disgust_r*0.4f)*(1-_nl_deep_r*0.6292f)*(1-_smile_r*0.6875f)*(1-_dimple_r*0.025f)*(1-_frown_r*0.1333f)*(1-_funnel_up_r*0.1917f)*(1-_o_up_r*0.4417f)*(1-_pucker_up_r*0.2875f)*(1-_tension_up_r*0.2f)*(1-_press_up_r*0.1f)*(1-_lip_in_up_r*0.8667f)*(1-_upr_lip_dn_up_r*0.725f)*(1-_mouth_r*0.1208f)*(1-_jaw_open*0.1208f);
//				Head_blendShape.sneer_7 = _sneer_r*(1-_nl_deep_r*0.2583f)*(1-_smile_r*0.1542f)*(1-_o_up_r*0.1667f)*(1-_pucker_up_r*0.2375f)*(1-_tension_up_r*0.1292f)*(1-_lip_in_up_r*0.3542f)*(1-_upr_lip_dn_up_r*0.3917f)*(1-_mouth_l*0.1083f)*(1-_lip_lock_open_r*0.4292f);
//				Head_blendShape.sneer_8 = _sneer_r*(1-_disgust_r*0.3125f)*(1-_nl_deep_r*0.2625f)*(1-_smile_r*0.4833f)*(1-_dimple_r*0.2208f)*(1-_lip_in_up_r*0.3167f)*(1-_upr_lip_dn_up_r*0.425f)*(1-_mouth_l*0.1292f)*(1-_jaw_open*0.4167f)*(1-_lip_lock_open_r*0.4625f);
//				Head_blendShape.disgust_1 = _disgust_l*(1-_sneer_l*1.0f)*(1-_corner_up_l*0.1167f)*(1-_smile_l*0.5667f)*(1-_dimple_l*0.0875f)*(1-_frown_l*0.3458f)*(1-_lwr_lip_l_l*0.2f)*(1-_lwr_lip_r_l*0.1917f)*(1-_funnel_dn_l*0.2167f)*(1-_o_dn_l*0.3083f)*(1-_pucker_dn_l*0.1708f)*(1-_tension_dn_l*0.2208f)*(1-_press_dn_l*0.2292f)*(1-_lip_in_dn_l*0.6167f)*(1-_upr_lip_dn_dn_l*0.2792f)*(1-_mouth_l*0.2583f)*(1-_mouth_r*0.6167f)*(1-_jaw_open*0.1792f)*(1-_lip_lock_open_l*0.4042f);
//				Head_blendShape.disgust_2 = _disgust_l*(1-_sneer_l*0.4708f)*(1-_nl_deep_l*0.15f)*(1-_smile_l*0.4292f)*(1-_frown_l*0.4208f)*(1-_funnel_up_l*0.4625f)*(1-_o_up_l*0.5833f)*(1-_pucker_up_l*0.2458f)*(1-_tension_up_l*0.2292f)*(1-_press_up_l*0.0875f)*(1-_lip_in_up_l*0.7792f)*(1-_upr_lip_dn_up_l*0.5583f)*(1-_mouth_l*0.2875f)*(1-_mouth_r*0.2292f)*(1-_jaw_open*0.1042f)*(1-_lip_lock_open_l*0.4208f);
//				Head_blendShape.disgust_3 = _disgust_l*(1-_sneer_l*0.875f)*(1-_nl_deep_l*0.1333f)*(1-_smile_l*0.2833f)*(1-_frown_l*0.1792f)*(1-_funnel_up_l*0.1292f)*(1-_o_up_l*0.1292f)*(1-_pucker_up_l*0.1083f)*(1-_lip_in_up_l*0.2792f)*(1-_mouth_l*0.4292f)*(1-_mouth_r*0.5167f)*(1-_jaw_open*0.1167f)*(1-_lip_lock_open_l*0.1625f);
//				Head_blendShape.disgust_4 = _disgust_l*(1-_sneer_l*0.4375f)*(1-_nl_deep_l*0.2167f)*(1-_corner_up_l*0.3042f)*(1-_smile_l*0.5083f)*(1-_dimple_l*0.3208f)*(1-_frown_l*0.3375f)*(1-_funnel_up_l*0.2667f)*(1-_o_up_l*0.1292f)*(1-_pucker_up_l*0.1583f)*(1-_lip_in_up_l*0.3708f)*(1-_mouth_l*0.4542f)*(1-_mouth_r*0.2417f)*(1-_jaw_open*0.2667f)*(1-_lip_lock_open_l*0.3875f);
//				Head_blendShape.disgust_5 = _disgust_r*(1-_sneer_r*1.0f)*(1-_corner_up_r*0.1167f)*(1-_smile_r*0.5667f)*(1-_dimple_r*0.0875f)*(1-_frown_r*0.3458f)*(1-_lwr_lip_l_r*0.2f)*(1-_lwr_lip_r_r*0.1917f)*(1-_funnel_dn_r*0.2167f)*(1-_o_dn_r*0.3083f)*(1-_pucker_dn_r*0.1708f)*(1-_tension_dn_r*0.2208f)*(1-_press_dn_r*0.2292f)*(1-_lip_in_dn_r*0.6167f)*(1-_upr_lip_dn_dn_r*0.2792f)*(1-_mouth_l*0.2583f)*(1-_mouth_r*0.6167f)*(1-_jaw_open*0.1792f)*(1-_lip_lock_open_r*0.4042f);
//				Head_blendShape.disgust_6 = _disgust_r*(1-_sneer_r*0.4708f)*(1-_nl_deep_r*0.15f)*(1-_smile_r*0.4292f)*(1-_frown_r*0.4208f)*(1-_funnel_up_r*0.4625f)*(1-_o_up_r*0.5833f)*(1-_pucker_up_r*0.2458f)*(1-_tension_up_r*0.2292f)*(1-_press_up_r*0.0875f)*(1-_lip_in_up_r*0.7792f)*(1-_upr_lip_dn_up_r*0.5583f)*(1-_mouth_l*0.2875f)*(1-_mouth_r*0.2292f)*(1-_jaw_open*0.1042f)*(1-_lip_lock_open_r*0.4208f);
//				Head_blendShape.disgust_7 = _disgust_r*(1-_sneer_r*0.875f)*(1-_nl_deep_r*0.1333f)*(1-_smile_r*0.2833f)*(1-_frown_r*0.1792f)*(1-_funnel_up_r*0.1292f)*(1-_o_up_r*0.1292f)*(1-_pucker_up_r*0.1083f)*(1-_lip_in_up_r*0.2792f)*(1-_mouth_l*0.4292f)*(1-_mouth_r*0.5167f)*(1-_jaw_open*0.1167f)*(1-_lip_lock_open_r*0.1625f);
//				Head_blendShape.disgust_8 = _disgust_r*(1-_sneer_r*0.4375f)*(1-_nl_deep_r*0.2167f)*(1-_corner_up_r*0.3042f)*(1-_smile_r*0.5083f)*(1-_dimple_r*0.3208f)*(1-_frown_r*0.3375f)*(1-_funnel_up_r*0.2667f)*(1-_o_up_r*0.1292f)*(1-_pucker_up_r*0.1583f)*(1-_lip_in_up_r*0.3708f)*(1-_mouth_l*0.4542f)*(1-_mouth_r*0.2417f)*(1-_jaw_open*0.2667f)*(1-_lip_lock_open_r*0.3875f);
//				Head_blendShape.nl_deep_1 = _nl_deep_l*(1-_disgust_l*0.6583f)*(1-_smile_l*0.75f)*(1-_stretch_l*0.2583f)*(1-_frown_l*0.0667f)*(1-_lwr_lip_l_l*0.2958f)*(1-_chin_raise_dn_l*0.1083f)*(1-_funnel_dn_l*0.5125f)*(1-_o_dn_l*0.7f)*(1-_pucker_dn_l*0.8125f)*(1-_tension_dn_l*0.0833f)*(1-_press_dn_l*0.2042f)*(1-_upr_lip_dn_dn_l*0.0375f)*(1-_jaw_open*0.0333f);
//				Head_blendShape.nl_deep_2 = _nl_deep_l*(1-_disgust_l*0.5667f)*(1-_smile_l*0.4583f)*(1-_dimple_l*0.1042f)*(1-_stretch_l*0.1875f)*(1-_frown_l*0.2917f)*(1-_lwr_lip_l_l*0.0208f)*(1-_chin_raise_up_l*0.125f)*(1-_funnel_up_l*0.4125f)*(1-_o_up_l*0.5792f)*(1-_pucker_up_l*0.525f)*(1-_tension_up_l*0.2375f)*(1-_press_up_l*0.0875f)*(1-_lip_in_up_l*0.525f)*(1-_upr_lip_dn_up_l*0.2042f)*(1-_jaw_open*0.1583f);
//				Head_blendShape.nl_deep_3 = _nl_deep_l*(1-_disgust_l*0.45f)*(1-_funnel_up_l*0.475f)*(1-_o_up_l*0.3125f);
//				Head_blendShape.nl_deep_4 = _nl_deep_l*(1-_sneer_l*0.2333f)*(1-_disgust_l*0.375f)*(1-_corner_up_l*0.375f)*(1-_smile_l*0.4792f)*(1-_jaw_open*0.1375f);
//				Head_blendShape.nl_deep_5 = _nl_deep_r*(1-_disgust_r*0.6583f)*(1-_smile_r*0.75f)*(1-_stretch_r*0.2583f)*(1-_frown_r*0.0667f)*(1-_lwr_lip_l_r*0.2958f)*(1-_chin_raise_dn_r*0.1083f)*(1-_funnel_dn_r*0.5125f)*(1-_o_dn_r*0.7f)*(1-_pucker_dn_r*0.8125f)*(1-_tension_dn_r*0.0833f)*(1-_press_dn_r*0.2042f)*(1-_upr_lip_dn_dn_r*0.0375f)*(1-_jaw_open*0.0333f);
//				Head_blendShape.nl_deep_6 = _nl_deep_r*(1-_disgust_r*0.5667f)*(1-_smile_r*0.4583f)*(1-_dimple_r*0.1042f)*(1-_stretch_r*0.1875f)*(1-_frown_r*0.2917f)*(1-_lwr_lip_l_r*0.0208f)*(1-_chin_raise_up_r*0.125f)*(1-_funnel_up_r*0.4125f)*(1-_o_up_r*0.5792f)*(1-_pucker_up_r*0.525f)*(1-_tension_up_r*0.2375f)*(1-_press_up_r*0.0875f)*(1-_lip_in_up_r*0.525f)*(1-_upr_lip_dn_up_r*0.2042f)*(1-_jaw_open*0.1583f);
//				Head_blendShape.nl_deep_7 = _nl_deep_r*(1-_disgust_r*0.45f)*(1-_funnel_up_r*0.475f)*(1-_o_up_r*0.3125f);
//				Head_blendShape.nl_deep_8 = _nl_deep_r*(1-_sneer_r*0.2333f)*(1-_disgust_r*0.375f)*(1-_corner_up_r*0.375f)*(1-_smile_r*0.4792f)*(1-_jaw_open*0.1375f);
//				Head_blendShape.corner_up_1 = _corner_up_l*(1-_sneer_l*0.3792f)*(1-_smile_l*1.0f)*(1-_dimple_l*0.1167f)*(1-_stretch_l*0.4542f)*(1-_frown_l*0.4f)*(1-_o_dn_l*0.5583f)*(1-_pucker_dn_l*0.1625f)*(1-_lip_in_dn_l*0.4417f)*(1-_mouth_l*0.3042f)*(1-_mouth_r*0.2083f)*(1-_jaw_open*0.3875f)*(1-_lip_lock_open_l*0.1917f);
//				Head_blendShape.corner_up_2 = _corner_up_l*(1-_sneer_l*0.3167f)*(1-_disgust_l*0.1333f)*(1-_nl_deep_l*0.1792f)*(1-_smile_l*0.6708f)*(1-_dimple_l*0.2875f)*(1-_stretch_l*0.2f)*(1-_funnel_dn_l*0.325f)*(1-_o_dn_l*0.4083f)*(1-_pucker_dn_l*0.6167f)*(1-_tension_dn_l*0.2917f)*(1-_lip_in_dn_l*0.6208f)*(1-_mouth_l*0.3333f)*(1-_mouth_r*0.4542f)*(1-_jaw_open*0.2125f)*(1-_lip_lock_open_l*0.2625f);
//				Head_blendShape.corner_up_3 = _corner_up_l*(1-_sneer_l*0.2375f)*(1-_disgust_l*0.1625f)*(1-_nl_deep_l*0.1458f)*(1-_smile_l*0.6292f)*(1-_dimple_l*0.1958f)*(1-_stretch_l*0.3417f)*(1-_o_up_l*0.4917f)*(1-_tension_up_l*0.1917f)*(1-_lip_in_up_l*0.6542f)*(1-_mouth_l*0.3583f)*(1-_mouth_r*0.5542f)*(1-_jaw_open*0.2958f)*(1-_lip_lock_open_l*0.4625f);
//				Head_blendShape.corner_up_4 = _corner_up_l*(1-_disgust_l*0.275f)*(1-_smile_l*0.0458f)*(1-_tension_up_l*0.1542f)*(1-_lip_in_up_l*0.4833f)*(1-_mouth_l*0.2333f)*(1-_mouth_r*0.3f)*(1-_jaw_open*0.2875f)*(1-_lip_lock_open_l*0.2375f);
//				Head_blendShape.corner_up_5 = _corner_up_l*(1-_sneer_l*0.3208f)*(1-_disgust_l*0.2583f)*(1-_nl_deep_l*0.3792f)*(1-_smile_l*0.5292f)*(1-_stretch_l*0.3792f)*(1-_frown_l*0.4208f)*(1-_tension_up_l*0.3042f)*(1-_lip_in_up_l*0.3625f)*(1-_mouth_l*0.35f)*(1-_mouth_r*0.5125f)*(1-_jaw_open*0.3958f)*(1-_lip_lock_open_l*0.2333f);
//				Head_blendShape.corner_up_6 = _corner_up_r*(1-_sneer_r*0.3792f)*(1-_smile_r*1.0f)*(1-_dimple_r*0.1167f)*(1-_stretch_r*0.4542f)*(1-_frown_r*0.4f)*(1-_o_dn_r*0.5583f)*(1-_pucker_dn_r*0.1625f)*(1-_lip_in_dn_r*0.4417f)*(1-_mouth_l*0.3042f)*(1-_mouth_r*0.2083f)*(1-_jaw_open*0.3875f)*(1-_lip_lock_open_r*0.1917f);
//				Head_blendShape.corner_up_7 = _corner_up_r*(1-_sneer_r*0.3167f)*(1-_disgust_r*0.1333f)*(1-_nl_deep_r*0.1792f)*(1-_smile_r*0.6708f)*(1-_dimple_r*0.2875f)*(1-_stretch_r*0.2f)*(1-_funnel_dn_r*0.325f)*(1-_o_dn_r*0.4083f)*(1-_pucker_dn_r*0.6167f)*(1-_tension_dn_r*0.2917f)*(1-_lip_in_dn_r*0.6208f)*(1-_mouth_l*0.3333f)*(1-_mouth_r*0.4542f)*(1-_jaw_open*0.2125f)*(1-_lip_lock_open_r*0.2625f);
//				Head_blendShape.corner_up_8 = _corner_up_r*(1-_sneer_r*0.2375f)*(1-_disgust_r*0.1625f)*(1-_nl_deep_r*0.1458f)*(1-_smile_r*0.6292f)*(1-_dimple_r*0.1958f)*(1-_stretch_r*0.3417f)*(1-_o_up_r*0.4917f)*(1-_tension_up_r*0.1917f)*(1-_lip_in_up_r*0.6542f)*(1-_mouth_l*0.3583f)*(1-_mouth_r*0.5542f)*(1-_jaw_open*0.2958f)*(1-_lip_lock_open_r*0.4625f);
//				Head_blendShape.corner_up_9 = _corner_up_r*(1-_disgust_r*0.275f)*(1-_smile_r*0.0458f)*(1-_tension_up_r*0.1542f)*(1-_lip_in_up_r*0.4833f)*(1-_mouth_l*0.2333f)*(1-_mouth_r*0.3f)*(1-_jaw_open*0.2875f)*(1-_lip_lock_open_r*0.2375f);
//				Head_blendShape.corner_up_10 = _corner_up_r*(1-_sneer_r*0.3208f)*(1-_disgust_r*0.2583f)*(1-_nl_deep_r*0.3792f)*(1-_smile_r*0.5292f)*(1-_stretch_r*0.3792f)*(1-_frown_r*0.4208f)*(1-_tension_up_r*0.3042f)*(1-_lip_in_up_r*0.3625f)*(1-_mouth_l*0.35f)*(1-_mouth_r*0.5125f)*(1-_jaw_open*0.3958f)*(1-_lip_lock_open_r*0.2333f);
//				Head_blendShape.smile_1 = _smile_l*(1-_dimple_l*0.2208f)*(1-_stretch_l*0.4458f)*(1-_frown_l*0.1708f)*(1-_funnel_dn_l*0.3958f)*(1-_o_dn_l*0.4833f)*(1-_pucker_dn_l*0.2708f)*(1-_lip_in_dn_l*0.9f)*(1-_mouth_l*0.625f)*(1-_mouth_r*0.6333f)*(1-_jaw_open*0.3167f);
//				Head_blendShape.smile_2 = _smile_l*(1-_disgust_l*0.0833f)*(1-_dimple_l*0.2792f)*(1-_stretch_l*0.1583f)*(1-_frown_l*0.15f)*(1-_funnel_dn_l*0.4625f)*(1-_o_dn_l*0.625f)*(1-_pucker_dn_l*0.4f)*(1-_tension_dn_l*0.2083f)*(1-_lip_in_dn_l*0.6208f)*(1-_mouth_l*0.5667f)*(1-_mouth_r*0.4792f)*(1-_jaw_open*0.0667f);
//				Head_blendShape.smile_3 = _smile_l*(1-_disgust_l*0.1667f)*(1-_corner_up_l*0.0083f)*(1-_stretch_l*0.125f)*(1-_frown_l*0.0917f)*(1-_funnel_up_l*0.425f)*(1-_o_up_l*0.7f)*(1-_pucker_up_l*0.55f)*(1-_tension_up_l*0.0875f)*(1-_lip_in_up_l*0.6458f)*(1-_upr_lip_dn_up_l*0.2708f)*(1-_mouth_l*0.35f)*(1-_mouth_r*0.5167f)*(1-_jaw_open*0.1458f);
//				Head_blendShape.smile_4 = _smile_l*(1-_sneer_l*0.5667f)*(1-_disgust_l*0.5708f)*(1-_nl_deep_l*0.2583f)*(1-_stretch_l*0.1375f)*(1-_frown_l*0.3333f)*(1-_funnel_up_l*0.1083f)*(1-_o_up_l*0.1458f)*(1-_pucker_up_l*0.2292f)*(1-_tension_up_l*0.1625f)*(1-_lip_in_up_l*0.5583f)*(1-_upr_lip_dn_up_l*0.3083f)*(1-_mouth_l*0.2333f)*(1-_mouth_r*0.4292f)*(1-_jaw_open*0.2083f);
//				Head_blendShape.smile_5 = _smile_l*(1-_sneer_l*0.1875f)*(1-_disgust_l*0.2625f)*(1-_nl_deep_l*0.1125f)*(1-_corner_up_l*0.15f)*(1-_dimple_l*0.1792f)*(1-_stretch_l*0.1625f)*(1-_frown_l*0.2792f)*(1-_funnel_up_l*0.1792f)*(1-_o_up_l*0.475f)*(1-_pucker_up_l*0.4f)*(1-_tension_up_l*0.1f)*(1-_lip_in_up_l*0.4125f)*(1-_mouth_l*0.4167f)*(1-_mouth_r*0.4375f)*(1-_jaw_open*0.25f);
//				Head_blendShape.smile_6 = _smile_r*(1-_dimple_r*0.2208f)*(1-_stretch_r*0.4458f)*(1-_frown_r*0.1708f)*(1-_funnel_dn_r*0.3958f)*(1-_o_dn_r*0.4833f)*(1-_pucker_dn_r*0.2708f)*(1-_lip_in_dn_r*0.9f)*(1-_mouth_l*0.625f)*(1-_mouth_r*0.6333f)*(1-_jaw_open*0.3167f);
//				Head_blendShape.smile_7 = _smile_r*(1-_disgust_r*0.0833f)*(1-_dimple_r*0.2792f)*(1-_stretch_r*0.1583f)*(1-_frown_r*0.15f)*(1-_funnel_dn_r*0.4625f)*(1-_o_dn_r*0.625f)*(1-_pucker_dn_r*0.4f)*(1-_tension_dn_r*0.2083f)*(1-_lip_in_dn_r*0.6208f)*(1-_mouth_l*0.5667f)*(1-_mouth_r*0.4792f)*(1-_jaw_open*0.0667f);
//				Head_blendShape.smile_8 = _smile_r*(1-_disgust_r*0.1667f)*(1-_corner_up_r*0.0083f)*(1-_stretch_r*0.125f)*(1-_frown_r*0.0917f)*(1-_funnel_up_r*0.425f)*(1-_o_up_r*0.7f)*(1-_pucker_up_r*0.55f)*(1-_tension_up_r*0.0875f)*(1-_lip_in_up_r*0.6458f)*(1-_upr_lip_dn_up_r*0.2708f)*(1-_mouth_l*0.35f)*(1-_mouth_r*0.5167f)*(1-_jaw_open*0.1458f);
//				Head_blendShape.smile_9 = _smile_r*(1-_sneer_r*0.5667f)*(1-_disgust_r*0.5708f)*(1-_nl_deep_r*0.2583f)*(1-_stretch_r*0.1375f)*(1-_frown_r*0.3333f)*(1-_funnel_up_r*0.1083f)*(1-_o_up_r*0.1458f)*(1-_pucker_up_r*0.2292f)*(1-_tension_up_r*0.1625f)*(1-_lip_in_up_r*0.5583f)*(1-_upr_lip_dn_up_r*0.3083f)*(1-_mouth_l*0.2333f)*(1-_mouth_r*0.4292f)*(1-_jaw_open*0.2083f);
//				Head_blendShape.smile_10 = _smile_r*(1-_sneer_r*0.1875f)*(1-_disgust_r*0.2625f)*(1-_nl_deep_r*0.1125f)*(1-_corner_up_r*0.15f)*(1-_dimple_r*0.1792f)*(1-_stretch_r*0.1625f)*(1-_frown_r*0.2792f)*(1-_funnel_up_r*0.1792f)*(1-_o_up_r*0.475f)*(1-_pucker_up_r*0.4f)*(1-_tension_up_r*0.1f)*(1-_lip_in_up_r*0.4125f)*(1-_mouth_l*0.4167f)*(1-_mouth_r*0.4375f)*(1-_jaw_open*0.25f);
//				Head_blendShape.dimple_1 = _dimple_l*(1-_nl_deep_l*0.3167f)*(1-_corner_up_l*0.4375f)*(1-_smile_l*0.425f)*(1-_stretch_l*0.725f)*(1-_frown_l*0.2917f)*(1-_lwr_lip_l_l*0.4958f)*(1-_lwr_lip_r_l*0.1542f)*(1-_chin_raise_dn_l*0.5167f)*(1-_funnel_dn_l*0.3625f)*(1-_o_dn_l*0.2875f)*(1-_pucker_dn_l*0.4125f)*(1-_tension_dn_l*0.2083f)*(1-_lip_in_dn_l*0.4333f)*(1-_mouth_l*0.625f)*(1-_mouth_r*0.6292f)*(1-_jaw_open*0.3542f)*(1-_lip_lock_open_l*0.7625f);
//				Head_blendShape.dimple_2 = _dimple_l*(1-_disgust_l*0.1167f)*(1-_nl_deep_l*0.3792f)*(1-_corner_up_l*0.3167f)*(1-_smile_l*0.4083f)*(1-_stretch_l*0.2292f)*(1-_frown_l*0.1875f)*(1-_lwr_lip_l_l*0.1042f)*(1-_lwr_lip_r_l*0.2958f)*(1-_chin_raise_up_l*0.0542f)*(1-_funnel_up_l*0.4375f)*(1-_o_up_l*0.2875f)*(1-_pucker_up_l*0.3125f)*(1-_tension_up_l*0.2875f)*(1-_lip_in_up_l*0.3958f)*(1-_mouth_l*0.4083f)*(1-_mouth_r*0.3375f)*(1-_jaw_open*0.2208f)*(1-_lip_lock_open_l*0.3917f);
//				Head_blendShape.dimple_3 = _dimple_r*(1-_nl_deep_r*0.3167f)*(1-_corner_up_r*0.4375f)*(1-_smile_r*0.425f)*(1-_stretch_r*0.725f)*(1-_frown_r*0.2917f)*(1-_lwr_lip_l_r*0.4958f)*(1-_lwr_lip_r_r*0.1542f)*(1-_chin_raise_dn_r*0.5167f)*(1-_funnel_dn_r*0.3625f)*(1-_o_dn_r*0.2875f)*(1-_pucker_dn_r*0.4125f)*(1-_tension_dn_r*0.2083f)*(1-_lip_in_dn_r*0.4333f)*(1-_mouth_l*0.625f)*(1-_mouth_r*0.6292f)*(1-_jaw_open*0.3542f)*(1-_lip_lock_open_r*0.7625f);
//				Head_blendShape.dimple_4 = _dimple_r*(1-_disgust_r*0.1167f)*(1-_nl_deep_r*0.3792f)*(1-_corner_up_r*0.3167f)*(1-_smile_r*0.4083f)*(1-_stretch_r*0.2292f)*(1-_frown_r*0.1875f)*(1-_lwr_lip_l_r*0.1042f)*(1-_lwr_lip_r_r*0.2958f)*(1-_chin_raise_up_r*0.0542f)*(1-_funnel_up_r*0.4375f)*(1-_o_up_r*0.2875f)*(1-_pucker_up_r*0.3125f)*(1-_tension_up_r*0.2875f)*(1-_lip_in_up_r*0.3958f)*(1-_mouth_l*0.4083f)*(1-_mouth_r*0.3375f)*(1-_jaw_open*0.2208f)*(1-_lip_lock_open_r*0.3917f);
//				Head_blendShape.stretch_1 = _stretch_l*(1-_smile_l*0.1333f)*(1-_chin_raise_dn_l*0.4667f)*(1-_o_dn_l*0.3375f)*(1-_pucker_dn_l*0.3667f)*(1-_tension_dn_l*0.0958f)*(1-_lip_in_dn_l*0.3958f)*(1-_upr_lip_dn_dn_l*0.1f)*(1-_mouth_l*0.525f)*(1-_mouth_r*0.5708f)*(1-_jaw_open*0.3583f)*(1-_lip_lock_open_l*0.6042f);
//				Head_blendShape.stretch_2 = _stretch_l*(1-_disgust_l*0.1042f)*(1-_smile_l*0.2375f)*(1-_o_up_l*0.1875f)*(1-_pucker_up_l*0.1375f)*(1-_tension_up_l*0.1292f)*(1-_lip_in_up_l*0.2917f)*(1-_mouth_l*0.6125f)*(1-_mouth_r*0.7167f)*(1-_jaw_open*0.2708f)*(1-_lip_lock_open_l*0.2042f);
//				Head_blendShape.stretch_3 = _stretch_r*(1-_smile_r*0.1333f)*(1-_chin_raise_dn_r*0.4667f)*(1-_o_dn_r*0.3375f)*(1-_pucker_dn_r*0.3667f)*(1-_tension_dn_r*0.0958f)*(1-_lip_in_dn_r*0.3958f)*(1-_upr_lip_dn_dn_r*0.1f)*(1-_mouth_l*0.525f)*(1-_mouth_r*0.5708f)*(1-_jaw_open*0.3583f)*(1-_lip_lock_open_r*0.6042f);
//				Head_blendShape.stretch_4 = _stretch_r*(1-_disgust_r*0.1042f)*(1-_smile_r*0.2375f)*(1-_o_up_r*0.1875f)*(1-_pucker_up_r*0.1375f)*(1-_tension_up_r*0.1292f)*(1-_lip_in_up_r*0.2917f)*(1-_mouth_l*0.6125f)*(1-_mouth_r*0.7167f)*(1-_jaw_open*0.2708f)*(1-_lip_lock_open_r*0.2042f);
//				Head_blendShape.frown_1 = _frown_l*(1-_smile_l*0.2542f)*(1-_stretch_l*0.0875f)*(1-_lwr_lip_l_l*0.1417f)*(1-_funnel_dn_l*0.0333f)*(1-_pucker_dn_l*0.0417f)*(1-_mouth_l*0.6875f)*(1-_mouth_r*0.7458f)*(1-_jaw_open*0.4083f);
//				Head_blendShape.frown_2 = _frown_l*(1-_disgust_l*0.0167f)*(1-_nl_deep_l*0.2625f)*(1-_smile_l*0.2375f)*(1-_stretch_l*0.2167f)*(1-_funnel_up_l*0.0917f)*(1-_o_up_l*0.1542f)*(1-_pucker_up_l*0.1333f)*(1-_lip_in_up_l*0.3375f)*(1-_mouth_l*0.5417f)*(1-_mouth_r*0.5083f)*(1-_jaw_open*0.625f);
//				Head_blendShape.frown_3 = _frown_r*(1-_smile_r*0.2542f)*(1-_stretch_r*0.0875f)*(1-_lwr_lip_l_r*0.1417f)*(1-_funnel_dn_r*0.0333f)*(1-_pucker_dn_r*0.0417f)*(1-_mouth_l*0.6875f)*(1-_mouth_r*0.7458f)*(1-_jaw_open*0.4083f);
//				Head_blendShape.frown_4 = _frown_r*(1-_disgust_r*0.0167f)*(1-_nl_deep_r*0.2625f)*(1-_smile_r*0.2375f)*(1-_stretch_r*0.2167f)*(1-_funnel_up_r*0.0917f)*(1-_o_up_r*0.1542f)*(1-_pucker_up_r*0.1333f)*(1-_lip_in_up_r*0.3375f)*(1-_mouth_l*0.5417f)*(1-_mouth_r*0.5083f)*(1-_jaw_open*0.625f);
//				Head_blendShape.lwr_lip_dn_1 = _lwr_lip_dn_l*(1-_stretch_l*0.3875f)*(1-_funnel_dn_l*0.2958f)*(1-_o_dn_l*0.3208f)*(1-_pucker_dn_l*0.4042f)*(1-_lip_in_dn_l*0.5875f)*(1-_jaw_open*0.3667f);
//				Head_blendShape.lwr_lip_dn_2 = _lwr_lip_dn_l*(1-_stretch_l*0.5042f)*(1-_chin_raise_up_l*0.5583f)*(1-_funnel_up_l*0.3167f)*(1-_o_up_l*0.2875f)*(1-_pucker_up_l*0.3083f)*(1-_lip_in_up_l*0.4417f)*(1-_jaw_open*0.4083f);
//				Head_blendShape.lwr_lip_dn_3 = _lwr_lip_dn_r*(1-_stretch_r*0.3875f)*(1-_funnel_dn_r*0.2958f)*(1-_o_dn_r*0.3208f)*(1-_pucker_dn_r*0.4042f)*(1-_lip_in_dn_r*0.5875f)*(1-_jaw_open*0.3667f);
//				Head_blendShape.lwr_lip_dn_4 = _lwr_lip_dn_r*(1-_stretch_r*0.5042f)*(1-_chin_raise_up_r*0.5583f)*(1-_funnel_up_r*0.3167f)*(1-_o_up_r*0.2875f)*(1-_pucker_up_r*0.3083f)*(1-_lip_in_up_r*0.4417f)*(1-_jaw_open*0.4083f);
//				Head_blendShape.lwr_lip_l_1 = _lwr_lip_l_l*(1-_chin_raise_dn_l*0.1625f);
//				Head_blendShape.lwr_lip_l_2 = _lwr_lip_l_l*(1-_sneer_l*0.1208f)*(1-_disgust_l*0.1708f)*(1-_nl_deep_l*0.0417f)*(1-_funnel_up_l*0.4542f)*(1-_pucker_up_l*0.1083f);
//				Head_blendShape.lwr_lip_l_3 = _lwr_lip_l_r*(1-_chin_raise_dn_r*0.1625f);
//				Head_blendShape.lwr_lip_l_4 = _lwr_lip_l_r*(1-_sneer_r*0.1208f)*(1-_disgust_r*0.1708f)*(1-_nl_deep_r*0.0417f)*(1-_funnel_up_r*0.4542f)*(1-_pucker_up_r*0.1083f);
//				Head_blendShape.lwr_lip_r_1 = _lwr_lip_r_l*(1-_chin_raise_dn_l*0.1542f);
//				Head_blendShape.lwr_lip_r_2 = _lwr_lip_r_l*(1-_pucker_up_l*0.4458f);
//				Head_blendShape.lwr_lip_r_3 = _lwr_lip_r_r*(1-_chin_raise_dn_r*0.1542f);
//				Head_blendShape.lwr_lip_r_4 = _lwr_lip_r_r*(1-_pucker_up_r*0.4458f);
//				Head_blendShape.chin_raise_1 = _chin_raise_dn_l*(1-_sneer_l*0.2917f)*(1-_disgust_l*0.5542f)*(1-_smile_l*0.2542f)*(1-_stretch_l*0.1417f)*(1-_lwr_lip_l_l*0.1583f)*(1-_lwr_lip_r_l*0.1708f)*(1-_press_dn_l*0.0417f)*(1-_lip_in_dn_l*0.475f)*(1-_mouth_l*0.3208f)*(1-_mouth_r*0.7333f);
//				Head_blendShape.chin_raise_2 = _chin_raise_dn_l*(1-_disgust_l*0.5458f)*(1-_nl_deep_l*0.0583f)*(1-_smile_l*0.4917f)*(1-_lwr_lip_dn_l*0.4375f)*(1-_mouth_l*0.2917f)*(1-_mouth_r*0.85f)*(1-_jaw_open*0.0708f);
//				Head_blendShape.chin_raise_3 = _chin_raise_up_l*(1-_sneer_l*0.3375f)*(1-_disgust_l*0.8958f)*(1-_nl_deep_l*0.3708f)*(1-_smile_l*0.5542f)*(1-_stretch_l*0.3583f)*(1-_lwr_lip_dn_l*1.0f)*(1-_lwr_lip_l_l*0.225f)*(1-_lwr_lip_r_l*0.175f)*(1-_funnel_up_l*0.55f)*(1-_o_up_l*0.2167f)*(1-_pucker_up_l*0.3f)*(1-_lip_in_up_l*0.35f)*(1-_upr_lip_dn_up_l*0.4667f)*(1-_mouth_l*0.3875f)*(1-_mouth_r*0.6958f)*(1-_jaw_open*0.5917f);
//				Head_blendShape.chin_raise_4 = _chin_raise_dn_r*(1-_sneer_r*0.2917f)*(1-_disgust_r*0.5542f)*(1-_smile_r*0.2542f)*(1-_stretch_r*0.1417f)*(1-_lwr_lip_l_r*0.1583f)*(1-_lwr_lip_r_r*0.1708f)*(1-_press_dn_r*0.0417f)*(1-_lip_in_dn_r*0.475f)*(1-_mouth_l*0.3208f)*(1-_mouth_r*0.7333f);
//				Head_blendShape.chin_raise_5 = _chin_raise_dn_r*(1-_disgust_r*0.5458f)*(1-_nl_deep_r*0.0583f)*(1-_smile_r*0.4917f)*(1-_lwr_lip_dn_r*0.4375f)*(1-_mouth_l*0.2917f)*(1-_mouth_r*0.85f)*(1-_jaw_open*0.0708f);
//				Head_blendShape.chin_raise_6 = _chin_raise_up_r*(1-_sneer_r*0.3375f)*(1-_disgust_r*0.8958f)*(1-_nl_deep_r*0.3708f)*(1-_smile_r*0.5542f)*(1-_stretch_r*0.3583f)*(1-_lwr_lip_dn_r*1.0f)*(1-_lwr_lip_l_r*0.225f)*(1-_lwr_lip_r_r*0.175f)*(1-_funnel_up_r*0.55f)*(1-_o_up_r*0.2167f)*(1-_pucker_up_r*0.3f)*(1-_lip_in_up_r*0.35f)*(1-_upr_lip_dn_up_r*0.4667f)*(1-_mouth_l*0.3875f)*(1-_mouth_r*0.6958f)*(1-_jaw_open*0.5917f);
//				Head_blendShape.funnel_1 = _funnel_dn_l*(1-_disgust_l*0.1167f)*(1-_nl_deep_l*0.1333f)*(1-_corner_up_l*0.1958f)*(1-_smile_l*0.1875f)*(1-_dimple_l*0.0625f)*(1-_stretch_l*0.3167f)*(1-_frown_l*0.1458f)*(1-_lwr_lip_l_l*0.1458f)*(1-_lwr_lip_r_l*0.0875f)*(1-_o_dn_l*1.0f)*(1-_pucker_dn_l*1.0f)*(1-_tension_dn_l*0.1417f)*(1-_mouth_l*0.3708f)*(1-_mouth_r*0.2083f)*(1-_jaw_open*0.35f)*(1-_lip_lock_open_l*0.3292f);
//				Head_blendShape.funnel_2 = _funnel_dn_l*(1-_disgust_l*0.2708f)*(1-_nl_deep_l*0.1792f)*(1-_corner_up_l*0.4333f)*(1-_dimple_l*0.3417f)*(1-_stretch_l*0.4292f)*(1-_frown_l*0.3292f)*(1-_lwr_lip_l_l*0.3083f)*(1-_lwr_lip_r_l*0.3958f)*(1-_o_dn_l*1.0f)*(1-_pucker_dn_l*1.0f)*(1-_tension_dn_l*0.0917f)*(1-_mouth_l*0.4292f)*(1-_mouth_r*0.4167f)*(1-_jaw_open*0.2917f)*(1-_lip_lock_open_l*0.3917f);
//				Head_blendShape.funnel_3 = _funnel_up_l*(1-_sneer_l*0.15f)*(1-_smile_l*0.1417f)*(1-_dimple_l*0.1333f)*(1-_stretch_l*0.3792f)*(1-_frown_l*0.2042f)*(1-_o_up_l*1.0f)*(1-_pucker_up_l*1.0f)*(1-_tension_up_l*0.1208f)*(1-_upr_lip_dn_up_l*0.25f)*(1-_mouth_l*0.1083f)*(1-_mouth_r*0.175f)*(1-_jaw_open*0.3f)*(1-_lip_lock_open_l*0.6333f);
//				Head_blendShape.funnel_4 = _funnel_dn_r*(1-_disgust_r*0.1167f)*(1-_nl_deep_r*0.1333f)*(1-_corner_up_r*0.1958f)*(1-_smile_r*0.1875f)*(1-_dimple_r*0.0625f)*(1-_stretch_r*0.3167f)*(1-_frown_r*0.1458f)*(1-_lwr_lip_l_r*0.1458f)*(1-_lwr_lip_r_r*0.0875f)*(1-_o_dn_r*1.0f)*(1-_pucker_dn_r*1.0f)*(1-_tension_dn_r*0.1417f)*(1-_mouth_l*0.3708f)*(1-_mouth_r*0.2083f)*(1-_jaw_open*0.35f)*(1-_lip_lock_open_r*0.3292f);
//				Head_blendShape.funnel_5 = _funnel_dn_r*(1-_disgust_r*0.2708f)*(1-_nl_deep_r*0.1792f)*(1-_corner_up_r*0.4333f)*(1-_dimple_r*0.3417f)*(1-_stretch_r*0.4292f)*(1-_frown_r*0.3292f)*(1-_lwr_lip_l_r*0.3083f)*(1-_lwr_lip_r_r*0.3958f)*(1-_o_dn_r*1.0f)*(1-_pucker_dn_r*1.0f)*(1-_tension_dn_r*0.0917f)*(1-_mouth_l*0.4292f)*(1-_mouth_r*0.4167f)*(1-_jaw_open*0.2917f)*(1-_lip_lock_open_r*0.3917f);
//				Head_blendShape.funnel_6 = _funnel_up_r*(1-_sneer_r*0.15f)*(1-_smile_r*0.1417f)*(1-_dimple_r*0.1333f)*(1-_stretch_r*0.3792f)*(1-_frown_r*0.2042f)*(1-_o_up_r*1.0f)*(1-_pucker_up_r*1.0f)*(1-_tension_up_r*0.1208f)*(1-_upr_lip_dn_up_r*0.25f)*(1-_mouth_l*0.1083f)*(1-_mouth_r*0.175f)*(1-_jaw_open*0.3f)*(1-_lip_lock_open_r*0.6333f);
//				Head_blendShape.o_1 = _o_dn_l*(1-_smile_l*0.2f)*(1-_dimple_l*0.1208f)*(1-_stretch_l*0.3f)*(1-_frown_l*0.1417f)*(1-_lwr_lip_l_l*0.075f)*(1-_lwr_lip_r_l*0.0917f)*(1-_chin_raise_dn_l*0.2625f)*(1-_pucker_dn_l*1.0f)*(1-_tension_dn_l*0.2875f)*(1-_lip_in_dn_l*1.0f)*(1-_mouth_l*0.6625f)*(1-_mouth_r*0.3292f)*(1-_jaw_open*0.4375f)*(1-_lip_lock_open_l*0.2583f);
//				Head_blendShape.o_2 = _o_dn_l*(1-_sneer_l*0.125f)*(1-_disgust_l*0.1833f)*(1-_nl_deep_l*0.2458f)*(1-_smile_l*0.1625f)*(1-_dimple_l*0.175f)*(1-_stretch_l*0.1083f)*(1-_frown_l*0.3458f)*(1-_lwr_lip_l_l*0.1125f)*(1-_lwr_lip_r_l*0.3583f)*(1-_chin_raise_dn_l*0.0917f)*(1-_pucker_dn_l*1.0f)*(1-_tension_dn_l*0.225f)*(1-_lip_in_dn_l*1.0f)*(1-_mouth_l*0.6f)*(1-_mouth_r*0.475f)*(1-_jaw_open*0.35f)*(1-_lip_lock_open_l*0.1375f);
//				Head_blendShape.o_3 = _o_up_l*(1-_sneer_l*0.1333f)*(1-_smile_l*0.125f)*(1-_dimple_l*0.2833f)*(1-_stretch_l*0.4167f)*(1-_frown_l*0.2375f)*(1-_lwr_lip_l_l*0.1333f)*(1-_lwr_lip_r_l*0.1167f)*(1-_chin_raise_up_l*0.2625f)*(1-_pucker_up_l*1.0f)*(1-_tension_up_l*0.1708f)*(1-_lip_in_up_l*1.0f)*(1-_mouth_l*0.1542f)*(1-_mouth_r*0.65f)*(1-_jaw_open*0.4833f)*(1-_lip_lock_open_l*0.3083f);
//				Head_blendShape.o_4 = _o_dn_r*(1-_smile_r*0.2f)*(1-_dimple_r*0.1208f)*(1-_stretch_r*0.3f)*(1-_frown_r*0.1417f)*(1-_lwr_lip_l_r*0.075f)*(1-_lwr_lip_r_r*0.0917f)*(1-_chin_raise_dn_r*0.2625f)*(1-_pucker_dn_r*1.0f)*(1-_tension_dn_r*0.2875f)*(1-_lip_in_dn_r*1.0f)*(1-_mouth_l*0.6625f)*(1-_mouth_r*0.3292f)*(1-_jaw_open*0.4375f)*(1-_lip_lock_open_r*0.2583f);
//				Head_blendShape.o_5 = _o_dn_r*(1-_sneer_r*0.125f)*(1-_disgust_r*0.1833f)*(1-_nl_deep_r*0.2458f)*(1-_smile_r*0.1625f)*(1-_dimple_r*0.175f)*(1-_stretch_r*0.1083f)*(1-_frown_r*0.3458f)*(1-_lwr_lip_l_r*0.1125f)*(1-_lwr_lip_r_r*0.3583f)*(1-_chin_raise_dn_r*0.0917f)*(1-_pucker_dn_r*1.0f)*(1-_tension_dn_r*0.225f)*(1-_lip_in_dn_r*1.0f)*(1-_mouth_l*0.6f)*(1-_mouth_r*0.475f)*(1-_jaw_open*0.35f)*(1-_lip_lock_open_r*0.1375f);
//				Head_blendShape.o_6 = _o_up_r*(1-_sneer_r*0.1333f)*(1-_smile_r*0.125f)*(1-_dimple_r*0.2833f)*(1-_stretch_r*0.4167f)*(1-_frown_r*0.2375f)*(1-_lwr_lip_l_r*0.1333f)*(1-_lwr_lip_r_r*0.1167f)*(1-_chin_raise_up_r*0.2625f)*(1-_pucker_up_r*1.0f)*(1-_tension_up_r*0.1708f)*(1-_lip_in_up_r*1.0f)*(1-_mouth_l*0.1542f)*(1-_mouth_r*0.65f)*(1-_jaw_open*0.4833f)*(1-_lip_lock_open_r*0.3083f);
//				Head_blendShape.pucker_1 = _pucker_dn_l*(1-_disgust_l*0.1167f)*(1-_nl_deep_l*0.1458f)*(1-_corner_up_l*0.0667f)*(1-_dimple_l*0.2208f)*(1-_stretch_l*0.3208f)*(1-_frown_l*0.2167f)*(1-_lwr_lip_l_l*0.2083f)*(1-_lwr_lip_r_l*0.2667f)*(1-_chin_raise_dn_l*0.2292f)*(1-_tension_dn_l*0.2875f)*(1-_lip_in_dn_l*1.0f)*(1-_mouth_l*0.3958f)*(1-_mouth_r*0.425f)*(1-_jaw_open*0.1167f);
//				Head_blendShape.pucker_2 = _pucker_dn_l*(1-_sneer_l*0.3667f)*(1-_disgust_l*0.3542f)*(1-_nl_deep_l*0.3583f)*(1-_corner_up_l*0.2542f)*(1-_smile_l*0.1833f)*(1-_dimple_l*0.2167f)*(1-_stretch_l*0.4167f)*(1-_frown_l*0.4833f)*(1-_lwr_lip_l_l*0.4417f)*(1-_lwr_lip_r_l*0.3083f)*(1-_chin_raise_dn_l*0.225f)*(1-_lip_in_dn_l*1.0f)*(1-_upr_lip_dn_dn_l*0.3625f)*(1-_mouth_l*0.2792f)*(1-_mouth_r*0.3167f)*(1-_jaw_open*0.4542f);
//				Head_blendShape.pucker_3 = _pucker_up_l*(1-_sneer_l*0.125f)*(1-_disgust_l*0.1f)*(1-_nl_deep_l*0.2542f)*(1-_corner_up_l*0.0792f)*(1-_dimple_l*0.225f)*(1-_stretch_l*0.2292f)*(1-_frown_l*0.1833f)*(1-_lwr_lip_l_l*0.1375f)*(1-_lwr_lip_r_l*0.175f)*(1-_tension_up_l*0.1292f)*(1-_lip_in_up_l*1.0f)*(1-_upr_lip_dn_up_l*0.4042f)*(1-_mouth_l*0.3208f)*(1-_mouth_r*0.2833f)*(1-_jaw_open*0.3583f);
//				Head_blendShape.pucker_4 = _pucker_dn_r*(1-_disgust_r*0.1167f)*(1-_nl_deep_r*0.1458f)*(1-_corner_up_r*0.0667f)*(1-_dimple_r*0.2208f)*(1-_stretch_r*0.3208f)*(1-_frown_r*0.2167f)*(1-_lwr_lip_l_r*0.2083f)*(1-_lwr_lip_r_r*0.2667f)*(1-_chin_raise_dn_r*0.2292f)*(1-_tension_dn_r*0.2875f)*(1-_lip_in_dn_r*1.0f)*(1-_mouth_l*0.3958f)*(1-_mouth_r*0.425f)*(1-_jaw_open*0.1167f);
//				Head_blendShape.pucker_5 = _pucker_dn_r*(1-_sneer_r*0.3667f)*(1-_disgust_r*0.3542f)*(1-_nl_deep_r*0.3583f)*(1-_corner_up_r*0.2542f)*(1-_smile_r*0.1833f)*(1-_dimple_r*0.2167f)*(1-_stretch_r*0.4167f)*(1-_frown_r*0.4833f)*(1-_lwr_lip_l_r*0.4417f)*(1-_lwr_lip_r_r*0.3083f)*(1-_chin_raise_dn_r*0.225f)*(1-_lip_in_dn_r*1.0f)*(1-_upr_lip_dn_dn_r*0.3625f)*(1-_mouth_l*0.2792f)*(1-_mouth_r*0.3167f)*(1-_jaw_open*0.4542f);
//				Head_blendShape.pucker_6 = _pucker_up_r*(1-_sneer_r*0.125f)*(1-_disgust_r*0.1f)*(1-_nl_deep_r*0.2542f)*(1-_corner_up_r*0.0792f)*(1-_dimple_r*0.225f)*(1-_stretch_r*0.2292f)*(1-_frown_r*0.1833f)*(1-_lwr_lip_l_r*0.1375f)*(1-_lwr_lip_r_r*0.175f)*(1-_tension_up_r*0.1292f)*(1-_lip_in_up_r*1.0f)*(1-_upr_lip_dn_up_r*0.4042f)*(1-_mouth_l*0.3208f)*(1-_mouth_r*0.2833f)*(1-_jaw_open*0.3583f);
//				Head_blendShape.tension_1 = _tension_dn_l*(1-_smile_l*0.3625f)*(1-_o_dn_l*0.2792f)*(1-_pucker_dn_l*0.1625f)*(1-_lip_in_dn_l*0.3083f)*(1-_mouth_l*0.6667f)*(1-_mouth_r*0.6833f)*(1-_jaw_open*0.3083f);
//				Head_blendShape.tension_2 = _tension_dn_l*(1-_sneer_l*0.2542f)*(1-_disgust_l*0.2042f)*(1-_smile_l*0.6833f)*(1-_dimple_l*0.2958f)*(1-_stretch_l*0.6583f)*(1-_funnel_dn_l*0.0792f)*(1-_o_dn_l*0.4583f)*(1-_pucker_dn_l*0.7458f)*(1-_lip_in_dn_l*0.175f)*(1-_mouth_l*0.5375f)*(1-_mouth_r*0.6542f)*(1-_jaw_open*0.2667f);
//				Head_blendShape.tension_3 = _tension_up_l*(1-_sneer_l*0.0625f)*(1-_disgust_l*0.1875f)*(1-_smile_l*0.5f)*(1-_stretch_l*0.1083f)*(1-_o_up_l*0.1833f)*(1-_lip_in_up_l*0.1583f)*(1-_mouth_l*0.4875f)*(1-_mouth_r*0.3375f)*(1-_jaw_open*0.0833f);
//				Head_blendShape.tension_4 = _tension_dn_r*(1-_smile_r*0.3625f)*(1-_o_dn_r*0.2792f)*(1-_pucker_dn_r*0.1625f)*(1-_lip_in_dn_r*0.3083f)*(1-_mouth_l*0.6667f)*(1-_mouth_r*0.6833f)*(1-_jaw_open*0.3083f);
//				Head_blendShape.tension_5 = _tension_dn_r*(1-_sneer_r*0.2542f)*(1-_disgust_r*0.2042f)*(1-_smile_r*0.6833f)*(1-_dimple_r*0.2958f)*(1-_stretch_r*0.6583f)*(1-_funnel_dn_r*0.0792f)*(1-_o_dn_r*0.4583f)*(1-_pucker_dn_r*0.7458f)*(1-_lip_in_dn_r*0.175f)*(1-_mouth_l*0.5375f)*(1-_mouth_r*0.6542f)*(1-_jaw_open*0.2667f);
//				Head_blendShape.tension_6 = _tension_up_r*(1-_sneer_r*0.0625f)*(1-_disgust_r*0.1875f)*(1-_smile_r*0.5f)*(1-_stretch_r*0.1083f)*(1-_o_up_r*0.1833f)*(1-_lip_in_up_r*0.1583f)*(1-_mouth_l*0.4875f)*(1-_mouth_r*0.3375f)*(1-_jaw_open*0.0833f);
//				Head_blendShape.press_1 = _press_dn_l*(1-_smile_l*0.3458f);
//				Head_blendShape.press_2 = _press_dn_l*(1-_frown_l*0.2083f);
//				Head_blendShape.press_3 = _press_up_l*(1-_smile_l*0.3667f)*(1-_chin_raise_up_l*0.3292f);
//				Head_blendShape.press_4 = _press_dn_r*(1-_smile_r*0.3458f);
//				Head_blendShape.press_5 = _press_dn_r*(1-_frown_r*0.2083f);
//				Head_blendShape.press_6 = _press_up_r*(1-_smile_r*0.3667f)*(1-_chin_raise_up_r*0.3292f);
//				Head_blendShape.lip_in_1 = _lip_in_dn_l*(1-_dimple_l*0.225f)*(1-_stretch_l*0.0083f)*(1-_mouth_l*0.8f)*(1-_mouth_r*0.4333f)*(1-_lip_lock_open_l*0.5708f);
//				Head_blendShape.lip_in_2 = _lip_in_dn_l*(1-_sneer_l*0.1042f)*(1-_dimple_l*0.3667f)*(1-_stretch_l*0.2417f)*(1-_frown_l*0.05f)*(1-_mouth_l*0.4708f)*(1-_mouth_r*0.3917f)*(1-_jaw_open*0.0792f);
//				Head_blendShape.lip_in_3 = _lip_in_up_l*(1-_smile_l*0.0042f)*(1-_dimple_l*0.1958f)*(1-_frown_l*0.1583f)*(1-_tension_up_l*0.1208f)*(1-_mouth_l*0.425f)*(1-_mouth_r*0.4875f)*(1-_lip_lock_open_l*0.3792f);
//				Head_blendShape.lip_in_4 = _lip_in_dn_r*(1-_dimple_r*0.225f)*(1-_stretch_r*0.0083f)*(1-_mouth_l*0.8f)*(1-_mouth_r*0.4333f)*(1-_lip_lock_open_r*0.5708f);
//				Head_blendShape.lip_in_5 = _lip_in_dn_r*(1-_sneer_r*0.1042f)*(1-_dimple_r*0.3667f)*(1-_stretch_r*0.2417f)*(1-_frown_r*0.05f)*(1-_mouth_l*0.4708f)*(1-_mouth_r*0.3917f)*(1-_jaw_open*0.0792f);
//				Head_blendShape.lip_in_6 = _lip_in_up_r*(1-_smile_r*0.0042f)*(1-_dimple_r*0.1958f)*(1-_frown_r*0.1583f)*(1-_tension_up_r*0.1208f)*(1-_mouth_l*0.425f)*(1-_mouth_r*0.4875f)*(1-_lip_lock_open_r*0.3792f);
//				Head_blendShape.upr_lip_dn_1 = _upr_lip_dn_dn_l;
//				Head_blendShape.upr_lip_dn_2 = _upr_lip_dn_dn_l*(1-_sneer_l*0.2708f)*(1-_disgust_l*0.2542f)*(1-_nl_deep_l*0.0542f)*(1-_smile_l*0.6333f)*(1-_funnel_dn_l*0.4f)*(1-_o_dn_l*0.3f)*(1-_pucker_dn_l*0.275f);
//				Head_blendShape.upr_lip_dn_3 = _upr_lip_dn_up_l*(1-_sneer_l*0.6917f)*(1-_disgust_l*0.5667f)*(1-_nl_deep_l*0.4083f)*(1-_smile_l*0.5417f)*(1-_funnel_up_l*0.4583f)*(1-_o_up_l*0.3167f)*(1-_pucker_up_l*0.1833f);
//				Head_blendShape.upr_lip_dn_4 = _upr_lip_dn_dn_r;
//				Head_blendShape.upr_lip_dn_5 = _upr_lip_dn_dn_r*(1-_sneer_r*0.2708f)*(1-_disgust_r*0.2542f)*(1-_nl_deep_r*0.0542f)*(1-_smile_r*0.6333f)*(1-_funnel_dn_r*0.4f)*(1-_o_dn_r*0.3f)*(1-_pucker_dn_r*0.275f);
//				Head_blendShape.upr_lip_dn_6 = _upr_lip_dn_up_r*(1-_sneer_r*0.6917f)*(1-_disgust_r*0.5667f)*(1-_nl_deep_r*0.4083f)*(1-_smile_r*0.5417f)*(1-_funnel_up_r*0.4583f)*(1-_o_up_r*0.3167f)*(1-_pucker_up_r*0.1833f);
//				Head_blendShape.puff_1 = _puff_l;
//				Head_blendShape.puff_2 = _puff_r;
//				Head_blendShape.suck_1 = _suck_l;
//				Head_blendShape.suck_2 = _suck_r;
//				Head_blendShape.jaw_open_1 = _jaw_open*(1-_stretch_l*0.0958f)*(1-_funnel_dn_l*0.1125f)*(1-_o_dn_l*0.075f)*(1-_pucker_dn_l*0.1458f)*(1-_lip_in_dn_l*0.3125f);
//				Head_blendShape.jaw_open_2 = _jaw_open*(1-_dimple_l*0.075f)*(1-_stretch_l*0.1542f)*(1-_chin_raise_dn_l*0.0833f)*(1-_funnel_dn_l*0.1833f)*(1-_o_dn_l*0.0875f)*(1-_pucker_dn_l*0.1292f)*(1-_lip_in_dn_l*0.3417f)*(1-_puff_dn_l*0.1333f);
//				Head_blendShape.jaw_open_3 = _jaw_open*(1-_sneer_l*0.2083f)*(1-_dimple_l*0.0958f)*(1-_pucker_up_l*0.2875f);
//				Head_blendShape.jaw_open_4 = _jaw_open*(1-_smile_l*0.0667f);
//				Head_blendShape.jaw_open_5 = _jaw_open*(1-_smile_l*0.35f);
//				Head_blendShape.jaw_open_6 = _jaw_open*(1-_stretch_r*0.0958f)*(1-_funnel_dn_r*0.1125f)*(1-_o_dn_r*0.075f)*(1-_pucker_dn_r*0.1458f)*(1-_lip_in_dn_r*0.3125f);
//				Head_blendShape.jaw_open_7 = _jaw_open*(1-_dimple_r*0.075f)*(1-_stretch_r*0.1542f)*(1-_chin_raise_dn_r*0.0833f)*(1-_funnel_dn_r*0.1833f)*(1-_o_dn_r*0.0875f)*(1-_pucker_dn_r*0.1292f)*(1-_lip_in_dn_r*0.3417f)*(1-_puff_dn_r*0.1333f);
//				Head_blendShape.jaw_open_8 = _jaw_open*(1-_sneer_r*0.2083f)*(1-_dimple_r*0.0958f)*(1-_pucker_up_r*0.2875f);
//				Head_blendShape.jaw_open_9 = _jaw_open*(1-_smile_r*0.0667f);
//				Head_blendShape.jaw_open_10 = _jaw_open*(1-_smile_r*0.35f);
//				//Head_blendShape.jaw_open_11 = _jaw_open;
//			}
//			// this segment generated from 'Eyes_controller'
//			{
//				float _upr_lid_dn_l = max(0,-Head_controller.UprLid_L_cntr.translateY/2.5f); 
//				float _eye_blink_l = max(0,-Head_controller.UprLid_L_cntr.translateX/2.5f);
//				float _eye_squint_l = max(0,-Head_controller.EyeSqz_L_cntr.translateX/2.5f);
//				float _eye_sqz_l = max(0,-Head_controller.EyeSqz_L_cntr.translateY/2.5f);
//				float _cheek_raise_l = max(0,Head_controller.Cheek_L_cntr.translateY/2.5f);
//				float _brows_dn_in_l = max(0,-Head_controller.BrowIn_L_cntr.translateY/2.5f);
//				float _brows_dn_out_l = max(0,-Head_controller.BrowOut_L_cntr.translateY/2.5f);
//				float _brows_sqz_l = max(0,-Head_controller.BrowIn_L_cntr.translateX/2.5f);
//				float _brows_up_out_l = max(0,Head_controller.BrowOut_L_cntr.translateY/2.5f);
//				float _brows_up_in_l = max(0,Head_controller.BrowIn_L_cntr.translateY/2.5f);
//				float _upr_lid_dn_r = max(0,-Head_controller.UprLid_R_cntr.translateY/2.5f);
//				float _eye_blink_r = max(0,-Head_controller.UprLid_R_cntr.translateX/2.5f);
//				float _eye_squint_r = max(0,-Head_controller.EyeSqz_R_cntr.translateX/2.5f);
//				float _eye_sqz_r = max(0,-Head_controller.EyeSqz_R_cntr.translateY/2.5f);
//				float _cheek_raise_r = max(0,Head_controller.Cheek_R_cntr.translateY/2.5f);
//				float _brows_dn_in_r = max(0,-Head_controller.BrowIn_R_cntr.translateY/2.5f);
//				float _brows_dn_out_r = max(0,-Head_controller.BrowOut_R_cntr.translateY/2.5f);
//				float _brows_sqz_r = max(0,-Head_controller.BrowIn_R_cntr.translateX/2.5f);
//				float _brows_up_out_r = max(0,Head_controller.BrowOut_R_cntr.translateY/2.5f);
//				float _brows_up_in_r = max(0,Head_controller.BrowIn_R_cntr.translateY/2.5f);
//				float _brows_sqz_up = max(0,Head_controller.Brow_cntr.translateY/2.5f); 
//				float _eye_look_up_l = 0;
//				float _eye_look_dn_l = 0;
//				float _eye_look_in_l = 0;
//				float _eye_look_out_l = 0;
//				float _eye_look_up_r = 0;
//				float _eye_look_dn_r = 0;
//				float _eye_look_in_r = 0;
//				float _eye_look_out_r = 0;
//				float _sneer_l = max(0,Head_controller.Nose_L_cntr.translateY/2.5f);
//				float _disgust_l = max(0,Head_controller.UprLip_L_2_cntr.translateY/2.5f);
//				float _smile_l = max(0,Head_controller.Crnr_L_2_cntr.translateX/2.5f);
//				float _sneer_r = max(0,Head_controller.Nose_R_cntr.translateY/2.5f);
//				float _disgust_r = max(0,Head_controller.UprLip_R_2_cntr.translateY/2.5f);
//				float _smile_r = max(0,Head_controller.Crnr_R_2_cntr.translateX/2.5f);
//				if(Head_controller.LeftEye.rotateY >= 0.0f && Head_controller.LeftEye.rotateY<180.0f) 
//				_eye_look_out_l = Head_controller.LeftEye.rotateY/40.0f;
//				else
//				_eye_look_out_l = 0.0f;
//				if(Head_controller.LeftEye.rotateY > -180 && Head_controller.LeftEye.rotateY<-90)
//				_eye_look_in_l = (180.0f + Head_controller.LeftEye.rotateY)/40.0f ;
//				else if(Head_controller.LeftEye.rotateY < 0.0f && Head_controller.LeftEye.rotateY > -90)
//				_eye_look_in_l = -Head_controller.LeftEye.rotateY/40.0f ;
//				else if(Head_controller.LeftEye.rotateY > 180.0f)
//				_eye_look_in_l = (Head_controller.LeftEye.rotateY -180.0f)/40.0f;
//				else
//				_eye_look_in_l = 0.0f;
//				if(Head_controller.LeftEye.rotateX >= 0.0f && Head_controller.LeftEye.rotateX <= 90.0f)
//				_eye_look_dn_l = Head_controller.LeftEye.rotateX/30.0f;
//				else if(Head_controller.LeftEye.rotateX > 180.0f)
//				_eye_look_dn_l = (Head_controller.LeftEye.rotateX-180.0f)/30.0f;
//				else if(Head_controller.LeftEye.rotateX > 90.0f && Head_controller.LeftEye.rotateX > 180.0f )
//				_eye_look_dn_l = (180-Head_controller.LeftEye.rotateX)/30.0f;
//				else
//				_eye_look_dn_l = 0.0f;
//				if(Head_controller.LeftEye.rotateX <= 0.0f && Head_controller.LeftEye.rotateX >= -90.0f)
//				_eye_look_up_l = -Head_controller.LeftEye.rotateX/25.0f;
//				else if(Head_controller.LeftEye.rotateX < -180.0f)
//				_eye_look_up_l = (-Head_controller.LeftEye.rotateX-180.0f)/25.0f;
//				else
//				_eye_look_up_l = 0.0f;
//				//////////////////////
//				if(Head_controller.RightEye.rotateY >= 0.0f && Head_controller.RightEye.rotateY<180.0f)
//				_eye_look_in_r = Head_controller.RightEye.rotateY/40.0f;
//				else
//				_eye_look_in_r = 0.0f;
//				if(Head_controller.RightEye.rotateY > -180 && Head_controller.RightEye.rotateY<-90)
//				_eye_look_out_r = (180.0f + Head_controller.RightEye.rotateY)/40.0f ;
//				else if(Head_controller.RightEye.rotateY < 0.0f && Head_controller.RightEye.rotateY > -90)
//				_eye_look_out_r = -Head_controller.RightEye.rotateY/40.0f ;
//				else if(Head_controller.RightEye.rotateY > 180.0f)
//				_eye_look_out_r = (Head_controller.RightEye.rotateY -180.0f)/40.0f;
//				else
//				_eye_look_out_r = 0.0f;
//				if(Head_controller.RightEye.rotateX >= 0.0f && Head_controller.RightEye.rotateX <= 90.0f)
//				_eye_look_dn_r = Head_controller.RightEye.rotateX/30.0f;
//				else if(Head_controller.RightEye.rotateX > 180.0f)
//				_eye_look_dn_r = (Head_controller.RightEye.rotateX-180.0f)/30.0f;
//				else if(Head_controller.RightEye.rotateX > 90.0f && Head_controller.RightEye.rotateX > 180.0f )
//				_eye_look_dn_r = (180-Head_controller.RightEye.rotateX)/30.0f;
//				else
//				_eye_look_dn_r = 0.0f;
//				if(Head_controller.RightEye.rotateX <= 0.0f && Head_controller.RightEye.rotateX >= -90.0f)
//				_eye_look_up_r = -Head_controller.RightEye.rotateX/25.0f;
//				else if(Head_controller.RightEye.rotateX < -180.0f)
//				_eye_look_up_r = (-Head_controller.RightEye.rotateX-180.0f)/25.0f;
//				else
//				_eye_look_up_r = 0.0f;
//				//Head_blendShape.upr_lid_dn_1 = _upr_lid_dn_l*(1-_eye_squint_l*0.0917f)*(1-_cheek_raise_l*0.0792f)*(1-_eye_look_up_l*0.1f)*(1-_eye_look_dn_l*0.2542f);
//				//Head_blendShape.upr_lid_dn_2 = _upr_lid_dn_r*(1-_eye_squint_r*0.0917f)*(1-_cheek_raise_r*0.0792f)*(1-_eye_look_up_r*0.1f)*(1-_eye_look_dn_r*0.2542f);
//				Head_blendShape.eye_blink_1 = _eye_blink_l*(1-_eye_squint_l*0.6833f);
//				Head_blendShape.eye_blink_2 = max(_upr_lid_dn_l,_eye_blink_l)*(1-_eye_squint_l*0.1958f)*(1-_eye_look_up_l*0.1958f)*(1-_eye_look_dn_l*0.1583f)*(1-_sneer_l*0.0542f);
//				Head_blendShape.eye_blink_3 = _eye_blink_r*(1-_eye_squint_r*0.6833f);
//				Head_blendShape.eye_blink_4 = max(_upr_lid_dn_r,_eye_blink_r)*(1-_eye_squint_r*0.1958f)*(1-_eye_look_up_r*0.1958f)*(1-_eye_look_dn_r*0.1583f)*(1-_sneer_r*0.0542f);
//				Head_blendShape.eye_squint_1 = _eye_squint_l*(1-_upr_lid_dn_l*0.5958f)*(1-_eye_blink_l*0.2167f)*(1-_eye_sqz_l*0.1875f)*(1-_eye_look_dn_l*0.2583f);
//				Head_blendShape.eye_squint_2 = _eye_squint_l*(1-_eye_sqz_l*1.0f);
//				Head_blendShape.eye_squint_3 = _eye_squint_l*(1-_eye_sqz_l*1.0f)*(1-_eye_look_up_l*0.5292f)*(1-_eye_look_dn_l*0.3583f);
//				Head_blendShape.eye_squint_4 = _eye_squint_r*(1-_upr_lid_dn_r*0.5958f)*(1-_eye_blink_r*0.2167f)*(1-_eye_sqz_r*0.1875f)*(1-_eye_look_dn_r*0.2583f);
//				Head_blendShape.eye_squint_5 = _eye_squint_r*(1-_eye_sqz_r*1.0f);
//				Head_blendShape.eye_squint_6 = _eye_squint_r*(1-_eye_sqz_r*1.0f)*(1-_eye_look_up_r*0.5292f)*(1-_eye_look_dn_r*0.3583f);
//				Head_blendShape.eye_sqz_1 = _eye_sqz_l*(1-_eye_squint_l*0.2458f)*(1-_cheek_raise_l*0.0625f)*(1-_eye_look_up_l*0.3625f)*(1-_eye_look_dn_l*0.3667f);
//				Head_blendShape.eye_sqz_2 = _eye_sqz_l*(1-_eye_look_dn_l*0.1667f);//*(1-_sneer_l*Head_controller.EyeSqz_L_cntr.eye_sqz_sneer);
//				Head_blendShape.eye_sqz_3 = _eye_sqz_l*(1-_eye_look_up_l*0.5792f)*(1-_eye_look_dn_l*0.3542f);
//				Head_blendShape.eye_sqz_4 = _eye_sqz_r*(1-_eye_squint_r*0.2458f)*(1-_cheek_raise_r*0.0625f)*(1-_eye_look_up_r*0.3625f)*(1-_eye_look_dn_r*0.3667f);
//				Head_blendShape.eye_sqz_5 = _eye_sqz_r*(1-_eye_look_dn_r*0.1667f);//*(1-_sneer_r*Head_controller.EyeSqz_R_cntr.eye_sqz_sneer);
//				Head_blendShape.eye_sqz_6 = _eye_sqz_r*(1-_eye_look_up_r*0.5792f)*(1-_eye_look_dn_r*0.3542f);
//				Head_blendShape.cheek_raise_1 = _cheek_raise_l;
//				Head_blendShape.cheek_raise_2 = _cheek_raise_l;
//				Head_blendShape.cheek_raise_3 = _cheek_raise_l*(1-_eye_sqz_l*0.2917f);
//				Head_blendShape.cheek_raise_4 = _cheek_raise_r;
//				Head_blendShape.cheek_raise_5 = _cheek_raise_r;
//				Head_blendShape.cheek_raise_6 = _cheek_raise_r*(1-_eye_sqz_r*0.2917f);
//				Head_blendShape.eye_look_up_1 = _eye_look_up_l*(1-_upr_lid_dn_l*0.6792f)*(1-_eye_blink_l*0.1417f)*(1-_eye_squint_l*0.2292f);
//				Head_blendShape.eye_look_up_2 = _eye_look_up_l*(1-_upr_lid_dn_l*1.0f)*(1-_eye_blink_l*1.0f)*(1-_eye_sqz_l*0.225f);
//				Head_blendShape.eye_look_up_3 = _eye_look_up_r*(1-_upr_lid_dn_r*0.6792f)*(1-_eye_blink_r*0.1417f)*(1-_eye_squint_r*0.2292f);
//				Head_blendShape.eye_look_up_4 = _eye_look_up_r*(1-_upr_lid_dn_r*1.0f)*(1-_eye_blink_r*1.0f)*(1-_eye_sqz_r*0.225f);
//				Head_blendShape.eye_look_dn_1 = _eye_look_dn_l*(1-_upr_lid_dn_l*0.7917f)*(1-_eye_blink_l*0.725f);
//				Head_blendShape.eye_look_dn_2 = _eye_look_dn_l*(1-_upr_lid_dn_l*0.5583f)*(1-_eye_blink_l*0.6625f)*(1-_eye_sqz_l*0.1208f);
//				Head_blendShape.eye_look_dn_3 = _eye_look_dn_r*(1-_upr_lid_dn_r*0.7917f)*(1-_eye_blink_r*0.725f);
//				Head_blendShape.eye_look_dn_4 = _eye_look_dn_r*(1-_upr_lid_dn_r*0.5583f)*(1-_eye_blink_r*0.6625f)*(1-_eye_sqz_r*0.1208f);
//				Head_blendShape.eye_look_in_1 = _eye_look_in_l*(1-_upr_lid_dn_l*1.0f)*(1-_eye_blink_l*1.0f)*(1-_eye_squint_l*0.3875f);
//				Head_blendShape.eye_look_in_2 = _eye_look_in_l*(1-_upr_lid_dn_l*1.0f)*(1-_eye_blink_l*1.0f)*(1-_eye_squint_l*0.2542f);
//				Head_blendShape.eye_look_in_3 = _eye_look_in_r*(1-_upr_lid_dn_r*1.0f)*(1-_eye_blink_r*1.0f)*(1-_eye_squint_r*0.3875f);
//				Head_blendShape.eye_look_in_4 = _eye_look_in_r*(1-_upr_lid_dn_r*1.0f)*(1-_eye_blink_r*1.0f)*(1-_eye_squint_r*0.2542f);
//				Head_blendShape.eye_look_out_1 = _eye_look_out_l*(1-_upr_lid_dn_l*1.0f)*(1-_eye_blink_l*1.0f)*(1-_eye_squint_l*0.4583f);
//				Head_blendShape.eye_look_out_2 = _eye_look_out_l*(1-_upr_lid_dn_l*1.0f)*(1-_eye_blink_l*1.0f)*(1-_eye_squint_l*0.3167f);
//				Head_blendShape.eye_look_out_3 = _eye_look_out_r*(1-_upr_lid_dn_r*1.0f)*(1-_eye_blink_r*1.0f)*(1-_eye_squint_r*0.4583f);
//				Head_blendShape.eye_look_out_4 = _eye_look_out_r*(1-_upr_lid_dn_r*1.0f)*(1-_eye_blink_r*1.0f)*(1-_eye_squint_r*0.3167f);
				 
//				Head_blendShape.brows_dn_in_1 = _brows_dn_in_l;
//				Head_blendShape.brows_dn_in_2 = _brows_dn_in_l;
//				Head_blendShape.brows_dn_in_3 = _brows_dn_in_r;
//				Head_blendShape.brows_dn_in_4 = _brows_dn_in_r;
//				Head_blendShape.brows_dn_out_1 = _brows_dn_out_l;
//				Head_blendShape.brows_dn_out_2 = _brows_dn_out_l;
//				Head_blendShape.brows_dn_out_3 = _brows_dn_out_r;
//				Head_blendShape.brows_dn_out_4 = _brows_dn_out_r;
//				Head_blendShape.brows_sqz_1 = _brows_sqz_l*(1-_brows_up_out_l*0.05f);
//				Head_blendShape.brows_sqz_2 = _brows_sqz_l*(1-_brows_dn_in_l*0.4708f);
//				Head_blendShape.brows_sqz_3 = _brows_sqz_r*(1-_brows_up_out_r*0.05f);
//				Head_blendShape.brows_sqz_4 = _brows_sqz_r*(1-_brows_dn_in_r*0.4708f);
//				Head_blendShape.brows_up_1 = _brows_up_out_l*(1-_brows_dn_in_l*0.3167f)*(1-_brows_sqz_l*0.2958f);
//				Head_blendShape.brows_up_2 = _brows_up_in_l;
//				Head_blendShape.brows_up_3 = _brows_up_out_r*(1-_brows_dn_in_r*0.3167f)*(1-_brows_sqz_r*0.2958f);
//				Head_blendShape.brows_up_4 = _brows_up_in_r;
//				Head_blendShape.brows_sqz_up_1 = _brows_sqz_up;
//				Head_blendShape.brows_sqz_up_2 = _brows_sqz_up;
//				Head_blendShape.brows_sqz_up_3 = _brows_sqz_up;
//				Head_blendShape.brows_sqz_up_4 = _brows_sqz_up;
//				Head_blendShape.eye_blink_out_1 =_eye_look_out_l * max(_upr_lid_dn_l ,_eye_blink_l);
//				Head_blendShape.eye_blink_out_2 =_eye_look_out_r * max(_upr_lid_dn_r ,_eye_blink_r);
//				Head_blendShape.eye_blink_in_1 =_eye_look_in_l * max(_upr_lid_dn_l ,_eye_blink_l);
//				Head_blendShape.eye_blink_in_2 =_eye_look_in_r * max(_upr_lid_dn_r ,_eye_blink_r);
//			}
//		}

//		// --- ResolveShaderParam
//		public override unsafe void ResolveShaderParam(void* ptrSnappersControllers, void* ptrSnappersBlendShapes, void* ptrSnappersShaderParam)
//		{
//			ResolveShaderParam(
//				ref UnsafeUtilityEx.AsRef<SnappersControllers>(ptrSnappersControllers),
//				ref UnsafeUtilityEx.AsRef<SnappersBlendShapes>(ptrSnappersBlendShapes),
//				ref UnsafeUtilityEx.AsRef<SnappersShaderParam>(ptrSnappersShaderParam)
//			);
//		}
//		public void ResolveShaderParam(ref SnappersControllers Head_controller, ref SnappersBlendShapes Head_blendShape, ref SnappersShaderParam SkinShader)
//		{
//			// this segment generated from 'Wrinkles'
//			{
//				SkinShader.Mask1 = max(0.0f,Head_blendShape.brows_dn_in_2 ) + 0.0f; 
//				SkinShader.Mask2 = max(0.0f,Head_blendShape.brows_dn_in_4 ) + 0.0f; 
//				SkinShader.Mask3 = Head_blendShape.sneer_3 + 0.0f; 
//				SkinShader.Mask4 = Head_blendShape.sneer_7 + 0.0f;
//				SkinShader.Mask10 = max(Head_blendShape.eye_sqz_2 - Head_blendShape.eye_sqz_open_1*0.5f,0.0f) *(1 + Head_blendShape.brows_sqz_2) + 0.0f;
//				SkinShader.Mask11 = max(Head_blendShape.eye_sqz_5 - Head_blendShape.eye_sqz_open_2*0.5f,0.0f) *(1 + Head_blendShape.brows_sqz_4) + 0.0f;
//				SkinShader.Mask12 = Head_blendShape.frown_2 + 0.0f;
//				SkinShader.Mask13 = Head_blendShape.frown_4 + 0.0f;
//				float _close =max(max(max(max(max(Head_blendShape.lip_lock_open_1,Head_blendShape.lip_lock_open_2),max(Head_blendShape.sneer_close_1,Head_blendShape.sneer_close_2)),max(Head_blendShape.disgust_close_1,Head_blendShape.disgust_close_2)),max(Head_blendShape.nl_deep_close_1,Head_blendShape.nl_deep_close_2)),max(Head_blendShape.funnel_close_1 ,Head_blendShape.funnel_close_2 ))+ 0.0f + 0.0f + 0.0f + 0.0f;
//				SkinShader.Mask14 = max(_close*0.75f,max(Head_blendShape.chin_tension,max(Head_blendShape.chin_raise_1,Head_blendShape.chin_raise_4))) + 0.0f;
//				SkinShader.Mask19 = Head_blendShape.brows_up_1 + 0.0f;
//				SkinShader.Mask20 = Head_blendShape.brows_up_2 + 0.0f;
//				SkinShader.Mask21 = Head_blendShape.brows_up_4 + 0.0f;
//				SkinShader.Mask22 = Head_blendShape.brows_up_3 + 0.0f;
//				SkinShader.Mask23 = Head_blendShape.stretch_2 + 0.0f;
//				SkinShader.Mask24 = Head_blendShape.stretch_4 + 0.0f;
//				SkinShader.Mask25 = Head_blendShape.neck_tension_1 + 0.0f;
//				SkinShader.Mask26 = Head_blendShape.neck_tension_2 + 0.0f;
//				SkinShader.Mask28 = max(Head_blendShape.brows_sqz_up_2,Head_blendShape.brows_sqz_up_4) + 0.0f;
//				SkinShader.Mask29 = max(Head_blendShape.jaw_open_1,Head_blendShape.jaw_open_6)-max(Head_blendShape.lip_lock_open_1,Head_blendShape.lip_lock_open_2)/2.0f + 0.0f;
//				SkinShader.Mask37 = Head_blendShape.puff_1 + 0.0f;
//				SkinShader.Mask38 = Head_blendShape.puff_2 + 0.0f;
//				SkinShader.Mask46 = Head_blendShape.smile_5 + 0.0f;
//				SkinShader.Mask47 = Head_blendShape.smile_10 + 0.0f;
//				SkinShader.Mask48 = Head_blendShape.cheek_raise_2 + 0.0f;
//				SkinShader.Mask49 = Head_blendShape.cheek_raise_5 + 0.0f;
//				SkinShader.Mask50 = Head_blendShape.brows_sqz_2 + 0.0f;
//				SkinShader.Mask51 = Head_blendShape.brows_sqz_4 + 0.0f;
//				SkinShader.Mask55 = max(Head_blendShape.pucker_2,Head_blendShape.o_2) + 0.0f + 0.0f;
//				SkinShader.Mask56 = max(Head_blendShape.pucker_5,Head_blendShape.o_5) + 0.0f + 0.0f;
//				SkinShader.Mask57 = max((max(Head_blendShape.eye_look_dn_2,max(Head_blendShape.eye_blink_2,0.0f)) - Head_blendShape.eye_sqz_2),0.0f) + 0.0f + 0.0f;
//				SkinShader.Mask58 = max((max(Head_blendShape.eye_look_dn_4,max(Head_blendShape.eye_blink_4,0.0f)) - Head_blendShape.eye_sqz_5),0.0f) + 0.0f + 0.0f;
//				SkinShader.Mask64 = Head_blendShape.funnel_1 + 0.0f;
//				SkinShader.Mask65 = Head_blendShape.funnel_3 + 0.0f;
//				SkinShader.Mask66 = Head_blendShape.funnel_4 + 0.0f;
//				SkinShader.Mask67 = Head_blendShape.funnel_6 + 0.0f;
//				SkinShader.Mask73 = max(Head_blendShape.tension_2,Head_blendShape.tension_5)+ max(Head_blendShape.press_2,Head_blendShape.press_5)*0.75f + 0.0f;
//				SkinShader.Mask74 = Head_blendShape.eye_squint_1 + 0.0f;
//				SkinShader.Mask75 = Head_blendShape.eye_squint_4 + 0.0f;
//				SkinShader.Mask82 = Head_blendShape.mouth_l;
//				SkinShader.Mask92 = Head_blendShape.mouth_r;
//				SkinShader.Mask100 = Head_blendShape.lip_in_2 + Head_blendShape.dimple_2 + 0.0f + 0.0f;
//				SkinShader.Mask101 = Head_blendShape.lip_in_5 + Head_blendShape.dimple_4 + 0.0f + 0.0f;
//				SkinShader.Mask109 = Head_blendShape.disgust_2 + Head_blendShape.nl_deep_2 + Head_blendShape.corner_up_2*0.5f + 0.0f + 0.0f;
//				SkinShader.Mask110 = Head_blendShape.disgust_5 + Head_blendShape.nl_deep_5 + Head_blendShape.corner_up_7*0.5f + 0.0f + 0.0f;
//				SkinShader.Mask118 = hermite(0,4,0,5,Head_blendShape.smile_drop_1 );
//				SkinShader.Mask119 = hermite(0,4,0,5,Head_blendShape.smile_drop_2 );
//				SkinShader.Mask120 = hermite(0,4,0,5,Head_blendShape.stretch_drop_1 );
//				SkinShader.Mask121 = hermite(0,4,0,5,Head_blendShape.stretch_drop_2 );
//			}
//		}

//		// --- InitializeControllerCaps
//		public override unsafe void InitializeControllerCaps(void* ptrSnappersControllers)
//		{
//			InitializeControllerCaps(
//				ref UnsafeUtilityEx.AsRef<SnappersControllers>(ptrSnappersControllers)
//			);
//		}
//		public void InitializeControllerCaps(ref SnappersControllers Head_controller)
//		{
//			Head_controller.Brow_1_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_1_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_1_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_1_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_2_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_2_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_2_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_2_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_3_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_3_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_3_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_3_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.Brow_cntr.caps = SnappersControllerCaps.translateY;
//			Head_controller.Brow_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.BrowIn_L_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY;
//			Head_controller.BrowIn_L_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.BrowIn_R_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY;
//			Head_controller.BrowIn_R_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.BrowOut_L_cntr.caps = SnappersControllerCaps.translateY;
//			Head_controller.BrowOut_L_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.BrowOut_R_cntr.caps = SnappersControllerCaps.translateY;
//			Head_controller.BrowOut_R_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Cheek_L_2_cntr.caps = SnappersControllerCaps.translateX;
//			Head_controller.Cheek_L_2_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Cheek_L_cntr.caps = SnappersControllerCaps.translateY;
//			Head_controller.Cheek_L_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Cheek_R_2_cntr.caps = SnappersControllerCaps.translateX;
//			Head_controller.Cheek_R_2_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Cheek_R_cntr.caps = SnappersControllerCaps.translateY;
//			Head_controller.Cheek_R_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Chin_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.Chin_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Chin_L_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY;
//			Head_controller.Chin_L_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Chin_R_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY;
//			Head_controller.Chin_R_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Corner_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.Corner_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.Corner_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.Corner_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.Crnr_L_2_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.Crnr_L_2_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Crnr_L_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.Crnr_L_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Crnr_R_2_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.Crnr_R_2_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Crnr_R_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.Crnr_R_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.EyeSqz_L_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY;
//			Head_controller.EyeSqz_L_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.EyeSqz_R_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY;
//			Head_controller.EyeSqz_R_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Head_cntr.caps = SnappersControllerCaps.translateY;
//			Head_controller.Head_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Jaw_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.Jaw_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.LeftEye.caps = SnappersControllerCaps.rotateX | SnappersControllerCaps.rotateY;
//			Head_controller.LwrLid_1_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_1_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_1_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_1_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_2_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_2_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_2_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_2_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_3_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_3_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_3_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_3_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_L_cntr.caps = SnappersControllerCaps.translateY;
//			Head_controller.LwrLid_L_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLid_R_cntr.caps = SnappersControllerCaps.translateY;
//			Head_controller.LwrLid_R_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLip_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateZ;
//			Head_controller.LwrLip_cntr_adj.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLip_cntr_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLip_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLip_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLip_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLip_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLip_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLip_L_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.LwrLip_L_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.LwrLip_R_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.LwrLip_R_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Mouth_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.Mouth_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Neck_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.Neck_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Nose_cntr.caps = SnappersControllerCaps.translateX;
//			Head_controller.Nose_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Nose_L_cntr.caps = SnappersControllerCaps.translateY;
//			Head_controller.Nose_L_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Nose_R_cntr.caps = SnappersControllerCaps.translateY;
//			Head_controller.Nose_R_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.RightEye.caps = SnappersControllerCaps.rotateX | SnappersControllerCaps.rotateY;
//			Head_controller.Teeth_cntr.caps = SnappersControllerCaps.none;
//			Head_controller.Teeth_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Tongue_cntr.caps = SnappersControllerCaps.none;
//			Head_controller.Tongue_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.Tongue_curl_cntr.caps = SnappersControllerCaps.none;
//			Head_controller.Tongue_curl_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_1_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_1_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_1_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_1_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_2_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_2_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_2_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_2_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_3_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_3_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_3_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_3_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_L_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY;
//			Head_controller.UprLid_L_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLid_R_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY;
//			Head_controller.UprLid_R_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_2_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.UprLip_2_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateZ;
//			Head_controller.UprLip_cntr_adj.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_cntr_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_cntr_L_adj.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_cntr_L_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_cntr_R_adj.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_cntr_R_adj_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_L_2_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY;
//			Head_controller.UprLip_L_2_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_L_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.UprLip_L_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_R_2_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY;
//			Head_controller.UprLip_R_2_cntr_p.caps = SnappersControllerCaps.none;
//			Head_controller.UprLip_R_cntr.caps = SnappersControllerCaps.translateX | SnappersControllerCaps.translateY | SnappersControllerCaps.translateZ;
//			Head_controller.UprLip_R_cntr_p.caps = SnappersControllerCaps.none;
//		}
//#pragma warning restore 0219
//	}
}
