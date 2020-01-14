namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDLitMasterNode
    {
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            var _ = k_Migrations.Migrate(this);
        }

        static partial class Migrations
        {
#pragma warning disable 618
            public static void InitialVersion(HDLitMasterNode instance)
            {
            }
#pragma warning restore 618
        }
    }
}
