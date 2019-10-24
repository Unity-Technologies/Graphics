
void GetSurfaceData(FragInputs input, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
{
	surfaceData.output = LOAD_TEXTURE2D_X(_PostProcessInput, input.texCoord0.xy) * float4(0, 0, 1, 1);
}
