using System.IO;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class PlanarReflectionAngleGenerator : MonoBehaviour
    {
        void Start()
        {
        }

        public class GenerationParameters
        {
            public int outputWidth;
            public int outputHeight;
            public int angleSubdivision;
            public float brdfPercentage;
        }

        float SafeDiv(float numer, float denom)
        {
            return (numer != denom) ? numer / denom : 1;
        }

        float D_GGXNoPI(float NdotH, float roughness)
        {
            float a2 = roughness * roughness;
            float s = (NdotH * a2 - NdotH) * NdotH + 1.0f;
            // If roughness is 0, returns (NdotH == 1 ? 1 : 0).
            // That is, it returns 1 for perfect mirror reflection, and 0 otherwise.
            return SafeDiv(a2, s * s);
        }

        public void GenerateTableExample()
        {
            GenerationParameters generationParameters = new GenerationParameters();
            generationParameters.outputWidth = 128;
            generationParameters.outputHeight = 128;
            generationParameters.angleSubdivision = 128;
            generationParameters.brdfPercentage = 0.7f;
            GenerateTable(generationParameters);
        }

        void GetLocalFrame(Vector3 localZ, Vector3 localX, out Vector3 localY)
        {
            localY = Vector3.Cross(localZ, localX);
        }

        public void GenerateTable(GenerationParameters generationParameters)
        {
            // This buffer will hold the theta values on which the brdfPercentage criterion is full-filed
            float[] thetaValues = new float[generationParameters.outputWidth * generationParameters.outputHeight];

            // To make it simple the point we are processing is placed at the center of the world and the camera will be moving around
            Vector3 pointPosition = new Vector3(0.0f, 0.0f, 0.0f);
            // Normal is fixed for the whole process
            Vector3 normalVector = new Vector3(0, 0, 1.0f);

            // This Buffer will hold the ggx values and histogram used to evaluate the theta angle when brdf percentage is full-filled
            float[] ggxValues = new float[generationParameters.outputWidth];
            float[] ggxHistogram = new float[generationParameters.outputHeight];

            // Let's go through all the roughness inputs
            for (int currentRoughnessIdx = 0; currentRoughnessIdx < generationParameters.outputHeight; ++currentRoughnessIdx)
            {
                // Evaluate the current roughness value
                float currentRoughness = currentRoughnessIdx / (float)generationParameters.outputHeight;

                // Loop through all the angle values that we need to process
                for (int currentAngleIdx = 0; currentAngleIdx < generationParameters.outputWidth; ++currentAngleIdx)
                {
                    // Evaluate the current angle value
                    float currentAngle = Mathf.Acos(currentAngleIdx / (float)generationParameters.outputWidth);
                    // Let's compute the view direction
                    Vector3 viewVector = new Vector3(Mathf.Sin(currentAngle), 0, Mathf.Cos(currentAngle));
                    Vector3 incidentVector = -viewVector;

                    // Let's compute the reflected direction
                    Vector3 reflected = incidentVector - 2 * normalVector * Vector3.Dot(incidentVector, normalVector);

                    // Let's compute the local to world matrix 
                    Vector3 localX = new Vector3(1.0f, 0.0f, 0.0f);
                    Vector3 localY = new Vector3();
                    GetLocalFrame(reflected, localX, out localY);

                    // We need to build a table that include the average direction BRDF to define the theta value that implies the cone we are interested in
                    for (int thetaIdx = 0; thetaIdx < generationParameters.angleSubdivision; ++thetaIdx)
                    {
                        // initialize the variable for the integration
                        ggxValues[thetaIdx] = 0.0f;

                        // Compute the current theta value
                        float theta = Mathf.PI * 0.5f * thetaIdx / (float)generationParameters.angleSubdivision;

                        for (int phiIdx = 0; phiIdx < generationParameters.angleSubdivision; ++phiIdx)
                        {
                            // Compute the current phi value
                            float phi = Mathf.PI * 2.0f * phiIdx / (float)generationParameters.angleSubdivision;

                            // Generate the direction in local space (reflected dir space)
                            Vector3 localSampleDir = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(theta));

                            // Move it to world space
                            localSampleDir = localSampleDir.x * localX + localSampleDir.y * localY + localSampleDir.z * reflected;

                            // Compute the half vector
                            Vector3 H = Vector3.Normalize((localSampleDir + viewVector) * 0.5f);
                            ggxValues[thetaIdx] += D_GGXNoPI(Vector3.Dot(H, normalVector), currentRoughness) * Vector3.Dot(localSampleDir, normalVector);
                        }
                        ggxValues[thetaIdx] /= (float)generationParameters.angleSubdivision;
                        ggxHistogram[thetaIdx] = thetaIdx == 0 ? ggxValues[thetaIdx] : (ggxValues[thetaIdx] + ggxHistogram[thetaIdx - 1]);
                    }

                    // Let's look index where we get brdfPercentage
                    for (int thetaIdx = 0; thetaIdx < generationParameters.angleSubdivision; ++thetaIdx)
                    {
                        if ((ggxHistogram[thetaIdx] / ggxHistogram[generationParameters.angleSubdivision - 1]) >= generationParameters.brdfPercentage)
                        {
                            thetaValues[currentAngleIdx + currentRoughnessIdx * generationParameters.outputWidth] = thetaIdx / (float)generationParameters.angleSubdivision;
                            break;
                        }
                    }
                }
            }


            Texture2D ggxThresholds = new Texture2D(generationParameters.outputWidth, generationParameters.outputHeight);
            Color color = new Color();
            for (int hIdx = 0; hIdx < generationParameters.outputHeight; ++hIdx)
            {
                for (int wIdx = 0; wIdx < generationParameters.outputWidth; ++wIdx)
                {
                    color.r = thetaValues[wIdx + hIdx * generationParameters.outputWidth];
                    color.g = thetaValues[wIdx + hIdx * generationParameters.outputWidth];
                    color.b = thetaValues[wIdx + hIdx * generationParameters.outputWidth];
                    color.a = 1.0f;
                    ggxThresholds.SetPixel(wIdx, hIdx, color);
                }
            }
            byte[] bytes = ggxThresholds.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/GGXConeAngle.png", bytes);
        }
    }
}
