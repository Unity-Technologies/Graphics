// Note: This file is included both in C# code and in hlsl code, avoiding duplication

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------

struct PunctualLightData
{
    public Vec3 positionWS;
    public float invSqrAttenuationRadius;
    public Vec3 color;
    public float unused;

    public Vec3 forward;
    public float diffuseScale;

    public Vec3 up;
    public float specularScale;

    public Vec3 right;
    public float shadowDimmer;

    public float angleScale;
    public float angleOffset;
    public Vec2 unused2;
};

struct AreaLightData
{
    public Vec3 positionWS;
};

struct EnvLightData
{
    public Vec3 positionWS;
};

struct PlanarLightData
{
    public Vec3 positionWS;
};

