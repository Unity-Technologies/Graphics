using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEngine.Experimental.UIElements
{
    public class Element3D : VisualElement
    {
        Mesh m_Mesh;

        Material m_Material;

        public Vector3 position { get; set; }
        public Vector3 eulerAngles { get; set; }

        public Element3D()
        {
            clipChildren = true;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);

            m_Mesh = go.GetComponent<MeshFilter>().sharedMesh;
            m_Material = go.GetComponent<MeshRenderer>().sharedMaterial;

            GameObject.DestroyImmediate(go);

            position = new Vector3(0, 0, -5);
            eulerAngles = Vector3.zero;
        }

        public override void DoRepaint()
        {
            Rect panelRect = this.panel.visualTree.layout;

            Rect viewPort = this.parent.ChangeCoordinatesTo(this.panel.visualTree, layout);
            float height = viewPort.height;
            viewPort.yMin = panelRect.height - viewPort.yMin - height;
            viewPort.height = height;

            GL.PushMatrix();
            GL.Viewport(viewPort);
            GL.Clear(true, true, Color.red);

            GL.LoadProjectionMatrix(Matrix4x4.Perspective(60, viewPort.width / viewPort.height, 0.01f, 100));
            GL.modelview = Matrix4x4.Translate(position) * Matrix4x4.Rotate(Quaternion.Euler(eulerAngles));

            m_Material.SetPass(0);
            /*
            GL.Begin(GL.TRIANGLES);
            GL.Vertex3(-1, 1, 0);
            GL.Vertex3(1, 1, 0);
            GL.Vertex3(0, -1, 0);
            GL.End();
            */
            Graphics.DrawMeshNow(m_Mesh, Matrix4x4.identity);

            GL.PopMatrix();
        }
    }
}
