namespace UnityEngine.MaterialGraph
{
	[Title ("Math/Vector/Transform")]
	public class TransformNode : Function1Input
	{
		public TransformNode ()
		{
			name = "Transform";
		}

		protected override string GetFunctionName ()
		{

			//mul(unity_WorldToObject, float4(i.posWorld.rgb,0) ).xyz - world to local
			//mul( tangentTransform, i.posWorld.rgb ).xyz - world to tangent
			//mul( UNITY_MATRIX_V, float4(i.posWorld.rgb,0) ).xyz - world to view

			//mul( unity_ObjectToWorld, float4(i.posWorld.rgb,0) ).xyz - local to world
			//mul( tangentTransform, mul( unity_ObjectToWorld, float4(i.posWorld.rgb,0) ).xyz - local to tangent
			//mul( UNITY_MATRIX_MV, float4(i.posWorld.rgb,0) ).xyz - local to view

			//mul( i.posWorld.rgb, tangentTransform ).xyz - tangent to world
			//mul( unity_WorldToObject, float4(mul( i.posWorld.rgb, tangentTransform ),0) ).xyz - tangent to local
			//mul( UNITY_MATRIX_V, float4(mul( i.posWorld.rgb, tangentTransform ),0) ).xyz - tangent to view

			//mul( float4(i.posWorld.rgb,0), UNITY_MATRIX_V ).xyz - view to world
			//mul( float4(i.posWorld.rgb,0), UNITY_MATRIX_MV ).xyz - view to local
			//mul( tangentTransform, mul( float4(i.posWorld.rgb,0), UNITY_MATRIX_V ).xyz ).xyz - view to tangent

			return "exp";
		}
	}
}

