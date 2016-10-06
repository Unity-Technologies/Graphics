/*using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Graphing.Drawing
{
    internal class DeleteSelected : IManipulate
    {
        public delegate void DeleteElements(List<CanvasElement> elements);

        private readonly DeleteElements m_DeletionCallback;

        public DeleteSelected(DeleteElements deletionCallback)
        {
            m_DeletionCallback = deletionCallback;
        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.ValidateCommand += Validate;
            element.ExecuteCommand += Delete;
        }

        private bool Validate(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            if (e.commandName != "Delete" && e.commandName != "SoftDelete")
                return false;

            e.Use();
            return true;
        }

        private bool Delete(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            if (e.commandName != "Delete" && e.commandName != "SoftDelete")
                return false;


            if (m_DeletionCallback != null)
            {
                m_DeletionCallback(parent.selection);
                parent.ReloadData();
                parent.Repaint();
            }
            e.Use();
            return true;
        }
    }
}
*/