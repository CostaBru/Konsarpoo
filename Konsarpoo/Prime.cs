using System;
using System.Runtime.ConstrainedExecution;

namespace Konsarpoo.Collections
{
    internal static class Prime
    {
        private static readonly int[] s_primes =
        {
            2,
            31,
            61,
            127,
            509,
            1021,
            2039,
            4091,
            8191,
            131071,
            524287,
            2946901,
            5893807,
            11787631,
            23575267
        };


        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static int GetPrime(int min)
        {
            if (min < 0)
            {
                throw new ArgumentException();
            }

            var inx = Array.BinarySearch(s_primes, 0, s_primes.Length, min);

            if (inx < 0)
            {
                inx = ~inx;

                if (inx + 1 < s_primes.Length)
                {
                    return s_primes[inx + 1];
                }
            }
            else
            {
                return s_primes[inx];
            }
       
            for (int j = min | 1; j < 0x7fffffff; j += 2)
            {
                if (IsPrime(j))
                {
                    return j;
                }
            }
            return min;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static bool IsPrime(int candidate)
        {
            if ((candidate & 1) == 0)
            {
                return (candidate == 2);
            }

            int num = (int)Math.Sqrt(candidate);
            for (int i = 3; i <= num; i += 2)
            {
                if ((candidate % i) == 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}