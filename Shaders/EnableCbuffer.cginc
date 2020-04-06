//
// EnableCbuffer.cginc
//
// Projector For LWRP
//
// Copyright 2020 NYAHOON GAMES PTE. LTD. All Rights Reserved.
//

#if !defined(P4LWRP_ENABLECBUFFER_CGINC_DEFINED)
#define P4LWRP_ENABLECBUFFER_CGINC_DEFINED

// enable CBUFFER macros for SRP Batcher
#if 201930 <= UNITY_VERSION

#define UNITY_ENABLE_CBUFFER

#elif 201820 <= UNITY_VERSION

#include "HLSLSupport.cginc"
#if !(defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL))
#undef CBUFFER_START
#undef CBUFFER_END
#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };
#endif

#endif

#endif // !defined(P4LWRP_ENABLECBUFFER_CGINC_DEFINED)
