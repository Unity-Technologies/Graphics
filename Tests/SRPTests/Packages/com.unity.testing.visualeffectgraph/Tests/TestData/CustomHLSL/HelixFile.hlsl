/*test
test*/

void Helix(
    // Test comment
    inout VFXAttributes attributes,
    in float3 Center,
    in float Radius,        /*test
test*/
    in float RotationSpeed,
    in float RiseSpeed,
    in float /*test
test*/ TotalTime
/*test
test*/
)
/*test
test*/
{
/*test
test*/
    float t = attributes.age / attributes.lifetime;
/*test
test*/

    float angle = (t * 6.28 * 2.0) + (TotalTime * RotationSpeed); /*test
test*/
    float3 targetPos;
    targetPos.x = Center.x + cos(angle) * Radius;
    targetPos.z /*test
test*/ = Center.z + sin(angle) * Radius;

  /*test
test*/  targetPos.y = Center.y + (attributes.age * RiseSpeed);

    attributes.position = targetPos;
/*test
test*/
}
/*test
test*/
