using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{
    class CustomDensityVolume : MonoBehaviour
    {
        public Vector3 size = Vector3.one;
        [HideInInspector]
        public Bounds aabb { get{
                _aabb.center = transform.position;
                _aabb.size = Vector3.zero;

                var sizeBy2 = size * 0.5f;
                _aabb.Encapsulate(transform.TransformPoint(new Vector3(-sizeBy2.x, -sizeBy2.y, -sizeBy2.z)));
                _aabb.Encapsulate(transform.TransformPoint(new Vector3(-sizeBy2.x, -sizeBy2.y,  sizeBy2.z)));
                _aabb.Encapsulate(transform.TransformPoint(new Vector3(-sizeBy2.x,  sizeBy2.y, -sizeBy2.z)));
                _aabb.Encapsulate(transform.TransformPoint(new Vector3(-sizeBy2.x,  sizeBy2.y,  sizeBy2.z)));
                _aabb.Encapsulate(transform.TransformPoint(new Vector3( sizeBy2.x, -sizeBy2.y, -sizeBy2.z)));
                _aabb.Encapsulate(transform.TransformPoint(new Vector3( sizeBy2.x, -sizeBy2.y,  sizeBy2.z)));
                _aabb.Encapsulate(transform.TransformPoint(new Vector3( sizeBy2.x,  sizeBy2.y, -sizeBy2.z)));
                _aabb.Encapsulate(transform.TransformPoint(new Vector3( sizeBy2.x,  sizeBy2.y,  sizeBy2.z)));

                return _aabb;
            } }
        private Bounds _aabb = new Bounds();

        public ComputeShader compute;

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
