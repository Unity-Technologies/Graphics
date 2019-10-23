
void GetSurfaceData(FragInputs input, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
{
	surfaceData.output = LOAD_TEXTURE2D_X(_InputFrame, input.positionSS.xy) * float4(0, 0, 1, 1);
}
