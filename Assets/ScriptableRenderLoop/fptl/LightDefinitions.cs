//#define LEFT_HAND_COORDINATES
[UnityEngine.ScriptableRenderLoop.GenerateHLSL]
public struct SFiniteLightData
{
	 // setup constant buffer
    public float fPenumbra;
	public int  flags;
    public uint uLightType;
    public uint uLightModel;        // DIRECT_LIGHT=0, REFLECTION_LIGHT=1

    public Vec3 vLpos;
    public float fLightIntensity;
    
	public Vec3 vLaxisX;
    public float fRecipRange;

	public Vec3 vLaxisY;
	public float fSphRadiusSq;

    public Vec3 vLaxisZ;      // spot +Z axis
    public float cotan;
	
	public Vec3	vCol;
	public int iSliceIndex;

    public Vec3 vBoxInnerDist;
    public float fDecodeExp;

    public Vec3 vBoxInvRange;
	public uint uShadowLightIndex;

    public Vec3 vLocalCubeCapturePoint;
    public float fProbeBlendDistance;
};

[UnityEngine.ScriptableRenderLoop.GenerateHLSL]
public struct SFiniteLightBound
{
    public Vec3 vBoxAxisX;
    public Vec3 vBoxAxisY;
    public Vec3 vBoxAxisZ;
    public Vec3 vCen;		// a center in camera space inside the bounding volume of the light source.
    public Vec2 vScaleXY;
    public float fRadius;
};

[UnityEngine.ScriptableRenderLoop.GenerateHLSL]
public struct DirectionalLight
{
	public Vec3 vCol;
	public float fLightIntensity;

	public Vec3 vLaxisX;
	public uint uShadowLightIndex;

	public Vec3 vLaxisY;
	public float fPad0;

	public Vec3 vLaxisZ;
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
