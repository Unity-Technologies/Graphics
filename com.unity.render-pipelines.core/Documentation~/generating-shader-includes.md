# Synchronizing shader code and C#

Unity can generate HLSL code based on C# structs to synchronize data and constants between shaders and C#. In Unity, the process of generating the HLSL code from C# code is called generating shader includes. When Unity generates shader includes, it parses all the C# files in the project and, for every file that contains a struct with a GenerateHLSL attribute, generates corresponding HLSL code. It places this HLSL code in a file with the same name as the origin, but uses the `.cs.hlsl` file extension.

## Generating shader includes

To generate an HLSL equivalent for a C# struct:

1. Add the GenerateHLSL attribute to the struct. To do this, above the line that declares the struct, add `[GenerateHLSL(PackingRules.Exact, false)]`. For an example on how to do this, see the sample code below. For more information about the GenerateHLSL attribute, see the [API documentation](../api/UnityEngine.Rendering.GenerateHLSL.html).
2. In the Unity Editor, go to **Edit** > **Render Pipeline** > **Generate Shader Includes**.

The following code example is from the High Definition Render Pipeline (HDRP). It shows an extract of the C# representation of a directional light. The original file is `LightDefinition.cs`. When Unity generates the HLSL shader code, it places it in a new file called `LightDefinition.cs.hlsl`.


```
// LightDefinition.cs

[GenerateHLSL(PackingRules.Exact, false)]
struct DirectionalLightData
{
        public Vector3 positionRWS;
        public uint lightLayers;
        public float lightDimmer;
        public float volumetricLightDimmer;   // Replaces 'lightDimer'
        public Vector3 forward;
        public Vector4 surfaceTextureScaleOffset;
};
```

```
// LightDefinition.cs.hlsl

// Generated from UnityEngine.Rendering.HighDefinition.DirectionalLightData
// PackingRules = Exact
struct DirectionalLightData
{
    float3 positionRWS;
    uint lightLayers;
    float lightDimmer;
    float volumetricLightDimmer;
    float3 forward;
    float4 surfaceTextureScaleOffset;
};
```
