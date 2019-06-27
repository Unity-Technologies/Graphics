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
 * Ground irradiance
 * 
 * The ground irradiance is the Sun light received on the ground after n \ge 0
 * bounces (where a bounce is either a scattering event or a reflection on the
 * ground). We need this for two purposes:
 * 
 * while precomputing the n-th order of scattering, with n \ge 2, in order
 * to compute the contribution of light paths whose (n-1)-th bounce is on the
 * ground (which requires the ground irradiance after n-2 bounces - see the Multiple
 * scattering section),
 * at rendering time, to compute the contribution of light paths whose last
 * bounce is on the ground (these paths are excluded, by definition, from our
 * precomputed scattering textures)
 * 
 * In the first case we only need the ground irradiance for horizontal surfaces
 * at the bottom of the atmosphere (during precomputations we assume a perfectly
 * spherical ground with a uniform albedo). In the second case, however, we need
 * the ground irradiance for any altitude and any surface normal, and we want to
 * precompute it for efficiency. In fact, as described in our
 * <a href="https://hal.inria.fr/inria-00288758/en">paper</a> we precompute it only
 * for horizontal surfaces, at any altitude (which requires only 2D textures,
 * instead of 4D textures for the general case), and we use approximations for
 * non-horizontal surfaces.
 * 
 * The following sections describe how we compute the ground irradiance, how we
 * store it in a precomputed texture, and how we read it back.
 * 
 * Computation
 * 
 * The ground irradiance computation is different for the direct irradiance,
 * i.e. the light received directly from the Sun, without any intermediate bounce,
 * and for the indirect irradiance (at least one bounce). We start here with the
 * direct irradiance.
 * 
 * The irradiance is the integral over an hemisphere of the incident radiance,
 * times a cosine factor. For the direct ground irradiance, the incident radiance
 * is the Sun radiance at the top of the atmosphere, times the transmittance
 * through the atmosphere. And, since the Sun solid angle is small, we can
 * approximate the transmittance with a constant, i.e. we can move it outside the
 * irradiance integral, which can be performed over (the visible fraction of) the
 * Sun disc rather than the hemisphere. Then the integral becomes equivalent to the
 * ambient occlusion due to a sphere, also called a view factor, which is given in
 * <a href="http://webserver.dmt.upm.es/~isidoro/tc3/Radiation%20View%20factors.pdf
 * ">Radiative view factors</a> (page 10). For a small solid angle, these complex
 * equations can be simplified as follows:
 */

IrradianceSpectrum ComputeDirectIrradiance(
    TransmittanceTexture transmittance_texture,
    Length r, Number mu_s) 
{

  Number alpha_s = sun_angular_radius / rad;

  // Approximate average of the cosine factor mu_s over the visible fraction of
  // the Sun disc.
  Number average_cosine_factor =
    mu_s < -alpha_s ? 0.0 : (mu_s > alpha_s ? mu_s :
        (mu_s + alpha_s) * (mu_s + alpha_s) / (4.0 * alpha_s));

  return solar_irradiance * GetTransmittanceToTopAtmosphereBoundary(transmittance_texture, r, mu_s) * average_cosine_factor;

}


/*
 * For the indirect ground irradiance the integral over the hemisphere must be
 * computed numerically. More precisely we need to compute the integral over all
 * the directions w of the hemisphere, of the product of:
 * 
 * the radiance arriving from direction w after n bounces,
 * the cosine factor, i.e. omega_z
 * 
 * This leads to the following implementation (where
 * multiple_scattering_texture is supposed to contain the n-th
 * order of scattering, if n>1, and scattering_order is equal to n):
 */

IrradianceSpectrum ComputeIndirectIrradiance(
    ReducedScatteringTexture single_rayleigh_scattering_texture,
    ReducedScatteringTexture single_mie_scattering_texture,
    ScatteringTexture multiple_scattering_texture,
    Length r, Number mu_s, int scattering_order) 
{

  const int SAMPLE_COUNT = 32;
  const Angle dphi = pi / Number(SAMPLE_COUNT);
  const Angle dtheta = pi / Number(SAMPLE_COUNT);

  IrradianceSpectrum result = IrradianceSpectrum(0, 0, 0);

  float3 omega_s = float3(sqrt(1.0 - mu_s * mu_s), 0.0, mu_s);

  for (int j = 0; j < SAMPLE_COUNT / 2; ++j) 
  {
    Angle theta = (Number(j) + 0.5) * dtheta;

    for (int i = 0; i < 2 * SAMPLE_COUNT; ++i) 
    {
      Angle phi = (Number(i) + 0.5) * dphi;
      float3 omega = float3(cos(phi) * sin(theta), sin(phi) * sin(theta), cos(theta));
      SolidAngle domega = (dtheta / rad) * (dphi / rad) * sin(theta) * sr;
      Number nu = dot(omega, omega_s);

      result += GetScattering(single_rayleigh_scattering_texture,
                single_mie_scattering_texture, multiple_scattering_texture,
                r, omega.z, mu_s, nu, false,scattering_order) * omega.z * domega;
    }
  }

  return result;
}

/*
 * Irradiance Precomputation
 * 
 * In order to precompute the ground irradiance in a texture we need a mapping
 * from the ground irradiance parameters to texture coordinates. Since we
 * precompute the ground irradiance only for horizontal surfaces, this irradiance
 * depends only on r and mu_s, so we need a mapping from (r,mu_s) to
 * (u,v) texture coordinates. The simplest, affine mapping is sufficient here,
 * because the ground irradiance function is very smooth:
 */

float2 GetIrradianceTextureUvFromRMuS(Length r, Number mu_s) 
{

  Number x_r = (r - bottom_radius) / (top_radius - bottom_radius);
  Number x_mu_s = mu_s * 0.5 + 0.5;
  return float2(GetTextureCoordFromUnitRange(x_mu_s, IRRADIANCE_TEXTURE_WIDTH),
                GetTextureCoordFromUnitRange(x_r, IRRADIANCE_TEXTURE_HEIGHT));
}

/*
 * The inverse mapping follows immediately:
 */

void GetRMuSFromIrradianceTextureUv(float2 uv, out Length r, out Number mu_s) 
{

  Number x_mu_s = GetUnitRangeFromTextureCoord(uv.x, IRRADIANCE_TEXTURE_WIDTH);
  Number x_r = GetUnitRangeFromTextureCoord(uv.y, IRRADIANCE_TEXTURE_HEIGHT);
  r = bottom_radius + x_r * (top_radius - bottom_radius);
  mu_s = ClampCosine(2.0 * x_mu_s - 1.0);
}

/*
 * It is now easy to define a fragment shader function to precompute a texel of
 * the ground irradiance texture, for the direct irradiance:
 */

IrradianceSpectrum ComputeDirectIrradianceTexture(
    TransmittanceTexture transmittance_texture,
    float2 gl_frag_coord) 
{
  Length r;
  Number mu_s;

  const float2 IRRADIANCE_TEXTURE_SIZE = float2(IRRADIANCE_TEXTURE_WIDTH, IRRADIANCE_TEXTURE_HEIGHT);

  GetRMuSFromIrradianceTextureUv(gl_frag_coord / IRRADIANCE_TEXTURE_SIZE, r, mu_s);
  return ComputeDirectIrradiance(transmittance_texture, r, mu_s);
}

/*
 * and the indirect one:
 */

IrradianceSpectrum ComputeIndirectIrradianceTexture(
    ReducedScatteringTexture single_rayleigh_scattering_texture,
    ReducedScatteringTexture single_mie_scattering_texture,
    ScatteringTexture multiple_scattering_texture,
    float2 gl_frag_coord, int scattering_order) 
{
  Length r;
  Number mu_s;

  const float2 IRRADIANCE_TEXTURE_SIZE = float2(IRRADIANCE_TEXTURE_WIDTH, IRRADIANCE_TEXTURE_HEIGHT);

  GetRMuSFromIrradianceTextureUv(gl_frag_coord / IRRADIANCE_TEXTURE_SIZE, r, mu_s);

  return ComputeIndirectIrradiance(single_rayleigh_scattering_texture, single_mie_scattering_texture,
                                   multiple_scattering_texture, r, mu_s, scattering_order);
}

/*
 * Irradiance Lookup
 * 
 * Thanks to these precomputed textures, we can now get the ground irradiance
 * with a single texture lookup:
 */

IrradianceSpectrum GetIrradiance(
    IrradianceTexture irradiance_texture,
    Length r, Number mu_s) 
{
  float2 uv = GetIrradianceTextureUvFromRMuS(r, mu_s);
  return TEX2D(irradiance_texture, uv).rgb;
}










