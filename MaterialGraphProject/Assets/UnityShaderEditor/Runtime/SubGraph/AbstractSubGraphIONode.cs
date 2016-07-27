namespace UnityEngine.MaterialGraph
{
    public abstract class AbstractSubGraphIONode : AbstractMaterialNode
    {
        public abstract int AddSlot();

        public abstract void RemoveSlot();

        /*public void FooterUI(GraphGUI host)
        {
            // TODO: make it pretty
            GUIStyle style = this is SubGraphOutputNode ?
                Styles.GetNodeStyle("shader in", Styles.Color.Aqua, false) :
                Styles.GetNodeStyle("shader out", Styles.Color.Aqua, false);
            var pos = GUILayoutUtility.GetRect(kTempContent, style);
            int id = GUIUtility.GetControlID(FocusType.Passive);

            Event evt = Event.current;
            if (evt.type == EventType.Layout || evt.type == EventType.Used)
                return;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseUp:
                    if (pos.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        AddSlot();
                        evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    style.Draw(pos, kTempContent, id);
                    break;
            }
        }*/
    }
}
