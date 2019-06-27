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
  * Rendering
  *
  * Here we assume that the transmittance, scattering and irradiance textures
  * have been precomputed, and we provide functions using them to compute the sky
  * color, the aerial perspective, and the ground radiance.
  *
  * More precisely, we assume that the single Rayleigh scattering, without its
  * phase function term, plus the multiple scattering terms (divided by the Rayleigh
  * phase function for dimensional homogeneity) are stored in a
  * scattering_texture. We also assume that the single Mie scattering
  * is stored, without its phase function term:
  *
  * either separately, in a single_mie_scattering_texture (this
  * option was not provided our <a href=
  * "http://evasion.inrialpes.fr/~Eric.Bruneton/PrecomputedAtmosphericScattering2.zip"
  * >original implementation</a>),
  * or, if the COMBINED_SCATTERING_TEXTURES preprocessor
  * macro is defined, in the scattering_texture. In this case, which is
  * only available with a GLSL compiler, Rayleigh and multiple scattering are stored
  * in the RGB channels, and the red component of the single Mie scattering is
  * stored in the alpha channel).
  *
  * In the second case, the green and blue components of the single Mie
  * scattering are extrapolated as described in our
  * <a href="https://hal.inria.fr/inria-00288758/en">paper</a>, with the following
  * function:
  */

float3 GetExtrapolatedSingleMieScattering(float4 scattering)
{
	if (scattering.r == 0.0)
		return float3(0, 0, 0);

	return scattering.rgb * scattering.a / scattering.r *
		(rayleigh_scattering.r / mie_scattering.r) *
		(mie_scattering / rayleigh_scattering);
}

/*
 * We can then retrieve all the scattering components (Rayleigh + multiple
 * scattering on one side, and single Mie scattering on the other side) with the
 * following function, based on GetScattering (we duplicate
 * some code here, instead of using two calls to GetScattering, to
 * make sure that the texture coordinates computation is shared between the lookups
 * in scattering_texture and single_mie_scattering_texture):
 */

IrradianceSpectrum GetCombinedScattering(
	ReducedScatteringTexture scattering_texture,
	ReducedScatteringTexture single_mie_scattering_texture,
	Length r, Number mu, Number mu_s, Number nu,
	bool ray_r_mu_intersects_ground,
	out IrradianceSpectrum single_mie_scattering)
{
	float4 uvwz = GetScatteringTextureUvwzFromRMuMuSNu(r, mu, mu_s, nu, ray_r_mu_intersects_ground);

	Number tex_coord_x = uvwz.x * Number(SCATTERING_TEXTURE_NU_SIZE - 1.0);
	Number tex_x = floor(tex_coord_x);
	Number lerp = tex_coord_x - tex_x;

	float3 uvw0 = float3((tex_x + uvwz.y) / Number(SCATTERING_TEXTURE_NU_SIZE), uvwz.z, uvwz.w);
	float3 uvw1 = float3((tex_x + 1.0 + uvwz.y) / Number(SCATTERING_TEXTURE_NU_SIZE), uvwz.z, uvwz.w);

	float4 combined_scattering = TEX3D(scattering_texture, uvw0) * (1.0 - lerp) + TEX3D(scattering_texture, uvw1) * lerp;
	IrradianceSpectrum scattering = combined_scattering;
	single_mie_scattering = GetExtrapolatedSingleMieScattering(combined_scattering);

	return scattering;
}

/*
 * Rendering Sky
 *
 * To render the sky we simply need to display the sky radiance, which we can
 * get with a lookup in the precomputed scattering texture(s), multiplied by the
 * phase function terms that were omitted during precomputation. We can also return
 * the transmittance of the atmosphere (which we can get with a single lookup in
 * the precomputed transmittance texture), which is needed to correctly render the
 * objects in space (such as the Sun and the Moon). This leads to the following
 * function, where most of the computations are used to correctly handle the case
 * of viewers outside the atmosphere, and the case of light shafts:
 */

RadianceSpectrum GetSkyRadiance(
	TransmittanceTexture transmittance_texture,
	ReducedScatteringTexture scattering_texture,
	ReducedScatteringTexture single_mie_scattering_texture,
	Position camera, Direction view_ray, Length shadow_length,
	Direction sun_direction, out DimensionlessSpectrum transmittance)
{
	// Compute the distance to the top atmosphere boundary along the view ray,
	// assuming the viewer is in space (or NaN if the view ray does not intersect
	// the atmosphere).
	Length r = length(camera);
	Length rmu = dot(camera, view_ray);
	Length distance_to_top_atmosphere_boundary = -rmu - sqrt(rmu * rmu - r * r + top_radius * top_radius);

	// If the viewer is in space and the view ray intersects the atmosphere, move
	// the viewer to the top atmosphere boundary (along the view ray):
	if (distance_to_top_atmosphere_boundary > 0.0 * m)
	{
		camera = camera + view_ray * distance_to_top_atmosphere_boundary;
		r = top_radius;
		rmu += distance_to_top_atmosphere_boundary;
	}
	else if (r > top_radius)
	{
		// If the view ray does not intersect the atmosphere, simply return 0.
		transmittance = DimensionlessSpectrum(1, 1, 1);
		return RadianceSpectrum(0, 0, 0);
	}

	// Compute the r, mu, mu_s and nu parameters needed for the texture lookups.
	Number mu = rmu / r;
	Number mu_s = dot(camera, sun_direction) / r;
	Number nu = dot(view_ray, sun_direction);
	bool ray_r_mu_intersects_ground = RayIntersectsGround(r, mu);

	transmittance = ray_r_mu_intersects_ground ? DimensionlessSpectrum(0, 0, 0) :
		GetTransmittanceToTopAtmosphereBoundary(transmittance_texture, r, mu);

	IrradianceSpectrum single_mie_scattering;
	IrradianceSpectrum scattering;

	if (shadow_length == 0.0 * m)
	{
		scattering = GetCombinedScattering(
			scattering_texture, single_mie_scattering_texture,
			r, mu, mu_s, nu, ray_r_mu_intersects_ground,
			single_mie_scattering);
	}
	else
	{
		// Case of light shafts (shadow_length is the total length noted l in our
		// paper): we omit the scattering between the camera and the point at
		// distance l, by implementing Eq. (18) of the paper (shadow_transmittance
		// is the T(x,x_s) term, scattering is the S|x_s=x+lv term).
		Length d = shadow_length;
		Length r_p = ClampRadius(sqrt(d * d + 2.0 * r * mu * d + r * r));

		Number mu_p = (r * mu + d) / r_p;
		Number mu_s_p = (r * mu_s + d * nu) / r_p;

		scattering = GetCombinedScattering(
			scattering_texture, single_mie_scattering_texture,
			r_p, mu_p, mu_s_p, nu, ray_r_mu_intersects_ground,
			single_mie_scattering);

		DimensionlessSpectrum shadow_transmittance =
			GetTransmittance(transmittance_texture,
				r, mu, shadow_length, ray_r_mu_intersects_ground);

		scattering = scattering * shadow_transmittance;
		single_mie_scattering = single_mie_scattering * shadow_transmittance;
	}

	return scattering * RayleighPhaseFunction(nu) + single_mie_scattering *
		MiePhaseFunction(mie_phase_function_g, nu);
}

/*
 * Rendering Aerial perspective
 *
 * To render the aerial perspective we need the transmittance and the scattering
 * between two points (i.e. between the viewer and a point on the ground, which can
 * at an arbibrary altitude). We already have a function to compute the
 * transmittance between two points (using 2 lookups in a texture which only
 * contains the transmittance to the top of the atmosphere), but we don't have one
 * for the scattering between 2 points. Hopefully, the scattering between 2 points
 * can be computed from two lookups in a texture which contains the scattering to
 * the nearest atmosphere boundary, as for the transmittance (except that here the
 * two lookup results must be subtracted, instead of divided). This is what we
 * implement in the following function (the initial computations are used to
 * correctly handle the case of viewers outside the atmosphere):
 */

RadianceSpectrum GetSkyRadianceToPoint(
	TransmittanceTexture transmittance_texture,
	ReducedScatteringTexture scattering_texture,
	ReducedScatteringTexture single_mie_scattering_texture,
	Position camera, Position pos, Length shadow_length,
	Direction sun_direction, out DimensionlessSpectrum transmittance)
{
	// Compute the distance to the top atmosphere boundary along the view ray,
	// assuming the viewer is in space (or NaN if the view ray does not intersect
	// the atmosphere).
	Direction view_ray = normalize(pos - camera);
	Length r = length(camera);
	Length rmu = dot(camera, view_ray);
	Length distance_to_top_atmosphere_boundary = -rmu - sqrt(rmu * rmu - r * r + top_radius * top_radius);

	// If the viewer is in space and the view ray intersects the atmosphere, move
	// the viewer to the top atmosphere boundary (along the view ray):
	if (distance_to_top_atmosphere_boundary > 0.0 * m)
	{
		camera = camera + view_ray * distance_to_top_atmosphere_boundary;
		r = top_radius;
		rmu += distance_to_top_atmosphere_boundary;
	}

	// Compute the r, mu, mu_s and nu parameters for the first texture lookup.
	Number mu = rmu / r;
	Number mu_s = dot(camera, sun_direction) / r;
	Number nu = dot(view_ray, sun_direction);
	Length d = length(pos - camera);
	bool ray_r_mu_intersects_ground = RayIntersectsGround(r, mu);

	transmittance = GetTransmittance(transmittance_texture,
		r, mu, d, ray_r_mu_intersects_ground);

	IrradianceSpectrum single_mie_scattering;
	IrradianceSpectrum scattering = GetCombinedScattering(
		scattering_texture, single_mie_scattering_texture,
		r, mu, mu_s, nu, ray_r_mu_intersects_ground,
		single_mie_scattering);

	// Compute the r, mu, mu_s and nu parameters for the second texture lookup.
	// If shadow_length is not 0 (case of light shafts), we want to ignore the
	// scattering along the last shadow_length meters of the view ray, which we
	// do by subtracting shadow_length from d (this way scattering_p is equal to
	// the S|x_s=x_0-lv term in Eq. (17) of our paper).
	d = max(d - shadow_length, 0.0 * m);
	Length r_p = ClampRadius(sqrt(d * d + 2.0 * r * mu * d + r * r));
	Number mu_p = (r * mu + d) / r_p;
	Number mu_s_p = (r * mu_s + d * nu) / r_p;

	IrradianceSpectrum single_mie_scattering_p;
	IrradianceSpectrum scattering_p = GetCombinedScattering(
		scattering_texture, single_mie_scattering_texture,
		r_p, mu_p, mu_s_p, nu, ray_r_mu_intersects_ground,
		single_mie_scattering_p);

	// Combine the lookup results to get the scattering between camera and point.
	DimensionlessSpectrum shadow_transmittance = transmittance;
	if (shadow_length > 0.0 * m)
	{
		// This is the T(x,x_s) term in Eq. (17) of our paper, for light shafts.
		shadow_transmittance = GetTransmittance(transmittance_texture, r, mu, d, ray_r_mu_intersects_ground);
	}

	scattering = scattering - shadow_transmittance * scattering_p;
	single_mie_scattering = single_mie_scattering - shadow_transmittance * single_mie_scattering_p;

	single_mie_scattering = GetExtrapolatedSingleMieScattering(
		float4(scattering, single_mie_scattering.r));

	// Hack to avoid rendering artifacts when the sun is below the horizon.
	single_mie_scattering = single_mie_scattering *
		smoothstep(Number(0), Number(0.01), mu_s);

	return scattering * RayleighPhaseFunction(nu) + single_mie_scattering *
		MiePhaseFunction(mie_phase_function_g, nu);
}

/*
 * Rendering Ground
 *
 * To render the ground we need the irradiance received on the ground after 0 or
 * more bounce(s) in the atmosphere or on the ground. The direct irradiance can be
 * computed with a lookup in the transmittance texture,
 * via GetTransmittanceToSun, while the indirect irradiance is given
 * by a lookup in the precomputed irradiance texture (this texture only contains
 * the irradiance for horizontal surfaces; we use the approximation defined in our
 * <a href="https://hal.inria.fr/inria-00288758/en">paper</a> for the other cases).
 * The function below returns the direct and indirect irradiances separately:
 */

IrradianceSpectrum GetSunAndSkyIrradiance(
	TransmittanceTexture transmittance_texture,
	IrradianceTexture irradiance_texture,
	Position pos, Direction normal, Direction sun_direction,
	out IrradianceSpectrum sky_irradiance)
{
	Length r = length(pos);
	Number mu_s = dot(pos, sun_direction) / r;

	// Indirect irradiance (approximated if the surface is not horizontal).
	sky_irradiance = GetIrradiance(irradiance_texture, r, mu_s) * (1.0 + dot(normal, pos) / r) * 0.5;

	// Direct irradiance.
	return solar_irradiance * GetTransmittanceToSun(transmittance_texture, r, mu_s) * max(dot(normal, sun_direction), 0.0);
}












