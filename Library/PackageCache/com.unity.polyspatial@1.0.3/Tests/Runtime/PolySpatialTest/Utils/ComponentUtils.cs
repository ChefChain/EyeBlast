using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Tests.Runtime.Functional;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEditor;
using UnityEngine;

namespace Tests.Runtime.PolySpatialTest.Utils
{
    /// <summary>
    /// A collection of helper methods and values to operate on backing component features, such as retrieving and
    /// destroying backing components.
    /// Note: there are dependencies on the UnitySceneGraph, which is part of the UnityPlayerPlatform. As such,
    /// current support is limited to UnityPlayerPlatform.
    /// </summary>
    internal static class BackingComponentUtils
    {
        // TODO LXR-414 - revert to previous regex "^PolySpatial iid -[0-9]+" when fixed
        public const string BackingGameObjectNameRegex = @"^PolySpatial iid .*";

        /// <summary>
        /// Returns a UnitySceneGraph instance from the current backend if found and supported.
        /// </summary>
        /// <returns>UnitySceneGraph instance</returns>
        private static UnitySceneGraph GetUnitySceneGraph()
        {
            if (PolySpatialCore.LocalBackend is PolySpatialUnityBackend u)
                return u.SceneGraph;
            return null;
        }

        /// <summary>
        /// Fetches a backing GameObject for the given simulation GameObject
        /// </summary>
        /// <param name="simulationInstanceId">Simulation GameObject instance ID</param>
        /// <returns>Backing GameObject if found, null otherwise</returns>
        public static GameObject GetBackingGameObjectFor(PolySpatialInstanceID simulationInstanceId)
        {
            return (GetUnitySceneGraph())?.FindBackingGameObjectForId(simulationInstanceId);
        }

        /// <summary>
        /// Fetches all PolySpatial backing GameObjects from backing Scene
        /// </summary>
        /// <returns>List of found backing GameObjects</returns>
        public static List<GameObject> GetBackingGameObjects()
        {

            return (GetUnitySceneGraph()) == null ? default : GetUnitySceneGraph().GetSimIDToScenegraphGOs().Values.ToList();
        }

        /// <summary>
        /// Calls destroy on a PolySpatial backing GameObject via the expected PolySpatial SceneGraph API
        /// Note: destroying simulation GameObjects will also call destroy on the backing GameObject automatically.
        /// </summary>
        /// <param name="simulationInstanceId">Simulation GameObject instance ID</param>
        public static void DestroyBackingGameObject(PolySpatialInstanceID simulationInstanceId)
        {
            GetUnitySceneGraph()?.DestroyBackingGameObjectForId(simulationInstanceId);
        }
    }

    /// <summary>
    /// A collection of helper methods and values for accessing PolySpatialComponent data and attributes
    /// </summary>
    internal static class PolySpatialComponentUtils
    {
        internal static TrackingData<PolySpatialMeshRendererTrackingData> GetTrackingData(MeshRenderer mr)
        {
            return GetMeshRendererTrackingData(mr.GetInstanceID());
        }

        internal static TrackingData<PolySpatialSkinnedMeshRendererTrackingData> GetTrackingData(SkinnedMeshRenderer sMr)
        {
            return GetSkinnedMeshRendererTrackingData(sMr.GetInstanceID());
        }

        internal static TrackingData<PolySpatialLightTrackingData> GetTrackingData(Light l)
        {
            return GetLightTrackingData(l.GetInstanceID());
        }

        internal static TrackingData<PolySpatialCanvasRendererTrackingData> GetTrackingData(CanvasRenderer cr)
        {
            return GetCanvasRendererTrackingData(cr.GetInstanceID());
        }

        internal static TrackingData<PolySpatialColliderTrackingData> GetTrackingData(Collider c)
        {
            return GetColliderTrackingData(c.GetInstanceID());
        }

        internal static TrackingData<PolySpatialSpriteRendererTrackingData> GetTrackingData(SpriteRenderer sr)
        {
            return GetSpriteRendererData(sr.GetInstanceID());
        }

        internal static TrackingData<PolySpatialSpriteMaskTrackingData> GetTrackingData(SpriteMask sm)
        {
            return GetSpriteMaskData(sm.GetInstanceID());
        }

        internal static TrackingData<PolySpatialParticleRendererTrackingData> GetTrackingData(ParticleSystem ps)
        {
            return GetParticleSystemData(ps.GetInstanceID());
        }

        internal static DefaultTrackingData GetTrackingData(VisionOSVideoComponent player)
        {
            return GetVideoPlayerData(player.GetInstanceID());
        }

        internal static TrackingData<PolySpatialPlatformTextComponentTrackingData> GetTrackingData(VisionOSNativeText text)
        {
            return GetTextData(text.GetInstanceID());
        }

        internal static DefaultTrackingData GetTrackingData(UnityEngine.Object qvc)
        {
            return GetPolySpatialVolumeCameraTrackingData(qvc.GetInstanceID());
        }

        internal static TrackingData<PolySpatialMeshRendererTrackingData> GetMeshRendererTrackingData(int iid)
        {
            return MeshRendererTracker.GetTrackingData(iid);
        }

        internal static TrackingData<PolySpatialSkinnedMeshRendererTrackingData> GetSkinnedMeshRendererTrackingData(int iid)
        {
            return SkinnedMeshRendererTracker.GetTrackingData(iid);
        }

        internal static TrackingData<PolySpatialLightTrackingData> GetLightTrackingData(int iid)
        {
            return LightTracker.GetTrackingData(iid);
        }

        internal static TrackingData<PolySpatialCanvasRendererTrackingData> GetCanvasRendererTrackingData(int iid)
        {
            return CanvasRendererTracker.GetTrackingData(iid);
        }

        internal static TrackingData<PolySpatialColliderTrackingData> GetColliderTrackingData(int iid)
        {
            return ColliderTracker.GetTrackingData(iid);
        }

        internal static TrackingData<PolySpatialSpriteRendererTrackingData> GetSpriteRendererData(int iid)
        {
            return SpriteRendererTracker.GetTrackingData(iid);
        }

        internal static TrackingData<PolySpatialSpriteMaskTrackingData> GetSpriteMaskData(int iid)
        {
            return SpriteMaskTracker.GetTrackingData(iid);
        }

        internal static TrackingData<PolySpatialParticleRendererTrackingData> GetParticleSystemData(int iid)
        {
            return ParticleSystemTracker.GetTrackingData(iid);
        }

        internal static DefaultTrackingData GetVideoPlayerData(int iid)
        {
            return VideoPlayerTracker.GetTrackingData(iid);
        }

        internal static TrackingData<PolySpatialPlatformTextComponentTrackingData> GetTextData(int iid)
        {
            return VisionOSNativeTextTracker.GetTrackingData(iid);
        }

        internal static DefaultTrackingData GetPolySpatialVolumeCameraTrackingData(int iid)
        {
            return PolySpatialVolumeCameraTracker.GetTrackingData(iid);
        }

        internal static void ValidateTrackingData<TTrackingData>(TTrackingData data, PolySpatialTrackingFlags expectFlag) where TTrackingData : ITrackingData
        {
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(expectFlag, data.GetLifecycleStage());
        }

        internal static Material CreateMaterial(Shader shader, Color color, string textureName = null, string texturePropertyName = null)
        {
            var material = new Material(shader);
            if (!String.IsNullOrEmpty(textureName))
            {
                var tex = Resources.Load(textureName) as Texture2D;
                if (texturePropertyName == null)
                    material.mainTexture = tex;
                else
                    material.SetTexture(texturePropertyName, tex);
            }
            material.color = color;
            return material;
        }

        internal static Material CreateUnlitMaterial(Color color, string textureName = null)
        {
            // this shader needs to be compiled in.  We have a material in resources that uses it.
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            return CreateMaterial(shader, color, textureName, null);
        }

        internal static Material CreateUnlitParticleURPMaterial(Color color, string textureName = null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            return CreateMaterial(shader, color, textureName, null);
        }

        internal static Material CreateUnlitParticleBIRPMaterial(Color color, string textureName = null)
        {
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                return null;

            return CreateMaterial(shader, color, textureName, "_MainTex");
        }

        internal static Material CreateLitParticleBIRPMaterial(Color color, string textureName = null)
        {
            var shader = Shader.Find("Particles/Standard Surface");
            if (shader == null)
                return null;

            return CreateMaterial(shader, color, textureName, "_MainTex");
        }

        internal static Material CreateLitParticleURPMaterial(Color color, string textureName = null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Lit");
            if (shader == null)
                return null;

            return CreateMaterial(shader, color, textureName, null);
        }
    }
}
