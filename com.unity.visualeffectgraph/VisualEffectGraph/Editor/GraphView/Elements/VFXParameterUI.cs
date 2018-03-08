using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Profiling;

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
            string path = null;
            if (s_Cache.TryGetValue(name, out path))
            {
                return path;
            }
            return GetUXMLPathRecursive("Assets", name);
        }

        static Dictionary<string, string> s_Cache = new Dictionary<string, string>();

        static string GetUXMLPathRecursive(string path, string name)
        {
            Profiler.BeginSample("UXMLHelper.GetUXMLPathRecursive");
            string localFileName = path + "/" + folderName + "/" + name;
            if (System.IO.File.Exists(localFileName))
            {
                Profiler.EndSample();
                s_Cache[name] = localFileName;
                return localFileName;
            }

            foreach (var dir in System.IO.Directory.GetDirectories(path))
            {
                if (dir.Length <= folderName.Length || !dir.EndsWith(folderName) || !"/\\".Contains(dir[dir.Length - folderName.Length - 1]))
                {
                    string result = GetUXMLPathRecursive(dir, name);
                    if (result != null)
                    {
                        Profiler.EndSample();
                        return result;
                    }
                }
            }

            Profiler.EndSample();
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

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            if (evt.GetEventTypeId() == MouseEnterEvent.TypeId() || evt.GetEventTypeId() == MouseLeaveEvent.TypeId())
            {
                VFXView view = GetFirstAncestorOfType<VFXView>();
                if (view != null)
                {
                    VFXBlackboard blackboard = view.blackboard;

                    VFXBlackboardRow row = blackboard.GetRowFromController(controller.parentController);

                    if (evt.GetEventTypeId() == MouseEnterEvent.TypeId())
                        row.AddToClassList("hovered");
                    else
                        row.RemoveFromClassList("hovered");
                }
            }
            base.ExecuteDefaultAction(evt);
        }
    }
}
