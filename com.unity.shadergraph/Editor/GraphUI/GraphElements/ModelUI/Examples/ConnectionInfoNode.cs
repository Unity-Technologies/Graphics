﻿using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements
{
    public class ConnectionInfoNode : CollapsibleInOutNode
    {
        public const string InfoTextPartName = "info-container";

        protected override void BuildPartList()
        {
            base.BuildPartList();
            PartList.AppendPart(new ConnectionInfoPart(InfoTextPartName, Model, this, ussClassName));
        }
    }
}
