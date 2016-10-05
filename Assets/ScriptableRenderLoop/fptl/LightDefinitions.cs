using UnityEngine;

[UnityEngine.ScriptableRenderLoop.GenerateHLSL]
public struct SFiniteLightData
{
	 // setup constant buffer
    public float fPenumbra;
	public int  flags;
    public uint uLightType;
    public uint uLightModel;        // DIRECT_LIGHT=0, REFLECTION_LIGHT=1

    public Vector3 vLpos;
    public float fLightIntensity;
    
	public Vector3 vLaxisX;
    public float fRecipRange;

	public Vector3 vLaxisY;
	public float fSphRadiusSq;

    public Vector3 vLaxisZ;      // spot +Z axis
    public float cotan;
	
	public Vector3	vCol;
	public int iSliceIndex;

    public Vector3 vBoxInnerDist;
    public float fDecodeExp;

    public Vector3 vBoxInvRange;
	public uint uShadowLightIndex;

    public Vector3 vLocalCubeCapturePoint;
    public float fProbeBlendDistance;
};

[UnityEngine.ScriptableRenderLoop.GenerateHLSL]
public struct SFiniteLightBound
{
    public Vector3 vBoxAxisX;
    public Vector3 vBoxAxisY;
    public Vector3 vBoxAxisZ;
    public Vector3 vCen;		// a center in camera space inside the bounding volume of the light source.
    public Vector2 vScaleXY;
    public float fRadius;
};

[UnityEngine.ScriptableRenderLoop.GenerateHLSL]
public struct DirectionalLight
{
	public Vector3 vCol;
	public float fLightIntensity;

	public Vector3 vLaxisX;
	public uint uShadowLightIndex;

	public Vector3 vLaxisY;
	public float fPad0;

	public Vector3 vLaxisZ;
	public float fPad1;
};

[UnityEngine.ScriptableRenderLoop.GenerateHLSL]
public class LightDefinitions
{
    public static int MAX_NR_LIGHTS_PER_CAMERA = 1024;
    public static float VIEWPORT_SCALE_Z = 1.0f;

    // flags
    public static int IS_CIRCULAR_SPOT_SHAPE = 1;
    public static int HAS_COOKIE_TEXTURE = 2;
    public static int IS_BOX_PROJECTED = 4;
    public static int HAS_SHADOW = 8;


    // types
    public static int MAX_TYPES = 3;

    public static int SPOT_LIGHT = 0;
    public static int SPHERE_LIGHT = 1;
    public static int BOX_LIGHT = 2;
	public static int DIRECTIONAL_LIGHT = 3;

	// direct lights and reflection probes for now
	public static int NR_LIGHT_MODELS = 2;
    public static int DIRECT_LIGHT = 0;
    public static int REFLECTION_LIGHT = 1;
}
