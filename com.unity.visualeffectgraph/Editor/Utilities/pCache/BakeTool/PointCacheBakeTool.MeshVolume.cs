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
            X_Descending = 0,
            X_Ascending = 1,
            Y_Descending = 2,
            Y_Ascending = 3,
            Z_Descending = 4,
            Z_Ascending = 5,
        };

        Axis[] m_Axes = new Axis[]{
            Axis.X_Ascending,
            Axis.Y_Ascending,
            Axis.Z_Ascending,
        };

        bool m_InverseFromCenter = false;

        void OnGUI_MeshVolume()
        {
            GUILayout.Label("Mesh Volume Baking", EditorStyles.boldLabel);
            m_Mesh = EditorGUILayout.ObjectField("Target Mesh", m_Mesh, typeof(Mesh), false) as Mesh;
            m_VoxelSize = Mathf.Max( EditorGUILayout.FloatField("Voxel Size", m_VoxelSize), 0.0000001f );
            
            m_Reorder = (Reorder)EditorGUILayout.EnumPopup("Reoroder", m_Reorder);

            if (m_Reorder == Reorder.Axis)
            {
                EditorGUI.BeginChangeCheck();
                m_Axes[0] = (Axis)EditorGUILayout.EnumPopup("First Axis", m_Axes[0]);

                m_Axes[1] = (Axis)EditorGUILayout.EnumPopup( new GUIContent( "Second Axis" ),
                    m_Axes[1], (axis) => !(( ((int)(Axis)axis ) / 2 ) == ( ((int)m_Axes[0]) / 2 ) ),
                    false );

                if ( EditorGUI.EndChangeCheck() )
                {
                    // Reorganize 2nd and 3rd axes

                    if ( ( ((int)m_Axes[1] ) / 2 ) == ( ((int)m_Axes[0]) / 2 ) )
                        m_Axes[1] = (Axis) ( ( ( ((int)m_Axes[0]) / 2 ) * 2 + 2 ) % 6 + ( ((int)m_Axes[1] ) % 2 ) );
                    
                    m_Axes[2] = (Axis) ( ( 3 - ( ((int)m_Axes[0]) / 2 ) - ( ((int)m_Axes[1]) / 2 ) ) * 2 + ( ((int)m_Axes[2] ) % 2 ) );
                }

                m_Axes[2] = (Axis)EditorGUILayout.EnumPopup( new GUIContent( "Third Axis" ), m_Axes[2],
                    (axis) => !( (( ((int)(Axis)axis ) / 2 ) == ( ((int)m_Axes[0]) / 2 ) ) ||
                        (( ((int)(Axis)axis ) / 2 ) == ( ((int)m_Axes[1]) / 2 ) ) )
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
                            for (var i=2 ; i>=0 ; --i)
                            {
                                int axisIndex = ( (int) m_Axes[i] ) / 2;

                                if (diff[axisIndex] != 0) return -((int)Mathf.Sign( diff[axisIndex] )) * ( ( ( (int) m_Axes[i])%2 ) * 2 - 1 );
                            }

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
            else
            {
                Debug.LogWarning("Creating the pCache file resulted in zero points and has failed: the voxel size is probably too large.");
            }
            
            EditorUtility.ClearProgressBar();
        }
    }
}