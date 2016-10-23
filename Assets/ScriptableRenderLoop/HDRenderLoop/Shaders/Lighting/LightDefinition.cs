using UnityEngine;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    // These structures share between C# and hlsl need to be align on float4, so we pad them.
    [GenerateHLSL]
    public struct PunctualLightData
    {
        public Vector3 positionWS;
        public float invSqrAttenuationRadius;

        public Vector3 color;
        public float useDistanceAttenuation;

        public Vector3 forward;
        public float diffuseScale;

        public Vector3 up;
        public float specularScale;

        public Vector3 right;
        public float shadowDimmer;

        public float angleScale;
        public float angleOffset;
        public int flags;
        public int IESIndex;
    };

    [GenerateHLSL]
    public enum AreaShapeType
    {
        Rectangle,
        Line,
        // Currently not supported in real time (just use for reference)
        Sphere,
        Disk,
        Hemisphere,
        Cylinder
    };

    [GenerateHLSL]
    public struct AreaLightData
    {
        public Vector3 positionWS;
        public float invSqrAttenuationRadius;

        public Vector3 color;
        public AreaShapeType shapeType;

        public Vector3 forward;
        public float diffuseScale;

        public Vector3 up;
        public float specularScale;

        public Vector3 right;
        public float shadowDimmer;

        public Vector2 size;
        public float twoSided;
        public float unused;
    };

    [GenerateHLSL]
    public enum EnvShapeType
    {
        None, 
        Box, 
        Sphere
    };

    [GenerateHLSL]
    public struct EnvLightData
    {
        public Vector3 positionWS;
        public EnvShapeType envShapeType;

        public Vector3 forward;
        public float unused2;

        public Vector3 up;
        public float blendDistance;     // blend transition outside the volume

        public Vector3 right;
        public int sliceIndex;

        public Vector3 innerDistance;   // equivalent to volume scale
        public float unused0;

        public Vector3 offsetLS;
        public float unused1;
    };

    struct PunctualShadowData
    {
	    // World to ShadowMap matrix
        // Include scale and bias for shadow atlas if any
	    public Vector4 shadowMatrix1;
	    public Vector4 shadowMatrix2;
	    public Vector4 shadowMatrix3;
	    public Vector4 shadowMatrix4;
	
	    float4	shadowMapAtlasParam[6];	// shadow map size and offset of atlas per face

	    float	shadowMapIndex[6];		//the shadow map index per face
	    float	shadowType;				// Disabled, spot, point
	    float	quality;				// shadow filtering quality
	
	    float	shadowAngleScale;
	    float	shadowAngleOffset;
	    float2	unused;
    };
} // namespace UnityEngine.Experimental.ScriptableRenderLoop
