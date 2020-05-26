using System.Collections.Generic;

namespace SkiesLogFlagChanges
{
    public static class BitUtils
    {

        // CLZ implementation from https://stackoverflow.com/a/10439333
        public static int CountLeadingZeros(int x)
        {
            const int numIntBits = sizeof(int) * 8; //compile time constant
                                                    //do the smearing
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            //count the ones
            x -= x >> 1 & 0x55555555;
            x = (x >> 2 & 0x33333333) + (x & 0x33333333);
            x = (x >> 4) + x & 0x0f0f0f0f;
            x += x >> 8;
            x += x >> 16;
            return numIntBits - (x & 0x0000003f); //subtract # of 1s from 32
        }

        // Get the list of bits that are set in the binary representation of a
        // number.  LSB is written as 0.
        public static List<int> GetListOfBits(int x)
        {
            const int numIntBits = sizeof(int) * 8;
            int y = x;
            var result = new List<int>();

            while(y != 0)
            {
                int nextBit = numIntBits - CountLeadingZeros(y) - 1;
                result.Add(nextBit);

                y &= ~(1 << nextBit);
            }

            return result;
        }
    }
}
