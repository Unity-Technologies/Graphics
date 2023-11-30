
float DistanceToBox(float3 p, float3 extents)
{
    float3 q = abs(p) - extents;
    return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

bool RayBoxIntersection(float3 o, float3 d, float3 extents, float colliderSign, out float tHit, out float3 hitNormal)
{
    bool hit = false;
    float3 faceSelection = - colliderSign * float3(FastSign(d.x), FastSign(d.y), FastSign(d.z));

    float3 ts = (faceSelection * extents - o)/d;
    float3 posTx = o + ts.x*d;
    float3 posTy = o + ts.y*d;
    float3 posTz = o + ts.z*d;

    bool hitx = ts.x >= 0 && ts.x < 1 && all(abs(posTx.yz) < extents.yz);
    bool hity = ts.y >= 0 && ts.y < 1 && all(abs(posTy.xz) < extents.xz);
    bool hitz = ts.z >= 0 && ts.z < 1 && all(abs(posTz.xy) < extents.xy);

    if(!(hitx || hity || hitz))
    {
        hit = false;
        tHit = 0.0f;
        hitNormal = (float3)0;
    }
    else
    {
        hit = true;

        //Take the minimal valid t
        float3 tFiltered = float3(abs(ts.x) + !hitx, abs(ts.y) + !hity, abs(ts.z) + !hitz);
        tHit = min(tFiltered.x, min(tFiltered.y, tFiltered.z));
        if (tFiltered.x < tFiltered.y && tFiltered.x < tFiltered.z)
            hitNormal = float3(faceSelection.x, 0.0f, 0.0f);
        else if (tFiltered.y < tFiltered.z)
            hitNormal = float3(0.0f, faceSelection.y, 0.0f);
        else
            hitNormal = float3(0.0f, 0.0f, faceSelection.z);

        hitNormal *= colliderSign;
    }
    return hit;
}

float3 ProjectOnBox(float3 position, float3 extents)
{
    float3 distanceToEdge = max(0, abs(position) - extents);
    float3 posSign = float3(FastSign(position.x), FastSign(position.y), FastSign(position.z));
    return position - distanceToEdge * posSign ;
}

float3 IterateTowardSDFSurface(VFXSampler3D sdf, float3 uvw, float3 uvStep, float offset, float stepSizeMeter, out float3 normal)
{
    float dist = SampleSDF(sdf, uvw) - offset;
    normal = SampleSDFUnscaledDerivatives(sdf, uvw, uvStep);
    normal = normalize(normal);
    float3 delta = -dist * normal;
    delta *= uvStep/stepSizeMeter;
    uvw += delta;
    return uvw;
}
