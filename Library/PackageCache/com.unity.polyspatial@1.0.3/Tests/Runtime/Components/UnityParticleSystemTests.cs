using System;
using System.Collections;
using NUnit.Framework;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.Collections;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Runtime.Functional.Components
{
    [TestFixture]
    public class UnityParticleSystemTests : ComponentTestBase
    {
        GameObject m_TestGameObject;
        GameObject m_SubEmitterObject;
        ParticleSystem m_ParticleSystem;
        ParticleSystem m_SubEmitterParticleSystems;

        const string k_TestMaterialName = "ParticleTestMaterial";
        const string k_TestMeshName = "ParticleTestMesh";
        const string k_TestParticleTextureName = "Texture2DBlue";

        PolySpatialSettings.ParticleReplicationMode m_previousParticleMode;

        // This test is designed to test ReplicateProperties; always entre that mode before running the test and
        // restore the prior state when exiting.
        internal override void InternalSetup()
        {
            m_previousParticleMode = PolySpatialSettings.instance.ParticleMode;
            PolySpatialSettings.instance.ParticleMode = PolySpatialSettings.ParticleReplicationMode.ReplicateProperties;
            base.InternalSetup();
        }

        GameObject GetBackingGameObjectForParticleSystem(ParticleSystem ps)
        {
            if (m_ParticleSystem == null)
                return null;

            if (PolySpatialSettings.instance.ParticleMode == PolySpatialSettings.ParticleReplicationMode.ReplicateProperties)
                return BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_ParticleSystem.gameObject));

            return BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_ParticleSystem));
        }

        private void CreateTestParticleSystem(Material material)
        {
            m_TestGameObject = new GameObject("Test object main particle system");
            m_SubEmitterObject = new GameObject("Test object subemitter particle system");
            m_SubEmitterObject.transform.SetParent(m_TestGameObject.transform);
            m_ParticleSystem = m_TestGameObject.AddComponent<ParticleSystem>();
            m_SubEmitterParticleSystems = m_SubEmitterObject.AddComponent<ParticleSystem>();
            var particleRenderer = m_TestGameObject.GetComponent<ParticleSystemRenderer>();

            m_ParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = m_ParticleSystem.main;
            var emission = m_ParticleSystem.emission;
            var shape = m_ParticleSystem.shape;
            var velocityOverTime = m_ParticleSystem.velocityOverLifetime;
            var limitVelocityOverTime = m_ParticleSystem.limitVelocityOverLifetime;
            var inheritVelocity = m_ParticleSystem.inheritVelocity;
            var forceOverTime = m_ParticleSystem.forceOverLifetime;
            var colorOverTime = m_ParticleSystem.colorOverLifetime;
            var sizeOverTime = m_ParticleSystem.sizeOverLifetime;
            var rotationOverTime = m_ParticleSystem.rotationOverLifetime;
            var noise = m_ParticleSystem.noise;
            var collision = m_ParticleSystem.collision;
            var subEmitters = m_ParticleSystem.subEmitters;
            var textureSheetAnimations = m_ParticleSystem.textureSheetAnimation;

            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3, 5);
            main.startSize = new ParticleSystem.MinMaxCurve(2, 3);
            main.startRotation3D = false;
            main.startRotation = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 0, 1, 1));
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(255f, 0, 100.75f, 180.5f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.playOnAwake = true;
            main.maxParticles = 1000;

            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(10);

            shape.enabled = true;
            var mesh = new Mesh();
            // a mesh must have some vertices and at least 1 submesh in order to be sent
            mesh.vertices = new[] { Vector3.up, Vector3.left, Vector3.down };
            mesh.SetIndices(new[] { 0, 1, 2}, MeshTopology.Triangles, 0);
            mesh.name = k_TestMeshName;

            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.mesh = mesh;
            shape.scale = new Vector3(2, 5, 3.5f);

            velocityOverTime.enabled = true;
            velocityOverTime.x = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0.75f, 1, 1, 0));
            velocityOverTime.y = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Constant(0, 0.5f, 5));
            velocityOverTime.z = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 0, 1, 1));
            velocityOverTime.orbitalX = new ParticleSystem.MinMaxCurve(1);
            velocityOverTime.orbitalY = new ParticleSystem.MinMaxCurve(5.25f);
            velocityOverTime.orbitalZ = new ParticleSystem.MinMaxCurve(8.5f);
            velocityOverTime.orbitalOffsetX = new ParticleSystem.MinMaxCurve(1, 2);
            velocityOverTime.orbitalOffsetY = new ParticleSystem.MinMaxCurve(5, 2.5f);
            velocityOverTime.orbitalOffsetZ = new ParticleSystem.MinMaxCurve(9.25f, 0.75f);
            velocityOverTime.radial = new ParticleSystem.MinMaxCurve(
                1,
                AnimationCurve.Constant(0, 0.5f, 5),
                AnimationCurve.EaseInOut(0, 0, 1, 1));
            velocityOverTime.space = ParticleSystemSimulationSpace.World;

            limitVelocityOverTime.enabled = true;
            limitVelocityOverTime.drag = new ParticleSystem.MinMaxCurve(5);
            limitVelocityOverTime.multiplyDragByParticleVelocity = true;
            limitVelocityOverTime.multiplyDragByParticleSize = false;

            inheritVelocity.enabled = true;
            inheritVelocity.curve = new ParticleSystem.MinMaxCurve(10, -6);

            forceOverTime.enabled = true;
            forceOverTime.x = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 0, 1, 1));
            forceOverTime.y = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 1, 0));
            forceOverTime.z = new ParticleSystem.MinMaxCurve(1,
                new AnimationCurve(
                    new Keyframe(0, 3),
                    new Keyframe(0.5f, 2),
                    new Keyframe(1, 5)));

            colorOverTime.enabled = true;

            var colorOverTimeGradient = new Gradient();
            colorOverTimeGradient.mode = GradientMode.Blend;
            colorOverTimeGradient.alphaKeys = new GradientAlphaKey[4]
            {
                new(0, 0),
                new(255, 3.5f),
                new(180, 89.3f),
                new(0, 100),
            };

            colorOverTimeGradient.colorKeys = new GradientColorKey[3]
            {
                new(Color.black, 0),
                new(Color.cyan, 4.5f),
                new(Color.black, 100),
            };

            colorOverTime.color = colorOverTimeGradient;

            sizeOverTime.enabled = true;
            sizeOverTime.separateAxes = true;
            sizeOverTime.x = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(1, 1, 0, 0));
            sizeOverTime.y = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0.25f, 1, 1, 0));
            sizeOverTime.z = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Constant(0, 1, 0.5f));

            rotationOverTime.enabled = true;
            rotationOverTime.x = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0.25f, 0, 1, 0));
            rotationOverTime.y = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0.5f, 0.25f, 1, 0.75f));
            rotationOverTime.z = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0f, 0, 1, 1));

            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(1, -1);
            noise.positionAmount = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Constant(5, 0, 1));
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(1,
                new AnimationCurve(
                    new Keyframe(0, 1),
                    new Keyframe(0.5f, 2),
                    new Keyframe(0.75f, 5)));

            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.bounce = new ParticleSystem.MinMaxCurve(5);
            collision.dampen = new ParticleSystem.MinMaxCurve(5, -50);

            subEmitters.enabled = true;
            subEmitters.AddSubEmitter(
                m_SubEmitterParticleSystems,
                ParticleSystemSubEmitterType.Death,
                ParticleSystemSubEmitterProperties.InheritLifetime & ParticleSystemSubEmitterProperties.InheritDuration);

            textureSheetAnimations.enabled = true;
            textureSheetAnimations.numTilesX = 6;
            textureSheetAnimations.numTilesY = 6;
            textureSheetAnimations.animation = ParticleSystemAnimationType.WholeSheet;
            textureSheetAnimations.startFrame = new ParticleSystem.MinMaxCurve(1);
            textureSheetAnimations.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
            textureSheetAnimations.frameOverTime = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 1));
            textureSheetAnimations.cycleCount = 6;

            particleRenderer.material = material;
            particleRenderer.material.name = k_TestMaterialName;
            particleRenderer.sortMode = ParticleSystemSortMode.Depth;
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;

            m_ParticleSystem.Play();
        }

        void AssertPolySpatialCurvesEqual(
            ParticleSystem.MinMaxCurve sourceCurve,
            ParticleSystem.MinMaxCurve polyspatialCurve)
        {
            Assert.AreEqual(polyspatialCurve.mode, sourceCurve.mode);
            switch (polyspatialCurve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    Assert.AreEqual(sourceCurve.constant, polyspatialCurve.constant);
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    Assert.AreEqual(sourceCurve.constantMin, polyspatialCurve.constantMin);
                    Assert.AreEqual(sourceCurve.constantMax, polyspatialCurve.constantMax);
                    break;
                case ParticleSystemCurveMode.Curve:
                    for (var i  = 0; i < sourceCurve.curve.length; i++)
                    {
                        Assert.AreEqual(sourceCurve.curve.keys[i], polyspatialCurve.curve.keys[i]);
                    }
                    break;
                case ParticleSystemCurveMode.TwoCurves:
                    for (var i  = 0; i < sourceCurve.curveMin.length; i++)
                    {
                        Assert.AreEqual(sourceCurve.curveMin.keys[i], polyspatialCurve.curveMin.keys[i]);
                    }

                    for (var i  = 0; i < sourceCurve.curveMax.length; i++)
                    {
                        Assert.AreEqual(sourceCurve.curveMax.keys[i], polyspatialCurve.curveMax.keys[i]);
                    }
                    break;
            }
        }

        void AssertParticleGradientsEqual(
            ParticleSystem.MinMaxGradient sourceGradient,
            ParticleSystem.MinMaxGradient polyspatialGradient)
        {
            Assert.AreEqual(sourceGradient.mode, polyspatialGradient.mode);
            switch (sourceGradient.mode)
            {
                case ParticleSystemGradientMode.Color:
                    Assert.AreEqual(sourceGradient.color, polyspatialGradient.color);
                    break;
                case ParticleSystemGradientMode.Gradient:
                    Assert.AreEqual(sourceGradient.gradient, polyspatialGradient.gradient);
                    break;
                case ParticleSystemGradientMode.RandomColor:
                    Assert.AreEqual(sourceGradient.gradient, polyspatialGradient.gradient);
                    break;
                case ParticleSystemGradientMode.TwoColors:
                    Assert.AreEqual(sourceGradient.colorMin, polyspatialGradient.colorMin);
                    Assert.AreEqual(sourceGradient.colorMax, polyspatialGradient.colorMax);
                    break;
                case ParticleSystemGradientMode.TwoGradients:
                    Assert.AreEqual(sourceGradient.gradientMin, polyspatialGradient.gradientMin);
                    Assert.AreEqual(sourceGradient.gradientMax, polyspatialGradient.gradientMax);
                    break;
            }
        }

        internal override IEnumerator InternalUnityTearDown()
        {
            var go = GetBackingGameObjectForParticleSystem(m_ParticleSystem);

            if (m_SubEmitterParticleSystems != null)
                Object.Destroy(m_SubEmitterParticleSystems);
            if (m_SubEmitterObject != null)
                Object.Destroy(m_SubEmitterObject);
            if (m_ParticleSystem != null)
                Object.Destroy(m_ParticleSystem);
            if (m_TestGameObject != null)
                Object.Destroy(m_TestGameObject);

            yield return base.InternalUnityTearDown();
            yield return null;
            Assert.IsTrue(go == null);
            PolySpatialSettings.instance.ParticleMode = m_previousParticleMode;
        }

        [UnityTest]
        public IEnumerator Test_UnityParticleSystem_Create_Destroy_Tracking()
        {
            var material = PolySpatialComponentUtils.CreateUnlitParticleURPMaterial(Color.blue);
            CreateTestParticleSystem(material);

            // Let a frame be processed and trigger the above assertions
            yield return null;

            // right after the frame, we should no longer be dirty
            var data = PolySpatialComponentUtils.GetTrackingData(m_ParticleSystem);
            Assert.IsTrue(data.ValidateTrackingFlags());
            PolySpatialComponentUtils.ValidateTrackingData(data, PolySpatialTrackingFlags.Running);

            var criid = m_ParticleSystem.GetInstanceID();

            // destroy the component
            Object.Destroy(m_ParticleSystem);

            yield return null;

#if UNITY_EDITOR
            // Check to see if data stays deleted. We no longer clear the dirty flag, as we will not touch this data any more.
            var deletedData = PolySpatialComponentUtils.GetParticleSystemData(criid);
            Assert.IsTrue(deletedData.ValidateTrackingFlags());
            PolySpatialComponentUtils.ValidateTrackingData(deletedData, PolySpatialTrackingFlags.Destroyed);
#endif

            yield return null;

            // After another frame, the tracking data is gone, and GetParticleSystemData throws.
            Assert.Throws<InvalidOperationException>(() => PolySpatialComponentUtils.GetParticleSystemData(criid));
        }

        [UnityTest]
        public IEnumerator Test_UnityParticleSystem_Check_PolySpatialParticlePlayState()
        {
            var material = PolySpatialComponentUtils.CreateUnlitParticleURPMaterial(Color.blue);
            CreateTestParticleSystem(material);
            m_ParticleSystem.Play();
            Assert.IsTrue(m_ParticleSystem.isPlaying);

            yield return null;

            var backingGO = GetBackingGameObjectForParticleSystem(m_ParticleSystem);
            if (backingGO != null)
            {
                var main = m_ParticleSystem.main;

                var backingParticleSystem = backingGO.GetComponent<ParticleSystem>();
                Assert.IsTrue(backingParticleSystem.isPlaying, "System was expected to be playing.");

                // Right now, there is a bug where simply calling Pause() or Stop() does not trigger change tracking.
                // To force change tracking, one of the variables exposed in ParticleSystem inspector must be changed.
                // This bug is tracked in LXR-1458.
                // Check if system is paused after playing.
                m_ParticleSystem.Pause();
                main.playOnAwake = !main.playOnAwake;

                yield return null;

                Assert.IsTrue(backingParticleSystem.isPaused, "System was expected to be paused.");

                // Check if the system has stopped emitting after a pause.
                m_ParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                main.playOnAwake = !main.playOnAwake;

                yield return null;

                Assert.IsFalse(backingParticleSystem.isEmitting, "System was expected to have stopped emitting.");
                Assert.IsFalse(backingParticleSystem.isPaused, "System was expected to not be paused, since StopEmitting was called.");

                // Restart system up again.
                m_ParticleSystem.Play();
                main.playOnAwake = !main.playOnAwake;

                yield return null;

                // Check if system has stopped emitting after playing.
                m_ParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                main.playOnAwake = !main.playOnAwake;

                yield return null;

                Assert.IsFalse(backingParticleSystem.isEmitting, "System was expected to have stopped emitting.");
                Assert.IsFalse(backingParticleSystem.isPaused, "System was expected to not be paused, since StopEmitting was called.");

                // Restart system up again and check to see if it playing again.
                m_ParticleSystem.Play();
                main.playOnAwake = !main.playOnAwake;

                yield return null;

                Assert.IsTrue(backingParticleSystem.isPlaying, "System was expected to be playing.");

                // Check if system can fully stop after playing.
                m_ParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                main.playOnAwake = !main.playOnAwake;

                yield return null;

                Assert.IsTrue(backingParticleSystem.isStopped, "System is meant to be fully stopped now.");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_UnityParticleSystem_Check_PolySpatialMinMaxCurves()
        {
            var material = PolySpatialComponentUtils.CreateUnlitParticleURPMaterial(Color.blue);
            CreateTestParticleSystem(material);

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_ParticleSystem);
            Assert.IsTrue(data.ValidateTrackingFlags());
            PolySpatialComponentUtils.ValidateTrackingData(data, PolySpatialTrackingFlags.Running);
            Assert.IsFalse(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            yield return null;

            var backingGO = GetBackingGameObjectForParticleSystem(m_ParticleSystem);
            if (backingGO != null)
            {
                var backingParticleSystem = backingGO.GetComponent<ParticleSystem>();

                // This isn't a test of all the minmax curve properties available, but it does contain a good sampling
                // (At least constant, curve, two const, two curves).
                var backingMain = backingParticleSystem.main;
                var sourceMain = m_ParticleSystem.main;
                AssertPolySpatialCurvesEqual(sourceMain.startSpeed, backingMain.startSpeed);
                AssertPolySpatialCurvesEqual(sourceMain.startLifetime, backingMain.startLifetime);
                AssertPolySpatialCurvesEqual(sourceMain.startSize, backingMain.startSize);
                if (sourceMain.startRotation3D)
                {
                    AssertPolySpatialCurvesEqual(sourceMain.startRotationX, backingMain.startRotationX);
                    AssertPolySpatialCurvesEqual(sourceMain.startRotationY, backingMain.startRotationY);
                    AssertPolySpatialCurvesEqual(sourceMain.startRotationZ, backingMain.startRotationZ);
                }
                else
                {
                    AssertPolySpatialCurvesEqual(sourceMain.startRotation, backingMain.startRotation);
                }

                var backingEmission = backingParticleSystem.emission;
                var sourceEmission = m_ParticleSystem.emission;
                AssertPolySpatialCurvesEqual(sourceEmission.rateOverTime, backingEmission.rateOverTime);

                var backingVelocity = backingParticleSystem.velocityOverLifetime;
                var sourceVelocity = m_ParticleSystem.velocityOverLifetime;
                AssertPolySpatialCurvesEqual(sourceVelocity.y, backingVelocity.y);
                AssertPolySpatialCurvesEqual(sourceVelocity.orbitalZ, backingVelocity.orbitalZ);
                AssertPolySpatialCurvesEqual(sourceVelocity.orbitalOffsetX, backingVelocity.orbitalOffsetX);
                AssertPolySpatialCurvesEqual(sourceVelocity.radial, backingVelocity.radial);

                var backingSheetAnimation = backingParticleSystem.textureSheetAnimation;
                var sourceSheetAnimation = m_ParticleSystem.textureSheetAnimation;
                AssertPolySpatialCurvesEqual(sourceSheetAnimation.frameOverTime, backingSheetAnimation.frameOverTime);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_UnityParticleSystem_Check_PolySpatialParticleGradients()
        {
            var material = PolySpatialComponentUtils.CreateUnlitParticleURPMaterial(Color.blue);
            CreateTestParticleSystem(material);

            yield return null;

            var backingGO = GetBackingGameObjectForParticleSystem(m_ParticleSystem);
            if (backingGO != null)
            {
                var backingParticleSystem = backingGO.GetComponent<ParticleSystem>();
                var sourceColor = m_ParticleSystem.colorOverLifetime;

                // Quick test to see if the gradients are coming through the polyspatial bridge alright.
                AssertParticleGradientsEqual(sourceColor.color, backingParticleSystem.colorOverLifetime.color);

                var newColor = sourceColor.color;
                newColor.mode = ParticleSystemGradientMode.Color;
                newColor.color = Color.blue;

                sourceColor.color = newColor;

                yield return null;
                AssertParticleGradientsEqual(sourceColor.color, backingParticleSystem.colorOverLifetime.color);

                newColor = m_ParticleSystem.colorOverLifetime.color;
                newColor.mode = ParticleSystemGradientMode.RandomColor;
                newColor.color = Color.gray;

                sourceColor.color = newColor;

                yield return null;
                AssertParticleGradientsEqual(sourceColor.color, backingParticleSystem.colorOverLifetime.color);

                newColor = m_ParticleSystem.colorOverLifetime.color;
                newColor.mode = ParticleSystemGradientMode.TwoColors;
                newColor.colorMin = Color.cyan;
                newColor.colorMax = Color.red;

                sourceColor.color = newColor;

                yield return null;
                AssertParticleGradientsEqual(sourceColor.color, backingParticleSystem.colorOverLifetime.color);

                newColor = m_ParticleSystem.colorOverLifetime.color;
                newColor.mode = ParticleSystemGradientMode.TwoGradients;

                var firstGradient = new Gradient();
                firstGradient.mode = GradientMode.PerceptualBlend;
                firstGradient.alphaKeys = new GradientAlphaKey[4]
                {
                    new(0, 0),
                    new(255, 3.5f),
                    new(1, 25),
                    new(0, 100),
                };

                firstGradient.colorKeys = new GradientColorKey[3]
                {
                    new(Color.black, 0),
                    new(Color.cyan, 4.5f),
                    new(Color.black, 100),
                };

                var secondGradient = new Gradient();
                secondGradient.mode = GradientMode.Fixed;
                secondGradient.alphaKeys = new GradientAlphaKey[2]
                {
                    new(0, 0),
                    new(255, 100)
                };

                secondGradient.colorKeys = new GradientColorKey[1]
                {
                    new(Color.green, 100),
                };

                newColor.gradientMin = firstGradient;
                newColor.gradientMax = secondGradient;

                sourceColor.color = newColor;

                yield return null;
                AssertParticleGradientsEqual(sourceColor.color, backingParticleSystem.colorOverLifetime.color);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_UnityParticleSystem_Check_BakeToMeshParticlesWithTrail()
        {
            PolySpatialSettings.instance.ParticleMode = PolySpatialSettings.ParticleReplicationMode.BakeToMesh;
            var material = PolySpatialComponentUtils.CreateUnlitParticleURPMaterial(Color.blue);

            CreateTestParticleSystem(material);

            yield return null;

            var particleSystemTrails = m_ParticleSystem.trails;
            particleSystemTrails.enabled = true;
            var particleRenderer = m_ParticleSystem.GetComponent<ParticleSystemRenderer>();
            particleRenderer.trailMaterial = material;
            yield return null;
        }

        /// <summary>
        /// Scales the auto-generated volume camera and checks to see if the baked mesh backing-GO's lossy scale reflects
        /// those changes. This ensures the backing-GO is parented to the root object that receives scaling updates relative
        /// to the scene's volume camera.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator Test_UnityParticleSystem_Check_BakeToMeshParticleScale()
        {
            const float scaleFactor = 10f;

            PolySpatialSettings.instance.ParticleMode = PolySpatialSettings.ParticleReplicationMode.BakeToMesh;
            var material = PolySpatialComponentUtils.CreateUnlitParticleURPMaterial(Color.blue);

            CreateTestParticleSystem(material);
            var particleSystemScale = m_ParticleSystem.transform.lossyScale;
            var qvc = GameObject.FindAnyObjectByType<VolumeCamera>();
            qvc.transform.localScale = Vector3.one * scaleFactor;

            // Not a typo - For some reason this requires 2 frames.
            yield return null;
            yield return null;

            var backingGO = GetBackingGameObjectForParticleSystem(m_ParticleSystem);

            // TODO: LXR-3062 for some reason, backingGO is null on macOS standalone tests
            if (backingGO == null)
                yield break;

            var lossyScale = backingGO.transform.lossyScale;
            Assert.IsTrue(Mathf.Approximately(particleSystemScale.x / scaleFactor, lossyScale.x) &&
                          Mathf.Approximately(particleSystemScale.y / scaleFactor, lossyScale.y) &&
                          Mathf.Approximately(particleSystemScale.z / scaleFactor, lossyScale.z));
        }

        [UnityTest]
        public IEnumerator Test_UnityParticleSystem_Check_BiRPUnlitMaterialParticleSystem()
        {
            PolySpatialSettings.instance.ParticleMode = PolySpatialSettings.ParticleReplicationMode.BakeToMesh;
            var material = PolySpatialComponentUtils.CreateUnlitParticleBIRPMaterial(Color.white, "Textures/" + k_TestParticleTextureName);

            if (material == null)
                yield break;

            var materialBaseMap = material.GetTexture("_MainTex");
            CreateTestParticleSystem(material);

            yield return null;

            var backingGO = GetBackingGameObjectForParticleSystem(m_ParticleSystem);
            var backingMaterial = backingGO.GetComponent<MeshRenderer>().material;
            var backingMaterialBaseMap = backingMaterial.GetTexture("_BaseMap");

            // Check dimensions are equal as texture name doesn't seem to be transmitted.
            Assert.IsTrue(backingMaterialBaseMap.width == materialBaseMap.width && backingMaterialBaseMap.height == materialBaseMap.height);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_UnityParticleSystem_Check_URPUnlitMaterialParticleSystem()
        {
            var baseColor = Color.red;

            PolySpatialSettings.instance.ParticleMode = PolySpatialSettings.ParticleReplicationMode.BakeToMesh;
            var material = PolySpatialComponentUtils.CreateUnlitParticleURPMaterial(baseColor, "Textures/" + k_TestParticleTextureName);

            if (material == null)
                yield break;

            CreateTestParticleSystem(material);

            yield return null;

            var backingGO = GetBackingGameObjectForParticleSystem(m_ParticleSystem);

            if (backingGO == null)
                yield break;

            var backingMaterial = backingGO.GetComponent<MeshRenderer>().material;
            var backingBaseColor = backingMaterial.GetColor(MaterialProperties.k_BaseColor);

            Assert.AreEqual(backingBaseColor, baseColor);
        }

        [UnityTest]
        public IEnumerator Test_UnityParticleSystem_ShadowsOnly()
        {
            var material = PolySpatialComponentUtils.CreateUnlitParticleURPMaterial(Color.blue);

            CreateTestParticleSystem(material);
            m_ParticleSystem.GetComponent<ParticleSystemRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            yield return null;

#if UNITY_EDITOR
            var backingGameObject = GetBackingGameObjectForParticleSystem(m_ParticleSystem);
            Assert.NotNull(backingGameObject);
            var particleSystemRenderer = backingGameObject.GetComponent<ParticleSystemRenderer>();
            Assert.Null(particleSystemRenderer);
#endif
		}

        [UnityTest]
        public IEnumerator Test_UnityParticleSystem_Check_BiRPLitMaterialParticleSystem()
        {
            const float k_Metallic = 0.25f;
            const float k_Smoothness = 0.75f;

            PolySpatialSettings.instance.ParticleMode = PolySpatialSettings.ParticleReplicationMode.BakeToMesh;
            var material = PolySpatialComponentUtils.CreateLitParticleBIRPMaterial(Color.white, "Textures/" + k_TestParticleTextureName);

            if (material == null)
                yield break;

            material.SetFloat(MaterialProperties.k_Metallic, k_Metallic);
            // Glossiness is the smoothness property of birp lit.
            material.SetFloat(MaterialProperties.k_Glossiness, k_Smoothness);
            var materialBaseMap = material.GetTexture("_MainTex");
            CreateTestParticleSystem(material);

            yield return null;

            var backingGO = GetBackingGameObjectForParticleSystem(m_ParticleSystem);
            var backingMaterial = backingGO.GetComponent<MeshRenderer>().material;
            var backingMaterialBaseMap = backingMaterial.GetTexture("_BaseMap");

            // Check dimensions are equal as texture name doesn't seem to be transmitted.
            Assert.IsTrue(backingMaterialBaseMap.width == materialBaseMap.width && backingMaterialBaseMap.height == materialBaseMap.height);

            var backingMaterialMetallic = backingMaterial.GetFloat(MaterialProperties.k_Metallic);
            var backingMaterialSmoothness = backingMaterial.GetFloat(MaterialProperties.k_Smoothness);

            Assert.IsTrue(Mathf.Approximately(backingMaterialMetallic, k_Metallic));
            Assert.IsTrue(Mathf.Approximately(backingMaterialSmoothness, k_Smoothness));
        }

        [UnityTest]
        public IEnumerator Test_UnityParticleSystem_Check_URPLitMaterialParticleSystem()
        {
            const float k_Metallic = 0.25f;
            const float k_Smoothness = 0.75f;
            var baseColor = Color.red;
            var emissionColor = Color.blue;

            PolySpatialSettings.instance.ParticleMode = PolySpatialSettings.ParticleReplicationMode.BakeToMesh;
            var material = PolySpatialComponentUtils.CreateLitParticleURPMaterial(baseColor, "Textures/" + k_TestParticleTextureName);

            if (material == null)
                yield break;

            material.SetFloat(MaterialProperties.k_Metallic, k_Metallic);
            material.SetFloat(MaterialProperties.k_Smoothness, k_Smoothness);
            var emissionKeyword = new LocalKeyword(material.shader, MaterialKeywords.k_Emission);
            material.SetKeyword(emissionKeyword, true);
            material.SetColor(MaterialProperties.k_EmissionColor, emissionColor);
            CreateTestParticleSystem(material);

            yield return null;

            var backingGO = GetBackingGameObjectForParticleSystem(m_ParticleSystem);
            var backingMaterial = backingGO.GetComponent<MeshRenderer>().material;
            var backingMaterialMetallic = backingMaterial.GetFloat(MaterialProperties.k_Metallic);
            var backingMaterialSmoothness = backingMaterial.GetFloat(MaterialProperties.k_Smoothness);
            var backingBaseColor = backingMaterial.GetColor(MaterialProperties.k_BaseColor);
            var backingEmissionColor = backingMaterial.GetColor(MaterialProperties.k_EmissionColor);

            Assert.IsTrue(Mathf.Approximately(backingMaterialMetallic, k_Metallic));
            Assert.IsTrue(Mathf.Approximately(backingMaterialSmoothness, k_Smoothness));
            Assert.IsTrue(backingMaterial.IsKeywordEnabled(MaterialKeywords.k_Emission));
            Assert.AreEqual(backingBaseColor, baseColor);
            Assert.AreEqual(backingEmissionColor, emissionColor);
        }
    }
}
