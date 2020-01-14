namespace UnityEditor.Rendering.HighDefinition
{
    partial class EyeMasterNode
    {
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            var _ = k_Migrations.Migrate(this);
        }

        static partial class Migrations
        {
#pragma warning disable 618
            public static void InitialVersion(EyeMasterNode instance)
            {
            }
#pragma warning restore 618
        }
    }
}
