using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class SubsurfaceScatteringProfile : ScriptableObject
    {
        public enum TexturingMode : int { PreAndPostScatter = 0, PostScatter = 1 };

        public const int numSamples = 11; // Must be an odd number

        [ColorUsage(false, true, 0.05f, 2.0f, 1.0f, 1.0f)]
        public Color         stdDev1;
        [ColorUsage(false, true, 0.05f, 2.0f, 1.0f, 1.0f)]
        public Color         stdDev2;
        public float         lerpWeight;
        public TexturingMode texturingMode;
        public bool          enableTransmission;
        public Vector2       thicknessRemap;
        [HideInInspector]
        public int           settingsIndex;
        [SerializeField]
        Vector4[]            m_FilterKernel;
        [SerializeField]
        Vector3[]            m_HalfRcpVariances;
        [SerializeField]
        Vector4              m_HalfRcpWeightedVariances;

        // --- Public Methods ---

        public SubsurfaceScatteringProfile()
        {
            stdDev1            = new Color(0.3f, 0.3f, 0.3f, 0.0f);
            stdDev2            = new Color(0.6f, 0.6f, 0.6f, 0.0f);
            lerpWeight         = 0.5f;
            texturingMode      = TexturingMode.PreAndPostScatter;
            enableTransmission = false;
            thicknessRemap     = new Vector2(0, 1);
            settingsIndex      = SubsurfaceScatteringSettings.neutralProfileID; // Updated by SubsurfaceScatteringSettings.OnValidate() once assigned

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
        public const int maxNumProfiles   = 8;
        public const int neutralProfileID = 7;

        public int                           numProfiles;
        public SubsurfaceScatteringProfile[] profiles;
        // Below is the cache filled during OnValidate().
        [NonSerialized] public int           texturingModeFlags; // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
        [NonSerialized] public int           transmissionFlags;  // 1 bit/profile; 0 = inf. thick, 1 = supports transmission
        [NonSerialized] public float[]       thicknessRemaps;
        [NonSerialized] public Vector4[]     halfRcpVariancesAndLerpWeights;
        [NonSerialized] public Vector4[]     halfRcpWeightedVariances;
        [NonSerialized] public Vector4[]     filterKernels;

        // --- Public Methods ---

        public SubsurfaceScatteringSettings()
        {
            numProfiles                    = 1;
            profiles                       = new SubsurfaceScatteringProfile[numProfiles];
            profiles[0]                    = null;
            texturingModeFlags             = 0;
            transmissionFlags              = 0;
            thicknessRemaps                = null;
            halfRcpVariancesAndLerpWeights = null;
            halfRcpWeightedVariances       = null;
            filterKernels                  = null;
        }

        public void OnValidate()
        {
            // Reserve one slot for the neutral profile.
            numProfiles = Math.Min(profiles.Length, maxNumProfiles - 1);

            if (profiles.Length != numProfiles)
            {
                Array.Resize(ref profiles, numProfiles);
            }

            for (int i = 0; i < numProfiles; i++)
            {
                if (profiles[i] != null)
                {
                    // Assign the profile IDs.
                    profiles[i].settingsIndex = i;
                }
            }

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

            Color c = new Color();

            for (int i = 0; i < numProfiles; i++)
            {
                // Skip unassigned profiles.
                if (profiles[i] == null) continue;

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

            texturingModeFlags = 0;
            transmissionFlags  = 0;

            // Use the updated data to fill the cache.
            for (int i = 0; i < numProfiles; i++)
            {
                // Skip unassigned profiles.
                if (profiles[i] == null) continue;

                texturingModeFlags |= ((int)profiles[i].texturingMode) << i;
                transmissionFlags  |= (profiles[i].enableTransmission ? 1 : 0) << i;

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

            // Fill the neutral profile.
            {
                int i = neutralProfileID;

                halfRcpWeightedVariances[i] = Vector4.one;

                for (int j = 0, n = SubsurfaceScatteringProfile.numSamples; j < n; j++)
                {
                    filterKernels[n * i + j]   = Vector4.one;
                    filterKernels[n * i + j].w = 0.0f;
                }
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
    public class SubsurfaceScatteringProfileEditor : Editor
    {
        private class Styles
        {
            public readonly GUIContent   sssProfilePreview0       = new GUIContent("Profile preview");
            public readonly GUIContent   sssProfilePreview1       = new GUIContent("Shows the fraction of light scattered from the source as radius increases to 1.");
            public readonly GUIContent   sssProfilePreview2       = new GUIContent("Note that the intensity of the region in the center may be clamped.");
            public readonly GUIContent   sssTransmittancePreview0 = new GUIContent("Transmittance preview");
            public readonly GUIContent   sssTransmittancePreview1 = new GUIContent("Shows the fraction of light passing through the object as thickness increases to 1.");
            public readonly GUIContent   sssProfileStdDev1        = new GUIContent("Standard deviation #1", "Determines the shape of the 1st Gaussian filter. Increases the strength and the radius of the blur of the corresponding color channel.");
            public readonly GUIContent   sssProfileStdDev2        = new GUIContent("Standard deviation #2", "Determines the shape of the 2nd Gaussian filter. Increases the strength and the radius of the blur of the corresponding color channel.");
            public readonly GUIContent   sssProfileLerpWeight     = new GUIContent("Filter interpolation", "Controls linear interpolation between the two Gaussian filters.");
            public readonly GUIContent   sssTexturingMode         = new GUIContent("Texturing mode", "Specifies when the diffuse texture should be applied.");
            public readonly GUIContent[] sssTexturingModeOptions  = new GUIContent[2]
            {
                new GUIContent("Pre- and post-scatter", "Texturing is performed during both the lighting and the SSS passes. Slightly blurs the diffuse texture. Choose this mode if your diffuse texture contains little to no SSS lighting."),
                new GUIContent("Post-scatter", "Texturing is performed only during the SSS pass. Effectively preserves the sharpness of the diffuse texture. Choose this mode if your diffuse texture already contains SSS lighting (e.g. a photo of skin).")
            };
            public readonly GUIContent   sssProfileTransmission   = new GUIContent("Enable transmission", "Toggles simulation of light passing through thin objects. Depends on the thickness of the material.");
            public readonly GUIContent   sssProfileThicknessRemap = new GUIContent("Thickness remap", "Remaps the thickness parameter from [0, 1] to the desired range.");

            public readonly GUIStyle     centeredMiniBoldLabel    = new GUIStyle(GUI.skin.label);

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

        private RenderTexture      m_ProfileImage, m_TransmittanceImage;
        private Material           m_ProfileMaterial, m_TransmittanceMaterial;
        private SerializedProperty m_StdDev1, m_StdDev2, m_LerpWeight,
                                   m_TexturingMode, m_Transmission, m_ThicknessRemap;

        void OnEnable()
        {
            m_StdDev1        = serializedObject.FindProperty("stdDev1");
            m_StdDev2        = serializedObject.FindProperty("stdDev2");
            m_LerpWeight     = serializedObject.FindProperty("lerpWeight");
            m_TexturingMode  = serializedObject.FindProperty("texturingMode");
            m_Transmission   = serializedObject.FindProperty("enableTransmission");
            m_ThicknessRemap = serializedObject.FindProperty("thicknessRemap");

            m_ProfileMaterial       = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawGaussianProfile");
            m_TransmittanceMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawTransmittanceGraph");

            m_ProfileImage          = new RenderTexture(256, 256, 0, RenderTextureFormat.DefaultHDR);
            m_TransmittanceImage    = new RenderTexture(16, 256, 0, RenderTextureFormat.DefaultHDR);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.PropertyField(m_StdDev1,      styles.sssProfileStdDev1);
                EditorGUILayout.PropertyField(m_StdDev2,      styles.sssProfileStdDev2);
                EditorGUILayout.PropertyField(m_LerpWeight,   styles.sssProfileLerpWeight);
                m_TexturingMode.intValue = EditorGUILayout.Popup(styles.sssTexturingMode, m_TexturingMode.intValue, styles.sssTexturingModeOptions);
                EditorGUILayout.PropertyField(m_Transmission, styles.sssProfileTransmission);

                Vector2 thicknessRemap = m_ThicknessRemap.vector2Value;
                EditorGUILayout.LabelField("Min thickness: ", thicknessRemap.x.ToString());
                EditorGUILayout.LabelField("Max thickness: ", thicknessRemap.y.ToString());
                EditorGUILayout.MinMaxSlider(styles.sssProfileThicknessRemap, ref thicknessRemap.x, ref thicknessRemap.y, 0, 10);
                m_ThicknessRemap.vector2Value = thicknessRemap;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(styles.sssProfilePreview0, styles.centeredMiniBoldLabel);
                EditorGUILayout.LabelField(styles.sssProfilePreview1, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField(styles.sssProfilePreview2, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space();
            }

            // Draw the profile.
            m_ProfileMaterial.SetColor("_StdDev1",    m_StdDev1.colorValue);
            m_ProfileMaterial.SetColor("_StdDev2",    m_StdDev2.colorValue);
            m_ProfileMaterial.SetFloat("_LerpWeight", m_LerpWeight.floatValue);
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(256, 256), m_ProfileImage, m_ProfileMaterial, ScaleMode.ScaleToFit, 1.0f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(styles.sssTransmittancePreview0, styles.centeredMiniBoldLabel);
            EditorGUILayout.LabelField(styles.sssTransmittancePreview1, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            // Draw the transmittance graph.
            m_TransmittanceMaterial.SetColor("_StdDev1",         m_StdDev1.colorValue);
            m_TransmittanceMaterial.SetColor("_StdDev2",         m_StdDev2.colorValue);
            m_TransmittanceMaterial.SetFloat("_LerpWeight",      m_LerpWeight.floatValue);
            m_TransmittanceMaterial.SetVector("_ThicknessRemap", m_ThicknessRemap.vector2Value);
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(16, 16), m_TransmittanceImage, m_TransmittanceMaterial, ScaleMode.ScaleToFit, 16.0f);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                // Validate each individual asset and update caches.
                HDRenderPipelineInstance hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipelineInstance;
                hdPipeline.sssSettings.OnValidate();
            }
        }
    }
#endif
}
