using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Channels/Splat Node")]
    class SplatNode : Function1Input
    {
        [SerializeField]
        private int m_SwizzleChannel;

        public override void Init()
        {
            base.Init();
            name = "SplatNode";
            m_SwizzleChannel = 0;
        }

        private string GetChannelFromConfiguration()
        {
            switch (m_SwizzleChannel)
            {
                case 0:
                    return "xxxx";
                case 1:
                    return "yyyy";
                case 2:
                    return "zzzz";
                default:
                    return "wwww";
            }
        }

        public override float GetNodeUIHeight(float width)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override bool NodeUI(Rect drawArea)
        {
            base.NodeUI(drawArea);
            string[] values = {"x", "y", "z", "w"};
            EditorGUI.BeginChangeCheck();
            m_SwizzleChannel = EditorGUI.Popup(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), "Channel", m_SwizzleChannel, values);
            if (EditorGUI.EndChangeCheck())
            {
                RegeneratePreviewShaders();
                return true;
            }
            return false;
        }

        protected override string GetFunctionName()
        {
            return "";
        }

        protected override string GetFunctionCallBody(string inputValue)
        {
            return inputValue + "." + GetChannelFromConfiguration();
        }
    }
}
