namespace UnityEditor.ShaderGraph
{
    public class NewHueNode : IShaderNode
    {
        PortRef m_InPort;
        PortRef m_OffsetPort;
        PortRef m_OutPort;

        public void Setup(ref NodeSetupContext context)
        {
            var type = new NodeTypeDescriptor
            {
                path = "Artistic/Adjustment",
                name = "Hue"
            };
            m_InPort = type.AddInput(0, "In", PortValue.Vector3());
            m_OffsetPort = type.AddInput(1, "Offset", PortValue.Vector1(0.5f));
            m_OutPort = type.AddOutput(2, "Out", PortValueType.Vector3);
            context.RegisterType(type);
        }

        public void OnChange(NodeChangeContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
