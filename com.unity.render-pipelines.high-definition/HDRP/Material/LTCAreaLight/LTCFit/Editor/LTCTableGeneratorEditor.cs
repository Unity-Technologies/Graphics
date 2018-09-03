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
        static readonly DirectoryInfo   TARGET_DIRECTORY = new DirectoryInfo("./Assets/Generated/LTCTables/");
        const int                       LTC_TABLE_SIZE = 64;    // Generated tables are 64x64

        [MenuItem("Window/Render Pipeline/LTC Tables Generator")]
        private static void Init()
        {
            // Create the window
            LTCTableGeneratorEditor window = (LTCTableGeneratorEditor) EditorWindow.GetWindow(typeof(LTCTableGeneratorEditor));
            window.titleContent.text = "LTC Tables Generator";
            window.BRDFTypes = ListBRDFTypes();
            window.Show();
        }


        BRDFType[]  m_BRDFTypes = new BRDFType[0];
        bool        m_continueComputation = true;   // Continue from where we left off
        bool        m_stopOnError = true;           // Stop as soon as we encounter an error
        bool        m_enableVisualDebugging = true; // True to show visual debugging (slower!)

        Type[]  BRDFTypes {
            get {
                Type[]  result = new Type[m_BRDFTypes.Length];
                for (int i=0; i < m_BRDFTypes.Length; i++)
                    result[i] = m_BRDFTypes[i].m_type;
                return result;
                }
            set {
                if (value == null)
                    value = new Type[0];

                m_BRDFTypes = new BRDFType[value.Length];
                for (int i=0; i < value.Length; i++)
                    m_BRDFTypes[i] = new BRDFType(this, value[i]);
            }
        }

        void OnInspectorUpdate()
        {
            bool    UIDirty = false;
            bool    databaseDirty = false;
            foreach (BRDFType T in m_BRDFTypes)
            {
                if (T.IsWorking || T.m_dirty)
                {   // Repaint to show progress as long as a thread is working...
                    T.m_dirty = false;
                    UIDirty = true;
                }
                if (!T.IsWorking && T.m_refreshAssetsDatabase)
                {   // Database needs refresh!
                    T.m_refreshAssetsDatabase = false;
                    databaseDirty = true;
                }
            }

            if (UIDirty)
                Repaint();
            if (databaseDirty)
                AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
// During development, the array gets reset (it's only populated when the window gets created)
if (m_BRDFTypes.Length == 0)
    BRDFTypes = ListBRDFTypes();


            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Recognized BRDF Types: " + m_BRDFTypes.Length);

            EditorGUILayout.BeginVertical(EditorStyles.miniButtonLeft);
//           GUILayout.Button(new GUIContent("Generate LTC Tables", ""), EditorStyles.centeredGreyMiniLabel);

            int fitCount = 0;
            int workingCount = 0;
            foreach (BRDFType T in m_BRDFTypes)
            {
                EditorGUILayout.Space();

                if (T.IsWorking)
                {   // Show current work progress
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(T.ToString() + " " + (T.Progress * 100.0f).ToString("G3") + "%", GUILayout.ExpandWidth(false));
//                        EditorGUILayout.LabelField("<COMPUTING> " + T.ToString(), GUILayout.ExpandWidth(false));

                        Rect r = EditorGUILayout.BeginVertical();
                            EditorGUI.ProgressBar(r, T.Progress, (T.Progress*100.0f).ToString("G3") + "%");
                            EditorGUILayout.Space();
                        EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
                    workingCount++;
                }
                else
                {   // Propose a new computation
                    T.m_needsFitting = EditorGUILayout.Toggle(T.ToString(), T.m_needsFitting);
                    fitCount += T.m_needsFitting ? 1 : 0;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Separator();

            EditorGUILayout.EndVertical();

            if (m_BRDFTypes.Length > 1)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(new GUIContent("Select All", ""), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    foreach (BRDFType T in m_BRDFTypes)
                        T.m_needsFitting = true;
                }
                if (GUILayout.Button(new GUIContent("Select None", ""), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    foreach (BRDFType T in m_BRDFTypes)
                        T.m_needsFitting = false;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Separator();

            m_continueComputation = EditorGUILayout.Toggle("Resume Computation", m_continueComputation);
            if (!m_continueComputation)
                EditorGUILayout.HelpBox("Be careful: if you do not wish to resume computation, existing values will be overwritten and you might lose existing work.", MessageType.Warning);

            m_stopOnError = EditorGUILayout.Toggle("Stop on Error", m_stopOnError);

            m_enableVisualDebugging = EditorGUILayout.Toggle("Visual Debugging", m_enableVisualDebugging);
            if (m_enableVisualDebugging)
                EditorGUILayout.HelpBox("Visual debugging can significantly slow down computation.", MessageType.Warning);

            if (fitCount > 0)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.Space();
                if (GUILayout.Button(new GUIContent("Generate LTC Tables", ""), EditorStyles.toolbarButton))
                {
                    // Make sure target directory exists before creating any file!
                    if (!TARGET_DIRECTORY.Exists)
                        TARGET_DIRECTORY.Create();

                    // Fit all selected BRDFs
                    foreach (BRDFType T in m_BRDFTypes)
                    {
                        if (T.m_needsFitting && !T.IsWorking)
                            T.StartFitting();
                    }
                }
            }

            if (workingCount > 0)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.Space();
                if (GUILayout.Button(new GUIContent("Abort Computation", ""), EditorStyles.toolbarButton))
                {
                    foreach (BRDFType T in m_BRDFTypes)
                        T.AbortFitting();
                }

                if (m_enableVisualDebugging)
                    ShowVisualDebugging();
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
                public delegate void    CompletionDelegate(bool _finishedWithErrors);

                LTCFitter           m_fitter = new LTCFitter();
                IBRDF               m_BRDF = null;
                FileInfo            m_tableFile = null;
                bool                m_overwriteExistingValues = false;
                bool                m_stopOnError = true;
                CompletionDelegate  m_jobComplete;

                // Runtime values
                public float        m_progress = 0;
                public bool         m_abort = false;

                public LTC          LastComputedLTC    { get { return m_fitter.LastComputedResult; } }
                public Vector3      LastComputedView   { get { return m_fitter.LastComputedView; } }
                public float        LastComputedAlpha  { get { return m_fitter.LastComputedAlpha; } }

                public FittingWorkerThread(IBRDF _BRDF, FileInfo _tableFile, bool _overwriteExistingValues, bool _stopOnError)
                {
                    m_BRDF = _BRDF;
                    m_tableFile = _tableFile;
                    m_overwriteExistingValues = _overwriteExistingValues;
                    m_stopOnError = _stopOnError;

                    m_progress = 0;
                    m_abort = false;

                    m_fitter.SetupBRDF(m_BRDF, LTC_TABLE_SIZE, m_tableFile);
                }

                public void Start(CompletionDelegate _jobComplete)
                {
                    m_jobComplete = _jobComplete;
                    System.Threading.ThreadPool.QueueUserWorkItem(DoFitting, this);
                }

                public void DoFitting(object _state)
                {
//Debug.Log("THREAD STARTED!");

                    Exception   exception = null;
                    try {
                      m_fitter.Fit(m_overwriteExistingValues, m_stopOnError, (float _progress) => { m_progress = _progress; return !m_abort; });
                    }
                    catch (LTCFitter.UserAbortException _e)
                    {
                        exception = _e;   // Store exception to signal computing is still needed
                        Debug.LogWarning(m_BRDF.GetType().Name + " - ABORTED.");
                    }
                    catch (Exception _e)
                    {
                        exception = _e;   // Store exception to signal computing is still needed

                        Debug.LogError(m_BRDF.GetType().Name + " THREAD EXCEPTION!!");
                        Debug.LogException(_e);
                    }

                    // Report any error, whether computation succeeded or not
                    if (m_fitter.ErrorsCount > 0)
                    {
                        Debug.LogError(m_BRDF.GetType().Name + " Fitter reported " + m_fitter.ErrorsCount + " errors:\n"
                                        + m_fitter.Errors);
                    }

                    // Notify of completion
                    if (m_jobComplete != null)
                        m_jobComplete(exception != null || m_fitter.ErrorsCount > 0);
                }
            }

            LTCTableGeneratorEditor     m_owner;
            public Type                 m_type = null;
            public bool                 m_needsFitting = true;
            public IBRDF                m_BRDF = null;
            FittingWorkerThread         m_worker = null;        // Worker thread, also used to indicate whether the fitter is already working

            public bool                 m_dirty = false;        // GUI needs repainting if this flag is set
            public bool                 m_refreshAssetsDatabase = false;    // Assets database needs a refresh if set

            public bool     IsWorking       { get { lock (this) return m_worker != null; } }
            public float    Progress        { get { lock (this) return m_worker != null ? m_worker.m_progress : 0; } }

            public LTC      LastComputedLTC    { get { lock (this) return m_worker != null ? m_worker.LastComputedLTC : null; } }
            public Vector3  LastComputedView   { get { lock (this) return m_worker != null ? m_worker.LastComputedView : new Vector3(0, 0, 1); } }
            public float    LastComputedAlpha  { get { lock (this) return m_worker != null ? m_worker.LastComputedAlpha : 1.0f; } }

            public BRDFType(LTCTableGeneratorEditor _owner, Type _BRDFType)
            {
                m_owner = _owner;
                m_type = _BRDFType;
                m_BRDF = m_type.GetConstructor(new Type[0]).Invoke(new object[0]) as IBRDF;  // Invoke default constructor
            }

            public override string ToString()
            {
                return m_BRDF.GetType().Name;
            }

            public void     StartFitting()
            {
                lock (this)
                {
                    if (m_worker != null)
                        throw new Exception("Already fitting!");

                    FileInfo    tableFileName = new FileInfo(Path.Combine(TARGET_DIRECTORY.FullName, m_type.Name + ".ltc"));

                    Debug.Log("Starting fit of BRDF " + m_type.FullName + " -> " + tableFileName.FullName);
                    m_worker = new FittingWorkerThread(m_BRDF, tableFileName, !m_owner.m_continueComputation, m_owner.m_stopOnError);
                    m_worker.Start((bool _finishedWithErrors) => {
                        m_needsFitting = _finishedWithErrors;   // Clear the "Need Fitting" flag if the worker exited without an error
                        m_worker = null;                        // Auto-clear worker once job is finished
                        m_dirty = true;

                        if (_finishedWithErrors)
                            return; // Can't export as long as we have errors...

                        // Export to C# source!
                        string      BRDFName = m_type.Name;
                        FileInfo    CSharpFileName = new FileInfo(Path.Combine(TARGET_DIRECTORY.FullName, "LtcData." + BRDFName + ".cs"));
                        try
                        {
                            LTCTableGeneratorEditor.ExportToCSharp(tableFileName, CSharpFileName, BRDFName);
                            m_refreshAssetsDatabase = true;
                        }
                        catch (Exception _e)
                        {
                            Debug.LogError("An error occurred during export to C# file \"" + CSharpFileName.FullName + "\":");
                            Debug.LogException(_e);
                        }
                    });
                }
            }

            public void     AbortFitting()
            {
                lock (this)
                {
                    if (m_worker != null)
                        m_worker.m_abort = true;    // Signal abort for the thread...
                }
            }
        }

        #endregion

        #region C# Code Export

        static void    ExportToCSharp(FileInfo _tableFileName, FileInfo _CSharpFileName, string _BRDFName)
        {
            int     validResultsCount = 0;
            LTC[,]  table = LTCFitter.LoadTable(_tableFileName, out validResultsCount);
            if (table == null)
                throw new Exception("LTCFitter.LoadTable() returned an empty LTC table!");

            // Make sure we have all the results before exporting
            if (validResultsCount != table.Length)
            {
                Debug.LogWarning("Can't generate C# code because the LTC table \"" + _tableFileName + "\" is incomplete: it only contains " + validResultsCount + " out of " + table.Length + " expected results\n" +
                    "Maybe some results failed because of an error? Try resuming computation or deleting the entire table on disk and restart the entire computation if the problem persists."
                   );
                return;
            }

            string  sourceCode = "";

            // Export LTC matrices
            int     tableSize = table.GetLength(0);
            LTC     defaultLTC = new LTC();
            defaultLTC.magnitude = 0.0;

Debug.Log("Exporting " + tableSize + "x" + tableSize + " LTC table " + _tableFileName + " to C# file " + _CSharpFileName + "...");

            string  tableName = "s_LtcMatrixData_" + _BRDFName;

            sourceCode += "using UnityEngine;\n"
                        + "using System;\n"
                        + "\n"
                        + "namespace UnityEngine.Experimental.Rendering.HDPipeline\n"
                        + "{\n"
                        + "    public partial class LTCAreaLight\n"
                        + "    {\n"
                        + "        // [GENERATED CONTENT " + DateTime.Now.ToString( "dd MMM yyyy HH:mm:ss" ) + "]\n"
                        + "        // Table contains 3x3 matrix coefficients of M^-1 for the fitting of the " + _BRDFName + " BRDF using the LTC technique\n"
                        + "        // From \"Real-Time Polygonal-Light Shading with Linearly Transformed Cosines\" 2016 (https://eheitzresearch.wordpress.com/415-2/)\n"
                        + "        //\n"
                        + "        // The table is accessed via LTCAreaLight." + tableName + "[<roughnessIndex> + 64 * <thetaIndex>]    // Theta values are along the Y axis, Roughness values are along the X axis\n"
                        + "        //    • roughness = ( <roughnessIndex> / " + (tableSize-1) + " )^2  (the table is indexed by perceptual roughness)\n"
                        + "        //    • cosTheta = 1 - ( <thetaIndex> / " + (tableSize-1) + " )^2\n"
                        + "        //\n"
//                        + "        public static double[,]    " + tableName + " = new double[k_LtcLUTResolution * k_LtcLUTResolution, k_LtcLUTMatrixDim * k_LtcLUTMatrixDim] {";
                        + "        public static double[,]    " + tableName + " = new double[" + tableSize + " * " + tableSize + ", 3 * 3]\n"
                        + "        {";

            string  lotsOfSpaces = "                                                                                                                            ";

            float   alpha, cosTheta;
            for (int thetaIndex=0; thetaIndex < tableSize; thetaIndex++)
            {
                LTCFitter.GetRoughnessAndAngle(0, thetaIndex, tableSize, out alpha, out cosTheta);
                sourceCode += "\n";
                sourceCode += "            // Cos(theta) = " + cosTheta + "\n";

                for (int roughnessIndex=0; roughnessIndex < tableSize; roughnessIndex++)
                {
                    LTC ltc = table[roughnessIndex,thetaIndex];
                    if (ltc == null)
                    {
                        ltc = defaultLTC;   // Should NOT happen since we checked all results are valid before export!
                    }
                    LTCFitter.GetRoughnessAndAngle(roughnessIndex, thetaIndex, tableSize, out alpha, out cosTheta);

                    // Export the matrix as a list of 3x3 doubles, columns first
//                  double  factor = 1.0 / ltc.invM[2,2];
                    double  factor = 1.0 / ltc.invM[1,1];   // Better precision, according to S.Hill

                    string  matrixString  = (factor * ltc.invM[0,0]) + ", " + (factor * ltc.invM[1,0]) + ", " + (factor * ltc.invM[2,0]) + ", ";
                            matrixString += (factor * ltc.invM[0,1]) + ", " + (factor * ltc.invM[1,1]) + ", " + (factor * ltc.invM[2,1]) + ", ";
                            matrixString += (factor * ltc.invM[0,2]) + ", " + (factor * ltc.invM[1,2]) + ", " + (factor * ltc.invM[2,2]);

                    string  line = "            { " + matrixString + " },";
                    if (line.Length < 132)
                        line += lotsOfSpaces.Substring(lotsOfSpaces.Length - (132 - line.Length));    // Pad with spaces
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
            using (StreamWriter W = _CSharpFileName.CreateText())
                W.Write( sourceCode);
        }

        #endregion

        #region Visual Debug

        LTC     m_lastDebuggedLTC = null;

        void    ShowVisualDebugging()
        {
            if (m_texFalseSpectrum == null)
            {
                // Create false spectrum texture from our hardcoded array
                int W = m_falseSpectrumColors.GetLength(0);
                m_texFalseSpectrum = new Texture2D(W, 1, TextureFormat.ARGB32, false);
                for (int X=0; X < W; X++)
                    m_texFalseSpectrum.SetPixel(X, 0, new Color(m_falseSpectrumColors[X,0], m_falseSpectrumColors[X,1], m_falseSpectrumColors[X,2]));
                m_texFalseSpectrum.Apply();
            }

            EditorGUILayout.Separator();
            EditorGUILayout.Space();

            foreach (BRDFType T in m_BRDFTypes)
            {
                if (!T.IsWorking)
                    continue;   // Only debug working threads...

                LTC lastComputedLTC = T.LastComputedLTC;
                if (lastComputedLTC == null)
                    continue;

                if (lastComputedLTC != m_lastDebuggedLTC)
                {   // Update textures
                    UpdateDebugTextures(T.m_BRDF, lastComputedLTC, T.LastComputedView, T.LastComputedAlpha);
                    m_lastDebuggedLTC = lastComputedLTC;
                }

                const int    MARGIN = 16;
                Rect    rect  = EditorGUILayout.BeginHorizontal();

                //////////////////////////////////////////////////////////////////////////
                // Draw Image Length
                rect.x = MARGIN;
                rect.width = 2*DEBUG_TEXTURE_SIZE;
                rect.height = 16;

                EditorGUI.LabelField(rect, T.m_type.Name);
                rect.x += DEBUG_TEXTURE_SIZE + MARGIN;
                EditorGUI.LabelField(rect, "LTC");
                rect.x += DEBUG_TEXTURE_SIZE + MARGIN;
                EditorGUI.LabelField(rect, "Relative Error (total = " + lastComputedLTC.error.ToString("G4") + ")");

                EditorGUILayout.EndHorizontal();

                //////////////////////////////////////////////////////////////////////////
                // Draw debug images
                rect.x = MARGIN;
                rect.y += 16;

                // ==================================================================================
                // Draw the target BRDF
                if (m_texDebugBRDF != null)
                    EditorGUI.DrawPreviewTexture(new Rect(rect.x, rect.y, DEBUG_TEXTURE_SIZE, DEBUG_TEXTURE_SIZE), m_texDebugBRDF);

                rect.x += DEBUG_TEXTURE_SIZE + MARGIN;

                // ==================================================================================
                // Draw the matching LTC
                if (m_texDebugLTC != null)
                    EditorGUI.DrawPreviewTexture(new Rect(rect.x, rect.y, DEBUG_TEXTURE_SIZE, DEBUG_TEXTURE_SIZE), m_texDebugLTC);

                rect.x += DEBUG_TEXTURE_SIZE + MARGIN;

                // ==================================================================================
                // Draw the relative error
                if (m_texDebugError != null)
                    EditorGUI.DrawPreviewTexture(new Rect(rect.x, rect.y, DEBUG_TEXTURE_SIZE, DEBUG_TEXTURE_SIZE), m_texDebugError);

                //////////////////////////////////////////////////////////////////////////
                // Draw false spectrum scale
                rect.x = MARGIN;
                rect.y += DEBUG_TEXTURE_SIZE + 16;

                EditorGUI.DrawPreviewTexture(new Rect(rect.x, rect.y, DEBUG_TEXTURE_SIZE, 4), m_texFalseSpectrum);
                rect.y += 8;
                EditorGUI.LabelField(rect, "1e" + ERROR_LOG10_MIN);
                rect.x += DEBUG_TEXTURE_SIZE - 32;
                EditorGUI.LabelField(rect, "1e" + ERROR_LOG10_MAX);

                break;
            }
        }

        /// <summary>
        /// Fill the debug textures
        /// </summary>
        /// <param name="_BRDF"></param>
        /// <param name="_LTC"></param>
        void    UpdateDebugTextures(IBRDF _BRDF, LTC _LTC, Vector3 _tsView, float _alpha)
        {
            if (m_texDebugBRDF == null)
            {
// Debug.Log("Creating textures");
                m_texDebugBRDF = new Texture2D(DEBUG_TEXTURE_SIZE, DEBUG_TEXTURE_SIZE, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
                m_texDebugLTC = new Texture2D(DEBUG_TEXTURE_SIZE, DEBUG_TEXTURE_SIZE, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
                m_texDebugError = new Texture2D(DEBUG_TEXTURE_SIZE, DEBUG_TEXTURE_SIZE, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
            }

            double  pdf;
            float   x2, y2, z2;
            Vector3 tsLight = new Vector3();
            int     pixelIndex = 0;
            for (int Y=0; Y < DEBUG_TEXTURE_SIZE; Y++)
            {
                tsLight.y = 2.0f * Y / (DEBUG_TEXTURE_SIZE-1) - 1.0f;
                y2 = tsLight.y*tsLight.y;
                for (int X=0; X < DEBUG_TEXTURE_SIZE; X++, pixelIndex++)
                {
                    tsLight.x = 2.0f * X / (DEBUG_TEXTURE_SIZE-1) - 1.0f;
                    x2 = tsLight.x*tsLight.x;
                    z2 = 1.0f - x2 - y2;
                    if (z2 <= 0.0f)
                        continue;   // Outside hemisphere
                    tsLight.z = Mathf.Sqrt(z2);

                    float   V_ref = (float) _BRDF.Eval(ref _tsView, ref tsLight, _alpha, out pdf);
                    float   V_ltc = (float) _LTC.Eval(ref tsLight);
                    ComputeFalseSpectrumColor(V_ref, ERROR_LOG10_MIN, ERROR_LOG10_MAX, ref m_texDebugBRDF_CPU[pixelIndex]);
                    ComputeFalseSpectrumColor(V_ltc, ERROR_LOG10_MIN, ERROR_LOG10_MAX, ref m_texDebugLTC_CPU[pixelIndex]);

                    // Compute relative error
                    float   relativeError = (V_ref > V_ltc ? V_ref / Math.Max(1e-6f, V_ltc) : V_ltc / Math.Max(1e-6f, V_ref)) - 1.0f;
                            relativeError *= Math.Min(Math.Abs(V_ref), Math.Abs(V_ltc));  // Weigh by the value itself to give very low importance to small values after all
                    ComputeFalseSpectrumColor(relativeError, ERROR_LOG10_MIN, ERROR_LOG10_MAX, ref m_texDebugError_CPU[pixelIndex]);
                }
            }

            // Upload to texture
//Debug.Log("Updating debug textures");
            m_texDebugBRDF.SetPixels(m_texDebugBRDF_CPU);
            m_texDebugLTC.SetPixels(m_texDebugLTC_CPU);
            m_texDebugError.SetPixels(m_texDebugError_CPU);
            m_texDebugBRDF.Apply(true);
            m_texDebugLTC.Apply(true);
            m_texDebugError.Apply(true);
        }

        const int   DEBUG_TEXTURE_SIZE = 128;

        const int   ERROR_LOG10_MIN = -4;
        const int   ERROR_LOG10_MAX = 4;

        Texture2D   m_texDebugBRDF = null;
        Texture2D   m_texDebugLTC = null;
        Texture2D   m_texDebugError = null;

        Color[]    m_texDebugBRDF_CPU = new Color[DEBUG_TEXTURE_SIZE * DEBUG_TEXTURE_SIZE];
        Color[]    m_texDebugLTC_CPU = new Color[DEBUG_TEXTURE_SIZE * DEBUG_TEXTURE_SIZE];
        Color[]    m_texDebugError_CPU = new Color[DEBUG_TEXTURE_SIZE * DEBUG_TEXTURE_SIZE];

        // Transform value into false spectrum color
        void        ComputeFalseSpectrumColor(float _value, float _log10Min, float _log10Max, ref Color _color)
        {
            float   logV = Mathf.Clamp(Mathf.Log10(Mathf.Max(1e-8f, _value)), _log10Min, _log10Max);
            float   t = (logV - _log10Min) / (_log10Max - _log10Min);
            int     it = Mathf.Clamp((int) (t * m_falseSpectrumColors.GetLength(0)), 0, m_falseSpectrumColors.GetLength(0)-1);
            _color.r = m_falseSpectrumColors[it,0];
            _color.g = m_falseSpectrumColors[it,1];
            _color.b = m_falseSpectrumColors[it,2];
            _color.a = 1.0f;
        }

        Texture2D   m_texFalseSpectrum = null;
        float[,]   m_falseSpectrumColors = new float[,] {
            { 0f, 0f, 0f }, { 0.003921569f, 0f, 0f }, { 0.007843138f, 0.003921569f, 0.007843138f }, { 0.007843138f, 0.003921569f, 0.007843138f }, { 0.01568628f, 0.007843138f, 0.01176471f }, { 0.01960784f, 0.01176471f, 0.01960784f }, { 0.02352941f, 0.01176471f, 0.02352941f }, { 0.02352941f, 0.01176471f, 0.02745098f }, { 0.03137255f, 0.01568628f, 0.03137255f }, { 0.03529412f, 0.01960784f, 0.03921569f }, { 0.03921569f, 0.02352941f, 0.04313726f }, { 0.04705882f, 0.02352941f, 0.04313726f }, { 0.05098039f, 0.02745098f, 0.05490196f }, { 0.05882353f, 0.03137255f, 0.05490196f }, { 0.0627451f, 0.03137255f, 0.0627451f }, { 0.07058824f, 0.03529412f, 0.06666667f }, { 0.07450981f, 0.03529412f, 0.07450981f }, { 0.07843138f, 0.03921569f, 0.07843138f }, { 0.08627451f, 0.04313726f, 0.08627451f }, { 0.09019608f, 0.04705882f, 0.09411765f }, { 0.1019608f, 0.04705882f, 0.1019608f }, { 0.1058824f, 0.05098039f, 0.1058824f }, { 0.1137255f, 0.05490196f, 0.1098039f }, { 0.1176471f, 0.05882353f, 0.1215686f }, { 0.1294118f, 0.0627451f, 0.1254902f }, { 0.1333333f, 0.06666667f, 0.1333333f }, { 0.1411765f, 0.06666667f, 0.1411765f }, { 0.1490196f, 0.07450981f, 0.145098f }, { 0.1529412f, 0.07450981f, 0.1568628f }, { 0.1607843f, 0.08235294f, 0.1607843f }, { 0.172549f, 0.08627451f, 0.1686275f }, { 0.1803922f, 0.08627451f, 0.1764706f }, { 0.1882353f, 0.09019608f, 0.1843137f }, { 0.1960784f, 0.09411765f, 0.1921569f }, { 0.2039216f, 0.09803922f, 0.2f }, { 0.2078431f, 0.1058824f, 0.2039216f }, { 0.2156863f, 0.1058824f, 0.2117647f }, { 0.227451f, 0.1098039f, 0.2196078f }, { 0.2352941f, 0.1137255f, 0.227451f }, { 0.2392157f, 0.1215686f, 0.2392157f }, { 0.2509804f, 0.1215686f, 0.2470588f }, { 0.2588235f, 0.1254902f, 0.2509804f }, { 0.2666667f, 0.1294118f, 0.2627451f }, { 0.2745098f, 0.1333333f, 0.2666667f }, { 0.282353f, 0.1372549f, 0.2784314f }, { 0.2901961f, 0.145098f, 0.2862745f }, { 0.2980392f, 0.1490196f, 0.2941177f }, { 0.3058824f, 0.1490196f, 0.3019608f }, { 0.3137255f, 0.1568628f, 0.3098039f }, { 0.3215686f, 0.1568628f, 0.3176471f }, { 0.3294118f, 0.1647059f, 0.3254902f }, { 0.3372549f, 0.1686275f, 0.3333333f }, { 0.345098f, 0.1686275f, 0.345098f }, { 0.3568628f, 0.172549f, 0.3490196f }, { 0.3647059f, 0.1764706f, 0.3607843f }, { 0.372549f, 0.1843137f, 0.3686275f }, { 0.3764706f, 0.1843137f, 0.3764706f }, { 0.3882353f, 0.1921569f, 0.3843137f }, { 0.3921569f, 0.1921569f, 0.3882353f }, { 0.4039216f, 0.2f, 0.3960784f }, { 0.4078431f, 0.2039216f, 0.4078431f }, { 0.4156863f, 0.2039216f, 0.4156863f }, { 0.427451f, 0.2117647f, 0.4235294f }, { 0.4313726f, 0.2117647f, 0.4313726f }, { 0.4392157f, 0.2196078f, 0.4392157f }, { 0.4470588f, 0.2196078f, 0.4431373f }, { 0.4509804f, 0.227451f, 0.4509804f }, { 0.4627451f, 0.2313726f, 0.4588235f }, { 0.4666667f, 0.2352941f, 0.4666667f }, { 0.4745098f, 0.2392157f, 0.4745098f }, { 0.4784314f, 0.2431373f, 0.4823529f }, { 0.4862745f, 0.2431373f, 0.4901961f }, { 0.4941176f, 0.2470588f, 0.4941176f }, { 0.5019608f, 0.2509804f, 0.5058824f }, { 0.5058824f, 0.254902f, 0.509804f }, { 0.5137255f, 0.254902f, 0.5176471f }, { 0.5176471f, 0.2627451f, 0.5254902f }, { 0.5215687f, 0.2627451f, 0.5294118f }, { 0.5294118f, 0.2705882f, 0.5372549f }, { 0.5333334f, 0.2705882f, 0.5411765f }, { 0.5411765f, 0.2745098f, 0.5490196f }, { 0.5450981f, 0.2784314f, 0.5568628f }, { 0.5450981f, 0.282353f, 0.5607843f }, { 0.5490196f, 0.2862745f, 0.5686275f }, { 0.5568628f, 0.2862745f, 0.572549f }, { 0.5607843f, 0.2862745f, 0.5764706f }, { 0.5647059f, 0.2901961f, 0.5803922f }, { 0.5647059f, 0.2941177f, 0.5843138f }, { 0.572549f, 0.2941177f, 0.5921569f }, { 0.5764706f, 0.3019608f, 0.5960785f }, { 0.5764706f, 0.2980392f, 0.6f }, { 0.5803922f, 0.3019608f, 0.6039216f }, { 0.5803922f, 0.3019608f, 0.6117647f }, { 0.5843138f, 0.3098039f, 0.6117647f }, { 0.5882353f, 0.3098039f, 0.6156863f }, { 0.5921569f, 0.3137255f, 0.6235294f }, { 0.5921569f, 0.3137255f, 0.6235294f }, { 0.5921569f, 0.3137255f, 0.627451f }, { 0.5921569f, 0.3137255f, 0.6313726f }, { 0.5921569f, 0.3176471f, 0.6352941f }, { 0.5921569f, 0.3176471f, 0.6352941f }, { 0.5921569f, 0.3215686f, 0.6352941f }, { 0.5921569f, 0.3215686f, 0.6352941f }, { 0.5921569f, 0.3215686f, 0.6352941f }, { 0.5921569f, 0.3254902f, 0.6352941f }, { 0.5921569f, 0.3254902f, 0.6352941f }, { 0.5921569f, 0.3254902f, 0.6352941f }, { 0.5921569f, 0.3254902f, 0.6352941f }, { 0.5921569f, 0.3254902f, 0.6352941f }, { 0.5882353f, 0.3254902f, 0.6352941f }, { 0.5882353f, 0.3254902f, 0.6352941f }, { 0.5882353f, 0.3254902f, 0.6352941f }, { 0.5843138f, 0.3254902f, 0.6352941f }, { 0.5803922f, 0.3254902f, 0.6352941f }, { 0.5803922f, 0.3254902f, 0.6352941f }, { 0.5764706f, 0.3254902f, 0.6352941f }, { 0.572549f, 0.3254902f, 0.6352941f }, { 0.572549f, 0.3254902f, 0.6352941f }, { 0.5647059f, 0.3254902f, 0.6352941f }, { 0.5647059f, 0.3254902f, 0.6352941f }, { 0.5607843f, 0.3254902f, 0.6352941f }, { 0.5529412f, 0.3254902f, 0.6352941f }, { 0.5529412f, 0.3254902f, 0.6352941f }, { 0.5490196f, 0.3254902f, 0.6352941f }, { 0.5411765f, 0.3254902f, 0.6352941f }, { 0.5411765f, 0.3254902f, 0.6352941f }, { 0.5372549f, 0.3215686f, 0.6352941f }, { 0.5333334f, 0.3215686f, 0.6352941f }, { 0.5254902f, 0.3215686f, 0.6352941f }, { 0.5176471f, 0.3215686f, 0.6352941f }, { 0.5137255f, 0.3215686f, 0.6352941f }, { 0.509804f, 0.3215686f, 0.6352941f }, { 0.5058824f, 0.3176471f, 0.6352941f }, { 0.4980392f, 0.3176471f, 0.6352941f }, { 0.4941176f, 0.3137255f, 0.6352941f }, { 0.4901961f, 0.3176471f, 0.6352941f }, { 0.4823529f, 0.3137255f, 0.6352941f }, { 0.4823529f, 0.3137255f, 0.6352941f }, { 0.4745098f, 0.3137255f, 0.6352941f }, { 0.4666667f, 0.3137255f, 0.6352941f }, { 0.4627451f, 0.3137255f, 0.6352941f }, { 0.4588235f, 0.3137255f, 0.6352941f }, { 0.4509804f, 0.3137255f, 0.6352941f }, { 0.4470588f, 0.3137255f, 0.6352941f }, { 0.4392157f, 0.3137255f, 0.6352941f }, { 0.4313726f, 0.3137255f, 0.6352941f }, { 0.427451f, 0.3137255f, 0.6352941f }, { 0.4235294f, 0.3137255f, 0.6352941f }, { 0.4156863f, 0.3137255f, 0.6352941f }, { 0.4117647f, 0.3137255f, 0.6352941f }, { 0.4039216f, 0.3137255f, 0.6352941f }, { 0.4f, 0.3137255f, 0.6352941f }, { 0.3921569f, 0.3137255f, 0.6352941f }, { 0.3882353f, 0.3137255f, 0.6352941f }, { 0.3803922f, 0.3137255f, 0.6352941f }, { 0.3764706f, 0.3137255f, 0.6352941f }, { 0.372549f, 0.3137255f, 0.6352941f }, { 0.3647059f, 0.3137255f, 0.6313726f }, { 0.3607843f, 0.3137255f, 0.6313726f }, { 0.3529412f, 0.3137255f, 0.6313726f }, { 0.3490196f, 0.3137255f, 0.6313726f }, { 0.3411765f, 0.3137255f, 0.6313726f }, { 0.3372549f, 0.3137255f, 0.627451f }, { 0.3333333f, 0.3137255f, 0.627451f }, { 0.3294118f, 0.3137255f, 0.627451f }, { 0.3215686f, 0.3137255f, 0.627451f }, { 0.3176471f, 0.3137255f, 0.627451f }, { 0.3137255f, 0.3137255f, 0.627451f }, { 0.3098039f, 0.3137255f, 0.627451f }, { 0.3019608f, 0.3137255f, 0.627451f }, { 0.2980392f, 0.3137255f, 0.627451f }, { 0.2941177f, 0.3137255f, 0.627451f }, { 0.2901961f, 0.3137255f, 0.627451f }, { 0.2862745f, 0.3137255f, 0.627451f }, { 0.282353f, 0.3137255f, 0.627451f }, { 0.2784314f, 0.3137255f, 0.627451f }, { 0.2745098f, 0.3137255f, 0.627451f }, { 0.2705882f, 0.3137255f, 0.627451f }, { 0.2666667f, 0.3137255f, 0.627451f }, { 0.2627451f, 0.3137255f, 0.627451f }, { 0.2627451f, 0.3137255f, 0.627451f }, { 0.2588235f, 0.3137255f, 0.6313726f }, { 0.2588235f, 0.3215686f, 0.6313726f }, { 0.254902f, 0.3215686f, 0.6313726f }, { 0.2509804f, 0.3254902f, 0.6313726f }, { 0.2470588f, 0.3254902f, 0.6352941f }, { 0.2509804f, 0.3294118f, 0.6392157f }, { 0.2509804f, 0.3333333f, 0.6431373f }, { 0.2470588f, 0.3372549f, 0.6431373f }, { 0.2470588f, 0.3411765f, 0.6470588f }, { 0.2470588f, 0.3411765f, 0.6470588f }, { 0.2470588f, 0.345098f, 0.6509804f }, { 0.2470588f, 0.3490196f, 0.654902f }, { 0.2470588f, 0.3529412f, 0.6588235f }, { 0.2470588f, 0.3568628f, 0.6588235f }, { 0.2470588f, 0.3607843f, 0.6627451f }, { 0.2470588f, 0.3647059f, 0.6705883f }, { 0.2470588f, 0.3686275f, 0.6705883f }, { 0.2470588f, 0.372549f, 0.6784314f }, { 0.2470588f, 0.3764706f, 0.682353f }, { 0.2470588f, 0.3803922f, 0.6862745f }, { 0.2470588f, 0.3882353f, 0.6901961f }, { 0.2470588f, 0.3921569f, 0.6941177f }, { 0.2470588f, 0.3960784f, 0.6980392f }, { 0.2509804f, 0.4f, 0.7019608f }, { 0.2470588f, 0.4078431f, 0.7098039f }, { 0.2470588f, 0.4117647f, 0.7137255f }, { 0.2470588f, 0.4156863f, 0.7176471f }, { 0.2470588f, 0.4196078f, 0.7254902f }, { 0.2470588f, 0.4235294f, 0.7294118f }, { 0.2470588f, 0.4313726f, 0.7372549f }, { 0.2470588f, 0.4352941f, 0.7411765f }, { 0.2470588f, 0.4431373f, 0.7450981f }, { 0.2470588f, 0.4470588f, 0.7529412f }, { 0.2470588f, 0.4509804f, 0.7568628f }, { 0.2470588f, 0.4588235f, 0.7607843f }, { 0.2470588f, 0.4627451f, 0.7686275f }, { 0.2470588f, 0.4705882f, 0.772549f }, { 0.2470588f, 0.4784314f, 0.7803922f }, { 0.2470588f, 0.4823529f, 0.7882353f }, { 0.2470588f, 0.4901961f, 0.7921569f }, { 0.2470588f, 0.4941176f, 0.8f }, { 0.2470588f, 0.4980392f, 0.8f }, { 0.2470588f, 0.5058824f, 0.8078431f }, { 0.2470588f, 0.509804f, 0.8117647f }, { 0.2470588f, 0.5176471f, 0.8235294f }, { 0.2470588f, 0.5254902f, 0.827451f }, { 0.2470588f, 0.5294118f, 0.8313726f }, { 0.2470588f, 0.5372549f, 0.8392157f }, { 0.2470588f, 0.5450981f, 0.8431373f }, { 0.2470588f, 0.5490196f, 0.8509804f }, { 0.2470588f, 0.5568628f, 0.8509804f }, { 0.2470588f, 0.5607843f, 0.8588235f }, { 0.2470588f, 0.5686275f, 0.8627451f }, { 0.2470588f, 0.5764706f, 0.8666667f }, { 0.2470588f, 0.5803922f, 0.8745098f }, { 0.2470588f, 0.5882353f, 0.8823529f }, { 0.2470588f, 0.5921569f, 0.8823529f }, { 0.2470588f, 0.5960785f, 0.8901961f }, { 0.2470588f, 0.6039216f, 0.8941177f }, { 0.2470588f, 0.6117647f, 0.8980392f }, { 0.2470588f, 0.6156863f, 0.9058824f }, { 0.2470588f, 0.6235294f, 0.9098039f }, { 0.2470588f, 0.627451f, 0.9098039f }, { 0.2470588f, 0.6392157f, 0.9137255f }, { 0.2470588f, 0.6392157f, 0.9215686f }, { 0.2470588f, 0.6470588f, 0.9215686f }, { 0.2470588f, 0.654902f, 0.9254902f }, { 0.2470588f, 0.6588235f, 0.9294118f }, { 0.2470588f, 0.6627451f, 0.9294118f }, { 0.2470588f, 0.6705883f, 0.9372549f }, { 0.2470588f, 0.6745098f, 0.9372549f }, { 0.2470588f, 0.682353f, 0.9372549f }, { 0.2470588f, 0.6862745f, 0.9372549f }, { 0.2470588f, 0.6941177f, 0.9372549f }, { 0.2470588f, 0.6980392f, 0.9372549f }, { 0.2470588f, 0.7058824f, 0.9372549f }, { 0.2470588f, 0.7098039f, 0.9372549f }, { 0.2470588f, 0.7137255f, 0.9372549f }, { 0.2470588f, 0.7176471f, 0.9372549f }, { 0.2470588f, 0.7254902f, 0.9372549f }, { 0.2470588f, 0.7294118f, 0.9372549f }, { 0.2470588f, 0.7333333f, 0.9372549f }, { 0.2470588f, 0.7411765f, 0.9372549f }, { 0.2470588f, 0.7450981f, 0.9372549f }, { 0.2470588f, 0.7490196f, 0.9372549f }, { 0.2470588f, 0.7568628f, 0.9372549f }, { 0.2470588f, 0.7568628f, 0.9372549f }, { 0.2470588f, 0.7647059f, 0.9372549f }, { 0.2470588f, 0.7647059f, 0.9372549f }, { 0.2470588f, 0.7686275f, 0.9372549f }, { 0.2470588f, 0.7764706f, 0.9372549f }, { 0.2470588f, 0.7764706f, 0.9372549f }, { 0.2470588f, 0.7843137f, 0.9333333f }, { 0.2509804f, 0.7882353f, 0.9294118f }, { 0.2509804f, 0.7921569f, 0.9294118f }, { 0.254902f, 0.7960784f, 0.9215686f }, { 0.2588235f, 0.8f, 0.9176471f }, { 0.2588235f, 0.8f, 0.9098039f }, { 0.2588235f, 0.8039216f, 0.9058824f }, { 0.2627451f, 0.8078431f, 0.9019608f }, { 0.2627451f, 0.8156863f, 0.8901961f }, { 0.2666667f, 0.8156863f, 0.8823529f }, { 0.2666667f, 0.8196079f, 0.8784314f }, { 0.2745098f, 0.827451f, 0.8666667f }, { 0.2745098f, 0.827451f, 0.8588235f }, { 0.2745098f, 0.8313726f, 0.8509804f }, { 0.282353f, 0.8352941f, 0.8431373f }, { 0.282353f, 0.8392157f, 0.8352941f }, { 0.2862745f, 0.8470588f, 0.8196079f }, { 0.2901961f, 0.8470588f, 0.8117647f }, { 0.2901961f, 0.8509804f, 0.8f }, { 0.2941177f, 0.854902f, 0.7921569f }, { 0.2980392f, 0.854902f, 0.7843137f }, { 0.3019608f, 0.8588235f, 0.7686275f }, { 0.3019608f, 0.8627451f, 0.7568628f }, { 0.3098039f, 0.8666667f, 0.7450981f }, { 0.3098039f, 0.8705882f, 0.7372549f }, { 0.3137255f, 0.8705882f, 0.7215686f }, { 0.3176471f, 0.8784314f, 0.7098039f }, { 0.3215686f, 0.8784314f, 0.6980392f }, { 0.3254902f, 0.8823529f, 0.6862745f }, { 0.3294118f, 0.8862745f, 0.6705883f }, { 0.3294118f, 0.8901961f, 0.6588235f }, { 0.3372549f, 0.8941177f, 0.6431373f }, { 0.3411765f, 0.8941177f, 0.6352941f }, { 0.3411765f, 0.8980392f, 0.6196079f }, { 0.3490196f, 0.9019608f, 0.6078432f }, { 0.3490196f, 0.9019608f, 0.5921569f }, { 0.3529412f, 0.9058824f, 0.5843138f }, { 0.3568628f, 0.9058824f, 0.5647059f }, { 0.3647059f, 0.9098039f, 0.5529412f }, { 0.3686275f, 0.9137255f, 0.5411765f }, { 0.3686275f, 0.9176471f, 0.5215687f }, { 0.372549f, 0.9176471f, 0.509804f }, { 0.3803922f, 0.9215686f, 0.4980392f }, { 0.3843137f, 0.9254902f, 0.4823529f }, { 0.3843137f, 0.9254902f, 0.4705882f }, { 0.3921569f, 0.9294118f, 0.454902f }, { 0.3960784f, 0.9294118f, 0.4431373f }, { 0.4f, 0.9333333f, 0.4313726f }, { 0.4039216f, 0.9372549f, 0.4156863f }, { 0.4078431f, 0.9372549f, 0.4039216f }, { 0.4117647f, 0.9411765f, 0.3882353f }, { 0.4196078f, 0.9411765f, 0.372549f }, { 0.4196078f, 0.945098f, 0.3607843f }, { 0.4235294f, 0.945098f, 0.345098f }, { 0.427451f, 0.9490196f, 0.3333333f }, { 0.4352941f, 0.9529412f, 0.3215686f }, { 0.4352941f, 0.9529412f, 0.3098039f }, { 0.4431373f, 0.9568627f, 0.2980392f }, { 0.4431373f, 0.9607843f, 0.2862745f }, { 0.4509804f, 0.9607843f, 0.2745098f }, { 0.454902f, 0.9607843f, 0.2627451f }, { 0.4588235f, 0.9607843f, 0.2509804f }, { 0.4627451f, 0.9647059f, 0.2392157f }, { 0.4705882f, 0.9647059f, 0.2235294f }, { 0.4745098f, 0.9686275f, 0.2156863f }, { 0.4784314f, 0.9686275f, 0.2039216f }, { 0.4823529f, 0.972549f, 0.1921569f }, { 0.4862745f, 0.972549f, 0.1803922f }, { 0.4901961f, 0.9764706f, 0.172549f }, { 0.4980392f, 0.972549f, 0.1607843f }, { 0.5019608f, 0.9764706f, 0.1568628f }, { 0.5058824f, 0.9764706f, 0.145098f }, { 0.509804f, 0.9764706f, 0.1372549f }, { 0.5137255f, 0.9803922f, 0.1294118f }, { 0.5176471f, 0.9803922f, 0.1215686f }, { 0.5215687f, 0.9843137f, 0.1137255f }, { 0.5294118f, 0.9843137f, 0.1058824f }, { 0.5333334f, 0.9882353f, 0.09803922f }, { 0.5372549f, 0.9843137f, 0.09019608f }, { 0.5450981f, 0.9882353f, 0.08235294f }, { 0.5490196f, 0.9882353f, 0.07843138f }, { 0.5529412f, 0.9882353f, 0.07058824f }, { 0.5529412f, 0.9882353f, 0.07058824f }, { 0.5607843f, 0.9882353f, 0.07058824f }, { 0.5607843f, 0.9882353f, 0.07058824f }, { 0.5686275f, 0.9882353f, 0.07058824f }, { 0.572549f, 0.9882353f, 0.07058824f }, { 0.5764706f, 0.9882353f, 0.07058824f }, { 0.5803922f, 0.9882353f, 0.07058824f }, { 0.5843138f, 0.9882353f, 0.07058824f }, { 0.5921569f, 0.9882353f, 0.07058824f }, { 0.5960785f, 0.9882353f, 0.07058824f }, { 0.5960785f, 0.9882353f, 0.07058824f }, { 0.6039216f, 0.9882353f, 0.07058824f }, { 0.6078432f, 0.9882353f, 0.07058824f }, { 0.6156863f, 0.9882353f, 0.07058824f }, { 0.6196079f, 0.9882353f, 0.07058824f }, { 0.6235294f, 0.9882353f, 0.07058824f }, { 0.6313726f, 0.9882353f, 0.07058824f }, { 0.6352941f, 0.9882353f, 0.07058824f }, { 0.6431373f, 0.9882353f, 0.07058824f }, { 0.6431373f, 0.9882353f, 0.07058824f }, { 0.6509804f, 0.9882353f, 0.07058824f }, { 0.654902f, 0.9882353f, 0.07058824f }, { 0.6627451f, 0.9882353f, 0.07058824f }, { 0.6666667f, 0.9882353f, 0.07058824f }, { 0.6705883f, 0.9882353f, 0.07058824f }, { 0.6784314f, 0.9882353f, 0.07058824f }, { 0.682353f, 0.9882353f, 0.07058824f }, { 0.6862745f, 0.9882353f, 0.07058824f }, { 0.6941177f, 0.9882353f, 0.07058824f }, { 0.7019608f, 0.9882353f, 0.07058824f }, { 0.7058824f, 0.9882353f, 0.07058824f }, { 0.7137255f, 0.9882353f, 0.07058824f }, { 0.7176471f, 0.9882353f, 0.07058824f }, { 0.7215686f, 0.9882353f, 0.07058824f }, { 0.7254902f, 0.9882353f, 0.07058824f }, { 0.7333333f, 0.9882353f, 0.07058824f }, { 0.7411765f, 0.9882353f, 0.07058824f }, { 0.7450981f, 0.9882353f, 0.07058824f }, { 0.7490196f, 0.9882353f, 0.07058824f }, { 0.7568628f, 0.9882353f, 0.07058824f }, { 0.7607843f, 0.9882353f, 0.07058824f }, { 0.7686275f, 0.9882353f, 0.07058824f }, { 0.772549f, 0.9882353f, 0.07058824f }, { 0.7764706f, 0.9882353f, 0.07058824f }, { 0.7843137f, 0.9882353f, 0.07058824f }, { 0.7921569f, 0.9882353f, 0.07058824f }, { 0.7921569f, 0.9882353f, 0.07058824f }, { 0.8f, 0.9882353f, 0.07058824f }, { 0.8039216f, 0.9882353f, 0.07058824f }, { 0.8117647f, 0.9882353f, 0.07058824f }, { 0.8156863f, 0.9882353f, 0.07058824f }, { 0.8235294f, 0.9882353f, 0.07058824f }, { 0.827451f, 0.9882353f, 0.07058824f }, { 0.8313726f, 0.9882353f, 0.07058824f }, { 0.8392157f, 0.9882353f, 0.07058824f }, { 0.8431373f, 0.9882353f, 0.07058824f }, { 0.8470588f, 0.9882353f, 0.07058824f }, { 0.854902f, 0.9882353f, 0.07058824f }, { 0.8588235f, 0.9882353f, 0.07058824f }, { 0.8627451f, 0.9882353f, 0.07058824f }, { 0.8705882f, 0.9882353f, 0.07058824f }, { 0.8745098f, 0.9882353f, 0.07058824f }, { 0.8823529f, 0.9882353f, 0.07058824f }, { 0.8823529f, 0.9882353f, 0.07058824f }, { 0.8862745f, 0.9882353f, 0.07058824f }, { 0.8941177f, 0.9882353f, 0.07058824f }, { 0.8980392f, 0.9882353f, 0.07058824f }, { 0.9019608f, 0.9882353f, 0.07058824f }, { 0.9058824f, 0.9882353f, 0.07058824f }, { 0.9098039f, 0.9882353f, 0.07058824f }, { 0.9176471f, 0.9882353f, 0.07058824f }, { 0.9176471f, 0.9882353f, 0.07058824f }, { 0.9215686f, 0.9882353f, 0.07058824f }, { 0.9254902f, 0.9882353f, 0.07058824f }, { 0.9294118f, 0.9882353f, 0.07058824f }, { 0.9333333f, 0.9882353f, 0.07058824f }, { 0.9372549f, 0.9843137f, 0.07058824f }, { 0.9411765f, 0.9803922f, 0.07058824f }, { 0.9490196f, 0.9764706f, 0.07058824f }, { 0.9490196f, 0.9764706f, 0.07058824f }, { 0.9529412f, 0.9686275f, 0.07058824f }, { 0.9568627f, 0.9686275f, 0.07058824f }, { 0.9568627f, 0.9647059f, 0.07058824f }, { 0.9607843f, 0.9607843f, 0.07058824f }, { 0.9647059f, 0.9568627f, 0.07058824f }, { 0.9686275f, 0.9529412f, 0.07058824f }, { 0.9686275f, 0.9490196f, 0.07058824f }, { 0.972549f, 0.945098f, 0.07058824f }, { 0.9764706f, 0.9411765f, 0.07058824f }, { 0.9764706f, 0.9333333f, 0.07058824f }, { 0.9803922f, 0.9333333f, 0.07058824f }, { 0.9803922f, 0.9254902f, 0.07058824f }, { 0.9803922f, 0.9254902f, 0.07058824f }, { 0.9803922f, 0.9176471f, 0.07450981f }, { 0.9803922f, 0.9137255f, 0.07058824f }, { 0.9803922f, 0.9098039f, 0.07058824f }, { 0.9803922f, 0.9058824f, 0.07450981f }, { 0.9803922f, 0.8980392f, 0.07058824f }, { 0.9803922f, 0.8941177f, 0.07058824f }, { 0.9803922f, 0.8862745f, 0.07450981f }, { 0.9803922f, 0.8862745f, 0.07450981f }, { 0.9803922f, 0.8784314f, 0.07450981f }, { 0.9803922f, 0.8705882f, 0.07450981f }, { 0.9803922f, 0.8627451f, 0.07450981f }, { 0.9803922f, 0.8588235f, 0.07450981f }, { 0.9803922f, 0.854902f, 0.07843138f }, { 0.9803922f, 0.8470588f, 0.07843138f }, { 0.9803922f, 0.8392157f, 0.07843138f }, { 0.9803922f, 0.8313726f, 0.07843138f }, { 0.9803922f, 0.827451f, 0.07843138f }, { 0.9803922f, 0.8196079f, 0.07843138f }, { 0.9803922f, 0.8117647f, 0.07843138f }, { 0.9803922f, 0.8039216f, 0.08235294f }, { 0.9803922f, 0.7960784f, 0.08235294f }, { 0.9803922f, 0.7882353f, 0.08235294f }, { 0.9803922f, 0.7803922f, 0.08235294f }, { 0.9803922f, 0.7764706f, 0.08627451f }, { 0.9803922f, 0.7686275f, 0.08627451f }, { 0.9803922f, 0.7568628f, 0.08627451f }, { 0.9803922f, 0.7490196f, 0.08235294f }, { 0.9803922f, 0.7411765f, 0.08627451f }, { 0.9803922f, 0.7333333f, 0.08627451f }, { 0.9803922f, 0.7254902f, 0.09019608f }, { 0.9803922f, 0.7176471f, 0.08627451f }, { 0.9803922f, 0.7098039f, 0.09019608f }, { 0.9803922f, 0.7019608f, 0.09019608f }, { 0.9803922f, 0.6941177f, 0.09019608f }, { 0.9803922f, 0.682353f, 0.09019608f }, { 0.9803922f, 0.6705883f, 0.09019608f }, { 0.9803922f, 0.6627451f, 0.09019608f }, { 0.9803922f, 0.6588235f, 0.09411765f }, { 0.9803922f, 0.6470588f, 0.09411765f }, { 0.9803922f, 0.6392157f, 0.09411765f }, { 0.9803922f, 0.6313726f, 0.09803922f }, { 0.9803922f, 0.6196079f, 0.09411765f }, { 0.9803922f, 0.6117647f, 0.09803922f }, { 0.9803922f, 0.6f, 0.09803922f }, { 0.9803922f, 0.5960785f, 0.09803922f }, { 0.9803922f, 0.5843138f, 0.09803922f }, { 0.9803922f, 0.5764706f, 0.09803922f }, { 0.9803922f, 0.5647059f, 0.1019608f }, { 0.9803922f, 0.5568628f, 0.1019608f }, { 0.9803922f, 0.5450981f, 0.1019608f }, { 0.9803922f, 0.5372549f, 0.1019608f }, { 0.9803922f, 0.5294118f, 0.1019608f }, { 0.9764706f, 0.5215687f, 0.1058824f }, { 0.9764706f, 0.5137255f, 0.1058824f }, { 0.972549f, 0.5019608f, 0.1058824f }, { 0.972549f, 0.4901961f, 0.1058824f }, { 0.9686275f, 0.4823529f, 0.1098039f }, { 0.9686275f, 0.4745098f, 0.1098039f }, { 0.9686275f, 0.4627451f, 0.1098039f }, { 0.9686275f, 0.4588235f, 0.1137255f }, { 0.9647059f, 0.4470588f, 0.1137255f }, { 0.9647059f, 0.4352941f, 0.1098039f }, { 0.9607843f, 0.427451f, 0.1137255f }, { 0.9647059f, 0.4196078f, 0.1137255f }, { 0.9607843f, 0.4117647f, 0.1176471f }, { 0.9568627f, 0.4f, 0.1176471f }, { 0.9607843f, 0.3921569f, 0.1137255f }, { 0.9568627f, 0.3843137f, 0.1176471f }, { 0.9568627f, 0.372549f, 0.1176471f }, { 0.9529412f, 0.3647059f, 0.1215686f }, { 0.9529412f, 0.3568628f, 0.1176471f }, { 0.9529412f, 0.3490196f, 0.1176471f }, { 0.9529412f, 0.3411765f, 0.1215686f }, { 0.9490196f, 0.3333333f, 0.1215686f }, { 0.945098f, 0.3215686f, 0.1215686f }, { 0.945098f, 0.3176471f, 0.1215686f }, { 0.945098f, 0.3058824f, 0.1215686f }, { 0.9411765f, 0.2980392f, 0.1254902f }, { 0.945098f, 0.2901961f, 0.1254902f }, { 0.9411765f, 0.2862745f, 0.1254902f }, { 0.9372549f, 0.2784314f, 0.1294118f }, { 0.9372549f, 0.2666667f, 0.1254902f }, { 0.9372549f, 0.2588235f, 0.1294118f }, { 0.9372549f, 0.2509804f, 0.1254902f }, { 0.9333333f, 0.2431373f, 0.1294118f }, { 0.9333333f, 0.2352941f, 0.1294118f }, { 0.9333333f, 0.227451f, 0.1333333f }, { 0.9333333f, 0.2235294f, 0.1333333f }, { 0.9294118f, 0.2156863f, 0.1294118f }, { 0.9294118f, 0.2117647f, 0.1333333f }, { 0.9294118f, 0.2039216f, 0.1333333f }, { 0.9294118f, 0.1960784f, 0.1333333f }, { 0.9254902f, 0.1882353f, 0.1333333f }, { 0.9254902f, 0.1803922f, 0.1333333f }, { 0.9254902f, 0.1764706f, 0.1372549f }, { 0.9215686f, 0.172549f, 0.1372549f }, { 0.9254902f, 0.1686275f, 0.1372549f }, { 0.9254902f, 0.1607843f, 0.1372549f }, { 0.9215686f, 0.1529412f, 0.1372549f }, { 0.9215686f, 0.1529412f, 0.1372549f }, { 0.9176471f, 0.145098f, 0.1411765f }, { 0.9176471f, 0.1372549f, 0.1411765f }, { 0.9215686f, 0.1372549f, 0.1411765f }, { 0.9176471f, 0.1333333f, 0.1372549f }, { 0.9176471f, 0.1254902f, 0.1411765f }, { 0.9215686f, 0.1215686f, 0.1411765f }, { 0.9176471f, 0.1176471f, 0.1411765f }
            };

        #endregion

        #region Helpers

        static Type[]   ListBRDFTypes()
        {
            // List all IBRDF implementers
            List<Type>  types = new List<Type>();
            Type        searchInterface = typeof(IBRDF);
//            foreach (System.Reflection.Assembly A in AppDomain.CurrentDomain.GetAssemblies())
//                foreach (Type T in A.GetTypes())
                foreach (Type T in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
                    if (searchInterface.IsAssignableFrom(T) && !T.IsInterface)
                        types.Add(T);

            return types.ToArray();
        }

        #endregion
    }
}
