namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Describes a value that is a GPU resource (a texture or a buffer) rather than a plain uniform. A resource
    /// is bound directly under its binding name, with no container variable holding it. What it declares in
    /// the generated shader is the business of the writer registered for the concrete description.
    /// </summary>
    /*public*/ abstract class ResourceValueData : ValueData
    {
    }
}
