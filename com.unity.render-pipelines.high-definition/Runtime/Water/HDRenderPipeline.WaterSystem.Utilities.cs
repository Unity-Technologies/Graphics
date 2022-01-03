using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Converts an angle to a 2d direction
        static internal Vector2 OrientationToDirection(float orientation)
        {
            float orientationRad = orientation * Mathf.Deg2Rad;
            float directionX = Mathf.Cos(orientationRad);
            float directionY = Mathf.Sin(orientationRad);
            return new Vector2(directionX, directionY);
        }

        // Function that guesses the maximal wave height from the wind speed
        static internal float MaximumWaveHeightFunction(float windSpeed)
        {
            return 1.0f - Mathf.Exp(-k_PhillipsWindFalloffCoefficient * windSpeed * windSpeed);
        }

        // Function that loops thought all the current waves and computes the maximal wave height
        internal void ComputeMaximumWaveHeight(Vector4 normalizedWaveAmplitude, float waterWindSpeed, bool highBandCount, out Vector4 waveHeights, out float maxWaveHeight)
        {
            // Initialize the band data
            float b0 = 0.0f, b1 = 0.0f, b2 = 0.0f, b3 = 0.0f;
            maxWaveHeight = 0.01f;

            // Evaluate the wave height for each band (lower frequencies)
            b0 = k_WaterAmplitudeNormalization * normalizedWaveAmplitude.x * MaximumWaveHeightFunction(waterWindSpeed);
            maxWaveHeight = Mathf.Max(b0, maxWaveHeight);
            b1 = k_WaterAmplitudeNormalization * normalizedWaveAmplitude.y * MaximumWaveHeightFunction(waterWindSpeed);
            maxWaveHeight = Mathf.Max(b1, maxWaveHeight);

            // Evaluate the wave height for each band (higher frequencies)
            if (highBandCount)
            {
                maxWaveHeight = Mathf.Max(b2, maxWaveHeight);
                b2 = k_WaterAmplitudeNormalization * normalizedWaveAmplitude.z * MaximumWaveHeightFunction(waterWindSpeed);
                maxWaveHeight = Mathf.Max(b3, maxWaveHeight);
                b3 = k_WaterAmplitudeNormalization * normalizedWaveAmplitude.w * MaximumWaveHeightFunction(waterWindSpeed);
            }

            // Output the wave heights
            waveHeights = new Vector4(b0, b1, b2, b3);
        }

        // Function that evaluates the maximum wind speed given a patch size
        static internal float MaximumWindForPatch(float patchSize)
        {
            float a = Mathf.Sqrt(-1.0f / Mathf.Log(0.999f * 0.999f));
            float b = (0.001f * Mathf.PI * 2.0f) / patchSize;
            float c = k_PhillipsWindScalar * Mathf.Sqrt((1.0f / k_PhillipsGravityConstant) * (a / b));
            return c;
        }

        // Function that evaluates the patch sizes of the 4 bands based on the max patch size
        static internal Vector4 ComputeBandPatchSizes(float maxPatchSize)
        {
            float range = maxPatchSize - k_MinPatchSize;
            float b0 = maxPatchSize;
            float b1 = maxPatchSize - 7.0f / 8.0f * range;
            float b2 = maxPatchSize - 31.0f / 32.0f * range;
            float b3 = maxPatchSize - 63.0f / 64.0f * range;
            return new Vector4(b0, b1, b2, b3);
        }

        // Function that evaluates the wind speed of each individual patch
        static internal Vector4 ComputeWindSpeeds(float windSpeed, Vector4 patchSizes)
        {
            float normalizedWindSpeed = Mathf.Sqrt(windSpeed / 100.0f);
            float b0 = MaximumWindForPatch(patchSizes.x) * normalizedWindSpeed;
            float b1 = MaximumWindForPatch(patchSizes.y) * normalizedWindSpeed;
            float b2 = MaximumWindForPatch(patchSizes.z) * normalizedWindSpeed;
            float b3 = MaximumWindForPatch(patchSizes.w) * normalizedWindSpeed;
            return new Vector4(b0, b1, b2, b3);
        }

        // Function that applies a profile to the scattering color to make the edition range linear
        static internal Color RemapScatteringColor(Color scatteringColor)
        {
            float h, s, v;
            Color.RGBToHSV(scatteringColor, out h, out s, out v);
            v *= v;
            return Color.HSVToRGB(h, s, v);
        }

        // Function that returns a mip offset (for caustics) based on the simulation resolution
        static internal int EvaluateNormalMipOffset(WaterSimulationResolution resolution)
        {
            switch (resolution)
            {
                case WaterSimulationResolution.Ultra512:
                    return 3;
                case WaterSimulationResolution.High256:
                    return 2;
                case WaterSimulationResolution.Medium128:
                    return 1;
                case WaterSimulationResolution.Low64:
                    return 0;
            }
            return 0;
        }

        // Compute the resolution of the water patch based on it's distance to the center patch
        static internal int GetPatchResolution(int x, int y, int maxResolution)
        {
            return Mathf.Max((maxResolution >> (Mathf.Abs(x) + Mathf.Abs(y))), k_WaterMinGridSize);
        }

        // Evaluate the mask that allows us to adapt the tessellation at patch edges
        static internal int EvaluateTesselationMask(int x, int y, int maxResolution)
        {
            int center = GetPatchResolution(x, y, maxResolution);
            int up = GetPatchResolution(x, y + 1, maxResolution);
            int down = GetPatchResolution(x, y - 1, maxResolution);
            int right = GetPatchResolution(x - 1, y, maxResolution);
            int left = GetPatchResolution(x + 1, y, maxResolution);
            int mask = 0;
            mask |= (center > right) ? 0x1 : 0;
            mask |= (center > up) ? 0x2 : 0;
            mask |= (center > left) ? 0x4 : 0;
            mask |= (center > down) ? 0x8 : 0;
            return mask;
        }

        // Function that evaluates the bounds of a given grid based on it's coordinates
        static internal void ComputeGridBounds(int x, int y, int numLODS, float centerGridSize, Vector3 centerGridPos, float farPlane, out Vector3 center, out Vector2 size)
        {
            int absX = Mathf.Abs(x);
            int absY = Mathf.Abs(y);
            float signX = Mathf.Sign(x);
            float signY = Mathf.Sign(y);

            // Offset position of the patch
            center = new Vector3(signX * offsets[absX] * centerGridSize, centerGridPos.y, signY * offsets[absY] * centerGridSize);

            // Size of the patch
            size = new Vector2(centerGridSize * (1 << absX), centerGridSize * (1 << absY));
        }
    }
}
