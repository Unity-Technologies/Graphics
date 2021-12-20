using UnityEditor;

namespace WaterSystem
{
    [CustomEditor(typeof(BuoyantObject))]
    public class BuoyantObjectEditor : Editor
    {
        private BuoyantObject obj;
        private bool _heightsDebugBool;
        private bool _generalSettingsBool;

        private void OnEnable()
        {
            obj = serializedObject.targetObject as BuoyantObject;
        }

        public override void OnInspectorGUI()
        {
            _generalSettingsBool = EditorGUILayout.BeginFoldoutHeaderGroup(_generalSettingsBool, "General Settings");
            if (_generalSettingsBool)
            {
                base.OnInspectorGUI();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _heightsDebugBool = EditorGUILayout.BeginFoldoutHeaderGroup(_heightsDebugBool, "Height Debug Values");
            if (_heightsDebugBool)
            {
                if (obj.Heights != null)
                {
                    for (var i = 0; i < obj.Heights.Length; i++)
                    {
                        var h = obj.Heights[i];
                        EditorGUILayout.LabelField($"{i})Wave(heights):", $"X:{h.x:00.00} Y:{h.y:00.00} Z:{h.z:00.00}");
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Height debug info only available in playmode.", MessageType.Info);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
