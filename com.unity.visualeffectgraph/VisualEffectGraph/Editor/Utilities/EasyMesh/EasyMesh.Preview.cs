using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace UnityEditor.VFX.Utilities
{

    public partial class EasyMesh
    {
        public enum PreviewMode
        {
            None,
            SearchGrid
        }

        public PreviewMode previewMode = PreviewMode.None;

        static Gradient prevgradient;

        public void OnSceneFunc(SceneView sceneView)
        {

            switch (previewMode)
            {
                case PreviewMode.None: break;
                case PreviewMode.SearchGrid: ShowSearchGrid(sceneView); break;
            }
        }

        public void ShowSearchGrid(SceneView sceneView)
        {
            if (prevgradient == null)
            {
                prevgradient = new Gradient();
                prevgradient.SetKeys(
                    new GradientColorKey[5]
                    {   new GradientColorKey(Color.blue, 0.0f),
                    new GradientColorKey(Color.cyan, 0.25f),
                    new GradientColorKey(Color.green, 0.5f),
                    new GradientColorKey(Color.yellow, 0.75f),
                    new GradientColorKey(Color.red, 1.0f)
                    },
                    new GradientAlphaKey[2]
                    {
                    new GradientAlphaKey(0.1f,0.0f),
                    new GradientAlphaKey(0.2f,1.0f)
                    }
                    );
            }

            GameObject safeTarget = m_PreviewGameObject;
            float size = m_SearchablePointGrid.CellSize;

            foreach (var kvp in m_SearchablePointGrid.cells)
            {
                var c = kvp.Key;
                Vector3 v = new Vector3((c.x + 0.5f) * size, (c.y + 0.5f) * size, (c.z + 0.5f) * size);
                v = safeTarget.transform.TransformPoint(v);

                Handles.color = prevgradient.Evaluate((float)kvp.Value.Count / 10.0f);
                Handles.CubeHandleCap(0, v, safeTarget.transform.rotation, size, EventType.Repaint);
                Handles.Label(v, kvp.Value.Count.ToString());
            }
        }
    }

}
