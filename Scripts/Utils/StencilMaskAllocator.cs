//
// StencilMaskAllocator.cs
//
// Projector For LWRP
//
// Copyright (c) 2020 NYAHOON GAMES PTE. LTD.
//

namespace ProjectorForLWRP
{
	public static class StencilMaskAllocator
	{
		const int STENCIL_BIT_COUNT = 8;
		private static int s_availableBits = 0xFF;
		private static int s_allocateCount = 0;
		public static void Init(int mask)
		{
			s_availableBits = mask;
			s_allocateCount = 0;
			MoveNext();
		}
		public static int AllocateSingleBit()
		{
			if (s_allocateCount < STENCIL_BIT_COUNT)
			{
				int bit = 1 << s_allocateCount++;
				MoveNext();
				return bit;
			}
			return 0;
		}
		public static int GetTemporaryBit()
		{
			if (s_allocateCount < STENCIL_BIT_COUNT)
			{
				return 1 << s_allocateCount;
			}
			return 0;
		}
		private static void MoveNext()
		{
			while ((s_availableBits & (1 << s_allocateCount)) == 0 && s_allocateCount < STENCIL_BIT_COUNT)
			{
				++s_allocateCount;
			}
		}
	}
}
