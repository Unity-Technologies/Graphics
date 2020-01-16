namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, omitStructDeclaration = true)]
    struct ShaderVariablesDecal
    {
        public Vector2  _DecalAtlasResolution;
        public uint    _EnableDecals;
        public uint    _DecalCount;
    }
}

