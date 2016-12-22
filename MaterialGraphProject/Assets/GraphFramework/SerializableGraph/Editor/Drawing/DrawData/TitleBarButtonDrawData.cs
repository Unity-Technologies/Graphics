using UnityEngine;
namespace UnityEditor.Graphing.Drawing
{
    public class TitleBarButtonDrawData : ScriptableObject
    {
        public delegate void ClickCallback();

        public string text;
        public ClickCallback onClick { get; set; }

        protected TitleBarButtonDrawData() {}
    }
}
