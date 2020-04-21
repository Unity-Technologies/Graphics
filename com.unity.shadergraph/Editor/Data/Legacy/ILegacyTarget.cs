namespace UnityEditor.ShaderGraph.Legacy
{
    public interface ILegacyTarget
    {
        bool TryUpgradeFromMasterNode(MasterNode1 masterNode);
    }
}
