void ScreenToProjectionPos_float(float4 screenPos, out float4 projectionPos)
{
  float4 o = 2 * screenPos;
  projectionPos.x = (2.0f * screenPos.x - screenPos.w);
  projectionPos.y = (2.0f * screenPos.y - screenPos.w) / _ProjectionParams.x;
  projectionPos.zw = screenPos.zw;
}
