using UnityEditor;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class SolidAngleKernelGenerator : MonoBehaviour
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
            public float maxCameraDistance;
            public float maxVirtualPlaneDistance;
            public float planeAngle;
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
            cameraParameters.farPlane = 100.0f;
            cameraParameters.maxVirtualPlaneDistance = 10.0f;
            cameraParameters.maxCameraDistance = 50.0f;
            cameraParameters.planeAngle = 45.0f;
            int angleSubdivision = 16;
            float brdfPercentage = 0.7f;
            int outputWidth = 32;
            int outputHeight = 32;
            int outputDepth = 64;
            GenerateTable(cameraParameters, angleSubdivision, brdfPercentage, outputWidth, outputHeight, outputDepth);
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

        public void GenerateTable(CameraParameters cameraParameters, int angleSubdivision, float brdfPercentage, int outputWidth, int outputHeight, int outputDepth)
        {
            // Allocate our output texture
            Texture3D kernelSize = new Texture3D(outputWidth, outputHeight, outputDepth, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);

            // First of all, let's compute the projection matrix
            Matrix4x4 cameraProjection = Matrix4x4.Perspective(cameraParameters.fov, cameraParameters.aspect, cameraParameters.nearPlane, cameraParameters.farPlane);

            // This buffer holds the mapping between the (rayAngle, solidAngle, distance) -> (with, height) (normalized by 32 and clamped)
            float[] outputFilter = new float[outputWidth * outputHeight * outputDepth * 2];

            // Color used to fill the tex3d
            Color currentColor = new Color();

            // We loop through all the camera distance values
            for (int currentCameraDistanceIdx = 0; currentCameraDistanceIdx < outputDepth; ++currentCameraDistanceIdx)
            {
                // Compute the distance between the plane an the camera
                float cameraDistance = cameraParameters.maxCameraDistance * (currentCameraDistanceIdx + 1) / (float)outputDepth;

                // Compute the position of the point
                Vector3 pointPosition = new Vector3(0.0f, 0.0f, cameraDistance);

                // Loop through the solid angle values
                for (int currentSolidAngleIdx = 0; currentSolidAngleIdx < outputHeight; ++currentSolidAngleIdx)
                {
                    // Evaluate the current angle value
                    float currentSolidAngle = Mathf.PI * 0.5f * currentSolidAngleIdx / (float)outputHeight;

                    // Loop through all the point -> virtual plane distances
                    for (int currentVirtualDistanceIdx = 0; currentVirtualDistanceIdx < outputWidth; ++currentVirtualDistanceIdx)
                    {
                        // Evaluate the current angle value
                        float currentVirtualDistance = cameraParameters.maxVirtualPlaneDistance * currentVirtualDistanceIdx / (float)outputWidth;

                        // Let's compute the rotated normal (takes degrees)
                        Vector3 normalVector = Quaternion.AngleAxis(cameraParameters.planeAngle, Vector3.right) * -Vector3.forward;

                        // Let's compute the local to world matrix
                        Vector3 localX = new Vector3(1.0f, 0.0f, 0.0f);
                        Vector3 localY = new Vector3();
                        GetLocalFrame(normalVector, localX, out localY);

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
                            Vector3 sampleDir = new Vector3(Mathf.Sin(currentSolidAngle) * Mathf.Cos(phi), Mathf.Sin(currentSolidAngle) * Mathf.Sin(phi), Mathf.Cos(currentSolidAngle));

                            // Move it to world space
                            sampleDir = sampleDir.x * localX + sampleDir.y * localY + sampleDir.z * normalVector;

                            // Compute the position on the virtual 1 meter plane
                            Vector3 virtualPoint = currentVirtualDistance * sampleDir + pointPosition;

                            // Then we need to project it along the reflected direction
                            float tx = -1.0f;
                            if (intersectPlane(normalVector, pointPosition, virtualPoint, -normalVector, ref tx))
                            {
                                Vector3 pointProject = virtualPoint - normalVector * tx;
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

                        float kernelSizeX = Mathf.Clamp((maxWidth - minWidth) / (float)32.0f, 0.0f, 1.0f);
                        float kernelSizeY = Mathf.Clamp((maxHeight - minHeight) / (float)32.0f, 0.0f, 1.0f);
                        currentColor.r = kernelSizeX;
                        currentColor.g = kernelSizeY;
                        currentColor.b = 0.0f;
                        currentColor.a = 1.0f;
                        kernelSize.SetPixel(currentVirtualDistanceIdx, currentSolidAngleIdx, currentCameraDistanceIdx, currentColor);
                    }
                }
            }

            // Save our texture to file
            AssetDatabase.CreateAsset(kernelSize, "Assets/ShadowFilterMapping.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
