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
        
        void OnGUI_MeshVolume()
        {
            GUILayout.Label("Mesh Volume Baking", EditorStyles.boldLabel);
            m_Mesh = EditorGUILayout.ObjectField("Target Mesh", m_Mesh, typeof(Mesh), false) as Mesh;
            m_VoxelSize = EditorGUILayout.FloatField("Voxel Size", m_VoxelSize);
            
            if (m_Mesh != null)
            {
                if (GUILayout.Button("Save to pCache file..."))
                {
                    m_FileName = EditorUtility.SaveFilePanelInProject("pCacheFile", m_Mesh.name, "pcache", "Save PCache");
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
                
                file.SetVector3Data("position", m_VolumeBaker.voxels);
                
                EditorUtility.ClearProgressBar();
                
                EditorUtility.DisplayProgressBar(m_ProgressBar_Title, m_ProgressBar_SaveFile, 1.0f);
                file.SaveToFile(m_FileName, m_OutputFormat);
                AssetDatabase.ImportAsset(m_FileName, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
            
            EditorUtility.ClearProgressBar();
        }
    }
}