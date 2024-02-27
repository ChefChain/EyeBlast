using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.PolySpatial;
using Unity.XR.CoreUtils.Capabilities;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using Unity.PolySpatial.Internals.Capabilities;
using UnityEditor.PolySpatial.Internals;
using UnityEditor.PolySpatial.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Tilemaps;
using UnityEngine.Video;
using UIDocument = UnityEngine.UIElements.UIDocument;
using UnityObject = UnityEngine.Object;

#if ENABLE_UGUI
using UnityEngine.EventSystems;
using UnityEngine.UI;
#endif

#if ENABLE_TEXT_MESH_PRO
using TMPro;
#endif

#if ENABLE_URP
using UnityEngine.Rendering.Universal;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.OnScreen;
#endif

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that validates components in the loaded scenes.
    /// </summary>
    // todo LXR-785 The Game Object/Component rule tracking feature should be moved to the ProjectValidator in the CoreUtils package.
    [InitializeOnLoad]
    static class PolySpatialSceneValidator
    {
        const string k_HaloTypeName = "UnityEngine.Halo, UnityEngine";
        const string k_FixObjectProgressBarTitle = "Fix Object Issues";
        const string k_RuleCreatorErrorFormat = "Cannot register Rule Creators for: {0}";

        delegate void RuleCreatorDelegate(List<ValueTuple<Type, IComponentRuleCreator>> ruleCreators, List<ValueTuple<Type, List<ITypeMessage>>> messages);

        internal const string RuleCategoryFormat = "PolySpatial - {0}";

        static readonly HashSet<int> s_VisitedComponents = new();
        static readonly List<Component> s_Components = new();
        static readonly List<Component> s_AddedComponents = new();
        static readonly HashSet<int> s_VisitedGameObjects = new();
        static readonly List<GameObject> s_AddedGameObjects = new();

        static readonly Dictionary<int, List<int>> s_GameObjectComponentsMap = new();
        static readonly Dictionary<int, List<BuildValidationRule>> s_ComponentRulesMap = new();
        static readonly Dictionary<int, List<BuildValidationRule>> s_ObjectsRuleMap = new();

        // Stores the root and its next child failure issue instanceIDs as the key and value respectively
        static readonly Dictionary<int, int> s_RootsNextChildFailureMap = new();
        static readonly HashSet<int> s_GameObjectsFailuresSet = new();
        static readonly List<int> s_GameObjectsFailuresList = new();

        static readonly BuildValidatorCopy s_BuildValidatorCopy = new();
        static readonly HashSet<BuildValidationRule> s_RuleFailures = new();

        static readonly List<BuildValidationRule> s_FixAllList = new();

        static readonly List<KeyValuePair<Type, IComponentRuleCreator>> s_TypeRuleCreatorList = new();
        static readonly List<KeyValuePair<Type, ITypeMessage[]>> s_TypeMessagesList = new();

        // Cached data for fast searching
        static readonly Dictionary<Type, IComponentRuleCreator> s_CachedRuleCreators = new();
        static readonly Dictionary<Type, ITypeMessage[]> s_CachedTypeMessages = new();

        // For local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<BuildValidationRule> s_ComponentRules = new();
        static readonly List<BuildValidationRule> s_CreatedRules = new();
        static readonly List<BuildValidationRule> s_GameObjectRules = new();
        static readonly List<Type> s_TypesToTrack = new();

        // The tracked types registered by this class in PolySpatialObjectAuthoringTracker, used to avoid registering callbacks
        // twice and to avoid collision errors with other classes registering the same type in PolySpatialObjectAuthoringTracker
        static readonly HashSet<Type> s_TrackedTypes = new();
        internal static event Action OnValidateRules;

        static string s_CachedCapabilityProfileNames;

        internal static string CachedCapabilityProfileNames => s_CachedCapabilityProfileNames ??= GetPolySpatialCapabilityProfileNames();

        static string GetPolySpatialCapabilityProfileNames()
        {
            var capabilityProfiles = new List<CapabilityProfile>();
            foreach (var assetGuid in AssetDatabase.FindAssets("t:CapabilityProfile"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                var capabilityProfile = AssetDatabase.LoadAssetAtPath<CapabilityProfile>(assetPath);
                if (capabilityProfile != null && capabilityProfile is PolySpatialCapabilityProfile)
                    capabilityProfiles.Add(capabilityProfile);
            }
            capabilityProfiles.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            return string.Join(", ", capabilityProfiles.Select(c => c.name));
        }

        static PolySpatialSceneValidator()
        {
            // Delay the initialization to allow AssetDatabase.FindAssets to work properly in a clean checkout (LXR-2335)
            EditorApplication.delayCall += Initialize;
        }

        static void Initialize()
        {
            // The order of these invocations are important, unsupported component types should always be added later (low priority)
            // to allow overrides of derived classes in PopulateSupportedTypes. PopulateTypesFromAttribute is called first to allow
            // users to override the default behavior.
            PopulateTypesFromAttribute();
            PopulateSupportedTypes();
            PopulateUnsupportedTypes();

            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RegisterListeners();
                Refresh();
            }
        }

        static IEnumerable<RuleCreatorDelegate> FetchDelegatesFromAttribute()
        {
            var methods = TypeCache.GetMethodsWithAttribute<CustomValidationAttribute>();
            return methods
                .Select(method =>
                {
                    try
                    {
                        var attribute = method.GetCustomAttribute<CustomValidationAttribute>();
                        if (attribute == null)
                            return new ValueTuple<CustomValidationAttribute, RuleCreatorDelegate>();

                        var callback = Delegate.CreateDelegate(typeof(RuleCreatorDelegate), method) as RuleCreatorDelegate;
                        return new ValueTuple<CustomValidationAttribute, RuleCreatorDelegate>(attribute, callback);
                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat(k_RuleCreatorErrorFormat, method.Name);;
                        Debug.LogException(e);
                    }
                    return new ValueTuple<CustomValidationAttribute, RuleCreatorDelegate>();
                })
                .Where(tuple => tuple.Item1 != null && tuple.Item2 != null)
                .OrderBy(tuple => tuple.Item1.Priority)
                .Select(tuple => tuple.Item2);
        }

        static void PopulateTypesFromAttribute()
        {
            var delegates = FetchDelegatesFromAttribute();
            var ruleCreators = new List<ValueTuple<Type, IComponentRuleCreator>>();
            var messages = new List<ValueTuple<Type, List<ITypeMessage>>>();
            foreach (var callback in delegates)
            {
                try
                {
                    ruleCreators.Clear();
                    messages.Clear();
                    callback?.Invoke(ruleCreators, messages);
                    foreach (var tuple in ruleCreators)
                        AddRuleCreator(tuple.Item1, tuple.Item2);
                    foreach (var tuple in messages)
                        AddMessages(tuple.Item1, tuple.Item2.ToArray());
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat(k_RuleCreatorErrorFormat, callback?.Method.Name);
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Use this method to populate the <see cref="s_TypeMessagesList"/> and <see cref="s_TypeRuleCreatorList"/> lists
        /// with messages and rule creators for supported or partially supported types.
        /// </summary>
        /// <remarks>
        /// Rule creators added in this method have higher priority and allow for overrides of derived types added in
        /// <see cref="PopulateSupportedTypes"/>. Rule creators are retrieved through a linear search (<see cref="GetRuleCreator"/>).
        /// Information about the partially supported components were last seen in the file <c>ComponentDataDefinition.cs</c>.
        /// </remarks>
        static void PopulateSupportedTypes()
        {
            AddMessages(typeof(Light), new LightSyncMessage());

            // Renderers
            var rendererRuleCreator = new RendererRuleCreator(true);

            AddRuleCreator(typeof(SpriteRenderer), rendererRuleCreator);

            AddMessages(typeof(MeshRenderer), new MeshRendererSyncMessage());
            AddRuleCreator(typeof(MeshRenderer), rendererRuleCreator);

            AddMessages(typeof(SkinnedMeshRenderer), new SkinnedMeshRendererSyncMessage());
            AddRuleCreator(typeof(SkinnedMeshRenderer), new SkinnedMeshRendererRuleCreator());

            //ParticleSystem
            AddMessages(typeof(ParticleSystem), new ParticleSystemMessage());
            AddRuleCreator(typeof(ParticleSystem), new ParticleRuleCreator());

            AddRuleCreator(typeof(VolumeCamera), new VolumeCameraSettingsRuleCreator());

            AddRuleCreator(typeof(Collider), new ColliderNonUniformScaleRule());

#if ENABLE_UGUI
            // UGUI
            AddMessages(typeof(ContentSizeFitter), null);
            AddMessages(typeof(AspectRatioFitter), null);
            AddMessages(typeof(LayoutGroup), null);
            AddMessages(typeof(LayoutElement), null);

            AddMessages(typeof(Image), new ImageSyncMessage());
            AddRuleCreator(typeof(Image), null);

            AddRuleCreator(typeof(Button), null);
            AddRuleCreator(typeof(CanvasScaler), null);
            AddRuleCreator(typeof(BaseRaycaster), null);

            AddRuleCreator(typeof(EventSystem), null);
            AddRuleCreator(typeof(BaseInputModule), null);

            AddRuleCreator(typeof(Canvas), new CanvasComponentRule());

#endif

#if ENABLE_TEXT_MESH_PRO
            // Text mesh pro Text
            var tmpRuleCreator =  new TMPRuleCreator();
            AddRuleCreator(typeof(TextMeshPro), tmpRuleCreator);
            AddRuleCreator(typeof(TextMeshProUGUI), tmpRuleCreator);
#endif
        }

        /// <summary>
        /// Use this method to populate the <see cref="s_TypeRuleCreatorList"/> with rule creators for unsupported types.
        /// </summary>
        /// <seealso cref="PopulateSupportedTypes"/>
        static void PopulateUnsupportedTypes()
        {
            var unsupportedRuleCreator = new UnsupportedComponentsRule();

            AddRuleCreator(typeof(UIDocument), unsupportedRuleCreator);

            AddRuleCreator(typeof(TilemapRenderer), unsupportedRuleCreator);
            AddRuleCreator(typeof(VideoPlayer), unsupportedRuleCreator);
            AddRuleCreator(typeof(TextMesh), unsupportedRuleCreator);
            AddRuleCreator(typeof(LensFlare), unsupportedRuleCreator);
            AddRuleCreator(typeof(LineRenderer), unsupportedRuleCreator);
            AddRuleCreator(typeof(ParticleSystem), unsupportedRuleCreator);
            AddRuleCreator(typeof(Projector), unsupportedRuleCreator);
            AddRuleCreator(typeof(TrailRenderer), unsupportedRuleCreator);
            AddRuleCreator(typeof(BillboardRenderer), unsupportedRuleCreator);

            // Halo class is internal
            var halloType = Type.GetType(k_HaloTypeName);
            if (halloType != null)
                AddRuleCreator(halloType, unsupportedRuleCreator);

            AddRuleCreator(typeof(Terrain), unsupportedRuleCreator);
            AddRuleCreator(typeof(Skybox), unsupportedRuleCreator);

            AddRuleCreator(typeof(LightProbeGroup), unsupportedRuleCreator);
            AddRuleCreator(typeof(LightProbeProxyVolume), unsupportedRuleCreator);
            AddRuleCreator(typeof(OcclusionArea), unsupportedRuleCreator);
            AddRuleCreator(typeof(OcclusionPortal), unsupportedRuleCreator);
            AddRuleCreator(typeof(ReflectionProbe), unsupportedRuleCreator);

#if ENABLE_URP
            AddRuleCreator(typeof(DecalProjector), unsupportedRuleCreator);
#endif

            AddRuleCreator(typeof(VisualEffect), unsupportedRuleCreator);
            AddRuleCreator(typeof(LODGroup), unsupportedRuleCreator);
            AddRuleCreator(typeof(StreamingController), unsupportedRuleCreator);

#if ENABLE_INPUT_SYSTEM
            // OnScreenButton and OnScreenStick
            AddRuleCreator(typeof(OnScreenControl), unsupportedRuleCreator);
#endif

#if ENABLE_UGUI
            // All UGUI and TextMeshPro components
            AddRuleCreator(typeof(UIBehaviour), unsupportedRuleCreator);
#endif
        }

        static void AddMessages(Type type, params ITypeMessage[] messages)
        {
            foreach (var pair in s_TypeMessagesList)
            {
                if (pair.Key == type)
                {
                    var list = pair.Value.ToList();
                    list.AddRange(messages);
                    s_TypeMessagesList.Remove(pair);
                    s_TypeMessagesList.Add(new KeyValuePair<Type, ITypeMessage[]>(type, list.ToArray()));
                    return;
                }
            }

            s_TypeMessagesList.Add(new KeyValuePair<Type, ITypeMessage[]>(type, messages));
        }

        static void AddRuleCreator(Type type, IComponentRuleCreator ruleCreator)
        {
            if (type == null)
            {
                Debug.LogError($"Cannot associate an {nameof(IComponentRuleCreator)} ({ruleCreator}) with a null type.");
                return;
            }

            s_TypeRuleCreatorList.Add(new KeyValuePair<Type, IComponentRuleCreator>(type, ruleCreator));
        }

        static void OnPlayModeChanged(PlayModeStateChange newState)
        {
            switch (newState)
            {
                case PlayModeStateChange.EnteredEditMode:
                    RegisterListeners();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    UnregisterListeners();
                    break;
            }
        }

        static void RegisterListeners()
        {
            CapabilityProfile.CapabilityChanged += OnCapabilityChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            CapabilityProfileSelection.SelectionSaved += OnCapabilityProfileSelectionSaved;
        }

        static void UnregisterListeners()
        {
            CapabilityProfile.CapabilityChanged -= OnCapabilityChanged;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            CapabilityProfileSelection.SelectionSaved -= OnCapabilityProfileSelectionSaved;
        }

        static void OnCapabilityProfileSelectionSaved()
        {
            UpdateRuleFailures(true);
        }

        static void OnCapabilityChanged(CapabilityProfile capabilityProfile)
        {
            UpdateRuleFailures(true);
        }

        static void OnObjectChangedByUser(List<UnityObject> changed, List<int> destroyed)
        {
            UpdateRuleFailures(true);
        }

        static void OnHierarchyChanged()
        {
            Refresh();
        }

        static void OnSceneUnloaded(Scene scene)
        {
            Refresh();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            Refresh();
        }

        static void Refresh()
        {
            if(PolySpatialSettings.instance.PolySpatialValidationOption == PolySpatialSettings.ValidationOption.Disabled)
                return;

            FetchAddedGameObjectsInLoadedScenes();
            UpdateRuleFailures();
        }

        static void FetchAddedGameObjectsInLoadedScenes()
        {
            s_AddedGameObjects.Clear();
            s_AddedComponents.Clear();
            s_Components.Clear();

            ComponentUtility<Component>.GetComponentsInAllScenes(s_Components, true, (int)HideFlags.HideAndDontSave);
            foreach (var component in s_Components)
            {
                // Fix for GetComponentsInChildren returning a null component for MonoBehaviours with a missing script
                if (component == null)
                    continue;

                var componentID = component.GetInstanceID();
                if (s_VisitedComponents.Contains(componentID))
                    continue;

                s_VisitedComponents.Add(componentID);
                s_AddedComponents.Add(component);

                // Below we fetch the newly added GameObjects
                if (component is not Transform)
                    continue;

                var gameObjectID = component.gameObject.GetInstanceID();
                if (s_VisitedGameObjects.Contains(gameObjectID))
                    continue;

                s_VisitedGameObjects.Add(gameObjectID);
                s_AddedGameObjects.Add(component.gameObject);
            }

            TryCreateRules(s_AddedGameObjects);
            TryCreateRules(s_AddedComponents);
        }

        static void TryCreateRules(List<Component> components)
        {
            if (components.Count == 0)
                return;

            s_ComponentRules.Clear();

            foreach (var component in components)
            {
                var componentType = component.GetType();
                var ruleCreator = GetRuleCreator(componentType);
                if (ruleCreator == null)
                    continue;

                s_CreatedRules.Clear();
                ruleCreator.CreateRules(component, s_CreatedRules);
                if (s_CreatedRules.Count == 0)
                    continue;

                if (ruleCreator is IPropertyValidator propertyValidator)
                {
                    s_TypesToTrack.Clear();
                    propertyValidator.GetTypesToTrack(component, s_TypesToTrack);
                    foreach (var typeToTrack in s_TypesToTrack)
                    {
                        if (s_TrackedTypes.Contains(typeToTrack))
                            continue;

                        s_TrackedTypes.Add(typeToTrack);
                        PolySpatialObjectAuthoringTracker.AddListener(typeToTrack, OnObjectChangedByUser);
                    }
                }

                Associate(component, s_CreatedRules);
                s_ComponentRules.AddRange(s_CreatedRules);
            }

            if (s_ComponentRules.Count == 0)
                return;

            if (EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.VisionOS &&
                PolySpatialSettings.instance.PolySpatialValidationOption == PolySpatialSettings.ValidationOption.EnabledForAllPlatforms)
            {
                BuildValidator.AddRules(EditorUserBuildSettings.selectedBuildTargetGroup, s_ComponentRules);
                s_BuildValidatorCopy.AddRules(EditorUserBuildSettings.selectedBuildTargetGroup, s_ComponentRules);
            }

            BuildValidator.AddRules(BuildTargetGroup.VisionOS, s_ComponentRules);
            s_BuildValidatorCopy.AddRules(BuildTargetGroup.VisionOS, s_ComponentRules);
        }

        static void TryCreateRules(List<GameObject> gameObjects)
        {
            if (gameObjects.Count == 0)
                return;

            s_GameObjectRules.Clear();

            foreach (var gameObject in gameObjects)
            {
                var createdRules = new List<BuildValidationRule>();
                PolySpatialLayerRule.CreateRules(gameObject, createdRules);
                s_ObjectsRuleMap.Add(gameObject.GetInstanceID(),createdRules);
                s_GameObjectRules.AddRange(createdRules);
            }

            if (s_GameObjectRules.Count == 0)
                return;

            if (EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.VisionOS &&
                PolySpatialSettings.instance.PolySpatialValidationOption == PolySpatialSettings.ValidationOption.EnabledForAllPlatforms)
            {
                BuildValidator.AddRules(EditorUserBuildSettings.selectedBuildTargetGroup, s_GameObjectRules);
                s_BuildValidatorCopy.AddRules(EditorUserBuildSettings.selectedBuildTargetGroup, s_GameObjectRules);
            }

            BuildValidator.AddRules(BuildTargetGroup.VisionOS, s_GameObjectRules);
            s_BuildValidatorCopy.AddRules(BuildTargetGroup.VisionOS, s_GameObjectRules);
        }

        static IComponentRuleCreator GetRuleCreator(Type componentType)
        {
            return GetCachedValue(componentType, s_TypeRuleCreatorList, s_CachedRuleCreators);
        }

        static void UpdateRuleFailures(bool repaintHierarchy = false)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            s_BuildValidatorCopy.GetCurrentValidationIssues(s_RuleFailures, buildTargetGroup);

            CacheObjectFailures();

            // Issues that don't add/remove objects will not trigger Hierarchy repaint. When needed, we'll call RepaintHierarchyWindow to ensure consistency
            if (repaintHierarchy)
                EditorApplication.RepaintHierarchyWindow();

            OnValidateRules?.Invoke();
        }

        static void CacheObjectFailures()
        {
            s_RootsNextChildFailureMap.Clear();
            s_GameObjectsFailuresSet.Clear();
            s_GameObjectsFailuresList.Clear();

            foreach (var component in s_Components)
            {
                if (component == null)
                    continue;

                var gameObjectID = component.gameObject.GetInstanceID();
                if (!ObjectHasRuleFailures(gameObjectID) && !ComponentHasRuleFailures(component.GetInstanceID()))
                    continue;

                // Cache the root object id of the failure rule
                var transform = component.transform;
                if (transform.parent != null)
                {
                    var rootObjectID = transform.root.gameObject.GetInstanceID();
                    if (!s_RootsNextChildFailureMap.ContainsKey(rootObjectID))
                        s_RootsNextChildFailureMap.Add(rootObjectID, gameObjectID);
                }

                // Cache the game object id with failure rules, used to get the next (or previous) failure game object in the scene
                if (!s_GameObjectsFailuresSet.Contains(gameObjectID))
                {
                    s_GameObjectsFailuresSet.Add(gameObjectID);
                    s_GameObjectsFailuresList.Add(gameObjectID);
                }
            }
        }

        /// <summary>
        /// Associate the given component and rules.
        /// </summary>
        /// <param name="component">The component to associate.</param>
        /// <param name="rules">The rules to associate.</param>
        /// <seealso cref="IsValidGameObject"/>
        /// <seealso cref="AutoFixGameObject"/>
        /// <seealso cref="GetComponentRulesFailures"/>
        static void Associate(Component component, IEnumerable<BuildValidationRule> rules)
        {
            if (!s_ComponentRulesMap.TryGetValue(component.GetInstanceID(), out var componentRules))
            {
                componentRules = new List<BuildValidationRule>();
                s_ComponentRulesMap.Add(component.GetInstanceID(), componentRules);
            }
            componentRules.AddRange(rules);

            if (!s_GameObjectComponentsMap.TryGetValue(component.gameObject.GetInstanceID(), out var goComponents))
            {
                goComponents = new List<int>();
                s_GameObjectComponentsMap.Add(component.gameObject.GetInstanceID(), goComponents);
            }
            goComponents.Add(component.GetInstanceID());
        }

        static bool ComponentHasRuleFailures(int instanceID)
        {
            if (!s_ComponentRulesMap.TryGetValue(instanceID, out var rules))
                return false;

            foreach (var rule in rules)
            {
                if (s_RuleFailures.Contains(rule))
                    return true;
            }

            return false;
        }

        static bool ObjectHasRuleFailures(int instanceID)
        {
            if (!s_ObjectsRuleMap.TryGetValue(instanceID, out var rules))
                return false;

            foreach (var rule in rules)
            {
                if (s_RuleFailures.Contains(rule))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns if the given Game Object only has valid components for PolySpatial.
        /// </summary>
        /// <param name="instanceID">The Game Object instance ID to validate.</param>
        /// <returns>Returns <see langword="true"/> if the given Game Object only has valid components for PolySpatial. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="UnityEngine.Object.GetInstanceID"/>
        internal static bool IsValidGameObject(int instanceID)
        {
            return !s_GameObjectsFailuresSet.Contains(instanceID);
        }

        /// <summary>
        /// Returns whether there are rules failing for any component in the given object hierarchy.
        /// </summary>
        /// <param name="instanceID">The instance ID of the root GameObject to validate.</param>
        /// <returns>Returns <see langword="true"/> if the given hierarchy only has valid components for PolySpatial. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="UnityEngine.Object.GetInstanceID"/>
        internal static bool IsRootObjectWithFailureChildren(int instanceID)
        {
            return s_RootsNextChildFailureMap.ContainsKey(instanceID);
        }

        /// <summary>
        /// Tries to automatically fix the issues associated to the given Game Object, if possible.
        /// </summary>
        /// <param name="gameObjectID">The GameObject instance ID to fix.</param>
        /// <seealso cref="UnityEngine.Object.GetInstanceID"/>
        internal static void AutoFixGameObject(int gameObjectID)
        {
            if (s_ObjectsRuleMap.TryGetValue(gameObjectID, out var goRules))
            {
                foreach (var goRule in goRules)
                {
                    if (s_RuleFailures.Contains(goRule) && goRule.FixItAutomatic)
                        s_FixAllList.Add(goRule);
                }
            }

            if (s_GameObjectComponentsMap.TryGetValue(gameObjectID, out var componentIds))
            {
                foreach (var componentId in componentIds)
                {
                    if (!s_ComponentRulesMap.TryGetValue(componentId, out var componentRules))
                        continue;

                    foreach (var rule in componentRules)
                    {
                        if (s_RuleFailures.Contains(rule) && rule.FixItAutomatic)
                            s_FixAllList.Add(rule);
                    }
                }
            }

            if (s_FixAllList.Count > 0)
            {
                BuildValidator.FixIssues(s_FixAllList, k_FixObjectProgressBarTitle);
                s_FixAllList.Clear();

                UpdateRuleFailures(true);
            }
        }

        /// <summary>
        /// Returns the rules associated with the given component that are currently failing.
        /// </summary>
        /// <param name="componentID">The component to get the issues.</param>
        /// <param name="componentIssues">The list to return the issues.</param>
        internal static void GetComponentRulesFailures(int componentID, List<BuildValidationRule> componentIssues)
        {
            if (!s_ComponentRulesMap.TryGetValue(componentID, out var rules))
                return;

            foreach (var rule in rules)
            {
                if (s_RuleFailures.Contains(rule))
                    componentIssues.Add(rule);
            }
        }

        /// <summary>
        /// Returns all component rules associated with the given Game Object that are currently failing.
        /// </summary>
        /// <param name="gameObjectID">The game object to get the issues.</param>
        /// <param name="gameObjectIssues">The list to return the issues.</param>
        internal static void GetGameObjectComponentRulesFailures(int gameObjectID, List<BuildValidationRule> gameObjectIssues)
        {
            if (!s_GameObjectComponentsMap.TryGetValue(gameObjectID, out var componentIds))
                return;

            foreach (var componentId in componentIds)
                GetComponentRulesFailures(componentId, gameObjectIssues);
        }

        /// <summary>
        /// Returns the non-component rules associated with the given object that are currently failing.
        /// </summary>
        /// <param name="objectId">The objects to get the issues.</param>
        /// <param name="objectIssues">The list to return the issues.</param>
        internal static bool TryGetObjectRules(int objectId, List<BuildValidationRule> objectIssues)
        {
            if (!s_ObjectsRuleMap.TryGetValue(objectId, out var rules))
                return false;

            foreach (var rule in rules)
            {
                if (s_RuleFailures.Contains(rule))
                    objectIssues.Add(rule);
            }

            return objectIssues.Count > 0;
        }

        /// <summary>
        /// Returns the messages associated with the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to get the messages.</param>
        /// <returns>Returns the messages for the given <paramref name="type"/>.</returns>
        internal static ITypeMessage[] GetTypeMessages(Type type)
        {
            return GetCachedValue(type, s_TypeMessagesList, s_CachedTypeMessages);
        }

        /// <summary>
        /// Searches and returns the value associated with the <paramref name="type"/> (polymorphism is considered) in the
        /// list <paramref name="typeValueList"/>, the search (the given <see cref="type"/> and returned value) are
        /// cached in <paramref name="cachedSearchMap"/> for fast searching on later calls.
        /// </summary>
        /// <param name="type">The type to search for its associated value.</param>
        /// <param name="typeValueList">The list to search for the value.</param>
        /// <param name="cachedSearchMap">The map to cache the search, used in later calls.</param>
        /// <typeparam name="T">The <see cref="Type"/> of the value.</typeparam>
        /// <returns>Returns the value in <see cref="typeValueList"/> associated with the given <see cref="type"/>. Can return <see langword="null"/>.</returns>
        static T GetCachedValue<T>(Type type, IEnumerable<KeyValuePair<Type, T>> typeValueList, IDictionary<Type, T> cachedSearchMap)
        {
            // Try the cached map first
            if (cachedSearchMap.TryGetValue(type, out var value))
                return value;

            // Do a linear search in the list
            foreach (var (itemType, itemValue) in typeValueList)
            {
                if (itemType.IsAssignableFrom(type))
                {
                    value = itemValue;
                    break;
                }
            }

            // It's possible to cache a null value and this is desired
            cachedSearchMap.Add(type, value);
            return value;
        }

        /// <summary>
        /// Class containing code copied from <see cref="BuildValidator"/>, required to search for rules failing validation.
        /// </summary>
        // todo LXR-785: Remove once we have public access to retrieve failures from the validator in the CoreUtils package.
        class BuildValidatorCopy
        {
            readonly Dictionary<BuildTargetGroup, List<BuildValidationRule>> m_PlatformRules = new();

            // Copied from BuildValidator.GetCurrentValidationIssues
            internal void GetCurrentValidationIssues(HashSet<BuildValidationRule> failures, BuildTargetGroup buildTargetGroup)
            {
                failures.Clear();
                if (!m_PlatformRules.TryGetValue(buildTargetGroup, out var rules))
                    return;

                var inPrefabStage = PrefabStageUtility.GetCurrentPrefabStage() != null;
                foreach (var validation in rules)
                {
                    // If current scene is prefab isolation do not run scene validation
                    if (inPrefabStage && validation.SceneOnlyValidation)
                        continue;

                    if (validation.CheckPredicate == null)
                    {
                        failures.Add(validation);
                    }
                    else
                    {
                        var isEnabled = validation.IsRuleEnabled.Invoke();
                        var checkPredicate = validation.CheckPredicate.Invoke();
                        if (isEnabled && !checkPredicate)
                            failures.Add(validation);
                    }
                }
            }

            // Copied from BuildValidator.AddRules
            internal void AddRules(BuildTargetGroup group, IEnumerable<BuildValidationRule> rules)
            {
                if (m_PlatformRules.TryGetValue(group, out var groupRules))
                {
                    groupRules.AddRange(rules);
                }
                else
                {
                    groupRules = new List<BuildValidationRule>(rules);
                    m_PlatformRules.Add(group, groupRules);
                }
            }
        }

        /// <summary>
        /// Gets the previous object ID with failing rules in the scene.
        /// </summary>
        /// <param name="instanceID">The instance ID of the object used as reference.</param>
        /// <returns>Returns the previous object ID with failing rules in the scene, the previous object with a warning icon in the Inspector window.</returns>
        internal static int GetPreviousFailureObjectID(int instanceID)
        {
            var index = s_GameObjectsFailuresList.IndexOf(instanceID);
            if (index == -1)
                return 0;

            index--;
            if (index < 0)
                index = s_GameObjectsFailuresList.Count - 1;

            return s_GameObjectsFailuresList[index];
        }

        /// <summary>
        /// Gets the next object ID with failing rules in the scene.
        /// If the given <paramref name="instanceID"/> is a root game object, then gets its next child that has a failing rule.
        /// </summary>
        /// <param name="instanceID">The instance ID of the object used as reference.</param>
        /// <returns>Returns the next object ID with failing rules in the scene, the next object with an active warning icon in the Inspector window.</returns>
        internal static int GetNextFailureObjectID(int instanceID)
        {
            if (s_RootsNextChildFailureMap.TryGetValue(instanceID, out var nextInstanceID))
                return nextInstanceID;

            var index = s_GameObjectsFailuresList.IndexOf(instanceID);
            if (index == -1)
                return 0;

            index = (index + 1) % s_GameObjectsFailuresList.Count;
            return s_GameObjectsFailuresList[index];
        }
    }
}
