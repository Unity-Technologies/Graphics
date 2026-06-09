namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)ConstantRegister.RayTracingLightLoop)]
    unsafe struct ShaderVariablesRaytracingLightLoop
    {
        // Cluster bounds are Vector4 (xyz used) so the trailing scalar lands at a vec4
        // boundary every backend agrees on. A bare Vector3 here would put _LightPerCellCount
        // at byte 12 on std140/D3D but 16 on Metal (a regular MSL float3 is 16 bytes),
        // silently corrupting the cbuffer on Metal.
        public Vector4 _MinClusterPos;
        public Vector4 _MaxClusterPos;
        public uint _LightPerCellCount;
    }
}
