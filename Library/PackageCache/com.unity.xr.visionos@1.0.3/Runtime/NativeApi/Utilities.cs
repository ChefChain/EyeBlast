using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.VisionOS
{
    static partial class NativeApi
    {
        internal static class Utilities
        {
            static readonly IntPtr k_UuidPointer;
            static readonly IntPtr k_FloatArrayPointer;

            static Utilities()
            {
                k_UuidPointer = Marshal.AllocHGlobal(Marshal.SizeOf<VisionOS_UUID>());
                k_FloatArrayPointer = Marshal.AllocHGlobal(sizeof(float) * 16);
            }

            public static TrackableId GetTrackableId(IntPtr anchor)
            {
                Anchor.ar_anchor_get_identifier(anchor, k_UuidPointer);
                var uuid = Marshal.PtrToStructure<VisionOS_UUID>(k_UuidPointer);
                return new TrackableId(uuid.low, uuid.high);
            }

            public static IntPtr TrackableIdToPtr(TrackableId trackableId)
            {
                var uuid = new VisionOS_UUID(trackableId.subId1, trackableId.subId2);
                Marshal.StructureToPtr(uuid, k_UuidPointer, true);
                return k_UuidPointer;
            }

            public static Pose GetWorldPose(IntPtr anchor)
            {
                // TODO: For some reason this method was just returning the same pointer you gave it, so it needed to be wrapped in ObjC
                var transformFloatArray =
                    Anchor.UnityVisionOS_impl_ar_anchor_get_origin_from_anchor_transform_to_float_array(
                        anchor);
                var worldMatrix = Marshal.PtrToStructure<FloatArrayToMatrix4x4>(transformFloatArray);
                return new Pose(worldMatrix.GetPosition(), worldMatrix.GetRotation());
            }

            public static IntPtr GetMatrixFloats(Pose pose)
            {
                // Convert pose to ARKit coordinate space
                var position = pose.position;
                position.z *= -1;
                var rotation = pose.rotation;
                rotation.z *= -1;
                rotation.w *= -1;

                var matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
                var float2matrix = new FloatArrayToMatrix4x4(matrix);
                Marshal.StructureToPtr(float2matrix, k_FloatArrayPointer, true);
                return k_FloatArrayPointer;
            }

            public static void DictionaryToNativeArray<T>(Dictionary<TrackableId, T> dictionary,
                ref NativeArray<T> array) where T : struct
            {
                var count = dictionary.Count;
                if (array.Length < count)
                {
                    array.Dispose();
                    array = new NativeArray<T>(count, Allocator.Persistent);
                }

                count = 0;
                foreach (var kvp in dictionary)
                {
                    array[count++] = kvp.Value;
                }
            }

            public static void HashSetToNativeArray<T>(HashSet<T> hashSet, ref NativeArray<T> array) where T : struct
            {
                var count = hashSet.Count;
                if (array.Length < count)
                {
                    array.Dispose();
                    array = new NativeArray<T>(count, Allocator.Persistent);
                }

                count = 0;
                foreach (var element in hashSet)
                {
                    array[count++] = element;
                }
            }
        }
    }
}
