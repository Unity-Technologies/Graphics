using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental
{
    internal class VFXEdLayoutUtility
    {
        internal static void LayoutSystem(VFXSystemModel system, VFXEdDataSource datasource)
        {
            if (system.GetNbChildren() > 0)
                LayoutSystem(system, datasource, datasource.GetUI<VFXEdContextNode>(system.GetChild(0)).translation);

        }
        internal static void LayoutSystem(VFXSystemModel system, VFXEdDataSource datasource, Vector2 initialPosition)
        {
            Vector2 pos = initialPosition;

            for(int i =0; i < system.GetNbChildren(); ++i)
            {
                VFXContextModel context = system.GetChild(i);
                VFXEdContextNode node = datasource.GetUI<VFXEdContextNode>(context);
                node.translation = pos;
                pos.y += node.scale.y + 50;
            }
            
        }
    }
}
