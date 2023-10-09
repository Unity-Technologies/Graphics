using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using System.IO;
using UnityEditor;
using System.Linq;

namespace UnityEngine.Rendering.HighDefinition.LTC
{
    internal class LTCTableGeneratorEditor : EditorWindow
    {
        // The output directory for the tool
        static string k_OutputDirectory = "./Assets/Generated/LTCTables/";

        // Generated table's resolutions
        const int k_TableResolution = 64;

        // Sample count that will be used for the generation
        const int k_DefaultSampleCount = 32;

        // The array of lighting models that we need to generate
        LTCTableGenerator.BRDFGenerator[] m_BRDFGeneratorArray = null;

        // Sample count that will be used for the generation
        int m_SampleCount = 32;

        // Flag to generate in parallel
        bool m_ParallelExecution = true;

        // Defines which parametrization should be use when generating the tables
        LTCTableGenerator.LTCTableParametrization m_Parametrization;

        static Type[] ListAllBRDFTypes()
        {
            // This function lists all the classes that implement the interface IBSDF
            List<Type> types = new List<Type>();
            Type searchInterface = typeof(IBRDF);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => searchInterface.IsAssignableFrom(p) && !p.IsInterface).ToArray();
        }

        static void BuildBRDFGenerators(ref LTCTableGenerator.BRDFGenerator[] BRDFGeneratorArray)
        {
            // Collect all the BRDFs that we need to generate
            Type[] brdfTypes = ListAllBRDFTypes();

            if (brdfTypes.Length != 0)
            {
                BRDFGeneratorArray = new LTCTableGenerator.BRDFGenerator[brdfTypes.Length];
                for (int i = 0; i < brdfTypes.Length; ++i)
                {
                    BRDFGeneratorArray[i] = new LTCTableGenerator.BRDFGenerator(brdfTypes[i], k_TableResolution, k_DefaultSampleCount, LTCTableGenerator.LTCTableParametrization.CosTheta, k_OutputDirectory);
                }
            }
        }

        // Not expose to users for now
        //[MenuItem("Edit/Render Pipeline/HD Render Pipeline/Generate Area Light LTC Tables")]
        private static void Init()
        {
            // Create the window
            LTCTableGeneratorEditor window = (LTCTableGeneratorEditor)EditorWindow.GetWindow(typeof(LTCTableGeneratorEditor));

            // Name the window
            window.titleContent.text = "LTC Tables Generator";

            // Build the generators that we will be executing later
            BuildBRDFGenerators(ref window.m_BRDFGeneratorArray);

            // Display the window
            window.Show();
        }

        private void OnGUI()
        {
            if (m_BRDFGeneratorArray == null)
                BuildBRDFGenerators(ref m_BRDFGeneratorArray);

            EditorGUILayout.LabelField("Recognized BRDF Types: " + m_BRDFGeneratorArray.Length);

            EditorGUILayout.Separator();

            // Display the generators and their toggles
            int numActiveGenerators = 0;
            for (int i = 0; i < m_BRDFGeneratorArray.Length; ++i)
            {
                LTCTableGenerator.BRDFGenerator currentGenerator = m_BRDFGeneratorArray[i];
                currentGenerator.shouldGenerate = EditorGUILayout.Toggle(currentGenerator.type.Name, currentGenerator.shouldGenerate);
                if (currentGenerator.shouldGenerate)
                    numActiveGenerators++;
                EditorGUILayout.Space();
            }

            m_ParallelExecution = EditorGUILayout.Toggle("Parallel", m_ParallelExecution);
            m_SampleCount = EditorGUILayout.IntField("Sample Count", m_SampleCount);
            m_Parametrization = (LTCTableGenerator.LTCTableParametrization)EditorGUILayout.EnumPopup("Theta parametrization", m_Parametrization);

            EditorGUILayout.Separator();

            if (m_BRDFGeneratorArray.Length > 1)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(new GUIContent("Select All", ""), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    for (int i = 0; i < m_BRDFGeneratorArray.Length; ++i)
                    {
                        m_BRDFGeneratorArray[i].shouldGenerate = true;
                    }
                }
                if (GUILayout.Button(new GUIContent("Select None", ""), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    for (int i = 0; i < m_BRDFGeneratorArray.Length; ++i)
                    {
                        m_BRDFGeneratorArray[i].shouldGenerate = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (numActiveGenerators > 0)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.Space();
                if (GUILayout.Button(new GUIContent("Generate LTC Tables", "")))
                {
                    // Make sure target directory exists before creating any file!
                    DirectoryInfo outputDir = new DirectoryInfo(k_OutputDirectory);

                    if (!outputDir.Exists)
                        outputDir.Create();

                    for (int i = 0; i < m_BRDFGeneratorArray.Length; ++i)
                    {
                        EditorUtility.DisplayProgressBar("Generating LTC Tables", $"Generating {m_BRDFGeneratorArray[i].type.Name}", (float)i / m_BRDFGeneratorArray.Length);
                        if (m_BRDFGeneratorArray[i].shouldGenerate)
                        {
                            m_BRDFGeneratorArray[i].sampleCount = m_SampleCount;
                            m_BRDFGeneratorArray[i].parametrization = m_Parametrization;
                            LTCTableGenerator.ExecuteFittingJob(m_BRDFGeneratorArray[i], m_ParallelExecution);
                        }
                    }
                    EditorUtility.ClearProgressBar();

                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
