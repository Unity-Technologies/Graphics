#ifndef VECTOR_LOGIC
#define VECTOR_LOGIC

#if defined(XBOX)
#define USE_HLSL2021_VECTOR_LOGIC_INTRINSICS
#endif

#if defined(USE_HLSL2021_VECTOR_LOGIC_INTRINSICS)

#define VECTOR_LOGIC_AND(x, y) and(x, y)
#define VECTOR_LOGIC_OR(x, y) or(x, y)
#define VECTOR_LOGIC_SELECT(condition, trueValue, falseValue) select(condition, trueValue, falseValue)

#else

#define VECTOR_LOGIC_AND(x, y) ((x) && (y))
#define VECTOR_LOGIC_OR(x, y) ((x) || (y))
#define VECTOR_LOGIC_SELECT(condition, trueValue, falseValue) ((condition) ? (trueValue) : (falseValue))

#endif
#undef USE_HLSL2021_VECTOR_LOGIC_INTRINSICS

#endif

