using UnityEngine;
using UnityEngine.Experimental.Rendering;

[GenerateHLSL]
public struct SFiniteLightData
{
    // setup constant buffer
    public float penumbra;
    public int  flags;
    public uint lightType;
    public uint lightModel;        // DIRECT_LIGHT=0, REFLECTION_LIGHT=1

    public Vector3 lightPos;
    public float lightIntensity;

    public Vector3 lightAxisX;
    public float recipRange;

    public Vector3 lightAxisY;
    public float radiusSq;

    public Vector3 lightAxisZ;      // spot +Z axis
    public float cotan;

    public Vector3 color;
    public int sliceIndex;

    public Vector3 boxInnerDist;
    public float decodeExp;

    public Vector3 boxInvRange;
    public uint shadowLightIndex;

    public Vector3 localCubeCapturePoint;
    public float probeBlendDistance;
};

[GenerateHLSL]
public struct SFiniteLightBound
{
    public Vector3 boxAxisX;
    public Vector3 boxAxisY;
    public Vector3 boxAxisZ;
    public Vector3 center;        // a center in camera space inside the bounding volume of the light source.
    public Vector2 scaleXY;
    public float radius;
};

[GenerateHLSL]
public struct DirectionalLight
{
    public Vector3 color;
    public float intensity;

    public Vector3 lightAxisX;
    public uint shadowLightIndex;

    public Vector3 lightAxisY;
    public float pad0;

    public Vector3 lightAxisZ;
    public float pad1;
};

[GenerateHLSL]
public class LightDefinitions
{
    public static int MAX_NR_LIGHTS_PER_CAMERA = 1024;
    public static int MAX_NR_BIGTILE_LIGHTS_PLUSONE = 512;      // may be overkill but the footprint is 2 bits per pixel using uint16.
    public static float VIEWPORT_SCALE_Z = 1.0f;

    // must be either 16, 32 or 64. Could go higher in principle but big tiles in the pre-pass are already 64x64
    public static int TILE_SIZE_CLUSTERED = 32;

    // enable unity's original left-hand shader camera space (right-hand internally in unity).
    public static int USE_LEFTHAND_CAMERASPACE = 0;

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
