using System;
using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using Unity.PolySpatial.Internals.Capabilities;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that creates validation rules for particle systems.
    /// </summary>
    class ParticleRuleCreator : RendererRuleCreator
    {
        const string k_MessageFormat = "The <b>{0}</b> profile(s) do not support the following properties: {1}.";
        const string k_FixItMessageFormat = "Do not use the following properties: {0}.";

        static readonly HashSet<ParticleSystemShapeType> k_AllowedShapeTypes = new HashSet<ParticleSystemShapeType>()
        {
            ParticleSystemShapeType.Sphere, ParticleSystemShapeType.Hemisphere, ParticleSystemShapeType.Cone, ParticleSystemShapeType.Donut,
            ParticleSystemShapeType.Box, ParticleSystemShapeType.Circle, ParticleSystemShapeType.BoxEdge, ParticleSystemShapeType.SingleSidedEdge,
            ParticleSystemShapeType.Rectangle, ParticleSystemShapeType.Mesh
        };

        internal ParticleRuleCreator() : base(false)
        {
        }

        /// <inheritdoc />
        public override void GetTypesToTrack(Component component, List<Type> types)
        {
            base.GetTypesToTrack(component, types);
            types.Add(typeof(ParticleSystemRenderer));
        }

        /// <inheritdoc />
        public override void CreateRules(Component component, List<BuildValidationRule> createdRules)
        {
            base.CreateRules(component, createdRules);

            var instanceID = component.GetInstanceID();
            var propertyRules = new List<PropertyRule>();
            var failedRules = new List<PropertyRule>();
            PopulatePropertyRules(EditorUtility.InstanceIDToObject(instanceID) as ParticleSystem, propertyRules);

            var rule = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile)
                                      && PolySpatialSettings.instance.ParticleMode != PolySpatialSettings.ParticleReplicationMode.BakeToMesh,
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, component.GetType().Name),
                Message = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames,
                    string.Join(", ", propertyRules.Where(rule => !rule.CheckPredicate()).Select(rule => rule.Name))),
                FixItAutomatic = true,
                FixItMessage = string.Format(k_FixItMessageFormat,
                    string.Join(", ", propertyRules.Where(rule => !rule.CheckPredicate()).Select(rule => rule.Name))),
                SceneOnlyValidation = true,
                Error = false,
                OnClick = () => BuildValidator.SelectObject(instanceID),
            };

            rule.CheckPredicate = () =>
            {
                var particleSystem = EditorUtility.InstanceIDToObject(instanceID) as ParticleSystem;
                if (particleSystem == null)
                    return true;

                failedRules.Clear();
                failedRules.AddRange(propertyRules.Where(propertyRule => !propertyRule.CheckPredicate()));

                rule.Message = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames,
                    string.Join(", ", failedRules.Select(propertyRule => propertyRule.Name)));

                return failedRules.Count == 0;
            };

            rule.FixIt = () =>
            {
                var particleSystem = EditorUtility.InstanceIDToObject(instanceID) as ParticleSystem;
                if (particleSystem == null)
                    return;

                Undo.RecordObjects(new Object[]{particleSystem, particleSystem.GetComponent<ParticleSystemRenderer>()}, "Fix Particle System Properties");
                foreach (var propertyRule in failedRules)
                    propertyRule.FixIt();
            };

            createdRules.Add(rule);
        }

        void PopulatePropertyRules (ParticleSystem particleSystem, List<PropertyRule> propertyRules)
        {
            //Main
            var main = particleSystem.main;

            propertyRules.Add(new PropertyRule("Start Delay",
                ()=> main.startDelay.mode == ParticleSystemCurveMode.Constant && main.startDelay.constant == 0f,
                () =>
                {
                    var startDelay = main.startDelay;
                    startDelay.mode = ParticleSystemCurveMode.Constant;
                    startDelay = 0;
                    main.startDelay = startDelay;
                }));

            propertyRules.Add(new PropertyRule("3D Start Rotation",
                ()=> !main.startRotation3D,
                () => main.startRotation3D = false));

            propertyRules.Add(new PropertyRule("Flip Rotation",
                ()=> main.flipRotation == 0,
                () => main.flipRotation = 0));

            propertyRules.Add(new PropertyRule("Simulation Speed",
                ()=> main.simulationSpeed == 1f,
                () => main.simulationSpeed = 1));

            propertyRules.Add(new PropertyRule("Auto Random Seed",
                () => particleSystem.useAutoRandomSeed,
                () => particleSystem.useAutoRandomSeed = true));

            propertyRules.Add(new PropertyRule("Stop Action",
                () => main.stopAction == ParticleSystemStopAction.None,
                () => main.stopAction = ParticleSystemStopAction.None));

            propertyRules.Add(new PropertyRule("Culling Mode",
                () => main.cullingMode == ParticleSystemCullingMode.Automatic,
                () => main.cullingMode = ParticleSystemCullingMode.Automatic));

            propertyRules.Add(new PropertyRule("Ring Buffer Mode",
                () => main.ringBufferMode == ParticleSystemRingBufferMode.Disabled,
                () => main.ringBufferMode = ParticleSystemRingBufferMode.Disabled));

            //Emission
            var emission = particleSystem.emission;;

            propertyRules.Add(new PropertyRule("Rate Over Distance",
                () => !emission.enabled || emission.rateOverDistance.mode == ParticleSystemCurveMode.Constant
                    && emission.rateOverDistance.constant == 0f,
                () =>
                {
                    var rate = emission.rateOverDistance;
                    rate.mode = ParticleSystemCurveMode.Constant;
                    rate.constant = 0f;
                    emission.rateOverDistance = rate;
                }));

            propertyRules.Add(new PropertyRule("Bursts",
                () => !emission.enabled || emission.burstCount == 0,
                () => emission.SetBursts(new ParticleSystem.Burst[]{})));

            //Shape
            var shape = particleSystem.shape;

            propertyRules.Add(new PropertyRule("Shape",
                () => !shape.enabled || k_AllowedShapeTypes.Contains(shape.shapeType),
                () => shape.shapeType = ParticleSystemShapeType.Cone));

            propertyRules.Add(new PropertyRule("Shape Texture",
                () => !shape.enabled || shape.texture == null,
                () => shape.texture = null));

            propertyRules.Add(new PropertyRule("Align to Direction",
                () => !shape.enabled || shape.alignToDirection == false,
                () => shape.alignToDirection = false));

            propertyRules.Add(new PropertyRule("Randomize Direction",
                () => !shape.enabled || shape.randomDirectionAmount == 0f,
                () => shape.randomDirectionAmount = 0f));

            propertyRules.Add(new PropertyRule("Spherize Direction",
                () => !shape.enabled || shape.sphericalDirectionAmount == 0f,
                () => shape.sphericalDirectionAmount = 0f));

            propertyRules.Add(new PropertyRule("Randomize Position",
                () => !shape.enabled || shape.randomPositionAmount == 0f,
                () => shape.randomPositionAmount = 0f));

            //Velocity over Lifetime
            var velocityOverLifetime = particleSystem.velocityOverLifetime;

            propertyRules.Add(new PropertyRule("Speed Modifier",
                () => !velocityOverLifetime.enabled || (velocityOverLifetime.speedModifier.mode == ParticleSystemCurveMode.Constant && velocityOverLifetime.speedModifier.constant == 1f),
                () =>
                {
                    var speedModifier = velocityOverLifetime.speedModifier;
                    speedModifier.constant = 1f;
                    velocityOverLifetime.speedModifier = speedModifier;
                }));

            //Inherit Velocity
            var inheritVelocity = particleSystem.inheritVelocity;
            propertyRules.Add (new PropertyRule("Inherit Velocity Mode",
                () => !inheritVelocity.enabled,
                () => inheritVelocity.enabled = false));

            //Lifetime By Emitter Speed
            var lifetimeByEmitterSpeed = particleSystem.lifetimeByEmitterSpeed;
            propertyRules.Add (new PropertyRule("Lifetime by Emitter Speed",
                () => !lifetimeByEmitterSpeed.enabled,
                () => lifetimeByEmitterSpeed.enabled = false));

            //Force Over Lifetime
            var forceOverLifetime = particleSystem.forceOverLifetime;

            propertyRules.Add(new PropertyRule("Force Over Lifetime Randomize",
                () => !forceOverLifetime.enabled || !forceOverLifetime.randomized,
                () => forceOverLifetime.randomized = false));

            //Color By Speed
            var colorBySpeed = particleSystem.colorBySpeed;
            propertyRules.Add(new PropertyRule("Color By Speed",
                () => !colorBySpeed.enabled,
                () => colorBySpeed.enabled= false));

            //Size Over Lifetime
            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            propertyRules.Add(new PropertyRule("Separate Axes",
                () => !sizeOverLifetime.enabled || sizeOverLifetime.separateAxes == false,
                () => sizeOverLifetime.separateAxes = false));

            //Size By Speed
            var sizeBySpeed = particleSystem.sizeBySpeed;
            propertyRules.Add(new PropertyRule("Size By Speed",
                () => !sizeBySpeed.enabled,
                () => sizeBySpeed.enabled = false));

            //Rotation By Speed
            var rotationBySpeed = particleSystem.rotationBySpeed;
            propertyRules.Add(new PropertyRule("Rotation By Speed",
                () => !rotationBySpeed.enabled,
                () => rotationBySpeed.enabled = false));

            //External Forces
            var externalForces = particleSystem.externalForces;
            propertyRules.Add(new PropertyRule("External Forces",
                () => !externalForces.enabled,
                () => externalForces.enabled = false));

            //Noise
            var noise = particleSystem.noise;

            propertyRules.Add(new PropertyRule("Noise Separate Axes",
                () => !noise.enabled || !noise.separateAxes,
                () => noise.separateAxes = false));

            propertyRules.Add(new PropertyRule("Noise Damping",
                () => !noise.enabled || !noise.damping,
                () => noise.damping = false));

            propertyRules.Add(new PropertyRule("Noise Octaves",
                () => !noise.enabled || noise.octaveCount == 1,
                () => noise.octaveCount = 1));

            propertyRules.Add(new PropertyRule("Noise Remap",
                () => !noise.enabled || !noise.remapEnabled,
                () => noise.remapEnabled = false));

            propertyRules.Add(new PropertyRule("Noise Rotation Amount",
                () => !noise.enabled || noise.rotationAmount.mode == ParticleSystemCurveMode.Constant && noise.rotationAmount.constant == 0f,
                () => noise.rotationAmount = 0f));

            propertyRules.Add(new PropertyRule("Noise Size Amount",
                () => !noise.enabled || noise.sizeAmount.mode == ParticleSystemCurveMode.Constant && noise.sizeAmount.constant == 0f,
                () => noise.sizeAmount = 0f));

            //Collision
            var collision = particleSystem.collision;

            propertyRules.Add(new PropertyRule("Collision Lifetime Loss",
                () => !collision.enabled || collision.lifetimeLoss.mode == ParticleSystemCurveMode.Constant && collision.lifetimeLoss.constant == 0f,
                () => collision.lifetimeLoss = 0f));

            propertyRules.Add(new PropertyRule("Collision Min Kill Speed",
                () => !collision.enabled || collision.minKillSpeed == 0f,
                () => collision.minKillSpeed = 0f));

            propertyRules.Add(new PropertyRule("Collision Radius Scale",
                () => !collision.enabled || collision.radiusScale == 1f,
                () => collision.radiusScale = 1f));

            propertyRules.Add(new PropertyRule("Collision Send Collision Messages",
                () => !collision.enabled || !collision.sendCollisionMessages,
                () => collision.sendCollisionMessages = false));

            //Triggers
            var trigger = particleSystem.trigger;
            propertyRules.Add(new PropertyRule("Triggers",
                () => !trigger.enabled,
                () => trigger.enabled = false));

            //Lights
            var lights = particleSystem.lights;
            propertyRules.Add(new PropertyRule("Lights",
                () => !lights.enabled,
                () => lights.enabled = false));

            //Trails
            var trails = particleSystem.trails;
            propertyRules.Add(new PropertyRule("Trails",
                () => !trails.enabled || PolySpatialSettings.instance.ParticleMode == PolySpatialSettings.ParticleReplicationMode.BakeToMesh,
                () => trails.enabled = false));

            //Custom Data
            var customData = particleSystem.customData;
            propertyRules.Add(new PropertyRule("Custom Data",
                () => !customData.enabled,
                () => customData.enabled = false));

            //Renderer
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();

            propertyRules.Add(new PropertyRule("Renderer Normal Direction",
                () => !renderer.enabled || renderer.normalDirection == 1f,
                () => renderer.normalDirection = 1f));

            propertyRules.Add(new PropertyRule("Sorting Fudge",
                () => !renderer.enabled || renderer.sortingFudge == 0f,
                () => renderer.sortingFudge = 0f));

            propertyRules.Add(new PropertyRule("Min Particle Size",
                () => !renderer.enabled || renderer.minParticleSize == 0f,
                () => renderer.minParticleSize = 0f));

            propertyRules.Add(new PropertyRule("Renderer Flip",
                () => !renderer.enabled || renderer.flip == Vector3.zero,
                () => renderer.flip = Vector3.zero));

            propertyRules.Add(new PropertyRule("Renderer Allow Roll",
                () => !renderer.enabled || !renderer.allowRoll,
                () => renderer.allowRoll = false));

            propertyRules.Add(new PropertyRule("Renderer Pivot",
                () => !renderer.enabled || renderer.pivot == Vector3.zero,
                () => renderer.pivot = Vector3.zero));

            propertyRules.Add(new PropertyRule("Renderer Masking",
                () => !renderer.enabled || renderer.maskInteraction == SpriteMaskInteraction.None,
                () => renderer.maskInteraction = SpriteMaskInteraction.None));

            propertyRules.Add(new PropertyRule("Cast Shadows",
                () => !renderer.enabled || renderer.shadowCastingMode == ShadowCastingMode.Off,
                () => renderer.shadowCastingMode = ShadowCastingMode.Off));

            propertyRules.Add(new PropertyRule("Receive Shadows",
                () => !renderer.enabled || !renderer.receiveShadows,
                () => renderer.receiveShadows = false));

            propertyRules.Add(new PropertyRule("Shadow Bias",
                () => !renderer.enabled || renderer.shadowBias == 0f,
                () => renderer.shadowBias = 0f));

            propertyRules.Add(new PropertyRule("Lighting Probes",
                () => !renderer.enabled || renderer.lightProbeUsage.Equals(LightProbeUsage.Off),
                () => renderer.lightProbeUsage = LightProbeUsage.Off));

            propertyRules.Add(new PropertyRule("Reflection Probes",
                () => !renderer.enabled || renderer.reflectionProbeUsage.Equals(ReflectionProbeUsage.Off),
                () => renderer.reflectionProbeUsage = ReflectionProbeUsage.Off));
        }

        struct PropertyRule
        {
            internal string Name;
            internal Func<bool> CheckPredicate;
            internal Action FixIt;

            public PropertyRule(string name, Func<bool> checkPredicate, Action fixIt)
            {
                Name = name;
                CheckPredicate = checkPredicate;
                FixIt = fixIt;
            }
        }
    }
}
