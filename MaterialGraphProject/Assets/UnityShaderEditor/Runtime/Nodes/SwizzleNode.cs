namespace UnityEngine.MaterialGraph
{
    [Title("Channels/Swizzle Node")]
    public class SwizzleNode : Function1Input
    {
        enum SwizzleChannels
        {
            R = 0,
            G = 1,
            B = 2,
            A = 3,
        }

        [SerializeField]
        private SwizzleChannels[] m_SwizzleChannels = new SwizzleChannels[4] { SwizzleChannels.R, SwizzleChannels.G, SwizzleChannels.B, SwizzleChannels.A };


        public SwizzleNode()
        {
            name = "SwizzleNode";
        }

        /*
        public override float GetNodeUIHeight(float width)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override GUIModificationType NodeUI(Rect drawArea)
        {
            base.NodeUI(drawArea);
            string[] channelNames = {"X", "Y", "Z", "W"};
            string[] values = {"0", "1", "Input1.x", "Input1.y", "Input1.z", "Input1.w", "Input2.x", "Input2.y", "Input2.z", "Input2.w"};
            EditorGUI.BeginChangeCheck();
            for (int n = 0; n < 4; n++)
                m_SwizzleChannel[n] = EditorGUI.Popup(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), channelNames[n] + "=", m_SwizzleChannel[n], values);
            if (EditorGUI.EndChangeCheck())
            {
                pixelGraph.RevalidateGraph();
                return GUIModificationType.Repaint;
            }
            return GUIModificationType.None;
        }
        */
        protected override string GetFunctionName()
        {
            return "";
        }

        protected override string GetFunctionCallBody(string inputValue)
        {
            string[] channelNames = { "x", "y", "z", "w" };
            int numInputChannels = (int)FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType;
            int numOutputChannels = (int)FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType;

            if (numOutputChannels == 0)
            {
                return "1.0";
            }

            string outputString = precision + "4(";
            //float4(1.0,1.0,1.0,1.0)
            if (numInputChannels == 0)
            {
                outputString += "1.0, 1.0, 1.0, 1.0)";
            }
            else
            {
                //float4(arg1.
                outputString += inputValue + ".";

                //float4(arg1.xy
                int i = 0;
                for (; i < numInputChannels; i++)
                {
                    int channel = (int)m_SwizzleChannels[i];
                    outputString += channelNames[channel];
                }

                //float4(arg1.xy, 1.0, 1.0)
                for (; i < 4; i++)
                {
                    outputString += ", 1.0";
                }
                outputString += ").";
            }

            //float4(arg1.xy, 1.0, 1.0).rg
            for (int j = 0; j < numOutputChannels; j++)
            {
                int channel = (int)m_SwizzleChannels[j];
                outputString += channelNames[channel];
            }
            return outputString;
        }
    }
}
