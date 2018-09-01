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


        class BRDFType
        {
            public Type    m_type = null;
            public bool    m_needsFitting = true;

            public override string ToString()
            {
                return m_type.Name;
            }
        }
        BRDFType[]  m_BRDFTypes = new BRDFType[0];
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
                    m_BRDFTypes[i] = new BRDFType() { m_type = value[i] };
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

// EditorGUILayout.LabelField( "GNAAAAAAAAAAAAAAAAAAAAAA!" );
// EditorGUILayout.LabelField( AppDomain.CurrentDomain.GetAssemblies().Length + " assemblies in domain" );

            EditorGUILayout.LabelField( "Recognized BRDF Types: " + m_BRDFTypes.Length );
//            EditorGUILayout.BeginToggleGroup();

            int fitCount = 0;
            foreach ( BRDFType T in m_BRDFTypes ) {
                EditorGUILayout.Space();
                T.m_needsFitting = EditorGUILayout.Toggle( T.ToString(), T.m_needsFitting );
                fitCount += T.m_needsFitting ? 1 : 0;
            }
//            EditorGUILayout.EndToggleGroup();

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

            if ( fitCount > 0 ) {
                if ( GUILayout.Button(new GUIContent( "Generate LTC Tables", "" ), EditorStyles.toolbarButton ) ) {
                    DoFitting();
                }
            }

            EditorGUILayout.EndVertical();
        }

        void    DoFitting()
        {
            // Make sure target directory exists before creating any file!
            if ( !TARGET_DIRECTORY.Exists )
                TARGET_DIRECTORY.Create();

//             using ( StreamWriter W = pipo.CreateText() )
//                 W.WriteLine( "BISOU!" );

            // Fit all selected BRDFs
            foreach ( BRDFType T in m_BRDFTypes )
                if ( T.m_needsFitting ) {
                    LTCFitter   fitter = new LTCFitter();
                    IBRDF       BRDF = T.m_type.GetConstructor( new Type[0] ).Invoke( new object[0] ) as IBRDF;  // Invoke default constructor
                    FileInfo    tableFile = new FileInfo( Path.Combine( TARGET_DIRECTORY.FullName, T.m_type.Name + ".ltc" ) );
                    fitter.SetupBRDF( BRDF, LTC_TABLE_SIZE, tableFile );
                }
        }
    }
}
