namespace UnityEditor.ShaderGraph.Legacy
{
    public interface ILegacyTarget
    {
        bool TryUpgradeFromMasterNode(IMasterNode1 masterNode);
    }
}
