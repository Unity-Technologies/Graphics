////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_________________________________________________________________/\___________________________________________________________________
//======================================================================================================================================
//
//                                            [MSSS] MSAA SCALING SUPER SAMPLING v20210624
//
//--------------------------------------------------------------------------------------------------------------------------------------
// CHANGELIST
// ==========
// 20210624 - Optimized MsssKrnF() logic with regards to saturation placement.
//======================================================================================================================================
// 0 = off
// 1 = view H filter
// 2 = view V filter
// 3 = view T filter
// 4 = view blend logic
#ifndef MSSS_BUG
 #define MSSS_BUG 0
#endif
//--------------------------------------------------------------------------------------------------------------------------------------
// 0 = denoiser off
// 1 = denoiser on
#ifndef MSSS_DENOISE
 #define MSSS_DENOISE 0
#endif
//--------------------------------------------------------------------------------------------------------------------------------------
// 4 = 4xMSAA
// 8 = 8xMSAA
#ifndef MSSS_MSAA
 #define MSSS_MSAA 4
#endif
//--------------------------------------------------------------------------------------------------------------------------------------
// 0 = off
// 1 = zoom in to view output pixels for debug
#ifndef MSSS_ZOOM
 #define MSSS_ZOOM 0
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_________________________________________________________________/\___________________________________________________________________
//======================================================================================================================================
//                                                             PORTABILITY
//======================================================================================================================================
#ifdef MSSS_GLSL
 #define MsssP1 bool
 #define MsssI1 int
 #define MsssI2 ivec2
 #define MsssF1 float
 #define MsssF2 vec2
 #define MsssF4 vec4
 #define MsssF4_(x) MsssF4(x,x,x,x)
//--------------------------------------------------------------------------------------------------------------------------------------
 MsssF4 MsssLerpF4(MsssF4 x,MsssF4 y,MsssF4 a){return mix(x,y,a);}
 MsssF1 MsssRcpF1(MsssF1 x){return 1.0/x;}
 MsssF1 MsssSatF1(MsssF1 x){return clamp(x,0.0,1.0);}
#endif
//======================================================================================================================================
#ifdef MSSS_HLSL
 #define MsssP1 bool
 #define MsssI1 int
 #define MsssI2 int2
 #define MsssF1 float
 #define MsssF2 float2
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
// [ ] Try approximations for RCP.
// [ ] Add FP16 option.
// [ ] Try retuning for 8xMSAA.
// [ ] If 'T' kernel isn't going to be used for color, maybe do only luma.
// ---
// [x] Finish kernel blending logic.
// [x] Introduce broadcast defines.
// [x] Add 8xMSAA option.
// [x] Add denoise logic.
//======================================================================================================================================
#ifdef MSSS_32BIT
 // MSAA texture fetch prototype.
 MsssF4 MsssTexF(MsssI2 p,MsssI1 s);
 // Load luma feedback prototype.
 MsssF4 MsssLdF(MsssI2 p);
 // Store luma feedback prototype.
 void MsssStF(MsssI2 p,MsssF4 c);
//======================================================================================================================================
 // Convert from MSAA sample color to "luma" used by the algorithm.
 MsssF1 MsssLumF(MsssF4 c){
  // Get maximum.
  MsssF1 m=max(max(c.r,c.g),c.b);
  // Invert the invertable tonemapper.
  m=MsssF1(1.0)-m;
  m=max(m,MsssF1(1.0/32768.0));
  return MsssRcpF1(m);}
//--------------------------------------------------------------------------------------------------------------------------------------
 // Process four.
 MsssF4 MsssLum4F(MsssF4 a,MsssF4 b,MsssF4 c,MsssF4 d){return MsssF4(MsssLumF(a),MsssLumF(b),MsssLumF(c),MsssLumF(d));}
//--------------------------------------------------------------------------------------------------------------------------------------
 // Denoise logic for four samples, returns weights.
 MsssF4 MsssDe4F(MsssI2 p,MsssF4 f,MsssF4 c0,MsssF4 c1,MsssF4 c2,MsssF4 c3,MsssP1 s0,MsssP1 s1,MsssF1 k1){
  // Skip logic if denoiser isn't used.
  #if MSSS_DENOISE==0
   return MsssF4_(1.0);
  #endif
  // Convert color into "luma".
  MsssF4 l=MsssLum4F(c0,c1,c2,c3);
  // Generate new feedback for next frame.
  // TODO: Tune this!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
  MsssF4 n=MsssLerpF4(f,l,MsssF4_(1.0/2.0));
  // Optionaly store feedback to one of the targets.
  if(s0)MsssStF(p,n);
  if(s1)MsssSt2F(p,n);
  // Get difference feedback and this frame's luma.
  MsssF4 w=abs(f-l);
  // Convert into weighting term.
  w=MsssF4_(k1)/(w+MsssF4_(k1));
  return w;}
//======================================================================================================================================
 // Filter kernel.
 MsssF1 MsssKrnF(MsssF1 x){x=MsssSatF1(1.0-x);return x*x;}
//======================================================================================================================================
 // Accumulation.
 void MsssAccF(inout MsssF4 kH,inout MsssF4 kV,inout MsssF4 kT,inout MsssF1 wH,inout MsssF1 wV,inout MsssF1 wT,MsssF4 c,MsssF2 o,MsssF1 w){
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
  MsssF2 oH=o;oH.x*=MSSS_ANISO;aH=MsssKrnF(dot(oH,oH)*MSSS_WIN);
  MsssF2 oV=o;oV.y*=MSSS_ANISO;aV=MsssKrnF(dot(oV,oV)*MSSS_WIN);
                               aT=MsssKrnF(dot(o ,o )*MSSS_TIN);
  // Weight.
  #if MSSS_DENOISE
   aH*=w;aV*=w;aT*=w;
  #endif
  // Accumulate.
  kH+=c*aH;kV+=c*aV;kT+=c*aT;
  wH+=aH;wV+=aV;wT+=aT;}
//======================================================================================================================================
 // Where,
 //  p ....... integer pixel position in the output
 //  k0.xy ... input/output resolution
 //  k0.zw ... k0.xy * 0.5 - 0.5
 //  k1 ...... denoise constant 'exp2(g)', where 'g=0' is full noise reduction, and 'g>0' reduces effect
 MsssF4 MsssF(MsssI2 p,MsssF4 k0,MsssF1 k1){
//--------------------------------------------------------------------------------------------------------------------------------------
  // Zoom in for debug.
  #if MSSS_ZOOM
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
  // 4xMSAA fetch samples for pixel, and do accumulation of kernels.
  #if MSSS_MSAA==4
   MsssF4 cA0=MsssTexF(pI,0);
   MsssF4 cA1=MsssTexF(pI,1);
   MsssF4 cA2=MsssTexF(pI,2);
   MsssF4 cA3=MsssTexF(pI,3); 
   MsssF4 nA=MsssLdF(pI);
   MsssF4 wA=MsssDe4F(pI,nA,cA0,cA1,cA2,cA3,true,false,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA0,pF+MsssF2(-1.0/8.0,-3.0/8.0),wA.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA1,pF+MsssF2( 3.0/8.0,-1.0/8.0),wA.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA2,pF+MsssF2(-3.0/8.0, 1.0/8.0),wA.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA3,pF+MsssF2( 1.0/8.0, 3.0/8.0),wA.w);
   pI.x+=1;
   MsssF4 cB0=MsssTexF(pI,0);
   MsssF4 cB1=MsssTexF(pI,1);
   MsssF4 cB2=MsssTexF(pI,2);
   MsssF4 cB3=MsssTexF(pI,3);
   MsssF4 nB=MsssLdF(pI);
   MsssF4 wB=MsssDe4F(pI,nB,cB0,cB1,cB2,cB3,false,false,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB0,pF+MsssF2(-1.0/8.0+1.0,-3.0/8.0),wB.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB1,pF+MsssF2( 3.0/8.0+1.0,-1.0/8.0),wB.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB2,pF+MsssF2(-3.0/8.0+1.0, 1.0/8.0),wB.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB3,pF+MsssF2( 1.0/8.0+1.0, 3.0/8.0),wB.w);
   pI.y+=1;
   MsssF4 cD0=MsssTexF(pI,0);
   MsssF4 cD1=MsssTexF(pI,1);
   MsssF4 cD2=MsssTexF(pI,2);
   MsssF4 cD3=MsssTexF(pI,3);
   MsssF4 nD=MsssLdF(pI);
   MsssF4 wD=MsssDe4F(pI,nD,cD0,cD1,cD2,cD3,false,false,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD0,pF+MsssF2(-1.0/8.0+1.0,-3.0/8.0+1.0),wD.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD1,pF+MsssF2( 3.0/8.0+1.0,-1.0/8.0+1.0),wD.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD2,pF+MsssF2(-3.0/8.0+1.0, 1.0/8.0+1.0),wD.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD3,pF+MsssF2( 1.0/8.0+1.0, 3.0/8.0+1.0),wD.w);
   pI.x-=1;
   MsssF4 cC0=MsssTexF(pI,0);
   MsssF4 cC1=MsssTexF(pI,1);
   MsssF4 cC2=MsssTexF(pI,2);
   MsssF4 cC3=MsssTexF(pI,3);
   MsssF4 nC=MsssLdF(pI);
   MsssF4 wC=MsssDe4F(pI,nC,cC0,cC1,cC2,cC3,false,false,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC0,pF+MsssF2(-1.0/8.0,-3.0/8.0+1.0),wC.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC1,pF+MsssF2( 3.0/8.0,-1.0/8.0+1.0),wC.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC2,pF+MsssF2(-3.0/8.0, 1.0/8.0+1.0),wC.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC3,pF+MsssF2( 1.0/8.0, 3.0/8.0+1.0),wC.w);
  #endif
//--------------------------------------------------------------------------------------------------------------------------------------
  // 8xMSAA fetch samples for pixel, and do accumulation of kernels.
  #if MSSS_MSAA==8
   MsssF4 cA0=MsssTexF(pI,0);
   MsssF4 cA1=MsssTexF(pI,1);
   MsssF4 cA2=MsssTexF(pI,2);
   MsssF4 cA3=MsssTexF(pI,3);
   MsssF4 nA=MsssLdF(pI);
   MsssF4 wA=MsssDe4F(pI,nA,cA0,cA1,cA2,cA3,true,false,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA0,pF+MsssF2( 1.0/16.0,-3.0/16.0),wA.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA1,pF+MsssF2(-1.0/16.0, 3.0/16.0),wA.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA2,pF+MsssF2( 5.0/16.0, 1.0/16.0),wA.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA3,pF+MsssF2(-3.0/16.0,-5.0/16.0),wA.w);
   MsssF4 cA4=MsssTexF(pI,4);
   MsssF4 cA5=MsssTexF(pI,5);
   MsssF4 cA6=MsssTexF(pI,6);
   MsssF4 cA7=MsssTexF(pI,7);
   nA=MsssLd2F(pI);
   wA=MsssDe4F(pI,nA,cA4,cA5,cA6,cA7,false,true,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA4,pF+MsssF2(-5.0/16.0, 5.0/16.0),wA.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA5,pF+MsssF2(-7.0/16.0, 1.0/16.0),wA.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA6,pF+MsssF2( 3.0/16.0, 7.0/16.0),wA.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cA7,pF+MsssF2( 7.0/16.0,-7.0/16.0),wA.w);
   pI.x+=1;
   MsssF4 cB0=MsssTexF(pI,0);
   MsssF4 cB1=MsssTexF(pI,1);
   MsssF4 cB2=MsssTexF(pI,2);
   MsssF4 cB3=MsssTexF(pI,3);
   MsssF4 nB=MsssLdF(pI);
   MsssF4 wB=MsssDe4F(pI,nB,cB0,cB1,cB2,cB3,false,false,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB0,pF+MsssF2( 1.0/16.0+1.0,-3.0/16.0),wB.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB1,pF+MsssF2(-1.0/16.0+1.0, 3.0/16.0),wB.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB2,pF+MsssF2( 5.0/16.0+1.0, 1.0/16.0),wB.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB3,pF+MsssF2(-3.0/16.0+1.0,-5.0/16.0),wB.w);
   MsssF4 cB4=MsssTexF(pI,4);
   MsssF4 cB5=MsssTexF(pI,5);
   MsssF4 cB6=MsssTexF(pI,6);
   MsssF4 cB7=MsssTexF(pI,7);
   nB=MsssLd2F(pI);
   wB=MsssDe4F(pI,nB,cB4,cB5,cB6,cB7,false,false,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB4,pF+MsssF2(-5.0/16.0+1.0, 5.0/16.0),wB.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB5,pF+MsssF2(-7.0/16.0+1.0, 1.0/16.0),wB.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB6,pF+MsssF2( 3.0/16.0+1.0, 7.0/16.0),wB.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cB7,pF+MsssF2( 7.0/16.0+1.0,-7.0/16.0),wB.w);
   pI.y+=1;
   MsssF4 cD0=MsssTexF(pI,0);
   MsssF4 cD1=MsssTexF(pI,1);
   MsssF4 cD2=MsssTexF(pI,2);
   MsssF4 cD3=MsssTexF(pI,3);
   MsssF4 nD=MsssLdF(pI);
   MsssF4 wD=MsssDe4F(pI,nD,cD0,cD1,cD2,cD3,false,false,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD0,pF+MsssF2( 1.0/16.0+1.0,-3.0/16.0+1.0),wD.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD1,pF+MsssF2(-1.0/16.0+1.0, 3.0/16.0+1.0),wD.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD2,pF+MsssF2( 5.0/16.0+1.0, 1.0/16.0+1.0),wD.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD3,pF+MsssF2(-3.0/16.0+1.0,-5.0/16.0+1.0),wD.w);
   MsssF4 cD4=MsssTexF(pI,4);
   MsssF4 cD5=MsssTexF(pI,5);
   MsssF4 cD6=MsssTexF(pI,6);
   MsssF4 cD7=MsssTexF(pI,7);
   nD=MsssLd2F(pI);
   wD=MsssDe4F(pI,nD,cD4,cD5,cD6,cD7,false,false,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD4,pF+MsssF2(-5.0/16.0+1.0, 5.0/16.0+1.0),wD.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD5,pF+MsssF2(-7.0/16.0+1.0, 1.0/16.0+1.0),wD.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD6,pF+MsssF2( 3.0/16.0+1.0, 7.0/16.0+1.0),wD.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cD7,pF+MsssF2( 7.0/16.0+1.0,-7.0/16.0+1.0),wD.w);
   pI.x-=1;
   MsssF4 cC0=MsssTexF(pI,0);
   MsssF4 cC1=MsssTexF(pI,1);
   MsssF4 cC2=MsssTexF(pI,2);
   MsssF4 cC3=MsssTexF(pI,3);
   MsssF4 nC=MsssLdF(pI);
   MsssF4 wC=MsssDe4F(pI,nC,cC0,cC1,cC2,cC3,false,false,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC0,pF+MsssF2( 1.0/16.0,-3.0/16.0+1.0),wC.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC1,pF+MsssF2(-1.0/16.0, 3.0/16.0+1.0),wC.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC2,pF+MsssF2( 5.0/16.0, 1.0/16.0+1.0),wC.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC3,pF+MsssF2(-3.0/16.0,-5.0/16.0+1.0),wC.w);
   MsssF4 cC4=MsssTexF(pI,4);
   MsssF4 cC5=MsssTexF(pI,5);
   MsssF4 cC6=MsssTexF(pI,6);
   MsssF4 cC7=MsssTexF(pI,7);
   nC=MsssLd2F(pI);
   wC=MsssDe4F(pI,nC,cC4,cC5,cC6,cC7,false,false,k1);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC4,pF+MsssF2(-5.0/16.0, 5.0/16.0+1.0),wC.x);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC5,pF+MsssF2(-7.0/16.0, 1.0/16.0+1.0),wC.y);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC6,pF+MsssF2( 3.0/16.0, 7.0/16.0+1.0),wC.z);
   MsssAccF(kH,kV,kT,wH,wV,wT,cC7,pF+MsssF2( 7.0/16.0,-7.0/16.0+1.0),wC.w);
  #endif
//--------------------------------------------------------------------------------------------------------------------------------------
  // Normalize by weight.
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
  // Blending logic. Blend between H and V based on which is closer to T.
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
  // Blend between H and V.
  MsssF4 c=MsssLerpF4(kH,kV,MsssF4_(dB));
  return c;}
#endif

