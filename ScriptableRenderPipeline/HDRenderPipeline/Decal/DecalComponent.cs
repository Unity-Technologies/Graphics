using UnityEditor;
using UnityEngine;
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public class DecalComponent : MonoBehaviour
    {
        public enum Kind
        {
            DiffuseOnly,
            NormalsOnly,
            Both
        }

        public Kind m_Kind;
        public Material m_Material;

        public void OnEnable()
        {
            DecalSystem.instance.AddDecal(this);
        }

        public void Start()
        {
            DecalSystem.instance.AddDecal(this);
        }

        public void OnDisable()
        {
            DecalSystem.instance.RemoveDecal(this);
        }

        private void DrawGizmo(bool selected)
        {
            var col = new Color(0.0f, 0.7f, 1f, 1.0f);
            col.a = selected ? 0.3f : 0.1f;
            Gizmos.color = col;
            Matrix4x4 offset = Matrix4x4.Translate(new Vector3(0.0f, -0.5f, 0.0f));
            Gizmos.matrix = transform.localToWorldMatrix * offset;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            col.a = selected ? 0.5f : 0.2f;
            Gizmos.color = col;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        public void OnDrawGizmos()
        {
            //DrawGizmo(false);
        }
        public void OnDrawGizmosSelected()
        {
            DrawGizmo(true);
        }

        [MenuItem("GameObject/Effects/Decal", false, 0)]
        static void CreateDecal(MenuCommand menuCommand)
        {
            // Create a custom game object
            GameObject go = new GameObject("Decal");
            go.AddComponent<DecalComponent>();
            // Ensure it gets re-parented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
}
