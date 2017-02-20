using System;
using UnityEngine.Rendering;
#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class SubsurfaceScatteringProfile
    {
        public const int numSamples = 7; // Must be an odd number

        [SerializeField, ColorUsage(false, true, 0.05f, 2.0f, 1.0f, 1.0f)]
        public Color   stdDev1;
        [SerializeField, ColorUsage(false, true, 0.05f, 2.0f, 1.0f, 1.0f)]
        public Color   stdDev2;
        [SerializeField]
        public float   lerpWeight;
        [SerializeField]
        public bool    enableTransmission;
        [SerializeField]
        public Vector2 thicknessRemap;
        [SerializeField] [HideInInspector]
        Vector4[]      m_FilterKernel;
        [SerializeField] [HideInInspector]
        Vector3[]      m_HalfRcpVariances;
        [SerializeField] [HideInInspector]
        Vector4        m_HalfRcpWeightedVariances;

        // --- Public Methods ---

        public SubsurfaceScatteringProfile()
        {
            stdDev1            = new Color(0.3f, 0.3f, 0.3f, 0.0f);
            stdDev2            = new Color(0.6f, 0.6f, 0.6f, 0.0f);
            lerpWeight         = 0.5f;
            enableTransmission = false;
            thicknessRemap     = new Vector2(0, 3);

            UpdateKernelAndVarianceData();
        }

        public Vector4[] filterKernel
        {
            // Set via UpdateKernelAndVarianceData().
            get { return m_FilterKernel; }
        }

        public Vector3[] halfRcpVariances
        {
            // Set via UpdateKernelAndVarianceData().
            get { return m_HalfRcpVariances; }
        }

        public Vector4 halfRcpWeightedVariances
        {
            // Set via UpdateKernelAndVarianceData().
            get { return m_HalfRcpWeightedVariances; }
        }

        public void UpdateKernelAndVarianceData()
        {
            if (m_FilterKernel == null)
            {
                m_FilterKernel = new Vector4[numSamples];
            }

            if (m_HalfRcpVariances == null)
            {
                m_HalfRcpVariances = new Vector3[2];
            }

            // Our goal is to blur the image using a filter which is represented
            // as a product of a linear combination of two normalized 1D Gaussians
            // as suggested by Jimenez et al. in "Separable Subsurface Scattering".
            // A normalized (i.e. energy-preserving) 1D Gaussian with the mean of 0
            // is defined as follows: G1(x, v) = exp(-x² / (2 * v)) / sqrt(2 * Pi * v),
            // where 'v' is variance and 'x' is the radial distance from the origin.
            // Using the weight 'w', our 1D and the resulting 2D filters are given as:
            // A1(v1, v2, w, x)    = G1(x, v1) * (1 - w) + G1(r, v2) * w,
            // A2(v1, v2, w, x, y) = A1(v1, v2, w, x) * A1(v1, v2, w, y).
            // The resulting filter function is a non-Gaussian PDF.
            // It is separable by design, but generally not radially symmetric.

            // Find the widest Gaussian across 3 color channels.
            float maxStdDev1 = Mathf.Max(stdDev1.r, stdDev1.g, stdDev1.b);
            float maxStdDev2 = Mathf.Max(stdDev2.r, stdDev2.g, stdDev2.b);

            Vector3 weightSum = new Vector3(0, 0, 0);

            // Importance sample the linear combination of two Gaussians.
            for (uint i = 0; i < numSamples; i++)
            {
                float u   = (i + 0.5f) / numSamples;
                float pos = GaussianCombinationCdfInverse(u, maxStdDev1, maxStdDev2, lerpWeight);
                float pdf = GaussianCombination(pos, maxStdDev1, maxStdDev2, lerpWeight);

                Vector3 val;
                val.x = GaussianCombination(pos, stdDev1.r, stdDev2.r, lerpWeight);
                val.y = GaussianCombination(pos, stdDev1.g, stdDev2.g, lerpWeight);
                val.z = GaussianCombination(pos, stdDev1.b, stdDev2.b, lerpWeight);

                // We do not divide by 'numSamples' since we will renormalize, anyway.
                m_FilterKernel[i].x = val.x * (1 / pdf);
                m_FilterKernel[i].y = val.y * (1 / pdf);
                m_FilterKernel[i].z = val.z * (1 / pdf);
                m_FilterKernel[i].w = pos;

                weightSum.x += m_FilterKernel[i].x;
                weightSum.y += m_FilterKernel[i].y;
                weightSum.z += m_FilterKernel[i].z;
            }

            // Renormalize the weights to conserve energy.
            for (uint i = 0; i < numSamples; i++)
            {
                m_FilterKernel[i].x *= 1 / weightSum.x;
                m_FilterKernel[i].y *= 1 / weightSum.y;
                m_FilterKernel[i].z *= 1 / weightSum.z;
            }

            // Store (1 / (2 * Variance)) per color channel per Gaussian.
            m_HalfRcpVariances[0].x = 0.5f / (stdDev1.r * stdDev1.r);
            m_HalfRcpVariances[0].y = 0.5f / (stdDev1.g * stdDev1.g);
            m_HalfRcpVariances[0].z = 0.5f / (stdDev1.b * stdDev1.b);
            m_HalfRcpVariances[1].x = 0.5f / (stdDev2.r * stdDev2.r);
            m_HalfRcpVariances[1].y = 0.5f / (stdDev2.g * stdDev2.g);
            m_HalfRcpVariances[1].z = 0.5f / (stdDev2.b * stdDev2.b);

            Vector4 weightedStdDev;
            weightedStdDev.x = Mathf.Lerp(stdDev1.r,  stdDev2.r,  lerpWeight);
            weightedStdDev.y = Mathf.Lerp(stdDev1.g,  stdDev2.g,  lerpWeight);
            weightedStdDev.z = Mathf.Lerp(stdDev1.b,  stdDev2.b,  lerpWeight);
            weightedStdDev.w = Mathf.Lerp(maxStdDev1, maxStdDev2, lerpWeight);

            // Store (1 / (2 * WeightedVariance)) per color channel.
            m_HalfRcpWeightedVariances.x = 0.5f / (weightedStdDev.x * weightedStdDev.x);
            m_HalfRcpWeightedVariances.y = 0.5f / (weightedStdDev.y * weightedStdDev.y);
            m_HalfRcpWeightedVariances.z = 0.5f / (weightedStdDev.z * weightedStdDev.z);
            m_HalfRcpWeightedVariances.w = 0.5f / (weightedStdDev.w * weightedStdDev.w);
        }

        // --- Private Methods ---

        static float Gaussian(float x, float stdDev)
        {
            float variance = stdDev * stdDev;
            return Mathf.Exp(-x * x / (2 * variance)) / Mathf.Sqrt(2 * Mathf.PI * variance);
        }

        static float GaussianCombination(float x, float stdDev1, float stdDev2, float lerpWeight)
        {
            return Mathf.Lerp(Gaussian(x, stdDev1), Gaussian(x, stdDev2), lerpWeight);
        }

        static float RationalApproximation(float t)
        {
            // Abramowitz and Stegun formula 26.2.23.
            // The absolute value of the error should be less than 4.5 e-4.
            float[] c = {2.515517f, 0.802853f, 0.010328f};
            float[] d = {1.432788f, 0.189269f, 0.001308f};
            return t - ((c[2] * t + c[1]) * t + c[0]) / (((d[2] * t + d[1]) * t + d[0]) * t + 1.0f);
        }

        // Ref: https://www.johndcook.com/blog/csharp_phi_inverse/
        static float NormalCdfInverse(float p, float stdDev)
        {
            float x;

            if (p < 0.5)
            {
                // F^-1(p) = - G^-1(p)
                x = -RationalApproximation(Mathf.Sqrt(-2.0f * Mathf.Log(p)));
            }
            else
            {
                // F^-1(p) = G^-1(1-p)
                x = RationalApproximation(Mathf.Sqrt(-2.0f * Mathf.Log(1.0f - p)));
            }

            return x * stdDev;
        }

        static float GaussianCombinationCdfInverse(float p, float stdDev1, float stdDev2, float lerpWeight)
        {
            return Mathf.Lerp(NormalCdfInverse(p, stdDev1), NormalCdfInverse(p, stdDev2), lerpWeight);
        }
    }

    public class SubsurfaceScatteringParameters : ScriptableObject
    {
        public const int maxNumProfiles = 8;

        [SerializeField]
        bool                          m_EnableSSS;
        [SerializeField]
        int                           m_NumProfiles;
        [SerializeField]
        int                           m_TransmissionFlags;
        [SerializeField]
        SubsurfaceScatteringProfile[] m_Profiles;
        [SerializeField]
        float[]                       m_ThicknessRemaps;
        [SerializeField]
        Vector4[]                     m_HalfRcpVariancesAndLerpWeights;
        [SerializeField]
        Vector4[]                     m_HalfRcpWeightedVariances;
        [SerializeField]
        Vector4[]                     m_FilterKernels;

        // --- Public Methods ---

        public SubsurfaceScatteringParameters()
        {
            m_EnableSSS   = true;
            m_NumProfiles = 1;
            m_Profiles    = new SubsurfaceScatteringProfile[m_NumProfiles];

            for (int i = 0; i < m_NumProfiles; i++)
            {
                m_Profiles[i] = new SubsurfaceScatteringProfile();
            }

            OnValidate();
        }

        public bool enableSSS {
            // Set via serialization.
            get { return m_EnableSSS; }
        }

        public SubsurfaceScatteringProfile[] profiles {
            // Set via serialization.
            get { return m_Profiles; }
        }

        // Returns a bit mask s.t. the i-th bit indicates whether the i-th profile requires transmittance evaluation.
        // Supplies '_TransmissionFlags' to Lit.hlsl.
        public int transmissionFlags {
            // Set during OnValidate().
            get { return m_TransmissionFlags; }
        }

        // Supplies '_ThicknessRemaps' to Lit.hlsl.
        public float[] thicknessRemaps
        {
            // Set during OnValidate().
            get { return m_ThicknessRemaps; }
        }

        // Supplies '_HalfRcpVariancesAndLerpWeights' to Lit.hlsl.
        public Vector4[] halfRcpVariancesAndLerpWeights {
            // Set during OnValidate().
            get { return m_HalfRcpVariancesAndLerpWeights; }
        }

        // Supplies '_HalfRcpWeightedVariances' to CombineSubsurfaceScattering.shader.
        public Vector4[] halfRcpWeightedVariances {
            // Set during OnValidate().
            get { return m_HalfRcpWeightedVariances; }
        }

        // Supplies '_FilterKernels' to CombineSubsurfaceScattering.shader.
        public Vector4[] filterKernels
        {
            // Set during OnValidate().
            get { return m_FilterKernels; }
        }

        public void OnValidate()
        {
            if (m_Profiles.Length > maxNumProfiles)
            {
                Array.Resize(ref m_Profiles, maxNumProfiles);
            }

            m_NumProfiles       = m_Profiles.Length;
            m_TransmissionFlags = 0;

            if (m_ThicknessRemaps == null)
            {
                m_ThicknessRemaps = new float[maxNumProfiles * 2];
            }

            if (m_HalfRcpVariancesAndLerpWeights == null)
            {
                m_HalfRcpVariancesAndLerpWeights = new Vector4[maxNumProfiles * 2];
            }

            if (m_HalfRcpWeightedVariances == null)
            {
                m_HalfRcpWeightedVariances = new Vector4[maxNumProfiles];
            }

            if (m_FilterKernels == null)
            {
                m_FilterKernels = new Vector4[maxNumProfiles * SubsurfaceScatteringProfile.numSamples];
            }

            Color c = new Color();

            for (int i = 0; i < m_NumProfiles; i++)
            {
                m_TransmissionFlags |= (m_Profiles[i].enableTransmission ? 1 : 0) << i;

                c.r = Mathf.Clamp(m_Profiles[i].stdDev1.r, 0.05f, 2.0f);
                c.g = Mathf.Clamp(m_Profiles[i].stdDev1.g, 0.05f, 2.0f);
                c.b = Mathf.Clamp(m_Profiles[i].stdDev1.b, 0.05f, 2.0f);
                c.a = 0.0f;

                m_Profiles[i].stdDev1 = c;

                c.r = Mathf.Clamp(m_Profiles[i].stdDev2.r, 0.05f, 2.0f);
                c.g = Mathf.Clamp(m_Profiles[i].stdDev2.g, 0.05f, 2.0f);
                c.b = Mathf.Clamp(m_Profiles[i].stdDev2.b, 0.05f, 2.0f);
                c.a = 0.0f;

                m_Profiles[i].stdDev2 = c;

                m_Profiles[i].lerpWeight = Mathf.Clamp01(m_Profiles[i].lerpWeight);

                m_Profiles[i].thicknessRemap.x = Mathf.Clamp(m_Profiles[i].thicknessRemap.x, 0, m_Profiles[i].thicknessRemap.y);
                m_Profiles[i].thicknessRemap.y = Mathf.Max(m_Profiles[i].thicknessRemap.x, m_Profiles[i].thicknessRemap.y);

                m_Profiles[i].UpdateKernelAndVarianceData();
            }

            // Use the updated data to fill the cache.
            for (int i = 0; i < m_NumProfiles; i++)
            {
                m_ThicknessRemaps[2 * i]                      = m_Profiles[i].thicknessRemap.x;
                m_ThicknessRemaps[2 * i + 1]                  = m_Profiles[i].thicknessRemap.y - m_Profiles[i].thicknessRemap.x;
                m_HalfRcpVariancesAndLerpWeights[2 * i]       = m_Profiles[i].halfRcpVariances[0];
                m_HalfRcpVariancesAndLerpWeights[2 * i].w     = 1.0f - m_Profiles[i].lerpWeight;
                m_HalfRcpVariancesAndLerpWeights[2 * i + 1]   = m_Profiles[i].halfRcpVariances[1];
                m_HalfRcpVariancesAndLerpWeights[2 * i + 1].w = m_Profiles[i].lerpWeight;
                m_HalfRcpWeightedVariances[i]                 = m_Profiles[i].halfRcpWeightedVariances;

                for (int j = 0, n = SubsurfaceScatteringProfile.numSamples; j < n; j++)
                {
                    m_FilterKernels[n * i + j] = m_Profiles[i].filterKernel[j];
                }
            }
        }
    }

    public class SubsurfaceScatteringSettings : Singleton<SubsurfaceScatteringSettings>
    {
        SubsurfaceScatteringParameters settings { get; set; }

        public static SubsurfaceScatteringParameters overrideSettings
        {
            get { return instance.settings; }
            set { instance.settings = value; }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SubsurfaceScatteringParameters))]
    public class SubsurfaceScatteringParametersEditor : Editor
    {
        private class Styles
        {
            public readonly GUIContent category              = new GUIContent("Subsurface scattering parameters");
            public readonly GUIContent[] profiles            = new GUIContent[SubsurfaceScatteringParameters.maxNumProfiles] { new GUIContent("Profile #0"), new GUIContent("Profile #1"), new GUIContent("Profile #2"), new GUIContent("Profile #3"), new GUIContent("Profile #4"), new GUIContent("Profile #5"), new GUIContent("Profile #6"), new GUIContent("Profile #7") };
            public readonly GUIContent profilePreview0       = new GUIContent("Profile preview");
            public readonly GUIContent profilePreview1       = new GUIContent("Shows the fraction of light scattered from the source as radius increases to 1.");
            public readonly GUIContent profilePreview2       = new GUIContent("Note that the intensity of the region in the center may be clamped.");
            public readonly GUIContent transmittancePreview0 = new GUIContent("Transmittance preview");
            public readonly GUIContent transmittancePreview1 = new GUIContent("Shows the fraction of light passing through the object as thickness increases to 1.");
            public readonly GUIContent numProfiles           = new GUIContent("Number of profiles");
            public readonly GUIContent profileStdDev1        = new GUIContent("Standard deviation #1", "Determines the shape of the 1st Gaussian filter. Increases the strength and the radius of the blur of the corresponding color channel.");
            public readonly GUIContent profileStdDev2        = new GUIContent("Standard deviation #2", "Determines the shape of the 2nd Gaussian filter. Increases the strength and the radius of the blur of the corresponding color channel.");
            public readonly GUIContent profileLerpWeight     = new GUIContent("Filter interpolation",  "Controls linear interpolation between the two Gaussian filters.");
            public readonly GUIContent profileTransmission   = new GUIContent("Enable transmission",   "Toggles simulation of light passing through thin objects. Depends on the thickness of the material.");
            public readonly GUIContent profileThicknessRemap = new GUIContent("Thickness remap",       "Remaps the thickness parameter from [0, 1] to the desired range.");

            public readonly GUIStyle   centeredMiniBoldLabel = new GUIStyle (GUI.skin.label);
        }

        private static Styles      s_Styles;
        private SerializedProperty m_EnableSSS, m_Profiles, m_NumProfiles;
        private Material           m_ProfileMaterial, m_TransmittanceMaterial;
        private RenderTexture[]    m_ProfileImages, m_TransmittanceImages;

        // --- Public Methods ---

        private static Styles styles
        {
            get
            {
                if (s_Styles == null)
                {
                    s_Styles = new Styles();

                    s_Styles.centeredMiniBoldLabel.alignment = TextAnchor.MiddleCenter;
                    s_Styles.centeredMiniBoldLabel.fontSize  = 10;
                    s_Styles.centeredMiniBoldLabel.fontStyle = FontStyle.Bold;
                }

                return s_Styles;
            }
        }

        void OnEnable()
        {
            m_EnableSSS             = serializedObject.FindProperty("m_EnableSSS");
            m_Profiles              = serializedObject.FindProperty("m_Profiles");
            m_NumProfiles           = m_Profiles.FindPropertyRelative("Array.size");
            m_ProfileMaterial       = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawGaussianProfile");
            m_TransmittanceMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawTransmittanceGraph");
            m_ProfileImages         = new RenderTexture[SubsurfaceScatteringParameters.maxNumProfiles];
            m_TransmittanceImages   = new RenderTexture[SubsurfaceScatteringParameters.maxNumProfiles];

            for (int i = 0; i < SubsurfaceScatteringParameters.maxNumProfiles; i++)
            {
                m_ProfileImages[i]       = new RenderTexture(256, 256, 0, RenderTextureFormat.DefaultHDR);
                m_TransmittanceImages[i] = new RenderTexture(16,  256, 0, RenderTextureFormat.DefaultHDR);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            // Validate the data before displaying it.
            ((SubsurfaceScatteringParameters)serializedObject.targetObject).OnValidate();

            EditorGUILayout.LabelField(styles.category, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_EnableSSS);
            EditorGUILayout.PropertyField(m_NumProfiles, styles.numProfiles);
            EditorGUILayout.PropertyField(m_Profiles);

            if (m_Profiles.isExpanded)
            {
                EditorGUI.indentLevel++;

                for (int i = 0, n = Math.Min(m_Profiles.arraySize, SubsurfaceScatteringParameters.maxNumProfiles); i < n; i++)
                {
                    SerializedProperty profile = m_Profiles.GetArrayElementAtIndex(i);
			        EditorGUILayout.PropertyField(profile, styles.profiles[i]);

                    if (profile.isExpanded)
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty profileStdDev1        = profile.FindPropertyRelative("stdDev1");
                        SerializedProperty profileStdDev2        = profile.FindPropertyRelative("stdDev2");
                        SerializedProperty profileLerpWeight     = profile.FindPropertyRelative("lerpWeight");
                        SerializedProperty profileTransmission   = profile.FindPropertyRelative("enableTransmission");
                        SerializedProperty profileThicknessRemap = profile.FindPropertyRelative("thicknessRemap");

                        EditorGUILayout.PropertyField(profileStdDev1,      styles.profileStdDev1);
                        EditorGUILayout.PropertyField(profileStdDev2,      styles.profileStdDev2);
                        EditorGUILayout.PropertyField(profileLerpWeight,   styles.profileLerpWeight);
                        EditorGUILayout.PropertyField(profileTransmission, styles.profileTransmission);

                        Vector2 thicknessRemap = profileThicknessRemap.vector2Value;
                        EditorGUILayout.LabelField("Min thickness: ", thicknessRemap.x.ToString());
                        EditorGUILayout.LabelField("Max thickness: ", thicknessRemap.y.ToString());
                        EditorGUILayout.MinMaxSlider(styles.profileThicknessRemap, ref thicknessRemap.x, ref thicknessRemap.y, 0, 10);
                        profileThicknessRemap.vector2Value = thicknessRemap;

                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField(styles.profilePreview0, styles.centeredMiniBoldLabel);
                        EditorGUILayout.LabelField(styles.profilePreview1, EditorStyles.centeredGreyMiniLabel);
                        EditorGUILayout.LabelField(styles.profilePreview2, EditorStyles.centeredGreyMiniLabel);
                        EditorGUILayout.Space();

                        // Draw the profile.
                        m_ProfileMaterial.SetColor("_StdDev1",    profileStdDev1.colorValue);
                        m_ProfileMaterial.SetColor("_StdDev2",    profileStdDev2.colorValue);
                        m_ProfileMaterial.SetFloat("_LerpWeight", profileLerpWeight.floatValue);
                        EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(256, 256), m_ProfileImages[i], m_ProfileMaterial, ScaleMode.ScaleToFit, 1.0f);

                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField(styles.transmittancePreview0, styles.centeredMiniBoldLabel);
                        EditorGUILayout.LabelField(styles.transmittancePreview1, EditorStyles.centeredGreyMiniLabel);
                        EditorGUILayout.Space();

                        // Draw the transmittance graph.
                        m_TransmittanceMaterial.SetColor("_StdDev1",         profileStdDev1.colorValue);
                        m_TransmittanceMaterial.SetColor("_StdDev2",         profileStdDev2.colorValue);
                        m_TransmittanceMaterial.SetFloat("_LerpWeight",      profileLerpWeight.floatValue);
                        m_TransmittanceMaterial.SetVector("_ThicknessRemap", profileThicknessRemap.vector2Value);
                        EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(16, 16), m_TransmittanceImages[i], m_TransmittanceMaterial, ScaleMode.ScaleToFit, 16.0f);

                        EditorGUILayout.Space();

                        EditorGUI.indentLevel--;
                    }
		        }

                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                // Serialization does not invoke setters, but does call OnValidate().
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif
}
