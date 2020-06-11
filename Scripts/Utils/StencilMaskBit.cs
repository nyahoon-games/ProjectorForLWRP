//
// StencilMaskBit.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//
namespace ProjectorForLWRP
{
	// Each Projector or ShadowBuffer needs only a single bit.
	// this enum should not be used as Flags.
	public enum StencilMaskBit : int
	{
		Bit0 = 1 << 0,
		Bit1 = 1 << 1,
		Bit2 = 1 << 2,
		Bit3 = 1 << 3,
		Bit4 = 1 << 4,
		Bit5 = 1 << 5,
		Bit6 = 1 << 6,
		Bit7 = 1 << 7
	}
}
