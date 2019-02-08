using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;

namespace UnityEditor.VFX.Utils
{
    public partial class PointCacheBakeTool : EditorWindow
    {
        float m_VoxelSize = 0.1f;

        string m_FileName;

        MeshVolumePCacheBaker m_VolumeBaker;

        IEnumerator m_BakingRoutine;

        enum Reorder
        {
            None,
            Axis,
            DistanceFromCenter
        };

        Reorder m_Reorder = Reorder.None;

        enum Axis
        {
            Xm,
            Xp,
            Ym,
            Yp,
            Zm,
            Zp
        };

        Axis m_1stAxis = Axis.Xp;
        Axis m_2ndAxis = Axis.Yp;
        Axis m_3rdAxis = Axis.Zp;

        bool m_InverseFromCenter = false;

        void OnGUI_MeshVolume()
        {
            GUILayout.Label("Mesh Volume Baking", EditorStyles.boldLabel);
            m_Mesh = EditorGUILayout.ObjectField("Target Mesh", m_Mesh, typeof(Mesh), false) as Mesh;
            m_VoxelSize = Mathf.Max( EditorGUILayout.FloatField("Voxel Size", m_VoxelSize), 0.0000001f );
            
            m_Reorder = (Reorder)EditorGUILayout.EnumPopup("Reoroder", m_Reorder);

            if (m_Reorder == Reorder.Axis)
            {
                m_1stAxis = (Axis)EditorGUILayout.EnumPopup("First Axis", m_1stAxis);
                m_2ndAxis = (Axis)EditorGUILayout.EnumPopup( new GUIContent( "Second Axis" ),
                    m_2ndAxis, (axis) => !axis.ToString().StartsWith(m_1stAxis.ToString().Substring(0,1)) ,
                    false );
                m_3rdAxis = (Axis)EditorGUILayout.EnumPopup( new GUIContent( "Third Axis" ), m_3rdAxis,
                    (axis) => !(axis.ToString().StartsWith(m_1stAxis.ToString().Substring(0,1)) ||
                        axis.ToString().StartsWith(m_2ndAxis.ToString().Substring(0,1)) )
                , false );
            }

            if (m_Reorder == Reorder.DistanceFromCenter)
                m_InverseFromCenter = EditorGUILayout.Toggle("Inverse", m_InverseFromCenter);
            
            if (m_Mesh != null)
            {
                if (GUILayout.Button("Save to pCache file..."))
                {
                    if (m_FileName == null) m_FileName = m_Mesh.name;
                    if (m_FileName.StartsWith("Assets/")) m_FileName = m_FileName.Substring(7, m_FileName.Length - 7);
                    
                    m_FileName = EditorUtility.SaveFilePanelInProject("pCacheFile", m_FileName, "pcache", "Save PCache");
                    if (m_FileName != null)
                    {
                        try
                        {
                            ComputePCacheFromMeshVolume();
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }

        void OnEnable()
        {
            EditorApplication.update += UpdateRoutines;
        }

        void OnDisable()
        {
            EditorApplication.update -= UpdateRoutines;
        }

        void UpdateRoutines()
        {
            if (m_BakingRoutine != null)
                m_BakingRoutine.MoveNext();
        }

        void ComputePCacheFromMeshVolume()
        {
            EditorUtility.DisplayProgressBar(m_ProgressBar_Title, m_ProgressBar_CapturingData, 0.0f);
            
            if(m_VolumeBaker == null)
                m_VolumeBaker = new MeshVolumePCacheBaker();
            
            m_VolumeBaker.mesh = m_Mesh;
            m_VolumeBaker.voxelsSize = m_VoxelSize;
            m_VolumeBaker.finishedCallback = SaveFile;

            m_BakingRoutine = m_VolumeBaker.Bake();
        }

        void SaveFile()
        {
            if (m_VolumeBaker.voxels.Count > 0)
            {
                var file = new PCache();
                file.AddVector3Property("position");
                
                EditorUtility.DisplayProgressBar(m_ProgressBar_Title, "Generating pCache...", 0.0f);

                var positions = m_VolumeBaker.voxels;

                switch (m_Reorder)
                {
                    case Reorder.Axis:
                        positions.Sort((a, b) =>
                        {
                            Vector3 diff = b - a;
                            int axisIndex1 = m_1stAxis.ToString().StartsWith("X") ? 0 : m_1stAxis.ToString().StartsWith("Y") ? 1 : 2;
                            int axisIndex2 = m_2ndAxis.ToString().StartsWith("X") ? 0 : m_2ndAxis.ToString().StartsWith("Y") ? 1 : 2;
                            int axisIndex3 = m_3rdAxis.ToString().StartsWith("X") ? 0 : m_3rdAxis.ToString().StartsWith("Y") ? 1 : 2;

                            if (diff[axisIndex3] != 0) return ( m_3rdAxis.ToString().Substring(1, 1) == "p")? ( (diff[axisIndex3] > 0)? -1 : 1 ) : ( (diff[axisIndex3] > 0)? 1 : -1 ) ;
                            if (diff[axisIndex2] != 0) return ( m_2ndAxis.ToString().Substring(1, 1) == "p")? ( (diff[axisIndex2] > 0)? -1 : 1 ) : ( (diff[axisIndex2] > 0)? 1 : -1 ) ;
                            if (diff[axisIndex1] != 0) return ( m_1stAxis.ToString().Substring(1, 1) == "p")? ( (diff[axisIndex1] > 0)? -1 : 1 ) : ( (diff[axisIndex1] > 0)? 1 : -1 ) ;

                            return 0;
                        });
                        break;
                    case Reorder.DistanceFromCenter:
                        positions.Sort( (a, b) => ( m_InverseFromCenter? -1 : 1 ) * a.sqrMagnitude.CompareTo(b.sqrMagnitude) );
                        break;
                }
                
                file.SetVector3Data("position", positions);
                
                EditorUtility.ClearProgressBar();
                
                EditorUtility.DisplayProgressBar(m_ProgressBar_Title, m_ProgressBar_SaveFile, 1.0f);
                file.SaveToFile(m_FileName, m_OutputFormat);
                AssetDatabase.ImportAsset(m_FileName, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
            
            EditorUtility.ClearProgressBar();
        }
    }
}