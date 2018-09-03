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
        bool        m_stopOnError = true;           // Stop as soon as we encounter an error

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
            bool    UIDirty = false;
            bool    databaseDirty = false;
            foreach ( BRDFType T in m_BRDFTypes )
            {
                if ( T.IsWorking || T.m_dirty )
                {   // Repaint to show progress as long as a thread is working...
                    T.m_dirty = false;
                    UIDirty = true;
                }
                if ( !T.IsWorking && T.m_refreshAssetsDatabase )
                {   // Database needs refresh!
                    T.m_refreshAssetsDatabase = false;
                    databaseDirty = true;
                }
            }

            if ( UIDirty )
                Repaint();
            if ( databaseDirty )
                AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
// During development, the array gets reset (it's only populated when the window gets created)
if ( m_BRDFTypes.Length == 0 )
    BRDFTypes = ListBRDFTypes();


            EditorGUILayout.Separator();

            EditorGUILayout.LabelField( "Recognized BRDF Types: " + m_BRDFTypes.Length );

            EditorGUILayout.BeginVertical( EditorStyles.miniButtonLeft );
//           GUILayout.Button(new GUIContent( "Generate LTC Tables", ""), EditorStyles.centeredGreyMiniLabel);

            int fitCount = 0;
            int workingCount = 0;
            foreach ( BRDFType T in m_BRDFTypes ) {
                EditorGUILayout.Space();

                if ( T.IsWorking )
                {   // Show current work progress
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField( T.ToString() + " " + (T.Progress * 100.0f).ToString( "G3" ) + "%", GUILayout.ExpandWidth( false ) );
//                        EditorGUILayout.LabelField( "<COMPUTING> " + T.ToString(), GUILayout.ExpandWidth( false ) );

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

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Separator();

            EditorGUILayout.EndVertical();

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

            m_continueComputation = EditorGUILayout.Toggle( "Resume Computation", m_continueComputation );
            if ( !m_continueComputation )
            {
                EditorGUILayout.HelpBox( "Be careful: if you do not wish to resume computation, existing values will be overwritten and you might lose existing work.", MessageType.Warning );
            }

            m_stopOnError = EditorGUILayout.Toggle( "Stop on Error", m_stopOnError );

            if ( fitCount > 0 ) {
                EditorGUILayout.Separator();
                EditorGUILayout.Space();
                if ( GUILayout.Button(new GUIContent( "Generate LTC Tables", "" ), EditorStyles.toolbarButton ) ) {
                    // Make sure target directory exists before creating any file!
                    if ( !TARGET_DIRECTORY.Exists )
                        TARGET_DIRECTORY.Create();

                    // Fit all selected BRDFs
                    foreach ( BRDFType T in m_BRDFTypes ) {
                        if ( T.m_needsFitting && !T.IsWorking )
                            T.StartFitting();
                    }
                }
            }

            if ( workingCount > 0 )
            {
                EditorGUILayout.Separator();
                EditorGUILayout.Space();
                if ( GUILayout.Button(new GUIContent( "Abort Computation", "" ), EditorStyles.toolbarButton ) ) {
                    foreach ( BRDFType T in m_BRDFTypes )
                        T.AbortFitting();
                }
            }
        }

        #region Fitting

        class BRDFType
        {
            /// <summary>
            /// Worker thread doing the LTC fitting
            /// </summary>
            class   FittingWorkerThread
            {
                public delegate void    CompletionDelegate( bool _finishedWithErrors );

                LTCFitter           m_fitter = new LTCFitter();
                IBRDF               m_BRDF = null;
                FileInfo            m_tableFile = null;
                bool                m_overwriteExistingValues = false;
                bool                m_stopOnError = true;
                CompletionDelegate  m_jobComplete;

                // Runtime values
                public float        m_progress = 0;
                public bool         m_abort = false;

                public FittingWorkerThread( IBRDF _BRDF, FileInfo _tableFile, bool _overwriteExistingValues, bool _stopOnError )
                {
                    m_BRDF = _BRDF;
                    m_tableFile = _tableFile;
                    m_overwriteExistingValues = _overwriteExistingValues;
                    m_stopOnError = _stopOnError;

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
//Debug.Log( "THREAD STARTED!" );

                    Exception   exception = null;
                    try {
                      m_fitter.Fit( m_overwriteExistingValues, m_stopOnError, ( float _progress ) => { m_progress = _progress; return !m_abort; } );


// Fake working loop
//System.Random   RNG = new System.Random( (int) DateTime.Now.Ticks + m_BRDF.GetHashCode() );
//int crashInt = 5 + (int) (55.0f * RNG.NextDouble());
//for ( int i=0; i < 30; i++ ) {
//    System.Threading.Thread.Sleep( 500 );
//    m_progress = i / 30.0f;
//    if ( m_abort )
//        throw new Exception( "User Abort!" );   // Abort computation!
//    if ( i == crashInt )
//        throw new Exception( "Rha!" );  // Simulate an exception
//}


//Debug.Log( m_BRDF.GetType().Name + " ==> SUCCESS!!" );

                    } catch ( LTCFitter.UserAbortException _e ) {
                        exception = _e;   // Store exception to signal computing is still needed
                        Debug.LogWarning( m_BRDF.GetType().Name + " - ABORTED." );
                    } catch ( Exception _e ) {
                        exception = _e;   // Store exception to signal computing is still needed

                        Debug.LogError( m_BRDF.GetType().Name + " THREAD EXCEPTION!!" );
                        Debug.LogException( _e );
                    }

                    // Report any error, whether computation succeeded or not
                    if ( m_fitter.ErrorsCount > 0 )
                    {
                        Debug.LogError( m_BRDF.GetType().Name + " Fitter reported " + m_fitter.ErrorsCount + " errors:\n"
                                        + m_fitter.Errors );
                    }

                    // Notify of completion
                    if ( m_jobComplete != null )
                        m_jobComplete( exception != null || m_fitter.ErrorsCount > 0 );
                }
            }

            LTCTableGeneratorEditor     m_owner;
            public Type                 m_type = null;
            public bool                 m_needsFitting = true;
            FittingWorkerThread         m_worker = null;        // Worker thread, also used to indicate whether the fitter is already working

            public bool                 m_dirty = false;        // GUI needs repainting if this flag is set
            public bool                 m_refreshAssetsDatabase = false;    // Assets database needs a refresh if set

            public bool     IsWorking {
                get { lock ( this ) return m_worker != null; }
            }
            public float    Progress {
                get { lock ( this ) return m_worker != null ? m_worker.m_progress : 0; }
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
                lock ( this ) {
                    if ( m_worker != null )
                        throw new Exception( "Already fitting!" );

                    IBRDF       BRDF = m_type.GetConstructor( new Type[0] ).Invoke( new object[0] ) as IBRDF;  // Invoke default constructor
                    FileInfo    tableFileName = new FileInfo( Path.Combine( TARGET_DIRECTORY.FullName, m_type.Name + ".ltc" ) );

                    Debug.Log( "Starting fit of BRDF " + m_type.FullName + " -> " + tableFileName.FullName );
                    m_worker = new FittingWorkerThread( BRDF, tableFileName, !m_owner.m_continueComputation, m_owner.m_stopOnError );
                    m_worker.Start( ( bool _finishedWithErrors ) => {
                        m_needsFitting = _finishedWithErrors;   // Clear the "Need Fitting" flag if the worker exited without an error
                        m_worker = null;                        // Auto-clear worker once job is finished
                        m_dirty = true;

                        if ( _finishedWithErrors )
                            return; // Can't export as long as we have errors...

                        // Export to C# source!
                        string      BRDFName = m_type.Name;
                        FileInfo    CSharpFileName = new FileInfo( Path.Combine( TARGET_DIRECTORY.FullName, "LtcData." + BRDFName + ".cs" ) );
                        try {
                            LTCTableGeneratorEditor.ExportToCSharp( tableFileName, CSharpFileName, BRDFName );
                            m_refreshAssetsDatabase = true;
                        } catch ( Exception _e ) {
                            Debug.LogError( "An error occurred during export to C# file \"" + CSharpFileName.FullName + "\":" );
                            Debug.LogException( _e );
                        }
                    } );
                }
            }

            public void     AbortFitting()
            {
                lock ( this ) {
                    if ( m_worker != null )
                        m_worker.m_abort = true;    // Signal abort for the thread...
                }
            }
        }

        #endregion

        #region C# Code Export

        static void    ExportToCSharp( FileInfo _tableFileName, FileInfo _CSharpFileName, string _BRDFName )
        {
            int     validResultsCount = 0;
            LTC[,]  table = LTCFitter.LoadTable( _tableFileName, out validResultsCount );
            if ( table == null )
                throw new Exception( "LTCFitter.LoadTable() returned an empty LTC table!" );

            // Make sure we have all the results before exporting
            if ( validResultsCount != table.Length )
            {
                Debug.LogWarning( "Can't generate C# code because the LTC table \"" + _tableFileName + "\" is incomplete: it only contains " + validResultsCount + " out of " + table.Length + " expected results\n" +
                    "Maybe some results failed because of an error? Try resuming computation or deleting the entire table on disk and restart the entire computation if the problem persists."
                    );
                return;
            }

            string  sourceCode = "";

            // Export LTC matrices
            int     tableSize = table.GetLength(0);
            LTC     defaultLTC = new LTC();
            defaultLTC.magnitude = 0.0;

Debug.Log( "Exporting " + tableSize + "x" + tableSize + " LTC table " + _tableFileName + " to C# file " + _CSharpFileName + "..." );

            string  tableName = "s_LtcMatrixData_" + _BRDFName;

            sourceCode += "using UnityEngine;\n"
                        + "using System;\n"
                        + "\n"
                        + "namespace UnityEngine.Experimental.Rendering.HDPipeline\n"
                        + "{\n"
                        + "    public partial class LTCAreaLight\n"
                        + "    {\n"
                        + "        // Table contains 3x3 matrix coefficients of M^-1 for the fitting of the " + _BRDFName + " BRDF using the LTC technique\n"
                        + "        // From \"Real-Time Polygonal-Light Shading with Linearly Transformed Cosines\" 2016 (https://eheitzresearch.wordpress.com/415-2/)\n"
                        + "        //\n"
                        + "        // The table is accessed via LTCAreaLight." + tableName + "[<roughnessIndex> + 64 * <thetaIndex>]    // Theta values are along the Y axis, Roughness values are along the X axis\n"
                        + "        //    • roughness = ( <roughnessIndex> / " + (tableSize-1) + " )^2  (the table is indexed by perceptual roughness)\n"
                        + "        //    • cosTheta = 1 - ( <thetaIndex> / " + (tableSize-1) + " )^2\n"
                        + "        //\n"
//                        + "        public static double[,]    " + tableName + " = new double[k_LtcLUTResolution * k_LtcLUTResolution, k_LtcLUTMatrixDim * k_LtcLUTMatrixDim] {";
                        + "        public static double[,]    " + tableName + " = new double[" + tableSize + " * " + tableSize + ", 3 * 3] {";

            string  lotsOfSpaces = "                                                                                                                            ";

            float   alpha, cosTheta;
            for ( int thetaIndex=0; thetaIndex < tableSize; thetaIndex++ ) {
                LTCFitter.GetRoughnessAndAngle( 0, thetaIndex, tableSize, out alpha, out cosTheta );
                sourceCode += "\n";
                sourceCode += "            // Cos(theta) = " + cosTheta + "\n";

                for ( int roughnessIndex=0; roughnessIndex < tableSize; roughnessIndex++ ) {
                    LTC ltc = table[roughnessIndex,thetaIndex];
                    if ( ltc == null ) {
                        ltc = defaultLTC;   // Should NOT happen since we checked all results are valid before export!
                    }
                    LTCFitter.GetRoughnessAndAngle( roughnessIndex, thetaIndex, tableSize, out alpha, out cosTheta );

                    // Export the matrix as a list of 3x3 doubles, columns first
//                  double  factor = 1.0 / ltc.invM[2,2];
                    double  factor = 1.0 / ltc.invM[1,1];   // Better precision, according to S.Hill

                    string  matrixString  = (factor * ltc.invM[0,0]) + ", " + (factor * ltc.invM[1,0]) + ", " + (factor * ltc.invM[2,0]) + ", ";
                            matrixString += (factor * ltc.invM[0,1]) + ", " + (factor * ltc.invM[1,1]) + ", " + (factor * ltc.invM[2,1]) + ", ";
                            matrixString += (factor * ltc.invM[0,2]) + ", " + (factor * ltc.invM[1,2]) + ", " + (factor * ltc.invM[2,2]);

                    string  line = "            { " + matrixString + " },";
                    if ( line.Length < 132 )
                        line += lotsOfSpaces.Substring( lotsOfSpaces.Length - (132 - line.Length) );    // Pad with spaces
                    sourceCode += line;
                    sourceCode += "// alpha = " + alpha + "\n";
                }
            }

            sourceCode += "        };\n";

            // End comment
            sourceCode += "\n";
            sourceCode += "        // NOTE: Formerly, we needed to also export and create a table for the BRDF's amplitude factor + fresnel coefficient\n";
            sourceCode += "        //    but it turns out these 2 factors are actually already precomputed and available in the FGD table corresponding\n";
            sourceCode += "        //    to the " + _BRDFName + " BRDF, therefore they are no longer exported...\n";

            // Close class and namespace
            sourceCode += "    }\n";
            sourceCode += "}\n";

            // Write content
            using ( StreamWriter W = _CSharpFileName.CreateText() )
                W.Write( sourceCode );
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
