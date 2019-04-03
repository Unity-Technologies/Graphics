#ifndef FILE_FOREST_VTXANIM_SHARED2
#define FILE_FOREST_VTXANIM_SHARED2

#define USE_VEGETATION_ANIM 1

//UnityPerMaterial
//float _WindElasticityLvlB;
//float _WindElasticityLvl0;
//float _WindElasticityLvl1;
//float _WindRangeLvlB;
//float _WindRangeLvl0;
//float _WindRangeLvl1;
//float _WindFlutterElasticity;
//float _WindFlutterScale;
//float _WindFlutterPhase;
//float _WindFlutterPeriodScale;
//float _WindFakeSingleObjectPivot;

//
CBUFFER_START(GlobalWindParams)

float4x4 _WindData_0;
#define _WindData_0_0 _WindData_0[0]
#define _WindData_0_1 _WindData_0[1]
//float4 _WindData_0_0;
//float4 _WindData_0_1;
#define _WindDirection			_WindData_0_0.xyz
#define _WindEnabled			_WindData_0_0.w
#define _WindDirectionStable	_WindData_0_1.xyz
#define _WindTime				_WindData_0_1.w
//float3 _WindDirection;
//float _WindEnabled;
//float3 _WindDirectionStable;
//float _WindTime;

float4x4 _WindData_1;
#define _WindData_1_0 _WindData_1[0]
#define _WindData_1_1 _WindData_1[1]
#define _WindData_1_2 _WindData_1[2]
#define _WindData_1_3 _WindData_1[3]
//float4 _WindData_1_0;
//float4 _WindData_1_1;
//float4 _WindData_1_2;
//float4 _WindData_1_3;
#define _WindBaseStrength                _WindData_1_0.x
#define _WindBaseStrengthOffset			 _WindData_1_0.y
#define _WindBaseStrengthPhase			 _WindData_1_0.z
#define _WindBaseStrengthPhase2			 _WindData_1_0.w
#define _WindBaseStrengthVariancePeriod	 _WindData_1_1.x
#define _WindGustStrength				 _WindData_1_1.y
#define _WindGustStrengthOffset			 _WindData_1_1.z
#define _WindGustStrengthPhase			 _WindData_1_1.w
#define _WindGustStrengthPhase2			 _WindData_1_2.x
#define _WindGustStrengthVariancePeriod	 _WindData_1_2.y
#define _WindGustInnerCosScale			 _WindData_1_2.z
#define _WindFlutterStrength			 _WindData_1_2.w
#define _WindFlutterGustStrength		 _WindData_1_3.x
#define _WindFlutterGustStrengthOffset	 _WindData_1_3.y
#define _WindFlutterGustStrengthScale	 _WindData_1_3.z
#define _WindFlutterGustVariancePeriod	 _WindData_1_3.w
//float _WindBaseStrength;
//float _WindBaseStrengthOffset;
//float _WindBaseStrengthPhase;
//float _WindBaseStrengthPhase2;
//float _WindBaseStrengthVariancePeriod;
//float _WindGustStrength;
//float _WindGustStrengthOffset;
//float _WindGustStrengthPhase;
//float _WindGustStrengthPhase2;
//float _WindGustStrengthVariancePeriod;
//float _WindGustInnerCosScale;
//float _WindFlutterStrength;
//float _WindFlutterGustStrength;
//float _WindFlutterGustStrengthOffset;
//float _WindFlutterGustStrengthScale;
//float _WindFlutterGustVariancePeriod;

float4x4 _WindData_2;
#define _WindData_2_0 _WindData_2[0]
#define _WindData_2_1 _WindData_2[1]
#define _WindData_2_2 _WindData_2[2]
#define _WindData_2_3 _WindData_2[3]
//float4 _WindData_2_0;
//float4 _WindData_2_1;
//float4 _WindData_2_2;
//float4 _WindData_2_3;
#define _WindTreeBaseStrength                 _WindData_2_0.x
#define _WindTreeBaseStrengthOffset			  _WindData_2_0.y
#define _WindTreeBaseStrengthPhase			  _WindData_2_0.z
#define _WindTreeBaseStrengthPhase2			  _WindData_2_0.w
#define _WindTreeBaseStrengthVariancePeriod	  _WindData_2_1.x
#define _WindTreeGustStrength				  _WindData_2_1.y
#define _WindTreeGustStrengthOffset			  _WindData_2_1.z
#define _WindTreeGustStrengthPhase			  _WindData_2_1.w
#define _WindTreeGustStrengthPhase2			  _WindData_2_2.x
#define _WindTreeGustStrengthVariancePeriod	  _WindData_2_2.y
#define _WindTreeGustInnerCosScale			  _WindData_2_2.z
#define _WindTreeFlutterStrength			  _WindData_2_2.w
#define _WindTreeFlutterGustStrength		  _WindData_2_3.x
#define _WindTreeFlutterGustStrengthOffset	  _WindData_2_3.y
#define _WindTreeFlutterGustStrengthScale	  _WindData_2_3.z
#define _WindTreeFlutterGustVariancePeriod	  _WindData_2_3.w
//float _WindTreeBaseStrength;
//float _WindTreeBaseStrengthOffset;
//float _WindTreeBaseStrengthPhase;
//float _WindTreeBaseStrengthPhase2;
//float _WindTreeBaseStrengthVariancePeriod;
//float _WindTreeGustStrength;
//float _WindTreeGustStrengthOffset;
//float _WindTreeGustStrengthPhase;
//float _WindTreeGustStrengthPhase2;
//float _WindTreeGustStrengthVariancePeriod;
//float _WindTreeGustInnerCosScale;
//float _WindTreeFlutterStrength;
//float _WindTreeFlutterGustStrength;
//float _WindTreeFlutterGustStrengthOffset;
//float _WindTreeFlutterGustStrengthScale;
//float _WindTreeFlutterGustVariancePeriod;

CBUFFER_END
//

#define WIND_PI 3.1416f
#define WIND_PI2 6.2832f

float4 QuaternionFromAxisAngle(float3 axis, float angle) {
	float2 hsc;
	sincos(angle * 0.5f, hsc.x, hsc.y);
	return float4(axis * hsc.x, hsc.y);
}

float3 QuaternionRotatePoint(float3 pnt, float4 quat) {
	return pnt + 2.0 * cross(quat.xyz, cross(quat.xyz, pnt) + quat.w * pnt);
}

float3 QuaternionRotatePointAbout(float3 pnt, float3 abt, float4 quat) {
	return abt + QuaternionRotatePoint(pnt - abt, quat);
}

float3 QuaternionRotateVector(float3 vec, float4 quat) {
	return vec + 2.0 * cross(quat.xyz, cross(quat.xyz, vec) + quat.w * vec);
}

float UnpackFixedToSFloat(uint val, float range, uint bits, uint shift) {
	const uint BitMask = (1 << bits) - 1;
	val = (val >> shift) & BitMask;
	float fval = val / (float)BitMask;
	return (fval * 2.f - 1.f) * range;
}

float UnpackFixedToUFloat(uint val, float range, uint bits, uint shift) {
	const uint BitMask = (1 << bits) - 1;
	val = (val >> shift) & BitMask;
	float fval = val / (float)BitMask;
	return fval * range;
}

// Needs to match shader packing in baking tool
bool UnpackPivot0(uint3 packedData, inout float3 pivotPos0, inout float3 pivotFwd0) {
	if(packedData.y & 0xFFFF0000) {
		pivotPos0.x = UnpackFixedToSFloat(packedData.x, 8.f, 10, 22);
		pivotPos0.y = UnpackFixedToUFloat(packedData.x, 32.f, 12, 10);
		pivotPos0.z = UnpackFixedToSFloat(packedData.x, 8.f, 10, 0);
		pivotFwd0.x = UnpackFixedToSFloat(packedData.y, 1.f, 8, 24);
		pivotFwd0.z = UnpackFixedToSFloat(packedData.y, 1.f, 7, 17);
		pivotFwd0.y = sqrt(1.f - saturate(dot(pivotFwd0.xz, pivotFwd0.xz))) * (((packedData.y >> 16) & 1) ? 1.f : -1.f);
		pivotFwd0 = normalize(pivotFwd0);
		return true;
	}
	return false;
}

// Needs to match shader packing in baking tool
bool UnpackPivot1(uint3 packedData, inout float3 pivotPos1, inout float3 pivotFwd1) {
	if(packedData.y & 0x0000FFFF) {
		pivotFwd1.x = UnpackFixedToSFloat(packedData.y, 1.f, 8, 8);
		pivotFwd1.z = UnpackFixedToSFloat(packedData.y, 1.f, 7, 1);
		pivotFwd1.y = sqrt(1.f - saturate(dot(pivotFwd1.xz, pivotFwd1.xz))) * ((packedData.y & 1) ? 1.f : -1.f);
		pivotFwd1 = normalize(pivotFwd1);
		pivotPos1.x = UnpackFixedToSFloat(packedData.z, 8.f, 10, 22);
		pivotPos1.y = UnpackFixedToUFloat(packedData.z, 32.f, 12, 10);
		pivotPos1.z = UnpackFixedToSFloat(packedData.z, 8.f, 10, 0);
		return true;
	}
	return false;
}

float4x4 GetActualObject2World() {
	float4x4 object2World = GetObjectToWorldMatrix();
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
	object2World._m03_m13_m23 += _WorldSpaceCameraPos;
#endif
	return object2World;
}

float3 GetWindDirection(float3 worldPivot) {
	return normalize(_WindDirection + frac(float3(worldPivot.x, 0, worldPivot.z)) * 0.2f - 0.1f);
}

float3 GetBaseGustWind(float3 worldPivot, float3 worldFwd, float timeNudge) {
	float objectBasePhase = dot(worldPivot + worldFwd, _WindBaseStrengthPhase) + dot(worldPivot + worldFwd, _WindDirectionStable) * -_WindBaseStrengthPhase2;
	float objectGustPhase = dot(worldPivot + worldFwd, _WindGustStrengthPhase) + dot(worldPivot + worldFwd, _WindDirectionStable) * -_WindGustStrengthPhase2;

	float windBaseCosInner = WIND_PI2 * (_WindTime + timeNudge + objectBasePhase) / _WindBaseStrengthVariancePeriod;
	float windBase = (cos(windBaseCosInner) * 0.5f + _WindBaseStrengthOffset) * _WindBaseStrength;
	float windGustCosInner = WIND_PI2 * (_WindTime + timeNudge + objectGustPhase) / _WindGustStrengthVariancePeriod;
	float windGust = (cos(windGustCosInner + cos(windGustCosInner * _WindGustInnerCosScale)) * 0.5f + _WindGustStrengthOffset) * _WindGustStrength;

	return float3(windBase, windGust, windBase + windGust);
}

float3 GetTreeBaseGustWind(float3 worldPivot, float timeNudge) {
	float objectBasePhase = dot(worldPivot, _WindTreeBaseStrengthPhase) + dot(worldPivot, _WindDirectionStable) * -_WindTreeBaseStrengthPhase2;
	float objectGustPhase = dot(worldPivot, _WindTreeGustStrengthPhase) + dot(worldPivot, _WindDirectionStable) * -_WindTreeGustStrengthPhase2;

	float windBaseCosInner = WIND_PI2 * (_WindTime + timeNudge + objectBasePhase) / _WindTreeBaseStrengthVariancePeriod;
	float windBase = (cos(windBaseCosInner) * 0.5f + _WindTreeBaseStrengthOffset) * _WindTreeBaseStrength;
	float windGustCosInner = WIND_PI2 * (_WindTime + timeNudge + objectGustPhase) / _WindTreeGustStrengthVariancePeriod;
	float windGust = (cos(windGustCosInner + cos(windGustCosInner * _WindTreeGustInnerCosScale)) * 0.5f + _WindTreeGustStrengthOffset) * _WindTreeGustStrength;

	return float3(windBase, windGust, windBase + windGust);
}

#if defined(USE_VEGETATION_ANIM) && defined(_ANIM_HIERARCHY_PIVOT)
void AnimateVegetationHierarchyPivot(inout float3 worldPos, inout float3 worldNrm, float3 pivotData, float3 pivotColor, float3 objectRoot, float timeNudge) {
	if(_WindEnabled == 0)
		return;

	uint3 packedData = asuint(pivotData);
	float3 pivotPos0, pivotPos1, pivotFwd0, pivotFwd1;
	bool pivotEnabled0 = UnpackPivot0(packedData, pivotPos0, pivotFwd0);
	bool pivotEnabled1 = UnpackPivot1(packedData, pivotPos1, pivotFwd1);

	float lvBRelativeObjectScale = mul(GetActualObject2World(), float4(0, _WindRangeLvlB, 0, 0)).y;

	float3 windFwd = GetWindDirection(objectRoot);
	float3 lvBBaseGustWind = GetTreeBaseGustWind(objectRoot, timeNudge);

	float3 lvBPos = objectRoot;
	float3 lvBFwd = float3(0, 1, 0); //TODO: grab from rotation matrix

	float lvBElasticity = _WindElasticityLvlB;
	float lvBDistScale = saturate((worldPos.y - objectRoot.y) / lvBRelativeObjectScale);
	lvBElasticity *= lvBDistScale;

	float lvBWindRotAngle = lvBBaseGustWind.x * lvBElasticity;
	lvBWindRotAngle = log2(1.f + abs(lvBWindRotAngle)) * sign(lvBWindRotAngle);

	float3 lvBWindAxis = cross(lvBFwd, windFwd);
	float4 lvBWindQuat = QuaternionFromAxisAngle(lvBWindAxis, lvBWindRotAngle);

	worldPos = QuaternionRotatePointAbout(worldPos, lvBPos, lvBWindQuat);
	worldNrm = QuaternionRotateVector(worldNrm, lvBWindQuat);

#if 1
	if(pivotEnabled0) {
		float3 lv0Pos = mul(GetActualObject2World(), float4(pivotPos0, 1)).xyz;
		float3 lv0PosStatic = lv0Pos;
		float3 lv0Fwd = normalize(mul((float3x3)GetActualObject2World(), pivotFwd0));
		lv0Pos = QuaternionRotatePointAbout(lv0Pos, lvBPos, lvBWindQuat);
		lv0Fwd = QuaternionRotateVector(lv0Fwd, lvBWindQuat);

		float3 lv0BaseGustWind = GetTreeBaseGustWind(lv0PosStatic, timeNudge);

		float lv0Elasticity = _WindElasticityLvl0;
		float lv0DistScale = saturate(length(worldPos - lv0Pos) / mul(GetActualObject2World(), float4(0, _WindRangeLvl0, 0, 0)).y);
		lv0Elasticity *= lv0DistScale * lvBDistScale;
		float lv0WindRotAngle = lv0BaseGustWind.z * lv0Elasticity;

		lv0Fwd.y *= lv0BaseGustWind.y * lvBDistScale;
		lv0Fwd = normalize(lv0Fwd);

		float3 lv0WindAxis = cross(windFwd, lv0Fwd);
		float3 lv0WindRight = cross(windFwd, lv0WindAxis);

		float lv0PerpendicularFactor = dot(lv0Fwd, lv0WindRight);
		float lv0AngleFactor = lv0PerpendicularFactor * lv0PerpendicularFactor;
		lv0AngleFactor *= sign(lv0PerpendicularFactor);

		lv0WindRotAngle *= lv0AngleFactor;

		float4 lv0WindQuat = QuaternionFromAxisAngle(lv0WindAxis, lv0WindRotAngle);

		worldPos = QuaternionRotatePointAbout(worldPos, lv0Pos, lv0WindQuat);
		worldNrm = QuaternionRotateVector(worldNrm, lv0WindQuat);

#if 1
		if(pivotEnabled1) {
			float3 lv1Pos = mul(GetActualObject2World(), float4(pivotPos1, 1)).xyz;
			float3 lv1PosStatic = lv1Pos;
			float3 lv1Fwd = normalize(mul((float3x3)GetActualObject2World(), pivotFwd1));
			lv1Pos = QuaternionRotatePointAbout(lv1Pos, lvBPos, lvBWindQuat);
			lv1Pos = QuaternionRotatePointAbout(lv1Pos, lv0Pos, lv0WindQuat);
			lv1Fwd = QuaternionRotateVector(lv1Fwd, lvBWindQuat);
			lv1Fwd = QuaternionRotateVector(lv1Fwd, lv0WindQuat);

			float3 lv1BaseGustWind = GetTreeBaseGustWind(lv1PosStatic, timeNudge);

			float lv1Elasticity = _WindElasticityLvl1;
			float lv1DistScale = saturate(length(worldPos - lv1Pos) / mul(GetActualObject2World(), float4(0, _WindRangeLvl1, 0, 0)).y);
			lv1Elasticity *= lv1DistScale * lvBDistScale;

			float vertexFlutterPhase = dot(worldPos, _WindFlutterPhase);
			float windFlutterCos = cos(WIND_PI2 * (_WindTime + timeNudge + vertexFlutterPhase) / (_WindTreeFlutterGustVariancePeriod * _WindFlutterPeriodScale));
			float windFlutterStrength = lv1Elasticity * _WindFlutterElasticity * _WindFlutterScale * (_WindTreeFlutterStrength + saturate((max(0.f, lv0BaseGustWind.z) - _WindTreeFlutterGustStrengthOffset) / _WindTreeFlutterGustStrengthScale) * _WindTreeFlutterGustStrength);

			float3 lv1WindAxis = cross(windFwd, lv1Fwd);
			worldPos += lv1WindAxis * windFlutterCos * windFlutterStrength;

			float lv1WindRotAngle = lv1BaseGustWind.z * lv1Elasticity * lv0DistScale;
			float4 lv1WindQuat = QuaternionFromAxisAngle(lv0WindAxis, lv1WindRotAngle);

			worldPos = QuaternionRotatePointAbout(worldPos, lv1Pos, lv1WindQuat);
			worldNrm = QuaternionRotateVector(worldNrm, lv1WindQuat);
		}
#endif
	}
#endif
}
#endif//defined(USE_VEGETATION_ANIM) && defined(_ANIM_HIERARCHY_PIVOT)

#if defined(USE_VEGETATION_ANIM) && (defined(_ANIM_SINGLE_PIVOT_COLOR) || defined(_ANIM_PROCEDURAL_BRANCH))
void AnimateVegetationSinglePivot(inout float3 worldPos, inout float3 worldNrm, float3 worldElementPivot, float3 worldElementPivotFwd, float3 pivotColor, float timeNudge) {
	float relativeObjectScale = length(worldElementPivotFwd);
	worldElementPivotFwd /= relativeObjectScale;

	float3 windFwd = GetWindDirection(worldElementPivot);
	float3 baseGustWind = GetBaseGustWind(worldElementPivot, worldElementPivotFwd, timeNudge);

	float windGust = baseGustWind.y;
	float windMain = baseGustWind.z;
	
	float distScale = pivotColor.r;
	distScale *= saturate(length(worldElementPivot - worldPos) / (_WindRangeLvlB * relativeObjectScale));

	float elasticity = distScale * _WindElasticityLvlB;
	float windRotAngle = windMain * elasticity;
	windRotAngle = log2(1.f + abs(windRotAngle)) * sign(windRotAngle);

	float3 windAxis = cross(windFwd, worldElementPivotFwd);
	float3 windRight = cross(windFwd, windAxis);

	float vertexFlutterPhase = dot(worldPos, _WindFlutterPhase / relativeObjectScale);
	float windFlutterCos = cos(WIND_PI2 * (_WindTime + timeNudge + vertexFlutterPhase) / (_WindFlutterGustVariancePeriod * _WindFlutterPeriodScale));
	float windFlutterStrength = distScale * _WindFlutterElasticity * _WindFlutterScale * (_WindFlutterStrength + saturate((max(0.f, windGust) - _WindFlutterGustStrengthOffset) / _WindFlutterGustStrengthScale) * _WindFlutterGustStrength);
	worldPos += windAxis * windFlutterCos * windFlutterStrength * relativeObjectScale;

	float perpendicularFactor = dot(worldElementPivotFwd, windRight);
	float angleFactor = saturate(perpendicularFactor * perpendicularFactor + 0.2f);
	angleFactor *= sign(perpendicularFactor);

	windRotAngle *= angleFactor;

	float4 windQuat = QuaternionFromAxisAngle(windAxis, windRotAngle);

	worldPos = QuaternionRotatePointAbout(worldPos.xyz, worldElementPivot, windQuat);
	worldNrm = QuaternionRotateVector(worldNrm, windQuat);
}

void AnimateVegetationSinglePivot(inout float3 worldPos, inout float3 worldNrm, float3 pivotData, float3 pivotColor, float timeNudge) {
	if(_WindEnabled == 0)
		return;

	uint3 packedData = asuint(pivotData);
	float3 pivotPos, pivotFwd;
	UnpackPivot0(packedData, pivotPos, pivotFwd);

	float3 worldElementPivot = mul(GetActualObject2World(), float4(pivotPos, 1)).xyz;
	float3 worldElementPivotFwd = mul((float3x3)GetActualObject2World(), pivotFwd);

	AnimateVegetationSinglePivot(worldPos, worldNrm, worldElementPivot, worldElementPivotFwd, pivotColor, timeNudge);
}

#endif//defined(USE_VEGETATION_ANIM) && (defined(_ANIM_SINGLE_PIVOT_COLOR) || defined(_ANIM_PROCEDURAL_BRANCH))

#if defined(USE_VEGETATION_ANIM) && defined(_ANIM_PROCEDURAL_BRANCH)
// Basic procedural branch sway (for tiny things): translate to single pivot
void AnimateVegetationProceduralBranch(inout float3 worldPos, inout float3 worldNrm, float3 objectRoot, float timeNudge) {
	float3 worldElementPivot = _WindFakeSingleObjectPivot ? objectRoot : float3(worldPos.x, objectRoot.y, worldPos.z);
	float3 worldElementPivotFwd = length(mul((float3x3)GetActualObject2World(), float3(1, 1, 1)) / 1.42f) * float3(0, 1, 0);
	float color = saturate((worldPos.y - objectRoot.y) * 5.f);

	AnimateVegetationSinglePivot(worldPos, worldNrm, worldElementPivot, worldElementPivotFwd, color.rrr, timeNudge);
}
#endif//defined(USE_VEGETATION_ANIM) && defined(_ANIM_PROCEDURAL_BRANCH)

#if defined(USE_VEGETATION_ANIM) && defined(_ANIM_SINGLE_PIVOT_COLOR)
	#define APPLY_VEGETATION_ANIM(worldPos, normalWorld, pivotData, pivotColor, objectRoot) { AnimateVegetationSinglePivot(worldPos, normalWorld, pivotData, pivotColor, 0); }
#elif defined(USE_VEGETATION_ANIM) && defined(_ANIM_HIERARCHY_PIVOT)
	#define APPLY_VEGETATION_ANIM(worldPos, normalWorld, pivotData, pivotColor, objectRoot) { AnimateVegetationHierarchyPivot(worldPos, normalWorld, pivotData, pivotColor, objectRoot, 0); }
#elif defined(USE_VEGETATION_ANIM) && defined(_ANIM_PROCEDURAL_BRANCH)
	#define APPLY_VEGETATION_ANIM(worldPos, normalWorld, pivotData, pivotColor, objectRoot)  { AnimateVegetationProceduralBranch(worldPos, normalWorld, objectRoot, 0); }
#else
	#define APPLY_VEGETATION_ANIM(worldPos, normalWorld, pivotData, pivotColor, objectRoot)
#endif

#if defined(USE_VEGETATION_ANIM) && defined(_ANIM_SINGLE_PIVOT_COLOR)
	#define APPLY_VEGETATION_ANIM_TIMENUDGE(worldPos, normalWorld, pivotData, pivotColor, objectRoot, timeNudge) { AnimateVegetationSinglePivot(worldPos, normalWorld, pivotData, pivotColor, timeNudge); }
#elif defined(USE_VEGETATION_ANIM) && defined(_ANIM_HIERARCHY_PIVOT)
	#define APPLY_VEGETATION_ANIM_TIMENUDGE(worldPos, normalWorld, pivotData, pivotColor, objectRoot, timeNudge) { AnimateVegetationHierarchyPivot(worldPos, normalWorld, pivotData, pivotColor, objectRoot, timeNudge); }
#elif defined(USE_VEGETATION_ANIM) && defined(_ANIM_PROCEDURAL_BRANCH)
	#define APPLY_VEGETATION_ANIM_TIMENUDGE(worldPos, normalWorld, pivotData, pivotColor, objectRoot, timeNudge)  { AnimateVegetationProceduralBranch(worldPos, normalWorld, objectRoot, timeNudge); }
#else
	#define APPLY_VEGETATION_ANIM_TIMENUDGE(worldPos, normalWorld, pivotData, pivotColor, objectRoot, timeNudge)
#endif

#endif //FILE_FOREST_VTXANIM_SHARED2
