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
 * Transmittance
 *
 * https://en.wikipedia.org/wiki/Transmittance
 *
 * As the light travels from a point p to a point q in the atmosphere,
 * it is partially absorbed and scattered out of its initial direction because of
 * the air molecules and the aerosol particles. Thus, the light arriving at q
 * is only a fraction of the light from p, and this fraction, which depends on
 * wavelength, is called the transmittance. The following sections describe how 
 * we compute it, how we store it in a precomputed
 * texture, and how we read it back.
 *
 * Computation
 *
 * For 3 aligned points p, q and r inside the atmosphere, in this
 * order, the transmittance between p and r is the product of the
 * transmittance between p and q and between q and r.
 * In particular, the transmittance between p and q is the transmittance
 * between p and the nearest intersection i of the half-line [p,q]
 * with the top or bottom atmosphere boundary, divided by the transmittance between
 * q and i (or 0 if the segment [p,q] intersects the ground).
 *
 * Also, the transmittance between p and q and between q and p
 * are the same. Thus, to compute the transmittance between arbitrary points, it
 * is sufficient to know the transmittance between a point p in the atmosphere,
 * and points i on the top atmosphere boundary. This transmittance depends on
 * only two parameters, which can be taken as the radius $r=\Vert\bo\bp\Vert$ and
 * the cosine of the "view zenith angle",
 * $\mu=\bo\bp\cdot\bp\bi/\Vert\bo\bp\Vert\Vert\bp\bi\Vert$. To compute it, we
 * first need to compute the length $\Vert\bp\bi\Vert$, and we need to know when
 * the segment [p,i] intersects the ground.
 *
 * Distance to the top atmosphere boundary
 *
 * A point at distance d from p along [p,i] has coordinates
 * $[d\sqrt{1-\mu^2}, r+d\mu]^\top$, whose squared norm is $d^2+2r\mu d+r^2$.
 * Thus, by definition of i, we have
 * $\Vert\bp\bi\Vert^2+2r\mu\Vert\bp\bi\Vert+r^2=r_{\mathrm{top}}^2$,
 * from which we deduce the length $\Vert\bp\bi\Vert$:
 */

Length DistanceToTopAtmosphereBoundary(Length r, Number mu) 
{
  Area discriminant = r * r * (mu * mu - 1.0) + top_radius * top_radius;
  return ClampDistance(-r * mu + SafeSqrt(discriminant));
}

/*
 * We will also need, in the other sections, the distance to the bottom
 * atmosphere boundary, which can be computed in a similar way (this code assumes
 * that [p,i] intersects the ground).
 */

Length DistanceToBottomAtmosphereBoundary(Length r, Number mu) 
{
  Area discriminant = r * r * (mu * mu - 1.0) + bottom_radius * bottom_radius;
  return ClampDistance(-r * mu - SafeSqrt(discriminant));
}

/*
 * Intersections with the ground
 * 
 * The segment [p,i] intersects the ground when
 * $d^2+2r\mu d+r^2=r_{\mathrm{bottom}}^2$ has a solution with $d \ge 0$. This
 * requires the discriminant $r^2(\mu^2-1)+r_{\mathrm{bottom}}^2$ to be positive,
 * from which we deduce the following function:
 */

bool RayIntersectsGround(Length r, Number mu) 
{
  return mu < 0.0 && r * r * (mu * mu - 1.0) + bottom_radius * bottom_radius >= 0.0 * m2;
}

/*
 * Transmittance to the top atmosphere boundary
 * 
 * We can now compute the transmittance between p and i. From its definition and the Beer-Lambert law,
 * this involves the integral of the number density of air molecules along the
 * segment [p,i], as well as the integral of the number density of aerosols
 * and the integral of the number density of air molecules that absorb light
 * (e.g. ozone) - along the same segment. These 3 integrals have the same form and,
 * when the segment [p,i] does not intersect the ground, they can be computed
 * numerically with the help of the following auxilliary function using the trapezoidal rule.
 *
 * https://en.wikipedia.org/wiki/Trapezoidal_rule
 * https://en.wikipedia.org/wiki/Beer-Lambert_law
 *
 */

Number GetLayerDensity(DensityProfileLayer layer, Length altitude) 
{
  Number density = layer.exp_term * exp(layer.exp_scale * altitude) + layer.linear_term * altitude + layer.constant_term;
  return clamp(density, Number(0.0), Number(1.0));
}

Number GetProfileDensity(DensityProfile profile, Length altitude) 
{
  return altitude < profile.layers[0].width ?
      GetLayerDensity(profile.layers[0], altitude) :
      GetLayerDensity(profile.layers[1], altitude);
}

Length ComputeOpticalLengthToTopAtmosphereBoundary(DensityProfile profile, Length r, Number mu) 
{
  // Number of intervals for the numerical integration.
  const int SAMPLE_COUNT = 500;
  // The integration step, i.e. the length of each integration interval.
  Length dx = DistanceToTopAtmosphereBoundary(r, mu) / Number(SAMPLE_COUNT);
  // Integration loop.
  Length result = 0.0 * m;
  for (int i = 0; i <= SAMPLE_COUNT; ++i) 
  {
    Length d_i = Number(i) * dx;
    // Distance between the current sample point and the planet center.
    Length r_i = sqrt(d_i * d_i + 2.0 * r * mu * d_i + r * r);
    // Number density at the current sample point (divided by the number density
    // at the bottom of the atmosphere, yielding a dimensionless number).
    Number y_i = GetProfileDensity(profile, r_i - bottom_radius);
    // Sample weight (from the trapezoidal rule).
    Number weight_i = i == 0 || i == SAMPLE_COUNT ? 0.5 : 1.0;
    result += y_i * weight_i * dx;
  }

  return result;
}

/*
 * With this function the transmittance between p and i is now easy to
 * compute (we continue to assume that the segment does not intersect the ground):
 */

DimensionlessSpectrum ComputeTransmittanceToTopAtmosphereBoundary(Length r, Number mu) 
{

  DimensionlessSpectrum density = 0;
  density += rayleigh_scattering * ComputeOpticalLengthToTopAtmosphereBoundary(RayleighDensity(), r, mu);
  density += mie_extinction * ComputeOpticalLengthToTopAtmosphereBoundary(MieDensity(), r, mu);
  density += absorption_extinction * ComputeOpticalLengthToTopAtmosphereBoundary(AbsorptionDensity(), r, mu);

  return exp(-density);
}

/*
 * Precomputation
 *
 * The above function is quite costly to evaluate, and a lot of evaluations are
 * needed to compute single and multiple scattering. Fortunately this function
 * depends on only two parameters and is quite smooth, so we can precompute it in a
 * small 2D texture to optimize its evaluation.
 * 
 * For this we need a mapping between the function parameters (r,mu) and the
 * texture coordinates (u,v), and vice-versa, because these parameters do not
 * have the same units and range of values. And even if it was the case, storing a
 * function f from the [0,1] interval in a texture of size n would sample the
 * function at 0.5/n, 1.5/n, ... (n-0.5)/n, because texture samples are at
 * the center of texels. Therefore, this texture would only give us extrapolated
 * function values at the domain boundaries (0 and 1). To avoid this we need
 * to store f(0) at the center of texel 0 and f(1) at the center of texel
 * n-1. This can be done with the following mapping from values x in [0,1] to
 * texture coordinates u in [0.5/n,1-0.5/n] - and its inverse:
 */

Number GetTextureCoordFromUnitRange(Number x, int texture_size) {
  return 0.5 / Number(texture_size) + x * (1.0 - 1.0 / Number(texture_size));
}

Number GetUnitRangeFromTextureCoord(Number u, int texture_size) {
  return (u - 0.5 / Number(texture_size)) / (1.0 - 1.0 / Number(texture_size));
}

/*
 * Using these functions, we can now define a mapping between (r,mu) and the
 * texture coordinates (u,v), and its inverse, which avoid any extrapolation
 * during texture lookups. In the <a href=
 * "http://evasion.inrialpes.fr/~Eric.Bruneton/PrecomputedAtmosphericScattering2.zip"
 * >original implementation</a> this mapping was using some ad-hoc constants chosen
 * for the Earth atmosphere case. Here we use a generic mapping, working for any
 * atmosphere, but still providing an increased sampling rate near the horizon.
 * Our improved mapping is based on the parameterization described in our
 * <a href="https://hal.inria.fr/inria-00288758/en">paper</a> for the 4D textures:
 * we use the same mapping for r, and a slightly improved mapping for mu
 * (considering only the case where the view ray does not intersect the ground).
 * More precisely, we map mu to a value $x_{\mu}$ between 0 and 1 by considering
 * the distance d to the top atmosphere boundary, compared to its minimum and
 * maximum values $d_{\mathrm{min}}=r_{\mathrm{top}}-r$ and
 * $d_{\mathrm{max}}=\rho+H$
 *
 * With these definitions, the mapping from (r,\mu)$ to the texture coordinates
 * (u,v) can be implemented as follows:
 */

float2 GetTransmittanceTextureUvFromRMu(Length r, Number mu) 
{
  // Distance to top atmosphere boundary for a horizontal ray at ground level.
  Length H = sqrt(top_radius * top_radius - bottom_radius * bottom_radius);
  // Distance to the horizon.
  Length rho = SafeSqrt(r * r - bottom_radius * bottom_radius);
  // Distance to the top atmosphere boundary for the ray (r,mu), and its minimum
  // and maximum values over all mu - obtained for (r,1) and (r,mu_horizon).
  Length d = DistanceToTopAtmosphereBoundary(r, mu);
  Length d_min = top_radius - r;
  Length d_max = rho + H;
  Number x_mu = (d - d_min) / (d_max - d_min);
  Number x_r = rho / H;
  return float2(GetTextureCoordFromUnitRange(x_mu, TRANSMITTANCE_TEXTURE_WIDTH),
                GetTextureCoordFromUnitRange(x_r, TRANSMITTANCE_TEXTURE_HEIGHT));
}

/*
 * and the inverse mapping follows immediately:
 */

void GetRMuFromTransmittanceTextureUv(float2 uv, out Length r, out Number mu) 
{

  Number x_mu = GetUnitRangeFromTextureCoord(uv.x, TRANSMITTANCE_TEXTURE_WIDTH);
  Number x_r = GetUnitRangeFromTextureCoord(uv.y, TRANSMITTANCE_TEXTURE_HEIGHT);
  // Distance to top atmosphere boundary for a horizontal ray at ground level.
  Length H = sqrt(top_radius * top_radius - bottom_radius * bottom_radius);
  // Distance to the horizon, from which we can compute r:
  Length rho = H * x_r;
  r = sqrt(rho * rho + bottom_radius * bottom_radius);
  // Distance to the top atmosphere boundary for the ray (r,mu), and its minimum
  // and maximum values over all mu - obtained for (r,1) and (r,mu_horizon) -
  // from which we can recover mu:
  Length d_min = top_radius - r;
  Length d_max = rho + H;
  Length d = d_min + x_mu * (d_max - d_min);
  mu = d == 0.0 * m ? Number(1.0) : (H * H - rho * rho - d * d) / (2.0 * r * d);
  mu = ClampCosine(mu);
}

/*
 * It is now easy to define a fragment shader function to precompute a texel of
 * the transmittance texture:
*/

DimensionlessSpectrum ComputeTransmittanceToTopAtmosphereBoundaryTexture(float2 gl_frag_coord) 
{
  const float2 TRANSMITTANCE_TEXTURE_SIZE = float2(TRANSMITTANCE_TEXTURE_WIDTH, TRANSMITTANCE_TEXTURE_HEIGHT);
  Length r;
  Number mu;
  GetRMuFromTransmittanceTextureUv(gl_frag_coord / TRANSMITTANCE_TEXTURE_SIZE, r, mu);
  return ComputeTransmittanceToTopAtmosphereBoundary(r, mu);
}

/*
 * Lookup
 *
 * With the help of the above precomputed texture, we can now get the
 * transmittance between a point and the top atmosphere boundary with a single
 * texture lookup (assuming there is no intersection with the ground):
 */

DimensionlessSpectrum GetTransmittanceToTopAtmosphereBoundary(TransmittanceTexture transmittance_texture, Length r, Number mu) 
{
  float2 uv = GetTransmittanceTextureUvFromRMu(r, mu);
  return TEX2D(transmittance_texture, uv).rgb;
}

/*
 * Also, with $r_d=\Vert\bo\bq\Vert=\sqrt{d^2+2r\mu d+r^2}$ and $\mu_d=
 * \bo\bq\cdot\bp\bi/\Vert\bo\bq\Vert\Vert\bp\bi\Vert=(r\mu+d)/r_d$ the values of
 * r and mu at q, we can get the transmittance between two arbitrary
 * points p and q inside the atmosphere with only two texture lookups
 * (recall that the transmittance between p and q is the transmittance
 * between p and the top atmosphere boundary, divided by the transmittance
 * between q and the top atmosphere boundary, or the reverse - we continue to
 * assume that the segment between the two points does not intersect the ground):
 */

DimensionlessSpectrum GetTransmittance(TransmittanceTexture transmittance_texture, Length r, Number mu, Length d, bool ray_r_mu_intersects_ground) 
{

  Length r_d = ClampRadius(sqrt(d * d + 2.0 * r * mu * d + r * r));
  Number mu_d = ClampCosine((r * mu + d) / r_d);

  if (ray_r_mu_intersects_ground) 
  {
    return min(
        GetTransmittanceToTopAtmosphereBoundary(transmittance_texture, r_d, -mu_d) / 
		GetTransmittanceToTopAtmosphereBoundary(transmittance_texture, r, -mu), 
		DimensionlessSpectrum(1,1,1));
  } 
  else 
  {
    return min(
        GetTransmittanceToTopAtmosphereBoundary(transmittance_texture, r, mu) /
        GetTransmittanceToTopAtmosphereBoundary( transmittance_texture, r_d, mu_d),
        DimensionlessSpectrum(1,1,1));
  }
}

/*
 * where ray_r_mu_intersects_ground should be true if the ray
 * defined by r and mu intersects the ground. We don't compute it here with
 * RayIntersectsGround because the result could be wrong for rays
 * very close to the horizon, due to the finite precision and rounding errors of
 * floating point operations. And also because the caller generally has more robust
 * ways to know whether a ray intersects the ground or not (see below).
 *
 * Finally, we will also need the transmittance between a point in the
 * atmosphere and the Sun. The Sun is not a point light source, so this is an
 * integral of the transmittance over the Sun disc. Here we consider that the
 * transmittance is constant over this disc, except below the horizon, where the
 * transmittance is 0. As a consequence, the transmittance to the Sun can be
 * computed with GetTransmittanceToTopAtmosphereBoundary, times the
 * fraction of the Sun disc which is above the horizon.
 * 
 * This fraction varies from 0 when the Sun zenith angle theta_s is larger
 * than the horizon zenith angle theta_h plus the Sun angular radius alpha_s,
 * to 1 when theta_s is smaller than theta_h-\alpha_s. Equivalently, it
 * varies from 0 when $\mu_s=\cos\theta_s$ is smaller than
 * $\cos(\theta_h+\alpha_s)\approx\cos\theta_h-\alpha_s\sin\theta_h$ to 1 when
 * $\mu_s$ is larger than
 * $\cos(\theta_h-\alpha_s)\approx\cos\theta_h+\alpha_s\sin\theta_h$. In between,
 * the visible Sun disc fraction varies approximately like a smoothstep (this can
 * be verified by plotting the area of <a
 * href="https://en.wikipedia.org/wiki/Circular_segment">circular segment</a> as a
 * function of its <a href="https://en.wikipedia.org/wiki/Sagitta_(geometry)"
 * >sagitta</a>). Therefore, since $\sin\theta_h=r_{\mathrm{bottom}}/r$, we can
 * approximate the transmittance to the Sun with the following function:
 */

DimensionlessSpectrum GetTransmittanceToSun(TransmittanceTexture transmittance_texture, Length r, Number mu_s) 
{
  Number sin_theta_h = bottom_radius / r;
  Number cos_theta_h = -sqrt(max(1.0 - sin_theta_h * sin_theta_h, 0.0));

  Number step = smoothstep(-sin_theta_h * sun_angular_radius / rad, sin_theta_h * sun_angular_radius / rad, mu_s - cos_theta_h);

  return GetTransmittanceToTopAtmosphereBoundary(transmittance_texture, r, mu_s) * step;
      
}














