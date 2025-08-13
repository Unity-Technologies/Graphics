namespace UnityEditor.ShaderGraph
{
    /// <summary>
    /// Literal mode says that the property being passed to a slot / node is a literal
    /// This is used if the node internally has a conditional statement that produces a texture reference.
    /// In these situations, the shader compiler requires the value being used for the switch / if to be known at compile time.
    /// </summary>
    internal interface IMaterialSlotSupportsLiteralMode
    {
        public bool LiteralMode { get; set; }
    }
}
