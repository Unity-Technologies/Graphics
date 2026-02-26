using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Data structure that can handle creation and deletion of IDs.
    /// </summary>
    /*public*/ class IdGeneratorData : IDataDescription
    {
        /// <inheritdoc cref="IDataDescription"/>
        public bool IsCompatible(IDataDescription other)
        {
            return other is IdGeneratorData;
        }
    }
}
