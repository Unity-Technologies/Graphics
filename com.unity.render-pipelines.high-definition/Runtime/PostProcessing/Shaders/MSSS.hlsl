
#define SCALE (0.707)

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_________________________________________________________________/\___________________________________________________________________
//======================================================================================================================================
//                                                      SUPPORT FOR TEST SHADER
//======================================================================================================================================
#define I1 int
#define I2 int2 
#define F1 float
#define F2 float2
#define F2_(x) F2(x,x)
#define F3 float3
#define F4 float4
//--------------------------------------------------------------------------------------------------------------------------------------
// Convert from linear to sRGB.
F1 Srgb(F1 c){return(c<0.0031308?c*12.92:1.055*pow(c,0.41666)-0.055);}
//--------------------------------------------------------------------------------------------------------------------------------------
// Convert from sRGB to linear.
F1 Linear(F1 c){return(c<=0.04045)?c/12.92:pow((c+0.055)/1.055,2.4);}
//--------------------------------------------------------------------------------------------------------------------------------------
// Dummy shader given pixel position.
#if 0
F3 Shade(F2 p){
 p.xy+=sin(iTime)*2.0;
 // Texture.
 F3 t=texture(iChannel0,F2_(4.0)*p/iChannelResolution[0].xy).rgb;
 // Pattern.
 F2 pp=F2(p.x+p.y/16.0,p.y+p.x/16.0);
 pp*=pp;
 F1 x=sin(pp.x/800.0)>0.0?0.5:0.0;
 F1 y=sin(pp.y/1000.0)>0.0?0.5:0.0;
 return t*(x+y);}
#endif
//--------------------------------------------------------------------------------------------------------------------------------------
// Simulated fetch callback.
#if 0
F4 MsssTexF(I2 p,I1 s){
 // Build sample position.
 F2 f=F2(p)+F2(0.5,0.5);
 if(s==0)f+=F2(-2.0/16.0,-6.0/16.0);
 if(s==1)f+=F2( 6.0/16.0,-2.0/16.0);
 if(s==2)f+=F2(-6.0/16.0, 2.0/16.0);
 if(s==3)f+=F2( 2.0/16.0, 6.0/16.0);
 F4 r;
 r.rgb=Shade(f);
 r.a=0.0;
 return r;}
#endif

//#define MSSS_DEBUG 1
#define MSSS_BUG 0
#define MSSS_HLSL 1
#define MSSS_32BIT 1
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_________________________________________________________________/\___________________________________________________________________
//======================================================================================================================================
//
//                                                [MSSS] MSAA SCALING SUPER SAMPLING
//
//======================================================================================================================================
// 0 = off
// 1 = view H filter
// 2 = view V filter
// 3 = view T filter
// 4 = min/max of H and V
#ifndef MSSS_BUG
 #define MSSS_BUG 0
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_________________________________________________________________/\___________________________________________________________________
//======================================================================================================================================
//                                                             PORTABILITY
//======================================================================================================================================
#ifdef MSSS_GLSL
 #define MsssI1 int
 #define MsssI2 ivec2
 #define MsssF1 float
 #define MsssF2 vec2
 #define MsssF3 vec3
 #define MsssF4 vec4
 #define MsssF4_(x) MsssF4(x,x,x,x)
//--------------------------------------------------------------------------------------------------------------------------------------
 MsssF4 MsssLerpF4(MsssF4 x,MsssF4 y,MsssF4 a){return mix(x,y,a);}
 MsssF1 MsssRcpF1(MsssF1 x){return 1.0/x;}
 MsssF1 MsssSatF1(MsssF1 x){return clamp(x,0.0,1.0);}
#endif
//======================================================================================================================================
#ifdef MSSS_HLSL
 #define MsssI1 int
 #define MsssI2 int2
 #define MsssF1 float
 #define MsssF2 float2
 #define MsssF3 float3
 #define MsssF4 float4
 #define MsssF4_(x) MsssF4(x,x,x,x)
//--------------------------------------------------------------------------------------------------------------------------------------
 MsssF4 MsssLerpF4(MsssF4 x,MsssF4 y,MsssF4 a){return lerp(x,y,a);}
 MsssF1 MsssRcpF1(MsssF1 x){return rcp(x);}
 MsssF1 MsssSatF1(MsssF1 x){return saturate(x);}
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_________________________________________________________________/\___________________________________________________________________
//======================================================================================================================================
//                                                       32-BIT SCALING RESOLVE
//--------------------------------------------------------------------------------------------------------------------------------------
// WORK IN PROGRESS
// ================
// - Fix floor() for HLSL.
// - Introduce broadcast defines.
// - Finish kernel blending logic...
// - Try negative lobes...
//======================================================================================================================================
#ifdef MSSS_32BIT
 // MSAA texture fetch prototype.
 MsssF4 MsssTexF(MsssI2 p,MsssI1 s);
//======================================================================================================================================
 // Accumulation.
 void MsssAccF(inout MsssF4 kH,inout MsssF4 kV,inout MsssF4 kT,inout MsssF1 wH,inout MsssF1 wV,inout MsssF1 wT,MsssF4 c,MsssF2 o){
  MsssF1 aH;
  MsssF1 aV;
  MsssF1 aT;
  // Kernel weights. Largest distance to avoid window artifacts at 4xMSAA with 16 taps on nearest 2x2 pixels is 0.625.
  // Which leaves very "zippery" (or dithered) edges when scaled up.
  // The 'H'orz and 'V'ert filters allow window artifacts on one axis to avoid dither on edges.
  // TODO: Optimize this...
  // ANISO...
  #define MSSS_ANISO 0.5
  // END is set to either 0.707 (full pixel) or 0.75 (slightly larger) which implies some amount of window artifacts.
  #define MSSS_END 0.75
  #define MSSS_WIN (1.0/(MSSS_END*MSSS_END))
  // The following controls the thin/diagonal filter.
  #define MSSS_TND 0.707
  #define MSSS_TIN (1.0/(MSSS_TND*MSSS_TND))
  MsssF2 oH=o;oH.x*=MSSS_ANISO;aH=MsssSatF1(dot(oH,oH)*MSSS_WIN)-1.0;aH*=aH;
  MsssF2 oV=o;oV.y*=MSSS_ANISO;aV=MsssSatF1(dot(oV,oV)*MSSS_WIN)-1.0;aV*=aV;
                               aT=MsssSatF1(dot(o ,o )*MSSS_TIN)-1.0;aT*=aT;
  // Accumulate.
  kH+=c*aH;kV+=c*aV;kT+=c*aT;
  wH+=aH;wV+=aV;wT+=aT;}
//======================================================================================================================================
 // Where,
 //  p ....... integer pixel position in the output
 //  k0.xy ... input/output resolution
 //  k0.zw ... k0.xy * 0.5 - 0.5
 MsssF4 MsssF(MsssI2 p,MsssF4 k0){
//--------------------------------------------------------------------------------------------------------------------------------------
  // ZOOM IN FOR DEBUG
  #ifdef MSSS_DEBUG
   p>>=2;
  #endif
//--------------------------------------------------------------------------------------------------------------------------------------
  // Weighted accumulation for the 3 kernels {Horz,Vert,Tight}.
  MsssF4 kH=MsssF4_(0.0);
  MsssF4 kV=MsssF4_(0.0);
  MsssF4 kT=MsssF4_(0.0);
  MsssF1 wH=MsssF1(0.0);
  MsssF1 wV=MsssF1(0.0);
  MsssF1 wT=MsssF1(0.0);
//--------------------------------------------------------------------------------------------------------------------------------------
  // Find upper left of nearest 2x2 pixels.
  //  +-------+-------+
  //  |       |       |
  //  |   A...|...B   |
  //  |   :   |   :   |
  //  +-------+-------+
  //  |   :   |   :   |
  //  |   C...|...D   |
  //  |       |       |
  //  +-------+-------+
  // Convert from integer pixel position in output to centered pixel position in output then to input and -0.5 get 'A'.
  MsssF2 p2=MsssF2(p)*k0.xy+k0.zw;
  // Floor that to get float integer pixel position in input for 'A'.
  MsssF2 pF=floor(p2);
  // Convert back to integer for fetch.
  MsssI2 pI=MsssI2(pF);
  // Get back to distance from pixel center 'A' to resolve position.
  // The following factors cancel out,
  //  pF+=0.5 -> get to center 'A'
  //  p2+=0.5 -> get back to resolve position
  pF=pF-p2;
//--------------------------------------------------------------------------------------------------------------------------------------
  // Fetch samples for pixel, and do accumulation of kernels.
  MsssF4 cA0=MsssTexF(pI,0);
  MsssF4 cA1=MsssTexF(pI,1);
  MsssF4 cA2=MsssTexF(pI,2);
  MsssF4 cA3=MsssTexF(pI,3);
  MsssAccF(kH,kV,kT,wH,wV,wT,cA0,pF+MsssF2(-1.0/8.0,-3.0/8.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cA1,pF+MsssF2( 3.0/8.0,-1.0/8.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cA2,pF+MsssF2(-3.0/8.0, 1.0/8.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cA3,pF+MsssF2( 1.0/8.0, 3.0/8.0));
  pI.x+=1;
  MsssF4 cB0=MsssTexF(pI,0);
  MsssF4 cB1=MsssTexF(pI,1);
  MsssF4 cB2=MsssTexF(pI,2);
  MsssF4 cB3=MsssTexF(pI,3);
  MsssAccF(kH,kV,kT,wH,wV,wT,cB0,pF+MsssF2(-1.0/8.0+1.0,-3.0/8.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cB1,pF+MsssF2( 3.0/8.0+1.0,-1.0/8.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cB2,pF+MsssF2(-3.0/8.0+1.0, 1.0/8.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cB3,pF+MsssF2( 1.0/8.0+1.0, 3.0/8.0));
  pI.y+=1;
  MsssF4 cD0=MsssTexF(pI,0);
  MsssF4 cD1=MsssTexF(pI,1);
  MsssF4 cD2=MsssTexF(pI,2);
  MsssF4 cD3=MsssTexF(pI,3);
  MsssAccF(kH,kV,kT,wH,wV,wT,cD0,pF+MsssF2(-1.0/8.0+1.0,-3.0/8.0+1.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cD1,pF+MsssF2( 3.0/8.0+1.0,-1.0/8.0+1.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cD2,pF+MsssF2(-3.0/8.0+1.0, 1.0/8.0+1.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cD3,pF+MsssF2( 1.0/8.0+1.0, 3.0/8.0+1.0));
  pI.x-=1;
  MsssF4 cC0=MsssTexF(pI,0);
  MsssF4 cC1=MsssTexF(pI,1);
  MsssF4 cC2=MsssTexF(pI,2);
  MsssF4 cC3=MsssTexF(pI,3);
  MsssAccF(kH,kV,kT,wH,wV,wT,cC0,pF+MsssF2(-1.0/8.0,-3.0/8.0+1.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cC1,pF+MsssF2( 3.0/8.0,-1.0/8.0+1.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cC2,pF+MsssF2(-3.0/8.0, 1.0/8.0+1.0));
  MsssAccF(kH,kV,kT,wH,wV,wT,cC3,pF+MsssF2( 1.0/8.0, 3.0/8.0+1.0));
//--------------------------------------------------------------------------------------------------------------------------------------
  // Normalize by weight.
  // TODO: Approximation?
  kH*=MsssRcpF1(wH);
  kV*=MsssRcpF1(wV);
  kT*=MsssRcpF1(wT);
//--------------------------------------------------------------------------------------------------------------------------------------
  // Compute approximate luma * 4.0.
  MsssF1 lH=(kH.g*MsssF1(2.0)+kH.r)+kH.b;
  MsssF1 lV=(kV.g*MsssF1(2.0)+kV.r)+kV.b;
  MsssF1 lT=(kT.g*MsssF1(2.0)+kT.r)+kT.b;
//--------------------------------------------------------------------------------------------------------------------------------------
  // View H filter.
  #if MSSS_BUG==1
   return kH;
  #endif
//--------------------------------------------------------------------------------------------------------------------------------------
  // View V filter.
  #if MSSS_BUG==2
   return kV;
  #endif
//--------------------------------------------------------------------------------------------------------------------------------------
  // View T filter.
  #if MSSS_BUG==3
   return kT;
  #endif
//--------------------------------------------------------------------------------------------------------------------------------------
  // Blending logic.
  //  - Blend between H and V based on which is closer to T
  //  - Blend to T if H and V get to similar
  // Visualize blend logic.
  #if MSSS_BUG==4
   kH=MsssF4(1.0,0.0,0.0,0.0);
   kV=MsssF4(0.0,1.0,0.0,0.0);
   kT=MsssF4(0.0,0.0,1.0,0.0);
  #endif
  // Distance to T.
  MsssF1 dH=abs(lH-lT);
  MsssF1 dV=abs(lV-lT);
  // Rcp sum of distance.
  MsssF1 dR=MsssRcpF1(max(MsssF1(1.0/32768.0),dH+dV));
  // Convert into blend ratio.
  MsssF1 dB=dH*dR;
  // TODO...
  #if 0
   dB=dB*dB*(MsssF1(3.0)-MsssF1(2.0)*dB);
  #endif
  // Blend between H and V.
  MsssF4 c=MsssLerpF4(kH,kV,MsssF4_(dB));
#if 0
  // Make near to 0.5 go to T.
  // TODO: Optimize this.
  dB-=MsssF1(0.5);
  dB*=MsssF1(2.0);
  dB=MsssSatF1(dB*dB);
  c=MsssLerpF4(c,kT,MsssF4_(dB));
#endif
  return c;}
#endif










////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_________________________________________________________________/\___________________________________________________________________
//======================================================================================================================================
//                                                             TEST SHADER
//======================================================================================================================================
void mainImage(out F4 fragColor,in F2 fragCoord){
 // Do actual resolve.
 fragColor.rgb=MsssF(
  //  p ....... integer pixel position in the output
  MsssI2(floor(fragCoord.xy)),
  //  k0.xy ... input/output resolution
  //  k0.zw ... k0.xy * 0.5 - 0.5
  MsssF4(SCALE,SCALE,SCALE*0.5-0.5,SCALE*0.5-0.5)).rgb;
  fragColor.r=Srgb(fragColor.r);
  fragColor.g=Srgb(fragColor.g);
  fragColor.b=Srgb(fragColor.b);}
