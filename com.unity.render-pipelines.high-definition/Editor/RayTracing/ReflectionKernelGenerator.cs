using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class ReflectionKernelGenerator : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

        public class CameraParameters
        {
            public int width;
            public int height;
            public float fov;
            public float aspect;
            public float nearPlane;
            public float farPlane;
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
            CameraParameters cameraParameters = new CameraParameters();
            cameraParameters.width = 1980;
            cameraParameters.height = 1080;
            cameraParameters.fov = 70.0f;
            cameraParameters.aspect = 1980.0f / 1080.0f;
            cameraParameters.nearPlane = 0.01f;
            cameraParameters.farPlane = 1000.0f;
            int angleSubdivision = 64;
            float brdfPercentage = 0.7f;
            int outputWidth = 128;
            int outputHeight = 128;
            GenerateTable(cameraParameters, angleSubdivision, brdfPercentage, outputWidth, outputHeight);
        }

        void GetLocalFrame(Vector3 localZ, Vector3 localX, out Vector3 localY)
        {
            localY = Vector3.Cross(localZ, localX);
        }

        bool intersectPlane(Vector3 n, Vector3 p0, Vector3 l0, Vector3 l, ref float t)
        {
            float denom = Vector3.Dot(n, l);
            if (Mathf.Abs(denom) > 1e-6)
            {
                Vector3 p0l0 = p0 - l0;
                t = Vector3.Dot(p0l0, n) / denom;
                return (t >= 0);
            }
            return false;
        }

        public void GenerateTable(CameraParameters cameraParameters, int angleSubdivision, float brdfPercentage, int outputWidth, int outputHeight)
        {
            // This buffer will hold the theta values on which the brdfPercentage criterion is full-filed
            float[] thetaValues = new float[outputWidth * outputHeight];

            // First of all, let's compute the projection matrix
            Matrix4x4 cameraProjection = Matrix4x4.Perspective(cameraParameters.fov, cameraParameters.aspect, cameraParameters.nearPlane, cameraParameters.farPlane);
            Vector3 pointPosition = new Vector3(0.0f, 0.0f, 10.0f);

            // In our case, the view vector is always fixed
            Vector3 viewVector = new Vector3(0, 0, -1.0f);
            Vector3 incidentViewVector = -viewVector;

            // This Buffer will hold the ggx values and histogram used to evaluate the theta angle when brdf percentage is full-filled
            float[] ggxValues = new float[outputWidth];
            float[] ggxHistogram = new float[outputWidth];

            // Let's go through all the roughness inputs
            for (int currentRoughnessIdx = 0; currentRoughnessIdx < outputHeight; ++currentRoughnessIdx)
            {
                // Evaluate the current roughness value
                float currentRoughness = currentRoughnessIdx / (float)outputHeight;

                // Loop through all the angle values that we need to process
                for (int currentAngleIdx = 0; currentAngleIdx < outputWidth; ++currentAngleIdx)
                {
                    // Evaluate the current angle value
                    float currentAngle = 180.0f * Mathf.Acos(currentAngleIdx / (float)outputWidth) / Mathf.PI;

                    // Let's compute the rotated normal (requires a degree angle)
                    Vector3 normalVector = Quaternion.AngleAxis(-currentAngle, Vector3.right) * Vector3.up;

                    // Let's compute the reflected direction
                    Vector3 reflected = incidentViewVector - 2 * normalVector * Vector3.Dot(incidentViewVector, normalVector);

                    // Let's compute the local to world matrix
                    Vector3 localX = new Vector3(1.0f, 0.0f, 0.0f);
                    Vector3 localY = new Vector3();
                    GetLocalFrame(reflected, localX, out localY);

                    // We need to build a table that include the average direction BRDF to define the theta value that implies the cone we are interested in
                    for (int thetaIdx = 0; thetaIdx < angleSubdivision; ++thetaIdx)
                    {
                        // initialize the variable for the integration
                        ggxValues[thetaIdx] = 0.0f;

                        // Compute the current theta value
                        float theta = Mathf.PI * 0.5f * thetaIdx / (float)angleSubdivision;

                        for (int phiIdx = 0; phiIdx < angleSubdivision; ++phiIdx)
                        {
                            // Compute the current phi value
                            float phi = Mathf.PI * 2.0f * phiIdx / (float)angleSubdivision;

                            // Generate the direction in local space (reflected dir space)
                            Vector3 localSampleDir = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(theta));

                            // Move it to world space
                            localSampleDir = localSampleDir.x * localX + localSampleDir.y * localY + localSampleDir.z * reflected;

                            // Compute the half vector
                            Vector3 H = Vector3.Normalize((localSampleDir + viewVector) * 0.5f);
                            ggxValues[thetaIdx] += D_GGXNoPI(Vector3.Dot(H, normalVector), currentRoughness) * Vector3.Dot(localSampleDir, normalVector);
                        }
                        ggxValues[thetaIdx] /= (float)angleSubdivision;
                        ggxHistogram[thetaIdx] = thetaIdx == 0 ? ggxValues[thetaIdx] : (ggxValues[thetaIdx] + ggxHistogram[thetaIdx - 1]);
                    }

                    // Let's look index where we get brdfPercentage
                    for (int thetaIdx = 0; thetaIdx < angleSubdivision; ++thetaIdx)
                    {
                        if ((ggxHistogram[thetaIdx] / ggxHistogram[angleSubdivision - 1]) >= brdfPercentage)
                        {
                            thetaValues[currentAngleIdx + currentRoughnessIdx * outputWidth] = thetaIdx / (float)angleSubdivision;
                            break;
                        }
                    }
                }
            }


            Texture2D ggxThresholds = new Texture2D(outputWidth, outputHeight);
            Color color = new Color();
            for (int hIdx = 0; hIdx < outputHeight; ++hIdx)
            {
                for (int wIdx = 0; wIdx < outputWidth; ++wIdx)
                {
                    color.r = thetaValues[wIdx + hIdx * outputWidth];
                    color.g = thetaValues[wIdx + hIdx * outputWidth];
                    color.b = thetaValues[wIdx + hIdx * outputWidth];
                    color.a = 1.0f;
                    ggxThresholds.SetPixel(wIdx, hIdx, color);
                }
            }
            byte[] bytes = ggxThresholds.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/ThetaValues.png", bytes);


            // This buffer holds the mapping between the (roughness, angle) -> (with, height) (normalized by 32 and clamped)
            float[] outputFilter = new float[outputWidth * outputHeight * 2];

            // We loop through all the roughness values
            for (int currentRoughnessIdx = 0; currentRoughnessIdx < outputHeight; ++currentRoughnessIdx)
            {
                // Loop through all the angle values
                for (int currentAngleIdx = 0; currentAngleIdx < outputWidth; ++currentAngleIdx)
                {
                    // Evaluate the current angle value
                    float currentAngle = 180.0f * Mathf.Acos(currentAngleIdx / (float)outputWidth) / Mathf.PI;

                    // Let's compute the rotated normal (takes degrees)
                    Vector3 normalVector = Quaternion.AngleAxis(-currentAngle, Vector3.right) * Vector3.up;

                    // Grab the current theta that we need to process
                    float theta = thetaValues[currentAngleIdx + currentRoughnessIdx * outputWidth] * Mathf.PI * 0.5f;

                    // Compute the distance between the point and the virtual 1 meter plane
                    float t = 1.0f / Mathf.Cos(theta);

                    // Let's compute the reflected direction
                    Vector3 reflected = incidentViewVector - 2 * normalVector * Vector3.Dot(incidentViewVector, normalVector);

                    // Let's compute the local to world matrix
                    Vector3 localX = new Vector3(1.0f, 0.0f, 0.0f);
                    Vector3 localY = new Vector3();
                    GetLocalFrame(reflected, localX, out localY);

                    int minWidth = int.MaxValue;
                    int minHeight = int.MaxValue;
                    int maxWidth = -int.MaxValue;
                    int maxHeight = -int.MaxValue;

                    // Then we loop through the points that are in the circle that matches the cone angle
                    for (int phiIdx = 0; phiIdx < angleSubdivision; ++phiIdx)
                    {
                        // Compute the current phi value
                        float phi = Mathf.PI * 2.0f * phiIdx / (float)angleSubdivision;

                        // Generate the direction in local space (reflected dir space)
                        Vector3 sampleDir = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(theta));

                        // Move it to world space
                        sampleDir = sampleDir.x * localX + sampleDir.y * localY + sampleDir.z * reflected;

                        // Compute the position on the virtual 1 meter plane
                        Vector3 virtualPoint = t * sampleDir + pointPosition;

                        // Then we need to project it along the reflected direction
                        float tx = -1.0f;
                        if (intersectPlane(normalVector, pointPosition, virtualPoint, -reflected, ref tx))
                        {
                            Vector3 pointProject = virtualPoint - reflected * tx;
                            // Project the point back to near plane.
                            Vector4 pointW = cameraProjection * new Vector4(pointProject.x, pointProject.y, pointProject.z, 1.0f);
                            pointW.x /= pointW.w;
                            pointW.y /= pointW.w;
                            pointW.x = pointW.x * 0.5f + 0.5f;
                            pointW.y = pointW.y * 0.5f + 0.5f;
                            minWidth = Mathf.Min(minWidth, (int)(pointW.x * cameraParameters.width));
                            maxWidth = Mathf.Max(maxWidth, (int)(pointW.x * cameraParameters.width));
                            minHeight = Mathf.Min(minHeight, (int)(pointW.y * cameraParameters.height));
                            maxHeight = Mathf.Max(maxHeight, (int)(pointW.y * cameraParameters.height));
                        }
                    }

                    outputFilter[2 * (currentAngleIdx + currentRoughnessIdx * outputWidth)] = Mathf.Clamp((maxWidth - minWidth) / (float)32.0f, 0.0f, 1.0f);
                    outputFilter[2 * (currentAngleIdx + currentRoughnessIdx * outputWidth) + 1] = Mathf.Clamp((maxHeight - minHeight) / (float)32.0f, 0.0f, 1.0f);
                }
            }

            Texture2D kernelSize = new Texture2D(outputWidth, outputHeight);
            for (int hIdx = 0; hIdx < outputHeight; ++hIdx)
            {
                for (int wIdx = 0; wIdx < outputWidth; ++wIdx)
                {
                    color.r = outputFilter[2 * (wIdx + hIdx * outputWidth)];
                    color.g = outputFilter[2 * (wIdx + hIdx * outputWidth) + 1];
                    color.b = 0.0f;
                    color.a = 1.0f;
                    kernelSize.SetPixel(wIdx, hIdx, color);
                }
            }
            bytes = kernelSize.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/KernelSizes.png", bytes);
        }
    }
}
