
void GetSurfaceData(FragInputs input, float3 V, PositionInputs posInput, out PostProcessSurfaceData surfaceData)
{
	surfaceData.output = LOAD_TEXTURE2D_X(_InputFrame, input.positionSS.xy).xyz * float3(0, 0, 1);
}
