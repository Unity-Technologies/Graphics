using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXParameterDataAnchor : VFXOutputDataAnchor
    {
        public static new VFXParameterDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            var anchor = new VFXParameterDataAnchor(controller.orientation, controller.direction, controller.portType, node);

            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = controller;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXParameterDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, type, node)
        {
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return base.ContainsPoint(localPoint) && !m_ConnectorText.ContainsPoint(this.ChangeCoordinatesTo(m_ConnectorText, localPoint));
        }
    }


    static class UXMLHelper
    {
        const string folderName = "Editor Default Resources";

        public static string GetUXMLPath(string name)
        {
            return GetUXMLPathRecursive("Assets", name);
        }

        static string GetUXMLPathRecursive(string path, string name)
        {
            string localFileName = path + "/" + folderName + "/" + name;
            if (System.IO.File.Exists(localFileName))
            {
                return localFileName;
            }

            foreach (var dir in System.IO.Directory.GetDirectories(path))
            {
                if (dir.Length <= folderName.Length || !dir.EndsWith(folderName) || !"/\\".Contains(dir[dir.Length - folderName.Length - 1]))
                {
                    string result = GetUXMLPathRecursive(dir, name);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }
    }


    class VFXParameterUI : VFXNodeUI
    {
        Image m_Icon;
        public VFXParameterUI() : base(UXMLHelper.GetUXMLPath("uxml/VFXParameter.uxml"))
        {
            RemoveFromClassList("VFXNodeUI");
            AddStyleSheetPath("VFXParameter");
        }

        public new VFXParameterNodeController controller
        {
            get { return base.controller as VFXParameterNodeController; }
        }

        protected override bool syncInput
        {
            get { return false; }
        }

        public override VFXDataAnchor InstantiateDataAnchor(VFXDataAnchorController controller, VFXNodeUI node)
        {
            return VFXParameterDataAnchor.Create(controller, node);
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            if (controller.parentController.exposed)
            {
                AddToClassList("exposed");
            }
            else
            {
                RemoveFromClassList("exposed");
            }
        }
    }
}
