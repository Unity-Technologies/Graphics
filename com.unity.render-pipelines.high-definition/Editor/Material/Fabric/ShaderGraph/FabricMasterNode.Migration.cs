namespace UnityEditor.Rendering.HighDefinition
{
    partial class FabricMasterNode
    {
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            var _ = k_Migrations.Migrate(this);
        }

        static partial class Migrations
        {
#pragma warning disable 618
            public static void InitialVersion(FabricMasterNode instance)
            {
            }
#pragma warning restore 618
        }
    }
}
