namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    public static class SCEUtils
    {
        static readonly string[] k_ProfileIds = { "sce_ps_orbis" , "sce_cs_orbis" };

        public static string GetProfileString(ShaderProfile profile)
        {
            return k_ProfileIds[(int)profile];
        }
    }
}
