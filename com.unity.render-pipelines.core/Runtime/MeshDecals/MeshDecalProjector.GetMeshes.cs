using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace UnityEngine.Rendering.MeshDecal
{
    public partial class MeshDecalProjector
    {
        public void GetMeshes()
        {
            meshFilters.Clear();
            // The meshFilters list need to be filled now.

            // Get all meshfilters in the scene.
            var allFilters = FindObjectsOfType<MeshFilter>();

            var projectorBounds = new Bounds(transform.position, Vector3.zero);

            var points = new Vector3[]
            {
            transform.TransformPoint( new Vector3(-m_Size.x, -m_Size.y, -m_Size.z)*0.5f ),
            transform.TransformPoint( new Vector3(-m_Size.x, -m_Size.y,  m_Size.z)*0.5f ),
            transform.TransformPoint( new Vector3(-m_Size.x,  m_Size.y, -m_Size.z)*0.5f ),
            transform.TransformPoint( new Vector3(-m_Size.x,  m_Size.y,  m_Size.z)*0.5f ),
            transform.TransformPoint( new Vector3( m_Size.x, -m_Size.y, -m_Size.z)*0.5f ),
            transform.TransformPoint( new Vector3( m_Size.x, -m_Size.y,  m_Size.z)*0.5f ),
            transform.TransformPoint( new Vector3( m_Size.x,  m_Size.y, -m_Size.z)*0.5f ),
            transform.TransformPoint( new Vector3( m_Size.x,  m_Size.y,  m_Size.z)*0.5f )
            };

            for (int i = 0; i < 8; i++)
            {
                projectorBounds.Encapsulate(points[i]);
                // Debug.DrawLine(points[i], points[i] + Vector3.up * 0.1f, Color.green, 10f);
            }

            // Debug.DrawLine(projectorBounds.min, projectorBounds.max, Color.red, 10f);

            meshFilters = allFilters.Where(mf => mf.GetComponent<MeshRenderer>().bounds.Intersects(projectorBounds)).ToList();
        }
    }
}
