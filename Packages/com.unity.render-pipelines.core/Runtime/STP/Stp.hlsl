// This is necessary to prevent Unity from deciding that our default config logic is actually an include guard declaration
#ifndef STP_UNITY_INCLUDE_GUARD
#define STP_UNITY_INCLUDE_GUARD
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//
//
//                                                SPATIAL TEMPORAL POST [STP] v1.0
//
//
//==============================================================================================================================
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
// C/C++/GLSL/HLSL PORTABILITY BASED ON AMD's 'ffx_a.h'.
// INCLUDING ASSOCIATED LICENSE BELOW
//------------------------------------------------------------------------------------------------------------------------------
// Copyright (c) 2021 Advanced Micro Devices, Inc. All rights reserved.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//==============================================================================================================================
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                           NOTES
//------------------------------------------------------------------------------------------------------------------------------
// PLATFORM SPECIFIC WORKAROUNDS
// =============================
// - These all default to not enabled {0}, define to {1} to enable.
// - define STP_BUG_ALIAS16 1 .... Define to enable workaround for asuint16()/asfloat16().
// - define STP_BUG_PRX 1 ........ Define to disable approximate transendentals.
// - define STP_BUG_SAT_INF 1 .... Define to workaround platforms with broken 16-bit saturate +/- INF.
// - define STP_BUG_SAT 1 ........ Define to workaround compiler incorrectly factoring out inner saturate in 16-bit code.
//------------------------------------------------------------------------------------------------------------------------------
// CONFIGURATIONS
// ==============
// - INDEPENDENT OPTIONS
//    - define STP_32BIT  {0 := disable, 1 := compile the 32-bit version or implicit precision version}
//    - define STP_MEDIUM {0 := disable, 1 := enable the implicit medium precision version for 32-bit}
//    - define STP_16BIT  {0 := disable, 1 := compile the explicit 16-bit version}
//    -----
//    - define STP_GPU  {to include shader code}
//    - define STP_GLSL {to include the GLSL version of the code}
//    - define STP_HLSL {to include the HLSL version of the code}
//    -----
//    - define STP_DIL {to include the StpDil<H,F>() entry points}
//    - define STP_PAT {to include the StpPat<H,F>() entry points}
//    - define STP_SAA {to include the StpSaa<H,F>() entry points}
//    - define STP_TAA {to include the StpTaa<H,F>() entry points}
//    -----
//    - define STP_POSTMAP {running STP, 0 := before, 1 := after, application tonemapping}
//------------------------------------------------------------------------------------------------------------------------------
// IMPORTANT
// =========
// - All callbacks should explicitly sample from MIP level 0.
//    - Meaning if used in a pixel shader do not allow implicit LOD calculation.
// - The algorithm is tuned for pre-tonemap operation, post-tonemap wasn't tested yet.
//==============================================================================================================================
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                      EXTERNAL OPTIONS
//==============================================================================================================================
// Enable {1} or default disable any debug functionality {0}.
#ifndef STP_BUG
    #define STP_BUG 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define to test a pass-through dummy shader that fetches all resources but does no logic.
#ifndef STP_BUG_BW_SOL
    #define STP_BUG_BW_SOL 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define to {1} to use the max/min sampling permutation for color values.
#ifndef STP_MAX_MIN_10BIT
    #define STP_MAX_MIN_10BIT 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define to {1} to use the max/min sampling permutation for UINT32 values.
#ifndef STP_MAX_MIN_UINT
    #define STP_MAX_MIN_UINT 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define to {1} to use sampling with offsets.
#ifndef STP_OFFSETS
    #define STP_OFFSETS 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// STP is currently only tested to run pre-tonemap at that is what Unity is using.
// Run 0 := pre-tonemap, 1 := post-tonemap.
#ifndef STP_POSTMAP
    #define STP_POSTMAP 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// STP TAA quality level {0 to 1}
#ifndef STP_TAA_Q
    #define STP_TAA_Q 1
#endif
//==============================================================================================================================
// PLATFORM SPECIFIC BUG WORKAROUNDS
// =================================
// Define to {1} to disable usage of transendental approximations using float/int aliasing.
#ifndef STP_BUG_PRX
    #define STP_BUG_PRX 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define to {1} for workaround if platform cannot use saturate of +/- INF correctly.
#ifndef STP_BUG_SAT_INF
    #define STP_BUG_SAT_INF 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define to {1} for workaround for compilier incorrectly factoring out inner saturate in 16-bit code.
#ifndef STP_BUG_SAT
    #define STP_BUG_SAT 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define to {1} for workarounds for broken asuint16()/asfloat16().
#ifndef STP_BUG_ALIAS16
    #define STP_BUG_ALIAS16 0
    #undef STP_BUG_PRX
    #define STP_BUG_PRX 1
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                  C/C++/GLSL/HLSL PORTABILITY
//==============================================================================================================================
#if defined(STP_CPU)
    #ifndef STP_RESTRICT
        #define STP_RESTRICT __restrict
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    #ifndef STP_STATIC
        #define STP_STATIC static
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    typedef unsigned char StpB1;
    typedef unsigned short StpW1;
    typedef float StpF1;
    typedef uint32_t StpU1;
    #define StpF1_(a) ((StpF1)(a))
    #define StpU1_(a) ((StpU1)(a))
    STP_STATIC StpU1 StpU1_F1(StpF1 a) { union { StpF1 f; StpU1 u; } bits; bits.f = a; return bits.u; }
    #define StpOutF2 StpF1 *STP_RESTRICT
    #define StpExp2F1(x) exp2f(x)
    STP_STATIC StpF1 StpMaxF1(StpF1 a, StpF1 b) { return a > b ? a : b; }
//------------------------------------------------------------------------------------------------------------------------------
    // Convert float to half (in lower 16-bits of output).
    // Same fast technique as documented here: ftp://ftp.fox-toolkit.org/pub/fasthalffloatconversion.pdf
    // Supports denormals.
    // Conversion rules are to make computations possibly "safer" on the GPU,
    //  -INF & -NaN -> -65504
    //  +INF & +NaN -> +65504
    STP_STATIC StpU1 StpU1_H1_F1(StpF1 f) {
        static StpW1 base[512] = {
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0001,0x0002,0x0004,0x0008,0x0010,0x0020,0x0040,0x0080,0x0100,
            0x0200,0x0400,0x0800,0x0c00,0x1000,0x1400,0x1800,0x1c00,0x2000,0x2400,0x2800,0x2c00,0x3000,0x3400,0x3800,0x3c00,
            0x4000,0x4400,0x4800,0x4c00,0x5000,0x5400,0x5800,0x5c00,0x6000,0x6400,0x6800,0x6c00,0x7000,0x7400,0x7800,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8001,0x8002,0x8004,0x8008,0x8010,0x8020,0x8040,0x8080,0x8100,
            0x8200,0x8400,0x8800,0x8c00,0x9000,0x9400,0x9800,0x9c00,0xa000,0xa400,0xa800,0xac00,0xb000,0xb400,0xb800,0xbc00,
            0xc000,0xc400,0xc800,0xcc00,0xd000,0xd400,0xd800,0xdc00,0xe000,0xe400,0xe800,0xec00,0xf000,0xf400,0xf800,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff };
        static StpB1 shift[512] = {
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x17,0x16,0x15,0x14,0x13,0x12,0x11,0x10,0x0f,
            0x0e,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,
            0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x17,0x16,0x15,0x14,0x13,0x12,0x11,0x10,0x0f,
            0x0e,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,
            0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18 };
        union { StpF1 f; StpU1 u; } bits;
        bits.f = f; StpU1 u = bits.u; StpU1 i = u >> 23;
        return (StpU1)(base[i]) + ((u & 0x7fffff) >> shift[i]); }
//------------------------------------------------------------------------------------------------------------------------------
    STP_STATIC StpU1 StpU1_H2_F2(StpInF2 a) { return StpU1_H1_F1(a[0]) + (StpU1_H1_F1(a[1]) << 16); }
#endif // defined(STP_CPU)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_GLSL)
    #define StpP1 bool
    #define StpP2 bvec2
//------------------------------------------------------------------------------------------------------------------------------
    #define StpF1 float
    #define StpF2 vec2
    #define StpF3 vec3
    #define StpF4 vec4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpI2 ivec2
//------------------------------------------------------------------------------------------------------------------------------
    #define StpU1 uint
    #define StpU2 uvec2
    #define StpU3 uvec3
    #define StpU4 uvec4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpF1_U1(x) uintBitsToFloat(StpU1(x))
    #define StpF2_U2(x) uintBitsToFloat(StpU2(x))
    #define StpF3_U3(x) uintBitsToFloat(StpU3(x))
    #define StpF4_U4(x) uintBitsToFloat(StpU4(x))
    #define StpU1_F1(x) floatBitsToUint(StpF1(x))
    #define StpU2_F2(x) floatBitsToUint(StpF2(x))
    #define StpU3_F3(x) floatBitsToUint(StpF3(x))
    #define StpU4_F4(x) floatBitsToUint(StpF4(x))
//------------------------------------------------------------------------------------------------------------------------------
    #define StpU1_H2_F2 packHalf2x16
    #define StpF2_H2_U1 unpackHalf2x16
//------------------------------------------------------------------------------------------------------------------------------
    StpU1 StpBfeU1(StpU1 src, StpU1 off, StpU1 bits) { return bitfieldExtract(src, int(off), int(bits)); }
    // Proxy for V_BFI_B32 where the 'mask' is set as 'bits', 'mask=(1<<bits)-1', and 'bits' needs to be an immediate.
    StpU1 StpBfiMskU1(StpU1 src, StpU1 ins, StpU1 bits) { return bitfieldInsert(src, ins, 0, int(bits)); }
#endif // defined(STP_GPU) && defined(STP_GLSL)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_GLSL) && defined(STP_16BIT)
    #define StpH1 float16_t
    #define StpH2 f16vec2
    #define StpH3 f16vec3
    #define StpH4 f16vec4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpW1 uint16_t
    #define StpW2 u16vec2
    #define StpW3 u16vec3
    #define StpW4 u16vec4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpW2_U1(x) unpackUint2x16(StpU1(x))
    #define StpH2_U1(x) unpackFloat2x16(StpU1(x))
//------------------------------------------------------------------------------------------------------------------------------
    #define StpW1_H1(x) halfBitsToUint16(StpH1(x))
    #define StpW2_H2(x) halfBitsToUint16(StpH2(x))
    #define StpW3_H3(x) halfBitsToUint16(StpH3(x))
    #define StpW4_H4(x) halfBitsToUint16(StpH4(x))
//------------------------------------------------------------------------------------------------------------------------------
    #define StpH1_W1(x) uint16BitsToHalf(StpW1(x))
    #define StpH2_W2(x) uint16BitsToHalf(StpW2(x))
    #define StpH3_W3(x) uint16BitsToHalf(StpW3(x))
    #define StpH4_W4(x) uint16BitsToHalf(StpW4(x))
//------------------------------------------------------------------------------------------------------------------------------
    #define StpU1_H2(x) packFloat2x16(StpH2(x))
#endif // defined(STP_GPU) && defined(STP_GLSL) && defined(STP_16BIT)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_HLSL)
    #define StpP1 bool
    #define StpP2 bool2
//------------------------------------------------------------------------------------------------------------------------------
    #define StpF1 float
    #define StpF2 float2
    #define StpF3 float3
    #define StpF4 float4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpI2 int2
//------------------------------------------------------------------------------------------------------------------------------
    #define StpU1 uint
    #define StpU2 uint2
    #define StpU3 uint3
    #define StpU4 uint4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpF1_U1(x) asfloat(StpU1(x))
    #define StpF2_U2(x) asfloat(StpU2(x))
    #define StpF3_U3(x) asfloat(StpU3(x))
    #define StpF4_U4(x) asfloat(StpU4(x))
    #define StpU1_F1(x) asuint(StpF1(x))
    #define StpU2_F2(x) asuint(StpF2(x))
    #define StpU3_F3(x) asuint(StpF3(x))
    #define StpU4_F4(x) asuint(StpF4(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpU1 StpU1_H2_F2_x(StpF2 a) { return f32tof16(a.x) | (f32tof16(a.y) << 16); }
    #define StpU1_H2_F2(a) StpU1_H2_F2_x(StpF2(a))
//------------------------------------------------------------------------------------------------------------------------------
    StpF2 StpF2_H2_U1_x(StpU1 x) { return StpF2(f16tof32(x & 0xFFFF), f16tof32(x >> 16)); }
    #define StpF2_H2_U1(x) StpF2_H2_U1_x(StpU1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpU1 StpBfeU1(StpU1 src, StpU1 off, StpU1 bits) { StpU1 msk = (1u << bits) - 1; return (src >> off) & msk; }
    StpU1 StpBfiMskU1(StpU1 src, StpU1 ins, StpU1 bits) { StpU1 msk = (1u << bits) - 1; return (ins & msk) | (src & (~msk)); }
#endif // defined(STP_GPU) && defined(STP_HLSL)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_HLSL) && defined(STP_MEDIUM)
    #define StpMU1 min16uint
    #define StpMU2 min16uint2
    #define StpMU3 min16uint3
    #define StpMU4 min16uint4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpMF1 min16float
    #define StpMF2 min16float2
    #define StpMF3 min16float3
    #define StpMF4 min16float4
#endif // defined(STP_GPU) && defined(STP_HLSL) && defined(STP_MEDIUM)
//==============================================================================================================================
#if defined(STP_GPU) && (!defined(STP_MEDIUM))
    #define StpMU1 StpU1
    #define StpMU2 StpU2
    #define StpMU3 StpU3
    #define StpMU4 StpU4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpMF1 StpF1
    #define StpMF2 StpF2
    #define StpMF3 StpF3
    #define StpMF4 StpF4
#endif // defined(STP_GPU) && (!defined(STP_MEDIUM))
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_HLSL) && defined(STP_16BIT)
    #define StpH1 float16_t
    #define StpH2 float16_t2
    #define StpH3 float16_t3
    #define StpH4 float16_t4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpW1 uint16_t
    #define StpW2 uint16_t2
    #define StpW3 uint16_t3
    #define StpW4 uint16_t4
//------------------------------------------------------------------------------------------------------------------------------
    StpW2 StpW2_U1_x(StpU1 x) { StpU2 t = StpU2(x & 0xFFFF, x >> 16); return StpW2(t); }
    #define StpW2_U1(x) StpW2_U1_x(StpU1(x))
    StpH2 StpH2_U1_x(StpU1 x) { return asfloat16(StpW2((StpW1)(x & 0xFFFF), (StpW1)(x >> 16))); }
    #define StpH2_U1(x) StpH2_U1_x(StpU1(x))
//------------------------------------------------------------------------------------------------------------------------------
    #define StpW1_H1(x) asuint16(StpH1(x))
    #define StpW2_H2(x) asuint16(StpH2(x))
    #define StpW3_H3(x) asuint16(StpH3(x))
    #define StpW4_H4(x) asuint16(StpH4(x))
//------------------------------------------------------------------------------------------------------------------------------
    #define StpH1_W1(x) asfloat16(StpW1(x))
    #define StpH2_W2(x) asfloat16(StpW2(x))
    #define StpH3_W3(x) asfloat16(StpW3(x))
    #define StpH4_W4(x) asfloat16(StpW4(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpU1 StpU1_H2_x(StpH2 x) { StpW2 t = asuint16(x); return (((StpU1)t.x) | (((StpU1)t.y) << 16)); }
    #define StpU1_H2(x) StpU1_H2_x(StpH2(x))
#endif // defined(STP_GPU) && defined(STP_HLSL) && defined(STP_16BIT)
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL))
    StpF1 StpMaxF1(StpF1 a, StpF1 b) { return max(a, b); }
//------------------------------------------------------------------------------------------------------------------------------
    StpP2 StpP2_x(StpP1 x) { return StpP2(x, x); }
    #define StpP2_(x) StpP2_x(StpP1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpF1 StpF1_x(StpF1 x) { return StpF1(x); }
    StpF2 StpF2_x(StpF1 x) { return StpF2(x, x); }
    StpF3 StpF3_x(StpF1 x) { return StpF3(x, x, x); }
    StpF4 StpF4_x(StpF1 x) { return StpF4(x, x, x, x); }
    #define StpF1_(x) StpF1_x(StpF1(x))
    #define StpF2_(x) StpF2_x(StpF1(x))
    #define StpF3_(x) StpF3_x(StpF1(x))
    #define StpF4_(x) StpF4_x(StpF1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpMF1_x(StpMF1 x) { return StpMF1(x); }
    StpMF2 StpMF2_x(StpMF1 x) { return StpMF2(x, x); }
    StpMF3 StpMF3_x(StpMF1 x) { return StpMF3(x, x, x); }
    StpMF4 StpMF4_x(StpMF1 x) { return StpMF4(x, x, x, x); }
    #define StpMF1_(x) StpMF1_x(StpMF1(x))
    #define StpMF2_(x) StpMF2_x(StpMF1(x))
    #define StpMF3_(x) StpMF3_x(StpMF1(x))
    #define StpMF4_(x) StpMF4_x(StpMF1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpMU1 StpMU1_x(StpMU1 x) { return StpMU1(x); }
    StpMU2 StpMU2_x(StpMU1 x) { return StpMU2(x, x); }
    StpMU3 StpMU3_x(StpMU1 x) { return StpMU3(x, x, x); }
    StpMU4 StpMU4_x(StpMU1 x) { return StpMU4(x, x, x, x); }
    #define StpMU1_(x) StpMU1_x(StpMU1(x))
    #define StpMU2_(x) StpMU2_x(StpMU1(x))
    #define StpMU3_(x) StpMU3_x(StpMU1(x))
    #define StpMU4_(x) StpMU4_x(StpMU1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpU1 StpU1_x(StpU1 x) { return StpU1(x); }
    StpU2 StpU2_x(StpU1 x) { return StpU2(x, x); }
    StpU3 StpU3_x(StpU1 x) { return StpU3(x, x, x); }
    StpU4 StpU4_x(StpU1 x) { return StpU4(x, x, x, x); }
    #define StpU1_(x) StpU1_x(StpU1(x))
    #define StpU2_(x) StpU2_x(StpU1(x))
    #define StpU3_(x) StpU3_x(StpU1(x))
    #define StpU4_(x) StpU4_x(StpU1(x))
//------------------------------------------------------------------------------------------------------------------------------
    #if 0
        // Slow implementation (if not pattern matched by a compiler).
        StpF1 StpCpySgnF1(StpF1 d, StpF1 s) { return StpF1_U1(StpU1_F1(d) | (StpU1_F1(s) & StpU1_(0x80000000u))); }
        StpF2 StpCpySgnF2(StpF2 d, StpF2 s) { return StpF2_U2(StpU2_F2(d) | (StpU2_F2(s) & StpU2_(0x80000000u))); }
        StpF3 StpCpySgnF3(StpF3 d, StpF3 s) { return StpF3_U3(StpU3_F3(d) | (StpU3_F3(s) & StpU3_(0x80000000u))); }
        StpF4 StpCpySgnF4(StpF4 d, StpF4 s) { return StpF4_U4(StpU4_F4(d) | (StpU4_F4(s) & StpU4_(0x80000000u))); }
    #else
        // Faster implementation (one portable BFI).
        StpF1 StpCpySgnF1(StpF1 d, StpF1 s) { return StpF1_U1(StpBfiMskU1(StpU1_F1(s), StpU1_F1(d), StpU1_(31))); }
        StpF2 StpCpySgnF2(StpF2 d, StpF2 s) { return StpF2(StpCpySgnF1(d.x, s.x), StpCpySgnF1(d.y, s.y)); }
        StpF3 StpCpySgnF3(StpF3 d, StpF3 s) {
            return StpF3(StpCpySgnF1(d.x, s.x), StpCpySgnF1(d.y, s.y), StpCpySgnF1(d.z, s.z)); }
        StpF4 StpCpySgnF4(StpF4 d, StpF4 s) {
            return StpF4(StpCpySgnF1(d.x, s.x), StpCpySgnF1(d.y, s.y), StpCpySgnF1(d.z, s.z), StpCpySgnF1(d.w, s.w)); }
    #endif
    StpF1 StpMax3F1(StpF1 x, StpF1 y, StpF1 z) { return max(x, max(y, z)); }
    StpF2 StpMax3F2(StpF2 x, StpF2 y, StpF2 z) { return max(x, max(y, z)); }
    StpF3 StpMax3F3(StpF3 x, StpF3 y, StpF3 z) { return max(x, max(y, z)); }
    StpF4 StpMax3F4(StpF4 x, StpF4 y, StpF4 z) { return max(x, max(y, z)); }
    StpF1 StpMin3F1(StpF1 x, StpF1 y, StpF1 z) { return min(x, min(y, z)); }
    StpF2 StpMin3F2(StpF2 x, StpF2 y, StpF2 z) { return min(x, min(y, z)); }
    StpF3 StpMin3F3(StpF3 x, StpF3 y, StpF3 z) { return min(x, min(y, z)); }
    StpF4 StpMin3F4(StpF4 x, StpF4 y, StpF4 z) { return min(x, min(y, z)); }
    StpU1 StpMax3U1(StpU1 x, StpU1 y, StpU1 z) { return max(x, max(y, z)); }
    StpU1 StpMin3U1(StpU1 x, StpU1 y, StpU1 z) { return min(x, min(y, z)); }
    StpU4 StpMin3U4(StpU4 x, StpU4 y, StpU4 z) { return min(x, min(y, z)); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpMax3MF1(StpMF1 x, StpMF1 y, StpMF1 z) { return max(x, max(y, z)); }
    StpMF2 StpMax3MF2(StpMF2 x, StpMF2 y, StpMF2 z) { return max(x, max(y, z)); }
    StpMF3 StpMax3MF3(StpMF3 x, StpMF3 y, StpMF3 z) { return max(x, max(y, z)); }
    StpMF4 StpMax3MF4(StpMF4 x, StpMF4 y, StpMF4 z) { return max(x, max(y, z)); }
    StpMF1 StpMin3MF1(StpMF1 x, StpMF1 y, StpMF1 z) { return min(x, min(y, z)); }
    StpMF2 StpMin3MF2(StpMF2 x, StpMF2 y, StpMF2 z) { return min(x, min(y, z)); }
    StpMF3 StpMin3MF3(StpMF3 x, StpMF3 y, StpMF3 z) { return min(x, min(y, z)); }
    StpMF4 StpMin3MF4(StpMF4 x, StpMF4 y, StpMF4 z) { return min(x, min(y, z)); }
//------------------------------------------------------------------------------------------------------------------------------
    // Make {<+0 := -1.0, >=+0 := 1.0}.
    StpF1 StpSgnOneF1(StpF1 x) { return StpF1_U1(StpBfiMskU1(StpU1_F1(x), StpU1_(0x3f800000), StpU1_(31))); }
#endif // defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL))
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && defined(STP_16BIT)
    StpH1 StpH1_x(StpH1 x) { return StpH1(x); }
    StpH2 StpH2_x(StpH1 x) { return StpH2(x, x); }
    StpH3 StpH3_x(StpH1 x) { return StpH3(x, x, x); }
    StpH4 StpH4_x(StpH1 x) { return StpH4(x, x, x, x); }
    #define StpH1_(x) StpH1_x(StpH1(x))
    #define StpH2_(x) StpH2_x(StpH1(x))
    #define StpH3_(x) StpH3_x(StpH1(x))
    #define StpH4_(x) StpH4_x(StpH1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpW1 StpW1_x(StpW1 x) { return StpW1(x); }
    StpW2 StpW2_x(StpW1 x) { return StpW2(x, x); }
    StpW3 StpW3_x(StpW1 x) { return StpW3(x, x, x); }
    StpW4 StpW4_x(StpW1 x) { return StpW4(x, x, x, x); }
    #define StpW1_(x) StpW1_x(StpW1(x))
    #define StpW2_(x) StpW2_x(StpW1(x))
    #define StpW3_(x) StpW3_x(StpW1(x))
    #define StpW4_(x) StpW4_x(StpW1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpH1 StpMax3H1(StpH1 x, StpH1 y, StpH1 z) { return max(x, max(y, z)); }
    StpH2 StpMax3H2(StpH2 x, StpH2 y, StpH2 z) { return max(x, max(y, z)); }
    StpH3 StpMax3H3(StpH3 x, StpH3 y, StpH3 z) { return max(x, max(y, z)); }
    StpH4 StpMax3H4(StpH4 x, StpH4 y, StpH4 z) { return max(x, max(y, z)); }
    StpH1 StpMin3H1(StpH1 x, StpH1 y, StpH1 z) { return min(x, min(y, z)); }
    StpH2 StpMin3H2(StpH2 x, StpH2 y, StpH2 z) { return min(x, min(y, z)); }
    StpH3 StpMin3H3(StpH3 x, StpH3 y, StpH3 z) { return min(x, min(y, z)); }
    StpH4 StpMin3H4(StpH4 x, StpH4 y, StpH4 z) { return min(x, min(y, z)); }
    StpW1 StpMax3W1(StpW1 x, StpW1 y, StpW1 z) { return max(x, max(y, z)); }
    StpW1 StpMin3W1(StpW1 x, StpW1 y, StpW1 z) { return min(x, min(y, z)); }
#endif // defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && defined(STP_16BIT)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_GLSL)
    StpF1 StpFractF1(StpF1 x) { return fract(x); }
    StpF2 StpFractF2(StpF2 x) { return fract(x); }
    StpF3 StpFractF3(StpF3 x) { return fract(x); }
    StpF4 StpFractF4(StpF4 x) { return fract(x); }
    StpF1 StpLerpF1(StpF1 x, StpF1 y, StpF1 z) { return mix(x, y, z); }
    StpF2 StpLerpF2(StpF2 x, StpF2 y, StpF2 z) { return mix(x, y, z); }
    StpF3 StpLerpF3(StpF3 x, StpF3 y, StpF3 z) { return mix(x, y, z); }
    StpF4 StpLerpF4(StpF4 x, StpF4 y, StpF4 z) { return mix(x, y, z); }
    StpF1 StpRcpF1(StpF1 x) { return StpF1_(1.0) / x; }
    StpF2 StpRcpF2(StpF2 x) { return StpF2_(1.0) / x; }
    StpF3 StpRcpF3(StpF3 x) { return StpF3_(1.0) / x; }
    StpF4 StpRcpF4(StpF4 x) { return StpF4_(1.0) / x; }
    StpF1 StpRsqF1(StpF1 x) { return inversesqrt(x); }
    StpF2 StpRsqF2(StpF2 x) { return inversesqrt(x); }
    StpF3 StpRsqF3(StpF3 x) { return inversesqrt(x); }
    StpF4 StpRsqF4(StpF4 x) { return inversesqrt(x); }
    StpF1 StpSatF1(StpF1 x) { return clamp(x, StpF1_(0.0), StpF1_(1.0)); }
    StpF2 StpSatF2(StpF2 x) { return clamp(x, StpF2_(0.0), StpF2_(1.0)); }
    StpF3 StpSatF3(StpF3 x) { return clamp(x, StpF3_(0.0), StpF3_(1.0)); }
    StpF4 StpSatF4(StpF4 x) { return clamp(x, StpF4_(0.0), StpF4_(1.0)); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpFractMF1(StpMF1 x) { return fract(x); }
    StpMF2 StpFractMF2(StpMF2 x) { return fract(x); }
    StpMF3 StpFractMF3(StpMF3 x) { return fract(x); }
    StpMF4 StpFractMF4(StpMF4 x) { return fract(x); }
    StpMF1 StpLerpMF1(StpMF1 x, StpMF1 y, StpMF1 z) { return mix(x, y, z); }
    StpMF2 StpLerpMF2(StpMF2 x, StpMF2 y, StpMF2 z) { return mix(x, y, z); }
    StpMF3 StpLerpMF3(StpMF3 x, StpMF3 y, StpMF3 z) { return mix(x, y, z); }
    StpMF4 StpLerpMF4(StpMF4 x, StpMF4 y, StpMF4 z) { return mix(x, y, z); }
    StpMF1 StpRcpMF1(StpMF1 x) { return StpMF1_(1.0) / x; }
    StpMF2 StpRcpMF2(StpMF2 x) { return StpMF2_(1.0) / x; }
    StpMF3 StpRcpMF3(StpMF3 x) { return StpMF3_(1.0) / x; }
    StpMF4 StpRcpMF4(StpMF4 x) { return StpMF4_(1.0) / x; }
    StpMF1 StpRsqMF1(StpMF1 x) { return inversesqrt(x); }
    StpMF2 StpRsqMF2(StpMF2 x) { return inversesqrt(x); }
    StpMF3 StpRsqMF3(StpMF3 x) { return inversesqrt(x); }
    StpMF4 StpRsqMF4(StpMF4 x) { return inversesqrt(x); }
    StpMF1 StpSatMF1(StpMF1 x) { return clamp(x, StpMF1_(0.0), StpMF1_(1.0)); }
    StpMF2 StpSatMF2(StpMF2 x) { return clamp(x, StpMF2_(0.0), StpMF2_(1.0)); }
    StpMF3 StpSatMF3(StpMF3 x) { return clamp(x, StpMF3_(0.0), StpMF3_(1.0)); }
    StpMF4 StpSatMF4(StpMF4 x) { return clamp(x, StpMF4_(0.0), StpMF4_(1.0)); }
#endif // defined(STP_GPU) && defined(STP_GLSL)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_GLSL) && defined(STP_16BIT)
    StpH1 StpFractH1(StpH1 x) { return fract(x); }
    StpH2 StpFractH2(StpH2 x) { return fract(x); }
    StpH3 StpFractH3(StpH3 x) { return fract(x); }
    StpH4 StpFractH4(StpH4 x) { return fract(x); }
    StpH1 StpLerpH1(StpH1 x, StpH1 y, StpH1 z) { return mix(x, y, z); }
    StpH2 StpLerpH2(StpH2 x, StpH2 y, StpH2 z) { return mix(x, y, z); }
    StpH3 StpLerpH3(StpH3 x, StpH3 y, StpH3 z) { return mix(x, y, z); }
    StpH4 StpLerpH4(StpH4 x, StpH4 y, StpH4 z) { return mix(x, y, z); }
    StpH1 StpRcpH1(StpH1 x) { return StpH1_(1.0) / x; }
    StpH2 StpRcpH2(StpH2 x) { return StpH2_(1.0) / x; }
    StpH3 StpRcpH3(StpH3 x) { return StpH3_(1.0) / x; }
    StpH4 StpRcpH4(StpH4 x) { return StpH4_(1.0) / x; }
    StpH1 StpRsqH1(StpH1 x) { return inversesqrt(x); }
    StpH2 StpRsqH2(StpH2 x) { return inversesqrt(x); }
    StpH3 StpRsqH3(StpH3 x) { return inversesqrt(x); }
    StpH4 StpRsqH4(StpH4 x) { return inversesqrt(x); }
    StpH1 StpSatH1(StpH1 x) { return clamp(x, StpH1_(0.0), StpH1_(1.0)); }
    StpH2 StpSatH2(StpH2 x) { return clamp(x, StpH2_(0.0), StpH2_(1.0)); }
    StpH3 StpSatH3(StpH3 x) { return clamp(x, StpH3_(0.0), StpH3_(1.0)); }
    StpH4 StpSatH4(StpH4 x) { return clamp(x, StpH4_(0.0), StpH4_(1.0)); }
#endif // defined(STP_GPU) && defined(STP_GLSL) && defined(STP_16BIT)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_HLSL)
    StpF1 StpFractF1(StpF1 x) { return x - floor(x); }
    StpF2 StpFractF2(StpF2 x) { return x - floor(x); }
    StpF3 StpFractF3(StpF3 x) { return x - floor(x); }
    StpF4 StpFractF4(StpF4 x) { return x - floor(x); }
    StpF1 StpLerpF1(StpF1 x, StpF1 y, StpF1 z) { return lerp(x, y, z); }
    StpF2 StpLerpF2(StpF2 x, StpF2 y, StpF2 z) { return lerp(x, y, z); }
    StpF3 StpLerpF3(StpF3 x, StpF3 y, StpF3 z) { return lerp(x, y, z); }
    StpF4 StpLerpF4(StpF4 x, StpF4 y, StpF4 z) { return lerp(x, y, z); }
    StpF1 StpRcpF1(StpF1 x) { return rcp(x); }
    StpF2 StpRcpF2(StpF2 x) { return rcp(x); }
    StpF3 StpRcpF3(StpF3 x) { return rcp(x); }
    StpF4 StpRcpF4(StpF4 x) { return rcp(x); }
    StpF1 StpRsqF1(StpF1 x) { return rsqrt(x); }
    StpF2 StpRsqF2(StpF2 x) { return rsqrt(x); }
    StpF3 StpRsqF3(StpF3 x) { return rsqrt(x); }
    StpF4 StpRsqF4(StpF4 x) { return rsqrt(x); }
    StpF1 StpSatF1(StpF1 x) { return saturate(x); }
    StpF2 StpSatF2(StpF2 x) { return saturate(x); }
    StpF3 StpSatF3(StpF3 x) { return saturate(x); }
    StpF4 StpSatF4(StpF4 x) { return saturate(x); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpFractMF1(StpMF1 x) { return x - floor(x); }
    StpMF2 StpFractMF2(StpMF2 x) { return x - floor(x); }
    StpMF3 StpFractMF3(StpMF3 x) { return x - floor(x); }
    StpMF4 StpFractMF4(StpMF4 x) { return x - floor(x); }
    StpMF1 StpLerpMF1(StpMF1 x, StpMF1 y, StpMF1 z) { return lerp(x, y, z); }
    StpMF2 StpLerpMF2(StpMF2 x, StpMF2 y, StpMF2 z) { return lerp(x, y, z); }
    StpMF3 StpLerpMF3(StpMF3 x, StpMF3 y, StpMF3 z) { return lerp(x, y, z); }
    StpMF4 StpLerpMF4(StpMF4 x, StpMF4 y, StpMF4 z) { return lerp(x, y, z); }
    StpMF1 StpRcpMF1(StpMF1 x) { return rcp(x); }
    StpMF2 StpRcpMF2(StpMF2 x) { return rcp(x); }
    StpMF3 StpRcpMF3(StpMF3 x) { return rcp(x); }
    StpMF4 StpRcpMF4(StpMF4 x) { return rcp(x); }
    StpMF1 StpRsqMF1(StpMF1 x) { return rsqrt(x); }
    StpMF2 StpRsqMF2(StpMF2 x) { return rsqrt(x); }
    StpMF3 StpRsqMF3(StpMF3 x) { return rsqrt(x); }
    StpMF4 StpRsqMF4(StpMF4 x) { return rsqrt(x); }
    StpMF1 StpSatMF1(StpMF1 x) { return saturate(x); }
    StpMF2 StpSatMF2(StpMF2 x) { return saturate(x); }
    StpMF3 StpSatMF3(StpMF3 x) { return saturate(x); }
    StpMF4 StpSatMF4(StpMF4 x) { return saturate(x); }
#endif // defined(STP_GPU) && defined(STP_HLSL)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_HLSL) && defined(STP_16BIT)
    StpH1 StpFractH1(StpH1 x) { return x - floor(x); }
    StpH2 StpFractH2(StpH2 x) { return x - floor(x); }
    StpH3 StpFractH3(StpH3 x) { return x - floor(x); }
    StpH4 StpFractH4(StpH4 x) { return x - floor(x); }
    StpH1 StpLerpH1(StpH1 x, StpH1 y, StpH1 z) { return lerp(x, y, z); }
    StpH2 StpLerpH2(StpH2 x, StpH2 y, StpH2 z) { return lerp(x, y, z); }
    StpH3 StpLerpH3(StpH3 x, StpH3 y, StpH3 z) { return lerp(x, y, z); }
    StpH4 StpLerpH4(StpH4 x, StpH4 y, StpH4 z) { return lerp(x, y, z); }
    StpH1 StpRcpH1(StpH1 x) { return rcp(x); }
    StpH2 StpRcpH2(StpH2 x) { return rcp(x); }
    StpH3 StpRcpH3(StpH3 x) { return rcp(x); }
    StpH4 StpRcpH4(StpH4 x) { return rcp(x); }
    StpH1 StpRsqH1(StpH1 x) { return rsqrt(x); }
    StpH2 StpRsqH2(StpH2 x) { return rsqrt(x); }
    StpH3 StpRsqH3(StpH3 x) { return rsqrt(x); }
    StpH4 StpRsqH4(StpH4 x) { return rsqrt(x); }
    StpH1 StpSatH1(StpH1 x) { return saturate(x); }
    StpH2 StpSatH2(StpH2 x) { return saturate(x); }
    StpH3 StpSatH3(StpH3 x) { return saturate(x); }
    StpH4 StpSatH4(StpH4 x) { return saturate(x); }
#endif // defined(STP_GPU) && defined(STP_HLSL) && defined(STP_16BIT)
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL))
    StpF1 StpExp2F1(StpF1 x) { return exp2(x); }
    StpF1 StpLog2F1(StpF1 x) { return log2(x); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpExp2MF1(StpMF1 x) { return exp2(x); }
    StpMF1 StpLog2MF1(StpMF1 x) { return log2(x); }
//------------------------------------------------------------------------------------------------------------------------------
    #define STP_INFN_F StpF1_U1(0xff800000u)
    #define STP_INFP_F StpF1_U1(0x7f800000u)
    #if STP_BUG_SAT_INF
        // Defined if unable to use the fast path because of problem related to saturating +/- INF.
        StpF1 StpGtZeroF1(StpF1 x) { return (x > StpF1_(0.0)) ? StpF1_(1.0) : StpF1_(0.0); }
        StpF3 StpGtZeroF3(StpF3 x) { return StpF3(StpGtZeroF1(x.r), StpGtZeroF1(x.g), StpGtZeroF1(x.b)); }
        StpF4 StpGtZeroF4(StpF4 x) { return StpF4(StpGtZeroF1(x.r), StpGtZeroF1(x.g),
            StpGtZeroF1(x.b), StpGtZeroF1(x.a)); }
        StpF1 StpSignedF1(StpF1 x) { return (x < StpF1_(0.0)) ? StpF1_(1.0) : StpF1_(0.0); }
        StpF2 StpSignedF2(StpF2 x) { return StpF2(StpSignedF1(x.r), StpSignedF1(x.g)); }
        StpF3 StpSignedF3(StpF3 x) { return StpF3(StpSignedF1(x.r), StpSignedF1(x.g), StpSignedF1(x.b)); }
        StpF4 StpSignedF4(StpF4 x) { return StpF4(StpSignedF1(x.r), StpSignedF1(x.g),
            StpSignedF1(x.b), StpSignedF1(x.a)); }
    #else
        StpF1 StpGtZeroF1(StpF1 x) { return StpSatF1(x * StpF1_(STP_INFP_F)); }
        StpF3 StpGtZeroF3(StpF3 x) { return StpSatF3(x * StpF3_(STP_INFP_F)); }
        StpF4 StpGtZeroF4(StpF4 x) { return StpSatF4(x * StpF4_(STP_INFP_F)); }
        StpF1 StpSignedF1(StpF1 x) { return StpSatF1(x * StpF1_(STP_INFN_F)); }
        StpF2 StpSignedF2(StpF2 x) { return StpSatF2(x * StpF2_(STP_INFN_F)); }
        StpF3 StpSignedF3(StpF3 x) { return StpSatF3(x * StpF3_(STP_INFN_F)); }
        StpF4 StpSignedF4(StpF4 x) { return StpSatF4(x * StpF4_(STP_INFN_F)); }
    #endif // STP_BUG_SAT_INF
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_BUG_PRX
        StpF1 StpPrxLoSqrtF1(StpF1 a) { return sqrt(a); }
        StpF3 StpPrxLoSqrtF3(StpF3 a) { return sqrt(a); }
        StpF4 StpPrxLoSqrtF4(StpF4 a) { return sqrt(a); }
    #else
        StpF1 StpPrxLoSqrtF1(StpF1 a) { return StpF1_U1((StpU1_F1(a) >> StpU1_(1)) + StpU1_(0x1fbc4639)); }
        StpF3 StpPrxLoSqrtF3(StpF3 a) { return StpF3_U3((StpU3_F3(a) >> StpU3_(1)) + StpU3_(0x1fbc4639)); }
        StpF4 StpPrxLoSqrtF4(StpF4 a) { return StpF4_U4((StpU4_F4(a) >> StpU4_(1)) + StpU4_(0x1fbc4639)); }
    #endif // STP_BUG_PRX
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_BUG_PRX
        StpF1 StpPrxLoRcpF1(StpF1 a) { return StpRcpF1(a); }
        StpF2 StpPrxLoRcpF2(StpF2 a) { return StpRcpF2(a); }
        StpF3 StpPrxLoRcpF3(StpF3 a) { return StpRcpF3(a); }
        StpF4 StpPrxLoRcpF4(StpF4 a) { return StpRcpF4(a); }
        StpF1 StpPrxMedRcpF1(StpF1 a) { return StpRcpF1(a); }
        StpF3 StpPrxMedRcpF3(StpF3 a) { return StpRcpF3(a); }
    #else
        StpF1 StpPrxLoRcpF1(StpF1 a) { return StpF1_U1(StpU1_(0x7ef07ebb) - StpU1_F1(a)); }
        StpF2 StpPrxLoRcpF2(StpF2 a) { return StpF2_U2(StpU2_(0x7ef07ebb) - StpU2_F2(a)); }
        StpF3 StpPrxLoRcpF3(StpF3 a) { return StpF3_U3(StpU3_(0x7ef07ebb) - StpU3_F3(a)); }
        StpF4 StpPrxLoRcpF4(StpF4 a) { return StpF4_U4(StpU4_(0x7ef07ebb) - StpU4_F4(a)); }
        StpF1 StpPrxMedRcpF1(StpF1 a) { StpF1 b = StpF1_U1(StpU1_(0x7ef19fff) - StpU1_F1(a));
            return b * (-b * a + StpF1_(2.0)); }
        StpF3 StpPrxMedRcpF3(StpF3 a) { StpF3 b = StpF3_U3(StpU3_(0x7ef19fff) - StpU3_F3(a));
            return b * (-b * a + StpF3_(2.0)); }
    #endif // STP_BUG_PRX
//------------------------------------------------------------------------------------------------------------------------------
    #define STP_STATIC /* */
    #define StpInF2 in StpF2
    #define StpInF4 in StpF4
    #define StpInOutU4 inout StpU4
    #define StpOutF2 out StpF2
    #define StpVarF2 StpF2
#endif // defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL))
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && defined(STP_MEDIUM)
    #if STP_BUG_SAT_INF
        // Defined if unable to use the fast path because of problem related to saturating +/- INF.
        StpMF1 StpGtZeroMF1(StpMF1 x) { return (x > StpMF1_(0.0)) ? StpMF1_(1.0) : StpMF1_(0.0); }
        StpMF3 StpGtZeroMF3(StpMF3 x) { return StpMF3(StpGtZeroMF1(x.r), StpGtZeroMF1(x.g), StpGtZeroMF1(x.b)); }
        StpMF4 StpGtZeroMF4(StpMF4 x) { return StpMF4(StpGtZeroMF1(x.r), StpGtZeroMF1(x.g),
            StpGtZeroMF1(x.b), StpGtZeroMF1(x.a)); }
        StpMF1 StpSignedMF1(StpMF1 x) { return (x < StpMF1_(0.0)) ? StpMF1_(1.0) : StpMF1_(0.0); }
        StpMF2 StpSignedMF2(StpMF2 x) { return StpMF2(StpSignedMF1(x.r), StpSignedMF1(x.g)); }
        StpMF3 StpSignedMF3(StpMF3 x) { return StpMF3(StpSignedMF1(x.r), StpSignedMF1(x.g), StpSignedMF1(x.b)); }
        StpMF4 StpSignedMF4(StpMF4 x) { return StpMF4(StpSignedMF1(x.r), StpSignedMF1(x.g),
            StpSignedMF1(x.b), StpSignedMF1(x.a)); }
    #elif STP_BUG_SAT
        // Defined if compiler factors out saturation incorrectly.
        #define STP_INFN_MF StpMF1(StpF1_U1(0xff800000u))
        #define STP_INFP_MF StpMF1(StpF1_U1(0x7f800000u))
        StpMF1 StpGtZeroMF1(StpMF1 x) { return max(min(x * StpMF1_(STP_INFP_MF), StpMF1_(1.0)), StpMF1_(0.0)); }
        StpMF3 StpGtZeroMF3(StpMF3 x) { return max(min(x * StpMF3_(STP_INFP_MF), StpMF3_(1.0)), StpMF3_(0.0)); }
        StpMF4 StpGtZeroMF4(StpMF4 x) { return max(min(x * StpMF4_(STP_INFP_MF), StpMF4_(1.0)), StpMF4_(0.0)); }
        StpMF1 StpSignedMF1(StpMF1 x) { return max(min(x * StpMF1_(STP_INFN_MF), StpMF1_(1.0)), StpMF1_(0.0)); }
        StpMF2 StpSignedMF2(StpMF2 x) { return max(min(x * StpMF2_(STP_INFN_MF), StpMF2_(1.0)), StpMF2_(0.0)); }
        StpMF3 StpSignedMF3(StpMF3 x) { return max(min(x * StpMF3_(STP_INFN_MF), StpMF3_(1.0)), StpMF3_(0.0)); }
        StpMF4 StpSignedMF4(StpMF4 x) { return max(min(x * StpMF4_(STP_INFN_MF), StpMF4_(1.0)), StpMF4_(0.0)); }
    #else
        // Using +/- INF typecast down to medium precision.
        #define STP_INFN_MF StpMF1(StpF1_U1(0xff800000u))
        #define STP_INFP_MF StpMF1(StpF1_U1(0x7f800000u))
        StpMF1 StpGtZeroMF1(StpMF1 x) { return StpSatMF1(x * StpMF1_(STP_INFP_MF)); }
        StpMF3 StpGtZeroMF3(StpMF3 x) { return StpSatMF3(x * StpMF3_(STP_INFP_MF)); }
        StpMF4 StpGtZeroMF4(StpMF4 x) { return StpSatMF4(x * StpMF4_(STP_INFP_MF)); }
        StpMF1 StpSignedMF1(StpMF1 x) { return StpSatMF1(x * StpMF1_(STP_INFN_MF)); }
        StpMF2 StpSignedMF2(StpMF2 x) { return StpSatMF2(x * StpMF2_(STP_INFN_MF)); }
        StpMF3 StpSignedMF3(StpMF3 x) { return StpSatMF3(x * StpMF3_(STP_INFN_MF)); }
        StpMF4 StpSignedMF4(StpMF4 x) { return StpSatMF4(x * StpMF4_(STP_INFN_MF)); }
    #endif // STP_BUG_SAT_INF
//------------------------------------------------------------------------------------------------------------------------------
    // Unable to use the approximations due to not knowing what the type actually is.
    StpMF1 StpPrxLoSqrtMF1(StpMF1 a) { return sqrt(a); }
    StpMF3 StpPrxLoSqrtMF3(StpMF3 a) { return sqrt(a); }
    StpMF4 StpPrxLoSqrtMF4(StpMF4 a) { return sqrt(a); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpPrxLoRcpMF1(StpMF1 a) { return StpRcpMF1(a); }
    StpMF2 StpPrxLoRcpMF2(StpMF2 a) { return StpRcpMF2(a); }
    StpMF3 StpPrxLoRcpMF3(StpMF3 a) { return StpRcpMF3(a); }
    StpMF4 StpPrxLoRcpMF4(StpMF4 a) { return StpRcpMF4(a); }
    StpMF1 StpPrxMedRcpMF1(StpMF1 a) { return StpRcpMF1(a); }
    StpMF3 StpPrxMedRcpMF3(StpMF3 a) { return StpRcpMF3(a); }
#endif // defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && defined(STP_MEDIUM)
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && (!defined(STP_MEDIUM))
    // Same types so just use the full precision version.
    #define StpGtZeroMF1(a) StpGtZeroF1(a)
    #define StpGtZeroMF2(a) StpGtZeroF2(a)
    #define StpGtZeroMF3(a) StpGtZeroF3(a)
    #define StpGtZeroMF4(a) StpGtZeroF4(a)
    #define StpSignedMF1(a) StpSignedF1(a)
    #define StpSignedMF2(a) StpSignedF2(a)
    #define StpSignedMF3(a) StpSignedF3(a)
    #define StpSignedMF4(a) StpSignedF4(a)
//------------------------------------------------------------------------------------------------------------------------------
    // The medium precision types are the same as the full precision so use the full precision approximations.
    #define StpPrxLoSqrtMF1(a) StpPrxLoSqrtF1(a)
    #define StpPrxLoSqrtMF3(a) StpPrxLoSqrtF3(a)
    #define StpPrxLoSqrtMF4(a) StpPrxLoSqrtF4(a)
//------------------------------------------------------------------------------------------------------------------------------
    #define StpPrxLoRcpMF1(a) StpPrxLoRcpF1(a)
    #define StpPrxLoRcpMF2(a) StpPrxLoRcpF2(a)
    #define StpPrxLoRcpMF3(a) StpPrxLoRcpF3(a)
    #define StpPrxLoRcpMF4(a) StpPrxLoRcpF4(a)
    #define StpPrxMedRcpMF1(a) StpPrxMedRcpF1(a)
    #define StpPrxMedRcpMF3(a) StpPrxMedRcpF3(a)
#endif // defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && (!defined(STP_MEDIUM))
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && defined(STP_16BIT)
    StpH1 StpExp2H1(StpH1 x) { return exp2(x); }
    StpH1 StpLog2H1(StpH1 x) { return log2(x); }
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_BUG_ALIAS16
        // Use 32-bit aliasing to build the +/-INF, then typecast to 16-bit.
        #define STP_INFN_H StpH1(StpF1_U1(0xff800000u))
        #define STP_INFP_H StpH1(StpF1_U1(0x7f800000u))
    #else
        #define STP_INFN_H StpH1_W1(StpW1_(0xfc00))
        #define STP_INFP_H StpH1_W1(StpW1_(0x7c00))
    #endif // STP_BUG_ALIAS16
    #if STP_BUG_SAT_INF
        StpH1 StpGtZeroH1(StpH1 x) { return (x > StpH1_(0.0)) ? StpH1_(1.0) : StpH1_(0.0); }
        StpH2 StpGtZeroH2(StpH2 x) { return StpH2(StpGtZeroH1(x.r), StpGtZeroH1(x.g)); }
        StpH3 StpGtZeroH3(StpH3 x) { return StpH3(StpGtZeroH1(x.r), StpGtZeroH1(x.g), StpGtZeroH1(x.b)); }
        StpH4 StpGtZeroH4(StpH4 x) { return StpH4(StpGtZeroH1(x.r), StpGtZeroH1(x.g),
            StpGtZeroH1(x.b), StpGtZeroH1(x.a)); }
        StpH1 StpSignedH1(StpH1 x) { return (x < StpH1_(0.0)) ? StpH1_(1.0) : StpH1_(0.0); }
        StpH2 StpSignedH2(StpH2 x) { return StpH2(StpSignedH1(x.r), StpSignedH1(x.g)); }
        StpH3 StpSignedH3(StpH3 x) { return StpH3(StpSignedH1(x.r), StpSignedH1(x.g), StpSignedH1(x.b)); }
        StpH4 StpSignedH4(StpH4 x) { return StpH4(StpSignedH1(x.r), StpSignedH1(x.g),
            StpSignedH1(x.b), StpSignedH1(x.a)); }
    #elif STP_BUG_SAT
        StpH1 StpGtZeroH1(StpH1 x) { return max(min(x * StpH1_(STP_INFP_H), StpH1_(1.0)), StpH1_(0.0)); }
        StpH2 StpGtZeroH2(StpH2 x) { return max(min(x * StpH2_(STP_INFP_H), StpH2_(1.0)), StpH2_(0.0)); }
        StpH3 StpGtZeroH3(StpH3 x) { return max(min(x * StpH3_(STP_INFP_H), StpH3_(1.0)), StpH3_(0.0)); }
        StpH4 StpGtZeroH4(StpH4 x) { return max(min(x * StpH4_(STP_INFP_H), StpH4_(1.0)), StpH4_(0.0)); }
        StpH1 StpSignedH1(StpH1 x) { return max(min(x * StpH1_(STP_INFN_H), StpH1_(1.0)), StpH1_(0.0)); }
        StpH2 StpSignedH2(StpH2 x) { return max(min(x * StpH2_(STP_INFN_H), StpH2_(1.0)), StpH2_(0.0)); }
        StpH3 StpSignedH3(StpH3 x) { return max(min(x * StpH3_(STP_INFN_H), StpH3_(1.0)), StpH3_(0.0)); }
        StpH4 StpSignedH4(StpH4 x) { return max(min(x * StpH4_(STP_INFN_H), StpH4_(1.0)), StpH4_(0.0)); }
    #else
        StpH1 StpGtZeroH1(StpH1 x) { return StpSatH1(x * StpH1_(STP_INFP_H)); }
        StpH2 StpGtZeroH2(StpH2 x) { return StpSatH2(x * StpH2_(STP_INFP_H)); }
        StpH3 StpGtZeroH3(StpH3 x) { return StpSatH3(x * StpH3_(STP_INFP_H)); }
        StpH4 StpGtZeroH4(StpH4 x) { return StpSatH4(x * StpH4_(STP_INFP_H)); }
        StpH1 StpSignedH1(StpH1 x) { return StpSatH1(x * StpH1_(STP_INFN_H)); }
        StpH2 StpSignedH2(StpH2 x) { return StpSatH2(x * StpH2_(STP_INFN_H)); }
        StpH3 StpSignedH3(StpH3 x) { return StpSatH3(x * StpH3_(STP_INFN_H)); }
        StpH4 StpSignedH4(StpH4 x) { return StpSatH4(x * StpH4_(STP_INFN_H)); }
    #endif // STP_BUG_SAT_INF
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_BUG_PRX
        StpH1 StpPrxLoSqrtH1(StpH1 a) { return sqrt(a); }
        StpH3 StpPrxLoSqrtH3(StpH3 a) { return sqrt(a); }
        StpH4 StpPrxLoSqrtH4(StpH4 a) { return sqrt(a); }
    #else
        StpH1 StpPrxLoSqrtH1(StpH1 a) { return StpH1_W1((StpW1_H1(a) >> StpW1_(1)) + StpW1_(0x1de2)); }
        StpH3 StpPrxLoSqrtH3(StpH3 a) { return StpH3_W3((StpW3_H3(a) >> StpW3_(1)) + StpW3_(0x1de2)); }
        StpH4 StpPrxLoSqrtH4(StpH4 a) { return StpH4_W4((StpW4_H4(a) >> StpW4_(1)) + StpW4_(0x1de2)); }
    #endif // STP_BUG_PRX
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_BUG_PRX
        StpH1 StpPrxLoRcpH1(StpH1 a) { return StpRcpH1(a); }
        StpH2 StpPrxLoRcpH2(StpH2 a) { return StpRcpH2(a); }
        StpH3 StpPrxLoRcpH3(StpH3 a) { return StpRcpH3(a); }
        StpH4 StpPrxLoRcpH4(StpH4 a) { return StpRcpH4(a); }
        StpH1 StpPrxMedRcpH1(StpH1 a) { return StpRcpH1(a); }
        StpH3 StpPrxMedRcpH3(StpH3 a) { return StpRcpH3(a); }
    #else
        // Note this will create denormals.
        //  MAPPING
        //  -------
        //   +INF (7c00) -> -61568
        //  65504 (7bff) -> -61600
        //  30800 (7785) -> NaN
        //  30784 (7784) -> 0 ........ (any input larger than 30784 will break)
        //  1     (3c00) -> 0.9395 ... (so not energy preserving for 1.0)
        //  0     (0000) -> 30784
        StpH1 StpPrxLoRcpH1(StpH1 a) { return StpH1_W1(StpW1_(0x7784) - StpW1_H1(a)); }
        StpH2 StpPrxLoRcpH2(StpH2 a) { return StpH2_W2(StpW2_(0x7784) - StpW2_H2(a)); }
        StpH3 StpPrxLoRcpH3(StpH3 a) { return StpH3_W3(StpW3_(0x7784) - StpW3_H3(a)); }
        StpH4 StpPrxLoRcpH4(StpH4 a) { return StpH4_W4(StpW4_(0x7784) - StpW4_H4(a)); }
        // Anything larger than 30928 will break in this function.
        StpH1 StpPrxMedRcpH1(StpH1 a) { StpH1 b = StpH1_W1(StpW1_(0x778d) - StpW1_H1(a));
            return b * (-b * a + StpH1_(2.0)); }
        StpH3 StpPrxMedRcpH3(StpH3 a) { StpH3 b = StpH3_W3(StpW3_(0x778d) - StpW3_H3(a));
            return b * (-b * a + StpH3_(2.0)); }
    #endif // STP_BUG_PRX
#endif // defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && defined(STP_16BIT)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                        LANE REMAPPING
//==============================================================================================================================
#if defined(STP_GPU)
    // More complex remap which is safe for both portability (different wave sizes up to 128) and for 2D wave reductions.
    //  6543210
    //  =======
    //  ..xx..x
    //  yy..yy.
    // Details,
    //  LANE TO 8x16 MAPPING
    //  ====================
    //  00 01 08 09 10 11 18 19
    //  02 03 0a 0b 12 13 1a 1b
    //  04 05 0c 0d 14 15 1c 1d
    //  06 07 0e 0f 16 17 1e 1f
    //  20 21 28 29 30 31 38 39
    //  22 23 2a 2b 32 33 3a 3b
    //  24 25 2c 2d 34 35 3c 3d
    //  26 27 2e 2f 36 37 3e 3f
    //  .......................
    //  ... repeat the 8x8 ....
    //  .... pattern, but .....
    //  .... for 40 to 7f .....
    //  .......................
    StpU2 StpRmp8x16U2(StpU1 a) {
        // Note the BFIs used for MSBs have "strange offsets" due to leaving space for the LSB bits replaced in the BFI.
        return StpU2(StpBfiMskU1(StpBfeU1(a, 2u, 3u), a, 1u),
            StpBfiMskU1(StpBfeU1(a, 3u, 4u), StpBfeU1(a, 1u, 2u), 2u)); }
#endif // defined(STP_GPU)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                     PRESETS (DON'T CHANGE)
//==============================================================================================================================
// High-end mobile.
#if (STP_TAA_Q == 0)
    #define STP_GEAA_P 1
    #define STP_GEAA_SUBPIX (2.0 / 16.0)
    #define STP_TAA_PEN_F1 (1.0 / 4.0)
    #define STP_TAA_PEN_F0 (1.0 / 2.0)
    #define STP_TAA_PEN_W (1.0 / 2.0)
    #define STP_TAA_PRX_LANCZOS 1
    #define STP_TAA_PRX_LANCZOS_DERING 0
#endif // (STP_TAA_Q == 0)
//------------------------------------------------------------------------------------------------------------------------------
// Desktop.
#if (STP_TAA_Q == 1)
    #define STP_GEAA_P 3
    #define STP_GEAA_SUBPIX (2.0 / 16.0)
    #define STP_TAA_PEN_F1 (1.0 / 4.0)
    #define STP_TAA_PEN_F0 (1.0 / 2.0)
    #define STP_TAA_PEN_W (1.0 / 2.0)
    #define STP_TAA_PRX_LANCZOS 2
    #define STP_TAA_PRX_LANCZOS_DERING 1
#endif // (STP_TAA_Q == 1)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                               INTERNAL TUNING (DON'T CHANGE)
//==============================================================================================================================
// Limits on anti-flicker weighting, tuning for range and precision challenges of FP16.
#define STP_ANTI_MAX 8192.0
// Using '1/8192' provides known problems on some platforms that are 16-bit precision challenged.
#define STP_ANTI_MIN (1.0 / 4096.0)
//------------------------------------------------------------------------------------------------------------------------------
#define STP_DITHER_DEPTH 1
#define STP_DITHER_MOTION 1
//------------------------------------------------------------------------------------------------------------------------------
// Ratios for luma in a gamma space, using BT.709 luma.
#define STP_LUMA_R 0.2126
#define STP_LUMA_G 0.7152
#define STP_LUMA_B 0.0722
#define STP_LUMA STP_LUMA_R, STP_LUMA_G, STP_LUMA_B
//------------------------------------------------------------------------------------------------------------------------------
// Maximum frames of feedback.
#define STP_FRAME_MAX 32.0
//------------------------------------------------------------------------------------------------------------------------------
// Control the min (motion match), and max (no motion match), in units of pixels.
// Settings of {max=1.0} won't work for 8x area scaling (trailing edge smears).
// Setting too tight won't have enough slop for motion matching (motion match easily fails, leading to loss of detail).
// If STP_PAT_MOT_MAX is too big, it will look like edges expand (or float) during change of motion.
#define STP_PAT_MOT_MIN (1.0 / 16.0)
#define STP_PAT_MOT_MAX (1.0 / 8.0)
// Computed constants.
#define STP_PAT_MOT_ADD (STP_PAT_MOT_MIN * STP_PAT_MOT_MIN)
#define STP_PAT_MOT_AMP (1.0 / (STP_PAT_MOT_MAX * STP_PAT_MOT_MAX - STP_PAT_MOT_ADD))
//------------------------------------------------------------------------------------------------------------------------------
// Larger numbers ghost more, smaller numbers flicker more.
#define STP_PAT_DEMOIRE 64.0
// Increase for less ghosting, decrease for more ghosting.
#define STP_PAT_SENSITIVITY (2.0 / 16.0)
// Amount to scale up sensitivity on responsive. Lower numbers ghost more, higher flicker more.
#define STP_PAT_RESPONSIVE 16.0
// Minimum neighborhood (defaults to 1/32 of maximum value of neighborhood to allow some noise).
#define STP_PAT_NE_MIN (1.0 / 32.0)
//------------------------------------------------------------------------------------------------------------------------------
// {0} = default lowest dilation (higher chance of slight trailing ghost, but less overall flicker)
// {1} = expand a little (higher cost)
// {2} = expand by too much (a lot more cost, more flicker, perhaps less trailing ghost)
// In practice it's dilation and motion match threshold (PAT_MOT) which results in the final {flicker, ghost} tradeoff.
#define STP_SAFE_DILATE 1
//------------------------------------------------------------------------------------------------------------------------------
// Adjusts the point at which spatial-only weights blend up and anti-flicker fully takes over.
#define STP_TAA_SAA (1.0 / 2.0)
// De-weight pixel contribution for chopped corner.
#define STP_TAA_TRI_MASK_AVOID (1.0 / 8192.0)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                      JITTER LOCATIONS
//------------------------------------------------------------------------------------------------------------------------------
// STP is now using Halton(2,3).
//==============================================================================================================================
// Generate jitter amount given frame index.
STP_STATIC void StpJit(StpOutF2 p, StpU1 frame) {
    // TODO: This function isn't used inside Unity, if ever this is used the implementation should be added here.
    p[0] = StpF1_(0.0);
    p[1] = StpF1_(0.0); }
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                     PARABOLIC {SIN,COS}
//==============================================================================================================================
#if defined(STP_GPU)
    // Input is {-1 to 1} representing {0 to 2 pi}, output is {-1/4 to 1/4} representing {-1 to 1}.
    void StpPSinF2(inout StpF2 p) { p = p * abs(p) - p; }
    // This is used to dither position of gather4 fetch for nearest motion vector to remove nearest artifacts when scaling.
    // Input 'p.x' is {0 to 1} representing {0 to 2 pi}, output is {-1/4 to 1/4} representing {-1 to 1}.
    void StpPSinCosF(inout StpF2 p) { p.y = StpFractF1(p.x + StpF1_(0.25)); p = p * StpF2_(2.0) - StpF2_(1.0); StpPSinF2(p); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpPSinMF2(inout StpMF2 p) { p = p * abs(p) - p; }
    void StpPSinCosMF(inout StpMF2 p) {
        p.y = StpFractMF1(p.x + StpMF1_(0.25));
        p = p * StpMF2_(2.0) - StpMF2_(1.0); StpPSinMF2(p); }
#endif // defined(STP_GPU)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_16BIT)
    void StpPSinH2(inout StpH2 p) { p = p * abs(p) - p; }
    void StpPSinCosH(inout StpH2 p) { p.y = StpFractH1(p.x + StpH1_(0.25)); p = p * StpH2_(2.0) - StpH2_(1.0); StpPSinH2(p); }
#endif // defined(STP_GPU) && defined(STP_16BIT)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                        DEPTH ENCODING
//------------------------------------------------------------------------------------------------------------------------------
// Using a log2() based encoding, takes {0 to inf} to {0 to 1}.
//  log2(k.x*z)*k.y
// Where
//  k.x = 1/near ............ (so that k0*z is 1 when z=near)
//  k.y = 1/log2(k.x*far) ... (so that output is {0 to 1} ranged)
//------------------------------------------------------------------------------------------------------------------------------
// And the inverse
//  exp2(x*k.x)*k.y
// Where
//  k.x = log2(far/near)
//  k.y = near
//==============================================================================================================================
#if defined(STP_GPU)
    // Build the constants, based on near and far planes.
    // The 'far' is where anything more distant clamps to 1.0.
    StpF2 StpZCon(StpF1 near, StpF1 far) {
        StpF2 k;
        k.x = StpRcpF1(near);
        k.y = StpRcpF1(log2(k.x * far));
        return k; }
//------------------------------------------------------------------------------------------------------------------------------
    // Where 'k' is generated by StpZCon().
    StpF1 StpZPack(StpF1 z, StpF2 k, StpF1 dit) {
        #if (STP_DITHER_DEPTH == 0)
            return StpSatF1(log2(k.x * z) * k.y);
        #endif // (STP_DITHER_DEPTH == 0)
        #if (STP_DITHER_DEPTH == 1)
            // Fast linearly incorrect dither for 10-bit.
            return StpSatF1(log2(k.x * z) * k.y + dit * StpF1_(1.0 / 1024.0) - StpF1_(0.5 / 1024.0));
        #endif // (STP_DITHER_DEPTH == 1)
    }
//==============================================================================================================================
    // Build the constants, based on near and far planes.
    // The 'far' is where anything more distant clamps to 1.0.
    StpF2 StpZUnCon(StpF1 near, StpF1 far) {
        StpF2 k;
        k.x = log2(far * StpRcpF1(near));
        k.y = near;
        return k; }
//------------------------------------------------------------------------------------------------------------------------------
    // Where 'k' is generated by StpZUnCon().
    StpF1 StpZUnpack(StpF1 x, StpF2 k) { return exp2(x * k.x) * k.y; }
#endif // defined(STP_GPU)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                            STATIC GEOMETRY MOTION FORWARD PROJECTION
//==============================================================================================================================
// This is a separate section simply for documentation.
// This logic must be computed in 32-bit precision (in theory).
//------------------------------------------------------------------------------------------------------------------------------
// MOTION MATCH NOTES
// ==================
// - The 'position - motion' is the reprojected position.
// - Where {0 to 1} is no motion to a screen in motion.
// - Motion check works with a differential vector '((motionPrior - motionCurrent) * kC)'.
// - For static forward projection it will be '((motionPrior*0.5 - motionCurrent) * kC)'.
//    - Due to motionPrior being in {-1 to 1} NDC instead of {0 to 1} for screen.
// - Working with motion vector differences to avoid complexity with jitter.
//------------------------------------------------------------------------------------------------------------------------------
// MOTION VECTOR NOTES
// ===================
// - 'reprojection = position - motion'
// - 'reprojection + motion = position'
// - 'motion = position - reprojection'
// - So motion points forward.
//------------------------------------------------------------------------------------------------------------------------------
// FORWARD PROJECTION LOGIC
// ========================
// HAVE INPUT {0 TO 1} SCREEN POSITION
//  xy
// GET XY INTO {-1 TO 1} NDC [2 FMA, CANNOT FACTOR, NEED AT END]
//  x=x*2-1
//  y=y*2-1
// HAVE INPUT {0 TO INF} DEPTH
//  z
// GET FROM {XY NDC, DEPTH} TO 3D VIEW POSITION [4 FMA]
//  xx=x*((z*g+h)/a) ... xx=x*(z*(g/a)+(h/a)) ... xx=x*(z*k0+k1)
//  yy=y*((z*g+h)/b) ... yy=y*(z*(g/b)+(h/b)) ... yy=y*(z*k2+k3)
// TRANSFORM TO NEW VIEW
//  xxx=xx*i+yy*j+z*k+l
//  yyy=xx*m+yy*n+z*o+p
//  zzz=xx*q+yy*r+z*s+t
// PROJECTION [9 FMA]
//  xxxx=xxx*a ..... xxxx=xx*(i*a)+yy*(j*a)+z*(k*a)+(l*a) ..... xxxx=xx*k4+yy*k5+z*k6+k7
//  yyyy=yyy*b ..... yyyy=xx*(m*b)+yy*(n*b)+z*(o*b)+(p*b) ..... yyyy=xx*k8+yy*k9+z*kA+kB
//  wwww=zzz*g+h ... wwww=xx*(q*g)+yy*(r*g)+z*(s*g)+(t*g+h) ... wwww=xx*kC+yy*kD+z*kE+kF
// PERSPECTIVE DIVIDE [1 RCP]
//  xxxxx=xxxx/wwww
//  yyyyy=yyyy/wwww
// SUBTRACT TO GET 2X MOTION [2 FMA]
//  u=xxxxx-x ... u=xxxx*(1/wwww)-x
//  v=yyyyy-y ... v=yyyy*(1/wwww)-y
// CONSTANTS (SEE BELOW FOR MEANING OF VARIABLES)
//  k0=g/a ... Constants {a,b,c,d,g,h} for prior projection
//  k1=h/a
//  k2=g/b
//  k3=h/b
//  k4=i*a ... Constants {a,b,c,d,g,h} for next projection
//  k5=j*a
//  k6=k*a
//  k7=l*a
//  k8=m*b
//  k9=n*b
//  kA=o*b
//  kB=p*b
//  kC=q*g
//  kD=r*g
//  kE=s*g
//  kF=t*g+h
//------------------------------------------------------------------------------------------------------------------------------
// BACKWARD PROJECTION LOGIC
// =========================
//  This starts from '3D VIEW POSITION' of 'FORWARD PROJECTION LOGIC', but with different constants.
// TRANSFORM TO NEW VIEW
//  xxx=xx*i+yy*j+z*k+l
//  yyy=xx*m+yy*n+z*o+p
//  zzz=xx*q+yy*r+z*s+t
// PROJECTION [9 FMA]
//  xxxx=xxx*a ..... xxxx=xx*(i*a)+yy*(j*a)+z*(k*a)+(l*a) ..... xxxx=xx*kG+yy*kH+z*kI+kJ
//  yyyy=yyy*b ..... yyyy=xx*(m*b)+yy*(n*b)+z*(o*b)+(p*b) ..... yyyy=xx*kK+yy*kL+z*kM+kN
//  wwww=zzz*g+h ... wwww=xx*(q*g)+yy*(r*g)+z*(s*g)+(t*g+h) ... wwww=xx*kO+yy*kP+z*kQ+kR
// PERSPECTIVE DIVIDE [1 RCP]
//  xxxxx=xxxx/wwww
//  yyyyy=yyyy/wwww
// SUBTRACT TO GET 2X MOTION [2 FMA]
//  u=xxxxx-x ... u=xxxx*(1/wwww)-x
//  v=yyyyy-y ... v=yyyy*(1/wwww)-y
// CONSTANTS (SEE BELOW FOR MEANING OF VARIABLES)
//  kG=i*a ... Constants {a,b,c,d,g,h} for previous prior projection, and {i,j,k,l,m,n,o,p,q,r,s,t} for prior back projection
//  kH=j*a
//  kI=k*a
//  kJ=l*a
//  kK=m*b
//  kL=n*b
//  kM=o*b
//  kN=p*b
//  kO=q*g
//  kP=r*g
//  kQ=s*g
//  kR=t*g+h
//==============================================================================================================================
// GET FROM {0 TO 1} TO {-1 TO 1}
// ==============================
// - Get to NDC for {x,y}
//   X:=x*2-1
//   Y:=y*2-1
//------------------------------------------------------------------------------------------------------------------------------
// FORWARD VIEW
// ============
// - Using 12 values
//    X:=x*i+y*j+z*k+l
//    Y:=x*m+y*n+z*o+p
//    Z:=x*q+y*r+z*s+t
//    W:=1
//     i j k l
//     m n o p
//     q r s t
//     0 0 0 1
//------------------------------------------------------------------------------------------------------------------------------
// PROJECTIONS
// ===========
// - INPUTS
//    n ... near plane z
//    f ... far plane z
// - DX ORTHO PROJECTION
//    c:=1/(f-n)
//    d:=-n/(f-n)
//    X:=x*a
//    Y:=y*b
//    Z:=z*c+d ... (w=1 on input)
//    W:=1
//     a 0 0 0
//     0 b 0 0
//     0 0 c d
//     0 0 0 1
// - DX PERSPECTIVE PROJECTION (LEFT HANDED)
//    c:=f/(f-n)
//    d:=-(f*n)/(f-n)
//    X:=x*a
//    Y:=y*b
//    Z:=z*c+d ... (w=1 on input)
//    W:=z
//     a 0 0 0
//     0 b 0 0
//     0 0 c d
//     0 0 1 0 ... (note DX allows the 1 to be non-one)
// - DX PERSPECTIVE PROJECTION REVERSED FOR BETTER PRECISION (LEFT HANDED)
//    c:=-n/(f-n)
//    d:=(f*n)/(f-n)
//    X:=x*a
//    Y:=y*b
//    Z:=z*c+d ... (w=1 on input)
//    W:=z
//     a 0 0 0
//     0 b 0 0
//     0 0 c d
//     0 0 1 0
// - DX PERSPECTIVE PROJECTION REVERSED WITH INF FAR (LEFT HANDED)
//    X:=x*a
//    Y:=y*b
//    Z:=n ... (w=1 on input)
//    W:=z
//    a 0 0 0
//    0 b 0 0
//    0 0 0 n
//    0 0 1 0
// - GL PERSPECTIVE PROJECTION
//    c:=-(f+n)/(f-n)
//    d:=-(2fn)/(f-n)
//    X:=x*a
//    Y:=y*b
//    Z:=z*c+d ... (w=1 on input)
//    W:=z
//     a 0  0 0
//     0 b  0 0
//     0 0  c d
//     0 0 -1 0
// - GENERALIZED (WILL DO ANYTHING)
//    X:=x*a
//    Y:=y*b
//    Z:=z*c+d ... (w=1 on input)
//    W:=z*g+h
//     a 0 0 0
//     0 b 0 0
//     0 0 c d
//     0 0 g h
//------------------------------------------------------------------------------------------------------------------------------
// PROJECTED TO NDC
// ================
// - Ignoring viewport transform
//    X:=x/w
//    Y:=y/w
//    Z:=z/w
//    W:=1/w
// - Inverse
//    x=X*w
//    y=Y*w
//==============================================================================================================================
//                                             MODIFICATIONS FOR COMPLEX PROJECTIONS
//------------------------------------------------------------------------------------------------------------------------------
// Since this worked out to just 2 more FMAs and 2 more constants, decided not to create a shader permutation.
//==============================================================================================================================
// COMPLEX PROJECTION
// ==================
// - GL PERSPECTIVE PROJECTION - WITH Z BASED {X,Y} MODIFICATIONS
//    c:=-(F+N)/(F-N)
//    d:=-(2FN)/(F-N)
//    X:=x*a + z*e
//    Y:=y*b + z*f
//    Z:=z*c+d ... (w=1 on input)
//    W:=z
//     a 0  e 0
//     0 b  f 0
//     0 0  c d
//     0 0 -1 0
// - GENERALIZED (WILL DO ANYTHING) - WITH Z BASED {X,Y} MODIFICATIONS
//    X:=x*a + z*e
//    Y:=y*b + z*f
//    Z:=z*c+d ... (w=1 on input)
//    W:=z*g+h
//     a 0 e 0
//     0 b f 0
//     0 0 c d
//     0 0 g h
// - INVERSE GIVEN 'z'
//    X:=x*a + z*e
//    Y:=y*b + z*f
//    X - z*e:=x*a
//    Y - z*f:=y*b
//    X/a - z*e/a:=x
//    Y/b - z*f/b:=y
//------------------------------------------------------------------------------------------------------------------------------
// FORWARD PROJECTION LOGIC
// ========================
// HAVE INPUT {0 TO 1} SCREEN POSITION
//  xy
// GET XY INTO {-1 TO 1} NDC [2 FMA, CANNOT FACTOR, NEED AT END]
//  x=x*2-1
//  y=y*2-1
// HAVE INPUT {0 TO INF} DEPTH
//  z
// GET FROM {XY NDC, DEPTH} TO 3D VIEW POSITION [4 FMA]
//   ... have {X,Y,z}
//   ... xx=(x*(z*g+h))*(1/a) + z*(e/a)
//   ... yy=(y*(z*g+h))*(1/b) + z*(f/b)
//   ... xx=x*((z*g+h)/a) + z*(e/a)
//   ... yy=y*((z*g+h)/b) + z*(f/b)
//   ... xx=x*(z*(g/a)+(h/a)) + z*(e/a)
//   ... yy=y*(z*(g/b)+(h/b)) + z*(f/b)
//  xx=x*(z*k0+k1)+z*k2
//  yy=y*(z*k3+k4)+z*k5
// TRANSFORM TO NEW VIEW
//  xxx=xx*i+yy*j+z*k+l
//  yyy=xx*m+yy*n+z*o+p
//  zzz=xx*q+yy*r+z*s+t
// PROJECTION [9 FMA]
//  xxxx=xxx*a+zzz*e
//   ... xxxx=xx*(i*a)+yy*(j*a)+z*(k*a)+(l*a) + xx*(q*e)+yy*(r*e)+z*(s*e)+(t*e)
//   ... xxxx=xx*k6+yy*k7+z*k8+k9
//  yyyy=yyy*b+zzz*f
//   ... yyyy=xx*(m*b)+yy*(n*b)+z*(o*b)+(p*b) + xx*(q*f)+yy*(r*f)+z*(s*f)+(t*f)
//   ... yyyy=xx*kA+yy*kB+z*kC+kD
//  wwww=zzz*g+h
//   ... wwww=xx*(q*g)+yy*(r*g)+z*(s*g)+(t*g+h)
//   ... wwww=xx*kE+yy*kF+z*kG+kH
// PERSPECTIVE DIVIDE [1 RCP]
//  xxxxx=xxxx/wwww
//  yyyyy=yyyy/wwww
// SUBTRACT TO GET 2X MOTION [2 FMA]
//  u=xxxxx-x ... u=xxxx*(1/wwww)-x
//  v=yyyyy-y ... v=yyyy*(1/wwww)-y
// CONSTANTS (SEE BELOW FOR MEANING OF VARIABLES)
//  k0=g/a ... Constants {a,b,c,d,e,f,g,h} for prior projection
//  k1=h/a
//  k2=e/a
//  k3=g/b
//  k4=h/b
//  k5=f/b
//  k6=(i*a)+(q*e) ... Constants {a,b,c,d,e,f,g,h} for next projection
//  k7=(j*a)+(r*e)
//  k8=(k*a)+(s*e)
//  k9=(l*a)+(t*e)
//  kA=(m*b)+(q*f)
//  kB=(n*b)+(r*f)
//  kC=(o*b)+(s*f)
//  kD=(p*b)+(t*f)
//  kE=q*g
//  kF=r*g
//  kG=s*g
//  kH=t*g+h
//------------------------------------------------------------------------------------------------------------------------------
// BACKWARD PROJECTION LOGIC
// =========================
//  This starts from '3D VIEW POSITION' of 'FORWARD PROJECTION LOGIC', but with different constants.
// TRANSFORM TO NEW VIEW
//  xxx=xx*i+yy*j+z*k+l
//  yyy=xx*m+yy*n+z*o+p
//  zzz=xx*q+yy*r+z*s+t
// PROJECTION [9 FMA]
//  xxxx=xxx*a+zzz*e
//   ..... xxxx=xx*(i*a)+yy*(j*a)+z*(k*a)+(l*a) + xx*(q*e)+yy*(r*e)+z*(s*e)+(t*e)
//   ..... xxxx=xx*kI+yy*kJ+z*kK+kJL
//  yyyy=yyy*b+zzz*f
//   ..... yyyy=xx*(m*b)+yy*(n*b)+z*(o*b)+(p*b) + xx*(q*f)+yy*(r*f)+z*(s*f)+(t*f)
//   ..... yyyy=xx*kM+yy*kN+z*kO+kP
//  wwww=zzz*g+h
//   ... wwww=xx*(q*g)+yy*(r*g)+z*(s*g)+(t*g+h)
//   ... wwww=xx*kQ+yy*kR+z*kS+kT
// PERSPECTIVE DIVIDE [1 RCP]
//  xxxxx=xxxx/wwww
//  yyyyy=yyyy/wwww
// SUBTRACT TO GET 2X MOTION [2 FMA]
//  u=xxxxx-x ... u=xxxx*(1/wwww)-x
//  v=yyyyy-y ... v=yyyy*(1/wwww)-y
// CONSTANTS (SEE BELOW FOR MEANING OF VARIABLES)
//   ... Constants {a,b,c,d,e,f,g,h} for previous prior projection
//   ... Constants {i,j,k,l,m,n,o,p,q,r,s,t} for prior back projection
//  kI=(i*a)+(q*e)
//  kJ=(j*a)+(r*e)
//  kK=(k*a)+(s*e)
//  kL=(l*a)+(t*e)
//  kM=(m*b)+(q*f)
//  kN=(n*b)+(r*f)
//  kO=(o*b)+(s*f)
//  kP=(p*b)+(t*f)
//  kQ=q*g
//  kR=r*g
//  kS=s*g
//  kT=t*g+h
//==============================================================================================================================
#if defined(STP_GPU)
    // Generates forward {-1 to 1} NDC forward projection vectors given (from prior frame),
    //  p .... {0 to 1} screen position
    //  z .... {0 to INF} depth
    //  m .... {0 to 1} prior motion vector
    // The results are approximately corrected for dynamic motion.
    // This takes 'dynamicMotion = priorMotionVector - priorStaticGeometryBackprojection'
    // Then adds that estimate of dynamic motion to the static geometry forward projection vector.
    StpF2 StpFor(StpF2 p, StpF1 z, StpF2 m, StpF1 kMotionMatch,
    StpF4 k0123, StpF4 k4567, StpF4 k89AB, StpF4 kCDEF, StpF4 kGHIJ, StpF4 kKLMN, StpF4 kOPQR, StpF2 kST,
    out StpF2 bugF, out StpF2 bugD){
        // Implements the logic described above in the comments.
        p = p * StpF2_(2.0) - StpF2_(1.0);
        StpF2 q;
        q.x = p.x * (z * k0123.x + k0123.y) + (z * k0123.z);
        q.y = p.y * (z * k0123.w + k4567.x) + (z * k4567.y);
        StpF3 v;
        v.x = q.x * k4567.z + q.y * k4567.w + z * k89AB.x + k89AB.y;
        v.y = q.x * k89AB.z + q.y * k89AB.w + z * kCDEF.x + kCDEF.y;
        v.z = q.x * kCDEF.z + q.y * kCDEF.w + z * kGHIJ.x + kGHIJ.y;
        v.z = StpRcpF1(v.z);
        StpF3 v2;
        v2.x = q.x * kGHIJ.z + q.y * kGHIJ.w + z * kKLMN.x + kKLMN.y;
        v2.y = q.x * kKLMN.z + q.y * kKLMN.w + z * kOPQR.x + kOPQR.y;
        v2.z = q.x * kOPQR.z + q.y * kOPQR.w + z *   kST.x +   kST.y;
        v2.z = StpRcpF1(v2.z);
        // Motion vector points forward (to estimated position in next frame).
        // Negative motion vector points back to where the pixel was in the prior frame.
        // Motion vector is {0 to 1} for one screen, but this logic is {-1 to 1} based (hence a 2x scaling).
        bugF = (v.xy * StpF2_(v.z) - p); // Static forward estimate.
        bugD = ((StpF2_(2.0) * m) - (p - v2.xy * StpF2_(v2.z))) * StpF2_(kMotionMatch); // Dynamic estimate.
        return bugF + bugD; }
#endif // defined(STP_GPU)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                    MOTION VECTOR ENCODING
//------------------------------------------------------------------------------------------------------------------------------
// {MSB 10-bit depth, LSB {11,11}-bit motion with sqrt() encoding}
// Motion is encoding in sqrt() space.
//------------------------------------------------------------------------------------------------------------------------------
// 11111111111111110000000000000000
// fedcba9876543210fedcba9876543210
// ================================
// zzzzzzzzzz...................... 10-bit encoded z
// ..........yyyyyyyyyyy........... 11-bit {-1 to <1} y encoded in gamma 2.0 (sqrt)
// .....................xxxxxxxxxxx 11-bit {-1 to <1} x encoded in gamma 2.0 (sqrt)
//------------------------------------------------------------------------------------------------------------------------------
// The 32-bit path is 8 ops to decode {x,y}.
//------------------------------------------------------------------------------------------------------------------------------
// There once was a 16-bit path which takes 6 ops to decode (bit extra because ABS isn't free).
//     hhhhhhhhhhhhhhhhllllllllllllllll
//     ================================
//     zzzzzzzzzzyyyyyyyyyyyxxxxxxxxxxx  input
//     zzzzzyyyyyyyyyyyxxxxxxxxxxx00000  << 5
//     00000yyyyyyyyyyyxxxxxxxxxxx00000  & 0x7FFFFFF
//     00000yyyyyyyyyyy00000xxxxxxxxxxx  >> 5 (for 16-bit LSB only)
// This gets 11-bit integers which perfectly alias lowest non-denormal and denormals of FP16.
// Can scale by '16384' and subtract 1 to decompress without a CVT.
//==============================================================================================================================
#if defined(STP_GPU)
    // The 'z' comes in {0 to 1}.
    // This depends on 'v' ranging inside and including {-1 to 1}.
    StpU1 StpMvPack(StpF1 z, StpF2 v, StpF1 dit) {
        // {-1 to 1} linear to gamma 2.0 {-1 to 1}
        #if STP_DITHER_MOTION
           v = StpCpySgnF2(StpSatF2(sqrt(abs(v)) + StpF2_(dit * StpF1_(1.0 / 1024.0) - StpF1_(0.5 / 1024.0))), v);
        #else
           v = StpCpySgnF2(sqrt(abs(v)), v);
        #endif
        // Limit to {-1024/1024 to 1023/1024}.
        v = min(v, StpF2_(1023.0/1024.0));
        // Encode to 11-bit with zero at center of one step.
        v = v * StpF2_(1024.0) + StpF2_(1024.0);
        // Pack.
        return (StpU1(z * StpF1(1023.0)) << StpU1(22)) + (StpU1(v.y) << StpU1(11)) + StpU1(v.x); }
//------------------------------------------------------------------------------------------------------------------------------
    // Unpacks all.
    void StpMvUnpack(out StpF1 z, out StpF2 v, StpU1 i) {
        StpU1 iz = StpBfeU1(i, 22u, 10u);
        StpU1 iy = StpBfeU1(i, 11u, 11u);
        StpU1 ix = StpBfeU1(i, 0, 11u);
        z = StpF1(iz) * StpF1_(1.0 / 1023.0);
        v.y = StpF1(iy) * StpF1_(1.0 / 1024.0) + StpF1_(-1.0);
        v.x = StpF1(ix) * StpF1_(1.0 / 1024.0) + StpF1_(-1.0);
        v *= abs(v); }
//------------------------------------------------------------------------------------------------------------------------------
    // Unpack just velocity.
    void StpMvUnpackV(out StpF2 v, StpU1 i) {
        StpU1 iy = StpBfeU1(i, 11u, 11u);
        StpU1 ix = StpBfeU1(i, 0, 11u);
        v.y = StpF1(iy) * StpF1_(1.0 / 1024.0) + StpF1_(-1.0);
        v.x = StpF1(ix) * StpF1_(1.0 / 1024.0) + StpF1_(-1.0);
        v *= abs(v); }
#endif // defined(STP_GPU)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                       COLOR CONVERSION
//==============================================================================================================================
#if defined(STP_GPU)
    // Scaling in the reversible tonemapper (should be >= 1).
    // Getting too close to 1.0 will result in luma inversions in highly saturated content in the oldest algorithm.
    // Using 4.0 or ideally 8.0 is recommended.
    #define STP_SAT 4.0
#endif // defined(STP_GPU)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_32BIT)
    void StpToneF1(inout StpF1 x) { StpF1 y = StpRcpF1(StpF1_(STP_SAT) + x); x = StpSatF1(x * StpF1_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    // Reversible tonemapper.
    void StpToneF3(inout StpF3 x) {
        StpF1 y = StpRcpF1(StpF1_(STP_SAT) + StpMax3F1(x.r, x.g, x.b));
        x = StpSatF3(x * StpF3_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpToneInvF3(inout StpF3 x) {
        StpF1 y = StpRcpF1(
            //               |-----| <- Using 32768.0 causes problems in Unity with bloom on at least some platforms.
            //               |     |    So output maximum is 16384 for StpToneInvF3().
            max(StpF1_(1.0 / 16384.0), StpSatF1(StpF1_(1.0 / STP_SAT) - StpMax3F1(x.r, x.g, x.b) * StpF1_(1.0 / STP_SAT))));
        x *= StpF3_(y); }
//------------------------------------------------------------------------------------------------------------------------------
    // This is currently unused but left in for reference.
    // Convert LDR RGB to Gamma 2.0 RGB {0 to 1}.
    // This is for storage to 8-bit.
    // This is temporal dithered.
    // Unoptimized logic (for reference).
    //     StpF3 n = sqrt(c);
    //     n = floor(n * StpF3_(255.0)) * StpF3_(1.0 / 255.0);
    //     StpF3 a = n * n;
    //     StpF3 b = n + StpF3_(1.0 / 255.0); b = b * b;
    //     // Ratio of 'a' to 'b' required to produce 'c'.
    //     StpF3 r = (c - b) * StpRcpF3(a - b);
    //     // Use the ratio as a cutoff to choose 'a' or 'b'.
    //     c = StpSatF3(n + StpGtZeroF3(StpF3_(dit) - r) * StpF3_(1.0 / 255.0));
    // Optimized from 57 to 42 clks on GCN.
    StpF3 StpRgbGamDit8F3(StpF3 c, StpF1 dit) {
        StpF3 n = sqrt(c);
        n = floor(n * StpF3_(255.0)) * StpF3_(1.0 / 255.0);
        StpF3 a = n * n;
        StpF3 b = n + StpF3_(1.0 / 255.0);
        c = StpSatF3(n + StpGtZeroF3(StpF3_(dit) * (b * b - a) - (b * b - c)) * StpF3_(1.0 / 255.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    // This is currently unused but left in for reference.
    // Version for 10-bit for feedback.
    StpF3 StpRgbGamDit10F3(StpF3 c, StpF1 dit) {
        StpF3 n = sqrt(c);
        n = floor(n * StpF3_(1023.0)) * StpF3_(1.0 / 1023.0);
        StpF3 a = n * n;
        StpF3 b = n + StpF3_(1.0 / 1023.0);
        c = StpSatF3(n + StpGtZeroF3(StpF3_(dit) * (b * b - a) - (b * b - c)) * StpF3_(1.0 / 1023.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    // Can use this function to convert feedback back to color.
    void StpFeed2ClrF(inout StpF3 c) {
        c *= c;
        #if (STP_POSTMAP == 0)
            StpToneInvF3(c.rgb);
        #endif
    }
#endif // defined(STP_GPU) && defined(STP_32BIT)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_32BIT)
    void StpToneMF1(inout StpMF1 x) { StpMF1 y = StpRcpMF1(StpMF1_(STP_SAT) + x); x = StpSatMF1(x * StpMF1_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpToneMF3(inout StpMF3 x) {
        StpMF1 y = StpRcpMF1(StpMF1_(STP_SAT) + StpMax3MF1(x.r, x.g, x.b));
        x = StpSatMF3(x * StpMF3_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpToneInvMF3(inout StpMF3 x) {
        StpMF1 y = StpRcpMF1(
            max(StpMF1_(1.0 / 16384.0), StpSatMF1(StpMF1_(1.0 / STP_SAT) -
                StpMax3MF1(x.r, x.g, x.b) * StpMF1_(1.0 / STP_SAT))));
        x *= StpMF3_(y); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF3 StpRgbGamDit8MF3(StpMF3 c, StpMF1 dit) {
        StpMF3 n = sqrt(c);
        n = floor(n * StpMF3_(255.0)) * StpMF3_(1.0 / 255.0);
        StpMF3 a = n * n;
        StpMF3 b = n + StpMF3_(1.0 / 255.0);
        c = StpSatMF3(n + StpGtZeroMF3(StpMF3_(dit) * (b * b - a) - (b * b - c)) * StpMF3_(1.0 / 255.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF3 StpRgbGamDit10MF3(StpMF3 c, StpMF1 dit) {
        StpMF3 n = sqrt(c);
        n = floor(n * StpMF3_(1023.0)) * StpMF3_(1.0 / 1023.0);
        StpMF3 a = n * n;
        StpMF3 b = n + StpMF3_(1.0 / 1023.0);
        c = StpSatMF3(n + StpGtZeroMF3(StpMF3_(dit) * (b * b - a) - (b * b - c)) * StpMF3_(1.0 / 1023.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    void StpFeed2ClrMF(inout StpMF3 c) {
        c *= c;
        #if (STP_POSTMAP == 0)
            StpToneInvMF3(c.rgb);
        #endif
    }
#endif // defined(STP_GPU) && defined(STP_32BIT)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_16BIT)
    void StpToneH1(inout StpH1 x) { StpH1 y = StpRcpH1(StpH1_(STP_SAT) + x); x = StpSatH1(x * StpH1_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpToneH3(inout StpH3 x) {
        StpH1 y = StpRcpH1(StpH1_(STP_SAT) + StpMax3H1(x.r, x.g, x.b));
        x = StpSatH3(x * StpH3_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpToneInvH3(inout StpH3 x) {
        StpH1 y = StpRcpH1(
            max(StpH1_(1.0 / 16384.0), StpSatH1(StpH1_(1.0 / STP_SAT) - StpMax3H1(x.r, x.g, x.b) * StpH1_(1.0 / STP_SAT))));
        x *= StpH3_(y); }
//------------------------------------------------------------------------------------------------------------------------------
    StpH3 StpRgbGamDit8H3(StpH3 c, StpH1 dit) {
        StpH3 n = sqrt(c);
        n = floor(n * StpH3_(255.0)) * StpH3_(1.0 / 255.0);
        StpH3 a = n * n;
        StpH3 b = n + StpH3_(1.0 / 255.0);
        c = StpSatH3(n + StpGtZeroH3(StpH3_(dit) * (b * b - a) - (b * b - c)) * StpH3_(1.0 / 255.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    StpH3 StpRgbGamDit10H3(StpH3 c, StpH1 dit) {
        StpH3 n = sqrt(c);
        n = floor(n * StpH3_(1023.0)) * StpH3_(1.0 / 1023.0);
        StpH3 a = n * n;
        StpH3 b = n + StpH3_(1.0 / 1023.0);
        c = StpSatH3(n + StpGtZeroH3(StpH3_(dit) * (b * b - a) - (b * b - c)) * StpH3_(1.0 / 1023.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    void StpFeed2ClrH(inout StpH3 c) {
        c *= c;
        #if (STP_POSTMAP == 0)
            StpToneInvH3(c.rgb);
        #endif
    }
#endif // defined(STP_GPU) && defined(STP_16BIT)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                   COLOR CONVERSION TOOLS
//------------------------------------------------------------------------------------------------------------------------------
// Some platforms do not have a hardware sRGB image store (requires manual conversion).
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_32BIT)
    StpF3 StpLinearToSrgbF3(StpF3 c) {
        StpF3 j = StpF3(0.0031308 * 12.92, 12.92, 1.0 / 2.4); StpF2 k = StpF2(1.055, -0.055);
        return clamp(j.xxx, c * j.yyy, pow(c, j.zzz) * k.xxx + k.yyy); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF3 StpLinearToSrgbMF3(StpMF3 c) {
        StpMF3 j = StpMF3(0.0031308 * 12.92, 12.92, 1.0 / 2.4); StpMF2 k = StpMF2(1.055, -0.055);
        return clamp(j.xxx, c * j.yyy, pow(c, j.zzz) * k.xxx + k.yyy); }
#endif // defined(STP_GPU) && defined(STP_32BIT)
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_16BIT)
    StpH3 StpLinearToSrgbH3(StpH3 c) {
        StpH3 j = StpH3(0.0031308 * 12.92, 12.92, 1.0 / 2.4); StpH2 k = StpH2(1.055, -0.055);
        return clamp(j.xxx, c * j.yyy, pow(c, j.zzz) * k.xxx + k.yyy); }
#endif // defined(STP_GPU) && defined(STP_16BIT)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                         DEBUG COMMON
//==============================================================================================================================
#if defined(STP_GPU) && STP_BUG
    void StpBugF(StpU3 p, StpF4 c);
#endif // defined(STP_GPU) && STP_BUG
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                     CONSTANT GENERATION
//==============================================================================================================================
STP_STATIC void StpDilCon(
// Generated constants.
StpInOutU4 con0,
// Current image resolution in pixels.
StpInF2 imgC) {
    // StpF2 kRcpR := 4/size of current input image in pixels.
    con0[0] = StpU1_F1(StpF1_(4.0) / imgC[0]);
    con0[1] = StpU1_F1(StpF1_(4.0) / imgC[1]);
    // StpU2 kR := size/4 of the current input image in pixels.
    // Used for pass merging (DIL and SAA), since convergence is 1/16 area of input, must check position.
    con0[2] = StpU1_(StpU1_(imgC[0]) >> StpU1_(2));
    con0[3] = StpU1_(StpU1_(imgC[1]) >> StpU1_(2)); }
//==============================================================================================================================
STP_STATIC void StpPatCon(
// Generated constants.
StpInOutU4 con0,
StpInOutU4 con1,
StpInOutU4 con2,
StpInOutU4 con3,
StpInOutU4 con4,
StpInOutU4 con5,
StpInOutU4 con6,
StpInOutU4 con7,
StpInOutU4 con8,
StpInOutU4 con9,
StpInOutU4 conA,
StpInOutU4 conB,
StpInOutU4 conC,
// Linear depth near plane for log2 depth encoding.
StpF1 near,
// Linear depth far plane for log2 depth encoding.
StpF1 far,
// Frame count for current frame (sets jitter).
StpU1 frame,
// Current image resolution in pixels.
StpInF2 imgC,
// Prior image resolution in pixels.
StpInF2 imgP,
// Feedback (aka output) resolution in pixels.
StpInF2 imgF,
// Ratio of 'currentFrameTime/priorFrameTime'.
StpF1 motionMatch,
// Projection matrix data {a,b,c,d,e,f,g,h}.
// This is used to do static geometry forward projection.
//  a 0 e 0
//  0 b f 0
//  0 0 c d
//  0 0 g h
// For reference, an DX ortho projection would be,
//  a 0 e 0
//  0 b f 0
//  0 0 c d
//  0 0 0 1
// And a DX, left handed perspective projection would be,
//  a 0 e 0
//  0 b f 0
//  0 0 c d ... c := F/(F-N), d := -(F*N)/(F-N)
//  0 0 1 0
// Previous prior projection.
StpInF4 prjPrvABEF,
StpInF4 prjPrvCDGH,
// Prior projection.
StpInF4 prjPriABEF,
StpInF4 prjPriCDGH,
// Current projection (the difference enables changing zoom).
StpInF4 prjCurABEF,
StpInF4 prjCurCDGH,
// Forward viewspace transform.
// Transform prior 3D view position into current 3D view position.
// This is used to do static geometry forward projection.
//  X := x*i + y*j +z*k +l
//  Y := x*m + y*n +z*o +p
//  Z := x*q + y*r +z*s +t
//  W := 1
//   i j k l
//   m n o p
//   q r s t
//   0 0 0 1
StpInF4 forIJKL,
StpInF4 forMNOP,
StpInF4 forQRST,
// Prior frame backward viewspace transform.
// Transform prior 3D view position into previous-prior 3D view position.
// This is used to 'fix' static geometry forward projection for dynamic motion.
//  X := x*i + y*j +z*k +l
//  Y := x*m + y*n +z*o +p
//  Z := x*q + y*r +z*s +t
//  W := 1
//   i j k l
//   m n o p
//   q r s t
//   0 0 0 1
StpInF4 bckIJKL,
StpInF4 bckMNOP,
StpInF4 bckQRST) {
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kRcpC := 1.0 / size of current input image in pixels.
    con0[0] = StpU1_F1(StpF1_(1.0) / imgC[0]);
    con0[1] = StpU1_F1(StpF1_(1.0) / imgC[1]);
    // StpF2 kHalfRcpC := 0.5 / size of current input image in pixels.
    con0[2] = StpU1_F1(StpF1_(0.5) / imgC[0]);
    con0[3] = StpU1_F1(StpF1_(0.5) / imgC[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // Grab jitter for current and prior frames.
    StpVarF2 jitP;
    StpVarF2 jitC;
    StpJit(jitP, frame - StpU1_(1));
    StpJit(jitC, frame);
    // StpF2 kJitCRcpCUnjitPRcpP := Map current into prior frame.
    con1[0] = StpU1_F1(jitC[0] / imgC[0] - jitP[0] / imgP[0]);
    con1[1] = StpU1_F1(jitC[1] / imgC[1] - jitP[1] / imgP[1]);
    // StpF2 kJitCRcpC := Take {0 to 1} position in current image, and map back to {0 to 1} position in feedback (removes jitter).
    con1[2] = StpU1_F1(jitC[0] / imgC[0]);
    con1[3] = StpU1_F1(jitC[1] / imgC[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kF := size of feedback (aka output) in pixels.
    con2[0] = StpU1_F1(imgF[0]);
    con2[1] = StpU1_F1(imgF[1]);
    // StpF2 kDepth := Copied logic from StpZCon().
    StpF1 k0 = StpRcpF1(near);
    StpF1 k1 = StpRcpF1(StpLog2F1(k0 * far));
    con2[2] = StpU1_F1(k0);
    con2[3] = StpU1_F1(k1);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF4 kOS := Scale and bias to check for out of bounds (and kill feedback).
    // Scaled and biased output needs to {-1 out of bounds, >-1 in bounds, <1 in bounds, 1 out of bounds}.
    StpVarF2 s;
    // Undo 'pM' scaling, and multiply by 2 (as this needs to be -1 to 1 at edge of acceptable reprojection).
    s[0] = StpF1_(2.0);
    s[1] = StpF1_(2.0);
    // Scaling to push outside safe reprojection over 1.
    s[0] *= imgP[0] / (imgP[0] + StpF1_(4.0));
    s[1] *= imgP[1] / (imgP[1] + StpF1_(4.0));
    con3[0] = StpU1_F1(s[0]);
    con3[1] = StpU1_F1(s[1]);
    // Factor out subtracting off the mid point scaled by the multiply term.
    con3[2] = StpU1_F1(StpF1_(-0.5) * s[0]);
    con3[3] = StpU1_F1(StpF1_(-0.5) * s[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kUnDepth := Copied logic from StpZUnCon().
    con4[0] = StpU1_F1(StpLog2F1(far * StpRcpF1(near)));
    con4[1] = StpU1_F1(near);
    // kMotionMatch
    con4[2] = StpU1_F1(motionMatch);
    // Unused for now.
    con4[3] = StpU1_(0);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kC := Size of current input image in pixels.
    con5[0] = StpU1_F1(imgC[0]);
    con5[1] = StpU1_F1(imgC[1]);
    // kST
    con5[2] = StpU1_F1(bckQRST.z * prjPrvCDGH.z);
    con5[3] = StpU1_F1(bckQRST.w * prjPrvCDGH.z + prjPrvCDGH.w);
//------------------------------------------------------------------------------------------------------------------------------
    // See header docs in "STATIC GEOMETRY MOTION FORWARD PROJECTION".
    // k0123
    con6[0] = StpU1_F1(prjPriCDGH.z / prjPriABEF.x);
    con6[1] = StpU1_F1(prjPriCDGH.w / prjPriABEF.x);
    con6[2] = StpU1_F1(prjPriABEF.z / prjPriABEF.x);
    con6[3] = StpU1_F1(prjPriCDGH.z / prjPriABEF.y);
    // k4567
    con7[0] = StpU1_F1(prjPriCDGH.w / prjPriABEF.y);
    con7[1] = StpU1_F1(prjPriABEF.w / prjPriABEF.y);
    con7[2] = StpU1_F1(forIJKL.x * prjCurABEF.x + forQRST.x * prjCurABEF.z);
    con7[3] = StpU1_F1(forIJKL.y * prjCurABEF.x + forQRST.y * prjCurABEF.z);
    // k89AB
    con8[0] = StpU1_F1(forIJKL.z * prjCurABEF.x + forQRST.z * prjCurABEF.z);
    con8[1] = StpU1_F1(forIJKL.w * prjCurABEF.x + forQRST.w * prjCurABEF.z);
    con8[2] = StpU1_F1(forMNOP.x * prjCurABEF.y + forQRST.x * prjCurABEF.w);
    con8[3] = StpU1_F1(forMNOP.y * prjCurABEF.y + forQRST.y * prjCurABEF.w);
    // kCDEF
    con9[0] = StpU1_F1(forMNOP.z * prjCurABEF.y + forQRST.z * prjCurABEF.w);
    con9[1] = StpU1_F1(forMNOP.w * prjCurABEF.y + forQRST.w * prjCurABEF.w);
    con9[2] = StpU1_F1(forQRST.x * prjCurCDGH.z);
    con9[3] = StpU1_F1(forQRST.y * prjCurCDGH.z);
    // kGHIJ
    conA[0] = StpU1_F1(forQRST.z * prjCurCDGH.z);
    conA[1] = StpU1_F1(forQRST.w * prjCurCDGH.z + prjCurCDGH.w);
    conA[2] = StpU1_F1(bckIJKL.x * prjPrvABEF.x + bckQRST.x * prjPrvABEF.z);
    conA[3] = StpU1_F1(bckIJKL.y * prjPrvABEF.x + bckQRST.y * prjPrvABEF.z);
    // kKLMN
    conB[0] = StpU1_F1(bckIJKL.z * prjPrvABEF.x + bckQRST.z * prjPrvABEF.z);
    conB[1] = StpU1_F1(bckIJKL.w * prjPrvABEF.x + bckQRST.w * prjPrvABEF.z);
    conB[2] = StpU1_F1(bckMNOP.x * prjPrvABEF.y + bckQRST.x * prjPrvABEF.w);
    conB[3] = StpU1_F1(bckMNOP.y * prjPrvABEF.y + bckQRST.y * prjPrvABEF.w);
    // kOPQR
    conC[0] = StpU1_F1(bckMNOP.z * prjPrvABEF.y + bckQRST.z * prjPrvABEF.w);
    conC[1] = StpU1_F1(bckMNOP.w * prjPrvABEF.y + bckQRST.w * prjPrvABEF.w);
    conC[2] = StpU1_F1(bckQRST.x * prjPrvCDGH.z);
    conC[3] = StpU1_F1(bckQRST.y * prjPrvCDGH.z);}
//==============================================================================================================================
STP_STATIC void StpTaaCon(
// Generated constants.
StpInOutU4 con0,
StpInOutU4 con1,
StpInOutU4 con2,
StpInOutU4 con3,
// Amount of grain {0 = maximum, >0 is amount of stops less of grain}.
StpF1 grain,
// Frame count for current frame (sets jitter).
StpU1 frame,
// Current image resolution in pixels.
StpInF2 imgC,
// Feedback (aka output) resolution in pixels.
StpInF2 imgF) {
//------------------------------------------------------------------------------------------------------------------------------
    // Grab jitter for current frame.
    StpVarF2 jitC;
    StpJit(jitC, frame);
//------------------------------------------------------------------------------------------------------------------------------
    // Conversion from integer pix position to center pix float pixel position in image for current input.
    //  xy := multiply term (M) --- Scale by 1/imgF to get to {0 to 1}.
    //  zw := addition term (A) --- Add 0.5*M to get to center of pixel, then subtract jitC to undo jitter.
    // StpF2 kCRcpF.
    con0[0] = StpU1_F1(imgC[0] / imgF[0]);
    con0[1] = StpU1_F1(imgC[1] / imgF[1]);
    // StpF2 kHalfCRcpFUnjitC.
    con0[2] = StpU1_F1(StpF1_(0.5) * imgC[0] / imgF[0] - jitC[0]);
    con0[3] = StpU1_F1(StpF1_(0.5) * imgC[1] / imgF[1] - jitC[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kRcpC := 1/size of current input image in pixels.
    con1[0] = StpU1_F1(StpF1_(1.0) / imgC[0]);
    con1[1] = StpU1_F1(StpF1_(1.0) / imgC[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kRcpF := 1/size of feedback image (aka output) in pixels.
    con1[2] = StpU1_F1(StpF1_(1.0) / imgF[0]);
    con1[3] = StpU1_F1(StpF1_(1.0) / imgF[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kHalfRcpF := 0.5/size of feedback image (aka output) in pixels.
    con2[0] = StpU1_F1(StpF1_(0.5) / imgF[0]);
    con2[1] = StpU1_F1(StpF1_(0.5) / imgF[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // Conversion from a {0 to 1} position in current input to feedback.
    // StpH3 kJitCRcpC0 := jitC / image image size in pixels + {-0.5/size, +0.5/size} of current input image in pixels.
    con2[2] = StpU1_F1(jitC[0] / imgC[0] - StpF1_(0.5) / imgC[0]);
    con2[3] = StpU1_F1(jitC[1] / imgC[1] + StpF1_(0.5) / imgC[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kHalfRcpC := 0.5/size of current input image in pixels.
    con3[0] = StpU1_F1(StpF1_(0.5) / imgC[0]);
    con3[1] = StpU1_F1(StpF1_(0.5) / imgC[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kF := size of feedback image in pixels.
    con3[2] = StpU1_F1(imgF[0]);
    con3[3] = StpU1_F1(imgF[1]); }
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//
//                                                     PATTERN ENTRY POINT
//
//==============================================================================================================================
// See the packed 16-bit version for comments.
#if defined(STP_GPU) && defined(STP_32BIT) && defined(STP_PAT)
    void StpPat4x4MaxF8(StpMU1 i, inout StpF4 a, inout StpF4 b);
    void StpPat4x4SumF4(StpMU1 i, inout StpF4 a);
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpPatPriConF(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    StpF2 StpPatDatMotF(StpMU2 o);
    StpMF3 StpPatDatColF(StpMU2 o);
    StpF1 StpPatDatZF(StpMU2 o);
    StpF1 StpPatFixZF(StpF1 z);
    StpU1 StpPatDatRF(StpMU2 o);
    StpMF1 StpPatFixRF(StpU1 v);
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpPatDitF(StpMU2 o);
//------------------------------------------------------------------------------------------------------------------------------
    StpMF4 StpPatPriFedF(StpF2 p);
    StpMF4 StpPatPriFedR4F(StpF2 p);
    StpMF4 StpPatPriFedG4F(StpF2 p);
    StpMF4 StpPatPriFedB4F(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    StpMF2 StpPatPriLumF(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    StpU4 StpPatPriMot4F(StpF2 p);
    #if STP_MAX_MIN_UINT
        StpU1 StpPatPriMotMinF(StpF2 p);
    #endif // STP_MAX_MIN_UINT
    #if STP_OFFSETS
        StpU4 StpPatPriMot4OF(StpF2 p, StpI2 o);
        #if STP_MAX_MIN_UINT
            StpU1 StpPatPriMotMinOF(StpF2 p, StpI2 o);
        #endif // STP_MAX_MIN_UINT
    #endif // STP_OFFSETS
//------------------------------------------------------------------------------------------------------------------------------
    void StpPatStMotF(StpMU2 p, StpU1 v);
    void StpPatStColF(StpMU2 p, StpMF4 v);
    void StpPatStLumF(StpMU2 p, StpMF2 v);
    void StpPatStCnvF(StpMU2 p, StpMF1 v);
//==============================================================================================================================
    void StpPatF(
    StpMU1 lane,
    StpMU2 pp,
    StpU4 con0,
    StpU4 con1,
    StpU4 con2,
    StpU4 con3,
    StpU4 con4,
    StpU4 con5,
    StpU4 con6,
    StpU4 con7,
    StpU4 con8,
    StpU4 con9,
    StpU4 conA,
    StpU4 conB,
    StpU4 conC,
    StpU4 conD) {
//------------------------------------------------------------------------------------------------------------------------------
        StpMF4 rC;
        StpU1 rM;
        StpMF2 rL;
        StpMF1 rCnv;
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 kRcpC = StpF2_U2(con0.xy);
        StpF2 kHalfRcpC = StpF2_U2(con0.zw);
        StpF2 kJitCRcpCUnjitPRcpP = StpF2_U2(con1.xy);
        StpF2 kJitCRcpC = StpF2_U2(con1.zw);
        StpF2 kF = StpF2_U2(con2.xy);
        StpF4 kOS = StpF4_U4(con3);
        StpF2 kDepth = StpF2_U2(con2.zw);
        StpF2 kUnDepth = StpF2_U2(con4.xy);
        StpF1 kMotionMatch = StpF1_U1(con4.z);
        StpF2 kC = StpF2_U2(con5.xy);
        StpF4 k0123 = StpF4_U4(con6);
        StpF4 k4567 = StpF4_U4(con7);
        StpF4 k89AB = StpF4_U4(con8);
        StpF4 kCDEF = StpF4_U4(con9);
        StpF4 kGHIJ = StpF4_U4(conA);
        StpF4 kKLMN = StpF4_U4(conB);
        StpF4 kOPQR = StpF4_U4(conC);
        StpF2 kST = StpF2_U2(conD.xy);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 m = StpPatDatMotF(pp);
        StpMF1 d = StpPatDitF(pp);
        StpF1 zPre = StpPatDatZF(pp);
        StpMF3 c = StpPatDatColF(pp);
//==============================================================================================================================
//      DEPENDENT INLINE INPUT MOTION
//==============================================================================================================================
        StpF2 p = StpF2(pp) * kRcpC + kHalfRcpC;
//------------------------------------------------------------------------------------------------------------------------------
        // Check the streaming bandwidth limit.
        #if STP_BUG_BW_SOL
        {   StpMF2 lum2 = StpPatPriLumF(p);
            StpMF1 cnvPrev = StpPatPriConF(p);
            StpU4 mZVP4 = StpPatPriMot4F(p);
            StpU1 rPre = StpPatDatRF(p);
            StpMF3 f = StpPatPriFedF(p).rgb;
            StpF1 z = StpPatFixZF(zPre);
            StpMF1 r = StpPatFixRF(rPre);
            rC.rgb = StpMF3_(m.x) + StpMF3_(d.x) + c + StpMF3_(lum2.x) + StpMF3_(cnvPrev) + StpMF3(mZVP4.xyz) + f + StpMF3_(z+r);
            rC.a = StpMF1_(0.0);
            rL = rC.rg;
            rM = StpU1_(rC.r);
            rCnv = rC.r;
            StpPatStMotF(pp, rM);
            StpPatStLumF(pp, rL);
            StpPatStColF(pp, rC);
            StpPatStCnvF(pp, rCnv);
            return; }
        #endif // STP_BUG_BW_SOL
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 pM = (p - m);
        StpF2 pF = pM + kJitCRcpC;
              pM = pM + kJitCRcpCUnjitPRcpP;
//------------------------------------------------------------------------------------------------------------------------------
        StpMF2 lum2 = StpPatPriLumF(pM);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 cnvPrev = StpPatPriConF(pM);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_SAFE_DILATE == 2)
            #if STP_MAX_MIN_UINT
                StpU4 mZVP4;
                #if STP_OFFSETS
                    mZVP4.x = StpPatPriMotMinOF(pM, StpI2(-1, -1));
                    mZVP4.y = StpPatPriMotMinOF(pM, StpI2( 1, -1));
                    mZVP4.z = StpPatPriMotMinOF(pM, StpI2(-1,  1));
                    mZVP4.w = StpPatPriMotMinOF(pM, StpI2( 1,  1));
                #else // STP_OFFSETS
                    mZVP4.x = StpPatPriMotMinF(pM + StpF2(-kRcpC.x, -kRcpC.y));
                    mZVP4.y = StpPatPriMotMinF(pM + StpF2( kRcpC.x, -kRcpC.y));
                    mZVP4.z = StpPatPriMotMinF(pM + StpF2(-kRcpC.x,  kRcpC.y));
                    mZVP4.w = StpPatPriMotMinF(pM + StpF2( kRcpC.x,  kRcpC.y));
                #endif // ST_OFFSETS
            #else // STP_MAX_MIN_UINT
                #if STP_OFFSETS
                    StpU4 mZVP4_0 = StpPatPriMot4OF(pM, StpI2(-1, -1));
                    StpU4 mZVP4_1 = StpPatPriMot4OF(pM, StpI2( 1, -1));
                    StpU4 mZVP4_2 = StpPatPriMot4OF(pM, StpI2(-1,  1));
                    StpU4 mZVP4_3 = StpPatPriMot4OF(pM, StpI2( 1,  1));
                #else // STP_OFFSETS
                    StpU4 mZVP4_0 = StpPatPriMot4F(pM + StpF2(-kRcpC.x, -kRcpC.y));
                    StpU4 mZVP4_1 = StpPatPriMot4F(pM + StpF2( kRcpC.x, -kRcpC.y));
                    StpU4 mZVP4_2 = StpPatPriMot4F(pM + StpF2(-kRcpC.x,  kRcpC.y));
                    StpU4 mZVP4_3 = StpPatPriMot4F(pM + StpF2( kRcpC.x,  kRcpC.y));
                #endif // STP_OFFSETS
            #endif // STP_MAX_MIN_UINT
        #else // (STP_SAFE_DILATE == 2)
            StpU1 mZVPN;
            StpU4 mZVP2a = StpPatPriMot4F(pM - kHalfRcpC);
            StpU4 mZVP2b = StpPatPriMot4F(pM + kHalfRcpC);
            #if STP_MAX_MIN_UINT
                mZVPN = StpPatPriMotMinF(pM);
            #else // STP_MAX_MIN_UINT
                StpU4 mZVP4 = StpPatPriMot4F(pM);
            #endif // STP_MAX_MIN_UINT
        #endif // (STP_SAFE_DILATE == 2)
//------------------------------------------------------------------------------------------------------------------------------
        StpU1 rPre = StpPatDatRF(pp);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF4 f4R = StpPatPriFedR4F(pF);
        StpMF4 f4G = StpPatPriFedG4F(pF);
        StpMF4 f4B = StpPatPriFedB4F(pF);
        StpMF3 f = StpPatPriFedF(pF).rgb;
//==============================================================================================================================
//      DEPENDENT ON DITHER AND INLINE INPUT PARAMETERS
//==============================================================================================================================
        StpF1 dd = StpF1_(d);
        StpF1 z = StpPatFixZF(zPre);
        z = StpZPack(z, kDepth, dd);
        rM = StpMvPack(z, m, dd);
        StpPatStMotF(pp, rM);
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG
            // Pattern/Clipped Input Color
            { StpF4 bug = StpF4_(0.0);
                bug.rgb = sqrt(StpF3(c.rgb));
                bug.rgb = StpSatF3(bug.rgb + StpF3_(StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 0), bug); }
//------------------------------------------------------------------------------------------------------------------------------
            // Pattern/Log Input Depth
            { StpF4 bug = StpF4_(0.0);
                bug.rgb = StpF3_(StpSatF1(z + StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 1), bug); }
        #endif // STP_BUG
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_POSTMAP == 0)
            StpToneMF3(c);
        #endif // (STP_POSTMAP == 0)
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG
            // Pattern/Reversible Tonemapped Input Color
            { StpF4 bug = StpF4_(0.0);
                bug.rgb = sqrt(StpF3(c.rgb));
                bug.rgb = StpSatF3(bug.rgb + StpF3_(StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 2), bug); }
        #endif // STP_BUG
//------------------------------------------------------------------------------------------------------------------------------
        c = sqrt(c);
        rC.rgb = StpSatMF3(c + StpMF3_(d * StpMF1(1.0 / 1023.0) + StpMF1(-0.5 / 1023.0)));
//------------------------------------------------------------------------------------------------------------------------------
        rL.x = dot(c, StpMF3(STP_LUMA));
        rL.y = lum2.x;
        StpPatStLumF(pp, rL);
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG
            // Pattern/Shaped Absolute Input Motion
            { StpF4 bug = StpF4_(0.0);
                bug.b = sqrt(StpF1_(rL.x) * StpF1_(0.25));
                bug.rg = StpF2_(1.0) - exp2(abs(StpF2(m)) * StpF2_(-32.0));
                bug.rgb = StpSatF3(bug.rgb + StpF3_(StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 3), bug); }
        #endif // STP_BUG
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 moire = min(abs(rL.x - lum2.x), abs(lum2.x - lum2.y));
        moire *= StpMF1_(STP_PAT_DEMOIRE);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF4 xnyRG = StpMF4(c.r, -c.r, c.g, -c.g);
        StpMF4 xnyBC = StpMF4(c.b, -c.b, -cnvPrev, -cnvPrev);
        #if defined(STP_16BIT)
        #else // defined(STP_16BIT)
            // We convert to full precision floats here since the reductions work on 32-bit values.
            StpF4 xnyRGF = StpF4(xnyRG);
            StpF4 xnyBCF = StpF4(xnyBC);
            StpPat4x4MaxF8(lane, xnyRGF, xnyBCF);
            xnyRG = StpMF4(xnyRGF);
            xnyBC = StpMF4(xnyBCF);
        #endif // defined(STP_16BIT)
        cnvPrev = -xnyBC.z;
        StpMF3 ne = max(StpMF3_(STP_PAT_NE_MIN) * StpMF3(xnyRG.x, xnyRG.z, xnyBC.x),
                       StpMF3(xnyRG.x + xnyRG.y, xnyRG.z + xnyRG.w, xnyBC.x + xnyBC.y));
        StpMF1 ne1 = dot(ne, StpMF3(STP_LUMA));
//------------------------------------------------------------------------------------------------------------------------------
        cnvPrev = StpSatMF1(cnvPrev + StpMF1_(1.0 / STP_FRAME_MAX));
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 onXY = StpF2(pM.xy);
        onXY = onXY * kOS.xy + kOS.zw;
        StpF1 onS = StpSignedF1(max(abs(onXY.x), abs(onXY.y)) - StpF1_(1.0));
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG
            // Pattern/Motion Reprojection {R=Prior G=This Sqrt Luma Feedback Diff, B=Offscreen}
            { StpF4 bug = StpF4_(0.0);
                bug.g = StpF1_(abs(rL.x - lum2.x));
                bug.r = StpF1_(abs(lum2.x - lum2.y));
                bug.b = StpF1_(1.0) - StpF1_(onS);
                bug.rg = sqrt(bug.rg);
                bug.rgb = StpSatF3(bug.rgb + StpF3_(StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 4), bug); }
        #endif // STP_BUG
//==============================================================================================================================
//      DEPENDENT ON PRIOR {Z, MOTION}
//==============================================================================================================================
        #if (STP_SAFE_DILATE == 2)
            #if (STP_MAX_MIN_UINT == 0)
                StpU4 mZVP4 = min(StpMin3U4(mZVP4_0, mZVP4_1, mZVP4_2), mZVP4_3);
            #endif // (STP_MAX_MIN_UINT == 0)
            StpU1 mZVPN = min(StpMin3U1(mZVP4.x, mZVP4.y, mZVP4.z), mZVP4.w);
        #else // (STP_SAFE_DILATE == 2)
            #if (STP_MAX_MIN_UINT == 0)
                mZVPN = min(StpMin3U1(mZVP4.x, mZVP4.y, mZVP4.z), mZVP4.w);
            #endif // (STP_MAX_MIN_UINT == 0)
            #if STP_SAFE_DILATE
                mZVPN = StpMin3U1(StpMin3U1(mZVPN, mZVP2a.x, mZVP2a.z), mZVP2b.x, mZVP2b.z);
            #endif // STP_SAFE_DILATE
        #endif // (STP_SAFE_DILATE == 2)
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 mPN;
        StpF1 mZPN;
        StpMvUnpack(mZPN, mPN, mZVPN);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 mE;
        mE = sqrt(abs(m)) + StpF2_(1.0 / 256.0);
        mE = mE * mE - abs(m);
//------------------------------------------------------------------------------------------------------------------------------
        StpF1 sgZ = StpZUnpack(mZPN, kUnDepth);
        StpF2 bugF; StpF2 bugD;
        StpF2 sgM = StpFor(pM, sgZ, mPN, kMotionMatch, k0123, k4567, k89AB, kCDEF, kGHIJ, kKLMN, kOPQR, kST, bugF, bugD);
        sgM = StpSatF2(abs(sgM * StpF2_(0.5) - m) - mE) * kC;
        StpMF1 sgD = StpMF1(dot(sgM, sgM));
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 match = StpMF1_(1.0) - StpSatMF1(sgD * StpMF1_(STP_PAT_MOT_AMP) - StpMF1_(STP_PAT_MOT_ADD * STP_PAT_MOT_AMP));
        match *= StpMF1_(onS);
        rC.a = match;
        StpPatStColF(pp, rC);
//------------------------------------------------------------------------------------------------------------------------------
        moire = moire * match + StpMF1_(1.0 / 8192.0);
        moire = min(StpMF1_(1.0), ne1 * StpRcpMF1(moire));
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 tS = moire;
        StpMF1 r = StpPatFixRF(rPre);
        tS = tS * (StpMF1_(STP_PAT_RESPONSIVE) - r * StpMF1_(STP_PAT_RESPONSIVE)) + tS;
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG
            // Pattern/Sensitivity {G=No motion match, R=Responsive, B=Luma}
            { StpF4 bug = StpF4_(0.0);
                bug.g = StpF1_(1.0) - StpF1(match);
                bug.r = StpF1_(1.0) - StpF1(r);
                bug.b = StpF1_(rL.x);
                bug.rgb = StpSatF3(bug.rgb + StpF3_(StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 5), bug); }
        #endif // STP_BUG
//==============================================================================================================================
//      DEPENDENT ON FEEDBACK
//==============================================================================================================================
        StpMF4 t;
        t.rgb = c - f;
        t.a = dot(abs(t.rgb), StpMF3(STP_LUMA));
        StpMF4 t4R = f4R - StpMF4_(c.r);
        StpMF4 t4G = f4G - StpMF4_(c.g);
        StpMF4 t4B = f4B - StpMF4_(c.b);
        StpMF4 t4A = abs(t4R) * StpMF4_(STP_LUMA_R) + abs(t4G) * StpMF4_(STP_LUMA_G) + abs(t4B) * StpMF4_(STP_LUMA_B);
        t.a = StpMin3MF1(t.a, t4A.x, StpMin3MF1(t4A.y, t4A.z, t4A.w));
        if(t.a == t4A.x) t.rgb = StpMF3(t4R.x, t4G.x, t4B.x);
        if(t.a == t4A.y) t.rgb = StpMF3(t4R.y, t4G.y, t4B.y);
        if(t.a == t4A.z) t.rgb = StpMF3(t4R.z, t4G.z, t4B.z);
        if(t.a == t4A.w) t.rgb = StpMF3(t4R.w, t4G.w, t4B.w);
//------------------------------------------------------------------------------------------------------------------------------
        t.rgb *= StpMF3_(tS);
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_16BIT)
            StpPat4x4SumH4(lane, t);
        #else // defined(STP_16BIT)
            // We convert to full precision floats here since the reductions work on 32-bit values, and MF might be 16-bit.
            StpF4 tF = StpF4(t);
            StpPat4x4SumF4(lane, tF);
            t = StpMF4(tF);
        #endif // defined(STP_16BIT)
        t.rgb *= StpMF3_(STP_PAT_SENSITIVITY);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF3 bln3 = StpSatMF3(ne * StpRcpMF3(abs(t.rgb)));
        StpMF1 bln = StpMin3MF1(bln3.r, bln3.g, bln3.b);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 cnv = StpSatMF1(bln * StpRcpMF1(StpMF1_(STP_FRAME_MAX) - StpMF1_(STP_FRAME_MAX) * bln));
//------------------------------------------------------------------------------------------------------------------------------
        cnv = StpSatMF1(cnv - StpMF1_(1.0 / STP_FRAME_MAX));
        rCnv = min(cnv, cnvPrev);
        StpPatStCnvF(pp, rCnv); }
#endif // defined(STP_GPU) && defined(STP_32BIT) && defined(STP_PAT)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                         16-BIT PATH
//==============================================================================================================================
// See the packed 16-bit version for comments.
#if defined(STP_GPU) && defined(STP_16BIT) && defined(STP_PAT)
    // 4x4 wave op: 8 component maximum.
    void StpPat4x4MaxH8(StpW1 i, inout StpH4 a, inout StpH4 b);
    // 4x4 wave op: 4 component sum.
    void StpPat4x4SumH4(StpW1 i, inout StpH4 a);
//------------------------------------------------------------------------------------------------------------------------------
    // Sample bilinear interpolated clamp to edge prior convergence.
    StpH1 StpPatPriConH(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // Note this is still designed to be an inline function pass merged to avoid DRAM traffic.
    // So in an ideal world (with better merging with pre-scale post) these would be already in registers.
    // But when PAT pass is non-inline, these callbacks are placed in the right order for loads.
    // Input motion, 'position - motion' is the reprojected position, where {0 to 1} is range of the screen.
    StpF2 StpPatDatMotH(StpW2 o);
    // Input color, this is linear HDR or post-tonemap-linear depending on STP_POSTMAP.
    StpH3 StpPatDatColH(StpW2 o);
    StpF1 StpPatDatZH(StpW2 o);
    // Input depth, this is linear {0:near to INF:far} ranged.
    StpF1 StpPatFixZH(StpF1 z);
    StpU1 StpPatDatRH(StpW2 o);
    // Responsive input pixel {0.0 := responsive, 1.0 := normal}.
    StpH1 StpPatFixRH(StpU1 v);
//------------------------------------------------------------------------------------------------------------------------------
    // Dither value {0 to 1} this should be input pixel frequency spatial temporal blue noise.
    StpH1 StpPatDitH(StpW2 o);
//------------------------------------------------------------------------------------------------------------------------------
    // Sample bilinear interpolated clamp to edge prior feedback.
    StpH4 StpPatPriFedH(StpF2 p);
    // Gather4 versions.
    StpH4 StpPatPriFedR4H(StpF2 p);
    StpH4 StpPatPriFedG4H(StpF2 p);
    StpH4 StpPatPriFedB4H(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // Sample bilinear interpolated clamp to edge 2-frame luma ring.
    StpH2 StpPatPriLumH(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // Gather4 on prior {z,motion}.
    StpU4 StpPatPriMot4H(StpF2 p);
    #if STP_MAX_MIN_UINT
        StpU1 StpPatPriMotMinH(StpF2 p);
    #endif // STP_MAX_MIN_UINT
    #if STP_OFFSETS
        StpU4 StpPatPriMot4OH(StpF2 p, StpI2 o);
        #if STP_MAX_MIN_UINT
            StpU1 StpPatPriMotMinOH(StpF2 p, StpI2 o);
        #endif // STP_MAX_MIN_UINT
    #endif // STP_OFFSETS
//------------------------------------------------------------------------------------------------------------------------------
    void StpPatStMotH(StpW2 p, StpU1 v);
    void StpPatStColH(StpW2 p, StpH4 v);
    void StpPatStLumH(StpW2 p, StpH2 v);
    void StpPatStCnvH(StpW2 p, StpH1 v);
//==============================================================================================================================
    void StpPatH(
    StpW1 lane,
    StpW2 pp,
    StpU4 con0,
    StpU4 con1,
    StpU4 con2,
    StpU4 con3,
    StpU4 con4,
    StpU4 con5,
    StpU4 con6,
    StpU4 con7,
    StpU4 con8,
    StpU4 con9,
    StpU4 conA,
    StpU4 conB,
    StpU4 conC,
    StpU4 conD) {
//------------------------------------------------------------------------------------------------------------------------------
        // Outputs.
        StpH4 rC;
        StpU1 rM;
        StpH2 rL;
        StpH1 rCnv;
//------------------------------------------------------------------------------------------------------------------------------
        // Rename constants.
        StpF2 kRcpC = StpF2_U2(con0.xy);
        StpF2 kHalfRcpC = StpF2_U2(con0.zw);
        StpF2 kJitCRcpCUnjitPRcpP = StpF2_U2(con1.xy);
        StpF2 kJitCRcpC = StpF2_U2(con1.zw);
        StpF2 kF = StpF2_U2(con2.xy);
        StpF4 kOS = StpF4_U4(con3);
        StpF2 kDepth = StpF2_U2(con2.zw);
        StpF2 kUnDepth = StpF2_U2(con4.xy);
        StpF1 kMotionMatch = StpF1_U1(con4.z);
        StpF2 kC = StpF2_U2(con5.xy);
        StpF4 k0123 = StpF4_U4(con6);
        StpF4 k4567 = StpF4_U4(con7);
        StpF4 k89AB = StpF4_U4(con8);
        StpF4 kCDEF = StpF4_U4(con9);
        StpF4 kGHIJ = StpF4_U4(conA);
        StpF4 kKLMN = StpF4_U4(conB);
        StpF4 kOPQR = StpF4_U4(conC);
        StpF2 kST = StpF2_U2(conD.xy);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 m = StpPatDatMotH(pp);
        // This dither fetch should likely be shared with pass merged pre-scale post work in the future.
        StpH1 d = StpPatDitH(pp);
        StpF1 zPre = StpPatDatZH(pp);
        StpH3 c = StpPatDatColH(pp);
//==============================================================================================================================
//      DEPENDENT INLINE INPUT MOTION
//==============================================================================================================================
        // Work towards getting all dependent fetches out first.
        // Compute float position {0 to 1} across screen.
        StpF2 p = StpF2(pp) * kRcpC + kHalfRcpC;
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG_BW_SOL
        {   StpH2 lum2 = StpPatPriLumH(p);
            StpH1 cnvPrev = StpPatPriConH(p);
            StpU4 mZVP4 = StpPatPriMot4H(p);
            StpU1 rPre = StpPatDatRH(p);
            StpH3 f = StpPatPriFedH(p).rgb;
            StpF1 z = StpPatFixZH(zPre);
            StpH1 r = StpPatFixRH(rPre);
            rC.rgb = StpH3_(m.x) + StpH3_(d.x) + c + StpH3_(lum2.x) + StpH3_(cnvPrev) + StpH3(mZVP4.xyz) + f + StpH3_(z+r);
            rC.a = StpH1_(0.0);
            rL = rC.rg;
            rM = StpU1_(rC.r);
            rCnv = rC.r;
            StpPatStMotH(pp, rM);
            StpPatStLumH(pp, rL);
            StpPatStColH(pp, rC);
            StpPatStCnvH(pp, rCnv);
            return; }
        #endif // STP_BUG_BW_SOL
//------------------------------------------------------------------------------------------------------------------------------
        // Reprojection position in prior input and feedback.
        StpF2 pM = (p - m);
        StpF2 pF = pM + kJitCRcpC;
              pM = pM + kJitCRcpCUnjitPRcpP;
//------------------------------------------------------------------------------------------------------------------------------
        // Fetch 2-frame reprojected history ring of luma.
        StpH2 lum2 = StpPatPriLumH(pM);
//------------------------------------------------------------------------------------------------------------------------------
        // Fetch reprojected low-frequency convergence prior frame.
        StpH1 cnvPrev = StpPatPriConH(pM);
//------------------------------------------------------------------------------------------------------------------------------
        // Grab large enough neighborhood for prior reprojected nearest {z,motion}.
        // This nearest dilates {z, motion} reprojection to avoid pulling in anti-aliased edges and leaving temporal ringing.
        #if (STP_SAFE_DILATE == 2)
            #if STP_MAX_MIN_UINT
                StpU4 mZVP4;
                #if STP_OFFSETS
                    mZVP4.x = StpPatPriMotMinOH(pM, StpI2(-1, -1));
                    mZVP4.y = StpPatPriMotMinOH(pM, StpI2( 1, -1));
                    mZVP4.z = StpPatPriMotMinOH(pM, StpI2(-1,  1));
                    mZVP4.w = StpPatPriMotMinOH(pM, StpI2( 1,  1));
                #else // STP_OFFSETS
                    mZVP4.x = StpPatPriMotMinH(pM + StpF2(-kRcpC.x, -kRcpC.y));
                    mZVP4.y = StpPatPriMotMinH(pM + StpF2( kRcpC.x, -kRcpC.y));
                    mZVP4.z = StpPatPriMotMinH(pM + StpF2(-kRcpC.x,  kRcpC.y));
                    mZVP4.w = StpPatPriMotMinH(pM + StpF2( kRcpC.x,  kRcpC.y));
                #endif // ST_OFFSETS
            #else // STP_MAX_MIN_UINT
                #if STP_OFFSETS
                    StpU4 mZVP4_0 = StpPatPriMot4OH(pM, StpI2(-1, -1));
                    StpU4 mZVP4_1 = StpPatPriMot4OH(pM, StpI2( 1, -1));
                    StpU4 mZVP4_2 = StpPatPriMot4OH(pM, StpI2(-1,  1));
                    StpU4 mZVP4_3 = StpPatPriMot4OH(pM, StpI2( 1,  1));
                #else // STP_OFFSETS
                    StpU4 mZVP4_0 = StpPatPriMot4H(pM + StpF2(-kRcpC.x, -kRcpC.y));
                    StpU4 mZVP4_1 = StpPatPriMot4H(pM + StpF2( kRcpC.x, -kRcpC.y));
                    StpU4 mZVP4_2 = StpPatPriMot4H(pM + StpF2(-kRcpC.x,  kRcpC.y));
                    StpU4 mZVP4_3 = StpPatPriMot4H(pM + StpF2( kRcpC.x,  kRcpC.y));
                #endif // STP_OFFSETS
            #endif // STP_MAX_MIN_UINT
        #else // (STP_SAFE_DILATE == 2)
            StpU1 mZVPN;
            // To be correct here this needs 'kHalfRcpP' (prior instead of current).
            // But didn't want to pass yet another pair of constants, so using current instead.
            // TODO: If later moving to 'kHalfRcpP' can use one sample by offset to save some VALU ops.
            // Also this is only used if STP_SAFE_DILATE=1 (else dead code).
            StpU4 mZVP2a = StpPatPriMot4H(pM - kHalfRcpC);
            StpU4 mZVP2b = StpPatPriMot4H(pM + kHalfRcpC);
            #if STP_MAX_MIN_UINT
                mZVPN = StpPatPriMotMinH(pM);
            #else // STP_MAX_MIN_UINT
                StpU4 mZVP4 = StpPatPriMot4H(pM);
            #endif // STP_MAX_MIN_UINT
        #endif // (STP_SAFE_DILATE == 2)
//------------------------------------------------------------------------------------------------------------------------------
        StpU1 rPre = StpPatDatRH(pp);
//------------------------------------------------------------------------------------------------------------------------------
        // Gather 4 on feedback.
        StpH4 f4R = StpPatPriFedR4H(pF);
        StpH4 f4G = StpPatPriFedG4H(pF);
        StpH4 f4B = StpPatPriFedB4H(pF);
        // Grab bilinear feedback.
        StpH3 f = StpPatPriFedH(pF).rgb;
//==============================================================================================================================
//      DEPENDENT ON DITHER AND INLINE INPUT PARAMETERS
//==============================================================================================================================
        StpF1 dd = StpF1_(d);
        // Convert depth {0 to inf} to {0 to 1} safe for 10-bit value.
        StpF1 z = StpPatFixZH(zPre);
        z = StpZPack(z, kDepth, dd);
        // Pack {MSB depth, LSB 11-bit XY motion}.
        rM = StpMvPack(z, m, dd);
        StpPatStMotH(pp, rM);
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG
            // Pattern/Clipped Input Color
            { StpF4 bug = StpF4_(0.0);
                bug.rgb = sqrt(StpF3(c));
                bug.rgb = StpSatF3(bug.rgb + StpF3_(StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 0), bug); }
//------------------------------------------------------------------------------------------------------------------------------
            // Pattern/Log Input Depth
            { StpF4 bug = StpF4_(0.0);
                bug.rgb = StpF3_(StpSatF1(z + StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 1), bug); }
        #endif // STP_BUG
//------------------------------------------------------------------------------------------------------------------------------
        // Pre-process color.
        // If running pre-tonemap, then do a fast reversible tonemapper (convert from {0 to inf} to {0 to 1}).
        #if (STP_POSTMAP == 0)
            StpToneH3(c);
        #endif // (STP_POSTMAP == 0)
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG
            // Pattern/Reversible Tonemapped Input Color
            { StpF4 bug = StpF4_(0.0);
                bug.rgb = sqrt(StpF3(c));
                bug.rgb = StpSatF3(bug.rgb + StpF3_(StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 2), bug); }
        #endif // STP_BUG
//------------------------------------------------------------------------------------------------------------------------------
        // Output intermediate color.
        // Dither from linear to gamma 2.0.
        // Simple non-energy conserving dither is working, using 10-bit/channel.
        c = sqrt(c);
        rC.rgb = StpSatH3(c + StpH3_(d * StpH1(1.0 / 1023.0) + StpH1(-0.5 / 1023.0)));
//------------------------------------------------------------------------------------------------------------------------------
        // Setup the new 3-ring output luma.
        rL.x = dot(c, StpH3(STP_LUMA));
        rL.y = lum2.x;
        StpPatStLumH(pp, rL);
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG
            // Pattern/Shaped Absolute Input Motion
            { StpF4 bug = StpF4_(0.0);
                bug.b = sqrt(StpF1_(rL.x) * StpF1_(0.25));
                bug.rg = StpF2_(1.0) - exp2(abs(StpF2(m)) * StpF2_(-32.0));
                bug.rgb = StpSatF3(bug.rgb + StpF3_(StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 3), bug); }
        #endif // STP_BUG
//------------------------------------------------------------------------------------------------------------------------------
        // Minimum change across the 3 frames {current, 2-frame reprojected history}.
        StpH1 moire = min(abs(rL.x - lum2.x), abs(lum2.x - lum2.y));
        moire *= StpH1_(STP_PAT_DEMOIRE);
//------------------------------------------------------------------------------------------------------------------------------
        // Grab neighborhood.
        // Parallel block {max,-min}, and -min of convergence.
        StpH4 xnyRG = StpH4(c.r, -c.r, c.g, -c.g);
        StpH4 xnyBC = StpH4(c.b, -c.b, -cnvPrev, -cnvPrev);
        #if defined(STP_16BIT)
            StpPat4x4MaxH8(lane, xnyRG, xnyBC);
        #else // defined(STP_16BIT)
            // We convert to full precision floats here since the reductions work on 32-bit values.
            StpF4 xnyRGF = StpF4_(xnyRG);
            StpF4 xnyBCF = StpF4_(xnyBC);
            StpPat4x4MaxF8(lane, xnyRGF, xnyBCF);
            xnyRG = StpMF4_(xnyRGF);
            xnyBC = StpMF4_(xnyBCF);
        #endif // defined(STP_16BIT)
        cnvPrev = -xnyBC.z;
        // This is max minus min (the '.y' is already negative).
        StpH3 ne = max(StpH3_(STP_PAT_NE_MIN) * StpH3(xnyRG.x, xnyRG.z, xnyBC.x),
                       StpH3(xnyRG.x + xnyRG.y, xnyRG.z + xnyRG.w, xnyBC.x + xnyBC.y));
        StpH1 ne1 = dot(ne, StpH3(STP_LUMA));
//------------------------------------------------------------------------------------------------------------------------------
        // Advance low frequency convergence.
        cnvPrev = StpSatH1(cnvPrev + StpH1_(1.0 / STP_FRAME_MAX));
//------------------------------------------------------------------------------------------------------------------------------
        // Estimate if reprojection is on-screen.
        StpF2 onXY = StpF2(pM.xy);
        // {-1 to 1} is on screen.
        onXY = onXY * kOS.xy + kOS.zw;
        // {0 := offscreen, 1 := onscreen}.
        StpF1 onS = StpSignedF1(max(abs(onXY.x), abs(onXY.y)) - StpF1_(1.0));
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG
            // Pattern/Motion Reprojection {R=Prior G=This Sqrt Luma Feedback Diff, B=Offscreen}
            { StpF4 bug = StpF4_(0.0);
                bug.g = StpF1_(abs(rL.x - lum2.x));
                bug.r = StpF1_(abs(lum2.x - lum2.y));
                bug.b = StpF1_(1.0) - StpF1_(onS);
                bug.rg = sqrt(bug.rg);
                bug.rgb = StpSatF3(bug.rgb + StpF3_(StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 4), bug); }
        #endif // STP_BUG
//==============================================================================================================================
//      DEPENDENT ON PRIOR {Z, MOTION}
//==============================================================================================================================
        // Compute a motion match value.
        // Finish {z, motion} nearest dilation.
        #if (STP_SAFE_DILATE == 2)
            #if (STP_MAX_MIN_UINT == 0)
                StpU4 mZVP4 = min(StpMin3U4(mZVP4_0, mZVP4_1, mZVP4_2), mZVP4_3);
            #endif // (STP_MAX_MIN_UINT == 0)
            StpU1 mZVPN = min(StpMin3U1(mZVP4.x, mZVP4.y, mZVP4.z), mZVP4.w);
        #else // (STP_SAFE_DILATE == 2)
            #if (STP_MAX_MIN_UINT == 0)
                mZVPN = min(StpMin3U1(mZVP4.x, mZVP4.y, mZVP4.z), mZVP4.w);
            #endif // (STP_MAX_MIN_UINT == 0)
            #if STP_SAFE_DILATE
                mZVPN = StpMin3U1(StpMin3U1(mZVPN, mZVP2a.x, mZVP2a.z), mZVP2b.x, mZVP2b.z);
            #endif // STP_SAFE_DILATE
        #endif // (STP_SAFE_DILATE == 2)
//------------------------------------------------------------------------------------------------------------------------------
        // The {motion} matching logic.
        StpF2 mPN;
        StpF1 mZPN;
        // Motion 'm' units are {1 := move by one screen}.
        StpMvUnpack(mZPN, mPN, mZVPN);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 mE;
        // Use a smoother error estimate.
        // This '1/256' instead of '1/1024' is to be more accepting of a motion match.
        // The 'sqrt()' cannot be the low precision approximation without visually seeing differences in the mask.
        mE = sqrt(abs(m)) + StpF2_(1.0 / 256.0);
        mE = mE * mE - abs(m);
//------------------------------------------------------------------------------------------------------------------------------
        // Static geometry motion + estimated dynamic motion matching logic.
        // Take unpacked low precision {0 to 1} Z and decode to {0 to INF}.
        StpF1 sgZ = StpZUnpack(mZPN, kUnDepth);
        StpF2 bugF; StpF2 bugD;
        StpF2 sgM = StpFor(pM, sgZ, mPN, kMotionMatch, k0123, k4567, k89AB, kCDEF, kGHIJ, kKLMN, kOPQR, kST, bugF, bugD);
        // Note 'sgM' is in NDC {-1 to 1} space and 'm' is in {0 to 1} space, thus the 0.5 scaling factor.
        // The difference gets conservative possible motion encoding error subtracted out via 'saturate(abs(..)-mE)'.
        sgM = StpSatF2(abs(sgM * StpF2_(0.5) - m) - mE) * kC;
        StpH1 sgD = StpH1(dot(sgM, sgM));
//------------------------------------------------------------------------------------------------------------------------------
        // Motion match {0 := no match, 1 := match}.
        StpH1 match = StpH1_(1.0) - StpSatH1(sgD * StpH1_(STP_PAT_MOT_AMP) - StpH1_(STP_PAT_MOT_ADD * STP_PAT_MOT_AMP));
        // Offscreen is a non-match.
        match *= StpH1_(onS);
        // Pass motion match in alpha.
        rC.a = match;
        StpPatStColH(pp, rC);
//------------------------------------------------------------------------------------------------------------------------------
        // Must disable on non-motion match, but make sure it doesn't fully /0 later.
        moire = moire * match + StpH1_(1.0 / 8192.0);
        // Scale down temporal change proportional to ratio of local neighborhood and minimum 3-frame temporal change.
        moire = min(StpH1_(1.0), ne1 * StpRcpH1(moire));
//------------------------------------------------------------------------------------------------------------------------------
        // Sensitivity modifiers.
        // The following which gets optimized to two FMAs.
        //  tS = tS * ((1-v)*k  + 1) ... logic
        //  tS = tS * ((1-v)*k) + tS
        //  tS = tS * (k-v*k) + tS ..... optimized
        StpH1 tS = moire;
        StpH1 r = StpPatFixRH(rPre);
        tS = tS * (StpH1_(STP_PAT_RESPONSIVE) - r * StpH1_(STP_PAT_RESPONSIVE)) + tS;
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG
            // Pattern/Sensitivity {G=No motion match, R=Responsive, B=Luma}
            { StpF4 bug = StpF4_(0.0);
                bug.g = StpF1_(1.0) - StpF1(match);
                bug.r = StpF1_(1.0) - StpF1(r);
                bug.b = StpF1_(rL.x);
                bug.rgb = StpSatF3(bug.rgb + StpF3_(StpF1_(d) * StpF1_(1.0 / 255.0) + StpF1_(-0.5 / 255.0)));
                StpBugF(StpU3(pp, 5), bug); }
        #endif // STP_BUG
//==============================================================================================================================
//      DEPENDENT ON FEEDBACK
//==============================================================================================================================
        // Find lowest temporal difference.
        StpH4 t;
        t.rgb = c - f;
        // Luma diff in alpha.
        t.a = dot(abs(t.rgb), StpH3(STP_LUMA));
        // Compute lowest difference for all in quad.
        StpH4 t4R = f4R - StpH4_(c.r);
        StpH4 t4G = f4G - StpH4_(c.g);
        StpH4 t4B = f4B - StpH4_(c.b);
        StpH4 t4A = abs(t4R) * StpH4_(STP_LUMA_R) + abs(t4G) * StpH4_(STP_LUMA_G) + abs(t4B) * StpH4_(STP_LUMA_B);
        // Override with lower from gather4.
        t.a = StpMin3H1(t.a, t4A.x, StpMin3H1(t4A.y, t4A.z, t4A.w));
        if(t.a == t4A.x) t.rgb = StpH3(t4R.x, t4G.x, t4B.x);
        if(t.a == t4A.y) t.rgb = StpH3(t4R.y, t4G.y, t4B.y);
        if(t.a == t4A.z) t.rgb = StpH3(t4R.z, t4G.z, t4B.z);
        if(t.a == t4A.w) t.rgb = StpH3(t4R.w, t4G.w, t4B.w);
//------------------------------------------------------------------------------------------------------------------------------
        // Factor in sensitivity and reduce.
        t.rgb *= StpH3_(tS);
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_16BIT)
            StpPat4x4SumH4(lane, t);
        #else // defined(STP_16BIT)
            // We convert to full precision floats here since the reductions work on 32-bit values, and MF might be 16-bit.
            StpF4 tF = StpF4(t);
            StpPat4x4SumF4(lane, tF);
            t = StpMF4(tF);
        #endif // defined(STP_16BIT)
        t.rgb *= StpH3_(STP_PAT_SENSITIVITY);
//------------------------------------------------------------------------------------------------------------------------------
        // Ratio of 'spatial/temporal' change.
        StpH3 bln3 = StpSatH3(ne * StpPrxLoRcpH3(abs(t.rgb)));
        // Worst channel limits to avoid chroma ghosting.
        StpH1 bln = StpMin3H1(bln3.r, bln3.g, bln3.b);
//------------------------------------------------------------------------------------------------------------------------------
        // Convert from blend ratio to convergence.
        // Note, 'rcp(0)=+INF' when approximations are not used.
        StpH1 cnv = StpSatH1(bln * StpPrxLoRcpH1(StpH1_(STP_FRAME_MAX) - StpH1_(STP_FRAME_MAX) * bln));
//------------------------------------------------------------------------------------------------------------------------------
        // Feedback the min of reprojected convergence, and subtract one frame (as next frame advances by one).
        cnv = StpSatH1(cnv - StpH1_(1.0 / STP_FRAME_MAX));
        rCnv = min(cnv, cnvPrev);
        StpPatStCnvH(pp, rCnv); }
#endif // defined(STP_GPU) && defined(STP_16BIT) && defined(STP_PAT)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//
//                                                PATTERN DILATION ENTRY POINT
//
//------------------------------------------------------------------------------------------------------------------------------
// This should be pass merged with STP_SAA.
// Dilates low frequency convergence.
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_32BIT) && defined(STP_DIL)
    StpMF1 StpDilDitF(StpMU2 o);
    StpMF1 StpDilConF(StpF2 p);
    StpMF4 StpDilCon4F(StpF2 p);
    #if STP_OFFSETS
        StpMF1 StpDilConOF(StpF2 p, StpI2 o);
        StpMF4 StpDilCon4OF(StpF2 p, StpI2 o);
    #endif // STP_OFFSETS
//==============================================================================================================================
    void StpDilF(out StpMF1 oC, StpU2 pp, StpU4 con0) {
        StpF2 kRcpR = StpF2_U2(con0.xy);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 p = StpF2(pp) * kRcpR;
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG_BW_SOL
        { oC = StpDilCon4F(p).x; return; }
        #endif // STP_BUG_BW_SOL
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_OFFSETS
            StpMF4 g0 = StpDilCon4OF(p, StpI2(-1.0, -1.0));
            StpMF4 g1 = StpDilCon4OF(p, StpI2( 1.0, -1.0));
            StpMF4 g2 = StpDilCon4OF(p, StpI2( 3.0, -1.0));
            StpMF4 g3 = StpDilCon4OF(p, StpI2(-1.0,  1.0));
            StpMF4 g4 = StpDilCon4OF(p, StpI2( 1.0,  1.0));
            StpMF4 g5 = StpDilCon4OF(p, StpI2( 3.0,  1.0));
            StpMF4 g6 = StpDilCon4OF(p, StpI2(-1.0,  3.0));
            StpMF4 g7 = StpDilCon4OF(p, StpI2( 1.0,  3.0));
            StpMF4 g8 = StpDilCon4OF(p, StpI2( 3.0,  3.0));
        #else // STP_OFFSETS
            StpMF4 g0 = StpDilCon4F(p + StpF2(-1.0 * kRcpR.x, -1.0 * kRcpR.y));
            StpMF4 g1 = StpDilCon4F(p + StpF2( 1.0 * kRcpR.x, -1.0 * kRcpR.y));
            StpMF4 g2 = StpDilCon4F(p + StpF2( 3.0 * kRcpR.x, -1.0 * kRcpR.y));
            StpMF4 g3 = StpDilCon4F(p + StpF2(-1.0 * kRcpR.x,  1.0 * kRcpR.y));
            StpMF4 g4 = StpDilCon4F(p + StpF2( 1.0 * kRcpR.x,  1.0 * kRcpR.y));
            StpMF4 g5 = StpDilCon4F(p + StpF2( 3.0 * kRcpR.x,  1.0 * kRcpR.y));
            StpMF4 g6 = StpDilCon4F(p + StpF2(-1.0 * kRcpR.x,  3.0 * kRcpR.y));
            StpMF4 g7 = StpDilCon4F(p + StpF2( 1.0 * kRcpR.x,  3.0 * kRcpR.y));
            StpMF4 g8 = StpDilCon4F(p + StpF2( 3.0 * kRcpR.x,  3.0 * kRcpR.y));
        #endif // STP_OFFSETS
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 cA = g0.w;
        StpMF1 cB = g0.z;
        StpMF1 cC = g1.w;
        StpMF1 cD = g1.z;
        StpMF1 cE = g2.w;
        StpMF1 cF = g0.x;
        StpMF1 cG = g0.y;
        StpMF1 cH = g1.x;
        StpMF1 cI = g1.y;
        StpMF1 cJ = g2.x;
        StpMF1 cK = g3.w;
        StpMF1 cL = g3.z;
        StpMF1 cM = g4.w;
        StpMF1 cN = g4.z;
        StpMF1 cO = g5.w;
        StpMF1 cP = g3.x;
        StpMF1 cQ = g3.y;
        StpMF1 cR = g4.x;
        StpMF1 cS = g4.y;
        StpMF1 cT = g5.x;
        StpMF1 cU = g6.w;
        StpMF1 cV = g6.z;
        StpMF1 cW = g7.w;
        StpMF1 cX = g7.z;
        StpMF1 cY = g8.w;
//------------------------------------------------------------------------------------------------------------------------------
        StpMF4 m1345;
        m1345.x = StpMin3MF1(StpMin3MF1(cG, cH, cI), cC, cM);
        m1345.y = StpMin3MF1(StpMin3MF1(cK, cL, cM), cG, cQ);
        m1345.z = StpMin3MF1(StpMin3MF1(cL, cM, cN), cH, cR);
        m1345.w = StpMin3MF1(StpMin3MF1(cM, cN, cO), cI, cS);
        StpMF1 m7 = StpMin3MF1(StpMin3MF1(cQ, cR, cS), cM, cW);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 b0 = StpMF1_(0.5);
        StpMF1 b1 = (StpMF1_(1.0) - b0) * StpMF1_(0.25);
        oC = m1345.z * b0 + m1345.x * b1 + m1345.y * b1 + m1345.w * b1 + m7 * b1; }
#endif // defined(STP_GPU) && defined(STP_32BIT) && defined(STP_DIL)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                         16-BIT PATH
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_16BIT) && defined(STP_DIL)
    // Some of these are unused, possibly for future experimentation.
    StpH1 StpDilDitH(StpW2 o);
    StpH1 StpDilConH(StpF2 p);
    StpH4 StpDilCon4H(StpF2 p);
    #if STP_OFFSETS
        StpH1 StpDilConOH(StpF2 p, StpI2 o);
        StpH4 StpDilCon4OH(StpF2 p, StpI2 o);
    #endif // STP_OFFSETS
//==============================================================================================================================
    void StpDilH(out StpH1 oC, StpU2 pp, StpU4 con0) {
        StpF2 kRcpR = StpF2_U2(con0.xy);
        StpF2 p = StpF2(pp) * kRcpR;
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG_BW_SOL
        { oC = StpDilCon4H(p).x; return; }
        #endif // STP_BUG_BW_SOL
//------------------------------------------------------------------------------------------------------------------------------
        // Gather.
        //  0   1   2
        //
        //  3   4   5
        //
        //  6   7   8
        // For.
        //  w z w z w z
        //  x y.x y x y
        //  w z[w]z w z
        //  x y x y x y
        //  w z w z w z
        //  x y x y x y
        #if STP_OFFSETS
            StpH4 g0 = StpDilCon4OH(p, StpI2(-1.0, -1.0));
            StpH4 g1 = StpDilCon4OH(p, StpI2( 1.0, -1.0));
            StpH4 g2 = StpDilCon4OH(p, StpI2( 3.0, -1.0));
            StpH4 g3 = StpDilCon4OH(p, StpI2(-1.0,  1.0));
            StpH4 g4 = StpDilCon4OH(p, StpI2( 1.0,  1.0));
            StpH4 g5 = StpDilCon4OH(p, StpI2( 3.0,  1.0));
            StpH4 g6 = StpDilCon4OH(p, StpI2(-1.0,  3.0));
            StpH4 g7 = StpDilCon4OH(p, StpI2( 1.0,  3.0));
            StpH4 g8 = StpDilCon4OH(p, StpI2( 3.0,  3.0));
        #else // STP_OFFSETS
            StpH4 g0 = StpDilCon4H(p + StpF2(-1.0 * kRcpR.x, -1.0 * kRcpR.y));
            StpH4 g1 = StpDilCon4H(p + StpF2( 1.0 * kRcpR.x, -1.0 * kRcpR.y));
            StpH4 g2 = StpDilCon4H(p + StpF2( 3.0 * kRcpR.x, -1.0 * kRcpR.y));
            StpH4 g3 = StpDilCon4H(p + StpF2(-1.0 * kRcpR.x,  1.0 * kRcpR.y));
            StpH4 g4 = StpDilCon4H(p + StpF2( 1.0 * kRcpR.x,  1.0 * kRcpR.y));
            StpH4 g5 = StpDilCon4H(p + StpF2( 3.0 * kRcpR.x,  1.0 * kRcpR.y));
            StpH4 g6 = StpDilCon4H(p + StpF2(-1.0 * kRcpR.x,  3.0 * kRcpR.y));
            StpH4 g7 = StpDilCon4H(p + StpF2( 1.0 * kRcpR.x,  3.0 * kRcpR.y));
            StpH4 g8 = StpDilCon4H(p + StpF2( 3.0 * kRcpR.x,  3.0 * kRcpR.y));
        #endif // STP_OFFSETS
//------------------------------------------------------------------------------------------------------------------------------
        // Rename
        //  a b c d e
        //  f g h i j
        //  k l m n o
        //  p q r s t
        //  u v w x y
        StpH1 cA = g0.w;
        StpH1 cB = g0.z;
        StpH1 cC = g1.w;
        StpH1 cD = g1.z;
        StpH1 cE = g2.w;
        StpH1 cF = g0.x;
        StpH1 cG = g0.y;
        StpH1 cH = g1.x;
        StpH1 cI = g1.y;
        StpH1 cJ = g2.x;
        StpH1 cK = g3.w;
        StpH1 cL = g3.z;
        StpH1 cM = g4.w;
        StpH1 cN = g4.z;
        StpH1 cO = g5.w;
        StpH1 cP = g3.x;
        StpH1 cQ = g3.y;
        StpH1 cR = g4.x;
        StpH1 cS = g4.y;
        StpH1 cT = g5.x;
        StpH1 cU = g6.w;
        StpH1 cV = g6.z;
        StpH1 cW = g7.w;
        StpH1 cX = g7.z;
        StpH1 cY = g8.w;
//------------------------------------------------------------------------------------------------------------------------------
        // 5 point min.
        //  . 1 .
        //  3 4 5
        //  . 7 .
        StpH4 m1345;
        m1345.x = StpMin3H1(StpMin3H1(cG, cH, cI), cC, cM);
        m1345.y = StpMin3H1(StpMin3H1(cK, cL, cM), cG, cQ);
        m1345.z = StpMin3H1(StpMin3H1(cL, cM, cN), cH, cR);
        m1345.w = StpMin3H1(StpMin3H1(cM, cN, cO), cI, cS);
        StpH1 m7 = StpMin3H1(StpMin3H1(cQ, cR, cS), cM, cW);
//------------------------------------------------------------------------------------------------------------------------------
        StpH1 b0 = StpH1_(0.5);
        StpH1 b1 = (StpH1_(1.0) - b0) * StpH1_(0.25);
        oC = m1345.z * b0 + m1345.x * b1 + m1345.y * b1 + m1345.w * b1 + m7 * b1; }
#endif // defined(STP_GPU) && defined(STP_16BIT) && defined(STP_DIL)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//
//                                              SPATIAL ANTI-ALIASING ENTRY POINT
//
//------------------------------------------------------------------------------------------------------------------------------
// This should be pass merged with STP_DIL.
// It's a shell, GEAA is separated as a modified form could be useful on its own.
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_32BIT) && defined(STP_SAA)
    StpMF4 StpSaaLum4F(StpF2 p);
    #if STP_OFFSETS
        StpMF4 StpSaaLum4OF(StpF2 p, StpI2 o);
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    #define STP_GEAA 1
    StpMF4 StpGeaa4F(StpF2 p) { return StpSaaLum4F(p); }
    #if STP_OFFSETS
        StpMF4 StpGeaa4OF(StpF2 p, StpI2 o) { return StpSaaLum4OF(p, o); }
    #endif
    void StpGeaaF(out StpMF1 gW, out StpMF1 gLuma, out StpF2 gFilter, out StpF2 gDilate, StpF2 p, StpF2 kRcpI, StpF2 kHalfRcpI);
//==============================================================================================================================
    void StpSaaF(out StpMF1 oN, StpU2 pp, StpU4 con0) {
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 kRcpC = StpF2_U2(con0.xy);
        StpF2 kHalfRcpC = StpF2_U2(con0.zw);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 p = StpF2(pp) * kRcpC + kHalfRcpC;
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG_BW_SOL
        { oN = StpSaaLum4F(p).x; return; }
        #endif // STP_BUG_BW_SOL
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 gLuma;
        StpMF1 gNe;
        StpF2 gFilter;
        StpF2 gDilate;
        StpGeaaF(oN, gLuma, gFilter, gDilate, p, kRcpC, kHalfRcpC); }
#endif // defined(STP_GPU) && defined(STP_32BIT) && defined(STP_SAA)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                         16-BIT PATH
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_16BIT) && defined(STP_SAA)
    // Gather4 on current luma.
    StpH4 StpSaaLum4H(StpF2 p);
    #if STP_OFFSETS
        StpH4 StpSaaLum4OH(StpF2 p, StpI2 o);
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    #define STP_GEAA 1
    StpH4 StpGeaa4H(StpF2 p) { return StpSaaLum4H(p); }
    #if STP_OFFSETS
        StpH4 StpGeaa4OH(StpF2 p, StpI2 o) { return StpSaaLum4OH(p, o); }
    #endif
    void StpGeaaH(out StpH1 gW, out StpH1 gLuma, out StpF2 gFilter, out StpF2 gDilate, StpF2 p, StpF2 kRcpI, StpF2 kHalfRcpI);
//==============================================================================================================================
    void StpSaaH(
    out StpH1 oN, // Output control (to be stored).
    StpU2 pp,     // Input position {0 to size-1} across the input frame.
    StpU4 con0) { // Shared, first constant generated by StpPatCon().
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 kRcpC = StpF2_U2(con0.xy);
        StpF2 kHalfRcpC = StpF2_U2(con0.zw);
//------------------------------------------------------------------------------------------------------------------------------
        // Float position {0 to 1} across screen.
        StpF2 p = StpF2(pp) * kRcpC + kHalfRcpC;
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG_BW_SOL
        { oN = StpSaaLum4H(p).x; return; }
        #endif // STP_BUG_BW_SOL
//------------------------------------------------------------------------------------------------------------------------------
        StpH1 gLuma;   // Spatial AA (unused).
        StpH1 gNe;     // Output spatial neighborhood (unused).
        StpF2 gFilter; // Output position for anti-aliased color sampling if standalone (unused).
        StpF2 gDilate; // Output for {z,motion} dilation (unused).
        StpGeaaH(oN, gLuma, gFilter, gDilate, p, kRcpC, kHalfRcpC); }
#endif // defined(STP_GPU) && defined(STP_16BIT) && defined(STP_SAA)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//
//                                                   SCALING TAA ENTRY POINT
//
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_TAA) && defined(STP_32BIT)
    StpMF4 StpTaaCtl4F(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    StpMF4 StpTaaCol4RF(StpF2 p);
    StpMF4 StpTaaCol4GF(StpF2 p);
    StpMF4 StpTaaCol4BF(StpF2 p);
    StpMF4 StpTaaCol4AF(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpTaaConF(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpTaaDitF(StpMU2 o);
//------------------------------------------------------------------------------------------------------------------------------
    StpU4 StpTaaMot4F(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    StpMF4 StpTaaPriFedF(StpF2 p);
    StpMF4 StpTaaPriFed4RF(StpF2 p);
    StpMF4 StpTaaPriFed4GF(StpF2 p);
    StpMF4 StpTaaPriFed4BF(StpF2 p);
    #if STP_MAX_MIN_10BIT
        StpMF4 StpTaaPriFedMaxF(StpF2 p);
        StpMF4 StpTaaPriFedMinF(StpF2 p);
    #endif // STP_MAX_MIN_10BIT
    #if STP_OFFSETS
        StpMF4 StpTaaPriFedOF(StpF2 p, StpI2 o);
        StpMF4 StpTaaPriFed4ROF(StpF2 p, StpI2 o);
        StpMF4 StpTaaPriFed4GOF(StpF2 p, StpI2 o);
        StpMF4 StpTaaPriFed4BOF(StpF2 p, StpI2 o);
    #endif // STP_OFFSETS
//==============================================================================================================================
    void StpTaaF(
    StpMU1 lane,
    StpMU2 o,
    out StpMF4 rF,
    out StpMF4 rW,
    StpU4 con0,
    StpU4 con1,
    StpU4 con2,
    StpU4 con3) {
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 dit = StpTaaDitF(o);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 kCRcpF = StpF2_U2(con0.xy);
        StpF2 kHalfCRcpFUnjitC = StpF2_U2(con0.zw);
        StpF2 kRcpC = StpF2_U2(con1.xy);
        StpF2 kRcpF = StpF2_U2(con1.zw);
        StpF2 kHalfRcpF = StpF2_U2(con2.xy);
        StpF2 kJitCRcpC0 = StpF2_U2(con2.zw);
        StpF2 kHalfRcpC = StpF2_U2(con3.xy);
        StpF2 kF = StpF2_U2(con3.zw);
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG_BW_SOL
        {   StpF2 oo = StpF2(o) * kRcpF;
            StpMF4 g4 = StpTaaCtl4RF(oo);
            StpU4 m4 = StpTaaMot4F(oo);
            StpMF1 cnv = StpTaaConF(oo);
            StpMF4 f = StpTaaPriFedF(oo);
            StpMF4 c4R = StpTaaCol4RF(oo);
            rW = rF = l4 + g4 + StpMF4(m4) + StpMF4_(cnv) + f + c4R;
            return; }
        #endif // STP_BUG_BW_SOL
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 oI = StpF2(o);
        StpF2 oC = oI * kCRcpF + kHalfCRcpFUnjitC;
        StpF2 oCNW = floor(oC + StpF2_(-0.5));
        StpF2 oC4 = oCNW * kRcpC + kRcpC;
        StpF2 oC1 = oC * kRcpC;
//==============================================================================================================================
//      FETCH {CONVERGENCE, COLOR, CONTROL, Z+MOTION}
//==============================================================================================================================
        StpMF1 cnv = StpTaaConF(oC1);
        StpMF4 c4R = StpTaaCol4RF(oC4);
        StpMF4 c4G = StpTaaCol4GF(oC4);
        StpMF4 c4B = StpTaaCol4BF(oC4);
        StpMF4 c4A = StpTaaCol4AF(oC4);
        StpMF4 g4 = StpTaaCtl4F(oC4);
        StpU4 m4 = StpTaaMot4F(oC4);
//------------------------------------------------------------------------------------------------------------------------------
//      INDEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
        StpMF2 rP = StpMF2(oC - oCNW) - StpMF2_(0.5);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF2 rPX10 = StpMF2(1.0, 0.0) + StpMF2(-rP.x, rP.x);
        StpMF2 rPY01 = StpMF2(0.0, 1.0) + StpMF2(rP.y, -rP.y);
        StpMF4 pen4x = StpMF4(rPX10.g, rPX10.r, rPX10.r, rPX10.g);
        StpMF4 pen4y = StpMF4(rPY01.g, rPY01.g, rPY01.r, rPY01.r);
        StpMF4 pen4 = StpSatMF4(pen4x * pen4x + pen4y * pen4y);
//==============================================================================================================================
//      DEPENDENT ON {CONVERGENCE}
//==============================================================================================================================
        cnv = StpSatMF1(cnv - StpMF1_(1.0 / STP_FRAME_MAX));
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 pen = StpMF1_(cnv) * StpMF1_(STP_FRAME_MAX) + StpMF1_(1.0);
        pen = StpPrxLoSqrtMF1(pen);
        pen4 = StpSatMF4(StpMF4_(1.0) - pen4 * StpMF4_(pen));
        #if defined(STP_16BIT)
        #else // defined(STP_16BIT)
            pen = StpSatMF1(pen4.x * pen4.x + pen4.y * pen4.y + pen4.z * pen4.z + pen4.w * pen4.w);
        #endif // defined(STP_16BIT)
//==============================================================================================================================
//      DEPENDENT ON {COLOR}
//==============================================================================================================================
        StpMF4 wG;
        StpMF4 l4 = c4R + c4G * StpMF4_(2.0) + c4B;
        StpMF2 difST = abs(l4.gr - l4.ab);
        StpP1 useS = difST.x > difST.y;
        StpMF2 wTrb = StpSatMF2(StpMF2(-rP.x, rP.x) + StpMF2(rP.y, -rP.y));
        StpMF2 wSrb = min(rPX10, rPY01);
        if(useS) wTrb = wSrb;
        StpMF2 wTga = rPY01 - wTrb;
        wG.rg = StpMF2(wTrb.x, wTga.x);
        wG.ba = StpMF2(wTrb.y, wTga.y);
        wG *= wG;
        wG *= wG;
//------------------------------------------------------------------------------------------------------------------------------
        wG *= g4;
        StpMF4 triMask = StpMF4_(1.0);
        StpMF2 wGmin2 = min(wG.xy, wG.zw);
//==============================================================================================================================
//      DEPENDENT ON {Z,MOTION}
//==============================================================================================================================
        if(wGmin2.x < wGmin2.y) {
            if(wG.x < wG.z) { triMask.x = StpMF1_(STP_TAA_TRI_MASK_AVOID); m4.x = 0xFFFFFFFF; }
            else            { triMask.z = StpMF1_(STP_TAA_TRI_MASK_AVOID); m4.z = 0xFFFFFFFF; } }
        else {
            if(wG.y < wG.w) { triMask.y = StpMF1_(STP_TAA_TRI_MASK_AVOID); m4.y = 0xFFFFFFFF; }
            else            { triMask.w = StpMF1_(STP_TAA_TRI_MASK_AVOID); m4.w = 0xFFFFFFFF; } }
        StpU1 m1 = min(StpMin3U1(m4.x, m4.y, m4.z), m4.w);
//------------------------------------------------------------------------------------------------------------------------------
        wG *= triMask;
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 mXY;
        StpMvUnpackV(mXY, m1);
//==============================================================================================================================
//      GET ALL FEEDBACK FILTERING DONE
//==============================================================================================================================
        StpF2 oF = oI * kRcpF + kHalfRcpF - mXY;
//------------------------------------------------------------------------------------------------------------------------------
        StpMF3 f;
        #if STP_TAA_PRX_LANCZOS
            StpF2 oM = oI + StpF2_(0.5) - mXY * kF;
            StpF2 oMNW = floor(oM + StpF2_(-0.5));
            StpF2 oM4 = oMNW * kRcpF + kRcpF;
            StpMF3 fMax, fMin;
        #else // STP_TAA_PRX_LANCZOS
            f = StpTaaPriFedF(oF).rgb;
        #endif // STP_TAA_PRX_LANCZOS
//==============================================================================================================================
        #if (STP_TAA_PRX_LANCZOS == 1)
            #if STP_OFFSETS
                StpF2 oM0 = StpF2(oF.x, oM4.y + kRcpF.y * StpF1_(-1.5));
                StpMF3 f0 = StpTaaPriFedF(oM0).rgb;
                StpMF3 f1 = StpTaaPriFedOF(oM0, StpI2(0, 1)).rgb;
                StpMF3 f2 = StpTaaPriFedOF(oM0, StpI2(0, 2)).rgb;
                StpMF3 f3 = StpTaaPriFedOF(oM0, StpI2(0, 3)).rgb;
            #else // STP_OFFSETS
                StpF2 oM0 = StpF2(oF.x, oM4.y + kRcpF.y * StpF1_(-1.5));
                StpF2 oM1 = StpF2(oF.x, oM4.y + kRcpF.y * StpF1_(-0.5));
                StpF2 oM2 = StpF2(oF.x, oM4.y + kRcpF.y * StpF1_( 0.5));
                StpF2 oM3 = StpF2(oF.x, oM4.y + kRcpF.y * StpF1_( 1.5));
                StpMF3 f0 = StpTaaPriFedF(oM0).rgb;
                StpMF3 f1 = StpTaaPriFedF(oM1).rgb;
                StpMF3 f2 = StpTaaPriFedF(oM2).rgb;
                StpMF3 f3 = StpTaaPriFedF(oM3).rgb;
            #endif // STP_OFFSETS
            #if (STP_MAX_MIN_10BIT && STP_TAA_PRX_LANCZOS_DERING)
                fMax = StpTaaPriFedMaxF(oM4).rgb;
                fMin = StpTaaPriFedMinF(oM4).rgb;
            #endif // (STP_MAX_MIN_10BIT && STP_TAA_PRX_LANCZOS_DERING)
            #if ((STP_MAX_MIN_10BIT == 0) && STP_TAA_PRX_LANCZOS_DERING)
                StpMF4 f4R = StpTaaPriFed4RF(oM4);
                StpMF4 f4G = StpTaaPriFed4GF(oM4);
                StpMF4 f4B = StpTaaPriFed4BF(oM4);
            #endif // ((STP_MAX_MIN_10BIT == 0) && STP_TAA_PRX_LANCZOS_DERING)
//------------------------------------------------------------------------------------------------------------------------------
//          INDEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
            StpMF2 fP = StpMF2(oM - oMNW);
            StpMF4 fPY = StpMF4_(-fP.y * StpMF1_(0.5)) + StpMF4(-0.5 * 0.5, 0.5 * 0.5, 1.5 * 0.5, 2.5 * 0.5);
            fPY = StpSatMF4(StpMF4_(1.0) - fPY * fPY);
            fPY *= fPY;
            StpMF4 fPY4 = fPY * fPY;
            fPY = (StpMF4_(1.0 + 81.0 / 175.0) * fPY4 - StpMF4_(81.0 / 175.0)) * fPY;
            #if defined(STP_16BIT)
            #else // defined(STP_16BIT)
                StpMF1 fRcp = StpPrxLoRcpMF1(fPY.r + fPY.g + fPY.b + fPY.a);
            #endif // defined(STP_16BIT)
//------------------------------------------------------------------------------------------------------------------------------
//          DEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
            f.rgb = f0 * StpMF3_(fPY.r) + f1 * StpMF3_(fPY.g) + f2 * StpMF3_(fPY.b) + f3 * StpMF3_(fPY.a);
            f.rgb *= StpMF3_(fRcp);
            #if STP_TAA_PRX_LANCZOS_DERING
                #if (STP_MAX_MIN_10BIT == 0)
                    #if defined(STP_16BIT)
                    #else // defined(STP_16BIT)
                        fMax.r = max(StpMax3MF1(f4R.x, f4R.y, f4R.z), f4R.w);
                        fMax.g = max(StpMax3MF1(f4G.x, f4G.y, f4G.z), f4G.w);
                        fMax.b = max(StpMax3MF1(f4B.x, f4B.y, f4B.z), f4B.w);
                        fMin.r = min(StpMin3MF1(f4R.x, f4R.y, f4R.z), f4R.w);
                        fMin.g = min(StpMin3MF1(f4G.x, f4G.y, f4G.z), f4G.w);
                        fMin.b = min(StpMin3MF1(f4B.x, f4B.y, f4B.z), f4B.w);
                        f = clamp(f, fMin, fMax);
                    #endif // defined(STP_16BIT)
                #else // (STP_MAX_MIN_10BIT == 0)
                    f = clamp(f, fMin, fMax);
                #endif // (STP_MAX_MIN_10BIT == 0)
            #endif // STP_TAA_PRX_LANCZOS_DERING
        #endif // (STP_TAA_PRX_LANCZOS == 1)
//==============================================================================================================================
        #if (STP_TAA_PRX_LANCZOS == 2)
            #if STP_OFFSETS
                StpMF4 f4R0 = StpTaaPriFed4ROF(oM4, StpI2(-1, -1));
                StpMF4 f4G0 = StpTaaPriFed4GOF(oM4, StpI2(-1, -1));
                StpMF4 f4B0 = StpTaaPriFed4BOF(oM4, StpI2(-1, -1));
                StpMF4 f4R1 = StpTaaPriFed4ROF(oM4, StpI2( 1, -1));
                StpMF4 f4G1 = StpTaaPriFed4GOF(oM4, StpI2( 1, -1));
                StpMF4 f4B1 = StpTaaPriFed4BOF(oM4, StpI2( 1, -1));
                StpMF4 f4R2 = StpTaaPriFed4ROF(oM4, StpI2(-1,  1));
                StpMF4 f4G2 = StpTaaPriFed4GOF(oM4, StpI2(-1,  1));
                StpMF4 f4B2 = StpTaaPriFed4BOF(oM4, StpI2(-1,  1));
                StpMF4 f4R3 = StpTaaPriFed4ROF(oM4, StpI2( 1,  1));
                StpMF4 f4G3 = StpTaaPriFed4GOF(oM4, StpI2( 1,  1));
                StpMF4 f4B3 = StpTaaPriFed4BOF(oM4, StpI2( 1,  1));
            #else // STP_OFFSETS
                StpF2 oM0 = oM4 + StpF2(-kRcpF.x, -kRcpF.y);
                StpF2 oM1 = oM4 + StpF2( kRcpF.x, -kRcpF.y);
                StpF2 oM2 = oM4 + StpF2(-kRcpF.x,  kRcpF.y);
                StpF2 oM3 = oM4 + StpF2( kRcpF.x,  kRcpF.y);
                StpMF4 f4R0 = StpTaaPriFed4RF(oM0);
                StpMF4 f4G0 = StpTaaPriFed4GF(oM0);
                StpMF4 f4B0 = StpTaaPriFed4BF(oM0);
                StpMF4 f4R1 = StpTaaPriFed4RF(oM1);
                StpMF4 f4G1 = StpTaaPriFed4GF(oM1);
                StpMF4 f4B1 = StpTaaPriFed4BF(oM1);
                StpMF4 f4R2 = StpTaaPriFed4RF(oM2);
                StpMF4 f4G2 = StpTaaPriFed4GF(oM2);
                StpMF4 f4B2 = StpTaaPriFed4BF(oM2);
                StpMF4 f4R3 = StpTaaPriFed4RF(oM3);
                StpMF4 f4G3 = StpTaaPriFed4GF(oM3);
                StpMF4 f4B3 = StpTaaPriFed4BF(oM3);
            #endif // STP_OFFSETS
            #if (STP_MAX_MIN_10BIT && STP_TAA_PRX_LANCZOS_DERING)
                fMax = StpTaaPriFedMaxF(oM4).rgb;
                fMin = StpTaaPriFedMinF(oM4).rgb;
            #endif // (STP_MAX_MIN_10BIT && STP_TAA_PRX_LANCZOS_DERING)
//------------------------------------------------------------------------------------------------------------------------------
//          INDEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
            StpMF2 fP = StpMF2(oM - oMNW);
            StpMF4 fPX = StpMF4_(-fP.x * StpMF1_(0.5)) + StpMF4(-0.5 * 0.5, 0.5 * 0.5, 1.5 * 0.5, 2.5 * 0.5);
            StpMF4 fPY = StpMF4_(-fP.y * StpMF1_(0.5)) + StpMF4(-0.5 * 0.5, 0.5 * 0.5, 1.5 * 0.5, 2.5 * 0.5);
            fPX = StpSatMF4(StpMF4_(1.0) - fPX * fPX);
            fPY = StpSatMF4(StpMF4_(1.0) - fPY * fPY);
            fPX *= fPX;
            fPY *= fPY;
            StpMF4 fPX4 = fPX * fPX;
            StpMF4 fPY4 = fPY * fPY;
            fPX = (StpMF4_(1.0 + 81.0 / 175.0) * fPX4 - StpMF4_(81.0 / 175.0)) * fPX;
            fPY = (StpMF4_(1.0 + 81.0 / 175.0) * fPY4 - StpMF4_(81.0 / 175.0)) * fPY;
            #if defined(STP_16BIT)
            #else // defined(STP_16BIT)
                fPX *= StpMF4_(StpPrxLoRcpMF1(fPX.r + fPX.g + fPX.b + fPX.a));
                fPY *= StpMF4_(StpPrxLoRcpMF1(fPY.r + fPY.g + fPY.b + fPY.a));
            #endif // defined(STP_16BIT)
            StpMF4 fPX0 = fPX * StpMF4_(fPY.r);
            StpMF4 fPX1 = fPX * StpMF4_(fPY.g);
            StpMF4 fPX2 = fPX * StpMF4_(fPY.b);
            StpMF4 fPX3 = fPX * StpMF4_(fPY.a);
//------------------------------------------------------------------------------------------------------------------------------
//          DEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
            #if defined(STP_16BIT)
            #else // defined(STP_16BIT)
                f.r = f4R0.w * fPX0.r + f4R0.z * fPX0.g + f4R1.w * fPX0.b + f4R1.z * fPX0.a +
                      f4R0.x * fPX1.r + f4R0.y * fPX1.g + f4R1.x * fPX1.b + f4R1.y * fPX1.a +
                      f4R2.w * fPX2.r + f4R2.z * fPX2.g + f4R3.w * fPX2.b + f4R3.z * fPX2.a +
                      f4R2.x * fPX3.r + f4R2.y * fPX3.g + f4R3.x * fPX3.b + f4R3.y * fPX3.a;
                f.g = f4G0.w * fPX0.r + f4G0.z * fPX0.g + f4G1.w * fPX0.b + f4G1.z * fPX0.a +
                      f4G0.x * fPX1.r + f4G0.y * fPX1.g + f4G1.x * fPX1.b + f4G1.y * fPX1.a +
                      f4G2.w * fPX2.r + f4G2.z * fPX2.g + f4G3.w * fPX2.b + f4G3.z * fPX2.a +
                      f4G2.x * fPX3.r + f4G2.y * fPX3.g + f4G3.x * fPX3.b + f4G3.y * fPX3.a;
                f.b = f4B0.w * fPX0.r + f4B0.z * fPX0.g + f4B1.w * fPX0.b + f4B1.z * fPX0.a +
                      f4B0.x * fPX1.r + f4B0.y * fPX1.g + f4B1.x * fPX1.b + f4B1.y * fPX1.a +
                      f4B2.w * fPX2.r + f4B2.z * fPX2.g + f4B3.w * fPX2.b + f4B3.z * fPX2.a +
                      f4B2.x * fPX3.r + f4B2.y * fPX3.g + f4B3.x * fPX3.b + f4B3.y * fPX3.a;
            #endif // defined(STP_16BIT)
            #if STP_TAA_PRX_LANCZOS_DERING
                #if (STP_MAX_MIN_10BIT == 0)
                    #if defined(STP_16BIT)
                    #else // defined(STP_16BIT)
                        fMax.r = max(StpMax3MF1(f4R0.y, f4R1.x, f4R2.z), f4R3.w);
                        fMax.g = max(StpMax3MF1(f4G0.y, f4G1.x, f4G2.z), f4G3.w);
                        fMax.b = max(StpMax3MF1(f4B0.y, f4B1.x, f4B2.z), f4B3.w);
                        fMin.r = min(StpMin3MF1(f4R0.y, f4R1.x, f4R2.z), f4R3.w);
                        fMin.g = min(StpMin3MF1(f4G0.y, f4G1.x, f4G2.z), f4G3.w);
                        fMin.b = min(StpMin3MF1(f4B0.y, f4B1.x, f4B2.z), f4B3.w);
                        f = clamp(f, fMin, fMax);
                    #endif // defined(STP_16BIT)
                #else // (STP_MAX_MIN_10BIT == 0)
                    f = clamp(f, fMin, fMax);
                #endif // (STP_MAX_MIN_10BIT == 0)
            #endif // STP_TAA_PRX_LANCZOS_DERING
        #endif // (STP_TAA_PRX_LANCZOS == 2)
//==============================================================================================================================
//      DISPLACEMENT
//==============================================================================================================================
        StpF2 oD0 = oC4 + kJitCRcpC0 - mXY;
        StpF2 oD1 = StpF2(kRcpC.x,      0.0) + oD0;
        StpF2 oD2 = StpF2(kRcpC.x, -kRcpC.y) + oD0;
        StpF2 oD3 = StpF2(0.0,     -kRcpC.y) + oD0;
        StpMF3 d0 = StpTaaPriFedF(oD0).rgb;
        StpMF3 d1 = StpTaaPriFedF(oD1).rgb;
        StpMF3 d2 = StpTaaPriFedF(oD2).rgb;
        StpMF3 d3 = StpTaaPriFedF(oD3).rgb;
//------------------------------------------------------------------------------------------------------------------------------
//      INDEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_16BIT)
        #else // defined(STP_16BIT)
            wG = StpSatMF4(wG * StpMF4_(StpPrxLoRcpMF1(wG.x + wG.y + wG.z + wG.w)));
        #endif // defined(STP_16BIT)
//------------------------------------------------------------------------------------------------------------------------------
        StpMF4 wT = abs(c4R - StpMF4_(f.r)) * StpMF4_(STP_LUMA_R) +
                    abs(c4G - StpMF4_(f.g)) * StpMF4_(STP_LUMA_G) +
                    abs(c4B - StpMF4_(f.b)) * StpMF4_(STP_LUMA_B);
        wT = StpPrxLoRcpMF4(wT * StpMF4_(STP_ANTI_MAX) + StpMF4_(STP_ANTI_MIN)) * triMask;
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_16BIT)
        #else // defined(STP_16BIT)
            wT = StpSatMF4(wT * StpMF4_(StpPrxLoRcpMF1(wT.x + wT.y + wT.z + wT.w)));
        #endif // defined(STP_16BIT)
//------------------------------------------------------------------------------------------------------------------------------
        StpMF4 wM = wT * StpMF4_(0.5) + wG * StpMF4_(0.5);
        #if defined(STP_16BIT)
        #else // defined(STP_16BIT)
            StpMF1 match = c4A.x * wM.x + c4A.y * wM.y + c4A.z * wM.z + c4A.w * wM.w;
        #endif // defined(STP_16BIT)
        cnv *= match;
//------------------------------------------------------------------------------------------------------------------------------
//      DEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
        StpMF3 dG = d0 * StpMF3_(wG.x) + d1 * StpMF3_(wG.y) + d2 * StpMF3_(wG.z) + d3 * StpMF3_(wG.w);
        StpMF3 dT = d0 * StpMF3_(wT.x) + d1 * StpMF3_(wT.y) + d2 * StpMF3_(wT.z) + d3 * StpMF3_(wT.w);
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_16BIT)
        #else // defined(STP_16BIT)
            StpMF3 t = StpMF3(
                c4R.x * wT.x + c4R.y * wT.y + c4R.z * wT.z + c4R.w * wT.w,
                c4G.x * wT.x + c4G.y * wT.y + c4G.z * wT.z + c4G.w * wT.w,
                c4B.x * wT.x + c4B.y * wT.y + c4B.z * wT.z + c4B.w * wT.w);
            StpMF3 c = StpMF3(
                c4R.x * wG.x + c4R.y * wG.y + c4R.z * wG.z + c4R.w * wG.w,
                c4G.x * wG.x + c4G.y * wG.y + c4G.z * wG.z + c4G.w * wG.w,
                c4B.x * wG.x + c4B.y * wG.y + c4B.z * wG.z + c4B.w * wG.w);
        #endif // defined(STP_16BIT)
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 bln = StpSatMF1(cnv * StpPrxLoRcpMF1(cnv + StpMF1_(1.0 / STP_FRAME_MAX)));
        StpMF1 blnT = StpMF1_(1.0) - bln;
        StpMF3 b = f * StpMF3_(bln) + t * StpMF3_(blnT);
        StpMF3 minNe = min(c, b);
        StpMF3 maxNe = max(c, b);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF3 penC = StpSatMF3(c + (f - dG) * StpMF3_(StpMF1_(0.9875) * match));
        StpMF2 penWF;
        penWF.x = pen * StpMF1_(STP_TAA_PEN_W);
        penWF.y = pen * lerp(StpMF1_(STP_TAA_PEN_F0), StpMF1_(STP_TAA_PEN_F1), cnv);
        StpMF2 penNotWF = StpMF2_(1.0) - penWF;
        rF.rgb = t + (f - dT);
        rF.rgb = rF.rgb * StpMF3_(blnT) + f * StpMF3_(bln);
        rW.rgb = StpSatMF3(rF.rgb * StpMF3_(penNotWF.x) + penC * StpMF3_(penWF.x));
        rF.rgb = StpSatMF3(rF.rgb * StpMF3_(penNotWF.y) + penC * StpMF3_(penWF.y));
        rW.rgb = clamp(rW.rgb, minNe, maxNe);
        rF.rgb = clamp(rF.rgb, minNe, maxNe);
//------------------------------------------------------------------------------------------------------------------------------
        rW.rgb *= rW.rgb;
        #if (STP_POSTMAP == 0)
            StpToneInvMF3(rW.rgb);
        #endif // (STP_POSTMAP == 0)
        rF.a = rW.a = StpMF1(0.0); }
#endif // defined(STP_GPU) && defined(STP_TAA) && defined(STP_32BIT)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                         16-BIT PATH
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_TAA) && defined(STP_16BIT)
    // Callbacks.
    // Gather4 of GEAA control data.
    StpH4 StpTaaCtl4H(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // Current frame {color,anti} input.
    // Gather4 specific channels.
    StpH4 StpTaaCol4RH(StpF2 p);
    StpH4 StpTaaCol4GH(StpF2 p);
    StpH4 StpTaaCol4BH(StpF2 p);
    StpH4 StpTaaCol4AH(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // Bilinear sampling of low-frequency convergence.
    StpH1 StpTaaConH(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // Dither value {0 to 1} this should be output pixel frequency spatial temporal blue noise.
    StpH1 StpTaaDitH(StpW2 o);
//------------------------------------------------------------------------------------------------------------------------------
    // Gather4 current frame motion {z,x,y} packed input, same as the 32-bit version (just renamed).
    StpU4 StpTaaMot4H(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // Feedback {color, alpha}.
    // Bilinear fetch with clamp to edge.
    StpH4 StpTaaPriFedH(StpF2 p);
    // Gather4.
    StpH4 StpTaaPriFed4RH(StpF2 p);
    StpH4 StpTaaPriFed4GH(StpF2 p);
    StpH4 StpTaaPriFed4BH(StpF2 p);
    // Min/max sampling used for dering.
    #if STP_MAX_MIN_10BIT
        StpH4 StpTaaPriFedMaxH(StpF2 p);
        StpH4 StpTaaPriFedMinH(StpF2 p);
    #endif // STP_MAX_MIN_10BIT
    // Sampling with offsets.
    #if STP_OFFSETS
        StpH4 StpTaaPriFedOH(StpF2 p, StpI2 o);
        StpH4 StpTaaPriFed4ROH(StpF2 p, StpI2 o);
        StpH4 StpTaaPriFed4GOH(StpF2 p, StpI2 o);
        StpH4 StpTaaPriFed4BOH(StpF2 p, StpI2 o);
    #endif // STP_OFFSETS
//==============================================================================================================================
    void StpTaaH(
    StpW1 lane,   // Currently unused but in the interface for possible future expansion.
    StpW2 o,      // Integer pixel offset in output.
    out StpH4 rF, // Return Feedback (to be stored).
    out StpH4 rW, // Return Output (to be stored).
    StpU4 con0,   // Constants generated by StpTaaCon().
    StpU4 con1,
    StpU4 con2,
    StpU4 con3) {
//------------------------------------------------------------------------------------------------------------------------------
        // This is only currently used for debug.
        StpH1 dit = StpTaaDitH(o);
//------------------------------------------------------------------------------------------------------------------------------
        // Rename constants.
        StpF2 kCRcpF = StpF2_U2(con0.xy);
        StpF2 kHalfCRcpFUnjitC = StpF2_U2(con0.zw);
        StpF2 kRcpC = StpF2_U2(con1.xy);
        StpF2 kRcpF = StpF2_U2(con1.zw);
        StpF2 kHalfRcpF = StpF2_U2(con2.xy);
        StpF2 kJitCRcpC0 = StpF2_U2(con2.zw);
        StpF2 kHalfRcpC = StpF2_U2(con3.xy);
        StpF2 kF = StpF2_U2(con3.zw);
//------------------------------------------------------------------------------------------------------------------------------
        // Check the streaming bandwidth limit.
        #if STP_BUG_BW_SOL
        {   StpF2 oo = StpF2(o) * kRcpF;
            StpH4 g4 = StpTaaCtl4RH(oo);
            StpU4 m4 = StpTaaMot4H(oo);
            StpH1 cnv = StpTaaConH(oo);
            StpH4 f = StpTaaPriFedH(oo);
            StpH4 c4R = StpTaaCol4RH(oo);
            rW = rF = l4 + g4 + StpH4(m4) + StpH4_(cnv) + f + c4R;
            return; }
        #endif // STP_BUG_BW_SOL
//------------------------------------------------------------------------------------------------------------------------------
        // Locate 2x2 neighborhood.
        // Float version of integer pixel offset in output.
        // All the 'o' prefixed variables are offset (aka position/coordinate) related.
        StpF2 oI = StpF2(o);
        // This gets to the center of the 2x2 quad directly because of possibility of shader/tex precision mismatch.
        // Precision mismatch could yield different 2x2 quads.
        StpF2 oC = oI * kCRcpF + kHalfCRcpFUnjitC;
        // NW of 2x2 quad.
        StpF2 oCNW = floor(oC + StpF2_(-0.5));
        // Center of the 2x2 quad.
        StpF2 oC4 = oCNW * kRcpC + kRcpC;
        // Coordinates for low frequency convergence.
        StpF2 oC1 = oC * kRcpC;
//==============================================================================================================================
//      FETCH {CONVERGENCE, COLOR, CONTROL, Z+MOTION}
//==============================================================================================================================
        // Fetch low-frequency convergence.
        StpH1 cnv = StpTaaConH(oC1);
        // Fetch color.
        StpH4 c4R = StpTaaCol4RH(oC4);
        StpH4 c4G = StpTaaCol4GH(oC4);
        StpH4 c4B = StpTaaCol4BH(oC4);
        StpH4 c4A = StpTaaCol4AH(oC4);
        // Control (GEAA weights)
        StpH4 g4 = StpTaaCtl4H(oC4);
        // Fetch {z,motion}.
        StpU4 m4 = StpTaaMot4H(oC4);
//------------------------------------------------------------------------------------------------------------------------------
//      INDEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
        // Setup resolve position {0 to 1} inside 2x2 quad.
        // The extra -0.5 is to get from NW position to center.
        StpH2 rP = StpH2(oC - oCNW) - StpH2_(0.5);
//------------------------------------------------------------------------------------------------------------------------------
        // The 'rP' is resolve position {0 to 1} inside 2x2 quad, this is distance to ends of 2x2.
        // Instead of using {a,a-1} this uses {a,1-a} for reuse with the simple angular filtering.
        StpH2 rPX10 = StpH2(1.0, 0.0) + StpH2(-rP.x, rP.x);
        StpH2 rPY01 = StpH2(0.0, 1.0) + StpH2(rP.y, -rP.y);
        // Distance^2 {0 := on, 1 := off}.
        StpH4 pen4x = StpH4(rPX10.g, rPX10.r, rPX10.r, rPX10.g);
        StpH4 pen4y = StpH4(rPY01.g, rPY01.g, rPY01.r, rPY01.r);
        // Pen starts with distance squared to all 2x2 points.
        StpH4 pen4 = StpSatH4(pen4x * pen4x + pen4y * pen4y);
//==============================================================================================================================
//      DEPENDENT ON {CONVERGENCE}
//==============================================================================================================================
        // Low frequency convergence keeps the next frame value, so subtract one frame.
        cnv = StpSatH1(cnv - StpH1_(1.0 / STP_FRAME_MAX));
//------------------------------------------------------------------------------------------------------------------------------
        // Pen size based on convergence.
        StpH1 pen = StpH1_(cnv) * StpH1_(STP_FRAME_MAX) + StpH1_(1.0);
        pen = StpPrxLoSqrtH1(pen);
        pen4 = StpSatH4(StpH4_(1.0) - pen4 * StpH4_(pen));
        #if defined(STP_16BIT)
            StpH2 pen2 = pen4.xy * pen4.xy + pen4.zw * pen4.zw;
            pen = StpSatH1(pen2.x + pen2.y);
        #else // defined(STP_16BIT)
            pen = StpSatMF1(pen4.x * pen4.x + pen4.y * pen4.y + pen4.z * pen4.z + pen4.w * pen4.w);
        #endif // defined(STP_16BIT)
//==============================================================================================================================
//      DEPENDENT ON {COLOR}
//==============================================================================================================================
        // Simple angular filtering (gets rid of block artifacts, adds sawtooth artifacts which are not a problem in practice).
        // Create a GEAA based weighting for no temporal feedback case.
        StpH4 wG;
        // Selects between either (S) or (T).
        //  (S) A--B ... (T) A--B
        //      |\ |         | /|
        //      | \|         |/ |
        //      R--G         R--G
        // S and T only use the other diagonal.
        // Exact luma not required.
        StpH4 l4 = c4R + c4G * StpH4_(2.0) + c4B;
        StpH2 difST = abs(l4.gr - l4.ab);
        // Choose configuration based on which difference is maximum.
        StpP1 useS = difST.x > difST.y;
        // Choose interpolation weights given the configuration.
        //      _T__________  _S__________
        //  R | sat( -x+  y)  min(1-x,  y) = y-G
        //  G | min(  x,  y)  sat(x-1+  y) = y-R
        //  B | sat(  x-  y)  min(  x,1-y) = (1-y)-A
        //  A | min(1-x,1-y)  sat(1-x-  y) = (1-y)-B
        // Difference between S and T is a {x} vs {1-x} and a RGBA vs GRAB swap.
        StpH2 wTrb = StpSatH2(StpH2(-rP.x, rP.x) + StpH2(rP.y, -rP.y));
        StpH2 wSrb = min(rPX10, rPY01);
        if(useS) wTrb = wSrb;
        StpH2 wTga = rPY01 - wTrb;
        wG.rg = StpH2(wTrb.x, wTga.x);
        wG.ba = StpH2(wTrb.y, wTga.y);
        // Shaping is needed to get good high area scaling (remove the transition region).
        wG *= wG;
        wG *= wG;
//------------------------------------------------------------------------------------------------------------------------------
        // Scale directional interpolation weights by GEAA weights to introduce anti-aliasing.
        wG *= g4;
        // Triangular nearest.
        // This works by removing the corner which contributes the least to the spatial interpolated result.
        StpH4 triMask = StpH4_(1.0);
        StpH2 wGmin2 = min(wG.xy, wG.zw);
//==============================================================================================================================
//      DEPENDENT ON {Z,MOTION}
//==============================================================================================================================
        // This overwrites gather4 results.
        if(wGmin2.x < wGmin2.y) {
            if(wG.x < wG.z) { triMask.x = StpH1_(STP_TAA_TRI_MASK_AVOID); m4.x = 0xFFFFFFFF; }
            else            { triMask.z = StpH1_(STP_TAA_TRI_MASK_AVOID); m4.z = 0xFFFFFFFF; } }
        else {
            if(wG.y < wG.w) { triMask.y = StpH1_(STP_TAA_TRI_MASK_AVOID); m4.y = 0xFFFFFFFF; }
            else            { triMask.w = StpH1_(STP_TAA_TRI_MASK_AVOID); m4.w = 0xFFFFFFFF; } }
        StpU1 m1 = min(StpMin3U1(m4.x, m4.y, m4.z), m4.w);
//------------------------------------------------------------------------------------------------------------------------------
        // Want to consume 'triMask' to free up register space.
        wG *= triMask;
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 mXY;
        // Motion 'm' units are {1 := move by one screen}.
        StpMvUnpackV(mXY, m1);
//==============================================================================================================================
//      GET ALL FEEDBACK FILTERING DONE
//==============================================================================================================================
        // This region of code will have the highest register pressure in some configs, so doing as early as possible.
        // Setup for fetch feedback.
        StpF2 oF = oI * kRcpF + kHalfRcpF - mXY;
//------------------------------------------------------------------------------------------------------------------------------
        StpH3 f;
        // Lanczos common.
        #if STP_TAA_PRX_LANCZOS
            // Motion reprojection position in feedback pixels.
            StpF2 oM = oI + StpF2_(0.5) - mXY * kF;
            // NW of center 2x2 quad.
            StpF2 oMNW = floor(oM + StpF2_(-0.5));
            // Center of the center 2x2 quad.
            StpF2 oM4 = oMNW * kRcpF + kRcpF;
            StpH3 fMax, fMin;
        #else // STP_TAA_PRX_LANCZOS
            // Sample nearest feedback.
            f = StpTaaPriFedH(oF).rgb;
        #endif // STP_TAA_PRX_LANCZOS
//==============================================================================================================================
        #if (STP_TAA_PRX_LANCZOS == 1)
            // This one does a fixed 1x4 to try to cut cost in half relative to the complete 4x4.
            // It uses bilinear sampling on the 'x'.
            // Lanczos on the 'y' because most floating camera motion is 'y' based.
            // Fetch {feedback}.
            #if STP_OFFSETS
                // TODO: Can optimize out the 'oM4.y' add with constant change.
                StpF2 oM0 = StpF2(oF.x, oM4.y + kRcpF.y * StpF1_(-1.5));
                StpH3 f0 = StpTaaPriFedH(oM0).rgb;
                StpH3 f1 = StpTaaPriFedOH(oM0, StpI2(0, 1)).rgb;
                StpH3 f2 = StpTaaPriFedOH(oM0, StpI2(0, 2)).rgb;
                StpH3 f3 = StpTaaPriFedOH(oM0, StpI2(0, 3)).rgb;
            #else // STP_OFFSETS
                StpF2 oM0 = StpF2(oF.x, oM4.y + kRcpF.y * StpF1_(-1.5));
                StpF2 oM1 = StpF2(oF.x, oM4.y + kRcpF.y * StpF1_(-0.5));
                StpF2 oM2 = StpF2(oF.x, oM4.y + kRcpF.y * StpF1_( 0.5));
                StpF2 oM3 = StpF2(oF.x, oM4.y + kRcpF.y * StpF1_( 1.5));
                StpH3 f0 = StpTaaPriFedH(oM0).rgb;
                StpH3 f1 = StpTaaPriFedH(oM1).rgb;
                StpH3 f2 = StpTaaPriFedH(oM2).rgb;
                StpH3 f3 = StpTaaPriFedH(oM3).rgb;
            #endif // STP_OFFSETS
            // Want this last because it's used last.
            #if (STP_MAX_MIN_10BIT && STP_TAA_PRX_LANCZOS_DERING)
                fMax = StpTaaPriFedMaxH(oM4).rgb;
                fMin = StpTaaPriFedMinH(oM4).rgb;
            #endif // (STP_MAX_MIN_10BIT && STP_TAA_PRX_LANCZOS_DERING)
            #if ((STP_MAX_MIN_10BIT == 0) && STP_TAA_PRX_LANCZOS_DERING)
                // Without {min,max} sampling, must gather4.
                StpH4 f4R = StpTaaPriFed4RH(oM4);
                StpH4 f4G = StpTaaPriFed4GH(oM4);
                StpH4 f4B = StpTaaPriFed4BH(oM4);
            #endif // ((STP_MAX_MIN_10BIT == 0) && STP_TAA_PRX_LANCZOS_DERING)
//------------------------------------------------------------------------------------------------------------------------------
//          INDEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
            // Convert to approximate lanczos weights.
            // Feedback position {0 to 1} inside 2x2 quad + 0.5.
            StpH2 fP = StpH2(oM - oMNW);
            // Convert to approximate lanczos weights.
            // This converts {-2 to 2} to {-1 to 1} because the kernel approximation is written for {-1 to 1}.
            StpH4 fPY = StpH4_(-fP.y * StpH1_(0.5)) + StpH4(-0.5 * 0.5, 0.5 * 0.5, 1.5 * 0.5, 2.5 * 0.5);
            // Weights in one axis.
            fPY = StpSatH4(StpH4_(1.0) - fPY * fPY);
            fPY *= fPY;
            StpH4 fPY4 = fPY * fPY;
            // ^6 (slightly more negative lobe than lanczos 2, slightly less expensive)
            fPY = (StpH4_(1.0 + 81.0 / 175.0) * fPY4 - StpH4_(81.0 / 175.0)) * fPY;
            #if defined(STP_16BIT)
                StpH2 fRcp2 = fPY.rg + fPY.ba;
                StpH1 fRcp = StpPrxLoRcpH1(fRcp2.x + fRcp2.y);
            #else // defined(STP_16BIT)
                StpMF1 fRcp = StpPrxLoRcpMF1(fPY.r + fPY.g + fPY.b + fPY.a);
            #endif // defined(STP_16BIT)
//------------------------------------------------------------------------------------------------------------------------------
//          DEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
            f.rgb = f0 * StpH3_(fPY.r) + f1 * StpH3_(fPY.g) + f2 * StpH3_(fPY.b) + f3 * StpH3_(fPY.a);
            f.rgb *= StpH3_(fRcp);
            #if STP_TAA_PRX_LANCZOS_DERING
                #if (STP_MAX_MIN_10BIT == 0)
                    #if defined(STP_16BIT)
                        StpH2 fXnyR = max(max(StpH2(f4R.x, -f4R.x), StpH2(f4R.y, -f4R.y)),
                                          max(StpH2(f4R.z, -f4R.z), StpH2(f4R.w, -f4R.w)));
                        StpH2 fXnyG = max(max(StpH2(f4G.x, -f4G.x), StpH2(f4G.y, -f4G.y)),
                                          max(StpH2(f4G.z, -f4G.z), StpH2(f4G.w, -f4G.w)));
                        StpH2 fXnyB = max(max(StpH2(f4B.x, -f4B.x), StpH2(f4B.y, -f4B.y)),
                                          max(StpH2(f4B.z, -f4B.z), StpH2(f4B.w, -f4B.w)));
                        f = clamp(f, StpH3(-fXnyR.y, -fXnyG.y, -fXnyB.y), StpH3(fXnyR.x, fXnyG.x, fXnyB.x));
                    #else // defined(STP_16BIT)
                        fMax.r = max(StpMax3H1(f4R.x, f4R.y, f4R.z), f4R.w);
                        fMax.g = max(StpMax3H1(f4G.x, f4G.y, f4G.z), f4G.w);
                        fMax.b = max(StpMax3H1(f4B.x, f4B.y, f4B.z), f4B.w);
                        fMin.r = min(StpMin3H1(f4R.x, f4R.y, f4R.z), f4R.w);
                        fMin.g = min(StpMin3H1(f4G.x, f4G.y, f4G.z), f4G.w);
                        fMin.b = min(StpMin3H1(f4B.x, f4B.y, f4B.z), f4B.w);
                        f = clamp(f, fMin, fMax);
                    #endif // defined(STP_16BIT)
                #else // (STP_MAX_MIN_10BIT == 0)
                    // Leaning on {min,max} sampling so no 16/32-bit permutation.
                    f = clamp(f, fMin, fMax);
                #endif // (STP_MAX_MIN_10BIT == 0)
            #endif // STP_TAA_PRX_LANCZOS_DERING
        #endif // (STP_TAA_PRX_LANCZOS == 1)
//==============================================================================================================================
        #if (STP_TAA_PRX_LANCZOS == 2)
            // Unstable approximate lanczos feedback, full 4x4.
            //  a = saturate(1-x*x)
            //  u = 1+v
            //  v = moves the zero crossing to 0.5
            //  w = adjusts the shape
            //  u*a^w - v*a^2
            // Fetch {feedback}.
            //  0w 0z 1w 1z | R
            //  0x 0y 1x 1y | G
            //  2w 2z 3w 3z | B
            //  2x 2y 3x 3y | A
            //  -- -- -- --
            //  R  G  B  A
            #if STP_OFFSETS
                StpH4 f4R0 = StpTaaPriFed4ROH(oM4, StpI2(-1, -1));
                StpH4 f4G0 = StpTaaPriFed4GOH(oM4, StpI2(-1, -1));
                StpH4 f4B0 = StpTaaPriFed4BOH(oM4, StpI2(-1, -1));
                StpH4 f4R1 = StpTaaPriFed4ROH(oM4, StpI2( 1, -1));
                StpH4 f4G1 = StpTaaPriFed4GOH(oM4, StpI2( 1, -1));
                StpH4 f4B1 = StpTaaPriFed4BOH(oM4, StpI2( 1, -1));
                StpH4 f4R2 = StpTaaPriFed4ROH(oM4, StpI2(-1,  1));
                StpH4 f4G2 = StpTaaPriFed4GOH(oM4, StpI2(-1,  1));
                StpH4 f4B2 = StpTaaPriFed4BOH(oM4, StpI2(-1,  1));
                StpH4 f4R3 = StpTaaPriFed4ROH(oM4, StpI2( 1,  1));
                StpH4 f4G3 = StpTaaPriFed4GOH(oM4, StpI2( 1,  1));
                StpH4 f4B3 = StpTaaPriFed4BOH(oM4, StpI2( 1,  1));
            #else // STP_OFFSETS
                StpF2 oM0 = oM4 + StpF2(-kRcpF.x, -kRcpF.y);
                StpF2 oM1 = oM4 + StpF2( kRcpF.x, -kRcpF.y);
                StpF2 oM2 = oM4 + StpF2(-kRcpF.x,  kRcpF.y);
                StpF2 oM3 = oM4 + StpF2( kRcpF.x,  kRcpF.y);
                StpH4 f4R0 = StpTaaPriFed4RH(oM0);
                StpH4 f4G0 = StpTaaPriFed4GH(oM0);
                StpH4 f4B0 = StpTaaPriFed4BH(oM0);
                StpH4 f4R1 = StpTaaPriFed4RH(oM1);
                StpH4 f4G1 = StpTaaPriFed4GH(oM1);
                StpH4 f4B1 = StpTaaPriFed4BH(oM1);
                StpH4 f4R2 = StpTaaPriFed4RH(oM2);
                StpH4 f4G2 = StpTaaPriFed4GH(oM2);
                StpH4 f4B2 = StpTaaPriFed4BH(oM2);
                StpH4 f4R3 = StpTaaPriFed4RH(oM3);
                StpH4 f4G3 = StpTaaPriFed4GH(oM3);
                StpH4 f4B3 = StpTaaPriFed4BH(oM3);
            #endif // STP_OFFSETS
            // Want this last because it's used last.
            #if (STP_MAX_MIN_10BIT && STP_TAA_PRX_LANCZOS_DERING)
                fMax = StpTaaPriFedMaxH(oM4).rgb;
                fMin = StpTaaPriFedMinH(oM4).rgb;
            #endif // (STP_MAX_MIN_10BIT && STP_TAA_PRX_LANCZOS_DERING)
//------------------------------------------------------------------------------------------------------------------------------
//          INDEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
            // Feedback position {0 to 1} inside 2x2 quad + 0.5.
            StpH2 fP = StpH2(oM - oMNW);
            // Convert to approximate lanczos weights.
            // This converts {-2 to 2} to {-1 to 1} because the kernel approximation is written for {-1 to 1}.
            StpH4 fPX = StpH4_(-fP.x * StpH1_(0.5)) + StpH4(-0.5 * 0.5, 0.5 * 0.5, 1.5 * 0.5, 2.5 * 0.5);
            StpH4 fPY = StpH4_(-fP.y * StpH1_(0.5)) + StpH4(-0.5 * 0.5, 0.5 * 0.5, 1.5 * 0.5, 2.5 * 0.5);
            // Weights in both axis.
            fPX = StpSatH4(StpH4_(1.0) - fPX * fPX);
            fPY = StpSatH4(StpH4_(1.0) - fPY * fPY);
            fPX *= fPX;
            fPY *= fPY;
            StpH4 fPX4 = fPX * fPX;
            StpH4 fPY4 = fPY * fPY;
            // ^6 (slightly more negative lobe than lanczos 2, slightly less expensive)
            fPX = (StpH4_(1.0 + 81.0 / 175.0) * fPX4 - StpH4_(81.0 / 175.0)) * fPX;
            fPY = (StpH4_(1.0 + 81.0 / 175.0) * fPY4 - StpH4_(81.0 / 175.0)) * fPY;
            #if defined(STP_16BIT)
                StpH2 fRcpX = fPX.rg + fPX.ba;
                StpH2 fRcpY = fPY.rg + fPY.ba;
                fPX *= StpH4_(StpPrxLoRcpH1(fRcpX.r + fRcpX.y));
                fPY *= StpH4_(StpPrxLoRcpH1(fRcpY.r + fRcpY.y));
            #else // defined(STP_16BIT)
                fPX *= StpMF4_(StpPrxLoRcpMF1(fPX.r + fPX.g + fPX.b + fPX.a));
                fPY *= StpMF4_(StpPrxLoRcpMF1(fPY.r + fPY.g + fPY.b + fPY.a));
            #endif // defined(STP_16BIT)
            StpH4 fPX0 = fPX * StpH4_(fPY.r);
            StpH4 fPX1 = fPX * StpH4_(fPY.g);
            StpH4 fPX2 = fPX * StpH4_(fPY.b);
            StpH4 fPX3 = fPX * StpH4_(fPY.a);
//------------------------------------------------------------------------------------------------------------------------------
//          DEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
            #if defined(STP_16BIT)
                StpH2 fR2 = f4R0.wz * fPX0.xy + f4R1.wz * fPX0.zw + f4R0.xy * fPX1.xy + f4R1.xy * fPX1.zw +
                            f4R2.wz * fPX2.xy + f4R3.wz * fPX2.zw + f4R2.xy * fPX3.xy + f4R3.xy * fPX3.zw;
                StpH2 fG2 = f4G0.wz * fPX0.xy + f4G1.wz * fPX0.zw + f4G0.xy * fPX1.xy + f4G1.xy * fPX1.zw +
                            f4G2.wz * fPX2.xy + f4G3.wz * fPX2.zw + f4G2.xy * fPX3.xy + f4G3.xy * fPX3.zw;
                StpH2 fB2 = f4B0.wz * fPX0.xy + f4B1.wz * fPX0.zw + f4B0.xy * fPX1.xy + f4B1.xy * fPX1.zw +
                            f4B2.wz * fPX2.xy + f4B3.wz * fPX2.zw + f4B2.xy * fPX3.xy + f4B3.xy * fPX3.zw;
                f = StpH3(fR2.x + fR2.y, fG2.x + fG2.y, fB2.x + fB2.y);
            #else // defined(STP_16BIT)
                f.r = f4R0.w * fPX0.r + f4R0.z * fPX0.g + f4R1.w * fPX0.b + f4R1.z * fPX0.a +
                      f4R0.x * fPX1.r + f4R0.y * fPX1.g + f4R1.x * fPX1.b + f4R1.y * fPX1.a +
                      f4R2.w * fPX2.r + f4R2.z * fPX2.g + f4R3.w * fPX2.b + f4R3.z * fPX2.a +
                      f4R2.x * fPX3.r + f4R2.y * fPX3.g + f4R3.x * fPX3.b + f4R3.y * fPX3.a;
                f.g = f4G0.w * fPX0.r + f4G0.z * fPX0.g + f4G1.w * fPX0.b + f4G1.z * fPX0.a +
                      f4G0.x * fPX1.r + f4G0.y * fPX1.g + f4G1.x * fPX1.b + f4G1.y * fPX1.a +
                      f4G2.w * fPX2.r + f4G2.z * fPX2.g + f4G3.w * fPX2.b + f4G3.z * fPX2.a +
                      f4G2.x * fPX3.r + f4G2.y * fPX3.g + f4G3.x * fPX3.b + f4G3.y * fPX3.a;
                f.b = f4B0.w * fPX0.r + f4B0.z * fPX0.g + f4B1.w * fPX0.b + f4B1.z * fPX0.a +
                      f4B0.x * fPX1.r + f4B0.y * fPX1.g + f4B1.x * fPX1.b + f4B1.y * fPX1.a +
                      f4B2.w * fPX2.r + f4B2.z * fPX2.g + f4B3.w * fPX2.b + f4B3.z * fPX2.a +
                      f4B2.x * fPX3.r + f4B2.y * fPX3.g + f4B3.x * fPX3.b + f4B3.y * fPX3.a;
            #endif // defined(STP_16BIT)
            #if STP_TAA_PRX_LANCZOS_DERING
                #if (STP_MAX_MIN_10BIT == 0)
                    #if defined(STP_16BIT)
                        StpH2 fXnyR = max(max(StpH2(f4R0.y, -f4R0.y), StpH2(f4R1.x, -f4R1.x)),
                                          max(StpH2(f4R2.z, -f4R2.z), StpH2(f4R3.w, -f4R3.w)));
                        StpH2 fXnyG = max(max(StpH2(f4G0.y, -f4G0.y), StpH2(f4G1.x, -f4G1.x)),
                                          max(StpH2(f4G2.z, -f4G2.z), StpH2(f4G3.w, -f4G3.w)));
                        StpH2 fXnyB = max(max(StpH2(f4B0.y, -f4B0.y), StpH2(f4B1.x, -f4B1.x)),
                                          max(StpH2(f4B2.z, -f4B2.z), StpH2(f4B3.w, -f4B3.w)));
                        f = clamp(f, StpH3(-fXnyR.y, -fXnyG.y, -fXnyB.y), StpH3(fXnyR.x, fXnyG.x, fXnyB.x));
                    #else // defined(STP_16BIT)
                        fMax.r = max(StpMax3H1(f4R0.y, f4R1.x, f4R2.z), f4R3.w);
                        fMax.g = max(StpMax3H1(f4G0.y, f4G1.x, f4G2.z), f4G3.w);
                        fMax.b = max(StpMax3H1(f4B0.y, f4B1.x, f4B2.z), f4B3.w);
                        fMin.r = min(StpMin3H1(f4R0.y, f4R1.x, f4R2.z), f4R3.w);
                        fMin.g = min(StpMin3H1(f4G0.y, f4G1.x, f4G2.z), f4G3.w);
                        fMin.b = min(StpMin3H1(f4B0.y, f4B1.x, f4B2.z), f4B3.w);
                        f = clamp(f, fMin, fMax);
                    #endif // defined(STP_16BIT)
                #else // (STP_MAX_MIN_10BIT == 0)
                    // Leaning on {min,max} sampling so no 16/32-bit permutation.
                    f = clamp(f, fMin, fMax);
                #endif // (STP_MAX_MIN_10BIT == 0)
            #endif // STP_TAA_PRX_LANCZOS_DERING
        #endif // (STP_TAA_PRX_LANCZOS == 2)
//==============================================================================================================================
//      DISPLACEMENT
//==============================================================================================================================
        // Note the 'kJitCRcpC0' gets to position 0 to save some runtime maths.
        //  3 2
        //  0 1
        StpF2 oD0 = oC4 + kJitCRcpC0 - mXY;
        StpF2 oD1 = StpF2(kRcpC.x,      0.0) + oD0;
        StpF2 oD2 = StpF2(kRcpC.x, -kRcpC.y) + oD0;
        StpF2 oD3 = StpF2(0.0,     -kRcpC.y) + oD0;
        StpH3 d0 = StpTaaPriFedH(oD0).rgb;
        StpH3 d1 = StpTaaPriFedH(oD1).rgb;
        StpH3 d2 = StpTaaPriFedH(oD2).rgb;
        StpH3 d3 = StpTaaPriFedH(oD3).rgb;
//------------------------------------------------------------------------------------------------------------------------------
//      INDEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
        // Normalize interpolation weights.
        #if defined(STP_16BIT)
            StpH2 wG2 = wG.xy + wG.zw;
            wG = StpSatH4(wG * StpH4_(StpPrxLoRcpH1(wG2.x + wG2.y)));
        #else // defined(STP_16BIT)
            wG = StpSatMF4(wG * StpMF4_(StpPrxLoRcpMF1(wG.x + wG.y + wG.z + wG.w)));
        #endif // defined(STP_16BIT)
//------------------------------------------------------------------------------------------------------------------------------
        // Temporal weighting.
        StpH4 wT = abs(c4R - StpH4_(f.r)) * StpH4_(STP_LUMA_R) +
                   abs(c4G - StpH4_(f.g)) * StpH4_(STP_LUMA_G) +
                   abs(c4B - StpH4_(f.b)) * StpH4_(STP_LUMA_B);
        wT = StpPrxLoRcpH4(wT * StpH4_(STP_ANTI_MAX) + StpH4_(STP_ANTI_MIN)) * triMask;
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_16BIT)
            StpH2 wT2 = wT.xy + wT.zw;
            wT = StpSatH4(wT * StpH4_(StpPrxLoRcpH1(wT2.x + wT2.y)));
        #else // defined(STP_16BIT)
            wT = StpSatMF4(wT * StpMF4_(StpPrxLoRcpMF1(wT.x + wT.y + wT.z + wT.w)));
        #endif // defined(STP_16BIT)
//------------------------------------------------------------------------------------------------------------------------------
        // Interpolate match.
        // Using a fixed 50/50 split of two normalized weights yields a normalized weight.
        StpH4 wM = wT * StpH4_(0.5) + wG * StpH4_(0.5);
        #if defined(STP_16BIT)
            StpH2 match2 = (c4A.xy * wM.xy) + (c4A.zw * wM.zw);
            StpH1 match = match2.x + match2.y;
        #else // defined(STP_16BIT)
            StpMF1 match = c4A.x * wM.x + c4A.y * wM.y + c4A.z * wM.z + c4A.w * wM.w;
        #endif // defined(STP_16BIT)
        // Non-motion-match kills convergence for this frame only.
        cnv *= match;
//------------------------------------------------------------------------------------------------------------------------------
//      DEPENDENT
//------------------------------------------------------------------------------------------------------------------------------
        // Interpolation, this first section doesn't have gather4, so probably no gain in swizzling.
        StpH3 dG = d0 * StpH3_(wG.x) + d1 * StpH3_(wG.y) + d2 * StpH3_(wG.z) + d3 * StpH3_(wG.w);
        StpH3 dT = d0 * StpH3_(wT.x) + d1 * StpH3_(wT.y) + d2 * StpH3_(wT.z) + d3 * StpH3_(wT.w);
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_16BIT)
            StpH2 t2R = (c4R.xy * wT.xy) + (c4R.zw * wT.zw);
            StpH2 t2G = (c4G.xy * wT.xy) + (c4G.zw * wT.zw);
            StpH2 t2B = (c4B.xy * wT.xy) + (c4B.zw * wT.zw);
            StpH3 t = StpH3(t2R.x + t2R.y, t2G.x + t2G.y, t2B.x + t2B.y);
            StpH2 c2R = (c4R.xy * wG.xy) + (c4R.zw * wG.zw);
            StpH2 c2G = (c4G.xy * wG.xy) + (c4G.zw * wG.zw);
            StpH2 c2B = (c4B.xy * wG.xy) + (c4B.zw * wG.zw);
            StpH3 c = StpH3(c2R.x + c2R.y, c2G.x + c2G.y, c2B.x + c2B.y);
        #else // defined(STP_16BIT)
            StpMF3 t = StpMF3(
                c4R.x * wT.x + c4R.y * wT.y + c4R.z * wT.z + c4R.w * wT.w,
                c4G.x * wT.x + c4G.y * wT.y + c4G.z * wT.z + c4G.w * wT.w,
                c4B.x * wT.x + c4B.y * wT.y + c4B.z * wT.z + c4B.w * wT.w);
            StpMF3 c = StpMF3(
                c4R.x * wG.x + c4R.y * wG.y + c4R.z * wG.z + c4R.w * wG.w,
                c4G.x * wG.x + c4G.y * wG.y + c4G.z * wG.z + c4G.w * wG.w,
                c4B.x * wG.x + c4B.y * wG.y + c4B.z * wG.z + c4B.w * wG.w);
        #endif // defined(STP_16BIT)
//------------------------------------------------------------------------------------------------------------------------------
        // Neighborhood.
        StpH1 bln = StpSatH1(cnv * StpPrxLoRcpH1(cnv + StpH1_(1.0 / STP_FRAME_MAX)));
        StpH1 blnT = StpH1_(1.0) - bln;
        StpH3 b = f * StpH3_(bln) + t * StpH3_(blnT);
        StpH3 minNe = min(c, b);
        StpH3 maxNe = max(c, b);
//------------------------------------------------------------------------------------------------------------------------------
        // Apply pen.
        StpH3 penC = StpSatH3(c + (f - dG) * StpH3_(StpH1_(0.9875) * match));
        StpH2 penWF;
        penWF.x = pen * StpH1_(STP_TAA_PEN_W);
        penWF.y = pen * lerp(StpH1_(STP_TAA_PEN_F0), StpH1_(STP_TAA_PEN_F1), cnv);
        StpH2 penNotWF = StpH2_(1.0) - penWF;
        rF.rgb = t + (f - dT);
        rF.rgb = rF.rgb * StpH3_(blnT) + f * StpH3_(bln);
        rW.rgb = StpSatH3(rF.rgb * StpH3_(penNotWF.x) + penC * StpH3_(penWF.x));
        rF.rgb = StpSatH3(rF.rgb * StpH3_(penNotWF.y) + penC * StpH3_(penWF.y));
        rW.rgb = clamp(rW.rgb, minNe, maxNe);
        rF.rgb = clamp(rF.rgb, minNe, maxNe);
//------------------------------------------------------------------------------------------------------------------------------
        // Get back into linear, and then HDR.
        rW.rgb *= rW.rgb;
        #if (STP_POSTMAP == 0)
            StpToneInvH3(rW.rgb);
        #endif // (STP_POSTMAP == 0)
        // Alpha is currently unused, this might improve compression (vs undefined).
        rF.a = rW.a = StpH1(0.0); }
#endif // defined(STP_GPU) && defined(STP_TAA) && defined(STP_16BIT)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//
//                                                GOOD ENOUGH ANTI-ALIASING [GEAA]
//
//------------------------------------------------------------------------------------------------------------------------------
// Yet another simplified spatial morphological AA.
// Not perfect, but it has low complexity (one pass), and is good enough for a TAA override.
// Fails on longer edges (due to low maximum search), doesn't get diagonals perfect.
// But good on already part AA'ed inputs.
// The spatial AA is not used in STP, only a weighting value which is later used to guide a quick-and-dirty scalar.
// With some modification this could be used for spatial AA, with or without scaling.
//------------------------------------------------------------------------------------------------------------------------------
// CALLBACKS
// =========
// StpMF4 StpGeaa4F(StpF2 p) - Gather4 of luma (or green as luma).
// ---------
// StpH4 StpGeaa4H(StpF2 p)
//==============================================================================================================================
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                      [GEAA] DEFAULTS
//==============================================================================================================================
// Choose a configuration of number of positions to sample.
//  0 ... 3 per side (faster, less quality)
//  1 ... 5 per side
//  2 ... 7 per side
//  3 ... 9 per side (slower, higher quality)
#ifndef STP_GEAA_P
    #define STP_GEAA_P 3
#endif // STP_GEAA_P
//------------------------------------------------------------------------------------------------------------------------------
// Amount of sub-pixel blur.
//  0.50 ... Turn it off
//  0.25 ... Middle ground
//  0.00 ... More blur
#ifndef STP_GEAA_SUBPIX
    #define STP_GEAA_SUBPIX (8.0 / 16.0)
#endif // STP_GEAA_SUBPIX
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                  [GEAA] INTERNAL TUNING
//==============================================================================================================================
// Higher numbers can reduce the amount of AA, lower numbers can increase it but can look dirty.
// Best not to mess with this, 1/3 is the 'correct' value for 2 of the 3 edge cases.
#define STP_GEAA_THRESHOLD (1.0/3.0)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                  [GEAA] 32-BIT ENTRY POINT
//==============================================================================================================================
// See the 16-bit version for all comments.
#if defined(STP_GPU) && defined(STP_GEAA) && defined(STP_32BIT)
    void StpGeaaF(
    out StpMF1 gW, out StpMF1 gLuma, out StpF2 gFilter, out StpF2 gDilate, StpF2 p, StpF2 kRcpI, StpF2 kHalfRcpI) {
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_OFFSETS
            StpF2 pDEBA = p + StpF2(-kHalfRcpI.x, -kHalfRcpI.y);
            StpMF4 gDEBA = StpGeaa4F(pDEBA);
            StpMF4 gEFCB = StpGeaa4OF(pDEBA, StpI2(1, 0));
            StpMF4 gGHED = StpGeaa4OF(pDEBA, StpI2(0, 1));
            StpMF4 gHIFE = StpGeaa4OF(pDEBA, StpI2(1, 1));
        #else // STP_OFFSETS
            StpMF4 gDEBA = StpGeaa4F(p + StpF2(-kHalfRcpI.x, -kHalfRcpI.y));
            StpMF4 gEFCB = StpGeaa4F(p + StpF2( kHalfRcpI.x, -kHalfRcpI.y));
            StpMF4 gGHED = StpGeaa4F(p + StpF2(-kHalfRcpI.x,  kHalfRcpI.y));
            StpMF4 gHIFE = StpGeaa4F(p + StpF2( kHalfRcpI.x,  kHalfRcpI.y));
        #endif // STP_OFFSETS
//------------------------------------------------------------------------------------------------------------------------------
        StpMF2 gHV0,gHV1,gHV2;
        gHV0.x = gDEBA.z * StpMF1_(-2.0) + gEFCB.z;
        gHV0.y = gDEBA.x * StpMF1_(-2.0) + gGHED.x;
        gHV0 += StpMF2_(gDEBA.w);
        gHV1.x = gDEBA.x + gEFCB.y;
        gHV1.y = gDEBA.z + gGHED.y;
        gHV1 += StpMF2_(gDEBA.y) * StpMF2_(-2.0);
        gHV2.x = gGHED.x + gGHED.y * StpMF1_(-2.0);
        gHV2.y = gEFCB.z + gEFCB.y * StpMF1_(-2.0);
        gHV2 += StpMF2_(gHIFE.y);
        #if 0
            StpMF2 gHV = abs(gHV0) + abs(gHV1) * StpMF2_(2.0) + abs(gHV2);
        #else
            StpMF2 gHV = gHV0 * gHV0 + gHV1 * gHV1 * StpMF2_(2.0) + gHV2 * gHV2;
        #endif
        StpP1 gVert = gHV.x > gHV.y;
//------------------------------------------------------------------------------------------------------------------------------
        StpMF2 gBH = gVert ? StpMF2(gDEBA.x, gEFCB.y) : StpMF2(gDEBA.z, gGHED.y);
        StpMF2 gAC = gVert ? StpMF2(gDEBA.w, gGHED.x) : StpMF2(gDEBA.w, gEFCB.z);
        StpMF2 gDF = gVert ? StpMF2(gDEBA.z, gGHED.y) : StpMF2(gDEBA.x, gEFCB.y);
        StpMF2 gGI = gVert ? StpMF2(gEFCB.y, gHIFE.y) : StpMF2(gGHED.x, gHIFE.y);
        StpMF2 gBHMinusE = gBH - StpMF2_(gDEBA.y);
        StpMF2 gEnd2 = abs(gBHMinusE);
        StpP1 gUp = gEnd2.x >= gEnd2.y;
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 gE = gDEBA.y;
        gBH = gUp ? gBH : gBH.yx;
//------------------------------------------------------------------------------------------------------------------------------
        StpMF2 gBi = gUp ? StpMF2(2.0 / 3.0, 1.0 / 3.0) : StpMF2(1.0 / 3.0 , 2.0 / 3.0);
        StpMF1 gBMinusE = gUp ? gBHMinusE.x : gBHMinusE.y;
        StpMF2 gBi0 = (gUp ? gAC : gGI) * StpMF2_(1.0 / 3.0) + gDF * StpMF2_(2.0 / 3.0);
        StpMF2 gLo0 = gDF;
        StpMF1 gAbsBMinusE = abs(gBMinusE);
        StpMF1 gNe = gAbsBMinusE;
        StpMF1 gGood = StpGtZeroMF1(gBMinusE);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 gWalk = gVert ? StpF2(0.0, kRcpI.y) : StpF2(kRcpI.x, 0.0);
        StpF2 gDecon = gVert ? StpF2(kRcpI.x, 0.0) : StpF2(0.0, kRcpI.y);
        if(gUp) gDecon = -gDecon;
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 gP = p + gDecon * StpF2_(1.0/3.0);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 gPN3 = gP - StpF2_(8.5) * gWalk;
        StpF2 gPN2 = gP - StpF2_(6.5) * gWalk;
        StpF2 gPN1 = gP - StpF2_(4.5) * gWalk;
        StpF2 gPN0 = gP - StpF2_(2.5) * gWalk;
        StpF2 gPP0 = gP + StpF2_(2.5) * gWalk;
        StpF2 gPP1 = gP + StpF2_(4.5) * gWalk;
        StpF2 gPP2 = gP + StpF2_(6.5) * gWalk;
        StpF2 gPP3 = gP + StpF2_(8.5) * gWalk;
//------------------------------------------------------------------------------------------------------------------------------
        StpMF4 gGN3, gGN2, gGN1, gGN0, gGP0, gGP1, gGP2, gGP3;
        gGN3 = StpGeaa4F(gPN3);
        gGN2 = StpGeaa4F(gPN2);
        gGN1 = StpGeaa4F(gPN1);
        gGN0 = StpGeaa4F(gPN0);
        gGP0 = StpGeaa4F(gPP0);
        gGP1 = StpGeaa4F(gPP1);
        gGP2 = StpGeaa4F(gPP2);
        gGP3 = StpGeaa4F(gPP3);
//------------------------------------------------------------------------------------------------------------------------------
        if(gVert) {
            gGN3 = gGN3.zyxw;
            gGN2 = gGN2.zyxw;
            gGN1 = gGN1.zyxw;
            gGN0 = gGN0.zyxw;
            gGP0 = gGP0.zyxw;
            gGP1 = gGP1.zyxw;
            gGP2 = gGP2.zyxw;
            gGP3 = gGP3.zyxw; }
//------------------------------------------------------------------------------------------------------------------------------
        StpMF2 gLo8 = StpMF2(gGN3.x, gGP3.y);
        StpMF2 gLo7 = StpMF2(gGN3.y, gGP3.x);
        StpMF2 gLo6 = StpMF2(gGN2.x, gGP2.y);
        StpMF2 gLo5 = StpMF2(gGN2.y, gGP2.x);
        StpMF2 gLo4 = StpMF2(gGN1.x, gGP1.y);
        StpMF2 gLo3 = StpMF2(gGN1.y, gGP1.x);
        StpMF2 gLo2 = StpMF2(gGN0.x, gGP0.y);
        StpMF2 gLo1 = StpMF2(gGN0.y, gGP0.x);
        if(!gUp) {
            gLo8 = StpMF2(gGN3.w, gGP3.z);
            gLo7 = StpMF2(gGN3.z, gGP3.w);
            gLo6 = StpMF2(gGN2.w, gGP2.z);
            gLo5 = StpMF2(gGN2.z, gGP2.w);
            gLo4 = StpMF2(gGN1.w, gGP1.z);
            gLo3 = StpMF2(gGN1.z, gGP1.w);
            gLo2 = StpMF2(gGN0.w, gGP0.z);
            gLo1 = StpMF2(gGN0.z, gGP0.w); }
//------------------------------------------------------------------------------------------------------------------------------
        StpMF2 gGN3Bi = gGN3.yx * StpMF2_(gBi.x) + gGN3.zw * StpMF2_(gBi.y);
        StpMF2 gGN2Bi = gGN2.yx * StpMF2_(gBi.x) + gGN2.zw * StpMF2_(gBi.y);
        StpMF2 gGN1Bi = gGN1.yx * StpMF2_(gBi.x) + gGN1.zw * StpMF2_(gBi.y);
        StpMF2 gGN0Bi = gGN0.yx * StpMF2_(gBi.x) + gGN0.zw * StpMF2_(gBi.y);
        StpMF2 gGP0Bi = gGP0.yx * StpMF2_(gBi.x) + gGP0.zw * StpMF2_(gBi.y);
        StpMF2 gGP1Bi = gGP1.yx * StpMF2_(gBi.x) + gGP1.zw * StpMF2_(gBi.y);
        StpMF2 gGP2Bi = gGP2.yx * StpMF2_(gBi.x) + gGP2.zw * StpMF2_(gBi.y);
        StpMF2 gGP3Bi = gGP3.yx * StpMF2_(gBi.x) + gGP3.zw * StpMF2_(gBi.y);
        StpMF2 gBi8 = StpMF2(gGN3Bi.y, gGP3Bi.x);
        StpMF2 gBi7 = StpMF2(gGN3Bi.x, gGP3Bi.y);
        StpMF2 gBi6 = StpMF2(gGN2Bi.y, gGP2Bi.x);
        StpMF2 gBi5 = StpMF2(gGN2Bi.x, gGP2Bi.y);
        StpMF2 gBi4 = StpMF2(gGN1Bi.y, gGP1Bi.x);
        StpMF2 gBi3 = StpMF2(gGN1Bi.x, gGP1Bi.y);
        StpMF2 gBi2 = StpMF2(gGN0Bi.y, gGP0Bi.x);
        StpMF2 gBi1 = StpMF2(gGN0Bi.x, gGP0Bi.y);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF2 gEndBase;
        gEndBase.y = gBMinusE * StpMF1_(1.0/3.0) + gE;
        gEndBase.x = gAbsBMinusE * StpMF1_(STP_GEAA_THRESHOLD);
        #if 0
            gEndBase.x = StpRcpMF1(max(StpMF1_(1.0 / 16384.0), gEndBase.x));
        #else
            gEndBase.x = StpPrxLoRcpMF1(gEndBase.x);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_GEAA_P > 2)
            StpMF2 gUseP8 = StpSatMF2(abs(gBi8 - StpMF2_(gEndBase.y)) * StpMF2_(gEndBase.x));
            StpMF2 gUseP7 = StpSatMF2(abs(gBi7 - StpMF2_(gEndBase.y)) * StpMF2_(gEndBase.x));
        #endif
        #if (STP_GEAA_P > 1)
            StpMF2 gUseP6 = StpSatMF2(abs(gBi6 - StpMF2_(gEndBase.y)) * StpMF2_(gEndBase.x));
            StpMF2 gUseP5 = StpSatMF2(abs(gBi5 - StpMF2_(gEndBase.y)) * StpMF2_(gEndBase.x));
        #endif
        #if (STP_GEAA_P > 0)
            StpMF2 gUseP4 = StpSatMF2(abs(gBi4 - StpMF2_(gEndBase.y)) * StpMF2_(gEndBase.x));
            StpMF2 gUseP3 = StpSatMF2(abs(gBi3 - StpMF2_(gEndBase.y)) * StpMF2_(gEndBase.x));
        #endif
            StpMF2 gUseP2 = StpSatMF2(abs(gBi2 - StpMF2_(gEndBase.y)) * StpMF2_(gEndBase.x));
            StpMF2 gUseP1 = StpSatMF2(abs(gBi1 - StpMF2_(gEndBase.y)) * StpMF2_(gEndBase.x));
            StpMF2 gUseP0 = StpSatMF2(abs(gBi0 - StpMF2_(gEndBase.y)) * StpMF2_(gEndBase.x));
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_GEAA_P == 3)
            StpMF2 gDst2 = StpMF2_(9.5);
        #endif
        #if (STP_GEAA_P == 2)
            StpMF2 gDst2 = StpMF2_(7.5);
        #endif
        #if (STP_GEAA_P == 1)
            StpMF2 gDst2 = StpMF2_(5.5);
        #endif
        #if (STP_GEAA_P == 0)
            StpMF2 gDst2 = StpMF2_(3.5);
        #endif
        #if (STP_GEAA_P > 2)
            gDst2 = gDst2 + (StpMF2_(8.5) - gDst2) * gUseP8;
            gDst2 = gDst2 + (StpMF2_(7.5) - gDst2) * gUseP7;
        #endif
        #if (STP_GEAA_P > 1)
            gDst2 = gDst2 + (StpMF2_(6.5) - gDst2) * gUseP6;
            gDst2 = gDst2 + (StpMF2_(5.5) - gDst2) * gUseP5;
        #endif
        #if (STP_GEAA_P > 0)
            gDst2 = gDst2 + (StpMF2_(4.5) - gDst2) * gUseP4;
            gDst2 = gDst2 + (StpMF2_(3.5) - gDst2) * gUseP3;
        #endif
            gDst2 = gDst2 + (StpMF2_(2.5) - gDst2) * gUseP2;
            gDst2 = gDst2 + (StpMF2_(1.5) - gDst2) * gUseP1;
            gDst2 = gDst2 + (StpMF2_(0.5) - gDst2) * gUseP0;
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 gLoSub = (gDst2.x + gDst2.y) * StpMF1_(0.5) - StpMF1_(STP_GEAA_SUBPIX);
        StpMF2 gLoW01 = StpMF2_(1.0) - StpSatMF2(StpMF2(1.0, 2.0) - StpMF2_(gLoSub));
        StpMF2 gLoW23 = StpMF2_(1.0) - StpSatMF2(StpMF2(3.0, 4.0) - StpMF2_(gLoSub));
        StpMF2 gLoW45 = StpMF2_(1.0) - StpSatMF2(StpMF2(5.0, 6.0) - StpMF2_(gLoSub));
        StpMF2 gLoW67 = StpMF2_(1.0) - StpSatMF2(StpMF2(7.0, 8.0) - StpMF2_(gLoSub));
        StpMF2 gLoW89 = StpMF2_(1.0) - StpSatMF2(StpMF2(9.0,10.0) - StpMF2_(gLoSub));
        StpMF2 gLoAcc2 =
            gLo0 * StpMF2_(gLoW01.x) +
            gLo1 * StpMF2_(gLoW01.y) +
            gLo2 * StpMF2_(gLoW23.x) +
            gLo3 * StpMF2_(gLoW23.y) +
            gLo4 * StpMF2_(gLoW45.x) +
            gLo5 * StpMF2_(gLoW45.y) +
            gLo6 * StpMF2_(gLoW67.x) +
            gLo7 * StpMF2_(gLoW67.y) +
            gLo8 * StpMF2_(gLoW89.x);
        StpMF1 gLoAcc = gE + gLoAcc2.x + gLoAcc2.y;
        StpMF2 gLoW2 = gLoW01 + gLoW23 + gLoW45 + gLoW67;
        gLoW2 *= StpMF2_(2.0);
        gLoAcc *= StpRcpMF1(StpMF1_(1.0) + gLoW89.x * StpMF1_(2.0) + gLoW2.x + gLoW2.y);
        StpMF1 gOff = StpSatMF1((gLoAcc - gE) * StpRcpMF1(gBH.x - gE));
        gOff = min(gOff, StpMF1_(0.5));
//------------------------------------------------------------------------------------------------------------------------------
        gDilate = p + gDecon;
        gFilter = p + gDecon * StpF2_(gOff);
        gLuma = lerp(gE, gBH.x, gOff);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 gAnti = lerp(gE, gBH.x, gOff);
        StpMF1 gT = StpSatMF1((StpMF1_(-2.0) * gAnti + gBH.x + gE) * StpRcpMF1(gE - gBH.y));
        StpMF1 gFix = gE * (gT - StpMF1_(1.0)) - gBH.y * gT;
        gFix = StpSatMF1((gFix + gAnti) * StpRcpMF1(gFix + gBH.x));
//------------------------------------------------------------------------------------------------------------------------------
        gW = gFix;
        gW = StpRcpMF1(gW + StpMF1_(0.5)) - StpMF1_(1.0);
        gW *= gW;
        gW = max(gW, StpMF1_(1.0/255.0)); }
#endif // defined(STP_GPU) && defined(STP_GEAA) && defined(STP_32BIT)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                               [GEAA] PACKED 16-BIT ENTRY POINT
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_GEAA) && defined(STP_16BIT)
    void StpGeaaH(
    out StpH1 gW,      // Output weight for pixel art scalar.
    out StpH1 gLuma,   // Filtered luma for debug.
    out StpF2 gFilter, // Location to sample for standalone unscaled spatial AA.
    out StpF2 gDilate, // Location of highest contrast neighbor.
    StpF2 p,           // {0 to 1} position across screen.
    StpF2 kRcpI,       // 1.0 / input image size in pixels.
    StpF2 kHalfRcpI) { // 0.5 / input image size in pixels.
//------------------------------------------------------------------------------------------------------------------------------
        // Sample 3x3 input pattern in luma (or green).
        //  A B C
        //  D E F
        //  G H I
        // Via four gather4s, usage for the next section to try to improve operand caching.
        #if STP_OFFSETS
            StpF2 pDEBA = p + StpF2(-kHalfRcpI.x, -kHalfRcpI.y);
            StpH4 gDEBA = StpGeaa4H(pDEBA);
            StpH4 gEFCB = StpGeaa4OH(pDEBA, StpI2(1, 0));
            StpH4 gGHED = StpGeaa4OH(pDEBA, StpI2(0, 1));
            StpH4 gHIFE = StpGeaa4OH(pDEBA, StpI2(1, 1));
        #else // STP_OFFSETS
            StpH4 gDEBA = StpGeaa4H(p + StpF2(-kHalfRcpI.x, -kHalfRcpI.y)); // .xyzw=DEBA
            StpH4 gEFCB = StpGeaa4H(p + StpF2( kHalfRcpI.x, -kHalfRcpI.y)); // .yz  =FC
            StpH4 gGHED = StpGeaa4H(p + StpF2(-kHalfRcpI.x,  kHalfRcpI.y)); // .xy  =GH
            StpH4 gHIFE = StpGeaa4H(p + StpF2( kHalfRcpI.x,  kHalfRcpI.y)); // .y   =I
        #endif // STP_OFFSETS
//------------------------------------------------------------------------------------------------------------------------------
        // Compute {horz,vert} change terms. Complex to decide on either horizontal or vertical direction.
        // Trouble case for some algorithms,
        //  0 1 0
        //  0 1 0
        //  0 1 0
        // This should present as a vertical search direction.
        // Simple stuff like sum of each 2x2 produces,
        //  2 2
        //  2 2
        // Which has no direction.
        // {ABC,ADG}
        StpH2 gHV0,gHV1,gHV2;
        gHV0.x = gDEBA.z * StpH1_(-2.0) + gEFCB.z;
        gHV0.y = gDEBA.x * StpH1_(-2.0) + gGHED.x;
        gHV0 += StpH2_(gDEBA.w);
        // {DEF,BEH}
        gHV1.x = gDEBA.x + gEFCB.y;
        gHV1.y = gDEBA.z + gGHED.y;
        gHV1 += StpH2_(gDEBA.y) * StpH2_(-2.0);
        // {GHI,CFI}
        gHV2.x = gGHED.x + gGHED.y * StpH1_(-2.0);
        gHV2.y = gEFCB.z + gEFCB.y * StpH1_(-2.0);
        gHV2 += StpH2_(gHIFE.y);
        // Combine terms.
        #if 0
            // What FXAA does, better for a diagonal computation (which is not needed), left for reference.
            StpH2 gHV = abs(gHV0) + abs(gHV1) * StpH2_(2.0) + abs(gHV2);
        #else
            // Slightly faster for packed 16-bit (which has no free ABS on AMD).
            StpH2 gHV = gHV0 * gHV0 + gHV1 * gHV1 * StpH2_(2.0) + gHV2 * gHV2;
        #endif
        // Choose search direction, the 'gVert' is true:=vert, false:=horz.
        // Go vertical search if horizontal has higher contrast (search perpendicular).
        StpP1 gVert = gHV.x > gHV.y;
//------------------------------------------------------------------------------------------------------------------------------
        // This is BH if search horzontal, else DF (as BH) if search vertical.
        StpH2 gBH = gVert ? StpH2(gDEBA.x, gEFCB.y) : StpH2(gDEBA.z, gGHED.y);
        // Will need these later, will let the compiler move around the transpose.
        StpH2 gAC = gVert ? StpH2(gDEBA.w, gGHED.x) : StpH2(gDEBA.w, gEFCB.z);
        StpH2 gDF = gVert ? StpH2(gDEBA.z, gGHED.y) : StpH2(gDEBA.x, gEFCB.y);
        StpH2 gGI = gVert ? StpH2(gEFCB.y, gHIFE.y) : StpH2(gGHED.x, gHIFE.y);
        // Start to compute threshold for end of span, compute a gradient pair.
        StpH2 gBHMinusE = gBH - StpH2_(gDEBA.y);
        StpH2 gEnd2 = abs(gBHMinusE);
        // If gradient is larger upward (or leftward if vert).
        StpP1 gUp = gEnd2.x >= gEnd2.y;
//------------------------------------------------------------------------------------------------------------------------------
        // Rename.
        StpH1 gE = gDEBA.y;
        // Swap if not up. From this point on, the B is the high-contrast neighbor, and the H is the other one in same dir.
        gBH = gUp ? gBH : gBH.yx;
//------------------------------------------------------------------------------------------------------------------------------
        // Choose the bilinear scalar (gets to 1/3 between texels during the search).
        //  .x ... For texel closer to pixel axis when up (reversed when down).
        //  .y ... For more distant texel.
        // LOGIC
        // =====
        // This keeps threshold of 2 of the 3 end conditions the same (so 1/3 shift is better than 1/4).
        // =====
        //  e         e    e   <- e = end cases
        //  0    0    1    1   <- 1/3 of high contrast neighbor
        //  0    1    0    1   <- 2/3 of self
        // ------------------
        //  0   2/3  1/3   1   <- blended value (2/3 is the target)
        // 2/3   0   1/3  1/3  <- abs(difference to target)
        StpH2 gBi = gUp ? StpH2(2.0 / 3.0, 1.0 / 3.0) : StpH2(1.0 / 3.0 , 2.0 / 3.0);
        // Choose either {B-E, or H-E}.
        StpH1 gBMinusE = gUp ? gBHMinusE.x : gBHMinusE.y;
        // Finish Bi0, this is the first 2 texture fetches (done using math instead) at P0 (1 texel away from center).
        StpH2 gBi0 = (gUp ? gAC : gGI) * StpH2_(1.0 / 3.0) + gDF * StpH2_(2.0 / 3.0);
        // Finish Lo0, for the directional blur.
        StpH2 gLo0 = gDF;
        // Store out spatial neighborhood.
        StpH1 gAbsBMinusE = abs(gBMinusE);
        // This is just the highest contrast neighbor along the choosen direction, may report less contrast then actual.
        StpH1 gNe = gAbsBMinusE;
        // Good direction to compare against at the end.
        // Good means 'don't flip' to the other side.
        // Have 'B-E' want 'signed(E-(B/2+E/2))' = 'signed(E/2-B/2)' = 'signed(E-B)' = 'gtzero(B-E)'
        StpH1 gGood = StpGtZeroH1(gBMinusE);
//------------------------------------------------------------------------------------------------------------------------------
        // One pixel walk distance for search.
        StpF2 gWalk = gVert ? StpF2(0.0, kRcpI.y) : StpF2(kRcpI.x, 0.0);
        // This is the direction of decontrast (towards the highest contrast neighbor).
        StpF2 gDecon = gVert ? StpF2(kRcpI.x, 0.0) : StpF2(0.0, kRcpI.y);
        // If up (or left) work negative.
        if(gUp) gDecon = -gDecon;
//------------------------------------------------------------------------------------------------------------------------------
        // Have enough now to build out sampling positions.
        // This works in gather4 to get two samples per gather, then uses math to finish the bilinear fetch.
        // In case the logic ever goes back to a non-gather4 version, this keeps with the 1/3 offset.
        // Build base, 1/3 to neighbor pixel.
        // It must be 1/3 to neighbor pixel to be able to find the end of thin stuff like this.
        //  . . . . . . . . . . .
        //  . . . . . . x x x x x
        //  . x x x x x . . . . .
        //      |       |
        //      |------>|
        //              |                             .     x
        //            If it was 1/2 to neighbor, then x and . would look the same.
        StpF2 gP = p + gDecon * StpF2_(1.0/3.0);
        // The gather4 positions are (assuming horizontal then up).
        //  3 3 2 2 1 1 0 0 A B C 0 0 1 1 2 2 3 3
        //  3 3 2 2 1 1 0 0 D E F 0 0 1 1 2 2 3 3
        //                  G H I
//------------------------------------------------------------------------------------------------------------------------------
        // Sampling positions.
        // Currently walking without gaps, but could skip along too!
        StpF2 gPN3 = gP - StpF2_(8.5) * gWalk;
        StpF2 gPN2 = gP - StpF2_(6.5) * gWalk;
        StpF2 gPN1 = gP - StpF2_(4.5) * gWalk;
        StpF2 gPN0 = gP - StpF2_(2.5) * gWalk;
        StpF2 gPP0 = gP + StpF2_(2.5) * gWalk;
        StpF2 gPP1 = gP + StpF2_(4.5) * gWalk;
        StpF2 gPP2 = gP + StpF2_(6.5) * gWalk;
        StpF2 gPP3 = gP + StpF2_(8.5) * gWalk;
//------------------------------------------------------------------------------------------------------------------------------
        // This attempts to do sampling in a cache friendly way.
        // Cannot sample with offsets, because it could be vertical or horizontal and offsets need to be static in DX.
        // Sampling pairs {negative, positive} directions.
        StpH4 gGN3, gGN2, gGN1, gGN0, gGP0, gGP1, gGP2, gGP3;
        gGN3 = StpGeaa4H(gPN3);
        gGN2 = StpGeaa4H(gPN2);
        gGN1 = StpGeaa4H(gPN1);
        gGN0 = StpGeaa4H(gPN0);
        gGP0 = StpGeaa4H(gPP0);
        gGP1 = StpGeaa4H(gPP1);
        gGP2 = StpGeaa4H(gPP2);
        gGP3 = StpGeaa4H(gPP3);
//------------------------------------------------------------------------------------------------------------------------------
        // Finish the bilinear fetch.
        // For 'vertical' this needs to do a transpose.
        // The FMAs are duplicated, else the compiler would need to do that anyway.
        //                             1st 2nd for N side (P side is reversed)
        //  -----------                  | |
        //  W Z     w z  !vert &  up ... Y X, Z W
        //  X Y [p] x y
        //  -----------
        //  W Z [p] w z  !vert & !up ... Z W, Y X
        //  X Y     x y
        //  -----------
        //  W Z           vert &  up ... Y Z, X W
        //  X Y
        //   [p]
        //  w z
        //  x y
        //  -----------
        //    W Z         vert & !up ... X W, Y Z
        //    X Y                        | |  | |
        //   [p]                         | |  0.33 term
        //    w z                        | |
        //    x y                        0.66 term
        //  -----------
        if(gVert) {
            gGN3 = gGN3.zyxw;
            gGN2 = gGN2.zyxw;
            gGN1 = gGN1.zyxw;
            gGN0 = gGN0.zyxw;
            gGP0 = gGP0.zyxw;
            gGP1 = gGP1.zyxw;
            gGP2 = gGP2.zyxw;
            gGP3 = gGP3.zyxw; }
//------------------------------------------------------------------------------------------------------------------------------
        // Grab the texels for the variable length inline low-pass box blur.
        StpH2 gLo8 = StpH2(gGN3.x, gGP3.y);
        StpH2 gLo7 = StpH2(gGN3.y, gGP3.x);
        StpH2 gLo6 = StpH2(gGN2.x, gGP2.y);
        StpH2 gLo5 = StpH2(gGN2.y, gGP2.x);
        StpH2 gLo4 = StpH2(gGN1.x, gGP1.y);
        StpH2 gLo3 = StpH2(gGN1.y, gGP1.x);
        StpH2 gLo2 = StpH2(gGN0.x, gGP0.y);
        StpH2 gLo1 = StpH2(gGN0.y, gGP0.x);
        if(!gUp) {
            gLo8 = StpH2(gGN3.w, gGP3.z);
            gLo7 = StpH2(gGN3.z, gGP3.w);
            gLo6 = StpH2(gGN2.w, gGP2.z);
            gLo5 = StpH2(gGN2.z, gGP2.w);
            gLo4 = StpH2(gGN1.w, gGP1.z);
            gLo3 = StpH2(gGN1.z, gGP1.w);
            gLo2 = StpH2(gGN0.w, gGP0.z);
            gLo1 = StpH2(gGN0.z, gGP0.w); }
//------------------------------------------------------------------------------------------------------------------------------
        // Simulate the bilinear fetch.
        StpH2 gGN3Bi = gGN3.yx * StpH2_(gBi.x) + gGN3.zw * StpH2_(gBi.y);
        StpH2 gGN2Bi = gGN2.yx * StpH2_(gBi.x) + gGN2.zw * StpH2_(gBi.y);
        StpH2 gGN1Bi = gGN1.yx * StpH2_(gBi.x) + gGN1.zw * StpH2_(gBi.y);
        StpH2 gGN0Bi = gGN0.yx * StpH2_(gBi.x) + gGN0.zw * StpH2_(gBi.y);
        StpH2 gGP0Bi = gGP0.yx * StpH2_(gBi.x) + gGP0.zw * StpH2_(gBi.y);
        StpH2 gGP1Bi = gGP1.yx * StpH2_(gBi.x) + gGP1.zw * StpH2_(gBi.y);
        StpH2 gGP2Bi = gGP2.yx * StpH2_(gBi.x) + gGP2.zw * StpH2_(gBi.y);
        StpH2 gGP3Bi = gGP3.yx * StpH2_(gBi.x) + gGP3.zw * StpH2_(gBi.y);
        // Note positive side the {x,y} order is reversed.
        StpH2 gBi8 = StpH2(gGN3Bi.y, gGP3Bi.x);
        StpH2 gBi7 = StpH2(gGN3Bi.x, gGP3Bi.y);
        StpH2 gBi6 = StpH2(gGN2Bi.y, gGP2Bi.x);
        StpH2 gBi5 = StpH2(gGN2Bi.x, gGP2Bi.y);
        StpH2 gBi4 = StpH2(gGN1Bi.y, gGP1Bi.x);
        StpH2 gBi3 = StpH2(gGN1Bi.x, gGP1Bi.y);
        StpH2 gBi2 = StpH2(gGN0Bi.y, gGP0Bi.x);
        StpH2 gBi1 = StpH2(gGN0Bi.x, gGP0Bi.y);
//------------------------------------------------------------------------------------------------------------------------------
        // Threshold for end of span (X), and base to compare against (Y).
        StpH2 gEndBase;
        // For a (1.0/3.0) pixel shift.
        // The 'gBMinusE = other - self', and want 'self * (2.0/3.0) + other * (1.0/3.0)'.
        gEndBase.y = gBMinusE * StpH1_(1.0/3.0) + gE;
        gEndBase.x = gAbsBMinusE * StpH1_(STP_GEAA_THRESHOLD);
        // Safer version here for reference.
        #if 0
            gEndBase.x = StpRcpH1(max(StpH1_(1.0 / 16384.0), gEndBase.x));
        #else
            gEndBase.x = StpPrxLoRcpH1(gEndBase.x);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Compute opacity term, {0 := not done, 1 := end of span}.
        #if (STP_GEAA_P > 2)
            StpH2 gUseP8 = StpSatH2(abs(gBi8 - StpH2_(gEndBase.y)) * StpH2_(gEndBase.x));
            StpH2 gUseP7 = StpSatH2(abs(gBi7 - StpH2_(gEndBase.y)) * StpH2_(gEndBase.x));
        #endif
        #if (STP_GEAA_P > 1)
            StpH2 gUseP6 = StpSatH2(abs(gBi6 - StpH2_(gEndBase.y)) * StpH2_(gEndBase.x));
            StpH2 gUseP5 = StpSatH2(abs(gBi5 - StpH2_(gEndBase.y)) * StpH2_(gEndBase.x));
        #endif
        #if (STP_GEAA_P > 0)
            StpH2 gUseP4 = StpSatH2(abs(gBi4 - StpH2_(gEndBase.y)) * StpH2_(gEndBase.x));
            StpH2 gUseP3 = StpSatH2(abs(gBi3 - StpH2_(gEndBase.y)) * StpH2_(gEndBase.x));
        #endif
            StpH2 gUseP2 = StpSatH2(abs(gBi2 - StpH2_(gEndBase.y)) * StpH2_(gEndBase.x));
            StpH2 gUseP1 = StpSatH2(abs(gBi1 - StpH2_(gEndBase.y)) * StpH2_(gEndBase.x));
            StpH2 gUseP0 = StpSatH2(abs(gBi0 - StpH2_(gEndBase.y)) * StpH2_(gEndBase.x));
//------------------------------------------------------------------------------------------------------------------------------
        // Work this like painters alpha blending.
        // This analog path is faster and cleaner than binary logic.
        // Distance traveled for {negative, positive} paths.
        // LOGIC
        // =====
        // Note distance factors already have the 0.5 factored in.
        //  N := negative search end (1 pixel away, but edge is 0.5 pixel away)
        //  P := positive search end (4 pixel away, but edge is 3.5 pixel away)
        //  X := the pixel to filter
        //               :<->:<------------->:
        //               :   :               :
        //               :   :             +---+---+---+---+
        //               :   :             | : |   |   |   |
        //               N +---+---+---+---+-P-+---+---+---+
        //                 | X |   |   |   |   |   |   |   |
        // +---+---+---+---+---+---+---+---+---+---+---+---+
        // |   |   |   |   |   |   |   |   |   |   |   |   |
        // +---+---+---+---+---+---+---+---+---+---+---+---+
        #if (STP_GEAA_P == 3)
            StpH2 gDst2 = StpH2_(9.5);
        #endif
        #if (STP_GEAA_P == 2)
            StpH2 gDst2 = StpH2_(7.5);
        #endif
        #if (STP_GEAA_P == 1)
            StpH2 gDst2 = StpH2_(5.5);
        #endif
        #if (STP_GEAA_P == 0)
            StpH2 gDst2 = StpH2_(3.5);
        #endif
        #if (STP_GEAA_P > 2)
            gDst2 = gDst2 + (StpH2_(8.5) - gDst2) * gUseP8;
            gDst2 = gDst2 + (StpH2_(7.5) - gDst2) * gUseP7;
        #endif
        #if (STP_GEAA_P > 1)
            gDst2 = gDst2 + (StpH2_(6.5) - gDst2) * gUseP6;
            gDst2 = gDst2 + (StpH2_(5.5) - gDst2) * gUseP5;
        #endif
        #if (STP_GEAA_P > 0)
            gDst2 = gDst2 + (StpH2_(4.5) - gDst2) * gUseP4;
            gDst2 = gDst2 + (StpH2_(3.5) - gDst2) * gUseP3;
        #endif
            gDst2 = gDst2 + (StpH2_(2.5) - gDst2) * gUseP2;
            gDst2 = gDst2 + (StpH2_(1.5) - gDst2) * gUseP1;
            gDst2 = gDst2 + (StpH2_(0.5) - gDst2) * gUseP0;
//------------------------------------------------------------------------------------------------------------------------------
        // Run the variable length low-pass box blur.
        // Need half distance with half pixel removed.
        StpH1 gLoSub = (gDst2.x + gDst2.y) * StpH1_(0.5) - StpH1_(STP_GEAA_SUBPIX);
        // compute the weights (if should be included or not).
        StpH2 gLoW01 = StpH2_(1.0) - StpSatH2(StpH2(1.0, 2.0) - StpH2_(gLoSub));
        StpH2 gLoW23 = StpH2_(1.0) - StpSatH2(StpH2(3.0, 4.0) - StpH2_(gLoSub));
        StpH2 gLoW45 = StpH2_(1.0) - StpSatH2(StpH2(5.0, 6.0) - StpH2_(gLoSub));
        StpH2 gLoW67 = StpH2_(1.0) - StpSatH2(StpH2(7.0, 8.0) - StpH2_(gLoSub));
        StpH2 gLoW89 = StpH2_(1.0) - StpSatH2(StpH2(9.0,10.0) - StpH2_(gLoSub));
        // Weighted accumulation of samples.
        StpH2 gLoAcc2 =
            gLo0 * StpH2_(gLoW01.x) +
            gLo1 * StpH2_(gLoW01.y) +
            gLo2 * StpH2_(gLoW23.x) +
            gLo3 * StpH2_(gLoW23.y) +
            gLo4 * StpH2_(gLoW45.x) +
            gLo5 * StpH2_(gLoW45.y) +
            gLo6 * StpH2_(gLoW67.x) +
            gLo7 * StpH2_(gLoW67.y) +
            gLo8 * StpH2_(gLoW89.x);
        StpH1 gLoAcc = gE + gLoAcc2.x + gLoAcc2.y;
        // Weight sum.
        StpH2 gLoW2 = gLoW01 + gLoW23 + gLoW45 + gLoW67;
        gLoW2 *= StpH2_(2.0);
        gLoAcc *= StpRcpH1(StpH1_(1.0) + gLoW89.x * StpH1_(2.0) + gLoW2.x + gLoW2.y);
        // Convert to blend between self and high-contrast neighbor.
        // This currently allows full {0.0 to 1.0} blend.
        StpH1 gOff = StpSatH1((gLoAcc - gE) * StpRcpH1(gBH.x - gE));
        // It is important to not exceed 0.5 weight for PIXart scaling.
        gOff = min(gOff, StpH1_(0.5));
//------------------------------------------------------------------------------------------------------------------------------
        // Save out dilation pixel for {z,motion}.
        gDilate = p + gDecon;
        // Save out filter position.
        gFilter = p + gDecon * StpF2_(gOff);
        gLuma = lerp(gE, gBH.x, gOff);
//------------------------------------------------------------------------------------------------------------------------------
        // GEAA up to this point creates weights that only help a scalar for aliased edges.
        // This attempts to increase weight to also restore some anti-aliased edges.
        // It does this by increasing weight as much as can be borrowed from the 'E to H' side.
        // An equation for movement towards H,
        //   E+(H-E)*T  ...  Where T must be {0 to 1} ranged, but want {0 to 0.5} ranged (same as 'gOff').
        // Equation for E motion with respect to the B side,
        //   A=E+(B-E)*F  ...  Where A is the anti-aliased output, and F would typically be 'gOff'.
        // Solving that for E,
        //   E=((A-F*B)/(1-F)
        // Combining equations,
        //   E+(H-E)*T = ((A-F*B)/(1-F)
        // Then solving for T when 'F=0.5' (maximum 'gOff' weight),
        //   T=(-2*A+B+E)/(E-H)
        // Then limit T inside {0 to 0.5}.
        // And use limited 'T' to recompute a new 'F' which becomes the 'gOff' fixed weight.
        StpH1 gAnti = lerp(gE, gBH.x, gOff);
        // Solve for the movement towards 'H'.
        // This in theory should be limited to {0 to 0.5}, but {0 to 1} seems to work too.
        StpH1 gT = StpSatH1((StpH1_(-2.0) * gAnti + gBH.x + gE) * StpRcpH1(gE - gBH.y));
        StpH1 gFix = gE * (gT - StpH1_(1.0)) - gBH.y * gT;
        gFix = StpSatH1((gFix + gAnti) * StpRcpH1(gFix + gBH.x));
//------------------------------------------------------------------------------------------------------------------------------
        // Output weight for pixel art scalar.
        // The 'gOff'set goes between {0 := no change, to 0.5 := half to neighbor}.
        // The half to neighbor position would be where the edge crosses between two pixels.
        // The sample size needs to be {0 := at the crossing, to 1 := no change}.
        // Can solve this, the 1D kernel will look like,
        //  u = (1-x)*s ... weighting terms
        //  v =    x *t
        //  w = 1/(u+v)
        //  o = a*u*w + b*v*w
        // The split is where weights are the same,
        //  u*w == v*w ... ((1-x)*s)/(((1-x)*s)+(x*t)) == (x*t)/(((1-x)*s)+(x*t))
        // Can assume s=1.0 (the other sample), thus this reduces to,
        //  u*w == v*w ... (1-x)/((1-x)+(x*t)) == (x*t)/((1-x)+(x*t))
        // Then solve for 't' given crossing point 'x'.
        //  t=1/x-1
        // Convert to 'x=gOffset+1/2'.
        // Solve for 't=1/x-1', or 't=1/(gOffset+1/2)-1'.
        gW = gFix;
        gW = StpRcpH1(gW + StpH1_(0.5)) - StpH1_(1.0);
        // Send squared (as needed by scalar).
        gW *= gW;
        // Make sure not zero.
        gW = max(gW, StpH1_(1.0/255.0)); }
#endif // defined(STP_GPU) && defined(STP_GEAA) && defined(STP_16BIT)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#endif // STP_UNITY_INCLUDE_GUARD
