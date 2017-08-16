using UnityEngine;
namespace UnityEditor.Graphing.Drawing
{
    // TODO JOCE: Needed at all?
    public class TitleBarButtonPresenter : ScriptableObject
    {
        public delegate void ClickCallback();

        public string text;
        public ClickCallback onClick { get; set; }

        protected TitleBarButtonPresenter() {}
    }
}
