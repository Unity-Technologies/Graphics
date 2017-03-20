using System;
#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class SubsurfaceScatteringProfile : ScriptableObject
    {
        public const int numSamples = 11; // Must be an odd number

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
            thicknessRemap     = new Vector2(0, 1);

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
            if (m_FilterKernel == null || m_FilterKernel.Length != numSamples)
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
            // is defined as follows: G1(x, v) = exp(-x * x / (2 * v)) / sqrt(2 * Pi * v),
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

    [Serializable]
    public class SubsurfaceScatteringSettings
    {
        public enum TexturingMode : int { PreScatter = 0, PostScatter = 1, PreAndPostScatter = 2, MaxValue = 2 };

        public const int maxNumProfiles = 8;

        public int                           numProfiles;
        public TexturingMode                 texturingMode;
        public int                           transmissionFlags;
        public SubsurfaceScatteringProfile[] profiles;
        public float[]                       thicknessRemaps;
        public Vector4[]                     halfRcpVariancesAndLerpWeights;
        public Vector4[]                     halfRcpWeightedVariances;
        public Vector4[]                     filterKernels;

        private static SubsurfaceScatteringSettings s_Instance       = null; // Singleton
        private static SubsurfaceScatteringProfile  s_DefaultProfile = null;

        // --- Public Methods ---

        public  static SubsurfaceScatteringSettings instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new SubsurfaceScatteringSettings();
                    s_Instance.CreateProfiles();
                }
                return s_Instance;
            }
        }


        public SubsurfaceScatteringSettings()
        {
            numProfiles                    = 1;
            texturingMode                  = TexturingMode.PreScatter;
            profiles                       = null;
            thicknessRemaps                = null;
            halfRcpVariancesAndLerpWeights = null;
            halfRcpWeightedVariances       = null;
            filterKernels                  = null;
        }

        public void OnValidate()
        {
            if (profiles == null)
            {
                // It will be called during the initialization of the HDRenderPipeline.
                CreateProfiles();
            }

            numProfiles = Math.Max(1, Math.Min(profiles.Length, maxNumProfiles));

            if (profiles.Length != numProfiles)
            {
                Array.Resize(ref profiles, numProfiles);
            }

            for (int i = 0; i < numProfiles; i++)
            {
                if (profiles[i] == null)
                {
                    // No invalid/empty assets allowed!
                    profiles[i] = defaultProfile;
                }
            }

            texturingMode = (TexturingMode)Math.Max(0, Math.Min((int)texturingMode, (int)TexturingMode.MaxValue));

            if (thicknessRemaps == null || thicknessRemaps.Length != (maxNumProfiles * 2))
            {
                thicknessRemaps = new float[maxNumProfiles * 2];
            }

            if (halfRcpVariancesAndLerpWeights == null || halfRcpVariancesAndLerpWeights.Length != (maxNumProfiles * 2))
            {
                halfRcpVariancesAndLerpWeights = new Vector4[maxNumProfiles * 2];
            }

            if (halfRcpWeightedVariances == null || halfRcpWeightedVariances.Length != maxNumProfiles)
            {
                halfRcpWeightedVariances = new Vector4[maxNumProfiles];
            }

            if (filterKernels == null || filterKernels.Length != (maxNumProfiles * SubsurfaceScatteringProfile.numSamples))
            {
                filterKernels = new Vector4[maxNumProfiles * SubsurfaceScatteringProfile.numSamples];
            }

            transmissionFlags = 0;
            Color c = new Color();

            for (int i = 0; i < numProfiles; i++)
            {
                transmissionFlags |= (profiles[i].enableTransmission ? 1 : 0) << i;

                c.r = Mathf.Clamp(profiles[i].stdDev1.r, 0.05f, 2.0f);
                c.g = Mathf.Clamp(profiles[i].stdDev1.g, 0.05f, 2.0f);
                c.b = Mathf.Clamp(profiles[i].stdDev1.b, 0.05f, 2.0f);
                c.a = 0.0f;

                profiles[i].stdDev1 = c;

                c.r = Mathf.Clamp(profiles[i].stdDev2.r, 0.05f, 2.0f);
                c.g = Mathf.Clamp(profiles[i].stdDev2.g, 0.05f, 2.0f);
                c.b = Mathf.Clamp(profiles[i].stdDev2.b, 0.05f, 2.0f);
                c.a = 0.0f;

                profiles[i].stdDev2 = c;

                profiles[i].lerpWeight = Mathf.Clamp01(profiles[i].lerpWeight);

                profiles[i].thicknessRemap.x = Mathf.Clamp(profiles[i].thicknessRemap.x, 0, profiles[i].thicknessRemap.y);
                profiles[i].thicknessRemap.y = Mathf.Max(profiles[i].thicknessRemap.x, profiles[i].thicknessRemap.y);

                profiles[i].UpdateKernelAndVarianceData();
            }

            // Use the updated data to fill the cache.
            for (int i = 0; i < numProfiles; i++)
            {
                thicknessRemaps[2 * i]                      = profiles[i].thicknessRemap.x;
                thicknessRemaps[2 * i + 1]                  = profiles[i].thicknessRemap.y - profiles[i].thicknessRemap.x;
                halfRcpVariancesAndLerpWeights[2 * i]       = profiles[i].halfRcpVariances[0];
                halfRcpVariancesAndLerpWeights[2 * i].w     = 1.0f - profiles[i].lerpWeight;
                halfRcpVariancesAndLerpWeights[2 * i + 1]   = profiles[i].halfRcpVariances[1];
                halfRcpVariancesAndLerpWeights[2 * i + 1].w = profiles[i].lerpWeight;
                halfRcpWeightedVariances[i]                 = profiles[i].halfRcpWeightedVariances;

                for (int j = 0, n = SubsurfaceScatteringProfile.numSamples; j < n; j++)
                {
                    filterKernels[n * i + j] = profiles[i].filterKernel[j];
                }
            }
        }

        // --- Private Methods ---

        private static SubsurfaceScatteringProfile defaultProfile
        {
            get
            {
                if (s_DefaultProfile == null)
                {
                    s_DefaultProfile = ScriptableObject.CreateInstance<SubsurfaceScatteringProfile>();
                    AssetDatabase.CreateAsset(s_DefaultProfile, "Assets/ScriptableRenderPipeline/HDRenderPipeline/Default SSS Profile.asset");
                    AssetDatabase.SaveAssets();
                }
                return s_DefaultProfile;
            }
        }

        // Limitation of Unity - cannot create assets in the constructor.
        public void CreateProfiles()
        {
            profiles = new SubsurfaceScatteringProfile[numProfiles];

            for (int i = 0; i < numProfiles; i++)
            {
                profiles[i] = defaultProfile;
            }
        }
    }

#if UNITY_EDITOR
    public class SubsurfaceScatteringProfileFactory
    {
        [MenuItem("Assets/Create/Subsurface Scattering Profile", priority = 666)]
        static void MenuCreateSubsurfaceScatteringProfile()
        {
            Texture2D icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
                ScriptableObject.CreateInstance<DoCreateSubsurfaceScatteringProfile>(),
                    "New SSS Profile.asset", icon, null);
        }

        public static SubsurfaceScatteringProfile CreateSssProfileAtPath(string path)
        {
            var profile  = ScriptableObject.CreateInstance<SubsurfaceScatteringProfile>();
            profile.name = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }
    }

    class DoCreateSubsurfaceScatteringProfile : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var profiles = SubsurfaceScatteringProfileFactory.CreateSssProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profiles);
        }
    }

    [CustomEditor(typeof(SubsurfaceScatteringProfile))]
    public class SubsurfaceScatteringProfileEditor : Editor {
        private class Styles
        {
            public readonly GUIContent sssProfilePreview0        = new GUIContent("Profile preview");
            public readonly GUIContent sssProfilePreview1        = new GUIContent("Shows the fraction of light scattered from the source as radius increases to 1.");
            public readonly GUIContent sssProfilePreview2        = new GUIContent("Note that the intensity of the region in the center may be clamped.");
            public readonly GUIContent sssTransmittancePreview0  = new GUIContent("Transmittance preview");
            public readonly GUIContent sssTransmittancePreview1  = new GUIContent("Shows the fraction of light passing through the object as thickness increases to 1.");
            public readonly GUIContent sssProfileStdDev1         = new GUIContent("Standard deviation #1", "Determines the shape of the 1st Gaussian filter. Increases the strength and the radius of the blur of the corresponding color channel.");
            public readonly GUIContent sssProfileStdDev2         = new GUIContent("Standard deviation #2", "Determines the shape of the 2nd Gaussian filter. Increases the strength and the radius of the blur of the corresponding color channel.");
            public readonly GUIContent sssProfileLerpWeight      = new GUIContent("Filter interpolation", "Controls linear interpolation between the two Gaussian filters.");
            public readonly GUIContent sssProfileTransmission    = new GUIContent("Enable transmission", "Toggles simulation of light passing through thin objects. Depends on the thickness of the material.");
            public readonly GUIContent sssProfileThicknessRemap  = new GUIContent("Thickness remap", "Remaps the thickness parameter from [0, 1] to the desired range.");

            public readonly GUIStyle   centeredMiniBoldLabel     = new GUIStyle(GUI.skin.label);

            public Styles()
            {
                centeredMiniBoldLabel.alignment = TextAnchor.MiddleCenter;
                centeredMiniBoldLabel.fontSize  = 10;
                centeredMiniBoldLabel.fontStyle = FontStyle.Bold;
            }
        }

        private static Styles styles
        {
            get
            {
                if (s_Styles == null)
                {
                    s_Styles = new Styles();
                }
                return s_Styles;
            }
        }

        private static Styles      s_Styles = null;

        private RenderTexture      m_ProfileImage,   m_TransmittanceImage;
        private Material           m_ProfileMaterial, m_TransmittanceMaterial;
        private SerializedProperty m_Profile, m_ProfileStdDev1, m_ProfileStdDev2,
                                   m_ProfileLerpWeight, m_ProfileTransmission,
                                   m_ProfileThicknessRemap;

        void OnEnable()
        {
            m_ProfileStdDev1        = serializedObject.FindProperty("stdDev1");
            m_ProfileStdDev2        = serializedObject.FindProperty("stdDev2");
            m_ProfileLerpWeight     = serializedObject.FindProperty("lerpWeight");
            m_ProfileTransmission   = serializedObject.FindProperty("enableTransmission");
            m_ProfileThicknessRemap = serializedObject.FindProperty("thicknessRemap");

            m_ProfileMaterial       = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawGaussianProfile");
            m_TransmittanceMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawTransmittanceGraph");

            m_ProfileImage          = new RenderTexture(256, 256, 0, RenderTextureFormat.DefaultHDR);
            m_TransmittanceImage    = new RenderTexture( 16, 256, 0, RenderTextureFormat.DefaultHDR);
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.PropertyField(m_ProfileStdDev1,      styles.sssProfileStdDev1);
                EditorGUILayout.PropertyField(m_ProfileStdDev2,      styles.sssProfileStdDev2);
                EditorGUILayout.PropertyField(m_ProfileLerpWeight,   styles.sssProfileLerpWeight);
                EditorGUILayout.PropertyField(m_ProfileTransmission, styles.sssProfileTransmission);

                Vector2 thicknessRemap = m_ProfileThicknessRemap.vector2Value;
                EditorGUILayout.LabelField("Min thickness: ", thicknessRemap.x.ToString());
                EditorGUILayout.LabelField("Max thickness: ", thicknessRemap.y.ToString());
                EditorGUILayout.MinMaxSlider(styles.sssProfileThicknessRemap, ref thicknessRemap.x, ref thicknessRemap.y, 0, 10);
                m_ProfileThicknessRemap.vector2Value = thicknessRemap;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(styles.sssProfilePreview0, styles.centeredMiniBoldLabel);
                EditorGUILayout.LabelField(styles.sssProfilePreview1, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField(styles.sssProfilePreview2, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space();
            }

            // Draw the profile.
            m_ProfileMaterial.SetColor("_StdDev1",    m_ProfileStdDev1.colorValue);
            m_ProfileMaterial.SetColor("_StdDev2",    m_ProfileStdDev2.colorValue);
            m_ProfileMaterial.SetFloat("_LerpWeight", m_ProfileLerpWeight.floatValue);
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(256, 256), m_ProfileImage, m_ProfileMaterial, ScaleMode.ScaleToFit, 1.0f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(styles.sssTransmittancePreview0, styles.centeredMiniBoldLabel);
            EditorGUILayout.LabelField(styles.sssTransmittancePreview1, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            // Draw the transmittance graph.
            m_TransmittanceMaterial.SetColor("_StdDev1",         m_ProfileStdDev1.colorValue);
            m_TransmittanceMaterial.SetColor("_StdDev2",         m_ProfileStdDev2.colorValue);
            m_TransmittanceMaterial.SetFloat("_LerpWeight",      m_ProfileLerpWeight.floatValue);
            m_TransmittanceMaterial.SetVector("_ThicknessRemap", m_ProfileThicknessRemap.vector2Value);
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(16, 16), m_TransmittanceImage, m_TransmittanceMaterial, ScaleMode.ScaleToFit, 16.0f);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                // Validate each individual asset and update caches.
                SubsurfaceScatteringSettings.instance.OnValidate();
            }
        }
    }
#endif
}
