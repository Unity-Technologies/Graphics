
using System.Collections.Generic;

using UnityEditorInternal;

using UnityEditor.EditorTools;

using UnityEngine;

using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;

using UnityEditor;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.Universal.Editor")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Editor")]

namespace UnityEditor.Rendering.Utilities
{
    class GenericEditorTool<T> : EditorTool where T : Component
    {
        readonly string m_Description;
        readonly EditMode.SceneViewEditMode m_Mode;
        readonly string m_IconName;
        GUIContent m_IconContent;

        protected GenericEditorTool(string description, EditMode.SceneViewEditMode mode, string iconName)
        {
            m_Description = description;
            m_Mode = mode;
            m_IconName = iconName;
        }

        public override GUIContent toolbarIcon => m_IconContent;
        public override void OnWillBeDeactivated() => EditMode.SetEditModeToNone();
        public override void OnToolGUI(EditorWindow window)
        {
            if (EditMode.editMode == m_Mode)
                return;

            List<T> usefulTargets = new();
            foreach (Object thisTarget in targets)
                if (thisTarget is T usefulTarget)
                    usefulTargets.Add(usefulTarget);

            if (usefulTargets.Count == 0)
                return;

            Bounds bounds = GetBoundsOfTargets(usefulTargets);
            EditMode.ChangeEditMode(m_Mode, bounds);
            ToolManager.SetActiveTool(this);
        }

        private static Bounds GetBoundsOfTargets(IEnumerable<T> targets)
        {
            var bounds = new Bounds { min = Vector3.positiveInfinity, max = Vector3.negativeInfinity };
            foreach (T t in targets)
                bounds.Encapsulate(t.transform.position);

            return bounds;
        }

        private void OnEnable() => m_IconContent = EditorGUIUtility.TrIconContent(m_IconName, m_Description);
    }
}
