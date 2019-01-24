// This structure defines holds the data that allows us to do solid angle sampling on a rectangle from a given point
struct SphericalRectangle
{
	float3 rectWSPos;
	float3 rectWSDir;

	float3 smpWSPos;
	float3 smpWSNormal;

	float3 smpV0;
	float3 smpV1;
	float3 smpV2;
	float3 smpV3;

	float3 h[2];

	float2 calpha;
	float2 salpha;
	float2 product;

	float2 alpha;
	float areaCoeff;

	// Solid angles
	float2 solidAngles;
	float totalSolidAngle;

	// Dimension of the light source
	float2 dimension;
};

bool IntersectPlane(float3 ray_origin, float3 ray_dir, float3 pos, float3 normal, out float t)
{
	float denom = dot(normal, ray_dir); 
	if (abs(denom) > 1e-6)
	{ 
	    float3 d = pos - ray_origin;
	    t = dot(d, normal) / denom;
	    return (t >= 0); 
	} 
	return false; 
}


bool SetupSphericalRectangle(float3 v0, float3 v1, float3 v2, float3 v3,
							float3 rectWSPos, float3 rectWSDir,
							float3 smpWSPos, float3 smpWSNormal, float2 dimension, 
							out SphericalRectangle outSr)
{
	outSr.dimension = dimension;
	outSr.rectWSPos = rectWSPos;
	outSr.rectWSDir = rectWSDir;

	outSr.smpWSPos = smpWSPos;
	outSr.smpWSNormal = smpWSNormal;

	outSr.smpV0 = v0 - outSr.smpWSPos;
	outSr.smpV1 = v1 - outSr.smpWSPos;
	outSr.smpV2 = v2 - outSr.smpWSPos;
	outSr.smpV3 = v3 - outSr.smpWSPos;

	bool invalid = dot(outSr.smpV0, smpWSNormal) < 0.0f && dot(outSr.smpV1, smpWSNormal) < 0.0f && dot(outSr.smpV2, smpWSNormal) < 0.0f && dot(outSr.smpV3, smpWSNormal) < 0.0f;
	if(invalid)
		return false;

	outSr.smpV0 = normalize(outSr.smpV0);
	outSr.smpV1 = normalize(outSr.smpV1);
	outSr.smpV2 = normalize(outSr.smpV2);
	outSr.smpV3 = normalize(outSr.smpV3);

	float cc0 =  clamp(dot(outSr.smpV0, outSr.smpV1), -1.0, 1.0);
	float cc1 =  clamp(dot(outSr.smpV2, outSr.smpV3), -1.0, 1.0);

	float3 nA0 = cross(outSr.smpV0, outSr.smpV1);
	float3 nA1 = cross(outSr.smpV1, outSr.smpV2);
	float3 nA2 = cross(outSr.smpV2, outSr.smpV0);

	float3 nB0 = cross(outSr.smpV2, outSr.smpV3);
	float3 nB1 = cross(outSr.smpV3, outSr.smpV0);
	float3 nB2 = cross(outSr.smpV0, outSr.smpV2);

	nA0 = normalize(nA0);
	nA1 = normalize(nA1);
	nA2 = normalize(nA2);

	nB0 = normalize(nB0);
	nB1 = normalize(nB1);
	nB2 = normalize(nB2);

	outSr.calpha.x = clamp(-dot(nA2, nA0), -1.0, 1.0);
	float cbeta0 = clamp(-dot(nA0, nA1), -1.0, 1.0);
	float cgamma0 = clamp(-dot(nA1, nA2), -1.0, 1.0);

	outSr.calpha.y = clamp(-dot(nB2, nB0), -1.0, 1.0);
	float cbeta1 = clamp(-dot(nB0, nB1), -1.0, 1.0);
	float cgamma1 = clamp(-dot(nB1, nB2), -1.0, 1.0);

	outSr.alpha.x = acos(outSr.calpha.x);
	float beta0 = acos(cbeta0);
	float gamma0 = acos(cgamma0);

	outSr.alpha.y = acos(outSr.calpha.y);
	float beta1 = acos(cbeta1);
	float gamma1 = acos(cgamma1);

	outSr.solidAngles.x = outSr.alpha.x + beta0 + gamma0 - PI;
	outSr.solidAngles.y = outSr.alpha.y + beta1 + gamma1 - PI;

	float dotp = dot(outSr.smpV2, outSr.smpV0);
	outSr.h[0] = outSr.smpV0 * -dotp + outSr.smpV2;
	outSr.h[1] = outSr.smpV2 * -dotp + outSr.smpV0;

	outSr.h[0] = normalize(outSr.h[0]);
	outSr.h[1] = normalize(outSr.h[1]);

	outSr.salpha.x = sin(outSr.alpha.x);
	outSr.salpha.y = sin(outSr.alpha.y);

	invalid = abs(outSr.salpha.x) < 1e-12 || abs(outSr.salpha.y) < 1e-12;
	if(invalid)
		return false;

	outSr.product.x = outSr.salpha.x * cc0;
	outSr.product.y = outSr.salpha.y * cc1;

	outSr.totalSolidAngle = outSr.solidAngles.x + outSr.solidAngles.y;
	outSr.areaCoeff = outSr.solidAngles.x / outSr.totalSolidAngle;

	return true;
}

bool SampleSphericalRectangle(SphericalRectangle sr, float2 rands, out float3 outDir, out float3 outPos)
{
	int faceIdx;
	if(rands.x < sr.areaCoeff)
	{
		faceIdx = 0;
		rands.x /= sr.areaCoeff;
	}
	else
	{
		faceIdx = 1;
		rands.x = (rands.x - sr.areaCoeff) / (1.0 - sr.areaCoeff);
	}

    float phi = rands.x * sr.solidAngles[faceIdx] - sr.alpha[faceIdx] + PI;
    float sphi = sin(phi);
    float cphi = cos(phi);

    float u = cphi + sr.calpha[faceIdx];
    float v = sphi - sr.product[faceIdx];

    float cbt = -v;
    float sbt = u;

    float q;
    if ((sr.salpha[faceIdx] * (sphi * cbt - cphi * sbt)) != 0.0f)
      q = (cbt + sr.calpha[faceIdx] * (cphi * cbt + sphi * sbt)) / (sr.salpha[faceIdx] * (sphi * cbt - cphi * sbt));
    else
      q = 0.0f;

    float q1 = 1.0f - q * q;
    if (q1 < 0.0f)
      q1 = 0.0f;

    float3 p0;
    float3 p1;
    if (faceIdx == 0)
    {
      p0 = sr.smpV0;
      p1 = sr.smpV1;
    }
    else
    {
      p0 = sr.smpV2;
      p1 = sr.smpV3;
    }

    float buf = sqrt(q1);
    float3 nc = p0 * q +  sr.h[faceIdx] *  buf;

    float dotp = dot(nc, p1);
    float z = 1.0f - rands.y * (1.0f - dotp);
    float z1 = 1.0f - z * z;
    if (z1 < 0.0f)
      z1 = 0.0f;

    float3 nd = nc - p1 * dotp;

    if(length(nd) < 0.00001f)
    {
    	outDir = p1;
    }
    else
    {
	    nd = normalize(nd);
		buf = sqrt(z1);
		outDir = p1 * z + nd * buf;
		outDir = normalize(outDir);
    }

    float vis = dot(sr.smpWSNormal, outDir);
    if ((vis <= 0.0f) || (dot(sr.smpWSNormal, outDir) <= 0.0f))
      return false;

    // Turn the sampling direction into a sample position on the light source.
    float3 vecL = sr.rectWSPos - sr.smpWSPos;
    float num = dot(vecL, sr.smpWSNormal);
    float t = 0.0f;
    if (!IntersectPlane(sr.smpWSPos, outDir, sr.rectWSPos, sr.rectWSDir, t))
    {
      return false;
    }

    // Spit out the out position
    outPos = outDir * t + sr.smpWSPos;

    // All done
    return true;
}