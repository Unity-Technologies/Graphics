/**
 * Copyright (c) 2017 Eric Bruneton
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holders nor the names of its
 *    contributors may be used to endorse or promote products derived from
 *    this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 *
 * Precomputed Atmospheric Scattering
 * Copyright (c) 2008 INRIA
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holders nor the names of its
 *    contributors may be used to endorse or promote products derived from
 *    this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

/*
 * Single scattering
 * 
 * The single scattered radiance is the light arriving from the Sun at some
 * point after exactly one scattering event inside the atmosphere (which can be due
 * to air molecules or aerosol particles; we exclude reflections from the ground,
 * computed <a href="#irradiance">separately</a>). The following sections describe
 * how we compute it, how we store it in a precomputed texture, and how we read it
 * back.
 * 
 * Computation
 * 
 * Consider the Sun light scattered at a point q by air molecules before
 * arriving at another point p (for aerosols, replace "Rayleigh" with "Mie").
 * 
 * The radiance arriving at $\bp$ is the product of:
 * 
 * the solar irradiance at the top of the atmosphere,
 * the transmittance between the Sun and q (i.e. the fraction of the Sun
 * light at the top of the atmosphere that reaches q),
 * the Rayleigh scattering coefficient at q (i.e. the fraction of the
 * light arriving at q which is scattered, in any direction),
 * the Rayleigh phase function (i.e. the fraction of the scattered light at
 * q which is actually scattered towards p),
 * the transmittance between q and p (i.e. the fraction of the light
 * scattered at q towards p that reaches p).
 * 
 * Thus, by noting w_s the unit direction vector towards the Sun, and with
 * the following definitions:
 * 
 * <li>$r=\Vert\bo\bp\Vert$,</li>
 * <li>$d=\Vert\bp\bq\Vert$,</li>
 * <li>$\mu=(\bo\bp\cdot\bp\bq)/rd$,</li>
 * <li>$\mu_s=(\bo\bp\cdot\bw_s)/r$,</li>
 * <li>$\nu=(\bp\bq\cdot\bw_s)/d$</li>
 * 
 * the values of r and mu_s for q are
 * 
 * <li>$r_d=\Vert\bo\bq\Vert=\sqrt{d^2+2r\mu d +r^2}$,</li>
 * <li>$\mu_{s,d}=(\bo\bq\cdot\bw_s)/r_d=((\bo\bp+\bp\bq)\cdot\bw_s)/r_d=
 * (r\mu_s + d\nu)/r_d$</li>
 * 
 * and the Rayleigh and Mie single scattering components can be computed as follows
 * (note that we omit the solar irradiance and the phase function terms, as well as
 * the scattering coefficients at the bottom of the atmosphere - we add them later
 * on for efficiency reasons):
 */

void ComputeSingleScatteringIntegrand(TransmittanceTexture transmittance_texture, 
				      Length r, Number mu, Number mu_s, Number nu, Length d, 
				      bool ray_r_mu_intersects_ground, out DimensionlessSpectrum rayleigh, out DimensionlessSpectrum mie) 
{
  Length r_d = ClampRadius(sqrt(d * d + 2.0 * r * mu * d + r * r));
  Number mu_s_d = ClampCosine((r * mu_s + d * nu) / r_d);

  DimensionlessSpectrum transmittance =
      GetTransmittance(transmittance_texture, r, mu, d, ray_r_mu_intersects_ground) *
      GetTransmittanceToSun(transmittance_texture, r_d, mu_s_d);

  rayleigh = transmittance * GetProfileDensity(RayleighDensity(), r_d - bottom_radius);
  mie = transmittance * GetProfileDensity(MieDensity(), r_d - bottom_radius);

}

/*
 * Consider now the Sun light arriving at p from a given direction w,
 * after exactly one scattering event. The scattering event can occur at any point
 * q between p and the intersection i of the half-line [p,w) with
 * the nearest atmosphere boundary. Thus, the single scattered radiance at p
 * from direction w is the integral of the single scattered radiance from q
 * to p for all points q between p and i. To compute it, we first
 * need the length $\Vert\bp\bi\Vert$:
 */

Length DistanceToNearestAtmosphereBoundary(Length r, Number mu, bool ray_r_mu_intersects_ground) 
{
  if (ray_r_mu_intersects_ground)
    return DistanceToBottomAtmosphereBoundary(r, mu);
  else
    return DistanceToTopAtmosphereBoundary(r, mu);
  
}

/*
 * The single scattering integral can then be computed as follows (using
 * the <a href="https://en.wikipedia.org/wiki/Trapezoidal_rule">trapezoidal
 * rule</a>):
 */

void ComputeSingleScattering(
    TransmittanceTexture transmittance_texture,
    Length r, Number mu, Number mu_s, Number nu,
    bool ray_r_mu_intersects_ground,
    out IrradianceSpectrum rayleigh, out IrradianceSpectrum mie) 
{

  // Number of intervals for the numerical integration.
  const int SAMPLE_COUNT = 50;

  // The integration step, i.e. the length of each integration interval.
  Length dx = DistanceToNearestAtmosphereBoundary(r, mu, ray_r_mu_intersects_ground) / Number(SAMPLE_COUNT);

  // Integration loop.
  DimensionlessSpectrum rayleigh_sum = DimensionlessSpectrum(0,0,0);
  DimensionlessSpectrum mie_sum = DimensionlessSpectrum(0,0,0);

  for (int i = 0; i <= SAMPLE_COUNT; ++i) 
  {
    Length d_i = Number(i) * dx;
    // The Rayleigh and Mie single scattering at the current sample point.
    DimensionlessSpectrum rayleigh_i;
    DimensionlessSpectrum mie_i;
    ComputeSingleScatteringIntegrand(transmittance_texture, r, mu, mu_s, nu, d_i, ray_r_mu_intersects_ground, rayleigh_i, mie_i);

    // Sample weight (from the trapezoidal rule).
    Number weight_i = (i == 0 || i == SAMPLE_COUNT) ? 0.5 : 1.0;
    rayleigh_sum += rayleigh_i * weight_i;
    mie_sum += mie_i * weight_i;
  }

  rayleigh = rayleigh_sum * dx * solar_irradiance * rayleigh_scattering;

  mie = mie_sum * dx * solar_irradiance * mie_scattering;
}

/*
 * Note that we added the solar irradiance and the scattering coefficient terms
 * that we omitted in ComputeSingleScatteringIntegrand, but not the
 * phase function terms - they are added at <a href="#rendering">render time</a>
 * for better angular precision. We provide them here for completeness:
 */

InverseSolidAngle RayleighPhaseFunction(Number nu) 
{
  InverseSolidAngle k = 3.0 / (16.0 * B_PI * sr);
  return k * (1.0 + nu * nu);
}

InverseSolidAngle MiePhaseFunction(Number g, Number nu) 
{
  InverseSolidAngle k = 3.0 / (8.0 * B_PI * sr) * (1.0 - g * g) / (2.0 + g * g);
  return k * (1.0 + nu * nu) / pow(abs(1.0 + g * g - 2.0 * g * nu), 1.5);
}

/*
 * Precomputation
 * 
 * The ComputeSingleScattering function is quite costly to
 * evaluate, and a lot of evaluations are needed to compute multiple scattering.
 * We therefore want to precompute it in a texture, which requires a mapping from
 * the 4 function parameters to texture coordinates. Assuming for now that we have
 * 4D textures, we need to define a mapping from (r,mu,mu_s,nu) to texture
 * coordinates (u,v,w,z). The function below implements the mapping defined in
 * our <a href="https://hal.inria.fr/inria-00288758/en">paper</a>, with some small
 * improvements (refer to the paper and to the above figures for the notations):
 * 
 * the mapping for mu takes the minimal distance to the nearest atmosphere
 * boundary into account, to map mu to the full [0,1]$ interval (the original
 * mapping was not covering the full [0,1] interval).
 * the mapping for mu_s is more generic than in the paper (the original
 * mapping was using ad-hoc constants chosen for the Earth atmosphere case). It is
 * based on the distance to the top atmosphere boundary (for the sun rays), as for
 * the mu mapping, and uses only one ad-hoc (but configurable) parameter. Yet,
 * as the original definition, it provides an increased sampling rate near the
 * horizon.
 * 
 */

float4 GetScatteringTextureUvwzFromRMuMuSNu(
    Length r, Number mu, Number mu_s, Number nu,
    bool ray_r_mu_intersects_ground) 
{

  // Distance to top atmosphere boundary for a horizontal ray at ground level.
  Length H = sqrt(top_radius * top_radius - bottom_radius * bottom_radius);

  // Distance to the horizon.
  Length rho = SafeSqrt(r * r - bottom_radius * bottom_radius);
  Number u_r = GetTextureCoordFromUnitRange(rho / H, SCATTERING_TEXTURE_R_SIZE);

  // Discriminant of the quadratic equation for the intersections of the ray
  // (r,mu) with the ground (see RayIntersectsGround).
  Length r_mu = r * mu;
  Area discriminant = r_mu * r_mu - r * r + bottom_radius * bottom_radius;
  Number u_mu;

  if (ray_r_mu_intersects_ground) 
  {
    // Distance to the ground for the ray (r,mu), and its minimum and maximum
    // values over all mu - obtained for (r,-1) and (r,mu_horizon).
    Length d = -r_mu - SafeSqrt(discriminant);
    Length d_min = r - bottom_radius;
    Length d_max = rho;
    u_mu = 0.5 - 0.5 * GetTextureCoordFromUnitRange(d_max == d_min ? 0.0 :
        (d - d_min) / (d_max - d_min), SCATTERING_TEXTURE_MU_SIZE / 2.0);
  } 
  else 
  {
    // Distance to the top atmosphere boundary for the ray (r,mu), and its
    // minimum and maximum values over all mu - obtained for (r,1) and
    // (r,mu_horizon).
    Length d = -r_mu + SafeSqrt(discriminant + H * H);
    Length d_min = top_radius - r;
    Length d_max = rho + H;
    u_mu = 0.5 + 0.5 * GetTextureCoordFromUnitRange(
        (d - d_min) / (d_max - d_min), SCATTERING_TEXTURE_MU_SIZE / 2.0);
  }

  Length d = DistanceToTopAtmosphereBoundary(bottom_radius, mu_s);
  Length d_min = top_radius - bottom_radius;
  Length d_max = H;
  Number a = (d - d_min) / (d_max - d_min);
  Number A = -2.0 * mu_s_min * bottom_radius / (d_max - d_min);

  Number u_mu_s = GetTextureCoordFromUnitRange(max(1.0 - a / A, 0.0) / (1.0 + a), SCATTERING_TEXTURE_MU_S_SIZE);

  Number u_nu = (nu + 1.0) / 2.0;
  return float4(u_nu, u_mu_s, u_mu, u_r);
}

/*
 * The inverse mapping follows immediately:
*/

void GetRMuMuSNuFromScatteringTextureUvwz(
    float4 uvwz, out Length r, out Number mu, out Number mu_s,
	out Number nu, out bool ray_r_mu_intersects_ground)
{

  // Distance to top atmosphere boundary for a horizontal ray at ground level.
  Length H = sqrt(top_radius * top_radius - bottom_radius * bottom_radius);
  // Distance to the horizon.
  Length rho = H * GetUnitRangeFromTextureCoord(uvwz.w, SCATTERING_TEXTURE_R_SIZE);
  r = sqrt(rho * rho + bottom_radius * bottom_radius);

  if (uvwz.z < 0.5) 
  {
    // Distance to the ground for the ray (r,mu), and its minimum and maximum
    // values over all mu - obtained for (r,-1) and (r,mu_horizon) - from which
    // we can recover mu:
    Length d_min = r - bottom_radius;
    Length d_max = rho;
    Length d = d_min + (d_max - d_min) * GetUnitRangeFromTextureCoord(1.0 - 2.0 * uvwz.z, SCATTERING_TEXTURE_MU_SIZE / 2.0);
    mu = d == 0.0 * m ? Number(-1.0) : ClampCosine(-(rho * rho + d * d) / (2.0 * r * d));
    ray_r_mu_intersects_ground = true;
  } 
  else 
  {
    // Distance to the top atmosphere boundary for the ray (r,mu), and its
    // minimum and maximum values over all mu - obtained for (r,1) and
    // (r,mu_horizon) - from which we can recover mu:
    Length d_min = top_radius - r;
    Length d_max = rho + H;
    Length d = d_min + (d_max - d_min) * GetUnitRangeFromTextureCoord(2.0 * uvwz.z - 1.0, SCATTERING_TEXTURE_MU_SIZE / 2.0);
    mu = d == 0.0 * m ? Number(1.0) : ClampCosine((H * H - rho * rho - d * d) / (2.0 * r * d));
    ray_r_mu_intersects_ground = false;
  }

  Number x_mu_s = GetUnitRangeFromTextureCoord(uvwz.y, SCATTERING_TEXTURE_MU_S_SIZE);
  Length d_min = top_radius - bottom_radius;
  Length d_max = H;
  Number A = -2.0 * mu_s_min * bottom_radius / (d_max - d_min);
  Number a = (A - x_mu_s * A) / (1.0 + x_mu_s * A);
  Length d = d_min + min(a, A) * (d_max - d_min);
  mu_s = d == 0.0 * m ? Number(1.0) : ClampCosine((H * H - d * d) / (2.0 * bottom_radius * d));

  nu = ClampCosine(uvwz.x * 2.0 - 1.0);
}

/*
 * We assumed above that we have 4D textures, which is not the case in practice.
 * We therefore need a further mapping, between 3D and 4D texture coordinates. The
 * function below expands a 3D texel coordinate into a 4D texture coordinate, and
 * then to (r,mu,mu_s,nu) parameters. It does so by "unpacking" two texel
 * coordinates from the x texel coordinate. Note also how we clamp the nu
 * parameter at the end. This is because nu is not a fully independent variable:
 * its range of values depends on mu and mu_s (this can be seen by computing
 * mu, mu_s and nu from the cartesian coordinates of the zenith, view and
 * sun unit direction vectors), and the previous functions implicitely assume this
 * (their assertions can break if this constraint is not respected).
 */

void GetRMuMuSNuFromScatteringTextureFragCoord(
    float3 gl_frag_coord,
    out Length r, out Number mu, out Number mu_s, out Number nu,
    out bool ray_r_mu_intersects_ground) 
{
  const float4 SCATTERING_TEXTURE_SIZE = float4(
      SCATTERING_TEXTURE_NU_SIZE - 1,
      SCATTERING_TEXTURE_MU_S_SIZE,
      SCATTERING_TEXTURE_MU_SIZE,
      SCATTERING_TEXTURE_R_SIZE);

  Number frag_coord_nu = floor(gl_frag_coord.x / Number(SCATTERING_TEXTURE_MU_S_SIZE));
  Number frag_coord_mu_s = mod(gl_frag_coord.x, Number(SCATTERING_TEXTURE_MU_S_SIZE));

  float4 uvwz = float4(frag_coord_nu, frag_coord_mu_s, gl_frag_coord.y, gl_frag_coord.z) / SCATTERING_TEXTURE_SIZE;

  GetRMuMuSNuFromScatteringTextureUvwz(uvwz, r, mu, mu_s, nu, ray_r_mu_intersects_ground);

  // Clamp nu to its valid range of values, given mu and mu_s.
  nu = clamp(nu, mu * mu_s - sqrt((1.0 - mu * mu) * (1.0 - mu_s * mu_s)),
      mu * mu_s + sqrt((1.0 - mu * mu) * (1.0 - mu_s * mu_s)));
}

/*
 * With this mapping, we can finally write a function to precompute a texel of
 * the single scattering in a 3D texture:
 */

void ComputeSingleScatteringTexture(
    TransmittanceTexture transmittance_texture, float3 gl_frag_coord,
    out IrradianceSpectrum rayleigh, out IrradianceSpectrum mie)
{
  Length r;
  Number mu;
  Number mu_s;
  Number nu;
  bool ray_r_mu_intersects_ground;
  GetRMuMuSNuFromScatteringTextureFragCoord(gl_frag_coord,
      r, mu, mu_s, nu, ray_r_mu_intersects_ground);
  ComputeSingleScattering(transmittance_texture,
      r, mu, mu_s, nu, ray_r_mu_intersects_ground, rayleigh, mie);
}

/*
 * Lookup
 * 
 * With the help of the above precomputed texture, we can now get the scattering
 * between a point and the nearest atmosphere boundary with two texture lookups (we
 * need two 3D texture lookups to emulate a single 4D texture lookup with
 * quadrilinear interpolation; the 3D texture coordinates are computed using the
 * inverse of the 3D-4D mapping defined in
 * GetRMuMuSNuFromScatteringTextureFragCoord):
 */

AbstractSpectrum GetScattering(
    AbstractScatteringTexture scattering_texture,
    Length r, Number mu, Number mu_s, Number nu,
    bool ray_r_mu_intersects_ground) 
{
  float4 uvwz = GetScatteringTextureUvwzFromRMuMuSNu(r, mu, mu_s, nu, ray_r_mu_intersects_ground);

  Number tex_coord_x = uvwz.x * Number(SCATTERING_TEXTURE_NU_SIZE - 1);
  Number tex_x = floor(tex_coord_x);
  Number lerp = tex_coord_x - tex_x;

  float3 uvw0 = float3((tex_x + uvwz.y) / Number(SCATTERING_TEXTURE_NU_SIZE), uvwz.z, uvwz.w);

  float3 uvw1 = float3((tex_x + 1.0 + uvwz.y) / Number(SCATTERING_TEXTURE_NU_SIZE), uvwz.z, uvwz.w);

  return TEX3D(scattering_texture, uvw0).rgb * (1.0 - lerp) + TEX3D(scattering_texture, uvw1).rgb * lerp;
}

/*
 * Finally, we provide here a convenience lookup function which will be useful
 * in the next section. This function returns either the single scattering, with
 * the phase functions included, or the n-th order of scattering, with n>1. It
 * assumes that, if scattering_order is strictly greater than 1, then
 * multiple_scattering_texture corresponds to this scattering order,
 * with both Rayleigh and Mie included, as well as all the phase function terms.
 */

RadianceSpectrum GetScattering(
    ReducedScatteringTexture single_rayleigh_scattering_texture,
    ReducedScatteringTexture single_mie_scattering_texture,
    ScatteringTexture multiple_scattering_texture,
    Length r, Number mu, Number mu_s, Number nu,
    bool ray_r_mu_intersects_ground,
    int scattering_order) 
{
  if (scattering_order == 1) 
  {
    IrradianceSpectrum rayleigh = GetScattering(
        single_rayleigh_scattering_texture, r, mu, mu_s, nu,
        ray_r_mu_intersects_ground);

    IrradianceSpectrum mie = GetScattering(
        single_mie_scattering_texture, r, mu, mu_s, nu,
        ray_r_mu_intersects_ground);

    return rayleigh * RayleighPhaseFunction(nu) +
        mie * MiePhaseFunction(mie_phase_function_g, nu);
  } 
  else 
  {
    return GetScattering(
        multiple_scattering_texture, r, mu, mu_s, nu,
        ray_r_mu_intersects_ground);
  }
}

/*
 * Multiple scattering
 * 
 * The multiply scattered radiance is the light arriving from the Sun at some
 * point in the atmosphere after two or more bounces (where a bounce is
 * either a scattering event or a reflection from the ground). The following
 * sections describe how we compute it, how we store it in a precomputed texture,
 * and how we read it back.
 * 
 * Note that, as for single scattering, we exclude here the light paths whose
 * last bounce is a reflection on the ground. The contribution from these
 * paths is computed separately, at rendering time, in order to take the actual
 * ground albedo into account (for intermediate reflections on the ground, which
 * are precomputed, we use an average, uniform albedo).
 * 
 * Computation
 * 
 * Multiple scattering can be decomposed into the sum of double scattering,
 * triple scattering, etc, where each term corresponds to the light arriving from
 * the Sun at some point in the atmosphere after exactly 2, 3, etc bounces.
 * Moreover, each term can be computed from the previous one. Indeed, the light
 * arriving at some point p from direction w after n bounces is an
 * integral over all the possible points q for the last bounce, which involves
 * the light arriving at q from any direction, after n-1 bounces.
 * 
 * This description shows that each scattering order requires a triple integral
 * to be computed from the previous one (one integral over all the points q
 * on the segment from p to the nearest atmosphere boundary in direction w,
 * and a nested double integral over all directions at each point q).
 * Therefore, if we wanted to compute each order "from scratch", we would need a
 * triple integral for double scattering, a sextuple integral for triple
 * scattering, etc. This would be clearly inefficient, because of all the redundant
 * computations (the computations for order n would basically redo all the
 * computations for all previous orders, leading to quadratic complexity in the
 * total number of orders). Instead, it is much more efficient to proceed as
 * follows:
 * 
 * precompute single scattering in a texture (as described above), for n^2:
 * 
 * precompute the n-th scattering in a texture, with a triple integral whose
 * integrand uses lookups in the (n-1)-th scattering texture
 * 
 * This strategy avoids many redundant computations but does not eliminate all
 * of them. Consider for instance the points p and p' in the figure below,
 * and the computations which are necessary to compute the light arriving at these
 * two points from direction w after n bounces. These computations involve,
 * in particular, the evaluation of the radiance L which is scattered at q in
 * direction -w, and coming from all directions after n-1 bounces:
 * 
 * 
 * Therefore, if we computed the n-th scattering with a triple integral as
 * described above, we would compute L redundantly (in fact, for all points p
 * between q and the nearest atmosphere boundary in direction -w). To avoid
 * this, and thus increase the efficiency of the multiple scattering computations,
 * we refine the above algorithm as follows:
 * 
 * precompute single scattering in a texture (as described above),for n^2:
 * 
 * for each point q and direction w, precompute the light which is
 * scattered at q towards direction -w, coming from any direction after
 * n-1 bounces (this involves only a double integral, whose integrand uses
 * lookups in the (n-1)-th scattering texture),
 * for each point p and direction w, precompute the light coming from
 * direction w after n bounces (this involves only a single integral, whose
 * integrand uses lookups in the texture computed at the previous line)
 * 
 * To get a complete algorithm, we must now specify how we implement the two
 * steps in the above loop. This is what we do in the rest of this section.
 * 
 * First step
 * 
 * The first step computes the radiance which is scattered at some point q
 * inside the atmosphere, towards some direction -w. Furthermore, we assume
 * that this scattering event is the n-th bounce.
 * 
 * This radiance is the integral over all the possible incident directions
 * w_i, of the product of
 * 
 * the incident radiance L_i arriving at q from direction w_i after
 * n-1 bounces, which is the sum of:
 * 
 * a term given by the precomputed scattering texture for the (n-1)-th
 * order,
 * if the ray [q, w_i) intersects the ground at r, the contribution
 * from the light paths with n-1 bounces and whose last bounce is at r, i.e.
 * on the ground (these paths are excluded, by definition, from our precomputed
 * textures, but we must take them into account here since the bounce on the ground
 * is followed by a bounce at q). This contribution, in turn, is the product
 * of:
 * 
 * the transmittance between q and r,
 * the (average) ground albedo,
 * the <a href="https://www.cs.princeton.edu/~smr/cs348c-97/surveypaper.html"
 * >Lambertian BRDF</a> $1/\pi$,
 * the irradiance received on the ground after n-2 bounces. We explain in the
 * <a href="#irradiance">next section</a> how we precompute it in a texture. For
 * now, we assume that we can use the following function to retrieve this
 * irradiance from a precomputed texture:
 */

IrradianceSpectrum GetIrradiance(
    IrradianceTexture irradiance_texture,
    Length r, Number mu_s);

/*
 * 
 * the scattering coefficient at q,
 * the scattering phase function for the directions w and w_i
 * 
 * This leads to the following implementation (where
 * multiple_scattering_texture is supposed to contain the (n-1)-th
 * order of scattering, if n>2, irradiance_texture is the irradiance
 * received on the ground after n-2 bounces, and <scattering_order is
 * equal to n):
 */

RadianceDensitySpectrum ComputeScatteringDensity(
    TransmittanceTexture transmittance_texture,
    ReducedScatteringTexture single_rayleigh_scattering_texture,
    ReducedScatteringTexture single_mie_scattering_texture,
    ScatteringTexture multiple_scattering_texture,
    IrradianceTexture irradiance_texture,
    Length r, Number mu, Number mu_s, Number nu, int scattering_order) 
{

  // Compute unit direction vectors for the zenith, the view direction omega and
  // and the sun direction omega_s, such that the cosine of the view-zenith
  // angle is mu, the cosine of the sun-zenith angle is mu_s, and the cosine of
  // the view-sun angle is nu. The goal is to simplify computations below.
  float3 zenith_direction = float3(0.0, 0.0, 1.0);
  float3 omega = float3(sqrt(1.0 - mu * mu), 0.0, mu);
  Number sun_dir_x = omega.x == 0.0 ? 0.0 : (nu - mu * mu_s) / omega.x;
  Number sun_dir_y = sqrt(max(1.0 - sun_dir_x * sun_dir_x - mu_s * mu_s, 0.0));
  float3 omega_s = float3(sun_dir_x, sun_dir_y, mu_s);

  const int SAMPLE_COUNT = 16;
  const Angle dphi = pi / Number(SAMPLE_COUNT);
  const Angle dtheta = pi / Number(SAMPLE_COUNT);
  RadianceDensitySpectrum rayleigh_mie = RadianceDensitySpectrum(0,0,0);

  // Nested loops for the integral over all the incident directions omega_i.
  for (int l = 0; l < SAMPLE_COUNT; ++l) 
  {
    Angle theta = (Number(l) + 0.5) * dtheta;
    Number cos_theta = cos(theta);
    Number sin_theta = sin(theta);
    bool ray_r_theta_intersects_ground = RayIntersectsGround(r, cos_theta);

    // The distance and transmittance to the ground only depend on theta, so we
    // can compute them in the outer loop for efficiency.
    Length distance_to_ground = 0.0 * m;
    DimensionlessSpectrum transmittance_to_ground = DimensionlessSpectrum(0,0,0);
    DimensionlessSpectrum ground_albedo = DimensionlessSpectrum(0,0,0);
    if (ray_r_theta_intersects_ground) 
    {
      distance_to_ground = DistanceToBottomAtmosphereBoundary(r, cos_theta);
      transmittance_to_ground = GetTransmittance(transmittance_texture, r, cos_theta, distance_to_ground, true);
    }

    for (int m = 0; m < 2 * SAMPLE_COUNT; ++m) 
    {
      Angle phi = (Number(m) + 0.5) * dphi;
      float3 omega_i = float3(cos(phi) * sin_theta, sin(phi) * sin_theta, cos_theta);
      SolidAngle domega_i = (dtheta / rad) * (dphi / rad) * sin(theta) * sr;

      // The radiance L_i arriving from direction omega_i after n-1 bounces is
      // the sum of a term given by the precomputed scattering texture for the
      // (n-1)-th order:
      Number nu1 = dot(omega_s, omega_i);
      RadianceSpectrum incident_radiance = GetScattering(
          single_rayleigh_scattering_texture, single_mie_scattering_texture,
          multiple_scattering_texture, r, omega_i.z, mu_s, nu1,
          ray_r_theta_intersects_ground, scattering_order - 1);

      // and of the contribution from the light paths with n-1 bounces and whose
      // last bounce is on the ground. This contribution is the product of the
      // transmittance to the ground, the ground albedo, the ground BRDF, and
      // the irradiance received on the ground after n-2 bounces.
      float3 ground_normal = normalize(zenith_direction * r + omega_i * distance_to_ground);
      IrradianceSpectrum ground_irradiance = GetIrradiance(irradiance_texture, bottom_radius, dot(ground_normal, omega_s));
      incident_radiance += transmittance_to_ground * ground_albedo * (1.0 / (B_PI * sr)) * ground_irradiance;

      // The radiance finally scattered from direction omega_i towards direction
      // -omega is the product of the incident radiance, the scattering
      // coefficient, and the phase function for directions omega and omega_i
      // (all this summed over all particle types, i.e. Rayleigh and Mie).
      Number nu2 = dot(omega, omega_i);

      Number rayleigh_density = GetProfileDensity(RayleighDensity(), r - bottom_radius);
      Number mie_density = GetProfileDensity(MieDensity(), r - bottom_radius);

      rayleigh_mie += incident_radiance * (
          rayleigh_scattering * rayleigh_density * RayleighPhaseFunction(nu2) +
          mie_scattering * mie_density * MiePhaseFunction(mie_phase_function_g, nu2)) *
          domega_i;
    }
  }

  return rayleigh_mie;
}

/*
 * Second step
 * 
 * The second step to compute the n-th order of scattering is to compute for
 * each point p and direction w, the radiance coming from direction w
 * after n bounces, using a texture precomputed with the previous function.
 * 
 * This radiance is the integral over all points q between p and the
 * nearest atmosphere boundary in direction w of the product of:
 * 
 * a term given by a texture precomputed with the previous function, namely
 * the radiance scattered at q towards p, coming from any direction after
 * n-1 bounces, the transmittance betweeen p and q
 * 
 * Note that this excludes the light paths with n bounces and whose last
 * bounce is on the ground, on purpose. Indeed, we chose to exclude these paths
 * from our precomputed textures so that we can compute them at render time
 * instead, using the actual ground albedo.
 * 
 * The implementation for this second step is straightforward:
*/

RadianceSpectrum ComputeMultipleScattering(
    TransmittanceTexture transmittance_texture,
    ScatteringDensityTexture scattering_density_texture,
    Length r, Number mu, Number mu_s, Number nu,
    bool ray_r_mu_intersects_ground) 
{

  // Number of intervals for the numerical integration.
  const int SAMPLE_COUNT = 50;
  // The integration step, i.e. the length of each integration interval.
  Length dx = DistanceToNearestAtmosphereBoundary(r, mu, ray_r_mu_intersects_ground) / Number(SAMPLE_COUNT);

  // Integration loop.
  RadianceSpectrum rayleigh_mie_sum = RadianceSpectrum(0,0,0);

  for (int i = 0; i <= SAMPLE_COUNT; ++i) 
  {
    Length d_i = Number(i) * dx;

    // The r, mu and mu_s parameters at the current integration point (see the
    // single scattering section for a detailed explanation).
    Length r_i = ClampRadius(sqrt(d_i * d_i + 2.0 * r * mu * d_i + r * r));
    Number mu_i = ClampCosine((r * mu + d_i) / r_i);
    Number mu_s_i = ClampCosine((r * mu_s + d_i * nu) / r_i);

    // The Rayleigh and Mie multiple scattering at the current sample point.
    RadianceSpectrum rayleigh_mie_i =
        GetScattering(scattering_density_texture, r_i, mu_i, mu_s_i, nu, ray_r_mu_intersects_ground) *
        GetTransmittance(transmittance_texture, r, mu, d_i, ray_r_mu_intersects_ground) * dx;

    // Sample weight (from the trapezoidal rule).
    Number weight_i = (i == 0 || i == SAMPLE_COUNT) ? 0.5 : 1.0;
    rayleigh_mie_sum += rayleigh_mie_i * weight_i;
  }

  return rayleigh_mie_sum;
}

/*
 * Precomputation
 * 
 * s explained in the overall algorithm to
 * compute multiple scattering, we need to precompute each order of scattering in a
 * texture to save computations while computing the next order. And, in order to
 * store a function in a texture, we need a mapping from the function parameters to
 * texture coordinates. Fortunately, all the orders of scattering depend on the
 * same (r,mu,mu_s,nu) parameters as single scattering, so we can simple reuse
 * the mappings defined for single scattering. This immediately leads to the
 * following simple functions to precompute a texel of the textures for the
 * first and second steps of each iteration
 * over the number of bounces:
*/

RadianceDensitySpectrum ComputeScatteringDensityTexture(
    TransmittanceTexture transmittance_texture,
    ReducedScatteringTexture single_rayleigh_scattering_texture,
    ReducedScatteringTexture single_mie_scattering_texture,
    ScatteringTexture multiple_scattering_texture,
    IrradianceTexture irradiance_texture,
    float3 gl_frag_coord, int scattering_order) 
{
  Length r;
  Number mu;
  Number mu_s;
  Number nu;
  bool ray_r_mu_intersects_ground;
  GetRMuMuSNuFromScatteringTextureFragCoord(gl_frag_coord,
      r, mu, mu_s, nu, ray_r_mu_intersects_ground);

  return ComputeScatteringDensity(transmittance_texture,
      single_rayleigh_scattering_texture, single_mie_scattering_texture,
      multiple_scattering_texture, irradiance_texture, r, mu, mu_s, nu,
      scattering_order);
}

RadianceSpectrum ComputeMultipleScatteringTexture(
    TransmittanceTexture transmittance_texture,
    ScatteringDensityTexture scattering_density_texture,
    float3 gl_frag_coord, out Number nu) 
{
  Length r;
  Number mu;
  Number mu_s;
  bool ray_r_mu_intersects_ground;
  GetRMuMuSNuFromScatteringTextureFragCoord(gl_frag_coord,
      r, mu, mu_s, nu, ray_r_mu_intersects_ground);

  return ComputeMultipleScattering(transmittance_texture,
      scattering_density_texture, r, mu, mu_s, nu,
      ray_r_mu_intersects_ground);
}




