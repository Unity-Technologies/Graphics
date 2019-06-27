Shader "Hidden/RenderSky"
{
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
	*/
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest LEqual

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile __ RADIANCE_API_ENABLED
			#pragma multi_compile __ COMBINED_SCATTERING_TEXTURES
			
			#include "UnityCG.cginc"
			#include "Definitions.cginc"
			#include "UtilityFunctions.cginc"
			#include "TransmittanceFunctions.cginc"
			#include "ScatteringFunctions.cginc"
			#include "IrradianceFunctions.cginc"
			#include "RenderingFunctions.cginc"

			static const float3 kSphereCenter = float3(0.0, 1.0, 0.0);
			static const float kSphereRadius = 1.0;
			static const float3 kSphereAlbedo = float3(0.8, 0.8, 0.8);
			static const float3 kGroundAlbedo = float3(0.0, 0.0, 0.04);

			float sky_exposure;
			float3 white_point;
			float3 earth_center;
			float3 sun_direction;
			float2 sun_size;
	        float sun_edge;

			float4x4 frustumCorners;

			sampler2D transmittance_texture;
			sampler2D irradiance_texture;
			sampler3D scattering_texture;
			sampler3D single_mie_scattering_texture;

			struct appdata
			{
				uint vid : SV_VertexID;
			};

			struct v2f
			{
				float3 view_ray : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;

				float4 vertex;
				if (v.vid == 0 || v.vid == 3)
				{
	                vertex = float4(-1.0f, -1.0f, 3.0f, 1.0f); // BL
				}
				else if (v.vid == 1)
				{
	                vertex = float4(1.0f, -1.0f, 2.0f, 1.0f); // BR
				}
				else if (v.vid == 2 || v.vid == 4)
				{
	                vertex = float4(1.0f, 1.0f, 1.0f, 1.0f); // TR
				}
				else
				{
	                vertex = float4(-1.0f, 1.0f, 0.0f, 1.0f); // TL
				}

				int index = vertex.z;
				#if defined(UNITY_REVERSED_Z)
				vertex.z = 0.0; // zfar plane
				#else
				vertex.z = 1.0; // zfar plane
				#endif

				o.vertex = vertex;

				#if UNITY_UV_STARTS_AT_TOP
				o.vertex.y *= -1;
				#endif

				o.view_ray = frustumCorners[index].xyz;

				return o;
			}

			/*
			The functions to compute shadows and light shafts must be defined before we
			can use them in the main shader function, so we define them first. Testing if
			a point is in the shadow of the sphere S is equivalent to test if the
			corresponding light ray intersects the sphere, which is very simple to do.
			However, this is only valid for a punctual light source, which is not the case
			of the Sun. In the following function we compute an approximate (and biased)
			soft shadow by taking the angular size of the Sun into account:
			*/
			float GetSunVisibility(float3 _point, float3 sun_direction)
			{
				float3 p = _point - kSphereCenter;
				float p_dot_v = dot(p, sun_direction);
				float p_dot_p = dot(p, p);
				float ray_sphere_center_squared_distance = p_dot_p - p_dot_v * p_dot_v;
				float distance_to_intersection = -p_dot_v - sqrt(max(0.0, kSphereRadius * kSphereRadius - ray_sphere_center_squared_distance));

				if (distance_to_intersection > 0.0) 
				{
					// Compute the distance between the view ray and the sphere, and the
					// corresponding (tangent of the) subtended angle. Finally, use this to
					// compute an approximate sun visibility.
					float ray_sphere_distance = kSphereRadius - sqrt(ray_sphere_center_squared_distance);
					float ray_sphere_angular_distance = -ray_sphere_distance / p_dot_v;

					return smoothstep(1.0, 0.0, ray_sphere_angular_distance / sun_size.x);
				}

				return 1.0;
			}

			/*
			The sphere also partially occludes the sky light, and we approximate this
			effect with an ambient occlusion factor. The ambient occlusion factor due to a
			sphere is given in <a href=
			"http://webserver.dmt.upm.es/~isidoro/tc3/Radiation%20View%20factors.pdf"
			>Radiation View Factors</a> (Isidoro Martinez, 1995). In the simple case where
			the sphere is fully visible, it is given by the following function:
			*/
			float GetSkyVisibility(float3 _point) 
			{
				float3 p = _point - kSphereCenter;
				float p_dot_p = dot(p, p);
				return 1.0 + p.y / sqrt(p_dot_p) * kSphereRadius * kSphereRadius / p_dot_p;
			}

			/*
			To compute light shafts we need the intersections of the view ray with the
			shadow volume of the sphere S. Since the Sun is not a punctual light source this
			shadow volume is not a cylinder but a cone (for the umbra, plus another cone for
			the penumbra, but we ignore it here):
			*/
			void GetSphereShadowInOut(float3 view_direction, float3 sun_direction, out float d_in, out float d_out)
			{
				float3 camera = _WorldSpaceCameraPos;
				float3 pos = camera - kSphereCenter;
				float pos_dot_sun = dot(pos, sun_direction);
				float view_dot_sun = dot(view_direction, sun_direction);
				float k = sun_size.x;
				float l = 1.0 + k * k;
				float a = 1.0 - l * view_dot_sun * view_dot_sun;
				float b = dot(pos, view_direction) - l * pos_dot_sun * view_dot_sun -
					k * kSphereRadius * view_dot_sun;
				float c = dot(pos, pos) - l * pos_dot_sun * pos_dot_sun -
					2.0 * k * kSphereRadius * pos_dot_sun - kSphereRadius * kSphereRadius;
				float discriminant = b * b - a * c;
				if (discriminant > 0.0) 
				{
					d_in = max(0.0, (-b - sqrt(discriminant)) / a);
					d_out = (-b + sqrt(discriminant)) / a;
					// The values of d for which delta is equal to 0 and kSphereRadius / k.
					float d_base = -pos_dot_sun / view_dot_sun;
					float d_apex = -(pos_dot_sun + kSphereRadius / k) / view_dot_sun;

					if (view_dot_sun > 0.0) 
					{
						d_in = max(d_in, d_apex);
						d_out = a > 0.0 ? min(d_out, d_base) : d_base;
					}
					else 
					{
						d_in = a > 0.0 ? max(d_in, d_base) : d_base;
						d_out = min(d_out, d_apex);
					}
				}
				else 
				{
					d_in = 0.0;
					d_out = 0.0;
				}
			}

#ifdef RADIANCE_API_ENABLED
			RadianceSpectrum GetSolarRadiance() 
			{
				return solar_irradiance / (B_PI * sun_angular_radius * sun_angular_radius);
			}

			RadianceSpectrum GetSkyRadiance(
				Position camera, Direction view_ray, Length shadow_length,
				Direction sun_direction, out DimensionlessSpectrum transmittance) 
			{
				return GetSkyRadiance(transmittance_texture,
					scattering_texture, single_mie_scattering_texture,
					camera, view_ray, shadow_length, sun_direction, transmittance);
			}

			RadianceSpectrum GetSkyRadianceToPoint(
				Position camera, Position _point, Length shadow_length,
				Direction sun_direction, out DimensionlessSpectrum transmittance) 
			{
				return GetSkyRadianceToPoint(transmittance_texture,
					scattering_texture, single_mie_scattering_texture,
					camera, _point, shadow_length, sun_direction, transmittance);
			}

			IrradianceSpectrum GetSunAndSkyIrradiance(
				Position p, Direction normal, Direction sun_direction,
				out IrradianceSpectrum sky_irradiance) 
			{
				return GetSunAndSkyIrradiance(transmittance_texture,
					irradiance_texture, p, normal, sun_direction, sky_irradiance);
			}
#else
			Luminance3 GetSolarRadiance()
			{
				return solar_irradiance /
					(B_PI * sun_angular_radius * sun_angular_radius) *
					SUN_SPECTRAL_RADIANCE_TO_LUMINANCE;
			}

			Luminance3 GetSkyRadiance(
				Position camera, Direction view_ray, Length shadow_length,
				Direction sun_direction, out DimensionlessSpectrum transmittance) 
			{
				return GetSkyRadiance(transmittance_texture,
					scattering_texture, single_mie_scattering_texture,
					camera, view_ray, shadow_length, sun_direction, transmittance) *
					SKY_SPECTRAL_RADIANCE_TO_LUMINANCE;
			}

			Luminance3 GetSkyRadianceToPoint(
				Position camera, Position _point, Length shadow_length,
				Direction sun_direction, out DimensionlessSpectrum transmittance) 
			{
				return GetSkyRadianceToPoint(transmittance_texture,
					scattering_texture, single_mie_scattering_texture,
					camera, _point, shadow_length, sun_direction, transmittance) *
					SKY_SPECTRAL_RADIANCE_TO_LUMINANCE;
			}

			Illuminance3 GetSunAndSkyIrradiance(
				Position p, Direction normal, Direction sun_direction,
				out IrradianceSpectrum sky_irradiance) 
			{
				IrradianceSpectrum sun_irradiance = GetSunAndSkyIrradiance(
					transmittance_texture, irradiance_texture, p, normal,
					sun_direction, sky_irradiance);
				sky_irradiance *= SKY_SPECTRAL_RADIANCE_TO_LUMINANCE;
				return sun_irradiance * SUN_SPECTRAL_RADIANCE_TO_LUMINANCE;
			}
#endif
			
	        half4 frag (v2f IN) : SV_Target
	        {
	            // Todo: Calculate the real shadow length
	            const float shadow_length = 0.f;

	            half3 col = half3(0.0, 0.0, 0.0);

				float3 camera = _WorldSpaceCameraPos;
				// Normalized view direction vector.
				float3 view_direction = normalize(IN.view_ray);

	            // Hack to fade out light shafts when the Sun is very close to the horizon.
	            float lightshaft_fadein_hack = smoothstep(0.02, 0.04, dot(normalize(camera - earth_center), sun_direction));

	            // Compute the distance between the view ray line and the sphere center,
	            // and the distance between the camera and the intersection of the view
	            // ray with the sphere (or NaN if there is no intersection).
	            float3 p = camera - kSphereCenter;
	            float p_dot_v = dot(p, view_direction);
	            float p_dot_p = dot(p, p);
	            float ray_sphere_center_squared_distance = p_dot_p - p_dot_v * p_dot_v;
	            float distance_to_intersection = -p_dot_v - sqrt(kSphereRadius * kSphereRadius - ray_sphere_center_squared_distance);

	            // Compute the radiance reflected by the sphere, if the ray intersects it.
	            float sphere_alpha = 0.0;
	            float3 sphere_radiance = float3(0,0,0);

	            p = camera - earth_center;
	            p_dot_v = dot(p, view_direction);
	            p_dot_p = dot(p, p);
	            float ray_earth_center_squared_distance = p_dot_p - p_dot_v * p_dot_v;
	            distance_to_intersection = -p_dot_v - sqrt(earth_center.y * earth_center.y - ray_earth_center_squared_distance);

	            // Compute the radiance reflected by the ground, if the ray intersects it.
	            float ground_alpha = 0.0;
	            float3 ground_radiance = float3(0,0,0);
	            if (distance_to_intersection > 0.0) 
	            {
	                float3 _point = camera + view_direction * distance_to_intersection;
	                float3 normal = normalize(_point - earth_center);

	                // Compute the radiance reflected by the ground.
	                float3 sky_irradiance;
	                float3 sun_irradiance = GetSunAndSkyIrradiance(_point - earth_center, normal, sun_direction, sky_irradiance);

	                ground_radiance = kGroundAlbedo * (1.0 / B_PI) * (sun_irradiance + sky_irradiance);

	                float3 transmittance;
	                float3 in_scatter = GetSkyRadianceToPoint(camera - earth_center, _point - earth_center, shadow_length, sun_direction, transmittance);

	                ground_radiance = ground_radiance * transmittance + in_scatter;
	                ground_alpha = 1.0;
	            }

	            float3 transmittance;
	            float3 radiance = GetSkyRadiance(camera - earth_center, view_direction, shadow_length, sun_direction, transmittance);

	            float renormalized_angle_to_sun = dot(view_direction, sun_direction) - sun_size.y;
	            float sun_gradient = (renormalized_angle_to_sun * sun_edge) / sun_size.x;
        		radiance += transmittance * GetSolarRadiance() * saturate(sun_gradient) * 1000.0; 

	            radiance = lerp(radiance, ground_radiance, ground_alpha);
	            radiance = lerp(radiance, sphere_radiance, sphere_alpha);

	            radiance = pow(float3(1,1,1) - exp(-radiance / white_point * sky_exposure), 1.0 / 2.2);

	            col.rgb = radiance;

	            return float4(col,1);
	        }

			ENDCG
		}
	}
}
