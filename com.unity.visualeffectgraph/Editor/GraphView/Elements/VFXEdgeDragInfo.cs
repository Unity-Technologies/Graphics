//#define OLD_COPY_PASTE
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEngine.UIElements;
using UnityEngine.Profiling;
using System.Reflection;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    class VFXEdgeDragInfo : VisualElement
    {
        VFXView m_View;
        public VFXEdgeDragInfo(VFXView view)
        {
            m_View = view;
            var tpl = VFXView.LoadUXML("VFXEdgeDragInfo");
            tpl.CloneTree(this);

            this.styleSheets.Add(VFXView.LoadStyleSheet("VFXEdgeDragInfo"));

            m_Text = this.Q<Label>("title");

            pickingMode = PickingMode.Ignore;
            m_Text.pickingMode = PickingMode.Ignore;
        }

        Label m_Text;

        public void DisplayEdgeDragInfo(VFXDataAnchor draggedAnchor, VFXDataAnchor overAnchor)
        {
            if (m_ScheduledItem != null)
            {
                m_ScheduledItem.Pause();
                m_ScheduledItem = null;
            }
            string error = null;
            if (draggedAnchor != overAnchor)
            {
                if (draggedAnchor.direction == overAnchor.direction)
                {
                    if (draggedAnchor.direction == Direction.Input)
                        error = "You must link an input to an output";
                    else
                        error = "You must link an output to an input";
                }
                else if (draggedAnchor.controller.connections.Any(t => draggedAnchor.direction == Direction.Input ? t.output == overAnchor.controller : t.input == overAnchor.controller))
                {
                    error = "An edge with the same input and output already exists";
                }
                else if (!draggedAnchor.controller.model.CanLink(overAnchor.controller.model))
                {
                    error = "The input and output have incompatible types";
                }
                else
                {
                    bool can = draggedAnchor.controller.CanLink(overAnchor.controller);

                    if (!can)
                    {
                        if (!draggedAnchor.controller.CanLinkToNode(overAnchor.controller.sourceNode, null))
                            error = "The edge would create a loop in the operators";
                        else
                            error = "Link impossible for an unknown reason";
                    }
                }
            }
            if (error == null)
            {
                m_Displaying = false;

                style.display = DisplayStyle.None;
            }
            else
            {
                m_Displaying = true;
                m_Text.text = error;
                style.display = DisplayStyle.Flex;
            }

            Rect anchorLayout = overAnchor.connector.parent.ChangeCoordinatesTo(m_View, overAnchor.connector.layout);

            style.top = anchorLayout.yMax + 16;
            style.left = anchorLayout.xMax;


            //make sure the info is within the view
            Rect viewLayout = m_View.layout;
            Vector2 size = layout.size;
            if (style.top.value.value < 0)
                style.top = 0;
            if (style.left.value.value < 0)
                style.left = 0;
            if (style.top.value.value + size.y > viewLayout.yMax)
            {
                style.top = viewLayout.yMax - size.y;
            }
            if (style.left.value.value + size.x > viewLayout.xMax)
            {
                style.left = viewLayout.xMax - size.x;
            }
        }

        IVisualElementScheduledItem m_ScheduledItem;
        bool m_Displaying;

        public void StartEdgeDragInfo(VFXDataAnchor draggedAnchor, VFXDataAnchor overAnchor)
        {
            if (m_Displaying)
                DisplayEdgeDragInfo(draggedAnchor, overAnchor);
            else if (m_ScheduledItem == null)
                m_ScheduledItem = m_View.schedule.Execute(t => DisplayEdgeDragInfo(draggedAnchor, overAnchor)).StartingIn(1000);
        }

        public void StopEdgeDragInfo()
        {
            style.display = DisplayStyle.None;
            if (m_ScheduledItem != null)
            {
                m_ScheduledItem.Pause();
                m_ScheduledItem = null;
            }
            m_Displaying = false;
        }
    }
}
