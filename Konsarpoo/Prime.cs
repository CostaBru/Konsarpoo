using System;
using System.Runtime.ConstrainedExecution;

namespace Konsarpoo.Collections
{
    internal static class Prime
    {
        private static readonly int[] s_primes =
        {
            Data<int>.SmallListCount,
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
            2946901
        };


        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static int GetPrime(int min)
        {
            if (min < 0)
            {
                throw new ArgumentException();
            }
            for (int i = 0; i < s_primes.Length; i++)
            {
                int prime = s_primes[i];
                if (prime >= min)
                {
                    return prime;
                }
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