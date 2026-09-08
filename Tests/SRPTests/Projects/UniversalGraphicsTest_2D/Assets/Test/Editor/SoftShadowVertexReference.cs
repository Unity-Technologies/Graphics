using System;

namespace UnityEngine.Rendering.Universal.Tests
{
    // A line-by-line transliteration of sim/sim_shader.py's vert(), in double precision.
    //
    // This is the middle layer of a three-layer port, and it exists so the other two can be
    // checked against something:
    //
    //     sim_shader.py vert()            the definition of correct, float64
    //       |  softshadow_goldens.txt     <- SoftShadowGoldenTests compares these two
    //     this file                       double, test-only
    //       |  direct comparison          <- the HLSL is diffed against this, with a tolerance
    //     SoftShadowProjectVertex.hlsl    float
    //
    // Kept deliberately dumb: same structure, same order of operations, same magic numbers, no
    // refactoring and no cleverness. Anything "improved" here stops it being a reference. It is
    // double rather than float so the comparison against the Python model can be tight; the HLSL
    // port is the layer where precision loss is expected and measured.
    //
    // Report 27's correction is baked in: the band-corner setback and the concave arc fire on the
    // SAME condition (concave && awayPrev && awayNext). Handoff v2 sections 6c and 9b still describe
    // the older, wrong rule -- do not "fix" this back.
    //
    // PROJECTION IS BY DISPLACEMENT, NOT DISTANCE, matching SoftShadowProjectVertex.hlsl. Each point
    // is thrown by a caller-supplied 2D offset rather than a scalar distance along its own ray, so
    // parallel and sheared projections are expressible and no light position appears in Vert at all.
    // Every test that used to read the light now reads a per-EDGE throw (EdgeThrow), and each reduces
    // to its old form exactly under a radial field -- see the HLSL header for the five identities.
    //
    // sim_shader.py MUST carry the same change and softshadow_goldens.txt must be regenerated from it.
    // Until that happens SoftShadowGoldenTests is comparing two different algorithms, and the goldens
    // will disagree wherever the old and new forms are not algebraically identical.
    static class SoftShadowVertexReference
    {
        // Vertex roles, from sim_shader.py. The integers are part of the golden file format.
        internal const int RoleOutline  = 0;
        internal const int RoleHard     = 1;
        internal const int RoleBandIn   = 2;
        internal const int RoleBandOut  = 3;
        internal const int RoleFinL     = 4;
        internal const int RoleFinR     = 5;
        internal const int RoleJoinIn   = 6;
        internal const int RoleJoinOut  = 7;
        internal const int RoleConvex   = 8;
        internal const int RoleConcave  = 9;
        internal const int RoleHardOpq  = 10;

        internal struct D2
        {
            public double x, y;
            public D2(double x, double y) { this.x = x; this.y = y; }

            public static D2 operator +(D2 a, D2 b) => new D2(a.x + b.x, a.y + b.y);
            public static D2 operator -(D2 a, D2 b) => new D2(a.x - b.x, a.y - b.y);
            public static D2 operator *(double s, D2 a) => new D2(s * a.x, s * a.y);
            public static D2 operator /(D2 a, double s) => new D2(a.x / s, a.y / s);
        }

        // 15 degrees, matching ShadowRendering.k_MaxShadowSoftnessAngle, which the HLSL receives as
        // _SoftShadowMaxAngle. Kept as the default rather than a required argument so existing callers
        // are untouched: every golden was captured at or below this, so the clamp is a no-op for them.
        internal const double k_MaxSoft = 15.0 * System.Math.PI / 180.0;

        internal struct Uniforms
        {
            public D2 light;
            public double length;
            public double soft;
            public double maxSoft;
            public double radiusRequested;
            public int segments;          // one knob for all three fans; see report 29

            public Uniforms(D2 light, double length, double soft, double radiusRequested, int segments,
                            double maxSoft = k_MaxSoft)
            {
                this.light = light;
                this.length = length;
                this.soft = soft;
                this.maxSoft = maxSoft;
                this.radiusRequested = radiusRequested;
                this.segments = segments;
            }
        }

        /// <summary>
        /// Penumbra half-angles, split by which boundary of the shadow they widen. Mirrors the HLSL's
        /// SoftShadowSoftness.
        ///
        /// Side is the fin's wedge -- the shadow's lateral edge -- and is v's own geometry, so it is
        /// carried once. Back is the band width W, carried at v, prev and next because the fillet budget
        /// measures a neighbour's width against the line that neighbour will itself sit on; not at
        /// prev2 / next2, since FarLimit takes a single wFar for both of the far vertex's offsets.
        ///
        /// Splitting them breaks the derived-W identity on purpose: the fin's corner no longer sits at
        /// radius W, so there is a step at every pole, and the join fan's radius lerp is what bridges
        /// it. That lerp was a no-op measured at 6.7e-16 while the two were coupled.
        /// </summary>
        internal struct Softness
        {
            public double side;
            public double back, backPrev, backNext;

            public static Softness Uniform(double soft) => new Softness
            {
                side = soft, back = soft, backPrev = soft, backNext = soft,
            };
        }

        // The 13-float payload, unpacked. prev2 / next2 exist solely so the radius budget can compute
        // CM at a neighbouring vertex -- handoff v2 section 7c.
        internal struct Payload
        {
            public D2 v, prev, next, prev2, next2;
            public int role;
            public double winding;
            public int k;
        }

        static readonly D2 k_Apex = new D2(0.0, 0.0);
        static readonly D2 k_Opq  = new D2(0.0, 1.0);
        static readonly D2 k_Clr  = new D2(1.0, 1.0);

        static double Dot(D2 a, D2 b) => a.x * b.x + a.y * b.y;
        static double Cross(D2 a, D2 b) => a.x * b.y - a.y * b.x;
        static double Length(D2 a) => Math.Sqrt(a.x * a.x + a.y * a.y);

        static D2 Norm(D2 a)
        {
            double n = Length(a);
            return n > 1e-12 ? a / n : new D2(0.0, 0.0);
        }

        static D2 Perp(D2 a) => new D2(a.y, -a.x);

        static D2 Rot(D2 a, double ang)
        {
            double c = Math.Cos(ang), s = Math.Sin(ang);
            return new D2(c * a.x - s * a.y, s * a.x + c * a.y);
        }

        static D2 Dir(double ang) => new D2(Math.Cos(ang), Math.Sin(ang));

        // FSign -- the signed twice-area of (a, b, light) that used to detect silhouette poles -- is
        // gone. A pole is now read off the two back-facing tests directly (isPole in Vert), which is
        // the same predicate without a second expression that could disagree with them.

        static bool Intersect(D2 p0, D2 d0, D2 p1, D2 d1, out D2 hit)
        {
            double det = d0.x * (-d1.y) - (-d1.x) * d0.y;
            if (Math.Abs(det) < 1e-9)
            {
                hit = default;
                return false;
            }
            D2 rhs = p1 - p0;
            double t = (rhs.x * (-d1.y) - (-d1.x) * rhs.y) / det;
            hit = p0 + t * d0;
            return true;
        }

        static double ShortestAngle(double da)
        {
            while (da > Math.PI) da -= 2.0 * Math.PI;
            while (da < -Math.PI) da += 2.0 * Math.PI;
            return da;
        }

        /// <summary>
        /// The whole per-vertex program. Reads one payload plus the uniforms and nothing else -- no
        /// arrays, no neighbours beyond what the payload carries. That is the constraint a real
        /// vertex shader operates under, and it is what makes the HLSL port a transliteration rather
        /// than a redesign.
        /// </summary>
        internal static void Vert(in Payload pay, in Uniforms u, out D2 position, out D2 value)
            => Vert(pay, u, Displacements.Radial(pay, u.light, u.length), out position, out value);

        internal static void Vert(in Payload pay, in Uniforms u, in Displacements disp,
                                  out D2 position, out D2 value)
            => Vert(pay, u, disp, Softness.Uniform(u.soft), out position, out value);

        internal static void Vert(in Payload pay, in Uniforms u, in Displacements disp,
                                  in Softness softness, out D2 position, out D2 value)
        {
            double wg = pay.winding;

            // Clamped here rather than at the caller so no entry point can bypass it, mirroring the
            // HLSL. Negative would make W negative and flip every band corner into the umbra; the upper
            // bound is the validated envelope and the mesh's baked fan resolution.
            double softSide  = Clamp(softness.side,     0.0, u.maxSoft);
            double softBack  = Clamp(softness.back,     0.0, u.maxSoft);
            double softBackP = Clamp(softness.backPrev, 0.0, u.maxSoft);
            double softBackN = Clamp(softness.backNext, 0.0, u.maxSoft);

            // The projection throw is per POINT, and the band width W is derived from it (handoff v2
            // section 3: W is never authored). The two must therefore vary together -- a W taken from
            // one vertex but applied at a neighbour measures against a line the neighbour's geometry
            // is not on, which lets the fillet overshoot into the neighbour's band. FarLimit below is
            // the only place W is used at anything other than v.
            //
            // Displacements come from the CALLER, mirroring the HLSL's SoftShadowDisplacements -- the
            // projection has to be the same function of the point regardless of which vertex is asking,
            // so a caller that varies it must evaluate at all five. Equal across all five is a parallel
            // projection; Displacements.Radial reproduces the old scalar form.
            D2 dV  = disp.v;
            D2 dP  = disp.prev;
            D2 dN  = disp.next;
            D2 dP2 = disp.prev2;
            D2 dN2 = disp.next2;

            // One throw per EDGE, shared by both endpoints, so v and its neighbour cannot disagree
            // about an edge they share. See EdgeThrow.
            D2 tPrev = EdgeThrow(dP, dV);
            D2 tNext = EdgeThrow(dV, dN);
            D2 tPP   = EdgeThrow(dP2, dP);
            D2 tNN   = EdgeThrow(dN, dN2);

            // Each width takes its OWN point's softness, for the same reason it takes its own
            // displacement: the width belongs to the vertex whose band corner it places, and FarLimit
            // measures against the line the far vertex will itself sit on.
            double wV = BandWidth(dV, softBack);
            double wP = BandWidth(dP, softBackP);
            double wN = BandWidth(dN, softBackN);

            D2 Hv  = Hard(pay.v, dV);
            D2 Hp  = Hard(pay.prev, dP);
            D2 Hn  = Hard(pay.next, dN);
            D2 Hp2 = Hard(pay.prev2, dP2);
            D2 Hn2 = Hard(pay.next2, dN2);

            if (pay.role == RoleOutline) { position = pay.v; value = k_Apex; return; }
            if (pay.role == RoleHard)    { position = Hv;    value = k_Apex; return; }
            if (pay.role == RoleHardOpq) { position = Hv;    value = k_Opq;  return; }

            // Projected segment normals for the two edges meeting at v.
            D2 mPrev = Norm(wg * Perp(Hv - Hp));
            D2 mNext = Norm(wg * Perp(Hn - Hv));

            // Caster-space normals, for the facing test.
            D2 nPrev = Norm(wg * Perp(pay.v - pay.prev));
            D2 nNext = Norm(wg * Perp(pay.next - pay.v));
            bool awayPrev = Dot(nPrev, tPrev) > 0.0;
            bool awayNext = Dot(nNext, tNext) > 0.0;

            // A pole is exactly "my two edges disagree about whether they cast", which is what the old
            // signed-area test against the light computed. Deriving it from the away tests rather than
            // recomputing it keeps the two from ever contradicting each other, and is why no light
            // position appears in this method.
            bool isPole = awayPrev != awayNext;
            double sweepV = Math.Atan2(Cross(mPrev, mNext), Dot(mPrev, mNext));
            bool concaveV = sweepV * wg < 0.0;

            D2 uA = Norm(Hp - Hv);
            D2 uB = Norm(Hn - Hv);
            double halfV = Math.Acos(Clamp(Dot(uA, uB), -0.999999, 0.999999)) / 2.0;

            switch (pay.role)
            {
                case RoleBandIn:
                case RoleBandOut:
                {
                    bool edgeAway = pay.role == RoleBandIn ? awayPrev : awayNext;
                    if (!edgeAway) { position = Hv; value = k_Clr; return; }   // no band: collapse

                    D2 m = pay.role == RoleBandIn ? mPrev : mNext;

                    // The setback needs BOTH edges back-facing, exactly like the arc: the fillet is
                    // the intersection of the two bands' outer lines, so it is undefined when one of
                    // those edges carries no band. Report 26 guarded this on concavity alone, which
                    // collapsed the band at a vertex that was concave and a silhouette pole at once.
                    // Report 27.
                    if (concaveV && awayPrev && awayNext &&
                        Fillet(pay, u, tPP, tNN, wV, wP, wN, Hv, Hp, Hn, Hp2, Hn2, mPrev, mNext, uA, uB, halfV,
                               awayPrev, awayNext, out D2 ta, out D2 tb, out _))
                    {
                        position = pay.role == RoleBandIn ? ta : tb;
                        value = k_Clr;
                        return;
                    }

                    position = Hv + wV * m;
                    value = k_Clr;
                    return;
                }

                case RoleFinL:
                case RoleFinR:
                {
                    if (!isPole) { position = pay.v; value = k_Apex; return; }  // collapse onto outline
                    FinCorners(pay, tPrev, tNext, dV, softSide, out D2 c0, out bool clr0, out D2 c1, out bool clr1);
                    position = pay.role == RoleFinL ? c0 : c1;
                    value = (pay.role == RoleFinL ? clr0 : clr1) ? k_Clr : k_Opq;
                    return;
                }

                case RoleJoinIn:
                case RoleJoinOut:
                {
                    bool edgeAway = pay.role == RoleJoinIn ? awayPrev : awayNext;
                    if (!isPole || !edgeAway) { position = Hv; value = k_Clr; return; }

                    FinCorners(pay, tPrev, tNext, dV, softSide, out D2 c0, out bool clr0, out D2 c1, out _);
                    D2 ext = clr0 ? c0 : c1;
                    D2 m = pay.role == RoleJoinIn ? mPrev : mNext;

                    D2 v0 = ext - Hv;
                    double r0 = Length(v0);

                    // The far angle comes from m itself, not from wV * m. Identical for wV > 0, since a
                    // positive scale does not move an Atan2 -- but a back angle of exactly zero makes wV
                    // zero, and Atan2(0, 0) is a garbage direction.
                    double a1 = Math.Atan2(m.y, m.x);

                    // A degenerate start collapses the SWEEP, never the fan. This used to return early
                    // and put every fan vertex on Hv, which was correct only while the two angles were
                    // the same: r0 == 0 then implied wV == 0, so the band corner was also at Hv. With
                    // Side and Back decoupled, a zero side angle leaves wV untouched, and collapsing the
                    // fan while the band corner sits at Hv + wV*m tears a hole exactly where the fan
                    // belongs. Starting at a1 makes the fan purely radial instead.
                    double a0 = (r0 > 1e-9) ? Math.Atan2(v0.y, v0.x) : a1;
                    double da = ShortestAngle(a1 - a0);

                    // The radius is lerped as well as the angle. With W derived from the same angle as
                    // the fin, its corner already sits at radius W, so the lerp was a no-op -- report 28
                    // measured that at 6.7e-16. Splitting Side from Back is what it was kept for.
                    double f = (double)pay.k / u.segments;
                    double rr = r0 + (wV - r0) * f;
                    double aa = a0 + da * f;
                    position = Hv + rr * Dir(aa);
                    value = k_Clr;
                    return;
                }

                case RoleConvex:
                {
                    if (isPole || concaveV || !(awayPrev && awayNext))
                    {
                        position = Hv; value = k_Clr; return;
                    }
                    D2 d = Rot(mPrev, sweepV * pay.k / u.segments);
                    position = Hv + wV * d;
                    value = k_Clr;
                    return;
                }

                case RoleConcave:
                {
                    if (isPole || !concaveV || !(awayPrev && awayNext))
                    {
                        position = Hv; value = k_Clr; return;
                    }
                    if (!Fillet(pay, u, tPP, tNN, wV, wP, wN, Hv, Hp, Hn, Hp2, Hn2, mPrev, mNext, uA, uB, halfV,
                                awayPrev, awayNext, out D2 ta, out D2 tb, out D2 cc))
                    {
                        position = Hv; value = k_Clr; return;
                    }

                    double r = Length(ta - cc);
                    double aA = Math.Atan2((ta - cc).y, (ta - cc).x);
                    double aB = Math.Atan2((tb - cc).y, (tb - cc).x);
                    double d = ShortestAngle(aB - aA);
                    double a = aA + d * pay.k / u.segments;
                    position = cc + r * Dir(a);
                    value = k_Clr;
                    return;
                }
            }

            position = Hv;
            value = k_Clr;
        }

        // hard(v) = v + d(v). Depends only on the vertex, never on which edge is asking, so adjacent
        // edges agree exactly at a shared vertex. That is what makes the per-vertex form possible at
        // all -- handoff v2 section 4. The construction never required the throw to be radial, only
        // that it be the same function of a point regardless of who asks.
        static D2 Hard(D2 p, D2 disp) => p + disp;

        // The throw associated with an EDGE rather than a vertex, used by every back-facing test.
        //
        // Symmetric in the edge's two endpoints, and that is the whole point: the band along
        // (prev -> v) is built from two different vertices' outputs, and both gate on "does this edge
        // cast?". They agree only if both read something neither one owns. Taking one endpoint's own
        // displacement lets v answer yes while prev answers no, collapsing one corner of the band quad.
        //
        // Unnormalised on purpose -- every consumer takes the sign of a dot or cross with it. Radially,
        // Dot(Perp(e), prev - light) and Dot(Perp(e), v - light) are EQUAL (they differ by
        // Dot(Perp(e), e), which is zero), so both terms carry the same sign and the reduction to the
        // old test is exact rather than approximate.
        static D2 EdgeThrow(D2 dA, D2 dB) => dA + dB;

        /// <summary>
        /// The offset each of the five points a vertex reads is thrown by. Mirrors the HLSL's
        /// SoftShadowDisplacements so the two stay structurally identical.
        /// </summary>
        internal struct Displacements
        {
            public D2 v, prev, next, prev2, next2;

            /// <summary>Every point thrown by the same offset: a parallel projection.</summary>
            public static Displacements Uniform(D2 d) => new Displacements
            {
                v = d, prev = d, next = d, prev2 = d, next2 = d,
            };

            /// <summary>
            /// Every point thrown <paramref name="len"/> along its own ray from the light -- the old
            /// scalar behaviour, reproduced exactly.
            /// </summary>
            public static Displacements Radial(in Payload pay, D2 light, double len) => new Displacements
            {
                v     = len * Norm(pay.v - light),
                prev  = len * Norm(pay.prev - light),
                next  = len * Norm(pay.next - light),
                prev2 = len * Norm(pay.prev2 - light),
                next2 = len * Norm(pay.next2 - light),
            };
        }

        // W, derived from the projection throw and never authored -- handoff v2 section 3. It is
        // exactly the chord between the hard ray and the softened ray at throw distance |d|, which is
        // what keeps the band's outer offset equal to the fin's lateral reach. Decoupling the two steps
        // the outer boundary at every pole (report 21).
        static double BandWidth(D2 disp, double soft) => Length(disp) * 2.0 * Math.Sin(soft / 2.0);

        static double Clamp(double x, double lo, double hi) => x < lo ? lo : (x > hi ? hi : x);

        // tPrev / tNext are the edge throws for (prev -> v) and (v -> next), the SAME values the
        // back-facing tests use. Deriving the interior side from them rather than from v's own
        // displacement is what keeps this test from contradicting isPole: isPole is exactly "these two
        // edges disagree", so a side test reading a different quantity could disagree about whether a
        // pole exists and on which side its body lies -- putting a full-shadow wedge on the lit side.
        static void FinCorners(in Payload pay, D2 tPrev, D2 tNext, D2 disp, double soft,
                               out D2 c0, out bool clr0, out D2 c1, out bool clr1)
        {
            // Which side is interior needs no winding knowledge: at a pole BOTH outline neighbours
            // lie on the same side of v's throw line. Summing both cross products rather than trusting
            // one keeps the sign stable on thin sliver casters, where a single neighbour can lie
            // nearly along the throw.
            double inter = Cross(tPrev, pay.prev - pay.v) + Cross(tNext, pay.next - pay.v);

            FinCorner(pay, disp, soft, inter, -1.0, out c0, out clr0);
            FinCorner(pay, disp, soft, inter, +1.0, out c1, out clr1);
        }

        static void FinCorner(in Payload pay, D2 disp, double soft, double inter,
                              double side, out D2 corner, out bool isClear)
        {
            // Interior half-wedge collapse: the interior corner is pulled onto the hard shadow
            // direction so the fin becomes exactly the exterior half-wedge, abutting the body along
            // the silhouette ray instead of straddling it.
            if (side * inter > 0.0)
            {
                corner = pay.v + disp;
                isClear = false;                       // opaque, and EXACTLY coincident with hard(v)
            }
            else
            {
                // Rotating the displacement rather than rebuilding it from an angle keeps the derived-W
                // identity exact: |Rot(d, soft) - d| == |d| * 2*sin(soft/2) == BandWidth(d, soft),
                // which is what report 28 measured at 6.7e-16 and what the join fan's radius lerp uses.
                corner = pay.v + Rot(disp, side * soft);
                isClear = true;
            }
        }

        // Limit along the shared edge's outer line at the far vertex.
        //
        // wFar is the band width at the FAR vertex, not at v. Every offset here is anchored at hFar,
        // so it is the far vertex's own width that decides where its band outer line sits -- and that
        // line is where the far vertex will place its own band corner when it is itself processed.
        // Using v's width instead measures against a line nothing is on, which lets the fillet
        // overshoot into the neighbour's band.
        //
        // mIn / mOut are the FAR vertex's incoming and outgoing projected edge normals, in that
        // order -- the sweep sign depends on it, and which of the two is the shared edge differs
        // between the backward and forward reach. Passing them the same way round both times inverts
        // the far-concavity test one way round, which over-granted the radius by 73% in report 26.
        //
        // farOtherAway says whether the far vertex's OTHER edge carries a band. Without it the far
        // vertex counts as filleted when it is not, which both mis-sets the limit and splits the
        // allotment in two. Same condition as fillable_set in the global build -- report 27.
        static bool FarLimit(double wg, double wFar, D2 hFar, D2 hFar2, D2 mIn, D2 mOut,
                             bool sharedIsIn, D2 hNear, bool farOtherAway,
                             out D2 limit)
        {
            D2 mShared = sharedIsIn ? mIn : mOut;
            double sw = Math.Atan2(Cross(mIn, mOut), Dot(mIn, mOut));
            bool farConcave = (sw * wg < 0.0) && farOtherAway;
            if (!farConcave)
            {
                limit = hFar + wFar * mShared;
                return false;
            }

            D2 mOther = sharedIsIn ? mOut : mIn;
            if (!Intersect(hFar + wFar * mOther, Norm(hFar2 - hFar),
                           hFar + wFar * mShared, Norm(hNear - hFar), out limit))
            {
                limit = hFar + wFar * mShared;
                return false;
            }
            return true;
        }

        // The radius budget of handoff v2 section 7b, per vertex, with no array access.
        //
        // tPP is the edge throw for (prev2 -> prev) and tNN the one for (next -> next2): the far
        // vertex's OTHER edge in each direction. They arrive precomputed so the back-facing test here
        // is the identical expression the far vertex will run on its own edge -- see EdgeThrow.
        static double Radius(in Payload pay, in Uniforms u, D2 tPP, D2 tNN, double wV, double wP, double wN,
                             D2 Hv, D2 Hp, D2 Hn, D2 Hp2, D2 Hn2,
                             D2 mPrev, D2 mNext, D2 uA, D2 uB, double halfV,
                             bool awayPrev, bool awayNext, out D2 cmv)
        {
            double wg = pay.winding;
            cmv = default;

            if (u.radiusRequested <= 1e-9 || halfV < 1e-4 || halfV > Math.PI / 2.0 - 1e-4)
                return 0.0;
            if (!Intersect(Hv + wV * mPrev, uA, Hv + wV * mNext, uB, out cmv))
                return 0.0;

            double r = u.radiusRequested;

            // An edge with no band imposes no constraint, so it is skipped -- exactly as the global
            // budget skips non-away edges.
            if (awayPrev)
            {
                // At prev the incoming edge is (prev2 -> prev), the outgoing is (prev -> v) = shared.
                D2 mpp = Norm(wg * Perp(Hp - Hp2));
                D2 npp = Norm(wg * Perp(pay.prev - pay.prev2));
                bool awayPp = Dot(npp, tPP) > 0.0;
                bool farCon = FarLimit(wg, wP, Hp, Hp2, mpp, mPrev, sharedIsIn: false, Hv, awayPp,
                                       out D2 limFar);
                double usable = Dot(cmv - limFar, Norm(Hv - Hp));
                int ends = 1 + (farCon ? 1 : 0);
                r = Math.Min(r, Math.Max(0.0, usable / ends) * Math.Tan(halfV));
            }
            if (awayNext)
            {
                // At next the incoming edge is (v -> next) = shared, the outgoing is (next -> next2).
                D2 mnn = Norm(wg * Perp(Hn2 - Hn));
                D2 nnn = Norm(wg * Perp(pay.next2 - pay.next));
                bool awayNn = Dot(nnn, tNN) > 0.0;
                bool farCon2 = FarLimit(wg, wN, Hn, Hn2, mNext, mnn, sharedIsIn: true, Hv, awayNn,
                                        out D2 limFar2);
                double usable2 = Dot(limFar2 - cmv, Norm(Hn - Hv));
                int ends2 = 1 + (farCon2 ? 1 : 0);
                r = Math.Min(r, Math.Max(0.0, usable2 / ends2) * Math.Tan(halfV));
            }
            return Math.Max(0.0, r);
        }

        // The tangent fillet of handoff v2 section 6c. R = 0 collapses TA = TB = CM and reproduces
        // the square corner exactly, so the degenerate path needs no special case.
        static bool Fillet(in Payload pay, in Uniforms u, D2 tPP, D2 tNN, double wV, double wP, double wN,
                           D2 Hv, D2 Hp, D2 Hn, D2 Hp2, D2 Hn2,
                           D2 mPrev, D2 mNext, D2 uA, D2 uB, double halfV,
                           bool awayPrev, bool awayNext,
                           out D2 ta, out D2 tb, out D2 cc)
        {
            ta = tb = cc = default;
            double r = Radius(pay, u, tPP, tNN, wV, wP, wN, Hv, Hp, Hn, Hp2, Hn2, mPrev, mNext, uA, uB, halfV,
                              awayPrev, awayNext, out D2 cmv);
            if (r <= 1e-9)
                return false;

            ta = cmv + (r / Math.Tan(halfV)) * uA;
            tb = cmv + (r / Math.Tan(halfV)) * uB;
            cc = cmv + (r / Math.Sin(halfV)) * Norm(uA + uB);
            return true;
        }

        /// <summary>
        /// Winding sign for an outline, matching sim_shader.build_mesh. Constant per caster, which is
        /// why the port carries it as a uniform rather than a payload float.
        /// </summary>
        internal static double Winding(D2[] outline)
        {
            int n = outline.Length;
            double area = 0.0;
            for (int i = 0; i < n; i++)
            {
                D2 a = outline[i], b = outline[(i + 1) % n];
                area += a.x * b.y - b.x * a.y;
            }
            return area < 0.0 ? -1.0 : 1.0;
        }

        /// <summary>
        /// <c>hard(v)</c> for a single point under the RADIAL throw, exposed so a test can identify a
        /// collapsed payload without duplicating the projection. Still takes a light and a length
        /// rather than a displacement, because every caller is checking the built-in projection.
        /// </summary>
        internal static D2 HardFor(D2 v, D2 light, double length) => Hard(v, length * Norm(v - light));

        /// <summary>Builds the payload for one (vertex, role, k) triple.</summary>
        internal static Payload MakePayload(D2[] outline, double winding, int i, int role, int k)
        {
            int n = outline.Length;
            return new Payload
            {
                v     = outline[i],
                prev  = outline[((i - 1) % n + n) % n],
                next  = outline[(i + 1) % n],
                prev2 = outline[((i - 2) % n + n) % n],
                next2 = outline[(i + 2) % n],
                role = role,
                winding = winding,
                k = k,
            };
        }
    }
}
