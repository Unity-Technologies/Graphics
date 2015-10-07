using UnityEngine;

namespace UnityEditor.Graphs.Material
{
	public class SubGraphOutputsNode : SubGraphIOBaseNode
	{
		public override void Init()
		{
			base.Init();
			name = "SubGraphOutputs";
			title = "Outputs";
			position = new Rect(BaseMaterialGraphGUI.kDefaultNodeWidth * 8, BaseMaterialGraphGUI.kDefaultNodeHeight * 2, Mathf.Max(300, position.width), position.height);
		}

		public override void AddSlot ()
		{
			AddSlot (new Slot (SlotType.InputSlot, GenerateSlotName (SlotType.InputSlot)));
		}
	}
}
