using System.Runtime.CompilerServices; 
using RuntimeResources = UnityEngine.Rendering.ProbeReferenceVolume.RuntimeResources;

namespace UnityEngine.Rendering
{
    internal static class DynamicSkyPrecomputedDirections
    {
        const int NB_SKY_PRECOMPUTED_DIRECTIONS = 255;

        static ComputeBuffer m_DirectionsBuffer = null;
        static Vector3[] m_Directions = null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GetRuntimeResources(ref RuntimeResources rr)
        {
            rr.SkyPrecomputedDirections = m_DirectionsBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3[] GetPrecomputedDirections()
        {
            return m_Directions;
        }

        internal static void Initialize()
        {
            if (m_DirectionsBuffer == null)
            {
                m_Directions = new Vector3[NB_SKY_PRECOMPUTED_DIRECTIONS];
                m_DirectionsBuffer = new ComputeBuffer(m_Directions.Length, 3 * sizeof(float));

                float sqrtNBpoints = Mathf.Sqrt((float)(NB_SKY_PRECOMPUTED_DIRECTIONS));
                float phi = 0.0f;
                float phiMax = 0.0f;
                float thetaMax = 0.0f;

                // Spiral based sampling on sphere
                // See http://web.archive.org/web/20120331125729/http://www.math.niu.edu/~rusin/known-math/97/spherefaq
                // http://www.math.vanderbilt.edu/saffeb/texts/161.pdf
                for (int i=0; i < NB_SKY_PRECOMPUTED_DIRECTIONS; i++)
                {
                    // theta from 0 to PI
                    // phi from 0 to 2PI
                    float h = -1.0f + (2.0f * i) / (NB_SKY_PRECOMPUTED_DIRECTIONS - 1.0f);
                    float theta = Mathf.Acos(h);
                    if (i == NB_SKY_PRECOMPUTED_DIRECTIONS - 1 || i==0)
                        phi = 0.0f;
                    else
                        phi = phi + 3.6f / sqrtNBpoints * 1.0f / (Mathf.Sqrt(1.0f-h*h));

                    Vector3 pointOnSphere = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(theta));

                    pointOnSphere.Normalize();
                    m_Directions[i] = pointOnSphere;

                    phiMax = Mathf.Max(phiMax, phi);
                    thetaMax = Mathf.Max(thetaMax, theta);
                }
                m_DirectionsBuffer.SetData(m_Directions);
            }
        }

        internal static void Cleanup()
        {
            CoreUtils.SafeRelease(m_DirectionsBuffer);
            m_DirectionsBuffer = null;
            m_Directions = null;
        }
    }
}
