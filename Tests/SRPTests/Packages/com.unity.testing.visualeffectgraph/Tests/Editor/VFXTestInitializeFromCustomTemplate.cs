namespace UnityEditor.VFX.Test
{
    class VFXTestInitializeFromCustomTemplate : VFXBasicInitialize
    {
        public static string templatePathWithoutExtension;

        public override string codeGeneratorTemplate => templatePathWithoutExtension;
    }
}
