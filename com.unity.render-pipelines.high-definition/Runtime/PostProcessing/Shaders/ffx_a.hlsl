//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//
//                                              [A] SHADER PORTABILITY 1.20190528
//
//==============================================================================================================================
// LICENSE
// =======
// Copyright (c) 2017-2019 Advanced Micro Devices, Inc. All rights reserved.
// -------
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// -------
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
// -------
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//------------------------------------------------------------------------------------------------------------------------------
// ABOUT
// =====
// For questions and comments, feel free to contact the author directly: timothy.lottes@amd.com
// Common central point for high-level shading language portability for various shader headers.
//------------------------------------------------------------------------------------------------------------------------------
// DEFINES
// =======
// A_CPU ..... Include the CPU related code.
// A_GPU ..... Include the GPU related code.
// A_GLSL .... Using GLSL.
// A_HLSL .... Using HLSL.
// A_GCC ..... Using a GCC compatible compiler (else assume MSVC compatible compiler by default).
// =======
// A_BYTE .... Support 8-bit integer.
// A_HALF .... Support 16-bit integer and floating point.
// A_LONG .... Support 64-bit integer.
// A_DUBL .... Support 64-bit floating point.
// =======
// A_WAVE .... Support wave-wide operations.
//------------------------------------------------------------------------------------------------------------------------------
// To get #include "a.h" working in GLSL use '#extension GL_GOOGLE_include_directive:require'.
//------------------------------------------------------------------------------------------------------------------------------
// SIMPLIFIED TYPE SYSTEM
// ======================
//  - All ints will be unsigned with exception of when signed is required.
//  - Type naming simplified and shortened "A<type><#components>",
//     - H = 16-bit float (half)
//     - F = 32-bit float (float)
//     - D = 64-bit float (double)
//     - P = 1-bit integer (predicate, not using bool because 'B' is used for byte)
//     - B = 8-bit integer (byte)
//     - W = 16-bit integer (word)
//     - U = 32-bit integer (unsigned)
//     - L = 64-bit integer (long)
//  - Using "AS<type><#components>" for signed when required.
//------------------------------------------------------------------------------------------------------------------------------
// TODO
// ====
//  - Make sure 'ALerp*(a,b,m)' does 'b*m+(-a*m+a)' (2 ops).
//  - Add subgroup ops.
//------------------------------------------------------------------------------------------------------------------------------
// CHANGE LOG
// ==========
// 20190528 - Fix AU1_AH2_x() on HLSL (had incorrectly swapped x and y), fixed asuint() cases.
// 20190527 - Added min3/max3 for low precision for HLSL.
// 20190526 - Updated with half approximations, added ARsq*(), and ASat*() for CPU.
// 20190519 - Added more approximations.
// 20190514 - Added long conversions.
// 20190513 - Added the real BFI moved the other one to ABfiM().
// 20190507 - Added extra remap useful for 2D reductions.
// 20190507 - Started adding wave ops, add parabolic sin/cos.
// 20190505 - Added ASigned*() and friends, setup more auto-typecast, GLSL extensions, etc.
// 20190504 - Added min3/max3 for 32-bit integers.
// 20190503 - Added type reinterpretation for half.
// 20190416 - Added min3/max3 for half.
// 20190405 - Misc bug fixing.
// 20190404 - Cleaned up color conversion code. Switched "splat" to shorter naming "type_". Misc bug fixing.
//==============================================================================================================================
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                           COMMON
//==============================================================================================================================
#define A_2PI 6.28318530718
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//
//                                                             CPU
//
//==============================================================================================================================
// Requires standard C types: stdint.h
//==============================================================================================================================
#ifdef A_CPU
 #ifndef A_RESTRICT
  #define A_RESTRICT __restrict
 #endif
//------------------------------------------------------------------------------------------------------------------------------
 // Same types across CPU and GPU.
 typedef float AF1;
 typedef double AD1;
 typedef uint8_t AB1;
 typedef uint16_t AW1;
 typedef uint32_t AU1;
 typedef uint64_t AL1;
 typedef int8_t ASB1;
 typedef int16_t ASW1;
 typedef int32_t ASU1;
 typedef int64_t ASL1;
//------------------------------------------------------------------------------------------------------------------------------
 // Predicate uses 32-bit integer (C friendly bool).
 typedef uint32_t AP1;
//------------------------------------------------------------------------------------------------------------------------------
 static AU1 AU1_AF1(AF1 x){union{AF1 f;AU1 u;}bits;bits.f=x;return bits.u;}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                            MATHS
//------------------------------------------------------------------------------------------------------------------------------
// Has some dependency on external built-in function support.
//==============================================================================================================================
 #ifdef A_GCC
  static AF1 AFloorF1(AF1 x){return __builtin_floorf(x);}
 #else
  static AF1 AFloorF1(AF1 x){return floorf(x);}
 #endif
//------------------------------------------------------------------------------------------------------------------------------
 static AF1 AFractF1(AF1 x){return x-AFloorF1(x);}
//------------------------------------------------------------------------------------------------------------------------------
 static AF1 AMaxF1(AF1 a,AF1 b){return a>b?a:b;}
 static AD1 AMaxD1(AD1 a,AD1 b){return a>b?a:b;}
 static AB1 AMaxB1(AB1 a,AB1 b){return a>b?a:b;}
 static AW1 AMaxW1(AW1 a,AW1 b){return a>b?a:b;}
 static AU1 AMaxU1(AU1 a,AU1 b){return a>b?a:b;}
 static AL1 AMaxL1(AL1 a,AL1 b){return a>b?a:b;}
 // These follow the convention that A integer types don't have sign, until they are operated on.
 static AB1 AMaxSB1(AB1 a,AB1 b){return ((ASB1)a)>((ASB1)b)?a:b;}
 static AW1 AMaxSW1(AW1 a,AW1 b){return ((ASW1)a)>((ASW1)b)?a:b;}
 static AU1 AMaxSU1(AU1 a,AU1 b){return ((ASU1)a)>((ASU1)b)?a:b;}
 static AL1 AMaxSL1(AL1 a,AL1 b){return ((ASL1)a)>((ASL1)b)?a:b;}
//------------------------------------------------------------------------------------------------------------------------------
 static AF1 AMinF1(AF1 a,AF1 b){return a<b?a:b;}
 static AD1 AMinD1(AD1 a,AD1 b){return a<b?a:b;}
 static AB1 AMinB1(AB1 a,AB1 b){return a<b?a:b;}
 static AW1 AMinW1(AW1 a,AW1 b){return a<b?a:b;}
 static AU1 AMinU1(AU1 a,AU1 b){return a<b?a:b;}
 static AL1 AMinL1(AL1 a,AL1 b){return a<b?a:b;}
 static AB1 AMinSB1(AB1 a,AB1 b){return ((ASB1)a)<((ASB1)b)?a:b;}
 static AW1 AMinSW1(AW1 a,AW1 b){return ((ASW1)a)<((ASW1)b)?a:b;}
 static AU1 AMinSU1(AU1 a,AU1 b){return ((ASU1)a)<((ASU1)b)?a:b;}
 static AL1 AMinSL1(AL1 a,AL1 b){return ((ASL1)a)<((ASL1)b)?a:b;}
//------------------------------------------------------------------------------------------------------------------------------
 static AF1 ASatF1(AF1 a){return AMinF1(1.0f,AMaxF1(0.0f,a));}
 static AD1 ASatD1(AD1 a){return AMinD1(1.0,AMaxD1(0.0,a));}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                         HALF FLOAT
//==============================================================================================================================
 // Convert float to half (in lower 16-bits of output).
 // Same fast technique as documented here: ftp://ftp.fox-toolkit.org/pub/fasthalffloatconversion.pdf
 // Supports denormals.
 // Conversion rules are to make computations possibly "safer" on the GPU,
 //  -INF & -NaN -> -65504
 //  +INF & +NaN -> +65504
 static AU1 AU1_AH1_AF1(AF1 f){
  static AW1 base[512]={
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
   0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff};
  static AB1 shift[512]={
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
   0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18};
  union{AF1 f;AU1 u;}bits;bits.f=f;AU1 u=bits.u;AU1 i=u>>23;return (AU1)(base[i])+((u&0x7fffff)>>shift[i]);}
//------------------------------------------------------------------------------------------------------------------------------
 // Used to output packed constant.
 static AU1 AU1_AH2_AF2(AF1 lo, AF1 hi){return AU1_AH1_AF1(lo)+(AU1_AH1_AF1(hi)<<16);}
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//
//                                                            GLSL
//
//==============================================================================================================================
#if defined(A_GLSL) && defined(A_GPU)
 // Unity preprocessor complain about #extension
 #ifndef A_SKIP_EXT
  #ifdef A_HALF
   //#extension GL_EXT_shader_16bit_storage:require
   //#extension GL_EXT_shader_explicit_arithmetic_types:require
  #endif
//------------------------------------------------------------------------------------------------------------------------------
  #ifdef A_LONG
   //#extension GL_ARB_gpu_shader_int64:require
   // TODO: Fixme to more portable extension!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
   //#extension GL_NV_shader_atomic_int64:require
  #endif
//------------------------------------------------------------------------------------------------------------------------------
  #ifdef A_WAVE
   //#extension GL_KHR_shader_subgroup_arithmetic:require
   //#extension GL_KHR_shader_subgroup_ballot:require
   //#extension GL_KHR_shader_subgroup_quad:require
   //#extension GL_KHR_shader_subgroup_shuffle:require
  #endif
 #endif
//==============================================================================================================================
 #define AP1 bool
 #define AP2 bvec2
 #define AP3 bvec3
 #define AP4 bvec4
//------------------------------------------------------------------------------------------------------------------------------
 #define AF1 float
 #define AF2 vec2
 #define AF3 vec3
 #define AF4 vec4
//------------------------------------------------------------------------------------------------------------------------------
 #define AU1 uint
 #define AU2 uvec2
 #define AU3 uvec3
 #define AU4 uvec4
//------------------------------------------------------------------------------------------------------------------------------
 #define ASU1 int
 #define ASU2 ivec2
 #define ASU3 ivec3
 #define ASU4 ivec4
//==============================================================================================================================
 #define AF1_AU1(x) uintBitsToFloat(AU1(x))
 #define AF2_AU2(x) uintBitsToFloat(AU2(x))
 #define AF3_AU3(x) uintBitsToFloat(AU3(x))
 #define AF4_AU4(x) uintBitsToFloat(AU4(x))
//------------------------------------------------------------------------------------------------------------------------------
 #define AU1_AF1(x) floatBitsToUint(AF1(x))
 #define AU2_AF2(x) floatBitsToUint(AF2(x))
 #define AU3_AF3(x) floatBitsToUint(AF3(x))
 #define AU4_AF4(x) floatBitsToUint(AF4(x))
//------------------------------------------------------------------------------------------------------------------------------
 #define AU1_AH2_AF2 packHalf2x16
 #define AU1_AW2Unorm_AF2 packUnorm2x16
 #define AU1_AB4Unorm_AF4 packUnorm4x8
//------------------------------------------------------------------------------------------------------------------------------
 #define AF2_AH2_AU1 unpackHalf2x16
 #define AF2_AW2Unorm_AU1 unpackUnorm2x16
 #define AF4_AB4Unorm_AU1 unpackUnorm4x8
//==============================================================================================================================
 AF1 AF1_x(AF1 a){return AF1(a);}
 AF2 AF2_x(AF1 a){return AF2(a,a);}
 AF3 AF3_x(AF1 a){return AF3(a,a,a);}
 AF4 AF4_x(AF1 a){return AF4(a,a,a,a);}
 #define AF1_(a) AF1_x(AF1(a))
 #define AF2_(a) AF2_x(AF1(a))
 #define AF3_(a) AF3_x(AF1(a))
 #define AF4_(a) AF4_x(AF1(a))
//------------------------------------------------------------------------------------------------------------------------------
 AU1 AU1_x(AU1 a){return AU1(a);}
 AU2 AU2_x(AU1 a){return AU2(a,a);}
 AU3 AU3_x(AU1 a){return AU3(a,a,a);}
 AU4 AU4_x(AU1 a){return AU4(a,a,a,a);}
 #define AU1_(a) AU1_x(AU1(a))
 #define AU2_(a) AU2_x(AU1(a))
 #define AU3_(a) AU3_x(AU1(a))
 #define AU4_(a) AU4_x(AU1(a))
//==============================================================================================================================
 AU1 ABfe(AU1 src,AU1 off,AU1 bits){return bitfieldExtract(src,ASU1(off),ASU1(bits));}
 AU1 ABfi(AU1 src,AU1 ins,AU1 mask){return (ins&mask)|(src&(~mask));}
 // Proxy for V_BFI_B32 where the 'mask' is set as 'bits', 'mask=(1<<bits)-1', and 'bits' needs to be an immediate.
 AU1 ABfiM(AU1 src,AU1 ins,AU1 bits){return bitfieldInsert(src,ins,0,ASU1(bits));}
//------------------------------------------------------------------------------------------------------------------------------
 // V_FRACT_F32 (note DX frac() is different).
 AF1 AFractF1(AF1 x){return fract(x);}
 AF2 AFractF2(AF2 x){return fract(x);}
 AF3 AFractF3(AF3 x){return fract(x);}
 AF4 AFractF4(AF4 x){return fract(x);}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 ALerpF1(AF1 x,AF1 y,AF1 a){return mix(x,y,a);}
 AF2 ALerpF2(AF2 x,AF2 y,AF2 a){return mix(x,y,a);}
 AF3 ALerpF3(AF3 x,AF3 y,AF3 a){return mix(x,y,a);}
 AF4 ALerpF4(AF4 x,AF4 y,AF4 a){return mix(x,y,a);}
//------------------------------------------------------------------------------------------------------------------------------
 // V_MAX3_F32.
 AF1 AMax3F1(AF1 x,AF1 y,AF1 z){return max(x,max(y,z));}
 AF2 AMax3F2(AF2 x,AF2 y,AF2 z){return max(x,max(y,z));}
 AF3 AMax3F3(AF3 x,AF3 y,AF3 z){return max(x,max(y,z));}
 AF4 AMax3F4(AF4 x,AF4 y,AF4 z){return max(x,max(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
 AU1 AMax3SU1(AU1 x,AU1 y,AU1 z){return AU1(max(ASU1(x),max(ASU1(y),ASU1(z))));}
 AU2 AMax3SU2(AU2 x,AU2 y,AU2 z){return AU2(max(ASU2(x),max(ASU2(y),ASU2(z))));}
 AU3 AMax3SU3(AU3 x,AU3 y,AU3 z){return AU3(max(ASU3(x),max(ASU3(y),ASU3(z))));}
 AU4 AMax3SU4(AU4 x,AU4 y,AU4 z){return AU4(max(ASU4(x),max(ASU4(y),ASU4(z))));}
//------------------------------------------------------------------------------------------------------------------------------
 AU1 AMax3U1(AU1 x,AU1 y,AU1 z){return max(x,max(y,z));}
 AU2 AMax3U2(AU2 x,AU2 y,AU2 z){return max(x,max(y,z));}
 AU3 AMax3U3(AU3 x,AU3 y,AU3 z){return max(x,max(y,z));}
 AU4 AMax3U4(AU4 x,AU4 y,AU4 z){return max(x,max(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
 // Clamp has an easier pattern match for med3 when some ordering is known.
 // V_MED3_F32.
 AF1 AMed3F1(AF1 x,AF1 y,AF1 z){return max(min(x,y),min(max(x,y),z));}
 AF2 AMed3F2(AF2 x,AF2 y,AF2 z){return max(min(x,y),min(max(x,y),z));}
 AF3 AMed3F3(AF3 x,AF3 y,AF3 z){return max(min(x,y),min(max(x,y),z));}
 AF4 AMed3F4(AF4 x,AF4 y,AF4 z){return max(min(x,y),min(max(x,y),z));}
//------------------------------------------------------------------------------------------------------------------------------
 // V_MIN3_F32.
 AF1 AMin3F1(AF1 x,AF1 y,AF1 z){return min(x,min(y,z));}
 AF2 AMin3F2(AF2 x,AF2 y,AF2 z){return min(x,min(y,z));}
 AF3 AMin3F3(AF3 x,AF3 y,AF3 z){return min(x,min(y,z));}
 AF4 AMin3F4(AF4 x,AF4 y,AF4 z){return min(x,min(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
 AU1 AMin3SU1(AU1 x,AU1 y,AU1 z){return AU1(min(ASU1(x),min(ASU1(y),ASU1(z))));}
 AU2 AMin3SU2(AU2 x,AU2 y,AU2 z){return AU2(min(ASU2(x),min(ASU2(y),ASU2(z))));}
 AU3 AMin3SU3(AU3 x,AU3 y,AU3 z){return AU3(min(ASU3(x),min(ASU3(y),ASU3(z))));}
 AU4 AMin3SU4(AU4 x,AU4 y,AU4 z){return AU4(min(ASU4(x),min(ASU4(y),ASU4(z))));}
//------------------------------------------------------------------------------------------------------------------------------
 AU1 AMin3U1(AU1 x,AU1 y,AU1 z){return min(x,min(y,z));}
 AU2 AMin3U2(AU2 x,AU2 y,AU2 z){return min(x,min(y,z));}
 AU3 AMin3U3(AU3 x,AU3 y,AU3 z){return min(x,min(y,z));}
 AU4 AMin3U4(AU4 x,AU4 y,AU4 z){return min(x,min(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
 // Normalized trig. Valid input domain is {-256 to +256}. No GLSL compiler intrinsic exists to map to this currently.
 // V_COS_F32.
 AF1 ANCosF1(AF1 x){return cos(x*AF1_(A_2PI));}
 AF2 ANCosF2(AF2 x){return cos(x*AF2_(A_2PI));}
 AF3 ANCosF3(AF3 x){return cos(x*AF3_(A_2PI));}
 AF4 ANCosF4(AF4 x){return cos(x*AF4_(A_2PI));}
//------------------------------------------------------------------------------------------------------------------------------
 // Normalized trig. Valid input domain is {-256 to +256}. No GLSL compiler intrinsic exists to map to this currently.
 // V_SIN_F32.
 AF1 ANSinF1(AF1 x){return sin(x*AF1_(A_2PI));}
 AF2 ANSinF2(AF2 x){return sin(x*AF2_(A_2PI));}
 AF3 ANSinF3(AF3 x){return sin(x*AF3_(A_2PI));}
 AF4 ANSinF4(AF4 x){return sin(x*AF4_(A_2PI));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 ARcpF1(AF1 x){return AF1_(1.0)/x;}
 AF2 ARcpF2(AF2 x){return AF2_(1.0)/x;}
 AF3 ARcpF3(AF3 x){return AF3_(1.0)/x;}
 AF4 ARcpF4(AF4 x){return AF4_(1.0)/x;}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 ARsqF1(AF1 x){return AF1_(1.0)/sqrt(x);}
 AF2 ARsqF2(AF2 x){return AF2_(1.0)/sqrt(x);}
 AF3 ARsqF3(AF3 x){return AF3_(1.0)/sqrt(x);}
 AF4 ARsqF4(AF4 x){return AF4_(1.0)/sqrt(x);}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 ASatF1(AF1 x){return clamp(x,AF1_(0.0),AF1_(1.0));}
 AF2 ASatF2(AF2 x){return clamp(x,AF2_(0.0),AF2_(1.0));}
 AF3 ASatF3(AF3 x){return clamp(x,AF3_(0.0),AF3_(1.0));}
 AF4 ASatF4(AF4 x){return clamp(x,AF4_(0.0),AF4_(1.0));}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                          GLSL BYTE
//==============================================================================================================================
 #ifdef A_BYTE
  #define AB1 uint8_t
  #define AB2 u8vec2
  #define AB3 u8vec3
  #define AB4 u8vec4
//------------------------------------------------------------------------------------------------------------------------------
  #define ASB1 int8_t
  #define ASB2 i8vec2
  #define ASB3 i8vec3
  #define ASB4 i8vec4
//------------------------------------------------------------------------------------------------------------------------------
  AB1 AB1_x(AB1 a){return AB1(a);}
  AB2 AB2_x(AB1 a){return AB2(a,a);}
  AB3 AB3_x(AB1 a){return AB3(a,a,a);}
  AB4 AB4_x(AB1 a){return AB4(a,a,a,a);}
  #define AB1_(a) AB1_x(AB1(a))
  #define AB2_(a) AB2_x(AB1(a))
  #define AB3_(a) AB3_x(AB1(a))
  #define AB4_(a) AB4_x(AB1(a))
 #endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                          GLSL HALF
//==============================================================================================================================
 #ifdef A_HALF
  #define AH1 float16_t
  #define AH2 f16vec2
  #define AH3 f16vec3
  #define AH4 f16vec4
//------------------------------------------------------------------------------------------------------------------------------
  #define AW1 uint16_t
  #define AW2 u16vec2
  #define AW3 u16vec3
  #define AW4 u16vec4
//------------------------------------------------------------------------------------------------------------------------------
  #define ASW1 int16_t
  #define ASW2 i16vec2
  #define ASW3 i16vec3
  #define ASW4 i16vec4
//==============================================================================================================================
  #define AH2_AU1(x) unpackFloat2x16(AU1(x))
  AH4 AH4_AU2_x(AU2 x){return AH4(unpackFloat2x16(x.x),unpackFloat2x16(x.y));}
  #define AH4_AU2(x) AH4_AU2_x(AU2(x))
  #define AW2_AU1(x) unpackUint2x16(AU1(x))
  #define AW4_AU2(x) unpackUint4x16(pack64(AU2(x)))
//------------------------------------------------------------------------------------------------------------------------------
  #define AU1_AH2(x) packFloat2x16(AH2(x))
  AU2 AU2_AH4_x(AH4 x){return AU2(packFloat2x16(x.xy),packFloat2x16(x.zw));}
  #define AU2_AH4(x) AU2_AH4_x(AH4(x))
  #define AU1_AW2(x) packUint2x16(AW2(x))
  #define AU2_AW4(x) unpack32(packUint4x16(AW4(x)))
//==============================================================================================================================
  #define AW1_AH1(x) halfBitsToUint16(AH1(x))
  #define AW2_AH2(x) halfBitsToUint16(AH2(x))
  #define AW3_AH3(x) halfBitsToUint16(AH3(x))
  #define AW4_AH4(x) halfBitsToUint16(AH4(x))
//------------------------------------------------------------------------------------------------------------------------------
  #define AH1_AW1(x) uint16BitsToHalf(AW1(x))
  #define AH2_AW2(x) uint16BitsToHalf(AW2(x))
  #define AH3_AW3(x) uint16BitsToHalf(AW3(x))
  #define AH4_AW4(x) uint16BitsToHalf(AW4(x))
//==============================================================================================================================
  AH1 AH1_x(AH1 a){return AH1(a);}
  AH2 AH2_x(AH1 a){return AH2(a,a);}
  AH3 AH3_x(AH1 a){return AH3(a,a,a);}
  AH4 AH4_x(AH1 a){return AH4(a,a,a,a);}
  #define AH1_(a) AH1_x(AH1(a))
  #define AH2_(a) AH2_x(AH1(a))
  #define AH3_(a) AH3_x(AH1(a))
  #define AH4_(a) AH4_x(AH1(a))
//------------------------------------------------------------------------------------------------------------------------------
  AW1 AW1_x(AW1 a){return AW1(a);}
  AW2 AW2_x(AW1 a){return AW2(a,a);}
  AW3 AW3_x(AW1 a){return AW3(a,a,a);}
  AW4 AW4_x(AW1 a){return AW4(a,a,a,a);}
  #define AW1_(a) AW1_x(AW1(a))
  #define AW2_(a) AW2_x(AW1(a))
  #define AW3_(a) AW3_x(AW1(a))
  #define AW4_(a) AW4_x(AW1(a))
//==============================================================================================================================
  AH1 AFractH1(AH1 x){return fract(x);}
  AH2 AFractH2(AH2 x){return fract(x);}
  AH3 AFractH3(AH3 x){return fract(x);}
  AH4 AFractH4(AH4 x){return fract(x);}
//------------------------------------------------------------------------------------------------------------------------------
  // No packed version of max3.
  AH1 AMax3H1(AH1 x,AH1 y,AH1 z){return max(x,max(y,z));}
  AH2 AMax3H2(AH2 x,AH2 y,AH2 z){return max(x,max(y,z));}
  AH3 AMax3H3(AH3 x,AH3 y,AH3 z){return max(x,max(y,z));}
  AH4 AMax3H4(AH4 x,AH4 y,AH4 z){return max(x,max(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
  // No packed version of min3.
  AH1 AMin3H1(AH1 x,AH1 y,AH1 z){return min(x,min(y,z));}
  AH2 AMin3H2(AH2 x,AH2 y,AH2 z){return min(x,min(y,z));}
  AH3 AMin3H3(AH3 x,AH3 y,AH3 z){return min(x,min(y,z));}
  AH4 AMin3H4(AH4 x,AH4 y,AH4 z){return min(x,min(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
  AH1 ARcpH1(AH1 x){return AH1_(1.0)/x;}
  AH2 ARcpH2(AH2 x){return AH2_(1.0)/x;}
  AH3 ARcpH3(AH3 x){return AH3_(1.0)/x;}
  AH4 ARcpH4(AH4 x){return AH4_(1.0)/x;}
//------------------------------------------------------------------------------------------------------------------------------
  AH1 ARsqH1(AH1 x){return AH1_(1.0)/sqrt(x);}
  AH2 ARsqH2(AH2 x){return AH2_(1.0)/sqrt(x);}
  AH3 ARsqH3(AH3 x){return AH3_(1.0)/sqrt(x);}
  AH4 ARsqH4(AH4 x){return AH4_(1.0)/sqrt(x);}
//------------------------------------------------------------------------------------------------------------------------------
  AH1 ASatH1(AH1 x){return clamp(x,AH1_(0.0),AH1_(1.0));}
  AH2 ASatH2(AH2 x){return clamp(x,AH2_(0.0),AH2_(1.0));}
  AH3 ASatH3(AH3 x){return clamp(x,AH3_(0.0),AH3_(1.0));}
  AH4 ASatH4(AH4 x){return clamp(x,AH4_(0.0),AH4_(1.0));}
 #endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                         GLSL DOUBLE
//==============================================================================================================================
 #ifdef A_DUBL
  #define AD1 double
  #define AD2 dvec2
  #define AD3 dvec3
  #define AD4 dvec4
//------------------------------------------------------------------------------------------------------------------------------
  AD1 AD1_x(AD1 a){return AD1(a);}
  AD2 AD2_x(AD1 a){return AD2(a,a);}
  AD3 AD3_x(AD1 a){return AD3(a,a,a);}
  AD4 AD4_x(AD1 a){return AD4(a,a,a,a);}
  #define AD1_(a) AD1_x(AD1(a))
  #define AD2_(a) AD2_x(AD1(a))
  #define AD3_(a) AD3_x(AD1(a))
  #define AD4_(a) AD4_x(AD1(a))
 #endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                         GLSL LONG
//==============================================================================================================================
 #ifdef A_LONG
  #define AL1 uint64_t
  #define AL2 u64vec2
  #define AL3 u64vec3
  #define AL4 u64vec4
//------------------------------------------------------------------------------------------------------------------------------
  #define ASL1 int64_t
  #define ASL2 i64vec2
  #define ASL3 i64vec3
  #define ASL4 i64vec4
//------------------------------------------------------------------------------------------------------------------------------
  #define AL1_AU2(x) packUint2x32(AU2(x))
  #define AU2_AL1(x) unpackUint2x32(AL1(x))
//------------------------------------------------------------------------------------------------------------------------------
  AL1 AL1_x(AL1 a){return AL1(a);}
  AL2 AL2_x(AL1 a){return AL2(a,a);}
  AL3 AL3_x(AL1 a){return AL3(a,a,a);}
  AL4 AL4_x(AL1 a){return AL4(a,a,a,a);}
  #define AL1_(a) AL1_x(AL1(a))
  #define AL2_(a) AL2_x(AL1(a))
  #define AL3_(a) AL3_x(AL1(a))
  #define AL4_(a) AL4_x(AL1(a))
 #endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                      WAVE OPERATIONS
//==============================================================================================================================
 #ifdef A_WAVE
  AF1 AWaveAdd(AF1 v){return subgroupAdd(v);}
  AF2 AWaveAdd(AF2 v){return subgroupAdd(v);}
  AF3 AWaveAdd(AF3 v){return subgroupAdd(v);}
  AF4 AWaveAdd(AF4 v){return subgroupAdd(v);}
 #endif
//==============================================================================================================================
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//
//                                                            HLSL
//
//==============================================================================================================================
#if defined(A_HLSL) && defined(A_GPU)
 #define AP1 bool
 #define AP2 bool2
 #define AP3 bool3
 #define AP4 bool4
//------------------------------------------------------------------------------------------------------------------------------
 #define AF1 float
 #define AF2 float2
 #define AF3 float3
 #define AF4 float4
//------------------------------------------------------------------------------------------------------------------------------
 #define AU1 uint
 #define AU2 uint2
 #define AU3 uint3
 #define AU4 uint4
//------------------------------------------------------------------------------------------------------------------------------
 #define ASU1 int
 #define ASU2 int2
 #define ASU3 int3
 #define ASU4 int4
//==============================================================================================================================
 #define AF1_AU1(x) asfloat(AU1(x))
 #define AF2_AU2(x) asfloat(AU2(x))
 #define AF3_AU3(x) asfloat(AU3(x))
 #define AF4_AU4(x) asfloat(AU4(x))
//------------------------------------------------------------------------------------------------------------------------------
 #define AU1_AF1(x) asuint(AF1(x))
 #define AU2_AF2(x) asuint(AF2(x))
 #define AU3_AF3(x) asuint(AF3(x))
 #define AU4_AF4(x) asuint(AF4(x))
//------------------------------------------------------------------------------------------------------------------------------
 AU1 AU1_AH2_AF2_x(AF2 a){return f32tof16(a.x)|(f32tof16(a.y)<<16);}
 #define AU1_AH2_AF2(a) AU1_AH2_AF2_x(AF2(a))
 #define AU1_AB4Unorm_AF4(x) D3DCOLORtoUBYTE4(AF4(x))
//------------------------------------------------------------------------------------------------------------------------------
 AF2 AF2_AH2_AU1_x(AU1 x){return AF2(f16tof32(x&0xFFFF),f16tof32(x>>16));}
 #define AF2_AH2_AU1(x) AF2_AH2_AU1_x(AU1(x))
//==============================================================================================================================
 AF1 AF1_x(AF1 a){return AF1(a);}
 AF2 AF2_x(AF1 a){return AF2(a,a);}
 AF3 AF3_x(AF1 a){return AF3(a,a,a);}
 AF4 AF4_x(AF1 a){return AF4(a,a,a,a);}
 #define AF1_(a) AF1_x(AF1(a))
 #define AF2_(a) AF2_x(AF1(a))
 #define AF3_(a) AF3_x(AF1(a))
 #define AF4_(a) AF4_x(AF1(a))
//------------------------------------------------------------------------------------------------------------------------------
 AU1 AU1_x(AU1 a){return AU1(a);}
 AU2 AU2_x(AU1 a){return AU2(a,a);}
 AU3 AU3_x(AU1 a){return AU3(a,a,a);}
 AU4 AU4_x(AU1 a){return AU4(a,a,a,a);}
 #define AU1_(a) AU1_x(AU1(a))
 #define AU2_(a) AU2_x(AU1(a))
 #define AU3_(a) AU3_x(AU1(a))
 #define AU4_(a) AU4_x(AU1(a))
//==============================================================================================================================
 AU1 ABfe(AU1 src,AU1 off,AU1 bits){AU1 mask=(1<<bits)-1;return (src>>off)&mask;}
 AU1 ABfi(AU1 src,AU1 ins,AU1 mask){return (ins&mask)|(src&(~mask));}
 AU1 ABfiM(AU1 src,AU1 ins,AU1 bits){AU1 mask=(1<<bits)-1;return (ins&mask)|(src&(~mask));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 AFractF1(AF1 x){return x-floor(x);}
 AF2 AFractF2(AF2 x){return x-floor(x);}
 AF3 AFractF3(AF3 x){return x-floor(x);}
 AF4 AFractF4(AF4 x){return x-floor(x);}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 ALerpF1(AF1 x,AF1 y,AF1 a){return lerp(x,y,a);}
 AF2 ALerpF2(AF2 x,AF2 y,AF2 a){return lerp(x,y,a);}
 AF3 ALerpF3(AF3 x,AF3 y,AF3 a){return lerp(x,y,a);}
 AF4 ALerpF4(AF4 x,AF4 y,AF4 a){return lerp(x,y,a);}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 AMax3F1(AF1 x,AF1 y,AF1 z){return max(x,max(y,z));}
 AF2 AMax3F2(AF2 x,AF2 y,AF2 z){return max(x,max(y,z));}
 AF3 AMax3F3(AF3 x,AF3 y,AF3 z){return max(x,max(y,z));}
 AF4 AMax3F4(AF4 x,AF4 y,AF4 z){return max(x,max(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
 AU1 AMax3SU1(AU1 x,AU1 y,AU1 z){return AU1(max(ASU1(x),max(ASU1(y),ASU1(z))));}
 AU2 AMax3SU2(AU2 x,AU2 y,AU2 z){return AU2(max(ASU2(x),max(ASU2(y),ASU2(z))));}
 AU3 AMax3SU3(AU3 x,AU3 y,AU3 z){return AU3(max(ASU3(x),max(ASU3(y),ASU3(z))));}
 AU4 AMax3SU4(AU4 x,AU4 y,AU4 z){return AU4(max(ASU4(x),max(ASU4(y),ASU4(z))));}
//------------------------------------------------------------------------------------------------------------------------------
 AU1 AMax3U1(AU1 x,AU1 y,AU1 z){return max(x,max(y,z));}
 AU2 AMax3U2(AU2 x,AU2 y,AU2 z){return max(x,max(y,z));}
 AU3 AMax3U3(AU3 x,AU3 y,AU3 z){return max(x,max(y,z));}
 AU4 AMax3U4(AU4 x,AU4 y,AU4 z){return max(x,max(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 AMed3F1(AF1 x,AF1 y,AF1 z){return max(min(x,y),min(max(x,y),z));}
 AF2 AMed3F2(AF2 x,AF2 y,AF2 z){return max(min(x,y),min(max(x,y),z));}
 AF3 AMed3F3(AF3 x,AF3 y,AF3 z){return max(min(x,y),min(max(x,y),z));}
 AF4 AMed3F4(AF4 x,AF4 y,AF4 z){return max(min(x,y),min(max(x,y),z));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 AMin3F1(AF1 x,AF1 y,AF1 z){return min(x,min(y,z));}
 AF2 AMin3F2(AF2 x,AF2 y,AF2 z){return min(x,min(y,z));}
 AF3 AMin3F3(AF3 x,AF3 y,AF3 z){return min(x,min(y,z));}
 AF4 AMin3F4(AF4 x,AF4 y,AF4 z){return min(x,min(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
 AU1 AMin3SU1(AU1 x,AU1 y,AU1 z){return AU1(min(ASU1(x),min(ASU1(y),ASU1(z))));}
 AU2 AMin3SU2(AU2 x,AU2 y,AU2 z){return AU2(min(ASU2(x),min(ASU2(y),ASU2(z))));}
 AU3 AMin3SU3(AU3 x,AU3 y,AU3 z){return AU3(min(ASU3(x),min(ASU3(y),ASU3(z))));}
 AU4 AMin3SU4(AU4 x,AU4 y,AU4 z){return AU4(min(ASU4(x),min(ASU4(y),ASU4(z))));}
//------------------------------------------------------------------------------------------------------------------------------
 AU1 AMin3U1(AU1 x,AU1 y,AU1 z){return min(x,min(y,z));}
 AU2 AMin3U2(AU2 x,AU2 y,AU2 z){return min(x,min(y,z));}
 AU3 AMin3U3(AU3 x,AU3 y,AU3 z){return min(x,min(y,z));}
 AU4 AMin3U4(AU4 x,AU4 y,AU4 z){return min(x,min(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 ANCosF1(AF1 x){return cos(x*AF1_(A_2PI));}
 AF2 ANCosF2(AF2 x){return cos(x*AF2_(A_2PI));}
 AF3 ANCosF3(AF3 x){return cos(x*AF3_(A_2PI));}
 AF4 ANCosF4(AF4 x){return cos(x*AF4_(A_2PI));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 ANSinF1(AF1 x){return sin(x*AF1_(A_2PI));}
 AF2 ANSinF2(AF2 x){return sin(x*AF2_(A_2PI));}
 AF3 ANSinF3(AF3 x){return sin(x*AF3_(A_2PI));}
 AF4 ANSinF4(AF4 x){return sin(x*AF4_(A_2PI));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 ARcpF1(AF1 x){return rcp(x);}
 AF2 ARcpF2(AF2 x){return rcp(x);}
 AF3 ARcpF3(AF3 x){return rcp(x);}
 AF4 ARcpF4(AF4 x){return rcp(x);}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 ARsqF1(AF1 x){return rsqrt(x);}
 AF2 ARsqF2(AF2 x){return rsqrt(x);}
 AF3 ARsqF3(AF3 x){return rsqrt(x);}
 AF4 ARsqF4(AF4 x){return rsqrt(x);}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 ASatF1(AF1 x){return saturate(x);}
 AF2 ASatF2(AF2 x){return saturate(x);}
 AF3 ASatF3(AF3 x){return saturate(x);}
 AF4 ASatF4(AF4 x){return saturate(x);}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                          HLSL BYTE
//==============================================================================================================================
 #ifdef A_BYTE
 #endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                          HLSL HALF
//==============================================================================================================================
 #ifdef A_HALF
  #define AH1 min16float
  #define AH2 min16float2
  #define AH3 min16float3
  #define AH4 min16float4
//------------------------------------------------------------------------------------------------------------------------------
  #define AW1 min16uint
  #define AW2 min16uint2
  #define AW3 min16uint3
  #define AW4 min16uint4
//------------------------------------------------------------------------------------------------------------------------------
  #define ASW1 min16int
  #define ASW2 min16int2
  #define ASW3 min16int3
  #define ASW4 min16int4
//==============================================================================================================================
  // Need to use manual unpack to get optimal execution (don't use packed types in buffers directly).
  // Unpack requires this pattern: https://gpuopen.com/first-steps-implementing-fp16/
  AH2 AH2_AU1_x(AU1 x){AF2 t=f16tof32(AU2(x&0xFFFF,x>>16));return AH2(t);}
  AH4 AH4_AU2_x(AU2 x){return AH4(AH2_AU1_x(x.x),AH2_AU1_x(x.y));}
  AW2 AW2_AU1_x(AU1 x){AU2 t=AU2(x&0xFFFF,x>>16);return AW2(t);}
  AW4 AW4_AU2_x(AU2 x){return AW4(AW2_AU1_x(x.x),AW2_AU1_x(x.y));}
  #define AH2_AU1(x) AH2_AU1_x(AU1(x))
  #define AH4_AU2(x) AH4_AU2_x(AU2(x))
  #define AW2_AU1(x) AW2_AU1_x(AU1(x))
  #define AW4_AU2(x) AW4_AU2_x(AU2(x))
//------------------------------------------------------------------------------------------------------------------------------
  AU1 AU1_AH2_x(AH2 x){return f32tof16(x.x)+(f32tof16(x.y)<<16);}
  AU2 AU2_AH4_x(AH4 x){return AU2(AU1_AH2_x(x.xy),AU1_AH2_x(x.zw));}
  AU1 AU1_AW2_x(AW2 x){return AU1(x.x)+(AU1(x.y)<<16);}
  AU2 AU2_AW4_x(AW4 x){return AU2(AU1_AW2_x(x.xy),AU1_AW2_x(x.zw));}
  #define AU1_AH2(x) AU1_AH2_x(AH2(x))
  #define AU2_AH4(x) AU2_AH4_x(AH4(x))
  #define AU1_AW2(x) AU1_AW2_x(AW2(x))
  #define AU2_AW4(x) AU2_AW4_x(AW4(x))
//==============================================================================================================================
  #define AW1_AH1(x) AW1(asuint(AF1(x)))
  #define AW2_AH2(x) AW2(asuint(AF2(x)))
  #define AW3_AH3(x) AW3(asuint(AF3(x)))
  #define AW4_AH4(x) AW4(asuint(AF4(x)))
//------------------------------------------------------------------------------------------------------------------------------
  #define AH1_AW1(x) AH1(asfloat(AU1(x)))
  #define AH2_AW2(x) AH2(asfloat(AU2(x)))
  #define AH3_AW3(x) AH3(asfloat(AU3(x)))
  #define AH4_AW4(x) AH4(asfloat(AU4(x)))
//==============================================================================================================================
  AH1 AH1_x(AH1 a){return AH1(a);}
  AH2 AH2_x(AH1 a){return AH2(a,a);}
  AH3 AH3_x(AH1 a){return AH3(a,a,a);}
  AH4 AH4_x(AH1 a){return AH4(a,a,a,a);}
  #define AH1_(a) AH1_x(AH1(a))
  #define AH2_(a) AH2_x(AH1(a))
  #define AH3_(a) AH3_x(AH1(a))
  #define AH4_(a) AH4_x(AH1(a))
//------------------------------------------------------------------------------------------------------------------------------
  AW1 AW1_x(AW1 a){return AW1(a);}
  AW2 AW2_x(AW1 a){return AW2(a,a);}
  AW3 AW3_x(AW1 a){return AW3(a,a,a);}
  AW4 AW4_x(AW1 a){return AW4(a,a,a,a);}
  #define AW1_(a) AW1_x(AW1(a))
  #define AW2_(a) AW2_x(AW1(a))
  #define AW3_(a) AW3_x(AW1(a))
  #define AW4_(a) AW4_x(AW1(a))
//==============================================================================================================================
 // V_FRACT_F16 (note DX frac() is different).
  AH1 AFractH1(AH1 x){return x-floor(x);}
  AH2 AFractH2(AH2 x){return x-floor(x);}
  AH3 AFractH3(AH3 x){return x-floor(x);}
  AH4 AFractH4(AH4 x){return x-floor(x);}
//------------------------------------------------------------------------------------------------------------------------------
  AH1 AMax3H1(AH1 x,AH1 y,AH1 z){return max(x,max(y,z));}
  AH2 AMax3H2(AH2 x,AH2 y,AH2 z){return max(x,max(y,z));}
  AH3 AMax3H3(AH3 x,AH3 y,AH3 z){return max(x,max(y,z));}
  AH4 AMax3H4(AH4 x,AH4 y,AH4 z){return max(x,max(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
  AH1 AMin3H1(AH1 x,AH1 y,AH1 z){return min(x,min(y,z));}
  AH2 AMin3H2(AH2 x,AH2 y,AH2 z){return min(x,min(y,z));}
  AH3 AMin3H3(AH3 x,AH3 y,AH3 z){return min(x,min(y,z));}
  AH4 AMin3H4(AH4 x,AH4 y,AH4 z){return min(x,min(y,z));}
//------------------------------------------------------------------------------------------------------------------------------
  AH1 ARcpH1(AH1 x){return rcp(x);}
  AH2 ARcpH2(AH2 x){return rcp(x);}
  AH3 ARcpH3(AH3 x){return rcp(x);}
  AH4 ARcpH4(AH4 x){return rcp(x);}
//------------------------------------------------------------------------------------------------------------------------------
  AH1 ARsqH1(AH1 x){return rsqrt(x);}
  AH2 ARsqH2(AH2 x){return rsqrt(x);}
  AH3 ARsqH3(AH3 x){return rsqrt(x);}
  AH4 ARsqH4(AH4 x){return rsqrt(x);}
//------------------------------------------------------------------------------------------------------------------------------
  AH1 ASatH1(AH1 x){return saturate(x);}
  AH2 ASatH2(AH2 x){return saturate(x);}
  AH3 ASatH3(AH3 x){return saturate(x);}
  AH4 ASatH4(AH4 x){return saturate(x);}
 #endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                         HLSL DOUBLE
//==============================================================================================================================
 #ifdef A_DUBL
  #define AD1 double
  #define AD2 double2
  #define AD3 double3
  #define AD4 double4
//------------------------------------------------------------------------------------------------------------------------------
  AD1 AD1_x(AD1 a){return AD1(a);}
  AD2 AD2_x(AD1 a){return AD2(a,a);}
  AD3 AD3_x(AD1 a){return AD3(a,a,a);}
  AD4 AD4_x(AD1 a){return AD4(a,a,a,a);}
  #define AD1_(a) AD1_x(AD1(a))
  #define AD2_(a) AD2_x(AD1(a))
  #define AD3_(a) AD3_x(AD1(a))
  #define AD4_(a) AD4_x(AD1(a))
 #endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                         HLSL LONG
//==============================================================================================================================
 #ifdef A_LONG
 #endif
//==============================================================================================================================
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//
//                                                          GPU COMMON
//
//==============================================================================================================================
#ifdef A_GPU
 // Negative and positive infinity.
 #define A_INFN_F AF1_AU1(0x7f800000u)
 #define A_INFP_F AF1_AU1(0xff800000u)
//------------------------------------------------------------------------------------------------------------------------------
 // Copy sign from 's' to positive 'd'.
 AF1 ACpySgnF1(AF1 d,AF1 s){return AF1_AU1(AU1_AF1(d)|(AU1_AF1(s)&AU1_(0x80000000u)));}
 AF2 ACpySgnF2(AF2 d,AF2 s){return AF2_AU2(AU2_AF2(d)|(AU2_AF2(s)&AU2_(0x80000000u)));}
 AF3 ACpySgnF3(AF3 d,AF3 s){return AF3_AU3(AU3_AF3(d)|(AU3_AF3(s)&AU3_(0x80000000u)));}
 AF4 ACpySgnF4(AF4 d,AF4 s){return AF4_AU4(AU4_AF4(d)|(AU4_AF4(s)&AU4_(0x80000000u)));}
//------------------------------------------------------------------------------------------------------------------------------
 // Single operation to return (useful to create a mask to use in lerp for branch free logic),
 //  m=NaN := 0
 //  m>=0  := 0
 //  m<0   := 1
 // Uses the following useful floating point logic,
 //  saturate(+a*(-INF)==-INF) := 0
 //  saturate( 0*(-INF)== NaN) := 0
 //  saturate(-a*(-INF)==+INF) := 1
 AF1 ASignedF1(AF1 m){return ASatF1(m*AF1_(A_INFN_F));}
 AF2 ASignedF2(AF2 m){return ASatF2(m*AF2_(A_INFN_F));}
 AF3 ASignedF3(AF3 m){return ASatF3(m*AF3_(A_INFN_F));}
 AF4 ASignedF4(AF4 m){return ASatF4(m*AF4_(A_INFN_F));}
//==============================================================================================================================
 #ifdef A_HALF
  #define A_INFN_H AH1_AW1(0x7c00u)
  #define A_INFP_H AH1_AW1(0xfc00u)
//------------------------------------------------------------------------------------------------------------------------------
  AH1 ACpySgnH1(AH1 d,AH1 s){return AH1_AW1(AW1_AH1(d)|(AW1_AH1(s)&AW1_(0x8000u)));}
  AH2 ACpySgnH2(AH2 d,AH2 s){return AH2_AW2(AW2_AH2(d)|(AW2_AH2(s)&AW2_(0x8000u)));}
  AH3 ACpySgnH3(AH3 d,AH3 s){return AH3_AW3(AW3_AH3(d)|(AW3_AH3(s)&AW3_(0x8000u)));}
  AH4 ACpySgnH4(AH4 d,AH4 s){return AH4_AW4(AW4_AH4(d)|(AW4_AH4(s)&AW4_(0x8000u)));}
//------------------------------------------------------------------------------------------------------------------------------
  AH1 ASignedH1(AH1 m){return ASatH1(m*AH1_(A_INFN_H));}
  AH2 ASignedH2(AH2 m){return ASatH2(m*AH2_(A_INFN_H));}
  AH3 ASignedH3(AH3 m){return ASatH3(m*AH3_(A_INFN_H));}
  AH4 ASignedH4(AH4 m){return ASatH4(m*AH4_(A_INFN_H));}
 #endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                     HALF APPROXIMATIONS
//------------------------------------------------------------------------------------------------------------------------------
// These support only positive inputs.
// Did not see value yet in specialization for range.
// Using quick testing, ended up mostly getting the same "best" approximation for various ranges.
// With hardware that can co-execute transcendentals, the value in approximations could be less than expected.
// However from a latency perspective, if execution of a transcendental is 4 clk, with no packed support, -> 8 clk total.
// And co-execution would require a compiler interleaving a lot of independent work for packed usage.
//------------------------------------------------------------------------------------------------------------------------------
// The one Newton Raphson iteration form of rsq() was skipped (requires 6 ops total).
// Same with sqrt(), as this could be x*rsq() (7 ops).
//------------------------------------------------------------------------------------------------------------------------------
// IDEAS
// =====
//  - Polaris hardware has 16-bit support, but non-double rate.
//    Could be possible still get part double rate for some of this logic,
//    by clearing out the lower half's sign when necessary and using 32-bit ops...
//==============================================================================================================================
 #ifdef A_HALF
  // Minimize squared error across full positive range, 2 ops.
  // The 0x1de2 based approximation maps {0 to 1} input maps to < 1 output.
  AH1 APrxLoSqrtH1(AH1 a){return AH1_AW1((AW1_AH1(a)>>AW1_(1))+AW1_(0x1de2));}
  AH2 APrxLoSqrtH2(AH2 a){return AH2_AW2((AW2_AH2(a)>>AW2_(1))+AW2_(0x1de2));}
//------------------------------------------------------------------------------------------------------------------------------
  // Lower precision estimation, 1 op.
  // Minimize squared error across {smallest normal to 16384.0}.
  AH1 APrxLoRcpH1(AH1 a){return AH1_AW1(AW1_(0x7784)-AW1_AH1(a));}
  AH2 APrxLoRcpH2(AH2 a){return AH2_AW2(AW2_(0x7784)-AW2_AH2(a));}
//------------------------------------------------------------------------------------------------------------------------------
  // Medium precision estimation, one Newton Raphson iteration, 3 ops.
  AH1 APrxMedRcpH1(AH1 a){AH1 b=AH1_AW1(AW1_(0x778d)-AW1_AH1(a));return b*(-b*a+AH1_(2.0));}
  AH2 APrxMedRcpH2(AH2 a){AH2 b=AH2_AW2(AW2_(0x778d)-AW2_AH2(a));return b*(-b*a+AH2_(2.0));}
//------------------------------------------------------------------------------------------------------------------------------
  // Minimize squared error across {smallest normal to 16384.0}, 2 ops.
  AH1 APrxLoRsqH1(AH1 a){return AH1_AW1(AW1_(0x59a3)-(AW1_AH1(a)>>AW1_(1)));}
  AH2 APrxLoRsqH2(AH2 a){return AH2_AW2(AW2_(0x59a3)-(AW2_AH2(a)>>AW2_(1)));}
 #endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                    FLOAT APPROXIMATIONS
//------------------------------------------------------------------------------------------------------------------------------
// Michal Drobot has an excellent presentation on these: "Low Level Optimizations For GCN",
//  - Idea dates back to SGI, then to Quake 3, etc.
//  - https://michaldrobot.files.wordpress.com/2014/05/gcn_alu_opt_digitaldragons2014.pdf
//     - sqrt(x)=rsqrt(x)*x
//     - rcp(x)=rsqrt(x)*rsqrt(x) for positive x
//  - https://github.com/michaldrobot/ShaderFastLibs/blob/master/ShaderFastMathLib.h
//------------------------------------------------------------------------------------------------------------------------------
// These below are from perhaps less complete searching for optimal.
// Used FP16 normal range for testing with +4096 32-bit step size for sampling error.
// So these match up well with the half approximations.
//==============================================================================================================================
 AF1 APrxLoSqrtF1(AF1 a){return AF1_AU1((AU1_AF1(a)>>AU1_(1))+AU1_(0x1fbc4639));}
 AF1 APrxLoRcpF1(AF1 a){return AF1_AU1(AU1_(0x7ef07ebb)-AU1_AF1(a));}
 AF1 APrxMedRcpF1(AF1 a){AF1 b=AF1_AU1(AU1_(0x7ef19fff)-AU1_AF1(a));return b*(-b*a+AF1_(2.0));}
 AF1 APrxLoRsqF1(AF1 a){return AF1_AU1(AU1_(0x5f347d74)-(AU1_AF1(a)>>AU1_(1)));}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                    PARABOLIC SIN & COS
//------------------------------------------------------------------------------------------------------------------------------
// Approximate answers to transcendental questions.
//------------------------------------------------------------------------------------------------------------------------------
// TODO
// ====
//  - Verify packed math ABS is correctly doing an AND.
//==============================================================================================================================
 // Valid input range is {-1 to 1} representing {0 to 2 pi}.
 // Output range is {-1/4 to -1/4} representing {-1 to 1}.
 AF1 APSinF1(AF1 x){return x*abs(x)-x;} // MAD.
 AF1 APCosF1(AF1 x){x=AFractF1(x*AF1_(0.5)+AF1_(0.75));x=x*AF1_(2.0)-AF1_(1.0);return APSinF1(x);} // 3x MAD, FRACT
//------------------------------------------------------------------------------------------------------------------------------
 #ifdef A_HALF
  // For a packed {sin,cos} pair,
  //  - Native takes 16 clocks and 4 issue slots (no packed transcendentals).
  //  - Parabolic takes 8 clocks and 8 issue slots (only fract is non-packed).
  AH2 APSinH2(AH2 x){return x*abs(x)-x;} // AND,FMA
  AH2 APCosH2(AH2 x){x=AFractH2(x*AH2_(0.5)+AH2_(0.75));x=x*AH2_(2.0)-AH2_(1.0);return APSinH2(x);} // 3x FMA, 2xFRACT, AND
 #endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                      COLOR CONVERSIONS
//------------------------------------------------------------------------------------------------------------------------------
// These are all linear to/from some other space (where 'linear' has been shortened out of the function name).
// So 'ToGamma' is 'LinearToGamma', and 'FromGamma' is 'LinearFromGamma'.
// These are branch free implementations.
// The AToSrgbF1() function is useful for stores for compute shaders for GPUs without hardware linear->sRGB store conversion.
//------------------------------------------------------------------------------------------------------------------------------
// TRANSFER FUNCTIONS
// ==================
// 709 ..... Rec709 used for some HDTVs
// Gamma ... Typically 2.2 for some PC displays, or 2.4-2.5 for CRTs, or 2.2 FreeSync2 native
// Pq ...... PQ native for HDR10
// Srgb .... The sRGB output, typical of PC displays, useful for 10-bit output, or storing to 8-bit UNORM without SRGB type
// Two ..... Gamma 2.0, fastest conversion (useful for intermediate pass approximations)
//------------------------------------------------------------------------------------------------------------------------------
// FOR PQ
// ======
// Both input and output is {0.0-1.0}, and where output 1.0 represents 10000.0 cd/m^2.
// All constants are only specified to FP32 precision.
// External PQ source reference,
//  - https://github.com/ampas/aces-dev/blob/master/transforms/ctl/utilities/ACESlib.Utilities_Color.a1.0.1.ctl
//------------------------------------------------------------------------------------------------------------------------------
// PACKED VERSIONS
// ===============
// These are the A*H2() functions.
// There is no PQ functions as FP16 seemed to not have enough precision for the conversion.
// The remaining functions are "good enough" for 8-bit, and maybe 10-bit if not concerned about a few 1-bit errors.
// Precision is lowest in the 709 conversion, higher in sRGB, higher still in Two and Gamma (when using 2.2 at least).
//------------------------------------------------------------------------------------------------------------------------------
// NOTES
// =====
// Could be faster for PQ conversions to be in ALU or a texture lookup depending on usage case.
//==============================================================================================================================
 AF1 ATo709F1(AF1 c){return max(min(c*AF1_(4.5),AF1_(0.018)),AF1_(1.099)*pow(c,AF1_(0.45))-AF1_(0.099));}
//------------------------------------------------------------------------------------------------------------------------------
 // Note 'rcpX' is '1/x', where the 'x' is what would be used in AFromGamma().
 AF1 AToGammaF1(AF1 c,AF1 rcpX){return pow(c,rcpX);}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 AToPqF1(AF1 x){AF1 p=pow(x,AF1_(0.159302));
  return pow((AF1_(0.835938)+AF1_(18.8516)*p)/(AF1_(1.0)+AF1_(18.6875)*p),AF1_(78.8438));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 AToSrgbF1(AF1 c){return max(min(c*AF1_(12.92),AF1_(0.0031308)),AF1_(1.055)*pow(c,AF1_(0.41666))-AF1_(0.055));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 AToTwoF1(AF1 c){return sqrt(c);}
//==============================================================================================================================
 AF1 AFrom709F1(AF1 c){return max(min(c*AF1_(1.0/4.5),AF1_(0.081)),
  pow((c+AF1_(0.099))*(AF1_(1.0)/(AF1_(1.099))),AF1_(1.0/0.45)));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 AFromGammaF1(AF1 c,AF1 x){return pow(c,x);}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 AFromPqF1(AF1 x){AF1 p=pow(x,AF1_(0.0126833));
  return pow(ASatF1(p-AF1_(0.835938))/(AF1_(18.8516)-AF1_(18.6875)*p),AF1_(6.27739));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 AFromSrgbF1(AF1 c){return max(min(c*AF1_(1.0/12.92),AF1_(0.04045)),
  pow((c+AF1_(0.055))*(AF1_(1.0)/AF1_(1.055)),AF1_(2.4)));}
//------------------------------------------------------------------------------------------------------------------------------
 AF1 AFromTwoF1(AF1 c){return c*c;}
//==============================================================================================================================
 #ifdef A_HALF
  AH2 ATo709H2(AH2 c){return max(min(c*AH2_(4.5),AH2_(0.018)),AH2_(1.099)*pow(c,AH2_(0.45))-AH2_(0.099));}
//------------------------------------------------------------------------------------------------------------------------------
  AH2 AToGammaH2(AH2 c,AH1 rcpX){return pow(c,AH2_(rcpX));}
//------------------------------------------------------------------------------------------------------------------------------
  AH2 AToSrgbH2(AH2 c){return max(min(c*AH2_(12.92),AH2_(0.0031308)),AH2_(1.055)*pow(c,AH2_(0.41666))-AH2_(0.055));}
//------------------------------------------------------------------------------------------------------------------------------
  AH2 AToTwoH2(AH2 c){return sqrt(c);}
 #endif
//==============================================================================================================================
 #ifdef A_HALF
  AH2 AFrom709H2(AH2 c){return max(min(c*AH2_(1.0/4.5),AH2_(0.081)),
   pow((c+AH2_(0.099))*(AH2_(1.0)/(AH2_(1.099))),AH2_(1.0/0.45)));}
//------------------------------------------------------------------------------------------------------------------------------
  AH2 AFromGammaH2(AH2 c,AH1 x){return pow(c,AH2_(x));}
//------------------------------------------------------------------------------------------------------------------------------
  AH2 AFromSrgbH2(AH2 c){return max(min(c*AH2_(1.0/12.92),AH2_(0.04045)),
   pow((c+AH2_(0.055))*(AH2_(1.0)/AH2_(1.055)),AH2_(2.4)));}
//------------------------------------------------------------------------------------------------------------------------------
  AH2 AFromTwoH2(AH2 c){return c*c;}
 #endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//                                                          CS REMAP
//==============================================================================================================================
 // Simple remap 64x1 to 8x8 with rotated 2x2 pixel quads in quad linear.
 //  543210
 //  ======
 //  ..xxx.
 //  yy...y
 AU2 ARmp8x8(AU1 a){return AU2(ABfe(a,1u,3u),ABfiM(ABfe(a,3u,3u),a,1u));}
//==============================================================================================================================
 // More complex remap 64x1 to 8x8 which is necessary for 2D wave reductions.
 //  543210
 //  ======
 //  .xx..x
 //  y..yy.
 // Details,
 //  LANE TO 8x8 MAPPING
 //  ===================
 //  00 01 08 09 10 11 18 19
 //  02 03 0a 0b 12 13 1a 1b
 //  04 05 0c 0d 14 15 1c 1d
 //  06 07 0e 0f 16 17 1e 1f
 //  20 21 28 29 30 31 38 39
 //  22 23 2a 2b 32 33 3a 3b
 //  24 25 2c 2d 34 35 3c 3d
 //  26 27 2e 2f 36 37 3e 3f
 AU2 ARmpRed8x8(AU1 a){return AU2(ABfiM(ABfe(a,2u,3u),a,1u),ABfiM(ABfe(a,3u,3u),ABfe(a,1u,2u),2u));}
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________/\_______________________________________________________________
//==============================================================================================================================
//
//                                                          REFERENCE
//
//------------------------------------------------------------------------------------------------------------------------------
// IEEE FLOAT RULES
// ================
//  - saturate(NaN)=0, saturate(-INF)=0, saturate(+INF)=1
//  - {+/-}0 * {+/-}INF = NaN
//  - -INF + (+INF) = NaN
//  - {+/-}0 / {+/-}0 = NaN
//  - {+/-}INF / {+/-}INF = NaN
//  - a<(-0) := sqrt(a) = NaN (a=-0.0 won't NaN)
//  - 0 == -0
//  - 4/0 = +INF
//  - 4/-0 = -INF
//  - 4+INF = +INF
//  - 4-INF = -INF
//  - 4*(+INF) = +INF
//  - 4*(-INF) = -INF
//  - -4*(+INF) = -INF
//  - sqrt(+INF) = +INF
//------------------------------------------------------------------------------------------------------------------------------
// FP16 ENCODING
// =============
// fedcba9876543210
// ----------------
// ......mmmmmmmmmm  10-bit mantissa (encodes 11-bit 0.5 to 1.0 except for denormals)
// .eeeee..........  5-bit exponent
// .00000..........  denormals
// .00001..........  -14 exponent
// .11110..........   15 exponent
// .111110000000000  infinity
// .11111nnnnnnnnnn  NaN with n!=0
// s...............  sign
//------------------------------------------------------------------------------------------------------------------------------
// FP16/INT16 ALIASING DENORMAL
// ============================
// 11-bit unsigned integers alias with half float denormal/normal values,
//     1 = 2^(-24) = 1/16777216 ....................... first denormal value
//     2 = 2^(-23)
//   ...
//  1023 = 2^(-14)*(1-2^(-10)) = 2^(-14)*(1-1/1024) ... last denormal value
//  1024 = 2^(-14) = 1/16384 .......................... first normal value that still maps to integers
//  2047 .............................................. last normal value that still maps to integers
// Scaling limits,
//  2^15 = 32768 ...................................... largest power of 2 scaling
// Largest pow2 conversion mapping is at *32768,
//     1 : 2^(-9) = 1/128
//  1024 : 8
//  2047 : a little less than 16
//==============================================================================================================================
