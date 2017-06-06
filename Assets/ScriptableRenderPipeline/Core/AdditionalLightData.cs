namespace UnityEngine.Experimental.Rendering
{
    public enum LightArchetype { Punctual, Area, Projector };

    //@TODO: We should continuously move these values
    // into the engine when we can see them being generally useful
    [RequireComponent(typeof(Light))]
    public class AdditionalLightData : MonoBehaviour
    {
        public const int DefaultShadowResolution = 512;

        public int shadowResolution = DefaultShadowResolution;

        public static int GetShadowResolution(AdditionalLightData lightData)
        {
            if (lightData != null)
                return lightData.shadowResolution;
            else
                return DefaultShadowResolution;
        }

        [Range(0.0F, 100.0F)]
        public float m_innerSpotPercent = 0.0f; // To display this field in the UI this need to be public

        public float GetInnerSpotPercent01()
        {
            return Mathf.Clamp(m_innerSpotPercent, 0.0f, 100.0f) / 100.0f;
        }

        [Range(0.0F, 1.0F)]
        public float shadowDimmer = 1.0f;
        [Range(0.0F, 1.0F)]
        public float lightDimmer = 1.0f;

        // Not used for directional lights.
        public float fadeDistance = 10000.0f;
        public float shadowFadeDistance = 10000.0f;

        public bool affectDiffuse = true;
        public bool affectSpecular = true;

        public LightArchetype archetype = LightArchetype.Punctual;

        [Range(0.0f, 20.0f)]
        public float lightLength = 0.0f; // Area & projector lights

        [Range(0.0f, 20.0f)]
        public float lightWidth  = 0.0f; // Area & projector lights

        // shadow related parameters
        [System.Serializable]
        public struct ShadowData
        {
            public int   format;
            public int[] data;
        };

        [HideInInspector, SerializeField] private int           shadowCascadeCount = 4;
        [HideInInspector, SerializeField] private float[]       shadowCascadeRatios = new float[3]{ 0.05f, 0.2f, 0.3f };
        [HideInInspector, SerializeField] private int           shadowAlgorithm;
        [HideInInspector, SerializeField] private int           shadowVariant;
        [HideInInspector, SerializeField] private int           shadowPrecision;
        [HideInInspector, SerializeField] private ShadowData    shadowData;
        [HideInInspector, SerializeField] private ShadowData[]  shadowDatas = new ShadowData[0];

        public int cascadeCount { get { return shadowCascadeCount; } }
        public void GetShadowCascades( out int cascadeCount, out float[] cascadeRatios ) { cascadeCount = shadowCascadeCount; cascadeRatios = shadowCascadeRatios; }
        public void GetShadowAlgorithm( out int algorithm, out int variant, out int precision )    { algorithm = shadowAlgorithm; variant = shadowVariant; precision = shadowPrecision; }
        public void SetShadowAlgorithm( int algorithm, int variant, int precision, int format, int[] data )
        {
            shadowAlgorithm   = algorithm;
            shadowVariant     = variant;
            shadowPrecision   = precision;
            shadowData.format = format;
            shadowData.data   = data;

            int idx = FindShadowData( format );
            if( idx < 0 )
            {
                idx = shadowDatas.Length;
                ShadowData[] tmp = new ShadowData[idx+1];
                for( int i = 0; i < idx; ++i )
                    tmp[i] = shadowDatas[i];
                shadowDatas = tmp;
            }
            shadowDatas[idx].format = format;
            shadowDatas[idx].data   = data != null ? data : new int[0];
        }
        // Load a specific shadow data. Returns null if requested data is not present.
        public int[] GetShadowData( int shadowDataFormat )
        {
            if( shadowData.format == shadowDataFormat )
                return shadowData.data;

            int idx = FindShadowData( shadowDataFormat );
            return idx >= 0 ? shadowDatas[idx].data : null;
        }
        // Returns the currently set shadow data and format. Can return null.
        public int[] GetShadowData( out int shadowDataFormat )
        {
            shadowDataFormat = shadowData.format;
            return shadowData.data;
        }
#if UNITY_EDITOR
        public void CompactShadowData()
        {
            shadowDatas = new ShadowData[0];
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
        private int FindShadowData( int shadowDataFormat )
        {
            for( int i = 0; i < shadowDatas.Length; ++i )
            {
                if( shadowDatas[i].format == shadowDataFormat )
                    return i;
            }
            return -1;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(AdditionalLightData))]
    [UnityEditor.CanEditMultipleObjects]
    public class AdditionalLightDataEditor : UnityEditor.Editor
    {
        static ShadowRegistry m_ShadowRegistry;

#pragma warning disable 414 // CS0414 The private field '...' is assigned but its value is never used
        UnityEditor.SerializedProperty m_ShadowAlgorithm;
        UnityEditor.SerializedProperty m_ShadowVariant;
        UnityEditor.SerializedProperty m_ShadowData;
        UnityEditor.SerializedProperty m_ShadowDatas;
#pragma warning restore 414
        UnityEditor.SerializedProperty m_ShadowCascadeCount;
        UnityEditor.SerializedProperty m_ShadowCascadeRatios;

        public static void SetRegistry( ShadowRegistry registry ) { m_ShadowRegistry = registry; }

        void OnEnable()
        {
            m_ShadowAlgorithm = serializedObject.FindProperty( "shadowAlgorithm" );
            m_ShadowVariant   = serializedObject.FindProperty( "shadowVariant" );
            m_ShadowData      = serializedObject.FindProperty( "shadowData" );
            m_ShadowDatas     = serializedObject.FindProperty( "shadowDatas" );
            m_ShadowCascadeCount  = serializedObject.FindProperty( "shadowCascadeCount" );
            m_ShadowCascadeRatios = serializedObject.FindProperty( "shadowCascadeRatios" );
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if( m_ShadowRegistry == null )
                return;

            AdditionalLightData ald = (AdditionalLightData) target;
            if( ald == null )
                return;

            UnityEditor.EditorGUI.BeginChangeCheck();
            
            m_ShadowRegistry.Draw( ald.gameObject.GetComponent<Light>() );
            serializedObject.Update();

            // cascade code
            if( ald.gameObject.GetComponent<Light>().type == LightType.Directional )
            {
                UnityEditor.EditorGUI.BeginChangeCheck();
                UnityEditor.EditorGUILayout.PropertyField( m_ShadowCascadeCount );
                if( UnityEditor.EditorGUI.EndChangeCheck() )
                {
                    const int kMaxCascades = (int) ShadowAtlas.k_MaxCascadesInShader; // depending on where you look this is either 32 or 4, so we're limiting it to 4 for now
                    int newcnt = m_ShadowCascadeCount.intValue <= 0 ? 1 : (m_ShadowCascadeCount.intValue > kMaxCascades ? kMaxCascades : m_ShadowCascadeCount.intValue);
                    m_ShadowCascadeCount.intValue = newcnt;
                    m_ShadowCascadeRatios.arraySize = newcnt-1;
                }
                UnityEditor.EditorGUI.indentLevel++;
                for( int i = 0; i < m_ShadowCascadeRatios.arraySize; i++ )
                {
                    UnityEditor.EditorGUILayout.Slider( m_ShadowCascadeRatios.GetArrayElementAtIndex( i ), 0.0f, 1.0f, new GUIContent( "Cascade " + i ) );
                }
                UnityEditor.EditorGUI.indentLevel--;
            }

            if( UnityEditor.EditorGUI.EndChangeCheck() )
            {
                UnityEditor.EditorUtility.SetDirty( ald );
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                UnityEditor.SceneView.RepaintAll();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
            }
