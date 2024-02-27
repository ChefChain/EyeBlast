using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.VisionOS
{
    struct VisionOS_UUID
    {
        public ulong low;
        public ulong high;

        public VisionOS_UUID(ulong low, ulong high)
        {
            this.low = low;
            this.high = high;
        }

        public override string ToString()
        {
            return $"{low:x}-{high:x}";
        }
    }

    struct FloatArrayToMatrix4x4
    {
        unsafe fixed float values[16];

        public unsafe FloatArrayToMatrix4x4(Matrix4x4 matrix)
        {
            values[0] = matrix.m00;
            values[1] = matrix.m10;
            values[2] = matrix.m20;
            values[3] = matrix.m30;
            values[4] = matrix.m01;
            values[5] = matrix.m11;
            values[6] = matrix.m21;
            values[7] = matrix.m31;
            values[8] = matrix.m02;
            values[9] = matrix.m12;
            values[10] = matrix.m22;
            values[11] = matrix.m32;
            values[12] = matrix.m03;
            values[13] = matrix.m13;
            values[14] = matrix.m23;
            values[15] = matrix.m33;
        }

        public unsafe Matrix4x4 ToMatrix4x4()
        {
            // TODO: fast in-place conversion
            return new Matrix4x4(
                new Vector4(values[0], values[1], values[2], values[3]),
                new Vector4(values[4], values[5], values[6], values[7]),
                new Vector4(values[8], values[9], values[10], values[11]),
                new Vector4(values[12], values[13], values[14], values[15]));
        }

        public Vector3 GetPosition()
        {
            var matrix = ToMatrix4x4();
            var position = matrix.GetPosition();
            position.z *= -1;
            return position;
        }

        public Quaternion GetRotation()
        {
            var matrix = ToMatrix4x4();
            var rotation = matrix.rotation;
            rotation.z *= -1;
            rotation.w *= -1;
            return rotation;
        }

        public override string ToString()
        {
            return ToMatrix4x4().ToString();
        }
    }

    static class NativeApi_Types
    {
        /// <summary>
        /// Convert simd_float4x4 to an array of floats for marshalling
        /// </summary>
        /// <param name="matrix">Pointer to a simd_float4x4</param>
        /// <returns>Pointer to an array of floats which can be used to convert to Matrix4x4</returns>
        [DllImport(NativeApi.Constants.LibraryName, EntryPoint = "UnityVisionOS_impl_simd_float4x4_to_float_array")]
        public static extern IntPtr UnityVisionOS_impl_simd_float4x4_to_float_array(IntPtr matrix);
    }
}
