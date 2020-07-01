// I am not sure why exactly, by a lower epsilon generates ray that even if they give a valid result with ray tracing
// nuke the performance. Changing the epsilon from 1e-6 to 1e-5 seems to solve the issue.
#define PLANE_INTERSECTION_EPSILON 1e-5

bool IntersectPlane(float3 ray_origin, float3 ray_dir, float3 pos, float3 normal, out float t)
{
    float denom = dot(normal, ray_dir); 
    if (abs(denom) > PLANE_INTERSECTION_EPSILON)
    { 
        float3 d = pos - ray_origin;
        t = dot(d, normal) / denom;
        return (t >= 0); 
    } 
    return false; 
}

struct SphQuad
{
    float3 o, x, y, z;
    float z0, z0sq;
    float x0, y0, y0sq;
    float x1, y1, y1sq;
    float b0, b1, b0sq, k;
    float S;
};

void SphQuadInit(float3 s, float3 ex, float3 ey, float3 o, inout SphQuad squad)
{
    squad.o = o;

    float exl = length(ex);
    float eyl = length(ey);

    // compute local reference system 'R'
    squad.x = ex / exl;
    squad.y = ey / eyl;
    squad.z = cross(squad.x, squad.y);

    // compute rectangle coords in local reference system
    float3 d = s - o;
    squad.z0 = dot(d, squad.z);

    // flip 'z' to make it point against 'Q'
    if (squad.z0 > 0.0f) {
        squad.z  = -squad.z;
        squad.z0 = -squad.z0;
    }

    squad.z0sq = squad.z0 * squad.z0;
    squad.x0 = dot(d, squad.x);
    squad.y0 = dot(d, squad.y);
    squad.x1 = squad.x0 + exl;
    squad.y1 = squad.y0 + eyl;
    squad.y0sq = squad.y0 * squad.y0;
    squad.y1sq = squad.y1 * squad.y1;

    // create vectors to four vertices
    float3 v00 = float3(squad.x0, squad.y0, squad.z0);
    float3 v01 = float3(squad.x0, squad.y1, squad.z0);
    float3 v10 = float3(squad.x1, squad.y0, squad.z0);
    float3 v11 = float3(squad.x1, squad.y1, squad.z0);

    // compute normals to edges
    float3 n0 = normalize(cross(v00, v10));
    float3 n1 = normalize(cross(v10, v11));
    float3 n2 = normalize(cross(v11, v01));
    float3 n3 = normalize(cross(v01, v00));

    // compute internal angles (gamma_i)
    float g0 = FastACos(-dot(n0, n1));
    float g1 = FastACos(-dot(n1, n2));
    float g2 = FastACos(-dot(n2, n3));
    float g3 = FastACos(-dot(n3, n0));
    
    // compute predefined constants
    squad.b0 = n0.z;
    squad.b1 = n2.z;
    squad.b0sq = squad.b0 * squad.b0;
    squad.k = 2.0f * PI - g2 - g3;

    // compute solid angle from internal angles
    squad.S = g0 + g1 - squad.k;
}

float3 SphQuadSample(SphQuad squad, float u, float v)
{
    // 1. compute 'cu'
    float au = u * squad.S + squad.k;
    float fu = (cos(au) * squad.b0 - squad.b1) / sin(au);
    float cu = 1.0f / sqrt(fu*fu + squad.b0sq) * (fu > 0.0f ? 1.0f : -1.0f);
    cu = clamp(cu, -1.0f, 1.0f); // avoid NaNs

    // 2. compute 'xu'
    float xu = -(cu * squad.z0) / sqrt(1.0f - cu * cu);
    xu = clamp(xu, squad.x0, squad.x1); // avoid Infs

    // 3. compute 'yv'
    float d = sqrt(xu * xu + squad.z0sq);
    float h0 = squad.y0 / sqrt(d*d + squad.y0sq);
    float h1 = squad.y1 / sqrt(d*d + squad.y1sq);
    float hv = h0 + v * (h1 - h0), hv2 = hv * hv;
    float yv = (hv2 < 1.0f - 1e-6f) ? (hv * d) / sqrt(1.0f - hv2) : squad.y1;

    // 4. transform (xu, yv, z0) to world coords
    return squad.o + xu*squad.x + yv*squad.y + squad.z0*squad.z;
}
