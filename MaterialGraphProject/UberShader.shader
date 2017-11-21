Shader "hidden/preview"
{
	Properties
	{
				[NonModifiableTextureData] [NoScaleOffset] Texture_507A46B3("Texture", 2D) = "white" {}
				Float_D2D9489B("FresnelPower", Float) = 3
				[NonModifiableTextureData] [NoScaleOffset] Texture_E2350D28("BaseColor", 2D) = "white" {}
				[NonModifiableTextureData] [NoScaleOffset] _Texture2D_8897CB1D_Tex("411a2135-a1df-4dfb-93ea-9254bbdf2727", 2D) = "white" {}
	}
	CGINCLUDE
	#include "UnityCG.cginc"
			void Unity_RadialShear_float(float2 UV, float2 Center, float2 Strength, float2 Offset, out float2 Out)
			{
			    float2 delta = UV - Center;
			    float delta2 = dot(delta.xy, delta.xy);
			    float2 delta_offset = delta2 * Strength;
			    Out = UV + float2(delta.y, -delta.x) * delta_offset + Offset;
			}
			void Unity_Twirl_float(float2 UV, float2 Center, float Strength, float2 Offset, out float2 Out)
			{
			    float2 delta = UV - Center;
			    float angle = Strength * length(delta);
			    float x = cos(angle) * delta.x - sin(angle) * delta.y;
			    float y = sin(angle) * delta.x + cos(angle) * delta.y;
			    Out = float2(x + Center.x + Offset.x, y + Center.y + Offset.y);
			}
			void Unity_Spherize_float(float2 UV, float2 Center, float2 Strength, float2 Offset, out float2 Out)
			{
			    float2 delta = UV - Center;
			    float delta2 = dot(delta.xy, delta.xy);
			    float delta4 = delta2 * delta2;
			    float2 delta_offset = delta4 * Strength;
			    Out = UV + delta * delta_offset + Offset;
			}
			void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA)
			{
			    RGBA = float4(R, G, B, A);
			}
			void Unity_OneMinus_float(float In, out float Out)
			{
			    Out = 1 - In;
			}
			void Unity_Normalize_float(float3 In, out float3 Out)
			{
			    Out = normalize(In);
			}
			void Unity_Power_float(float A, float B, out float Out)
			{
			    Out = pow(A, B);
			}
			void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
			{
			    Out = A * B;
			}
			void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
			{
			    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
			}
			void Unity_DotProduct_float(float3 A, float3 B, out float Out)
			{
			    Out = dot(A, B);
			}
			void Unity_Multiply_float(float A, float B, out float Out)
			{
			    Out = A * B;
			}
			void Unity_Add_float(float A, float B, out float Out)
			{
			    Out = A + B;
			}
			void Unity_Multiply_float(float4 A, float4 B, out float4 Out)
			{
			    Out = A * B;
			}
			void Unity_HSVToRGB_float(float3 hsv, out float3 rgb)
			{
			    //Reference code from:https://github.com/Unity-Technologies/PostProcessing/blob/master/PostProcessing/Resources/Shaders/ColorGrading.cginc#L175
			    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
			    float3 P = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
			    rgb = hsv.z * lerp(K.xxx, saturate(P - K.xxx), hsv.y);
			}
			void Unity_RGBToLuminance_float(float3 rgb, out float luminance)
			{
			    luminance = dot(rgb, float3(0.2126729, 0.7151522, 0.0721750));
			}
			void Unity_Subtract_float(float A, float B, out float Out)
			{
			    Out = A - B;
			}
			void Unity_Add_float(float3 A, float3 B, out float3 Out)
			{
			    Out = A + B;
			}
			void Unity_Normalize_float(float2 In, out float2 Out)
			{
			    Out = normalize(In);
			}
	struct GraphVertexInput
	{
	     float4 vertex : POSITION;
	     float3 normal : NORMAL;
	     float4 tangent : TANGENT;
	     float4 color : COLOR;
	     float4 texcoord0 : TEXCOORD0;
	     float4 lightmapUV : TEXCOORD1;
	     UNITY_VERTEX_INPUT_INSTANCE_ID
	};
			struct SurfaceInputs{
				float3 WorldSpaceNormal;
				float3 WorldSpaceViewDirection;
				half4 uv0;
			};
			struct SurfaceDescription{
				float4 PreviewOutput;
			};
			float Float_AC7EF5E9;
			UNITY_DECLARE_TEX2D(Texture_507A46B3);
			float Float_D2D9489B;
			UNITY_DECLARE_TEX2D(Texture_E2350D28);
			float4 _RadialShear_72E659FB_UV;
			float4 _RadialShear_72E659FB_Center;
			float4 _RadialShear_72E659FB_Strength;
			float4 _RadialShear_72E659FB_Offset;
			float4 _Twirl_35851FBC_Center;
			float _Twirl_35851FBC_Strength;
			float4 _Twirl_35851FBC_Offset;
			float4 _Spherize_8852C787_Center;
			float4 _Spherize_8852C787_Strength;
			float4 _Spherize_8852C787_Offset;
			float _Combine_93029BDA_B;
			float _Combine_93029BDA_A;
			UNITY_DECLARE_TEX2D(_Texture2D_8897CB1D_Tex);
			float4 _Texture2D_8897CB1D_UV;
			float4 Vector3_DC3A5D1E;
			float4 _Texture2D_63B21599_UV;
			float _Power_8AAB91A9_B;
			float Vector1_91963E7E;
			float4 _Remap_49700E8_InMinMax;
			float4 _Remap_49700E8_OutMinMax;
			float4 _HeightToNormal_3EA45BA1_UV;
			float _HeightToNormal_3EA45BA1_Offset;
			float _HeightToNormal_3EA45BA1_Strength;
			float Vector1_FA7B64E2;
			float4 _Texture2D_CE7FDB22_UV;
			float _Add_955670F1_B;
			float _Combine_76C873_G;
			float _Combine_76C873_B;
			float _Combine_76C873_A;
			float4 _Multiply_F77E48F9_B;
			float Vector1_1A8B01A5;
			float _Subtract_4053AE6C_A;
			float _Combine_EBDC57C5_G;
			float _Combine_EBDC57C5_B;
			float _Combine_EBDC57C5_A;
			float _Multiply_77943E40_B;
			GraphVertexInput PopulateVertexData(GraphVertexInput v){
				return v;
			}
			SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
				SurfaceDescription surface = (SurfaceDescription)0;
				float3 WorldSpaceNormal = IN.WorldSpaceNormal;
				float3 WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
				half4 uv0 = IN.uv0;
				float2 _RadialShear_72E659FB_Out;
				Unity_RadialShear_float(uv0.xy, _RadialShear_72E659FB_Center, _RadialShear_72E659FB_Strength, _RadialShear_72E659FB_Offset, _RadialShear_72E659FB_Out);
				if (Float_AC7EF5E9 == 0) { surface.PreviewOutput = half4(_RadialShear_72E659FB_Out.x, _RadialShear_72E659FB_Out.y, 0.0, 1.0); return surface; }
				float2 _Twirl_35851FBC_Out;
				Unity_Twirl_float(_RadialShear_72E659FB_Out, _Twirl_35851FBC_Center, _Twirl_35851FBC_Strength, _Twirl_35851FBC_Offset, _Twirl_35851FBC_Out);
				if (Float_AC7EF5E9 == 1) { surface.PreviewOutput = half4(_Twirl_35851FBC_Out.x, _Twirl_35851FBC_Out.y, 0.0, 1.0); return surface; }
				float2 _Spherize_8852C787_Out;
				Unity_Spherize_float(_Twirl_35851FBC_Out, _Spherize_8852C787_Center, _Spherize_8852C787_Strength, _Spherize_8852C787_Offset, _Spherize_8852C787_Out);
				if (Float_AC7EF5E9 == 2) { surface.PreviewOutput = half4(_Spherize_8852C787_Out.x, _Spherize_8852C787_Out.y, 0.0, 1.0); return surface; }
				float _Split_1D533D55_R = _Spherize_8852C787_Out[0];
				float _Split_1D533D55_G = _Spherize_8852C787_Out[1];
				float _Split_1D533D55_B = 1.0;
				float _Split_1D533D55_A = 1.0;
				float4 _Combine_93029BDA_RGBA;
				Unity_Combine_float(_Split_1D533D55_R, _Split_1D533D55_G, _Combine_93029BDA_B, _Combine_93029BDA_A, _Combine_93029BDA_RGBA);
				if (Float_AC7EF5E9 == 3) { surface.PreviewOutput = half4(_Combine_93029BDA_RGBA.x, _Combine_93029BDA_RGBA.y, _Combine_93029BDA_RGBA.z, 1.0); return surface; }
				float4 _Texture2D_8897CB1D_RGBA = UNITY_SAMPLE_TEX2D(_Texture2D_8897CB1D_Tex,uv0.xy);
				float _Texture2D_8897CB1D_R = _Texture2D_8897CB1D_RGBA.r;
				float _Texture2D_8897CB1D_G = _Texture2D_8897CB1D_RGBA.g;
				float _Texture2D_8897CB1D_B = _Texture2D_8897CB1D_RGBA.b;
				float _Texture2D_8897CB1D_A = _Texture2D_8897CB1D_RGBA.a;
				if (Float_AC7EF5E9 == 4) { surface.PreviewOutput = half4(_Texture2D_8897CB1D_RGBA.x, _Texture2D_8897CB1D_RGBA.y, _Texture2D_8897CB1D_RGBA.z, 1.0); return surface; }
				float _OneMinus_CE09816_Out;
				Unity_OneMinus_float(_Texture2D_8897CB1D_R, _OneMinus_CE09816_Out);
				if (Float_AC7EF5E9 == 5) { surface.PreviewOutput = half4(_OneMinus_CE09816_Out, _OneMinus_CE09816_Out, _OneMinus_CE09816_Out, 1.0); return surface; }
				float3 _Normalize_AA754DA4_Out;
				Unity_Normalize_float(Vector3_DC3A5D1E, _Normalize_AA754DA4_Out);
				if (Float_AC7EF5E9 == 6) { surface.PreviewOutput = half4(_Normalize_AA754DA4_Out.x, _Normalize_AA754DA4_Out.y, _Normalize_AA754DA4_Out.z, 1.0); return surface; }
				float4 _Texture2D_63B21599_RGBA = UNITY_SAMPLE_TEX2D(Texture_507A46B3,uv0.xy);
				float _Texture2D_63B21599_R = _Texture2D_63B21599_RGBA.r;
				float _Texture2D_63B21599_G = _Texture2D_63B21599_RGBA.g;
				float _Texture2D_63B21599_B = _Texture2D_63B21599_RGBA.b;
				float _Texture2D_63B21599_A = _Texture2D_63B21599_RGBA.a;
				if (Float_AC7EF5E9 == 7) { surface.PreviewOutput = half4(_Texture2D_63B21599_RGBA.x, _Texture2D_63B21599_RGBA.y, _Texture2D_63B21599_RGBA.z, 1.0); return surface; }
				float _OneMinus_D38B6E7C_Out;
				Unity_OneMinus_float(_Texture2D_63B21599_R, _OneMinus_D38B6E7C_Out);
				if (Float_AC7EF5E9 == 8) { surface.PreviewOutput = half4(_OneMinus_D38B6E7C_Out, _OneMinus_D38B6E7C_Out, _OneMinus_D38B6E7C_Out, 1.0); return surface; }
				float _Power_8AAB91A9_Out;
				Unity_Power_float(_OneMinus_D38B6E7C_Out, _Power_8AAB91A9_B, _Power_8AAB91A9_Out);
				if (Float_AC7EF5E9 == 9) { surface.PreviewOutput = half4(_Power_8AAB91A9_Out, _Power_8AAB91A9_Out, _Power_8AAB91A9_Out, 1.0); return surface; }
				float3 _Multiply_731F83FC_Out;
				Unity_Multiply_float(_Normalize_AA754DA4_Out, (Vector1_91963E7E.xxx), _Multiply_731F83FC_Out);
				if (Float_AC7EF5E9 == 10) { surface.PreviewOutput = half4(_Multiply_731F83FC_Out.x, _Multiply_731F83FC_Out.y, _Multiply_731F83FC_Out.z, 1.0); return surface; }
				float _Remap_49700E8_Out;
				Unity_Remap_float(_SinTime.w, _Remap_49700E8_InMinMax, _Remap_49700E8_OutMinMax, _Remap_49700E8_Out);
				if (Float_AC7EF5E9 == 11) { surface.PreviewOutput = half4(_Remap_49700E8_Out, _Remap_49700E8_Out, _Remap_49700E8_Out, 1.0); return surface; }
				float3 _Multiply_D447D798_Out;
				Unity_Multiply_float(_Multiply_731F83FC_Out, (_Remap_49700E8_Out.xxx), _Multiply_D447D798_Out);
				if (Float_AC7EF5E9 == 12) { surface.PreviewOutput = half4(_Multiply_D447D798_Out.x, _Multiply_D447D798_Out.y, _Multiply_D447D798_Out.z, 1.0); return surface; }
				float3 _Multiply_3760727D_Out;
				Unity_Multiply_float((_Power_8AAB91A9_Out.xxx), _Multiply_D447D798_Out, _Multiply_3760727D_Out);
				if (Float_AC7EF5E9 == 13) { surface.PreviewOutput = half4(_Multiply_3760727D_Out.x, _Multiply_3760727D_Out.y, _Multiply_3760727D_Out.z, 1.0); return surface; }
				float3 _HeightToNormal_3EA45BA1_Normal;
				{
					float2 offsetU = float2(uv0.xy.x + _HeightToNormal_3EA45BA1_Offset, uv0.xy.y);
					float2 offsetV = float2(uv0.xy.x, uv0.xy.y + _HeightToNormal_3EA45BA1_Offset);
					float normalSample = UNITY_SAMPLE_TEX2D(Texture_507A46B3, uv0.xy);
					float uSample = UNITY_SAMPLE_TEX2D(Texture_507A46B3, offsetU);
					float vSample = UNITY_SAMPLE_TEX2D(Texture_507A46B3, offsetV);
					float3 va = float3(1, 0, (uSample - normalSample) * _HeightToNormal_3EA45BA1_Strength);
					float3 vb = float3(0, 1, (vSample - normalSample) * _HeightToNormal_3EA45BA1_Strength);
					_HeightToNormal_3EA45BA1_Normal = cross(va, vb);
				}
				if (Float_AC7EF5E9 == 14) { surface.PreviewOutput = half4(_HeightToNormal_3EA45BA1_Normal.x, _HeightToNormal_3EA45BA1_Normal.y, _HeightToNormal_3EA45BA1_Normal.z, 1.0); return surface; }
				if (Float_AC7EF5E9 == 15) { surface.PreviewOutput = half4(WorldSpaceViewDirection.x, WorldSpaceViewDirection.y, WorldSpaceViewDirection.z, 1.0); return surface; }
				if (Float_AC7EF5E9 == 16) { surface.PreviewOutput = half4(WorldSpaceNormal.x, WorldSpaceNormal.y, WorldSpaceNormal.z, 1.0); return surface; }
				float _DotProduct_4F24AFD1_Out;
				Unity_DotProduct_float(WorldSpaceViewDirection, WorldSpaceNormal, _DotProduct_4F24AFD1_Out);
				if (Float_AC7EF5E9 == 17) { surface.PreviewOutput = half4(_DotProduct_4F24AFD1_Out, _DotProduct_4F24AFD1_Out, _DotProduct_4F24AFD1_Out, 1.0); return surface; }
				float _OneMinus_DFCAFBB5_Out;
				Unity_OneMinus_float(_DotProduct_4F24AFD1_Out, _OneMinus_DFCAFBB5_Out);
				if (Float_AC7EF5E9 == 18) { surface.PreviewOutput = half4(_OneMinus_DFCAFBB5_Out, _OneMinus_DFCAFBB5_Out, _OneMinus_DFCAFBB5_Out, 1.0); return surface; }
				float _Property_DEDFE7DB_float = Float_D2D9489B;
				float _Power_BB12720D_Out;
				Unity_Power_float(_OneMinus_DFCAFBB5_Out, _Property_DEDFE7DB_float, _Power_BB12720D_Out);
				if (Float_AC7EF5E9 == 19) { surface.PreviewOutput = half4(_Power_BB12720D_Out, _Power_BB12720D_Out, _Power_BB12720D_Out, 1.0); return surface; }
				float _Multiply_CC88C9FE_Out;
				Unity_Multiply_float(_Power_BB12720D_Out, Vector1_FA7B64E2, _Multiply_CC88C9FE_Out);
				if (Float_AC7EF5E9 == 20) { surface.PreviewOutput = half4(_Multiply_CC88C9FE_Out, _Multiply_CC88C9FE_Out, _Multiply_CC88C9FE_Out, 1.0); return surface; }
				float4 _Texture2D_CE7FDB22_RGBA = UNITY_SAMPLE_TEX2D(Texture_507A46B3,uv0.xy);
				float _Texture2D_CE7FDB22_R = _Texture2D_CE7FDB22_RGBA.r;
				float _Texture2D_CE7FDB22_G = _Texture2D_CE7FDB22_RGBA.g;
				float _Texture2D_CE7FDB22_B = _Texture2D_CE7FDB22_RGBA.b;
				float _Texture2D_CE7FDB22_A = _Texture2D_CE7FDB22_RGBA.a;
				if (Float_AC7EF5E9 == 21) { surface.PreviewOutput = half4(_Texture2D_CE7FDB22_RGBA.x, _Texture2D_CE7FDB22_RGBA.y, _Texture2D_CE7FDB22_RGBA.z, 1.0); return surface; }
				float _Add_955670F1_Out;
				Unity_Add_float(_SinTime.w, _Add_955670F1_B, _Add_955670F1_Out);
				if (Float_AC7EF5E9 == 22) { surface.PreviewOutput = half4(_Add_955670F1_Out, _Add_955670F1_Out, _Add_955670F1_Out, 1.0); return surface; }
				float4 _Combine_76C873_RGBA;
				Unity_Combine_float(_Add_955670F1_Out, _Combine_76C873_G, _Combine_76C873_B, _Combine_76C873_A, _Combine_76C873_RGBA);
				if (Float_AC7EF5E9 == 23) { surface.PreviewOutput = half4(_Combine_76C873_RGBA.x, _Combine_76C873_RGBA.y, _Combine_76C873_RGBA.z, 1.0); return surface; }
				float4 _Multiply_89C71AC1_Out;
				Unity_Multiply_float(_Texture2D_CE7FDB22_RGBA, _Combine_76C873_RGBA, _Multiply_89C71AC1_Out);
				if (Float_AC7EF5E9 == 24) { surface.PreviewOutput = half4(_Multiply_89C71AC1_Out.x, _Multiply_89C71AC1_Out.y, _Multiply_89C71AC1_Out.z, 1.0); return surface; }
				float3 _HSVtoRGB_AE8D0A0E_rgb;
				Unity_HSVToRGB_float((_Multiply_89C71AC1_Out.xyz), _HSVtoRGB_AE8D0A0E_rgb);
				if (Float_AC7EF5E9 == 25) { surface.PreviewOutput = half4(_HSVtoRGB_AE8D0A0E_rgb.x, _HSVtoRGB_AE8D0A0E_rgb.y, _HSVtoRGB_AE8D0A0E_rgb.z, 1.0); return surface; }
				float3 _Multiply_F77E48F9_Out;
				Unity_Multiply_float(_HSVtoRGB_AE8D0A0E_rgb, _Multiply_F77E48F9_B, _Multiply_F77E48F9_Out);
				if (Float_AC7EF5E9 == 26) { surface.PreviewOutput = half4(_Multiply_F77E48F9_Out.x, _Multiply_F77E48F9_Out.y, _Multiply_F77E48F9_Out.z, 1.0); return surface; }
				float3 _Multiply_4E995ACC_Out;
				Unity_Multiply_float(_Multiply_F77E48F9_Out, (Vector1_1A8B01A5.xxx), _Multiply_4E995ACC_Out);
				if (Float_AC7EF5E9 == 27) { surface.PreviewOutput = half4(_Multiply_4E995ACC_Out.x, _Multiply_4E995ACC_Out.y, _Multiply_4E995ACC_Out.z, 1.0); return surface; }
				float _RGBtoLuminance_BF8F33FC_luminance;
				Unity_RGBToLuminance_float(_Multiply_4E995ACC_Out, _RGBtoLuminance_BF8F33FC_luminance);
				if (Float_AC7EF5E9 == 28) { surface.PreviewOutput = half4(_RGBtoLuminance_BF8F33FC_luminance, _RGBtoLuminance_BF8F33FC_luminance, _RGBtoLuminance_BF8F33FC_luminance, 1.0); return surface; }
				float _Subtract_4053AE6C_Out;
				Unity_Subtract_float(_Subtract_4053AE6C_A, _CosTime.w, _Subtract_4053AE6C_Out);
				if (Float_AC7EF5E9 == 29) { surface.PreviewOutput = half4(_Subtract_4053AE6C_Out, _Subtract_4053AE6C_Out, _Subtract_4053AE6C_Out, 1.0); return surface; }
				float4 _Combine_EBDC57C5_RGBA;
				Unity_Combine_float(_Subtract_4053AE6C_Out, _Combine_EBDC57C5_G, _Combine_EBDC57C5_B, _Combine_EBDC57C5_A, _Combine_EBDC57C5_RGBA);
				if (Float_AC7EF5E9 == 30) { surface.PreviewOutput = half4(_Combine_EBDC57C5_RGBA.x, _Combine_EBDC57C5_RGBA.y, _Combine_EBDC57C5_RGBA.z, 1.0); return surface; }
				float3 _HSVtoRGB_6DD0292E_rgb;
				Unity_HSVToRGB_float((_Combine_EBDC57C5_RGBA.xyz), _HSVtoRGB_6DD0292E_rgb);
				if (Float_AC7EF5E9 == 31) { surface.PreviewOutput = half4(_HSVtoRGB_6DD0292E_rgb.x, _HSVtoRGB_6DD0292E_rgb.y, _HSVtoRGB_6DD0292E_rgb.z, 1.0); return surface; }
				float _Split_81BA869A_R = _HSVtoRGB_AE8D0A0E_rgb[0];
				float _Split_81BA869A_G = _HSVtoRGB_AE8D0A0E_rgb[1];
				float _Split_81BA869A_B = _HSVtoRGB_AE8D0A0E_rgb[2];
				float _Split_81BA869A_A = 1.0;
				float _Multiply_9164EC34_Out;
				Unity_Multiply_float(_Split_81BA869A_R, _Split_81BA869A_G, _Multiply_9164EC34_Out);
				if (Float_AC7EF5E9 == 32) { surface.PreviewOutput = half4(_Multiply_9164EC34_Out, _Multiply_9164EC34_Out, _Multiply_9164EC34_Out, 1.0); return surface; }
				float _Multiply_841C266E_Out;
				Unity_Multiply_float(_Multiply_9164EC34_Out, _Split_81BA869A_B, _Multiply_841C266E_Out);
				if (Float_AC7EF5E9 == 33) { surface.PreviewOutput = half4(_Multiply_841C266E_Out, _Multiply_841C266E_Out, _Multiply_841C266E_Out, 1.0); return surface; }
				float _Multiply_77943E40_Out;
				Unity_Multiply_float(_Multiply_841C266E_Out, _Multiply_77943E40_B, _Multiply_77943E40_Out);
				if (Float_AC7EF5E9 == 34) { surface.PreviewOutput = half4(_Multiply_77943E40_Out, _Multiply_77943E40_Out, _Multiply_77943E40_Out, 1.0); return surface; }
				float _OneMinus_F47415CA_Out;
				Unity_OneMinus_float(_Multiply_77943E40_Out, _OneMinus_F47415CA_Out);
				if (Float_AC7EF5E9 == 35) { surface.PreviewOutput = half4(_OneMinus_F47415CA_Out, _OneMinus_F47415CA_Out, _OneMinus_F47415CA_Out, 1.0); return surface; }
				float3 _Multiply_E8E6BAEA_Out;
				Unity_Multiply_float((_OneMinus_F47415CA_Out.xxx), _HSVtoRGB_6DD0292E_rgb, _Multiply_E8E6BAEA_Out);
				if (Float_AC7EF5E9 == 36) { surface.PreviewOutput = half4(_Multiply_E8E6BAEA_Out.x, _Multiply_E8E6BAEA_Out.y, _Multiply_E8E6BAEA_Out.z, 1.0); return surface; }
				float3 _Multiply_156218A9_Out;
				Unity_Multiply_float((_Multiply_CC88C9FE_Out.xxx), _Multiply_E8E6BAEA_Out, _Multiply_156218A9_Out);
				if (Float_AC7EF5E9 == 37) { surface.PreviewOutput = half4(_Multiply_156218A9_Out.x, _Multiply_156218A9_Out.y, _Multiply_156218A9_Out.z, 1.0); return surface; }
				float3 _Add_A93EC3AF_Out;
				Unity_Add_float(_Multiply_156218A9_Out, _Multiply_F77E48F9_Out, _Add_A93EC3AF_Out);
				if (Float_AC7EF5E9 == 38) { surface.PreviewOutput = half4(_Add_A93EC3AF_Out.x, _Add_A93EC3AF_Out.y, _Add_A93EC3AF_Out.z, 1.0); return surface; }
				float2 _Normalize_F1FF54ED_Out;
				Unity_Normalize_float(_Spherize_8852C787_Out, _Normalize_F1FF54ED_Out);
				if (Float_AC7EF5E9 == 39) { surface.PreviewOutput = half4(_Normalize_F1FF54ED_Out.x, _Normalize_F1FF54ED_Out.y, 0.0, 1.0); return surface; }
				float _OneMinus_603C867D_Out;
				Unity_OneMinus_float(_RGBtoLuminance_BF8F33FC_luminance, _OneMinus_603C867D_Out);
				if (Float_AC7EF5E9 == 40) { surface.PreviewOutput = half4(_OneMinus_603C867D_Out, _OneMinus_603C867D_Out, _OneMinus_603C867D_Out, 1.0); return surface; }
				float _DotProduct_3364603C_Out;
				Unity_DotProduct_float((_Combine_93029BDA_RGBA.xyz), (_Combine_93029BDA_RGBA.xyz), _DotProduct_3364603C_Out);
				if (Float_AC7EF5E9 == 41) { surface.PreviewOutput = half4(_DotProduct_3364603C_Out, _DotProduct_3364603C_Out, _DotProduct_3364603C_Out, 1.0); return surface; }
				return surface;
			}
	ENDCG
	SubShader
	{
	    Tags { "RenderType"="Opaque" }
	    LOD 100
	    Pass
	    {
	        CGPROGRAM
	        #pragma vertex vert
	        #pragma fragment frag
	        #include "UnityCG.cginc"
	        struct GraphVertexOutput
	        {
	            float4 position : POSITION;
	            float3 WorldSpaceNormal : TEXCOORD0;
	float3 WorldSpaceViewDirection : TEXCOORD1;
	half4 uv0 : TEXCOORD2;
	        };
	        GraphVertexOutput vert (GraphVertexInput v)
	        {
	            v = PopulateVertexData(v);
	            GraphVertexOutput o;
	            o.position = UnityObjectToClipPos(v.vertex);
	            o.WorldSpaceNormal = mul(v.normal,(float3x3)unity_WorldToObject);
	o.WorldSpaceViewDirection = mul((float3x3)unity_ObjectToWorld,ObjSpaceViewDir(v.vertex));
	o.uv0 = v.texcoord0;
	            return o;
	        }
	        fixed4 frag (GraphVertexOutput IN) : SV_Target
	        {
	            float3 WorldSpaceNormal = normalize(IN.WorldSpaceNormal);
	float3 WorldSpaceViewDirection = normalize(IN.WorldSpaceViewDirection);
	float4 uv0  = IN.uv0;
	            SurfaceInputs surfaceInput = (SurfaceInputs)0;;
	            surfaceInput.WorldSpaceNormal = WorldSpaceNormal;
	surfaceInput.WorldSpaceViewDirection = WorldSpaceViewDirection;
	surfaceInput.uv0  =uv0;
	            SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
	            return surf.PreviewOutput;
	        }
	        ENDCG
	    }
	}
}
