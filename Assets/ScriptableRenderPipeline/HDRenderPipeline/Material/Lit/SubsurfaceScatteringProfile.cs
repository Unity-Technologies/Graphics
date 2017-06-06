using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public class SssConstants
    {
        public const int SSS_N_PROFILES           = 8;  // Max. number of profiles, including the slot taken by the neutral profile
        public const int SSS_NEUTRAL_PROFILE_ID   = SSS_N_PROFILES - 1; // Does not result in blurring
        public const int SSS_N_SAMPLES_NEAR_FIELD = 55; // Used for extreme close ups; must be a Fibonacci number
        public const int SSS_N_SAMPLES_FAR_FIELD  = 21; // Used at a regular distance; must be a Fibonacci number
        public const int SSS_LOD_THRESHOLD        = 4;  // The LoD threshold of the near-field kernel (in pixels)
        public const int SSS_TRSM_MODE_NONE       = 0;
        public const int SSS_TRSM_MODE_THIN       = 1;
    }

    [Serializable]
    public class SubsurfaceScatteringProfile : ScriptableObject
    {
        public enum TexturingMode    : uint { PreAndPostScatter = 0, PostScatter = 1 };
        public enum TransmissionMode : uint { None = SssConstants.SSS_TRSM_MODE_NONE, ThinObject = SssConstants.SSS_TRSM_MODE_THIN, Regular };

        public Color            surfaceAlbedo;              // Color, 0 to 1
        public Color            volumeAlbedo;               // Color, 0 to 1
        public float            lenVolMeanFreePath;         // Length of the volume mean free path (in millimeters)
        public TexturingMode    texturingMode;
        public TransmissionMode transmissionMode;
        public Vector2          thicknessRemap;             // X = min, Y = max (in millimeters)
        public float            worldScale;                 // Size of the world unit in meters
        [HideInInspector]
        public int              settingsIndex;              // SubsurfaceScatteringSettings.profiles[i]
        [SerializeField]
        Vector3                 m_SurfaceShapeParam;        // RGB = shape parameter: S = 1 / D
        [SerializeField]
        Vector3                 m_VolumeShapeParam;         // RGB = shape parameter: S = 1 / D
        [SerializeField]
        float                   m_ScatteringDistance;       // Filter radius (in millimeters)
        [SerializeField]
        Vector2[]               m_FilterKernelNearField;    // X = radius, Y = reciprocal of the PDF
        [SerializeField]
        Vector2[]               m_FilterKernelFarField;     // X = radius, Y = reciprocal of the PDF

        // --- Public Methods ---

        public SubsurfaceScatteringProfile()
        {
            surfaceAlbedo      = Color.white;
            volumeAlbedo       = Color.white;
            lenVolMeanFreePath = 0.5f;
            texturingMode      = TexturingMode.PreAndPostScatter;
            transmissionMode   = TransmissionMode.None;
            thicknessRemap     = new Vector2(0.0f, 5.0f);
            worldScale         = 1.0f;
            settingsIndex      = SssConstants.SSS_NEUTRAL_PROFILE_ID; // Updated by SubsurfaceScatteringSettings.OnValidate() once assigned

            BuildKernel();
        }

        // Ref: Approximate Reflectance Profiles for Efficient Subsurface Scattering by Pixar.
        public void BuildKernel()
        {
            if (m_FilterKernelNearField == null || m_FilterKernelNearField.Length != SssConstants.SSS_N_SAMPLES_NEAR_FIELD)
            {
                m_FilterKernelNearField = new Vector2[SssConstants.SSS_N_SAMPLES_NEAR_FIELD];
            }

            if (m_FilterKernelFarField == null || m_FilterKernelFarField.Length != SssConstants.SSS_N_SAMPLES_FAR_FIELD)
            {
                m_FilterKernelFarField = new Vector2[SssConstants.SSS_N_SAMPLES_FAR_FIELD];
            }

            m_SurfaceShapeParam = new Vector3();

            // Evaluate the fit for diffuse surface transmission.
            m_SurfaceShapeParam.x = FindFitForS(surfaceAlbedo.r);
            m_SurfaceShapeParam.y = FindFitForS(surfaceAlbedo.g);
            m_SurfaceShapeParam.z = FindFitForS(surfaceAlbedo.b);
            m_VolumeShapeParam.x  = FindFitForS(volumeAlbedo.r);
            m_VolumeShapeParam.y  = FindFitForS(volumeAlbedo.g);
            m_VolumeShapeParam.z  = FindFitForS(volumeAlbedo.b);

            // Compute { 1 / D = S / L } as you can substitute s = 1 / d in all formulas.
            m_SurfaceShapeParam *= 1.0f / lenVolMeanFreePath;
            m_VolumeShapeParam  *= 1.0f / lenVolMeanFreePath;

            // We importance sample the color channel with the highest albedo value,
            // since higher albedo values result in scattering over a larger distance.
            // S(A) is a monotonically decreasing function.
            float s = Mathf.Min(m_SurfaceShapeParam.x, m_SurfaceShapeParam.y, m_SurfaceShapeParam.z);

            // Importance sample the normalized diffusion profile for the computed value of 's'.
            // ------------------------------------------------------------------------------------
            // R(r, s)   = s * (Exp[-r * s] + Exp[-r * s / 3]) / (8 * Pi * r)
            // PDF(r, s) = s * (Exp[-r * s] + Exp[-r * s / 3]) / 4
            // CDF(r, s) = 1 - 1/4 * Exp[-r * s] - 3/4 * Exp[-r * s / 3]
            // ------------------------------------------------------------------------------------

            // Importance sample the near field kernel.
            for (int i = 0; i < SssConstants.SSS_N_SAMPLES_NEAR_FIELD; i++)
            {
                float p = i * (1.0f / SssConstants.SSS_N_SAMPLES_NEAR_FIELD);
                float r = KernelCdfInverse(p, s);

                // N.b.: computation of normalized weights, and multiplication by the surface albedo
                // of the actual geometry is performed at runtime (in the shader).
                m_FilterKernelNearField[i].x = r;
                m_FilterKernelNearField[i].y = 1.0f / KernelPdf(r, s);
            }

            m_ScatteringDistance = m_FilterKernelNearField[SssConstants.SSS_N_SAMPLES_NEAR_FIELD - 1].x;

            // Importance sample the far field kernel.
            for (int i = 0; i < SssConstants.SSS_N_SAMPLES_FAR_FIELD; i++)
            {
                float p = i * (1.0f / SssConstants.SSS_N_SAMPLES_FAR_FIELD);
                float r = KernelCdfInverse(p, s);

                // N.b.: computation of normalized weights, and multiplication by the surface albedo
                // of the actual geometry is performed at runtime (in the shader).
                m_FilterKernelFarField[i].x = r;
                m_FilterKernelFarField[i].y = 1.0f / KernelPdf(r, s);
            }
        }

        public Vector3 surfaceShapeParameter
        {
            // Set in BuildKernel().
            get { return m_SurfaceShapeParam; }
        }

        public Vector3 volumeShapeParameter
        {
            // Set in BuildKernel().
            get { return m_VolumeShapeParam; }
        }

        public float scatteringDistance
        {
            // Set in BuildKernel().
            get { return m_ScatteringDistance; }
        }

        public Vector2[] filterKernelNearField
        {
            // Set in BuildKernel().
            get { return m_FilterKernelNearField; }
        }

        public Vector2[] filterKernelFarField
        {
            // Set in BuildKernel().
            get { return m_FilterKernelFarField; }
        }

        // --- Private Methods ---

        static float FindFitForS(float A)
        {
            return 1.9f - A + 3.5f * (A - 0.8f) * (A - 0.8f);
        }

        static float KernelVal(float r, float s)
        {
            return s * (Mathf.Exp(-r * s) + Mathf.Exp(-r * s * (1.0f / 3.0f))) / (8.0f * Mathf.PI * r);
        }

        // Computes the value of the integrand over a disk: (2 * PI * r) * KernelVal().
        static float KernelValCircle(float r, float s)
        {
            return 0.25f * s * (Mathf.Exp(-r * s) + Mathf.Exp(-r * s * (1.0f / 3.0f)));
        }

        static float KernelPdf(float r, float s)
        {
            return KernelValCircle(r, s);
        }

        static float KernelCdf(float r, float s)
        {
            return 1.0f - 0.25f * Mathf.Exp(-r * s) - 0.75f * Mathf.Exp(-r * s * (1.0f / 3.0f));
        }

        static float KernelCdfDerivative1(float r, float s)
        {
            return 0.25f * s * Mathf.Exp(-r * s) * (1.0f + Mathf.Exp(r * s * (2.0f / 3.0f)));
        }

        static float KernelCdfDerivative2(float r, float s)
        {
            return (-1.0f / 12.0f) * s * s * Mathf.Exp(-r * s) * (3.0f + Mathf.Exp(r * s * (2.0f / 3.0f)));
        }

        // The CDF is not analytically invertible, so we use Halley's Method of root finding.
        // { f(r, s, p) = CDF(r, s) - p = 0 } with the initial guess { r = (10^p - 1) / s }.
        static float KernelCdfInverse(float p, float s)
        {
            // Supply the initial guess.
            float r = (Mathf.Pow(10.0f, p) - 1.0f) / s;
            float t = float.MaxValue;

            while (true)
            {
                float f0 = KernelCdf(r, s) - p;
                float f1 = KernelCdfDerivative1(r, s);
                float f2 = KernelCdfDerivative2(r, s);
                float dr = f0 / (f1 * (1.0f - f0 * f2 / (2.0f * f1 * f1)));

                if (Mathf.Abs(dr) < t)
                {
                    r = r - dr;
                    t = Mathf.Abs(dr);
                }
                else
                {
                    // Converged to the best result.
                    break;
                }
            }

            return r;
        }
    }

    [Serializable]
    public class SubsurfaceScatteringSettings : ISerializationCallbackReceiver
    {
        public int                           numProfiles;               // Excluding the neutral profile
        public SubsurfaceScatteringProfile[] profiles;
        // Below are the cached values.
        [NonSerialized] public uint          texturingModeFlags;        // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
        [NonSerialized] public uint          transmissionFlags;         // 2 bit/profile; 0 = inf. thick, 1 = thin, 2 = regular
        [NonSerialized] public float[]       thicknessRemaps;           // Remap: 0 = start, 1 = end - start
        [NonSerialized] public Vector4[]     surfaceShapeParams;        // RGB = S = 1 / D, A = filter radius
        [NonSerialized] public Vector4[]     volumeShapeParams;         // RGB = S = 1 / D, A = unused
        [NonSerialized] public Vector4[]     volumeAlbedos;             // RGB = color, A = unused
        [NonSerialized] public float[]       worldScales;               // Size of the world unit in meters
        [NonSerialized] public float[]       filterKernelsNearField;    // 0 = radius, 1 = reciprocal of the PDF
        [NonSerialized] public float[]       filterKernelsFarField;     // 0 = radius, 1 = reciprocal of the PDF

        // --- Public Methods ---

        public SubsurfaceScatteringSettings()
        {
            numProfiles            = 1;
            profiles               = new SubsurfaceScatteringProfile[numProfiles];
            profiles[0]            = null;
            texturingModeFlags     = 0;
            transmissionFlags      = 0;
            thicknessRemaps        = null;
            surfaceShapeParams     = null;
            volumeShapeParams      = null;
            filterKernelsNearField = null;
            filterKernelsFarField  = null;

            UpdateCache();
        }

        public void OnValidate()
        {
            // Reserve one slot for the neutral profile.
            numProfiles = Math.Min(profiles.Length, SssConstants.SSS_N_PROFILES - 1);

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

            for (int i = 0; i < numProfiles; i++)
            {
                // Skip unassigned profiles.
                if (profiles[i] == null) continue;

                profiles[i].thicknessRemap.y = Mathf.Max(profiles[i].thicknessRemap.y, 0);
                profiles[i].thicknessRemap.x = Mathf.Clamp(profiles[i].thicknessRemap.x, 0, profiles[i].thicknessRemap.y);
                profiles[i].worldScale       = Mathf.Max(profiles[i].worldScale, 0.001f);

                profiles[i].BuildKernel();
            }

            UpdateCache();
        }

        public void UpdateCache()
        {
            texturingModeFlags = transmissionFlags = 0;

            const int thicknessRemapsLen = SssConstants.SSS_N_PROFILES * 2;
            if (thicknessRemaps == null || thicknessRemaps.Length != thicknessRemapsLen)
            {
                thicknessRemaps = new float[thicknessRemapsLen];
            }

            if (worldScales == null || worldScales.Length != SssConstants.SSS_N_PROFILES)
            {
                worldScales = new float[SssConstants.SSS_N_PROFILES];
            }

            if (surfaceShapeParams == null || surfaceShapeParams.Length != SssConstants.SSS_N_PROFILES)
            {
                surfaceShapeParams = new Vector4[SssConstants.SSS_N_PROFILES];
            }

            if (volumeShapeParams == null || volumeShapeParams.Length != SssConstants.SSS_N_PROFILES)
            {
                volumeShapeParams = new Vector4[SssConstants.SSS_N_PROFILES];
            }

            if (volumeAlbedos == null || volumeAlbedos.Length != SssConstants.SSS_N_PROFILES)
            {
                volumeAlbedos = new Vector4[SssConstants.SSS_N_PROFILES];
            }

            const int filterKernelsNearFieldLen = 2 * SssConstants.SSS_N_PROFILES * SssConstants.SSS_N_SAMPLES_NEAR_FIELD;
            if (filterKernelsNearField == null || filterKernelsNearField.Length != filterKernelsNearFieldLen)
            {
                filterKernelsNearField = new float[filterKernelsNearFieldLen];
            }

            const int filterKernelsFarFieldLen = 2 * SssConstants.SSS_N_PROFILES * SssConstants.SSS_N_SAMPLES_FAR_FIELD;
            if (filterKernelsFarField == null || filterKernelsFarField.Length != filterKernelsFarFieldLen)
            {
                filterKernelsFarField = new float[filterKernelsFarFieldLen];
            }

            for (int i = 0; i < numProfiles; i++)
            {
                // Skip unassigned profiles.
                if (profiles[i] == null) continue;

                Debug.Assert(numProfiles < 16, "Transmission flags (32-bit integer) cannot support more than 16 profiles.");

                texturingModeFlags |= (uint)profiles[i].texturingMode    << i;
                transmissionFlags  |= (uint)profiles[i].transmissionMode << i * 2;

                thicknessRemaps[2 * i]     = profiles[i].thicknessRemap.x;
                thicknessRemaps[2 * i + 1] = profiles[i].thicknessRemap.y - profiles[i].thicknessRemap.x;
                worldScales[i]             = profiles[i].worldScale;
                surfaceShapeParams[i]      = profiles[i].surfaceShapeParameter;
                surfaceShapeParams[i].w    = profiles[i].scatteringDistance;
                volumeShapeParams[i]       = profiles[i].volumeShapeParameter;
                volumeAlbedos[i]           = profiles[i].volumeAlbedo;

                for (int j = 0, n = SssConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
                {
                    filterKernelsNearField[2 * (n * i + j) + 0] = profiles[i].filterKernelNearField[j].x;
                    filterKernelsNearField[2 * (n * i + j) + 1] = profiles[i].filterKernelNearField[j].y;
                }

                for (int j = 0, n = SssConstants.SSS_N_SAMPLES_FAR_FIELD; j < n; j++)
                {
                    filterKernelsFarField[2 * (n * i + j) + 0] = profiles[i].filterKernelFarField[j].x;
                    filterKernelsFarField[2 * (n * i + j) + 1] = profiles[i].filterKernelFarField[j].y;
                }
            }

            // Fill the neutral profile.
            {
                int i = SssConstants.SSS_NEUTRAL_PROFILE_ID;

                surfaceShapeParams[i] = Vector4.zero;
                volumeShapeParams[i]  = Vector4.zero;
                volumeAlbedos[i]      = Vector4.zero;
                worldScales[i]        = 1.0f;

                for (int j = 0, n = SssConstants.SSS_N_SAMPLES_NEAR_FIELD; j < n; j++)
                {
                    filterKernelsNearField[2 * (n * i + j) + 0] = 0.0f;
                    filterKernelsNearField[2 * (n * i + j) + 1] = 1.0f;
                }

                for (int j = 0, n = SssConstants.SSS_N_SAMPLES_FAR_FIELD; j < n; j++)
                {
                    filterKernelsFarField[2 * (n * i + j) + 0] = 0.0f;
                    filterKernelsFarField[2 * (n * i + j) + 1] = 1.0f;
                }
            }
        }

        public void OnBeforeSerialize()
        {
            // No special action required.
        }

        public void OnAfterDeserialize()
        {
            UpdateCache();
        }
    }

#if UNITY_EDITOR
    public class SubsurfaceScatteringProfileFactory
    {
        [MenuItem("Assets/Create/HDRenderPipeline/Subsurface Scattering Profile", priority = 666)]
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
            public readonly GUIContent   sssProfilePreview0           = new GUIContent("Profile Preview");
            public readonly GUIContent   sssProfilePreview1           = new GUIContent("Shows the fraction of light scattered from the source (center).");
            public readonly GUIContent   sssProfilePreview2           = new GUIContent("The distance to the boundary of the image corresponds to the Scattering Distance.");
            public readonly GUIContent   sssProfilePreview3           = new GUIContent("Note that the intensity of pixels around the center may be clipped.");
            public readonly GUIContent   sssTransmittancePreview0     = new GUIContent("Transmittance Preview");
            public readonly GUIContent   sssTransmittancePreview1     = new GUIContent("Shows the fraction of light passing through the object for thickness values from the remap.");
            public readonly GUIContent   sssTransmittancePreview2     = new GUIContent("Can be viewed as a cross section of a slab of material illuminated by white light from the left.");
            public readonly GUIContent   sssProfileSurfaceAlbedo      = new GUIContent("Surface Albedo", "Color which determines the shape of the profile. Alpha is ignored. Typically, it is similar to the diffuse color.");
            public readonly GUIContent   sssProfileVolumeAlbedo       = new GUIContent("Volume Albedo", "Color which tints transmitted light. Alpha is ignored. Typically, it is a more saturated version of the diffuse color.");
            public readonly GUIContent   sssProfileLenVolMeanFreePath = new GUIContent("Volume Mean Free Path", "The length of the volume mean free path (in millimeters) describes the average distance a photon travels within the volume before an extinction event occurs. Determines the effective radius of the filter.");
            public readonly GUIContent   sssProfileScatteringDistance = new GUIContent("Scattering Distance", "Effective radius of the filter (in millimeters). The blur is energy-preserving, so a wide filter results in a large area with small contributions of individual samples. Reducing the distance increases the sharpness of the result.");
            public readonly GUIContent   sssTexturingMode             = new GUIContent("Texturing Mode", "Specifies when the diffuse texture should be applied.");
            public readonly GUIContent[] sssTexturingModeOptions      = new GUIContent[2]
            {
                new GUIContent("Pre- and post-scatter", "Texturing is performed during both the lighting and the SSS passes. Slightly blurs the diffuse texture. Choose this mode if your diffuse texture contains little to no SSS lighting."),
                new GUIContent("Post-scatter",          "Texturing is performed only during the SSS pass. Effectively preserves the sharpness of the diffuse texture. Choose this mode if your diffuse texture already contains SSS lighting (e.g. a photo of skin).")
            };
            public readonly GUIContent   sssProfileTransmissionMode = new GUIContent("Transmission Mode", "Configures the simulation of light passing through thin objects. Depends on the thickness value (which is applied in the normal direction).");
            public readonly GUIContent[] sssTransmissionModeOptions = new GUIContent[3]
            {
                new GUIContent("None",         "Disables transmission. Choose this mode for completely opaque, or very thick translucent objects."),
                new GUIContent("Thin Object",  "Choose this mode for thin objects, such as paper or leaves. Transmitted light reuses the shadowing state of the surface."),
                new GUIContent("Regular",      "Choose this mode for moderately thick objects. For performance reasons, transmitted light ignores occlusion (shadows).")
            };
            public readonly GUIContent   sssProfileMinMaxThickness = new GUIContent("Min-Max Thickness", "Shows the values of the thickness remap below (in millimeters).");
            public readonly GUIContent   sssProfileThicknessRemap  = new GUIContent("Thickness Remap", "Remaps the thickness parameter from [0, 1] to the desired range (in millimeters).");
            public readonly GUIContent   sssProfileWorldScale      = new GUIContent("World Scale", "Size of the world unit in meters.");

            public readonly GUIStyle     centeredMiniBoldLabel     = new GUIStyle(GUI.skin.label);

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
        private SerializedProperty m_LenVolMeanFreePath, m_ScatteringDistance, m_SurfaceAlbedo, m_VolumeAlbedo, m_SurfaceShapeParam, m_VolumeShapeParam,
                                   m_TexturingMode, m_TransmissionMode, m_ThicknessRemap, m_WorldScale;

        void OnEnable()
        {
            m_SurfaceAlbedo         = serializedObject.FindProperty("surfaceAlbedo");
            m_VolumeAlbedo          = serializedObject.FindProperty("volumeAlbedo");
            m_LenVolMeanFreePath    = serializedObject.FindProperty("lenVolMeanFreePath");
            m_ScatteringDistance    = serializedObject.FindProperty("m_ScatteringDistance");
            m_SurfaceShapeParam     = serializedObject.FindProperty("m_SurfaceShapeParam");
            m_VolumeShapeParam      = serializedObject.FindProperty("m_VolumeShapeParam");
            m_TexturingMode         = serializedObject.FindProperty("texturingMode");
            m_TransmissionMode      = serializedObject.FindProperty("transmissionMode");
            m_ThicknessRemap        = serializedObject.FindProperty("thicknessRemap");
            m_WorldScale            = serializedObject.FindProperty("worldScale");

            // These shaders don't need to be reference by RenderPipelineResource as they are not use at runtime
            m_ProfileMaterial       = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawSssProfile");
            m_TransmittanceMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/DrawTransmittanceGraph");

            m_ProfileImage          = new RenderTexture(256, 256, 0, RenderTextureFormat.DefaultHDR);
            m_TransmittanceImage    = new RenderTexture( 16, 256, 0, RenderTextureFormat.DefaultHDR);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.PropertyField(m_SurfaceAlbedo, styles.sssProfileSurfaceAlbedo);
                m_LenVolMeanFreePath.floatValue = EditorGUILayout.Slider(styles.sssProfileLenVolMeanFreePath, m_LenVolMeanFreePath.floatValue, 0.01f, 1.0f);

                GUI.enabled = false;
                EditorGUILayout.PropertyField(m_ScatteringDistance, styles.sssProfileScatteringDistance);
                GUI.enabled = true;

                m_TexturingMode.intValue        = EditorGUILayout.Popup(styles.sssTexturingMode,           m_TexturingMode.intValue,    styles.sssTexturingModeOptions);
                m_TransmissionMode.intValue     = EditorGUILayout.Popup(styles.sssProfileTransmissionMode, m_TransmissionMode.intValue, styles.sssTransmissionModeOptions);

                EditorGUILayout.PropertyField(m_VolumeAlbedo,   styles.sssProfileVolumeAlbedo);
                EditorGUILayout.PropertyField(m_ThicknessRemap, styles.sssProfileMinMaxThickness);
                Vector2 thicknessRemap = m_ThicknessRemap.vector2Value;
                EditorGUILayout.MinMaxSlider(styles.sssProfileThicknessRemap, ref thicknessRemap.x, ref thicknessRemap.y, 0.0f, 50.0f);
                m_ThicknessRemap.vector2Value = thicknessRemap;
                EditorGUILayout.PropertyField(m_WorldScale, styles.sssProfileWorldScale);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(styles.sssProfilePreview0, styles.centeredMiniBoldLabel);
                EditorGUILayout.LabelField(styles.sssProfilePreview1, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField(styles.sssProfilePreview2, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField(styles.sssProfilePreview3, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space();
            }

            float   d  = m_ScatteringDistance.floatValue;
            Vector4 aS = m_SurfaceAlbedo.colorValue;
            Vector4 aV = m_VolumeAlbedo.colorValue;
            Vector3 sS = m_SurfaceShapeParam.vector3Value;
            Vector3 sV = m_VolumeShapeParam.vector3Value;
            Vector2 R  = m_ThicknessRemap.vector2Value;
            bool transmissionEnabled = m_TransmissionMode.intValue != (int)SubsurfaceScatteringProfile.TransmissionMode.None;

            // Draw the profile.
            m_ProfileMaterial.SetFloat("_ScatteringDistance", d);
            m_ProfileMaterial.SetVector("_SurfaceAlbedo",     aS);
            m_ProfileMaterial.SetVector("_SurfaceShapeParam", sS);
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(256, 256), m_ProfileImage, m_ProfileMaterial, ScaleMode.ScaleToFit, 1.0f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(styles.sssTransmittancePreview0, styles.centeredMiniBoldLabel);
            EditorGUILayout.LabelField(styles.sssTransmittancePreview1, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField(styles.sssTransmittancePreview2, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            // Draw the transmittance graph.
            m_TransmittanceMaterial.SetFloat("_ScatteringDistance", d);
            m_TransmittanceMaterial.SetVector("_VolumeAlbedo",      transmissionEnabled ? aV : Vector4.zero);
            m_TransmittanceMaterial.SetVector("_VolumeShapeParam",  sV);
            m_TransmittanceMaterial.SetVector("_ThicknessRemap",    R);
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(16, 16), m_TransmittanceImage, m_TransmittanceMaterial, ScaleMode.ScaleToFit, 16.0f);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                // Validate each individual asset and update caches.
                HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                hdPipeline.sssSettings.OnValidate();
            }
        }
    }
#endif
}
