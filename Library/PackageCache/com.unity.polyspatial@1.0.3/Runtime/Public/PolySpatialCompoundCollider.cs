using System.Collections.Generic;
using UnityEngine;

namespace Unity.PolySpatial.Internals
{
    /// <summary>
    /// This is primarily to hold references to Colliders in the Unity Backing Layer that represent a Compound Collider.
    /// </summary>
    [DisallowMultipleComponent]
    internal class PolySpatialCompoundCollider : MonoBehaviour
    {
        Dictionary<int, Collider> m_ColliderMap = new();

        public enum PolySpatialTrackingFlags : uint
        {
            /// <summary>
            ///  The tracked item has been created.
            /// </summary>
            Created = 1,
            /// <summary>
            ///  The tracked item has transitioned to the Running state.
            /// </summary>
            Running = 2,
            /// <summary>
            ///  The tracked item has been destroyed. And data pertaining to
            ///  this item should be cleaned up.
            /// </summary>
            Destroyed = 4,
            /// <summary>
            ///  The tracked item and data have been cleaned up. The tracking data is no
            ///  longer valid in this state.
            /// </summary>
            Deallocated = 8,
            /// <summary>
            ///  The tracked item data is dirty. This means that some change has occured
            ///  to the state of this tracked item. Any thing that depends on this data
            ///  must check the data for changes and update accordingly.
            /// </summary>
            Dirty = 16,
            /// <summary>
            ///  The tracked item is disabled. The tracked data is still valid, but the item
            ///  is no longer active.
            /// </summary>
            Disabled = 32,
        }

        public enum UnityColliderShape : int
        {
            Box = 0,
            Sphere = 1,
            Capsule = 2,
            Mesh = 3,
        }

        internal struct UnityColliderInfo
        {
            internal int colliderId;
            internal bool isTrigger;
            internal Vector3 size;
            internal Vector3 center;
            internal UnityColliderShape shape;
            internal Mesh sharedMesh;
            internal bool convex;
        }

        private static void DestroyAppropriately(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
                return;
            }
#endif
            Object.Destroy(obj);
        }

        internal unsafe void SetPolySpatialColliderData(PolySpatialTrackingFlags trackingFlags, UnityColliderInfo colliderInfo)
        {
            if (trackingFlags.HasFlag(PolySpatialTrackingFlags.Destroyed) &&
                TryGetBackingCollider(colliderInfo.colliderId, out var destroyedComponent))
            {
                m_ColliderMap.Remove(colliderInfo.colliderId);
                DestroyAppropriately(destroyedComponent);
                return;
            }

            if (trackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled) &&
                TryGetBackingCollider(colliderInfo.colliderId, out var disabledComponent))
            {
                disabledComponent.enabled = false;
                return;
            }

            switch (colliderInfo.shape)
            {
                case UnityColliderShape.Box:
                    var boxCollider = GetOrAddBackingCollider<BoxCollider>(colliderInfo.colliderId);
                    boxCollider.isTrigger = colliderInfo.isTrigger;
                    boxCollider.size = colliderInfo.size;
                    boxCollider.center = colliderInfo.center;
                    break;
                case UnityColliderShape.Sphere:
                    var sphereCollider = GetOrAddBackingCollider<SphereCollider>(colliderInfo.colliderId);
                    sphereCollider.isTrigger = colliderInfo.isTrigger;
                    sphereCollider.radius = colliderInfo.size.x;
                    sphereCollider.center = colliderInfo.center;
                    break;
                case UnityColliderShape.Capsule:
                    var capsuleCollider = GetOrAddBackingCollider<CapsuleCollider>(colliderInfo.colliderId);
                    capsuleCollider.isTrigger = colliderInfo.isTrigger;
                    capsuleCollider.radius = colliderInfo.size.x;
                    capsuleCollider.height = colliderInfo.size.y;
                    capsuleCollider.center = colliderInfo.center;
                    break;
                case UnityColliderShape.Mesh:
                    var meshCollider = GetOrAddBackingCollider<MeshCollider>(colliderInfo.colliderId);
                    // The setters for both isTrigger and convex report an error if (isTrigger && !convex).
                    // Avoid that error if we are setting both at once to a valid state.
                    if (!colliderInfo.convex && meshCollider.isTrigger)
                        meshCollider.isTrigger = false;
                    meshCollider.convex = colliderInfo.convex;
                    meshCollider.isTrigger = colliderInfo.isTrigger;
                    meshCollider.sharedMesh = colliderInfo.sharedMesh;
                    break;
                default:
                    Debug.Log($"ColliderShape {colliderInfo.shape} not implemented.");
                    break;
            }
        }

        bool TryGetBackingCollider(int id, out Collider foundCollider)
        {
            if (m_ColliderMap.TryGetValue(id, out var foundComponent))
            {
                foundComponent.enabled = true;
                foundCollider = foundComponent;
                return true;
            }

            foundCollider = null;
            return false;
        }

        T GetOrAddBackingCollider<T>(int id) where T : Collider
        {
            if (m_ColliderMap.TryGetValue(id, out var foundComponent))
            {
                foundComponent.enabled = true;
                return foundComponent as T;
            }

            var component = gameObject.AddComponent<T>();
            //gameObject.MarkAsBackingComponent(component);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                component.hideFlags = HideFlags.DontSave | HideFlags.NotEditable | HideFlags.HideInInspector;
            }
#endif
            m_ColliderMap.Add(id, component);
            return component;
        }
    }
}
