using System;
using JetBrains.Annotations;
using Konsarpoo.Collections.Allocators;

namespace Konsarpoo.Collections
{
    /// <summary>
    /// Global setup of max Data subarray leaf size for all types.
    /// </summary>
    public static class KonsarpooAllocatorGlobalSetup
    {
        /// <summary>
        /// Max size of node for data storage leaf.
        /// </summary>
        public const int PoolMaxSizeOfArray = 1024 * 1024;
        
        internal static volatile int MaxSizeOfArray = PoolMaxSizeOfArray;

        /// <summary>
        /// Sets up default array pool behaviour.
        /// </summary>
        public static volatile bool ClearArrayOnReturn = true;
        
        /// <summary>
        /// Sets up default array pool behaviour.
        /// </summary>
        public static volatile bool ClearArrayOnRequest = true;

        private static volatile IDefaultAllocatorSetup s_defaultAllocatorSetup = new DefaultGcArrayPoolMixedAllocatorSetup();

        /// <summary> Sets up max subarray leaf size. 1048576 is the max value for now and 16 is min. </summary>
        /// <param name="val"></param>
        public static void SetMaxSizeOfArrayBucket(int val)
        {
            MaxSizeOfArray = Math.Min(PoolMaxSizeOfArray, Math.Max(16, 1 << (int)Math.Round(Math.Log(val, 2))));
        }

        public static IDefaultAllocatorSetup DefaultAllocatorSetup => s_defaultAllocatorSetup;

        public static void SetDefaultAllocatorSetup([NotNull] IDefaultAllocatorSetup setup)
        {
            s_defaultAllocatorSetup = setup ?? throw new ArgumentNullException(nameof(setup));
        }
        
        public static void SetMixedAllocatorSetup(int gcCount = 64, int? maxDataArrayLen = null)
        {
            s_defaultAllocatorSetup = new DefaultGcArrayPoolMixedAllocatorSetup(gcCount, maxDataArrayLen);
        }
        
        public static void SetGcAllocatorSetup(int? maxDataArrayLen = null)
        {
            s_defaultAllocatorSetup = new DefaultGcAllocatorSetup(maxDataArrayLen);
        }
        
        public static void SetArrayPoolAllocatorSetup(int? maxDataArrayLen = null)
        {
            s_defaultAllocatorSetup = new DefaultArrayPoolAllocatorSetup(maxDataArrayLen);
        }
    }
}