// Upgrade NOTE: replaced 'defined in' with 'defined (in)'

#define REQUIRE_DEFINED(X_) \
	#ifndef X_  \
		#error X_ must be defined (in) the platform include \
	#endif X_  \

REQUIRE_DEFINED(UNITY_UV_STARTS_AT_TOP)