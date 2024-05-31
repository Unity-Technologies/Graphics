using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

using RuntimeResources = UnityEngine.Rendering.ProbeReferenceVolume.RuntimeResources;

namespace UnityEngine.Rendering
{
    static class ProbeVolumeConstantRuntimeResources
    {
        static ComputeBuffer m_SkySamplingDirectionsBuffer = null;
        static ComputeBuffer m_AntiLeakDataBuffer = null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GetRuntimeResources(ref RuntimeResources rr)
        {
            rr.SkyPrecomputedDirections = m_SkySamplingDirectionsBuffer;
            rr.QualityLeakReductionData = m_AntiLeakDataBuffer;
        }

        internal static void Initialize()
        {
            if (m_SkySamplingDirectionsBuffer == null)
            {
                k_SkyDirections = GenerateSkyDirections();
                m_SkySamplingDirectionsBuffer = new ComputeBuffer(k_SkyDirections.Length, 3 * sizeof(float));
                m_SkySamplingDirectionsBuffer.SetData(k_SkyDirections);
            }

            if (m_AntiLeakDataBuffer == null)
            {
                m_AntiLeakDataBuffer = new ComputeBuffer(k_AntiLeakData.Length, sizeof(uint));
                m_AntiLeakDataBuffer.SetData(k_AntiLeakData);
            }
        }

        public static Vector3[] GetSkySamplingDirections()
        {
            return k_SkyDirections;
        }

        internal static void Cleanup()
        {
            CoreUtils.SafeRelease(m_SkySamplingDirectionsBuffer);
            m_SkySamplingDirectionsBuffer = null;

            CoreUtils.SafeRelease(m_AntiLeakDataBuffer);
            m_AntiLeakDataBuffer = null;
        }

        #region Sky Directions Buffer generator
        const int NB_SKY_PRECOMPUTED_DIRECTIONS = 255;
        static Vector3[] k_SkyDirections = new Vector3[NB_SKY_PRECOMPUTED_DIRECTIONS];

        static Vector3[] GenerateSkyDirections()
        {
            var skyDirections = new Vector3[NB_SKY_PRECOMPUTED_DIRECTIONS];

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
                skyDirections[i] = pointOnSphere;

                phiMax = Mathf.Max(phiMax, phi);
                thetaMax = Mathf.Max(thetaMax, theta);
            }

            return skyDirections;
        }
        #endregion

        #region AntiLeak Buffer generator
        #if UNITY_EDITOR
        static uint3 GetSampleOffset(uint i)
        {
            return new uint3(i, i >> 1, i >> 2) & 1;
        }
        static int GetProbeIndex(int x, int y, int z)
        {
            return x + y * 2 + z * 4;
        }

        static uint BuildFace(int axis, int idx)
        {
            uint mask = 0;
            int[] coords = new int[3];
            coords[axis] = idx;
            for (int i = 0; i < 2; i++)
            {
                coords[(axis + 1) % 3] = i;
                for (int j = 0; j < 2; j++)
                {
                    coords[(axis + 2) % 3] = j;
                    mask = mask | (uint)(1 << GetProbeIndex(coords[0], coords[1], coords[2]));
                }
            }
            return mask;
        }

        static bool TryGetEdge(uint validityMask, uint samplingMask, out uint edge, out uint3 offset)
        {
            for (int i = 0; i < 8; i++)
            {
                if ((validityMask & (1 << i)) == 0)
                    continue;

                uint3 p = GetSampleOffset((uint)i);
                if (p.x == 0)
                {
                    edge = (1u << i) | (1u << GetProbeIndex(1, (int)p.y, (int)p.z));
                    if ((validityMask & edge) == edge && (samplingMask & edge) == 0)
                    {
                        offset = 2 * p;
                        offset.x = 1;
                        return true;
                    }
                }
                if (p.y == 0)
                {
                    edge = (1u << i) | (1u << GetProbeIndex((int)p.x, 1, (int)p.z));
                    if ((validityMask & edge) == edge && (samplingMask & edge) == 0)
                    {
                        offset = 2 * p;
                        offset.y = 1;
                        return true;
                    }
                }
                if (p.z == 0)
                {
                    edge = (1u << i) | (1u << GetProbeIndex((int)p.x, (int)p.y, 1));
                    if ((validityMask & edge) == edge && (samplingMask & edge) == 0)
                    {
                        offset = 2 * p;
                        offset.z = 1;
                        return true;
                    }
                }
            }

            edge = 0;
            offset = 0;
            return false;
        }

        static List<uint3> ComputeMask(uint validityMask)
        {
            List<uint3> samples = new();

            // Cube sample
            if (validityMask == 0 || validityMask == 255)
            {
                samples.Add(1);
                return samples;
            }

            // track which probes are sampled
            uint samplingMask = 0;

            // Find face sample
            for (int i = 0; i < 6; i++)
            {
                int axis = i / 2;
                uint face = BuildFace(axis, i % 2);
                if ((validityMask & face) == face) // all face is valid, sample it
                {
                    uint3 offset = 0;
                    offset[axis] = (i % 2) == 0 ? 0u : 2u;
                    offset[(axis + 1) % 3] = 1;
                    offset[(axis + 2) % 3] = 1;

                    samples.Add(offset);
                    samplingMask = face;
                    break;
                }
            }

            // Find edge samples
            while (true)
            {
                if (!TryGetEdge(validityMask, samplingMask, out uint edge, out uint3 offset))
                    break;

                samples.Add(offset);
                samplingMask |= edge;
            }

            // Find single probe samples
            for (int i = 0; i < 8; i++)
            {
                if (((1 << i) & (validityMask & ~samplingMask)) == 0)
                    continue;
                samples.Add(2 * GetSampleOffset((uint)i));
                samplingMask |= (uint)(1 << i);
            }

            return samples;
        }

        static uint PackSamplingDir(uint val)
        {
            // On a single axis there is up to 2 probes. A face or edge sample needs to sample in between the probes
            // We encode 0 as sample first probe, 1 as sample between probe, 2 as sample second probe (2 bits)
            // For faster decoding, we use a third bit that reduces ALU in shader
            return /* 2 bits */ (val << 1) |  /* 1 bit */ ((~val & 2) >> 1);
        }

        static uint InvalidSampleMask()
        {
            // This is a special code that results in no sampling in shader without any additional ALU
            return 2 | (2 << 3) | (2 << 6);
        }

        static uint ComputeAntiLeakData(uint validityMask)
        {
            // This may generate more than 3 samples, but we limit to 3
            var samples = ComputeMask(validityMask);
            uint mask = 0;

            for (int i = 0; i < 3; i++)
            {
                uint sampleMask;
                if (i < samples.Count)
                    sampleMask = PackSamplingDir(samples[i].x) | (PackSamplingDir(samples[i].y) << 3) | (PackSamplingDir(samples[i].z) << 6);
                else
                    sampleMask = InvalidSampleMask();

                // 32bits - 9bits per samples (up to 3 samples)
                // Each sample encodes sampling on each axis (3axis * 3bits)
                // See PackSamplingDir for axis encoding
                mask |= sampleMask << (9 * i);
            }

            return mask;
        }

        //[UnityEditor.MenuItem("Edit/Rendering/Global Illumination/Generate AntiLeak Buffer")]
        static uint[] BuildAntiLeakDataArray()
        {
            uint[] antileak = new uint[256];
            for (uint validityMask = 0; validityMask < 256; validityMask++)
                antileak[validityMask] = ComputeAntiLeakData(validityMask);

            string str = "static uint[] k_AntiLeakData = new uint[256] {\n";
            for (int i = 0; i < 16; i++)
            {
                str += "	        ";
                for (int j = 0; j < 16; j++)
                {
                    str += antileak[i * 16 + j] + (j == 15 ? ",\n" : ", ");
                }
            }
            str += "        };";
            Debug.Log(str);

            return antileak;
        }
        #endif

        // This is autogenerated using the MenuItem above -- do not edit by hand
        static uint[] k_AntiLeakData = new uint[256] {
	        38347995, 38347849, 38347852, 38347851, 38347873, 38347865, 38322764, 38322763, 38347876, 38324297, 38347868, 38324299, 38347875, 38324313, 38322780, 38347867,
	        38348041, 38347977, 38408780, 38408779, 38408801, 38408793, 69517900, 69517899, 38408804, 38324425, 38408796, 69519435, 38408803, 69519449, 69517916, 38408795,
	        38348044, 38410313, 38347980, 38410315, 38410337, 38410329, 38322892, 70304331, 38410340, 70305865, 38410332, 70305867, 38410339, 70305881, 70304348, 38410331,
	        38348043, 38410441, 38408908, 38347979, 38322955, 38409817, 69518028, 38322891, 38324491, 70305993, 38409820, 38324427, 38409827, 26351193, 25564764, 38323915,
	        38348065, 38421065, 38421068, 38421067, 38348001, 38421081, 38312161, 38388299, 38421092, 75810889, 38421084, 75810891, 38421091, 75810905, 38388316, 38421083,
	        38348057, 38421193, 38312217, 38416971, 38408929, 38347993, 69507297, 38312153, 38324505, 75811017, 38416988, 26358347, 38416995, 38324441, 69583452, 38320345,
	        38421260, 75896905, 38421196, 75896907, 38410465, 75896921, 38388428, 70369867, 75896932, 70305865, 75896924, 70305867, 75896931, 70305881, 70369884, 75896923,
	        38421259, 75897033, 38417100, 38421195, 38409953, 38410457, 69583564, 38377689, 75811083, 70305993, 75896412, 75811019, 75896419, 70306009, 70107740, 70301913,
	        38348068, 38422601, 38422604, 38422603, 38422625, 38422617, 76595788, 76595787, 38348004, 38310628, 38422620, 38389835, 38422627, 38389849, 76595804, 38422619,
	        38422793, 38422729, 76681804, 76681803, 76681825, 76681817, 69517900, 69517899, 38408932, 38389961, 76681820, 69584971, 76681827, 69584985, 69517916, 76681819,
	        38348060, 38310684, 38422732, 38418507, 38322972, 38418521, 76595916, 25573451, 38410468, 70292196, 38347996, 38310620, 38418531, 70371417, 38322908, 38318812,
	        38422795, 38418633, 76681932, 38422731, 76595979, 76682841, 69518028, 76595915, 38409956, 70371529, 38408924, 38376156, 76682851, 70109273, 69518044, 69513948,
	        38348067, 38310691, 38312227, 38422091, 38422753, 38422105, 76585185, 76661323, 38421220, 75797220, 38422108, 75876427, 38348003, 38310627, 38312163, 38311651,
	        38422809, 38422217, 76585241, 76689995, 76681953, 38422745, 69507297, 76585177, 38417124, 75876553, 76690012, 73779275, 38408931, 38389977, 69507299, 76593369,
	        38421276, 75797276, 38422220, 75905099, 38418657, 75905113, 76661452, 74564171, 75897060, 70292196, 38421212, 75797212, 38410467, 70292195, 38388444, 75805404,
	        38348059, 38310683, 38312219, 38422219, 38322971, 38418649, 25467163, 76650713, 38324507, 26252059, 38417116, 75862748, 38409955, 70371545, 69583580, 38347995,
        };
        #endregion
    }
}
