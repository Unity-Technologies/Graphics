//////////////////////////////////////////////////////////////////////////
// LTC Tables Generator
// The generator works by listing all the classes implementing the IBRDF interface and offers the user
//  to generate the LTC table as well as the C# script files directly useable in Unity's HDRP
//
// Once your C# file is generated, just add a call to LoadLUT() with your new table in LTCAreaLight.cs
//  so it adds a new slice in the Texture2DArray then add a line in LTCAreaLight.hlsl to add your BRDF
//
//////////////////////////////////////////////////////////////////////////
//
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering.HDPipeline.LTCFit;
using System.IO;

namespace UnityEditor.Experimental.Rendering.HDPipeline.LTCFit
{
    public class LTCTableGeneratorEditor : EditorWindow
    {
        static readonly DirectoryInfo   TARGET_DIRECTORY = new DirectoryInfo( "./Assets/Generated/LTCTables/" );
        const int                       LTC_TABLE_SIZE = 64;    // Generated tables are 64x64

        [MenuItem("Window/Render Pipeline/LTC Tables Generator")]
        private static void Init()
        {
            // Create the window
            LTCTableGeneratorEditor window = (LTCTableGeneratorEditor) EditorWindow.GetWindow( typeof(LTCTableGeneratorEditor) );
            window.titleContent.text = "LTC Tables Generator";
            window.BRDFTypes = ListBRDFTypes();
            window.Show();
        }


        BRDFType[]  m_BRDFTypes = new BRDFType[0];
        bool        m_continueComputation = true;   // Continue from where we left off

        Type[]  BRDFTypes {
            get {
                Type[]  result = new Type[m_BRDFTypes.Length];
                for ( int i=0; i < m_BRDFTypes.Length; i++ )
                    result[i] = m_BRDFTypes[i].m_type;
                return result;
                }
            set {
                if ( value == null )
                    value = new Type[0];
                m_BRDFTypes = new BRDFType[value.Length];
                for ( int i=0; i < value.Length; i++ )
                    m_BRDFTypes[i] = new BRDFType( this ) { m_type = value[i] };
            }
        }

        void OnInspectorUpdate()
        {
            foreach ( BRDFType T in m_BRDFTypes )
                if ( T.IsWorking )
                {   // Repaint to show progress as long as a thread is working...
                    Repaint();
                    break;
                }
        }

        private void OnGUI()
        {
// During development, the array gets reset (it's only populated when the window gets created)
if ( m_BRDFTypes.Length == 0 )
    BRDFTypes = ListBRDFTypes();


            EditorGUILayout.BeginVertical( EditorStyles.miniButton );
            GUILayout.Button(new GUIContent( "Generate LTC Tables", ""), EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField( "Recognized BRDF Types: " + m_BRDFTypes.Length );

            int fitCount = 0;
            int workingCount = 0;
            foreach ( BRDFType T in m_BRDFTypes ) {
                EditorGUILayout.Space();

                if ( T.IsWorking )
                {   // Show current work progress
//                    EditorGUILayout.LabelField( "<COMPUTING> " + T.ToString() + " Progress = " + (T.Progress * 100.0f).ToString( "G3" ) );

                    EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField( "<COMPUTING> " + T.ToString(), GUILayout.ExpandWidth( false ) );

                        Rect r = EditorGUILayout.BeginVertical();
                            EditorGUI.ProgressBar( r, T.Progress, (T.Progress*100.0f).ToString( "G3" ) + "%" );
                            EditorGUILayout.Space();
                        EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
                    workingCount++;
                }
                else
                {   // Propose a new computation
                    T.m_needsFitting = EditorGUILayout.Toggle( T.ToString(), T.m_needsFitting );
                    fitCount += T.m_needsFitting ? 1 : 0;
                }
            }

            EditorGUILayout.Separator();

            if ( m_BRDFTypes.Length > 1 ) {
                EditorGUILayout.BeginHorizontal();

                if ( GUILayout.Button( new GUIContent( "Select All", "" ), EditorStyles.miniButton, GUILayout.ExpandWidth( false ) ) ) {
                    foreach ( BRDFType T in m_BRDFTypes )
                        T.m_needsFitting = true;
                }
                if ( GUILayout.Button( new GUIContent( "Select None", "" ), EditorStyles.miniButton, GUILayout.ExpandWidth( false ) ) ) {
                    foreach ( BRDFType T in m_BRDFTypes )
                        T.m_needsFitting = false;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Separator();

            m_continueComputation = EditorGUILayout.Toggle( "Continue Last Computation", m_continueComputation );

            EditorGUILayout.Separator();
            EditorGUILayout.Space();

            if ( fitCount > 0 ) {
                if ( GUILayout.Button(new GUIContent( "Generate LTC Tables", "" ), EditorStyles.toolbarButton ) ) {
                    DoFitting();
                }
            }

            EditorGUILayout.EndVertical();
        }

        #region Fitting

        class BRDFType
        {
            /// <summary>
            /// Worker thread doing the LTC fitting
            /// </summary>
            class   FittingWorkerThread
            {
                public delegate void    CompletionDelegate();

                LTCFitter           m_fitter = new LTCFitter();
                IBRDF               m_BRDF = null;
                FileInfo            m_tableFile = null;
                bool                m_overwriteExistingValues = false;
                CompletionDelegate  m_jobComplete;

                public float        m_progress = 0;
                public bool         m_abort = false;
                public FittingWorkerThread( IBRDF _BRDF, FileInfo _tableFile, bool _overwriteExistingValues )
                {
                    m_BRDF = _BRDF;
                    m_tableFile = _tableFile;
                    m_overwriteExistingValues = _overwriteExistingValues;

                    m_progress = 0;
                    m_abort = false;

                    m_fitter.SetupBRDF( m_BRDF, LTC_TABLE_SIZE, m_tableFile );
                }

                public void Start( CompletionDelegate _jobComplete )
                {
                    m_jobComplete = _jobComplete;
                    System.Threading.ThreadPool.QueueUserWorkItem( DoFitting, this );
                }

                public void DoFitting( object _state )
                {
                    Debug.Log( "STREAD STARTED!" );
//                    m_fitter.Fit( m_overwriteExistingValues, ( float _progress ) => { m_progress = _progress; return m_abort; } );

// Fake working loop
for ( int i=0; i < 30; i++ ) {
    System.Threading.Thread.Sleep( 1000 );
//    new WaitForSeconds( 1 );
    m_progress = i / 30.0f;
}

                    Debug.Log( "STREAD STOPPED!" );

                    if ( m_jobComplete != null )
                        m_jobComplete();    // Notify
                }
            }

            LTCTableGeneratorEditor     m_owner;
            public Type                 m_type = null;
            public bool                 m_needsFitting = true;
            FittingWorkerThread         m_worker = null;

            public bool     IsWorking {
                get { return m_worker != null; }
            }
            public float    Progress {
                get { return m_worker != null ? m_worker.m_progress : 0; }
            }

            public BRDFType( LTCTableGeneratorEditor _owner )
            {
                m_owner = _owner;
            }

            public override string ToString()
            {
                return m_type.Name;
            }

            public void     StartFitting()
            {
                if ( m_worker != null )
                    throw new Exception( "Already fitting!" );

                IBRDF       BRDF = m_type.GetConstructor( new Type[0] ).Invoke( new object[0] ) as IBRDF;  // Invoke default constructor
                FileInfo    tableFile = new FileInfo( Path.Combine( TARGET_DIRECTORY.FullName, m_type.Name + ".ltc" ) );

                Debug.Log( "Starting fit of BRDF " + m_type.FullName + " -> " + tableFile.FullName );
                m_worker = new FittingWorkerThread( BRDF, tableFile, !m_owner.m_continueComputation );
                m_worker.Start( () => { m_worker = null; } );   // Auto-clear worker once job is finished
            }
        }

        void    DoFitting()
        {
            // Make sure target directory exists before creating any file!
            if ( !TARGET_DIRECTORY.Exists )
                TARGET_DIRECTORY.Create();

            // Fit all selected BRDFs
            foreach ( BRDFType T in m_BRDFTypes ) {
                if ( T.m_needsFitting && !T.IsWorking )
                    T.StartFitting();
            }
        }

        #endregion

        #region Helpers

        static Type[]   ListBRDFTypes() {
            // List all IBRDF implementers
            List<Type>  types = new List<Type>();
            Type        searchInterface = typeof(IBRDF);
//            foreach ( System.Reflection.Assembly A in AppDomain.CurrentDomain.GetAssemblies() )
//                foreach ( Type T in A.GetTypes() )
                foreach ( Type T in System.Reflection.Assembly.GetExecutingAssembly().GetTypes() )
                    if ( searchInterface.IsAssignableFrom( T ) && !T.IsInterface )
                        types.Add( T );

            return types.ToArray();
        }

        #endregion
    }
}
