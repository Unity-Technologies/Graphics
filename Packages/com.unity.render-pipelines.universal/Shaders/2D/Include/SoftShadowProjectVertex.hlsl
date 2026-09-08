#ifndef SOFT_SHADOW_PROJECT_VERTEX_INCLUDED
#define SOFT_SHADOW_PROJECT_VERTEX_INCLUDED

// Vertex program for the Unity.SoftShadow geometry generator.
//
// Deliberately separate from ShadowProjectVertex.hlsl, which Unity.Legacy renders through and which
// is frozen -- every reference image in the 2D shadow suite was captured against it. In particular
// handoff v2 section 8's sign fix to the shading value is applied HERE and only here; applying it to
// CalculateShadowValue would move Legacy's output.
//
// This is a transliteration of SoftShadowVertexReference.Vert in the UniversalGraphicsTest_2D
// project, which is itself verified payload-by-payload against sim/sim_shader.py's vert() by
// SoftShadowGoldenTests. Three layers, each checked against the one above:
//
//     sim_shader.py vert()          float64, the definition of correct
//     SoftShadowVertexReference     double, verified to 1e-12 against the goldens
//     this file                     float, expected to differ only by precision
//
// So: keep the structure, the order of operations and the magic numbers identical to the reference.
// Anything reorganised here stops the comparison meaning anything.
//
// PROJECTION IS BY DISPLACEMENT, NOT DISTANCE. Each point is thrown by a caller-supplied 2D offset
// rather than a scalar distance along its own ray from the light, so parallel and sheared projections
// are expressible and no light position appears anywhere in the vertex program. The construction
// never needed radiality -- only that the throw be the same function of a point regardless of which
// vertex is asking -- and every test that used to read the light now reads a per-EDGE throw instead
// (SoftEdgeThrow). Each reduces to its old form exactly, not approximately, under a radial field:
//
//     hard(p)      p + len*normalize(p - light)  ->  p + d(p)
//     away(edge)   dot(n, mid - light) > 0       ->  dot(n, d(a) + d(b)) > 0
//     isPole       FSign(prev,v,L)*FSign(v,next,L) < 0  ->  awayPrev != awayNext
//     fin interior cross(v-L, prev-L) + cross(v-L, next-L)
//                                                ->  cross(tPrev, prev-v) + cross(tNext, next-v)
//     W            len * 2*sin(soft/2)           ->  |d| * 2*sin(soft/2)
//
// SOFTNESS IS PER POINT TOO, and SPLIT IN TWO. The fin's wedge (the shadow's lateral edge) and the
// band's width (its far boundary) take separate angles, so a caster can have crisp sides and a diffuse
// back or the reverse. W = |d| * 2*sin(soft/2) still belongs to the vertex whose band corner it places,
// so the band angle is carried at v, prev and next; the fin is that vertex's own geometry and is
// carried at v alone. A single angle in all four slots reduces to the old form exactly, so nothing
// that does not author softness changes. See SoftShadowSoftness for what splitting them costs.
//
// sim_shader.py and SoftShadowVertexReference MUST carry the same change, and the goldens must be
// regenerated from the updated model, or the three-layer comparison above is comparing two different
// algorithms and will report agreement it has not established.
//
// The whole program reads one payload plus uniforms -- no arrays, no neighbours beyond what the
// payload carries. Roles that do not apply for the current light collapse onto a degenerate position
// so their triangles have zero area; at the default segment count about two thirds of vertices
// collapse on a typical caster.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define SOFT_SHADOW_MIN_Y 0.000001f

// Roles, matching SoftShadowGeometryGenerator's k_Role* constants. Compared as floats because that
// is how they travel in tangent.x.
#define SOFT_ROLE_OUTLINE   0.0f
#define SOFT_ROLE_HARD      1.0f
#define SOFT_ROLE_BAND_IN   2.0f
#define SOFT_ROLE_BAND_OUT  3.0f
#define SOFT_ROLE_FIN_L     4.0f
#define SOFT_ROLE_FIN_R     5.0f
#define SOFT_ROLE_JOIN_IN   6.0f
#define SOFT_ROLE_JOIN_OUT  7.0f
#define SOFT_ROLE_CONVEX    8.0f
#define SOFT_ROLE_CONCAVE   9.0f
#define SOFT_ROLE_HARD_OPQ 10.0f

// The 14-float payload with no semantics attached, so a consumer whose input struct is generated
// elsewhere can feed this without a name collision. A ShaderGraph SubTarget is the case that forces
// it: ShaderGraph emits its own `Attributes` and `Varyings` inside every pass, so the semantic-bearing
// structs at the bottom of this file cannot be visible when it does.
struct SoftShadowPayload
{
    float3 positionOS;               // v.xy, 0        -- z is unused; POSITION-only passes read it
    float4 tangent;                  // role, fanParam, prev.xy
    float3 next;                     // next.xy, windingSign
    float4 neighbor2;                // prev2.xy, next2.xy
};

// The offset each of the five points this vertex reads is thrown by.
//
// A DISPLACEMENT, not a distance: direction and magnitude together. The construction below never
// requires the throw to be radial from the light -- what it requires is that the throw be the same
// function of a point no matter which vertex is asking, since adjacent edges meet only if hard(prev)
// computed at v equals hard(prev) computed at prev. A displacement satisfies that exactly as a
// distance did, which is why the whole vertex program now carries no light position at all.
//
// Supplied by the CALLER rather than read from a uniform, so a caller that varies the throw per point
// (a ShaderGraph vertex port, say) must evaluate its function at each of the five. Equal across all
// five gives parallel projection; SoftShadowRadialDisplacements reproduces the built-in behaviour.
struct SoftShadowDisplacements
{
    float2 v;
    float2 prev;
    float2 next;
    float2 prev2;
    float2 next2;
};

// Penumbra half-angles, split by which part of the shadow's boundary they widen.
//
// SIDE is the fin: the exterior half-wedge at a silhouette pole, which forms the shadow's lateral
// edge running away from the caster. It is needed at v only, because the fin is that vertex's own
// geometry and no neighbour reads it.
//
// BACK is the band: the offset along each away-facing edge, which projected forms the shadow's far
// boundary, plus everything derived from that width -- the convex fan, the concave fillet, and the
// radius the join fan sweeps to. Needed at v, prev and next, because SoftFarLimit measures against the
// line the FAR vertex will place its own band corner on. Not at prev2 / next2: FarLimit takes a single
// wFar for both of the far vertex's offsets, so the second ring of neighbours contributes no width.
//
// SPLITTING THEM BREAKS THE DERIVED-W IDENTITY, deliberately. W is normally the chord between the hard
// ray and the softened ray at the same angle, which puts the fin's corner at exactly radius W and makes
// the band's outer offset equal the fin's lateral reach -- "decoupling the two steps the outer boundary
// at every pole (report 21)". With side != back there IS such a step, and the join fan absorbs it: it
// lerps RADIUS as well as angle, from the fin's reach to the band's width, which the fan was written to
// do for precisely this case ("keeps the fan correct if the two are ever decoupled"). The bridge is
// resolved by the mesh's baked fan segment count, so a large side/back ratio on a coarsely fanned caster
// will look faceted across that transition rather than smooth.
//
// Same contract as SoftShadowDisplacements: each value must be the same function of its point no
// matter which vertex is asking. A width computed for prev at v that disagrees with the one prev
// computes for itself measures the fillet budget against a line prev's geometry is not on, which is
// exactly the overshoot SoftFarLimit's wFar exists to prevent.
struct SoftShadowSoftness
{
    float side;        // fin wedge, at v
    float back;        // band width, at v
    float backPrev;
    float backNext;
};

SoftShadowSoftness SoftShadowUniformSoftness(float soft)
{
    SoftShadowSoftness o;
    o.side = o.back = o.backPrev = o.backNext = soft;
    return o;
}

// Shared with the Legacy path -- set per caster by ShadowRendering.SetShadowProjectionGlobals.
uniform float3   _LightPos;
uniform float4x4 _ShadowModelMatrix;       // model matrix without scale
uniform float4x4 _ShadowModelInvMatrix;
uniform float3   _ShadowModelScale;
uniform float    _SoftShadowAngle;

// The ceiling an authored softness is held to, in radians -- ShadowRendering's k_MaxShadowSoftnessAngle,
// uploaded rather than duplicated here so there is one value and not two that can drift.
//
// It is a real limit, not defensive padding. Every golden, every acceptance sheet and the whole
// three-layer verification were captured at or below it, and the fan segment count that resolves the
// penumbra arc is baked into the mesh by SoftShadowGeometryGenerator at generation time -- so a softness
// far past this does not just leave the validated envelope, it out-runs the tessellation that was built
// for it and the penumbra boundary goes visibly faceted. Raising the ceiling is a change that has to
// come with a tessellation story.
uniform float    _SoftShadowMaxAngle;

// New, and material properties on the test shader rather than per-light state. Handoff v2 section 3
// puts both per light; nothing in URP supplies them yet. Both are pure uniforms -- the mesh depends
// on neither -- so promoting them later touches only the declaration and whoever sets them.
uniform float _ShadowLength;              // LENGTH: distance each vertex is thrown along its own ray
uniform float _FilletRadiusScale;         // R, as a fraction of W, so it tracks softness

// ----------------------------------------------------------------------------------------------
// Small helpers. Named and shaped to match the reference rather than to be idiomatic HLSL.
// ----------------------------------------------------------------------------------------------

float SoftDot(float2 a, float2 b)   { return a.x * b.x + a.y * b.y; }
float SoftCross(float2 a, float2 b) { return a.x * b.y - a.y * b.x; }

float2 SoftNorm(float2 a)
{
    float n = length(a);
    return n > 1e-12f ? a / n : float2(0.0f, 0.0f);
}

float2 SoftPerp(float2 a) { return float2(a.y, -a.x); }

float2 SoftRot(float2 a, float ang)
{
    float c = cos(ang), s = sin(ang);
    return float2(c * a.x - s * a.y, s * a.x + c * a.y);
}

float2 SoftDir(float ang) { return float2(cos(ang), sin(ang)); }

// SoftFSign -- the signed twice-area of (a, b, light) that used to detect silhouette poles -- is gone.
// A pole is now read off the two back-facing tests directly (isPole in SoftShadowVert), which is the
// same predicate without a second expression that could disagree with them.

bool SoftIntersect(float2 p0, float2 d0, float2 p1, float2 d1, out float2 hit)
{
    float det = d0.x * (-d1.y) - (-d1.x) * d0.y;
    if (abs(det) < 1e-9f)
    {
        hit = float2(0.0f, 0.0f);
        return false;
    }
    float2 rhs = p1 - p0;
    float t = (rhs.x * (-d1.y) - (-d1.x) * rhs.y) / det;
    hit = p0 + t * d0;
    return true;
}

float SoftShortestAngle(float da)
{
    // Branchless form of the reference's while loops. |da| is a difference of two atan2 results so it
    // never exceeds 2*pi, and one wrap is always enough.
    if (da > PI)  da -= 2.0f * PI;
    if (da < -PI) da += 2.0f * PI;
    return da;
}

// hard(v) = v + d(v). Depends only on the vertex, never on which edge is asking, so adjacent edges
// agree exactly at a shared vertex. That exactness is what makes the per-vertex form possible at all
// (handoff v2 section 4), and it is why the far end of the shadow follows the caster's outline
// instead of converging on an arc. With d(p) = len * normalize(p - light) this is the old radial
// form; with d constant it is a parallel projection.
float2 SoftHard(float2 p, float2 disp)
{
    return p + disp;
}

// The throw associated with an EDGE rather than a vertex, used by every back-facing test below.
//
// Symmetric in the edge's two endpoints, and that is the whole point. The band along (prev -> v) is
// built from two different vertices' outputs -- prev contributes one outer corner, v the other -- and
// both gate on "does this edge cast?". They agree only if both compute it from something neither one
// owns. Taking one endpoint's own displacement instead lets v answer yes while prev answers no, which
// collapses one corner of the band quad and leaves a wedge that flickers along the outline.
//
// Unnormalised on purpose: every consumer takes the SIGN of a dot or cross product with it, and the
// sum is already the direction that reduces to the old test. Radially, dot(perp(e), prev - light) and
// dot(perp(e), v - light) are EQUAL -- they differ by dot(perp(e), e), which is zero -- so both terms
// of the sum carry the same sign and the reduction is exact, not approximate.
float2 SoftEdgeThrow(float2 dA, float2 dB)
{
    return dA + dB;
}

// W, derived from the projection throw and never authored -- handoff v2 section 3. It is exactly the
// chord between the hard ray and the softened ray at throw distance |d|, which is what keeps the
// band's outer offset equal to the fin's lateral reach. Decoupling the two steps the outer boundary
// at every pole (report 21).
float SoftBandWidth(float2 disp, float soft)
{
    return length(disp) * 2.0f * sin(soft * 0.5f);
}

// ----------------------------------------------------------------------------------------------
// The radius budget (handoff v2 section 7b) and the tangent fillet (section 6c)
// ----------------------------------------------------------------------------------------------

// Limit along the shared edge's outer line at the far vertex.
//
// mIn / mOut are the FAR vertex's incoming and outgoing projected edge normals, in that order. The
// sweep sign depends on the order, and which of the two is the shared edge differs between the
// backward and forward reach -- passing them the same way round both times inverts the far-concavity
// test one way round, which over-granted the radius by 73% in report 26.
//
// farOtherAway says whether the far vertex's OTHER edge carries a band. Without it the far vertex
// counts as filleted when it is not, which both mis-sets the limit and splits the allotment in two.
// Same condition as fillable_set in the global build -- report 27.
//
// wFar is the band width at the FAR vertex, not at v. Every offset here is anchored at hFar, so it is
// the far vertex's own width that decides where its band outer line sits -- and that line is where the
// far vertex will place its own band corner when it is itself processed. Using v's width instead
// measures against a line nothing is on, which lets the fillet overshoot into the neighbour's band.
bool SoftFarLimit(float wg, float wFar, float2 hFar, float2 hFar2, float2 mIn, float2 mOut,
                  bool sharedIsIn, float2 hNear, bool farOtherAway, out float2 limit)
{
    float2 mShared = sharedIsIn ? mIn : mOut;
    float sw = atan2(SoftCross(mIn, mOut), SoftDot(mIn, mOut));
    bool farConcave = (sw * wg < 0.0f) && farOtherAway;
    if (!farConcave)
    {
        limit = hFar + wFar * mShared;
        return false;
    }

    float2 mOther = sharedIsIn ? mOut : mIn;
    float2 hit;
    if (!SoftIntersect(hFar + wFar * mOther, SoftNorm(hFar2 - hFar),
                       hFar + wFar * mShared, SoftNorm(hNear - hFar), hit))
    {
        limit = hFar + wFar * mShared;
        return false;
    }
    limit = hit;
    return true;
}

// tPP is the edge throw for (prev2 -> prev) and tNN the one for (next -> next2): the far vertex's
// OTHER edge in each direction. They arrive precomputed rather than as displacements so that the
// back-facing test here is the identical expression the far vertex will run on its own edge -- see
// SoftEdgeThrow.
float SoftRadius(float2 v, float2 prev, float2 next, float2 prev2, float2 next2,
                 float2 tPP, float2 tNN, float wg, float wV, float wP, float wN, float radiusRequested,
                 float2 Hv, float2 Hp, float2 Hn, float2 Hp2, float2 Hn2,
                 float2 mPrev, float2 mNext, float2 uA, float2 uB, float halfV,
                 bool awayPrev, bool awayNext, out float2 cmv)
{
    cmv = float2(0.0f, 0.0f);

    if (radiusRequested <= 1e-9f || halfV < 1e-4f || halfV > PI * 0.5f - 1e-4f)
        return 0.0f;
    if (!SoftIntersect(Hv + wV * mPrev, uA, Hv + wV * mNext, uB, cmv))
        return 0.0f;

    float r = radiusRequested;

    // An edge with no band imposes no constraint, so it is skipped -- exactly as the global budget
    // skips non-away edges.
    if (awayPrev)
    {
        // At prev the incoming edge is (prev2 -> prev), the outgoing is (prev -> v) = shared.
        float2 mpp = SoftNorm(wg * SoftPerp(Hp - Hp2));
        float2 npp = SoftNorm(wg * SoftPerp(prev - prev2));
        bool awayPp = SoftDot(npp, tPP) > 0.0f;
        float2 limFar;
        bool farCon = SoftFarLimit(wg, wP, Hp, Hp2, mpp, mPrev, false, Hv, awayPp, limFar);
        float usable = SoftDot(cmv - limFar, SoftNorm(Hv - Hp));
        float ends = farCon ? 2.0f : 1.0f;
        r = min(r, max(0.0f, usable / ends) * tan(halfV));
    }
    if (awayNext)
    {
        // At next the incoming edge is (v -> next) = shared, the outgoing is (next -> next2).
        float2 mnn = SoftNorm(wg * SoftPerp(Hn2 - Hn));
        float2 nnn = SoftNorm(wg * SoftPerp(next2 - next));
        bool awayNn = SoftDot(nnn, tNN) > 0.0f;
        float2 limFar2;
        bool farCon2 = SoftFarLimit(wg, wN, Hn, Hn2, mNext, mnn, true, Hv, awayNn, limFar2);
        float usable2 = SoftDot(limFar2 - cmv, SoftNorm(Hn - Hv));
        float ends2 = farCon2 ? 2.0f : 1.0f;
        r = min(r, max(0.0f, usable2 / ends2) * tan(halfV));
    }
    return max(0.0f, r);
}

// R = 0 collapses TA = TB = CM and reproduces the square corner exactly, so the degenerate path
// needs no special case.
bool SoftFillet(float2 v, float2 prev, float2 next, float2 prev2, float2 next2,
                float2 tPP, float2 tNN, float wg, float wV, float wP, float wN, float radiusRequested,
                float2 Hv, float2 Hp, float2 Hn, float2 Hp2, float2 Hn2,
                float2 mPrev, float2 mNext, float2 uA, float2 uB, float halfV,
                bool awayPrev, bool awayNext,
                out float2 ta, out float2 tb, out float2 cc)
{
    ta = tb = cc = float2(0.0f, 0.0f);

    float2 cmv;
    float r = SoftRadius(v, prev, next, prev2, next2, tPP, tNN, wg, wV, wP, wN, radiusRequested,
                         Hv, Hp, Hn, Hp2, Hn2, mPrev, mNext, uA, uB, halfV,
                         awayPrev, awayNext, cmv);
    if (r <= 1e-9f)
        return false;

    ta = cmv + (r / tan(halfV)) * uA;
    tb = cmv + (r / tan(halfV)) * uB;
    cc = cmv + (r / sin(halfV)) * SoftNorm(uA + uB);
    return true;
}

// ----------------------------------------------------------------------------------------------
// The fin
// ----------------------------------------------------------------------------------------------

// Which side is interior needs no winding knowledge: at a pole BOTH outline neighbours lie on the
// same side of v's throw line. Summing both cross products rather than trusting one keeps the sign
// stable on thin sliver casters, where a single neighbour can lie nearly along the throw and
// contribute a near-zero cross on its own.
void SoftFinCorner(float2 v, float2 disp, float soft, float inter,
                   float side, out float2 corner, out bool isClear)
{
    if (side * inter > 0.0f)
    {
        // Interior corner, pulled onto the hard shadow direction so the fin is exactly the exterior
        // half-wedge, abutting the body along the silhouette ray instead of straddling it. This lands
        // coincident with hard(v) -- now EXACTLY so, rather than to within an atan2/cos/sin round trip
        // as the angular form did.
        corner = v + disp;
        isClear = false;
    }
    else
    {
        // Rotating the displacement rather than rebuilding it from an angle keeps the derived-W
        // identity exact: |rot(d, soft) - d| == |d| * 2*sin(soft/2) == SoftBandWidth(d, soft), which is
        // what report 28 measured at 6.7e-16 and what the join fan's radius lerp relies on.
        corner = v + SoftRot(disp, side * soft);
        isClear = true;
    }
}

// tPrev / tNext are the edge throws for (prev -> v) and (v -> next), the SAME values the back-facing
// tests use. Deriving the interior side from them rather than from v's own displacement is what keeps
// this test from contradicting isPole: isPole is exactly "these two edges disagree", so if the side
// test read a different quantity the two could disagree about whether a pole exists and on which side
// its body lies -- putting a full-shadow wedge on the lit side, which max blending cannot undo.
void SoftFinCorners(float2 v, float2 prev, float2 next, float2 tPrev, float2 tNext,
                    float2 disp, float soft,
                    out float2 c0, out bool clr0, out float2 c1, out bool clr1)
{
    float inter = SoftCross(tPrev, prev - v) + SoftCross(tNext, next - v);

    SoftFinCorner(v, disp, soft, inter, -1.0f, c0, clr0);
    SoftFinCorner(v, disp, soft, inter, +1.0f, c1, clr1);
}

// ----------------------------------------------------------------------------------------------
// The per-vertex program
// ----------------------------------------------------------------------------------------------

// Shading value encoding (handoff v2 section 8). The fragment computes
//     value = 1 - saturate(|x| / clamp(y, MIN, 1))
// so .x is signed position across the wedge and .y is the width normaliser:
//     on the caster / fin apex   (0, 0)   -> 1
//     on the umbra boundary      (0, 1)   -> 1
//     on the outer boundary      (1, 1)   -> 0
//
// Never place (0,0) next to an outer corner: |x|/y degenerates to 1 along that edge and flattens the
// ramp across the whole triangle. That is why HARD and HARD_OPQ are separate roles at the same
// position -- sharing them cost max |diff| 1.0 over ~17000 pixels on a plain square.
//
// BOTH fin corners carry x = +1 here, which is section 8's sign fix. With Legacy's signs SoftLeft is
// -1 while the band's outer corner is +1, so at whichever pole has SoftLeft as its exterior corner
// the join fan would span -1 to +1 and, because the fragment takes |x|, climb back to full shadow
// along the interior line. That is an opaque wedge through the middle of the fan on half the poles.
#define SOFT_VALUE_APEX float2(0.0f, 0.0f)
#define SOFT_VALUE_OPQ  float2(0.0f, 1.0f)
#define SOFT_VALUE_CLR  float2(1.0f, 1.0f)

void SoftShadowVert(SoftShadowPayload a, SoftShadowDisplacements disp, SoftShadowSoftness softness,
                    out float2 position, out float2 value)
{
    float2 v     = a.positionOS.xy;
    float2 prev  = a.tangent.zw;
    float2 next  = a.next.xy;
    float2 prev2 = a.neighbor2.xy;
    float2 next2 = a.neighbor2.zw;
    float  role  = a.tangent.x;
    float  fanParam = a.tangent.y;
    float  wg    = a.next.z;

    // Clamped HERE rather than at the caller, so no entry point can bypass it. A negative angle would
    // make W negative and flip every band corner to the interior of the shadow, overlapping the umbra
    // and inverting the fin's rotation across the hard ray; the upper bound is the validated envelope
    // and the mesh's baked fan resolution. See _SoftShadowMaxAngle.
    float softSide  = clamp(softness.side,     0.0f, _SoftShadowMaxAngle);
    float softBack  = clamp(softness.back,     0.0f, _SoftShadowMaxAngle);
    float softBackP = clamp(softness.backPrev, 0.0f, _SoftShadowMaxAngle);
    float softBackN = clamp(softness.backNext, 0.0f, _SoftShadowMaxAngle);

    // The projection throw is per POINT, and W is DERIVED from it -- never authored (handoff v2
    // section 3, report 21). The two must therefore vary together: a W taken from one vertex but
    // applied at a neighbour measures against a line the neighbour's geometry is not on, which lets
    // the fillet overshoot into the neighbour's band. SoftFarLimit is the only place a width is used
    // at anything other than v, which is why it takes its own.
    //
    // With an equal displacement at every point -- parallel projection -- every width below is equal
    // and this reduces to the single-scalar form.
    float2 dV  = disp.v;
    float2 dP  = disp.prev;
    float2 dN  = disp.next;
    float2 dP2 = disp.prev2;
    float2 dN2 = disp.next2;

    // One throw per EDGE, shared by both of its endpoints. Every back-facing test below reads these
    // and nothing else, so v and its neighbour cannot disagree about an edge they share.
    float2 tPrev = SoftEdgeThrow(dP, dV);
    float2 tNext = SoftEdgeThrow(dV, dN);
    float2 tPP   = SoftEdgeThrow(dP2, dP);
    float2 tNN   = SoftEdgeThrow(dN, dN2);

    // Each width takes its OWN point's softness, for the same reason it takes its own displacement: the
    // width belongs to the vertex whose band corner it places, and SoftFarLimit measures against the
    // line the far vertex will itself sit on.
    float wV = SoftBandWidth(dV, softBack);
    float wP = SoftBandWidth(dP, softBackP);
    float wN = SoftBandWidth(dN, softBackN);
    float radiusRequested = _FilletRadiusScale * wV;

    float2 Hv  = SoftHard(v, dV);
    float2 Hp  = SoftHard(prev, dP);
    float2 Hn  = SoftHard(next, dN);
    float2 Hp2 = SoftHard(prev2, dP2);
    float2 Hn2 = SoftHard(next2, dN2);

    if (role < SOFT_ROLE_HARD - 0.5f)        { position = v;  value = SOFT_VALUE_APEX; return; }
    if (role < SOFT_ROLE_BAND_IN - 0.5f)     { position = Hv; value = SOFT_VALUE_APEX; return; }
    if (role > SOFT_ROLE_CONCAVE + 0.5f)     { position = Hv; value = SOFT_VALUE_OPQ;  return; }

    float2 mPrev = SoftNorm(wg * SoftPerp(Hv - Hp));
    float2 mNext = SoftNorm(wg * SoftPerp(Hn - Hv));

    float2 nPrev = SoftNorm(wg * SoftPerp(v - prev));
    float2 nNext = SoftNorm(wg * SoftPerp(next - v));
    bool awayPrev = SoftDot(nPrev, tPrev) > 0.0f;
    bool awayNext = SoftDot(nNext, tNext) > 0.0f;

    // A pole is exactly "my two edges disagree about whether they cast", which is what the old signed
    // area test against the light computed -- SoftFSign(prev,v,L) and dot(nPrev, mid - L) differ only
    // by the winding factor and a positive scale, because dot(perp(e), e) is zero and so the midpoint
    // drops out. Deriving it from the away tests rather than recomputing it keeps the two from ever
    // contradicting each other, and is why no light position appears in this function.
    bool isPole = awayPrev != awayNext;
    float sweepV = atan2(SoftCross(mPrev, mNext), SoftDot(mPrev, mNext));
    bool concaveV = sweepV * wg < 0.0f;

    float2 uA = SoftNorm(Hp - Hv);
    float2 uB = SoftNorm(Hn - Hv);
    float halfV = acos(clamp(SoftDot(uA, uB), -0.999999f, 0.999999f)) * 0.5f;

    // ---- band outer corners ----
    if (role < SOFT_ROLE_FIN_L - 0.5f)
    {
        bool isBandIn = role < SOFT_ROLE_BAND_OUT - 0.5f;
        bool edgeAway = isBandIn ? awayPrev : awayNext;
        if (!edgeAway)                       { position = Hv; value = SOFT_VALUE_CLR; return; }

        // The setback needs BOTH edges back-facing, exactly like the arc: the fillet is the
        // intersection of the two bands' outer lines, so it is undefined when one of those edges
        // carries no band. Report 26 guarded this on concavity alone, which collapsed the band at a
        // vertex that was concave and a silhouette pole at once -- report 27. Handoff v2 sections 6c
        // and 9b still describe the older rule; do not "fix" this back to them.
        if (concaveV && awayPrev && awayNext)
        {
            float2 ta, tb, cc;
            if (SoftFillet(v, prev, next, prev2, next2, tPP, tNN, wg, wV, wP, wN, radiusRequested,
                           Hv, Hp, Hn, Hp2, Hn2, mPrev, mNext, uA, uB, halfV,
                           awayPrev, awayNext, ta, tb, cc))
            {
                position = isBandIn ? ta : tb;
                value = SOFT_VALUE_CLR;
                return;
            }
        }

        position = Hv + wV * (isBandIn ? mPrev : mNext);
        value = SOFT_VALUE_CLR;
        return;
    }

    // ---- fin ----
    if (role < SOFT_ROLE_JOIN_IN - 0.5f)
    {
        if (!isPole)                         { position = v; value = SOFT_VALUE_APEX; return; }

        float2 c0, c1; bool clr0, clr1;
        SoftFinCorners(v, prev, next, tPrev, tNext, dV, softSide, c0, clr0, c1, clr1);
        bool isLeft = role < SOFT_ROLE_FIN_R - 0.5f;
        position = isLeft ? c0 : c1;
        value = (isLeft ? clr0 : clr1) ? SOFT_VALUE_CLR : SOFT_VALUE_OPQ;
        return;
    }

    // ---- join fans ----
    if (role < SOFT_ROLE_CONVEX - 0.5f)
    {
        bool isJoinIn = role < SOFT_ROLE_JOIN_OUT - 0.5f;
        bool edgeAway = isJoinIn ? awayPrev : awayNext;
        if (!isPole || !edgeAway)            { position = Hv; value = SOFT_VALUE_CLR; return; }

        float2 c0, c1; bool clr0, clr1;
        SoftFinCorners(v, prev, next, tPrev, tNext, dV, softSide, c0, clr0, c1, clr1);
        float2 ext = clr0 ? c0 : c1;
        float2 m = isJoinIn ? mPrev : mNext;

        float2 v0 = ext - Hv;
        float r0 = length(v0);

        // The fan's far angle comes from m itself, NOT from wV * m. For wV > 0 the two are identical --
        // a positive scale does not move an atan2 -- but a back softness of exactly zero makes wV zero,
        // and atan2(0, 0) is a garbage direction that would sweep the fan somewhere arbitrary. Reading m
        // directly is exact in both cases.
        float a1 = atan2(m.y, m.x);

        // A degenerate start collapses the SWEEP, never the fan.
        //
        // This used to return early and put every fan vertex on Hv. That was correct only because the
        // two angles were the same: r0 == 0 meant the side angle was zero, which meant the back angle
        // was zero too, so wV was zero and the band corner was also at Hv -- the fan had nothing to
        // span. Decoupled, a zero SIDE angle leaves wV untouched, so collapsing the fan while the band
        // corner sits out at Hv + wV*m tears a wedge-shaped hole exactly where the fan should be. That
        // is the "fans not connecting" you see when side and back are randomised independently.
        //
        // Starting the sweep at a1 instead makes the fan purely radial: zero angular span, radius
        // lerping 0 -> wV, which fills the gap and still degenerates to a point when wV is also zero.
        float a0 = (r0 > 1e-9f) ? atan2(v0.y, v0.x) : a1;
        float da = SoftShortestAngle(a1 - a0);

        // The radius is lerped as well as the angle. With W derived from the same angle the fin's corner
        // already sits at radius W -- report 28 measured that at 6.7e-16 -- so the lerp was a no-op
        // while the two were coupled. Splitting Side from Back is what it was kept for.
        float rr = r0 + (wV - r0) * fanParam;
        float aa = a0 + da * fanParam;
        position = Hv + rr * SoftDir(aa);
        value = SOFT_VALUE_CLR;
        return;
    }

    // ---- convex fan ----
    if (role < SOFT_ROLE_CONCAVE - 0.5f)
    {
        if (isPole || concaveV || !(awayPrev && awayNext))
        {
            position = Hv; value = SOFT_VALUE_CLR; return;
        }
        // Holding the boundary at radius W rather than chording it removes the corner pinch: a single
        // bevel makes the band only W*cos(halfAngle) at the corner (handoff v2 section 6b).
        position = Hv + wV * SoftRot(mPrev, sweepV * fanParam);
        value = SOFT_VALUE_CLR;
        return;
    }

    // ---- concave fillet arc ----
    if (isPole || !concaveV || !(awayPrev && awayNext))
    {
        position = Hv; value = SOFT_VALUE_CLR; return;
    }

    float2 ta2, tb2, cc2;
    if (!SoftFillet(v, prev, next, prev2, next2, tPP, tNN, wg, wV, wP, wN, radiusRequested,
                    Hv, Hp, Hn, Hp2, Hn2, mPrev, mNext, uA, uB, halfV,
                    awayPrev, awayNext, ta2, tb2, cc2))
    {
        position = Hv; value = SOFT_VALUE_CLR; return;
    }

    float r2 = length(ta2 - cc2);
    float aA = atan2((ta2 - cc2).y, (ta2 - cc2).x);
    float aB = atan2((tb2 - cc2).y, (tb2 - cc2).x);
    float d2 = SoftShortestAngle(aB - aA);
    float ang2 = aA + d2 * fanParam;
    position = cc2 + r2 * SoftDir(ang2);
    value = SOFT_VALUE_CLR;
}

// ----------------------------------------------------------------------------------------------
// Entry point
//
// Payload in, clip position and shading value out. Every consumer wants the same space handling, so
// it lives here rather than in the pass bodies -- which stay thin enough for a hand-written shader or
// a ShaderGraph SubTarget template to be a single call.
// ----------------------------------------------------------------------------------------------

// The light in the space the projection runs in.
//
// Exposed because anything computing a per-point quantity against the light -- a ShaderGraph node
// feeding a projection length, say -- has to use the SAME space, and it cannot see the local that
// SoftShadowPrepare produces. Two copies of this transform would be two things to keep in step.
// All THREE components, in the caster's own frame.
//
// z is the light's signed distance from the caster's plane, and it is the one quantity a fake-3D
// shadow needs that the planar projection cannot use: the projection is strictly 2D, so
// SoftShadowLightPosition below drops z, but "how high is the light above the ground" IS that z.
// Exposed so a node can compute a shadow length by similar triangles without reaching for a world
// axis -- the caster's plane is z = 0 in this frame by construction, which is what makes the result
// invariant when an entire scene is rotated as a rigid body.
float3 SoftShadowLightLocal()
{
#if NOT_TRANSFORMABLE
    return _LightPos.xyz;
#else
    return mul(_ShadowModelInvMatrix, float4(_LightPos.xyz, 1.0f)).xyz;
#endif
}

float2 SoftShadowLightPosition()
{
    return SoftShadowLightLocal().xy;
}

// A point of the caster's outline in the space the projection runs in.
//
// The payload arrives unscaled and SoftShadowPrepare scales it, so anything evaluated BEFORE prepare
// -- which is where the vertex graph runs -- has to apply the same scale itself or it measures
// against a light that has already been moved into the scaled space. That mismatch is silent and only
// appears once a caster is scaled, which is why it lives here rather than in each caster.
float2 SoftShadowPointToLocal(float2 positionOS)
{
#if NOT_TRANSFORMABLE
    return positionOS;
#else
    return positionOS * _ShadowModelScale.xy;
#endif
}

// Brings the payload and the light into one space.
//
// Exposed separately because a caller that evaluates the projection length per point must do so in the
// SAME space the projection runs in. Measuring a distance from the unscaled payload while the
// projection runs on scaled positions disagrees under non-uniform caster scale -- so prepare first,
// then evaluate, then project.
//
// Non-uniform caster scale is unexercised (handoff v2 section 13): the classification stays
// self-consistent because every input is scaled alike, but the softness angle is no longer a uniform
// angle in a non-uniformly scaled space.
void SoftShadowPrepare(inout SoftShadowPayload a, out float2 light)
{
    light = SoftShadowLightPosition();
#if !NOT_TRANSFORMABLE
    a.positionOS.xy *= _ShadowModelScale.xy;
    a.tangent.zw    *= _ShadowModelScale.xy;
    a.next.xy       *= _ShadowModelScale.xy;
    a.neighbor2.xy  *= _ShadowModelScale.xy;
    a.neighbor2.zw  *= _ShadowModelScale.xy;
#endif
}

// The projected position in world space.
//
// Exists because a consumer that bypasses the standard vertex path has no other way to fill a
// world-space varying, and without one every fragment input derived from world position -- Position
// (World), and Screen Position, which ShaderGraph derives FROM world position -- reads zero.
float3 SoftShadowToWorld(float2 position)
{
#if NOT_TRANSFORMABLE
    return float3(position, 0.0f);
#else
    return mul(_ShadowModelMatrix, float4(position, 0.0f, 1.0f)).xyz;
#endif
}

float4 SoftShadowToHClip(float2 position)
{
#if NOT_TRANSFORMABLE
    return mul(GetWorldToHClipMatrix(), float4(position, 0.0f, 1.0f));
#else
    return mul(GetWorldToHClipMatrix(), mul(_ShadowModelMatrix, float4(position, 0.0f, 1.0f)));
#endif
}

// The built-in throw: each point pushed `len` along its own ray from the light. Radial projection is
// now one displacement field among others rather than the only thing the construction can express,
// so it lives here as a helper instead of being baked into SoftHard.
float2 SoftShadowRadialDisplacement(float2 p, float2 light, float len)
{
    return len * SoftNorm(p - light);
}

// All five points thrown the same distance radially -- the built-in behaviour, reproduced exactly.
//
// Takes the payload AFTER SoftShadowPrepare, for the same reason the per-point evaluation has always
// had to run there: measuring an unscaled point against a light that prepare has already moved into
// the scaled space makes the length computed for `prev` at this vertex disagree with the one computed
// for `v` at the previous vertex, and the seams open.
SoftShadowDisplacements SoftShadowRadialDisplacements(SoftShadowPayload a, float2 light, float len)
{
    SoftShadowDisplacements o;
    o.v     = SoftShadowRadialDisplacement(a.positionOS.xy, light, len);
    o.prev  = SoftShadowRadialDisplacement(a.tangent.zw,    light, len);
    o.next  = SoftShadowRadialDisplacement(a.next.xy,       light, len);
    o.prev2 = SoftShadowRadialDisplacement(a.neighbor2.xy,  light, len);
    o.next2 = SoftShadowRadialDisplacement(a.neighbor2.zw,  light, len);
    return o;
}

// prepare + vert + transform, for a caller with nothing per-point to contribute.
void SoftShadowProject(SoftShadowPayload a, float len,
                       out float4 positionHCS, out float2 shadow)
{
    float2 light;
    SoftShadowPrepare(a, light);

    SoftShadowDisplacements disp = SoftShadowRadialDisplacements(a, light, len);

    float2 position, value;
    SoftShadowVert(a, disp, SoftShadowUniformSoftness(_SoftShadowAngle), position, value);

    positionHCS = SoftShadowToHClip(position);
    shadow = value;
}

// ----------------------------------------------------------------------------------------------
// Semantic-bearing structs and the hand-written-shader entry point.
//
// Guarded because a ShaderGraph pass emits its own `Attributes` and `Varyings` inside every
// HLSLPROGRAM, and two declarations of either name in one translation unit is a redefinition error.
// A consumer that brings its own defines SOFT_SHADOW_EXTERNAL_ATTRIBUTES before including this file
// and calls SoftShadowProject directly.
// ----------------------------------------------------------------------------------------------

#ifndef SOFT_SHADOW_EXTERNAL_ATTRIBUTES

struct Attributes
{
    float3 positionOS : POSITION;    // v.xy, 0        -- z is unused; POSITION-only passes read it
    float4 tangent    : TANGENT;     // role, fanParam, prev.xy
    float3 next       : TEXCOORD1;   // next.xy, windingSign
    float4 neighbor2  : TEXCOORD2;   // prev2.xy, next2.xy
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float2 shadow : TEXCOORD0;
};

Varyings ProjectSoftShadow(Attributes a)
{
    SoftShadowPayload p;
    p.positionOS = a.positionOS;
    p.tangent    = a.tangent;
    p.next       = a.next;
    p.neighbor2  = a.neighbor2;

    Varyings o;
    SoftShadowProject(p, _ShadowLength, o.vertex, o.shadow);
    return o;
}

#endif // SOFT_SHADOW_EXTERNAL_ATTRIBUTES

#endif // SOFT_SHADOW_PROJECT_VERTEX_INCLUDED
