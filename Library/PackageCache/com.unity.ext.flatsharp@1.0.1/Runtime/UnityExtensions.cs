using System;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class NativeArrayUnsafeUtilityEx
    {
        public static NativeArray<T> ConvertExistingDataToNativeArray<T>(Span<T> span, Allocator allocator) where T : struct
        {
            var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                UnsafeUtility.AddressOf(ref span.GetPinnableReference()),
                span.Length,
                allocator);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif
            return nativeArray;
        }
    }
}
