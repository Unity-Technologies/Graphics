using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Reflection;
using UnityEditor.Graphs;

namespace UnityEditor.MaterialGraph
{
    class MaterialSubGraphGUI : BaseMaterialGraphGUI
    {
        public override void NodeGUI(Node n)
        {
            // Handle node selection
            SelectNode(n);
            // Make node minimize, etc. buttons
            // Make default input slots
            foreach (Slot s in n.inputSlots)
                LayoutSlot(s, s.title, false, true, false, Styles.varPinIn);
            // Make custom node UI
            n.NodeUI(this);
            // Make default output slots
            foreach (Slot s in n.outputSlots)
                LayoutSlot(s, s.title, true, false, true, Styles.varPinOut);

            var subnode = n as SubGraphIOBaseNode;
            if (subnode != null)
                subnode.FooterUI(this);

            DragNodes();
        }
    }
}
