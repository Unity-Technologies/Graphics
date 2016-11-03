using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

// Very basic scriptable rendering loop example:
// - Use with BasicRenderLoopShader.shader (the loop expects "BasicPass" pass type to exist)
// - Supports up to 8 enabled lights in the scene (directional, point or spot)
// - No shadows
// - This loop also does not setup lightmaps, light probes or reflection probes

[ExecuteInEditMode]
public class BasicRenderLoop : MonoBehaviour
{
    private ShaderPassName shaderPassBasic;

    public void OnEnable ()
    {
        shaderPassBasic = new ShaderPassName ("BasicPass");
        RenderLoop.renderLoopDelegate += Render;
    }

    public void OnDisable ()
    {
        RenderLoop.renderLoopDelegate -= Render;
    }

    // Main entry point for our scriptable render loop
    bool Render (Camera[] cameras, RenderLoop loop)
    {
        foreach (var camera in cameras)
        {
            // Culling
            CullingParameters cullingParams;
            if (!CullResults.GetCullingParameters (camera, out cullingParams))
                continue;
            CullResults cull = CullResults.Cull (ref cullingParams, loop);

            // Setup camera for rendering (sets render target, view/projection matrices and other
            // per-camera built-in shader variables).
            loop.SetupCameraProperties (camera);

            // Setup global lighting shader variables
            SetupLightShaderVariables (cull.visibleLights, loop);

            // Draw opaque objects using BasicPass shader pass
            var settings = new DrawRendererSettings (cull, camera, shaderPassBasic);
            settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;
            settings.inputCullingOptions.SetQueuesOpaque ();
            loop.DrawRenderers (ref settings);

            // Draw skybox
            loop.DrawSkybox (camera);

            // Draw transparent objects using BasicPass shader pass
            settings.sorting.sortOptions = SortOptions.BackToFront; // sort back to front
            settings.inputCullingOptions.SetQueuesTransparent ();
            loop.DrawRenderers (ref settings);

            loop.Submit ();
        }

        return true;
    }

    static void SetupLightShaderVariables (VisibleLight[] lights, RenderLoop loop)
    {
        const int kMaxLights = 8;
        int lightCount = Mathf.Min (lights.Length, kMaxLights);
        // x - light count
        // y - zero (needed by d3d9 VS loop instruction; initial loop value)
        // z - one (needed by d3d9 VS loop instruction; loop increment)
        // w - unused
        Vector4 lightCountVector = new Vector4 (lightCount, 0, 1, 0);

        Vector4[] lightColors = new Vector4[kMaxLights];
        Vector4[] lightPositions = new Vector4[kMaxLights];
        Vector4[] lightSpotDirections = new Vector4[kMaxLights];
        Vector4[] lightAtten = new Vector4[kMaxLights];
        for (var i = 0; i < lightCount; ++i)
        {
            VisibleLight light = lights[i];
            lightColors[i] = light.finalColor;
            if (light.lightType == LightType.Directional)
            {
                var dir = light.localToWorld.GetColumn (2);
                lightPositions[i] = new Vector4 (-dir.x, -dir.y, -dir.z, 0);
            }
            else
            {
                var pos = light.localToWorld.GetColumn (3);
                lightPositions[i] = new Vector4 (pos.x, pos.y, pos.z, 1);
            }
            // attenuation set in a way where distance attenuation can be computed:
            //	float lengthSq = dot(toLight, toLight);
            //	float atten = 1.0 / (1.0 + lengthSq * unity_LightAtten[i].z);
            // and spot cone attenuation:
            //	float rho = max (0, dot(normalize(toLight), unity_SpotDirection[i].xyz));
            //	float spotAtt = (rho - unity_LightAtten[i].x) * unity_LightAtten[i].y;
            //	spotAtt = saturate(spotAtt);
            // and the above works for all light types, i.e. spot light code works out
            // to correct math for point & directional lights as well.

            float rangeSq = light.range * light.range;

            float quadAtten;
            if (light.lightType == LightType.Directional)
            {
                quadAtten = 0.0f;
            }
            else
            {
                quadAtten = 25.0f / rangeSq;
            }

            // spot direction & attenuation
            if (light.lightType == LightType.Spot)
            {
                var dir = light.localToWorld.GetColumn (2);
                lightSpotDirections[i] = new Vector4 (-dir.x, -dir.y, -dir.z, 0);

                float radAngle = Mathf.Deg2Rad * light.light.spotAngle;
                float cosTheta = Mathf.Cos (radAngle * 0.25f);
                float cosPhi = Mathf.Cos (radAngle * 0.5f);
                float cosDiff = cosTheta - cosPhi;
                lightAtten[i] = new Vector4 (cosPhi, (cosDiff != 0.0f) ? 1.0f / cosDiff : 1.0f, quadAtten, rangeSq);
            }
            else
            {
                // non-spot light
                lightSpotDirections[i] = new Vector4 (0, 0, 1, 0);
                lightAtten[i] = new Vector4 (-1, 1, quadAtten, rangeSq);
            }
    	}

        const int kSHCoefficients = 7;
        Vector4[] shConstants = new Vector4[kSHCoefficients];
        SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe * RenderSettings.ambientIntensity;
        GetShaderConstantsFromNormalizedSH (ref ambientSH, shConstants);


        CommandBuffer cmd = new CommandBuffer();
        cmd.SetGlobalVectorArray ("globalLightColor", lightColors);
        cmd.SetGlobalVectorArray ("globalLightPos", lightPositions);
        cmd.SetGlobalVectorArray ("globalLightSpotDir", lightSpotDirections);
        cmd.SetGlobalVectorArray ("globalLightAtten", lightAtten);
        cmd.SetGlobalVector ("globalLightCount", lightCountVector);
        cmd.SetGlobalVectorArray ("globalSH", shConstants);
        loop.ExecuteCommandBuffer (cmd);
        cmd.Dispose ();
    }

    static void GetShaderConstantsFromNormalizedSH (ref SphericalHarmonicsL2 ambientProbe, Vector4[] outCoefficients)
    {
        for (int channelIdx = 0; channelIdx < 3; ++channelIdx)
        {
            // Constant + Linear
            // In the shader we multiply the normal is not swizzled, so it's normal.xyz.
            // Swizzle the coefficients to be in { x, y, z, DC } order.
            outCoefficients[channelIdx].x = ambientProbe[channelIdx, 3];
            outCoefficients[channelIdx].y = ambientProbe[channelIdx, 1];
            outCoefficients[channelIdx].z = ambientProbe[channelIdx, 2];
            outCoefficients[channelIdx].w = ambientProbe[channelIdx, 0] - ambientProbe[channelIdx, 6];
            // Quadratic polynomials
            outCoefficients[channelIdx + 3].x = ambientProbe[channelIdx, 4];
            outCoefficients[channelIdx + 3].y = ambientProbe[channelIdx, 5];
            outCoefficients[channelIdx + 3].z = ambientProbe[channelIdx, 6] * 3.0f;
            outCoefficients[channelIdx + 3].w = ambientProbe[channelIdx, 7];
        }
        // Final quadratic polynomial
        outCoefficients[6].x = ambientProbe[0, 8];
        outCoefficients[6].y = ambientProbe[1, 8];
        outCoefficients[6].z = ambientProbe[2, 8];
        outCoefficients[6].w = 1.0f;
    }
}
