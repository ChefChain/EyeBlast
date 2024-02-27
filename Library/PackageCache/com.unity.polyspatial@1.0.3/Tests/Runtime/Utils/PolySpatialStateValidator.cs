using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = System.Object;
using UnityObject = UnityEngine.Object;

/// <summary>
/// This class is used to fetch and compare state between clientSim and hostPlatform layer GameObjects. A collection of
/// tracked GameObjects are retrieved from the hostPlatform layer. The clientSim layer GameObjects is compared against
/// the hostPlatform layer GameObjects, checking components and their properties, and generating diff String.
/// There are 2 main components to this class:
/// 1. GameObject State Retrieval - when invoked, retrieves all supported GameObject and GameObject component properties
///    to generate 2 GameObjectNode collections (for clientSim and hostPlatform layers).
/// 2. Diff String Generator - when invoked, compares collection of GameObjectNodes and generates a diff String for all
///    non-matching values. Note that comparisons may not always be 1-1 as some conversion and rounding is expected in
///    PolySpatial hostPlatform GameObjects.
/// There are 3 facets of development to this class:
/// 1. Supporting additional GameObject components and properties - currently a subset of all PolySpatial supported
///    components are tracked and validated. The current goal is to support all components tracked via PolySpatial tracker classes.
/// 2. Extend component property validation - Some supported components may not fully validate all component properties,
///    or validations are high level only (checking only for the presence of the component, not comparing values).
/// 3. Supporting additional platforms - currently only UnityPolySpatialNativePlatform is supported
///
/// There are known issues with certain component properties that generate diffs which should be tracked by a JIRA (a
/// JIRA link should be included above the relevant lines of code). If missing, please add a JIRA or reach out to the
/// PolySpatial QA and/or PolySpatial Core team.
/// </summary>
public class PolySpatialStateValidator
{
    // collection of all properties a GameObjectNode may have
    private const string k_ActiveProperty = "active";
    private const string k_LocalPositionProperty = "localPosition";
    private const string k_LocalRotationProperty = "localRotation";
    private const string k_LocalScaleProperty = "localScale";
    private const string k_MeshFilterMeshProperty = "MeshFilter.mesh";
    private const string k_MeshRendererMaterialsProperty = "MeshRenderer.materials";
    private const string k_CanvasProperty = "Canvas";
    private const string k_CanvasRendererProperty = "CanvasRenderer";
    private const string k_RectTransformProperty = "RectTransform";
    private const string k_SkinnedMeshRendererProperty = "skinnedMeshRenderer";

    private static readonly Dictionary<string, List<string>> k_TransformedProperties = new()
    {
        {k_CanvasProperty, new(){k_MeshFilterMeshProperty, k_MeshRendererMaterialsProperty}},
        {k_CanvasRendererProperty, new(){k_MeshFilterMeshProperty, k_MeshRendererMaterialsProperty}},
        {k_RectTransformProperty, new(){k_LocalPositionProperty, k_LocalRotationProperty, k_LocalScaleProperty}}
    };
    UnitySceneGraph m_UnitySceneGraph;
    Dictionary<PolySpatialInstanceID, PolySpatialInstanceID> m_HostPlatformIdToClientSimId = new();
    Dictionary<PolySpatialInstanceID, string> m_ClientSimIdToName;
    private const string k_DecimalFormatter = "N10";
    private const float k_MaxFloatDelta = 1e-5f;
    // special cases for known failing property value comparisons
    // TODO: XRQA-1020 - remove some or all Material properties to omit diffing once resolved
    private static List<string> s_MaterialPropertiesToSkip = new()
    {
        "_Glossiness",
        "_GlossMapScale",
        "_GlossyReflections",
        "_Parallax",
        "_Blend",
        "_SpecColor",
        "_BumpScale",
        // TODO: LXR-1621 - remove if/when properties are supported
        "_SpecularHighlights",
        "_ReceiveShadows",
        "_EnvironmentReflections"
    };
    // custom shader used by PolySpatialSamples project
    private const string k_ShaderSGVertColorVeg = "Shader Graphs/vertcolor_veg";
    private const string k_ShaderSGTestGraph = "Shader Graphs/TestGraph";
    private const string k_ShaderSGSamplerState = "Shader Graphs/SamplerState";
    private const string k_ShaderSGCustomInterpolators = "Shader Graphs/CustomInterpolators";
    private const string k_ShaderSGUnlit = "Shader Graphs/Unlit";
    private const string k_ShaderSGViewRay = "Shader Graphs/ViewRay";
    private const string k_ShaderSGAdditiveAlpha = "Shader Graphs/AdditiveAlpha";
    private const string k_ShaderSGScriptedColor = "Shader Graphs/ScriptedColor";
    private const string k_ShaderSGNormalMapped = "Shader Graphs/NormalMapped";
    private const string k_ShaderSGDynamicTexCoords = "Shader Graphs/DynamicTexCoords";
    private const string k_ShaderSGVertexStage = "Shader Graphs/VertexStage";
    private const string k_ShaderSGScreenFresnel = "Shader Graphs/ScreenFresnel";
    private const string k_ShaderSGVertexTexture = "Shader Graphs/VertexTexture";
    private const string k_ShaderSGNonPropertyTextures = "Shader Graphs/NonPropertyTextures";
    private const string k_ShaderSGScaledWorldPosition = "Shader Graphs/ScaledWorldPosition";

    private readonly List<string> k_NoColorShaders = new()
    {
        k_ShaderSGVertColorVeg,
        k_ShaderSGTestGraph,
        k_ShaderSGSamplerState,
        k_ShaderSGCustomInterpolators,
        k_ShaderSGUnlit,
        k_ShaderSGViewRay,
        k_ShaderSGAdditiveAlpha,
        k_ShaderSGScriptedColor,
        k_ShaderSGNormalMapped,
        k_ShaderSGDynamicTexCoords,
        k_ShaderSGVertexStage,
        k_ShaderSGScreenFresnel,
        k_ShaderSGVertexTexture,
        k_ShaderSGNonPropertyTextures,
        k_ShaderSGScaledWorldPosition,
        MaterialShaders.k_TextShaderName
    };
    private readonly List<string> k_NoTextureShaders = new()
    {
        k_ShaderSGVertColorVeg,
        k_ShaderSGTestGraph,
        k_ShaderSGSamplerState,
        k_ShaderSGCustomInterpolators,
        k_ShaderSGUnlit,
        k_ShaderSGViewRay,
        k_ShaderSGAdditiveAlpha,
        k_ShaderSGScriptedColor,
        k_ShaderSGNormalMapped,
        k_ShaderSGDynamicTexCoords,
        k_ShaderSGVertexStage,
        k_ShaderSGScreenFresnel,
        k_ShaderSGVertexTexture,
        k_ShaderSGNonPropertyTextures,
        k_ShaderSGScaledWorldPosition
    };
    private static Dictionary<string, List<string>> s_ShaderPropertiesToSkip = new()
    {
        {
            MaterialShaders.k_LitShaderName,
            new List<string>()
            {
                "_Color",
                "_DetailNormalMap_TexelSize",
                "_EmissionColor", // TODO: LXR-1569 - ignore conditionally only if Material.emissions flag is not set
                "anisoLevel" // PolySpatial does not transfer anisotropic filtering level
            }
        }
    };

    // setting to true enables additional, high performance cost comparisons for certain components, such as Textures
    private bool EnableExtraValidation { get; }
    private StringBuilder DiffSb { get; }

    internal const string kPlaymodeTestControllerName = "Code-based tests runner";
    public static List<string> s_MarkedForDestroy = new();
    private Regex m_PolySpatialNameRegex = new Regex(@"PolySpatial iid (?<clientSimId>-?[0-9]+)(\:[0-9]+)");
    private const string k_ClientSimIdGroupName = "clientSimId";

    /// <summary>
    /// Captures a GameObject's state as well as linking clientSim and hostPlatform GameObjects and their respective parents.
    /// GameObject properties define its state, capturing simple (string, int, etc) or complex (Mesh, Material) types.
    /// </summary>
    public class GameObjectNode
    {
        public string clientSimName;
        public string hostPlatformName;
        public PolySpatialInstanceID clientSimId;
        public PolySpatialInstanceID hostPlatformId;
        public PolySpatialInstanceID clientSimParentId;
        public PolySpatialInstanceID hostPlatformParentId;

        public Dictionary<string, Object> properties = new();

        public bool Equivalent(GameObjectNode otherGameObjectNode)
        {
            if (clientSimName != otherGameObjectNode.clientSimName)
                return false;

            if (!clientSimId.Equals(otherGameObjectNode.clientSimId))
                return false;

            if (!hostPlatformId.Equals(otherGameObjectNode.hostPlatformId))
                return false;

            if (!hostPlatformParentId.Equals(otherGameObjectNode.hostPlatformParentId))
                return false;

            if (properties.Count != otherGameObjectNode.properties.Count)
                return false;

            if (properties.Keys.Except(otherGameObjectNode.properties.Keys).ToList().Count > 0)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Captures SkinnedMeshRenderer properties used for state validation; allows for cloning of referenced values
    /// such as bones, to get a snapshot of SMR properties for a given frame.
    /// </summary>
    public class SkinnedMeshRendererNode
    {
        public TransformNode rootBone;
        public TransformNode[] bones;
        public Mesh sharedMesh;
        public SkinQuality quality;

        /* Properties unused by PolySpatial:
         * updateWhenOffscreen
         * skinnedMotionVectors
         */

        public SkinnedMeshRendererNode(SkinnedMeshRenderer smr)
        {
            Clone(smr);
        }

        private void Clone(SkinnedMeshRenderer smr)
        {
            // clone bone values
            var smrBones = smr.bones;
            bones = new TransformNode[smrBones.Length];
            for (var i = 0; i < bones.Length; i++)
            {
                Vector3 tPos = new()
                {
                    x = smrBones[i].position.x,
                    y = smrBones[i].position.y,
                    z = smrBones[i].position.z
                };

                Quaternion tRot = new()
                {
                    w = smrBones[i].rotation.w,
                    x = smrBones[i].rotation.x,
                    y = smrBones[i].rotation.y,
                    z = smrBones[i].rotation.z
                };

                Vector3 tScale = new()
                {
                    x = smrBones[i].localScale.x,
                    y = smrBones[i].localScale.y,
                    z = smrBones[i].localScale.z
                };

                TransformNode t = new()
                {
                    name = smrBones[i].name,
                    gameObjectInstanceId = smrBones[i].gameObject.GetInstanceID(),
                    position = tPos,
                    rotation = tRot,
                    localScale = tScale
                };

                bones[i] = t;
            }

            sharedMesh = UnityObject.Instantiate(smr.sharedMesh);
            var smrRootBone = smr.rootBone;
            var position = smrRootBone.position;

            Vector3 rPos = new()
            {
                x = position.x,
                y = position.y,
                z = position.z
            };
            var rotation = smrRootBone.rotation;
            Quaternion rRot = new()
            {
                w = rotation.w,
                x = rotation.x,
                y = rotation.y,
                z = rotation.z

            };
            var localScale = smrRootBone.localScale;
            Vector3 rScale = new()
            {
                x = localScale.x,
                y = localScale.y,
                z = localScale.z
            };

            rootBone = new TransformNode()
            {
                name = smrRootBone.name,
                gameObjectInstanceId = smrRootBone.gameObject.GetInstanceID(),
                position = rPos,
                rotation = rRot,
                localScale = rScale
            };

            quality = smr.quality;
        }
    }

    /// <summary>
    /// Captures Transform properties used for state validation; allows for cloning Transform values and removing any links to
    /// GameObject references and capturing a snapshot of Transform properties for a given frame.
    /// </summary>
    public class TransformNode
    {
        public string name;
        public int gameObjectInstanceId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }

    public PolySpatialStateValidator(bool enableExtraValidation = false)
    {
        EnableExtraValidation = enableExtraValidation;
        DiffSb = new();
        // TODO - LXR-701: support non native platforms
        var backend = PolySpatialCore.LocalBackend as PolySpatialUnityBackend;
        Assert.IsNotNull(backend);
        m_UnitySceneGraph = backend.SceneGraph;
        Assert.IsNotNull(m_UnitySceneGraph);
    }

    private static void PopulateClientSimData(GameObject go, GameObjectNode gameObjectNode)
    {
        gameObjectNode.clientSimId = PolySpatialInstanceID.For(go);
        gameObjectNode.clientSimName = go.name;
        gameObjectNode.clientSimParentId = (go.transform.parent == null)
            ? PolySpatialInstanceID.None
            : PolySpatialInstanceID.For(go.transform.parent.gameObject);
    }

    private void PopulateHostPlatformData(GameObject go, GameObjectNode gameObjectNode)
    {
        gameObjectNode.hostPlatformId = PolySpatialInstanceID.For(go);
        gameObjectNode.hostPlatformName = go.name;
        gameObjectNode.hostPlatformParentId = (go.transform.parent == null)
            ? PolySpatialInstanceID.None
            : PolySpatialInstanceID.For(go.transform.parent.gameObject);

        if (!m_HostPlatformIdToClientSimId.TryGetValue(gameObjectNode.hostPlatformId, out gameObjectNode.clientSimId))
        {
            gameObjectNode.clientSimId = PolySpatialInstanceID.None;
        }

        if (!m_ClientSimIdToName.TryGetValue(gameObjectNode.clientSimId, out gameObjectNode.clientSimName))
        {
            gameObjectNode.clientSimName = "<NOT FOUND>";
        }

        if (gameObjectNode.hostPlatformParentId.IsValid()
            && !m_HostPlatformIdToClientSimId.TryGetValue(gameObjectNode.hostPlatformParentId, out gameObjectNode.clientSimParentId))
        {
            gameObjectNode.clientSimParentId = PolySpatialInstanceID.None;
        }
    }

    private void PopulateObjectProperties(GameObject obj, GameObjectNode objNode, bool isClientSimObject)
    {
        if (isClientSimObject)
            PopulateClientSimData(obj, objNode);
        else
            PopulateHostPlatformData(obj, objNode);

        objNode.properties[k_ActiveProperty] = obj.activeInHierarchy.ToString();

        // Add transform data selectively unless extra validation flag is set
        if (EnableExtraValidation || obj.transform.localPosition != Vector3.zero)
            objNode.properties[k_LocalPositionProperty] = obj.transform.localPosition;
        if (EnableExtraValidation || obj.transform.localRotation != Quaternion.identity)
            objNode.properties[k_LocalRotationProperty] = obj.transform.localRotation;
        if (EnableExtraValidation || obj.transform.localScale != Vector3.one)
            objNode.properties[k_LocalScaleProperty] = obj.transform.localScale;

        //if (obj.TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer))
        //    objNode.properties[k_kinnedMeshRendererProperty] = new SkinnedMeshRendererNode(skinnedMeshRenderer);
        if (obj.TryGetComponent<Canvas>(out var canvas))
            objNode.properties[k_CanvasProperty] = canvas;
        if (obj.TryGetComponent<CanvasRenderer>(out var canvasRenderer))
            objNode.properties[k_CanvasRendererProperty] = canvasRenderer;
        if (obj.TryGetComponent<RectTransform>(out var rectTransform))
            objNode.properties[k_RectTransformProperty] = rectTransform;
    }

    private List<UnityObject> ReportAssets(GameObject obj, GameObjectNode objNode, bool isClientSimObject)
    {
        List<UnityObject> unregisteredAssets = new();

        if (obj.TryGetComponent<MeshFilter>(out var mf))
        {
            // note: clone the Mesh to capture the state at a given frame. If not cloned, ref value continues to update
            // which may cause diffs when comparing previous frame state to current frame state.
            var mesh = mf.sharedMesh;
            GetAssetOrReportUnregistered(
                "MeshRenderer.sharedMesh",
                mesh,
                objNode,
                unregisteredAssets,
                isClientSimObject);

            if (mesh != null)
                objNode.properties[k_MeshFilterMeshProperty] = UnityObject.Instantiate(mesh);
        }

        if (obj.TryGetComponent<MeshRenderer>(out var mr))
        {
            // Clone the materials in order to capture their properties at a given frame.
            var clonedMaterials = new Material[mr.sharedMaterials.Length];
            for (int i = 0; i < mr.sharedMaterials.Length; ++i)
            {
                var material = mr.sharedMaterials[i];
                GetAssetOrReportUnregistered(
                    $"MeshRenderer.sharedMaterials[{i}]",
                    material,
                    objNode,
                    unregisteredAssets,
                    isClientSimObject);

                clonedMaterials[i] = UnityObject.Instantiate(material);
            }

            objNode.properties[k_MeshRendererMaterialsProperty] = clonedMaterials;
        }

        return unregisteredAssets;
    }

    /// <summary>
    /// Returns the current state of ClientSim and HostPlatform GameObject data, mapping their QIDs to GameObject data
    /// encapsulated in GameObjectNode instances.
    /// Each collection of entries represents found GameObjects in their respective (clientSim or PolySpatial/HostPlatform) layer.
    /// Also fetches any unregistered assets found for each layer.
    /// </summary>
    /// <param name="clientSimState">collection of all clientSim layer PolySpatialInstanceIds and GameObjectNodes found</param>
    /// <param name="hostPlatformState">collection of all hostPlatform layer PolySpatialInstanceIds and GameObjectNodes found.
    /// Note that hostPlatform layer state dictionary is still indexed by clientSim GO PolySpatialInstanceIds.</param>
    /// <param name="unregisteredClientSimAssets">collection of all unregistered clientSim Assets found</param>
    /// <param name="unregisteredHostPlatformAssets">collection of all unregistered hostPlatform Assets found</param>
    public void GetState(
        out Dictionary<PolySpatialInstanceID, GameObjectNode> clientSimState,
        out Dictionary<PolySpatialInstanceID, GameObjectNode> hostPlatformState,
        out List<UnityEngine.Object> unregisteredClientSimAssets,
        out List<UnityEngine.Object> unregisteredHostPlatformAssets)
    {
        clientSimState = new();
        hostPlatformState = new();
        unregisteredClientSimAssets = new();
        unregisteredHostPlatformAssets = new();

        m_HostPlatformIdToClientSimId.Clear();

        // Get map of all tracked clientSim IDs => hostPlatform GOs so we can loop through GOs and map clientSim ID => hostPlatform ID
        var clientSimIdToHostPlatformGOs = m_UnitySceneGraph.GetSimIDToScenegraphGOs();
        foreach (var clientSimIdToHostPlatformGO in clientSimIdToHostPlatformGOs)
        {
            PolySpatialInstanceID clientSimId = clientSimIdToHostPlatformGO.Key;
            PolySpatialInstanceID hostPlatformId = PolySpatialInstanceID.For(clientSimIdToHostPlatformGO.Value);
            m_HostPlatformIdToClientSimId[hostPlatformId] = clientSimId;
        }
        // Get all clientSim GO names for each clientSim ID
        m_ClientSimIdToName = m_UnitySceneGraph.GetSimInstanceIDsToNames();

        // Fetch all GameObjects to classify them as either clientSim GO or hostPlatform GO
        var allObjs = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.InstanceID);

        var allObjIds = allObjs.Select(go => go.GetInstanceID()).ToHashSet();
        foreach (var obj in allObjs)
        {
            if (obj.name.StartsWith("Mirror Main Camera"))
                continue;

            var objNode = new GameObjectNode();

            if (IsHostPlatformObject(obj))
            {
                PopulateObjectProperties(obj, objNode, false);
                // Special-case skip the test controller itself
                if (objNode.clientSimName == kPlaymodeTestControllerName)
                    continue;
                unregisteredHostPlatformAssets = ReportAssets(obj, objNode, false);

                // Results are always keyed by simId, regardless of whether the state is for the sim or the renderer. Duplicate
                // keys are an error
                hostPlatformState.Add(objNode.clientSimId, objNode);
            }
            else if (IsMarkedForDestroy(obj, allObjIds))
            {
                // this works for backing GOs only because each backing GO name should be unique to the GO its tracking
                s_MarkedForDestroy.Add(obj.name);
            }
            else if (IsClientSimObject(obj))
            {
                PopulateObjectProperties(obj, objNode, true);
                // Special-case skip the test controller itself
                if (objNode.clientSimName == kPlaymodeTestControllerName)
                    continue;
                unregisteredClientSimAssets = ReportAssets(obj, objNode, true);

                // Results are always keyed by simId, regardless of whether the state is for the sim or the renderer. Duplicate
                // keys are an error
                clientSimState.Add(objNode.clientSimId, objNode);
            }
        }
    }

    /// <summary>
    /// Destroys the cloned assets in the specified state map and clears it.
    /// </summary>
    /// <param name="state">collection of all PolySpatialInstanceIds and GameObjectNodes
    /// (can be null, in which case this is a no-op)</param>
    public static void DestroyState(Dictionary<PolySpatialInstanceID, GameObjectNode> state)
    {
        if (state == null)
            return;

        foreach (var objNode in state.Values)
        {
            if (objNode.properties.TryGetValue(k_MeshFilterMeshProperty, out var mesh))
                UnityObject.Destroy((Mesh)mesh);

            if (objNode.properties.TryGetValue(k_MeshRendererMaterialsProperty, out var materials))
            {
                foreach (var material in (Material[])materials)
                {
                    UnityObject.Destroy(material);
                }
            }

            if (objNode.properties.TryGetValue(k_SkinnedMeshRendererProperty, out var smr))
            {
                UnityObject.Destroy(((SkinnedMeshRendererNode)smr).sharedMesh);
            }
        }
        state.Clear();
    }

    private bool IsMarkedForDestroy(GameObject obj, HashSet<int> allObjIds)
    {
        // determine if obj is a backing GO whose clientSim GO has been destroyed
        if (!m_UnitySceneGraph.IsUnitySceneGraphObject(obj.GetInstanceID()) && m_PolySpatialNameRegex.IsMatch(obj.name))
        {
            Match match = m_PolySpatialNameRegex.Match(obj.name);
            var matchGroup = match.Groups;
            var matchClientSimId = matchGroup[k_ClientSimIdGroupName].Value;

            // validate the clientSimId does not exist
            if(int.TryParse(matchClientSimId, out int clientSimId) && !allObjIds.Contains(clientSimId))
                return true;
        }

        return false;
    }

    internal static bool IsClientSimObject(GameObject go)
    {
        if (go == null)
            return false;

        // TODO: LXR-1469 - update once object tracking issue is resolved

        // Client side objects that are in the PolySpatial layer
        // are not tracked and so will not have matching items
        // in the host list.
        if (go.layer == LayerMask.NameToLayer(PolySpatialUnityBackend.PolySpatialLayerName))
            return false;

        // Can't use GameObjectTracker.IsTrackedGameObjectInstanceID because we may not have actually
        // tracked it yet, due to tests
        return GameObjectTracker.ShouldTrackGameObjectStatic(go);
    }

    private bool IsHostPlatformObject(GameObject go)
    {
        if (go == null)
            return false;

        // this has no equivalent on the clientSim side, but is a hostPlatform object
        if (m_UnitySceneGraph.IsRootGameObject(go))
            return false;

        return m_UnitySceneGraph.IsUnitySceneGraphObject(go.GetInstanceID());
    }

    /// <summary>
    /// Fetches assetIDs and stores them in the target/owner Object. Reports any unregistered asset.
    /// </summary>
    /// <param name="assetPropertyName">Asset property name</param>
    /// <param name="asset">Asset object</param>
    /// <param name="owner">Owning object storing the asset ID</param>
    /// <param name="unregisteredAssets">Collection of unregistered assets found</param>
    /// <param name="isClientSimAsset">Set to true if Asset ID is associated with a clientSim layer Object</param>
    private void GetAssetOrReportUnregistered(
        string assetPropertyName,
        UnityEngine.Object asset,
        GameObjectNode owner,
        List<UnityObject> unregisteredAssets,
        bool isClientSimAsset)
    {
        var assetId = isClientSimAsset
            ? UnitySceneGraphAssetManager.Shared.GetRegisteredAssetID(asset)
            : PolySpatialCore.LocalAssetManager.GetRegisteredAssetID(asset);

        if (!assetId.IsValid())
            unregisteredAssets.Add(asset);
        else
            owner.properties[assetPropertyName] = assetId.ToString();
    }

    /// <summary>
    /// Simple null check to ensure both instances are non-null.
    /// </summary>
    /// <param name="clientSimName">Name of the clientSim GameObject</param>
    /// <param name="clientSimId">PolySpatialInstanceId string value for the clientSim GameObject</param>
    /// <param name="propertyKey">Tracked GameObject property name</param>
    /// <param name="clientSimObj">clientSim layer Object instance</param>
    /// <param name="hostPlatformObj">HostPlatform layer Object instance</param>
    /// <returns>true if both Objects are not null, false otherwise</returns>
    private bool DiffNullCheck(string clientSimName, string clientSimId, string propertyKey, Object clientSimObj,
        Object hostPlatformObj)
    {
        if (clientSimObj == null && hostPlatformObj == null)
            return false;

        if (clientSimObj != null && hostPlatformObj != null)
            return true;

        var valueMismatchStr = "Property null check mismatch ("
                               + clientSimObj + " != "
                               + hostPlatformObj + ")";
        DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{propertyKey}: {valueMismatchStr}");

        return false;
    }

    /// <summary>
    /// Helper method to compare Textures for equality based on instance property values.
    /// TODO: XRQA-1012 - add and gate pixel-by-pixel texture comparison
    /// </summary>
    /// <param name="clientSimName">Name of the clientSim GameObject</param>
    /// <param name="clientSimId">PolySpatialInstance Id string value for the clientSim GameObject</param>
    /// <param name="clientSimKey">Tracked GameObject property name</param>
    /// <param name="clientSimTexture">clientSim layer Texture instance</param>
    /// <param name="hostPlatformTexture">HostPlatform layer Texture instance</param>
    private void DiffTextures(string clientSimName, string clientSimId, string clientSimKey, Texture clientSimTexture,
        Texture hostPlatformTexture, string shaderType)
    {
        if (!DiffNullCheck(clientSimName, clientSimId, clientSimKey, clientSimTexture, hostPlatformTexture))
            return;

        if (clientSimTexture.anisoLevel != hostPlatformTexture.anisoLevel)
        {
            // check if we can skip property
            if (!(s_ShaderPropertiesToSkip.TryGetValue(shaderType, out var propertiesToSkip) &&
                propertiesToSkip.Contains("anisoLevel")))
            {
                var valueMismatchStr = "Texture anisotropic filtering level mismatch ("
                                       + clientSimTexture.anisoLevel + " != "
                                       + hostPlatformTexture.anisoLevel + ")";
                DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
            }
        }

        if (!clientSimTexture.dimension.Equals(hostPlatformTexture.dimension))
        {
            var valueMismatchStr = "Texture dimensionality type mismatch ("
                                   + clientSimTexture.dimension + " != "
                                   + hostPlatformTexture.dimension + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (clientSimTexture.height != hostPlatformTexture.height)
        {
            var valueMismatchStr = "Texture height mismatch ("
                                   + clientSimTexture.height + " != "
                                   + hostPlatformTexture.height + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (clientSimTexture.width != hostPlatformTexture.width)
        {
            var valueMismatchStr = "Texture width mismatch ("
                                   + clientSimTexture.width + " != "
                                   + hostPlatformTexture.width + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (clientSimTexture.filterMode.CompareTo(hostPlatformTexture.filterMode) != 0)
        {
            var valueMismatchStr = "Texture filter mode mismatch ("
                                   + clientSimTexture.filterMode + " != "
                                   + hostPlatformTexture.filterMode + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (clientSimTexture.graphicsFormat.CompareTo(hostPlatformTexture.graphicsFormat) != 0)
        {
            var valueMismatchStr = "Texture graphics format mismatch ("
                                   + clientSimTexture.graphicsFormat + " != "
                                   + hostPlatformTexture.graphicsFormat + ")";
            Debug.Log(valueMismatchStr);
            // TODO: XRQA-1014 - enable diff logging once resolved
            // DiffSb.AppendLine($"'{clientSimName}'({simId}).{simKey}: {valueMismatchStr}");
        }

        if (clientSimTexture.mipmapCount != hostPlatformTexture.mipmapCount)
        {
            var valueMismatchStr = "Texture mipmap count mismatch ("
                                   + clientSimTexture.mipmapCount + " != "
                                   + hostPlatformTexture.mipmapCount + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (clientSimTexture.wrapMode != hostPlatformTexture.wrapMode)
        {
            var valueMismatchStr = "Texture wrap mode mismatch ("
                                   + clientSimTexture.wrapMode + " != "
                                   + hostPlatformTexture.wrapMode + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (clientSimTexture.wrapModeU != hostPlatformTexture.wrapModeU)
        {
            var valueMismatchStr = "Texture U wrap mode mismatch ("
                                   + clientSimTexture.wrapModeU + " != "
                                   + hostPlatformTexture.wrapModeU + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (clientSimTexture.wrapModeV != hostPlatformTexture.wrapModeV)
        {
            var valueMismatchStr = "Texture V wrap mode mismatch ("
                                   + clientSimTexture.wrapModeV + " != "
                                   + hostPlatformTexture.wrapModeV + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (clientSimTexture.wrapModeW != hostPlatformTexture.wrapModeW)
        {
            var valueMismatchStr = "Texture wrap mode mismatch ("
                                   + clientSimTexture.wrapModeW + " != "
                                   + hostPlatformTexture.wrapModeW + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }
    }

    private void DiffMaterialProperty(string clientSimName, string clientSimId, string clientSimKey, Material clientSimMaterial,
        Material hostPlatformMaterial, MaterialPropertyType materialPropertyType, string propName)
    {
        // determine if the material property should be ignored for the given shader type
        var materialShaderName = clientSimMaterial.shader.name;
        if (s_ShaderPropertiesToSkip.TryGetValue(materialShaderName, out var shaderProps))
        {
            if (shaderProps.Contains(propName))
                return;
        }

        string valueMismatchStr = null;
        switch (materialPropertyType)
        {
            case MaterialPropertyType.Float:
                var clientSimFloatValue = clientSimMaterial.GetFloat(propName);
                var hostPlatformFloatValue = hostPlatformMaterial.GetFloat(propName);
                // compare floats with an acceptable delta to account for precision loss from conversion
                if (!MathExtensions.ApproximatelyEqual(clientSimFloatValue, hostPlatformFloatValue, k_MaxFloatDelta))
                {
                    valueMismatchStr = $"Material.{propName} ({materialPropertyType}) value mismatch ("
                                           + clientSimFloatValue + " != "
                                           + hostPlatformFloatValue + ")";
                }

                break;
            case MaterialPropertyType.Int:
                var clientSimIntValue = clientSimMaterial.GetInt(propName);
                var hostPlatformIntValue = hostPlatformMaterial.GetInt(propName);
                if (clientSimIntValue != hostPlatformIntValue)
                {
                    valueMismatchStr = $"Material.{propName} ({materialPropertyType}) value mismatch ("
                                           + clientSimIntValue + " != "
                                           + hostPlatformIntValue + ")";
                }

                break;
            case MaterialPropertyType.Matrix:
                var clientSimMatrixValue = clientSimMaterial.GetMatrix(propName);
                var hostPlatformMatrixValue = hostPlatformMaterial.GetMatrix(propName);
                if (!clientSimMatrixValue.Equals(hostPlatformMatrixValue))
                {
                    valueMismatchStr = $"Material.{propName} ({materialPropertyType}) value mismatch ("
                                           + clientSimMatrixValue + " != "
                                           + hostPlatformMatrixValue + ")";
                }

                break;
            case MaterialPropertyType.Texture:
                var clientSimTextureValue = clientSimMaterial.GetTexture(propName);
                var hostPlatformTextureValue = hostPlatformMaterial.GetTexture(propName);
                // shader type validation is run elsewhere; retrieve shader type to pass to DiffTextures only
                var shaderType = clientSimMaterial.shader.name;
                DiffTextures(clientSimName, clientSimId, clientSimKey, clientSimTextureValue, hostPlatformTextureValue, shaderType);

                break;
            case MaterialPropertyType.Vector:
                // To avoid converting property from Color to Vector, check to see if its a Color property
                if (clientSimMaterial.HasColor(propName))
                {
                    Color32 clientSimColorValue = clientSimMaterial.GetColor(propName);
                    Color32 hostPlatformColorValue = hostPlatformMaterial.GetColor(propName);

                    if (!clientSimColorValue.Equals(hostPlatformColorValue))
                    {
                        valueMismatchStr = $"Material.{propName} ({materialPropertyType} / Color) value mismatch ("
                                               + clientSimColorValue + " != "
                                               + hostPlatformColorValue + ")";
                    }
                }
                else
                {
                    var clientSimVectorValue = clientSimMaterial.GetVector(propName);
                    var hostPlatformVectorValue = hostPlatformMaterial.GetVector(propName);

                    // compare vector float values with an acceptable delta to account for precision loss from conversion
                    if (!MathExtensions.ApproximatelyEqual(clientSimVectorValue, hostPlatformVectorValue, k_MaxFloatDelta))
                    {
                        valueMismatchStr = $"Material.{propName} ({materialPropertyType}) value mismatch ("
                                               + clientSimVectorValue + " != "
                                               + hostPlatformVectorValue + ")";
                    }
                }

                break;
            case MaterialPropertyType.ComputeBuffer:
                // PolySpatial does not support ComputeShaders; we can skip comparison for these
                break;
            case MaterialPropertyType.ConstantBuffer:
                // PolySpatial does not support ComputeShaders; we can skip comparison for these
                break;
            default:
                Debug.Log($"Unsupported Material property name {propName} of type {materialPropertyType}");
                break;
        }

        if (valueMismatchStr == null)
            return;

        if (!s_MaterialPropertiesToSkip.Contains(propName))
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
    }

    /// <summary>
    /// Checks that a Shader conversion is expected; some Shaders are converted when creating backing GameObjects
    /// </summary>
    /// <param name="clientSimShader">Shader instance from a clientSim layer material</param>
    /// <param name="hostPlatformShader">Shader instance from a hostPlatform layer material</param>
    /// <returns>result of comparing Shaders given their expected conversion</returns>
    private bool IsExpectedShader(Shader clientSimShader, Shader hostPlatformShader)
    {
        if (clientSimShader == null && hostPlatformShader == null)
            return true;

        if (clientSimShader == null || hostPlatformShader == null)
            return false;

        return clientSimShader.name switch
        {
            // Simple Lit shaders are converted to Lit shaders
            MaterialShaders.k_SimpleLitShaderName => hostPlatformShader.name == MaterialShaders.k_LitShaderName,
            // font shaders converted to shader graph
            MaterialShaders.k_MobileTextShaderName => hostPlatformShader.name == MaterialShaders.k_TextShaderGraph,
            _ => clientSimShader.name == hostPlatformShader.name
        };
    }

    /// <summary>
    /// Helper method to compare Materials for equality based on instance property values.
    /// </summary>
    /// <param name="clientSimName">Name of the clientSim GameObject</param>
    /// <param name="clientSimId">PolySpatialInstance Id string value for the clientSim GameObject</param>
    /// <param name="clientSimKey">Tracked GameObject property name</param>
    /// <param name="clientSimMaterial">clientSim layer Material instance</param>
    /// <param name="hostPlatformMaterial">HostPlatform layer Material instance</param>
    private void DiffMaterials(string clientSimName, string clientSimId, string clientSimKey, Material clientSimMaterial, Material hostPlatformMaterial)
    {
        if (!DiffNullCheck(clientSimName, clientSimId, clientSimKey, clientSimMaterial, hostPlatformMaterial))
            return;

        // fetch and compare Material property names
        foreach (var materialPropertyType in Enum.GetValues(typeof(MaterialPropertyType)).Cast<MaterialPropertyType>())
        {
            var clientSimMaterialProps = clientSimMaterial.GetPropertyNames(materialPropertyType);
            var hostPlatformMaterialProps = hostPlatformMaterial.GetPropertyNames(materialPropertyType);

            // TODO: LXR-1610 - re-enable property list validation once JIRA resolved
            // remove any property not found in Material instances
            var sharedMaterialProps = clientSimMaterialProps.Intersect(hostPlatformMaterialProps).ToArray();
            foreach (var propName in sharedMaterialProps)
                DiffMaterialProperty(clientSimName, clientSimId, clientSimKey, clientSimMaterial, hostPlatformMaterial, materialPropertyType, propName);
        }

        if (clientSimMaterial.doubleSidedGI != hostPlatformMaterial.doubleSidedGI)
        {
            var valueMismatchStr = "Material double side GI mismatch ("
                                   + clientSimMaterial.doubleSidedGI + " != "
                                   + hostPlatformMaterial.doubleSidedGI + ")";

            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }


        if (!clientSimMaterial.shader.Equals(hostPlatformMaterial.shader))
        {
            // check that shader name diffs are expected transformations
            if (!IsExpectedShader(clientSimMaterial.shader, hostPlatformMaterial.shader))
            {
                var valueMismatchStr = "Material Shader mismatch ("
                                       + clientSimMaterial.shader + " != "
                                       + hostPlatformMaterial.shader + ")";
                DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
            }
        }

        if (!clientSimMaterial.hideFlags.Equals(hostPlatformMaterial.hideFlags))
        {
            var valueMismatchStr = "Material hide flags mismatch ("
                                   + clientSimMaterial.hideFlags + " != "
                                   + hostPlatformMaterial.hideFlags + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        // run additional property validation for for materials that should have said properties

        // shader type validation is run elsewhere; retrieve shader type to pass to DiffTextures only
        var shaderType = clientSimMaterial.shader.name;

        // Some shaders used in tests do not have certain properties:
        // 1. color
        if (!k_NoColorShaders.Contains(shaderType))
        {
            Color32 clientSimColor = clientSimMaterial.color;
            Color32 hostPlatformColor = hostPlatformMaterial.color;

            CheckEquals(
                "Material color",
                clientSimColor,
                hostPlatformColor,
                clientSimName, clientSimId, clientSimKey);
        }

        // 2. mainTexture*
        if (!k_NoTextureShaders.Contains(shaderType))
        {
            CheckEquals(
                "Material main texture scale",
                clientSimMaterial.mainTextureScale,
                hostPlatformMaterial.mainTextureScale,
                clientSimName, clientSimId, clientSimKey);

            CheckEquals(
                "Material main texture offset",
                clientSimMaterial.mainTextureOffset,
                hostPlatformMaterial.mainTextureOffset,
                clientSimName, clientSimId, clientSimKey);

            DiffTextures(clientSimName, clientSimId, clientSimKey, clientSimMaterial.mainTexture, hostPlatformMaterial.mainTexture, shaderType);
            if (EnableExtraValidation)
                DiffTexturesPerPixel(clientSimName, clientSimId, clientSimKey, clientSimMaterial.mainTexture, hostPlatformMaterial.mainTexture);
        }
    }

    private bool CheckEquals(string propertyNameNice, Object clientSimValue, Object hostPlatformValue, string clientSimName, string clientSimId,
        string clientSimKey)
    {
        var equals = clientSimValue.Equals(hostPlatformValue);
        if(!equals)
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {propertyNameNice} mismatch ({clientSimValue} != {hostPlatformValue})");

        return equals;
    }

    private void DiffColors(string clientSimName, string clientSimId, string clientSimKey, Color32 clientSimColor, Color32 hostPlatformColor)
    {
        if (clientSimColor.Equals(hostPlatformColor))
            return;

        var valueMismatchStr = "Color Value mismatch ("
                               + clientSimColor + " != "
                               + hostPlatformColor + ")";
        DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
    }

    private void DiffTexturesPerPixel(string clientSimName, string clientSimId, string clientSimKey, Texture clientSimTexture, Texture hostPlatformTexture)
    {
        // Note: For this function to succeed, Texture.isReadable must be true
        if (!(clientSimTexture != null && clientSimTexture.isReadable && hostPlatformTexture != null && hostPlatformTexture.isReadable))
            return;

        Color32[] clientSimPixels;
        Color32[] hostPlatformPixels;

        switch (clientSimTexture)
        {
            case Texture2D simTexture2D when hostPlatformTexture is Texture2D rendererTexture2D:
                clientSimPixels = simTexture2D.GetPixels32();
                hostPlatformPixels = rendererTexture2D.GetPixels32();
                break;
            case Texture3D simTexture3D when hostPlatformTexture is Texture3D rendererTexture3D:
                clientSimPixels = simTexture3D.GetPixels32();
                hostPlatformPixels = rendererTexture3D.GetPixels32();
                break;
            default:
            {
                var valueMismatchStr = "Unsupported Texture and/or type mismatch ("
                                       + clientSimTexture + " != "
                                       + hostPlatformTexture + ")";
                DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
                return;
            }
        }

        // 1. check that pixel count is the same
        if (clientSimPixels.Length != hostPlatformPixels.Length)
        {
            var valueMismatchStr = "Texture Pixel Count Value mismatch ("
                                   + clientSimPixels.Length + " != "
                                   + hostPlatformPixels.Length + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
            return;
        }

        // 2. loop through all pixels and compare
        for (var i = 0; i < clientSimPixels.Length; i++)
        {
            var clientSimColor = clientSimPixels[i];
            var hostPlatformColor = hostPlatformPixels[i];

            DiffColors(clientSimName, clientSimId, clientSimKey, clientSimColor, hostPlatformColor);
        }
    }

    private void DiffMeshes(string clientSimName, string clientSimId, string clientSimKey, Mesh clientSimMesh, Mesh hostPlatformMesh)
    {
        if (!clientSimMesh.vertices.SequenceEqual(hostPlatformMesh.vertices))
        {
            var valueMismatchStr = "Vertices Value mismatch ("
                                   + string.Join(",", clientSimMesh.vertices) + " != "
                                   + string.Join(",", hostPlatformMesh.vertices) + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        // Gating additional Mesh property comparison behind extra validation flag
        if (!EnableExtraValidation)
            return;

        /*
         * Compare Additional Mesh Properties
         * TODO: XRQA-999 - compare additional mesh properties:
        boneWeights	The BoneWeight for each vertex in the Mesh, which represents 4 bones per vertex.
        colors	Vertex colors of the Mesh.
        indexBufferTarget	The intended target usage of the Mesh GPU index buffer.
        indexFormat	Format of the mesh index buffer data.
        skinWeightBufferLayout	The dimension of data in the bone weight buffer.
        vertexAttributeCount	Returns the number of vertex attributes that the mesh has. (Read Only)
        vertexBufferCount	Gets the number of vertex buffers present in the Mesh. (Read Only)
        vertexBufferTarget	The intended target usage of the Mesh GPU vertex buffer.
         */
        if (!clientSimMesh.bindposes.SequenceEqual(hostPlatformMesh.bindposes))
        {
            var valueMismatchStr = "Bind Poses Value mismatch ("
                                   + string.Join(",", clientSimMesh.bindposes) + " != "
                                   + string.Join(",", hostPlatformMesh.bindposes) + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (clientSimMesh.blendShapeCount != hostPlatformMesh.blendShapeCount)
        {
            var valueMismatchStr = "Blend Shape Count Value mismatch ("
                                   + clientSimMesh.blendShapeCount + " != "
                                   + hostPlatformMesh.blendShapeCount + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (!clientSimMesh.bounds.Equals(hostPlatformMesh.bounds))
        {
            var valueMismatchStr = "Bounds Value mismatch ("
                                   + clientSimMesh.bounds + " != "
                                   + hostPlatformMesh.bounds + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        var clientSimColors = clientSimMesh.colors32;
        var hostPlatformColors = hostPlatformMesh.colors32;

        if (clientSimColors.Length != hostPlatformColors.Length)
        {
            var valueMismatchStr = "Colors32 Length Value mismatch ("
                                   + clientSimColors.Length + " != "
                                   + hostPlatformColors.Length + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }
        else
        {
            for(var i = 0; i < clientSimColors.Length; i++)
                DiffColors(clientSimName, clientSimId, clientSimKey, clientSimColors[i], hostPlatformColors[i]);
        }

        if (clientSimMesh.subMeshCount != hostPlatformMesh.subMeshCount)
        {
            var valueMismatchStr = "SubMesh Count Value mismatch ("
                                   + clientSimMesh.subMeshCount + " != "
                                   + hostPlatformMesh.subMeshCount + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (!clientSimMesh.normals.SequenceEqual(hostPlatformMesh.normals))
        {
            var valueMismatchStr = "Tangents Value mismatch ("
                                   + string.Join(",", clientSimMesh.normals) + " != "
                                   + string.Join(",", hostPlatformMesh.normals) + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (!clientSimMesh.tangents.SequenceEqual(hostPlatformMesh.tangents))
        {
            var valueMismatchStr = "Tangents Value mismatch ("
                                   + string.Join(",", clientSimMesh.tangents) + " != "
                                   + string.Join(",", hostPlatformMesh.tangents) + ")";

            // TODO: XRQA-1027 - enable diff logging once resolved
            // diffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        if (!clientSimMesh.triangles.SequenceEqual(hostPlatformMesh.triangles))
        {
            var valueMismatchStr = "Triangles Value mismatch ("
                                   + string.Join(",", clientSimMesh.triangles) + " != "
                                   + string.Join(",", hostPlatformMesh.triangles) + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        // Check all 8 UV channels (https://docs.unity3d.com/2023.1/Documentation/ScriptReference/Mesh.GetUVs.html)
        for (var c = 0; c < 8; c++)
        {
            List<Vector2> clientSimUvContent = new();
            List<Vector2> hostPlatformUvContent = new();

            clientSimMesh.GetUVs(c, clientSimUvContent);
            hostPlatformMesh.GetUVs(c, hostPlatformUvContent);

            if (clientSimUvContent.Count != hostPlatformUvContent.Count)
            {
                var valueMismatchStr = $"UV{c} Count Value mismatch ("
                                       + clientSimUvContent.Count + " != "
                                       + hostPlatformUvContent.Count + ")";
                // TODO: XRQA-1027 - re-enable diff logging once resolved
                // diffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
            }
            else
            {
                for (var i = 0; i < clientSimUvContent.Count; i++)
                {
                    var clientSimUv = clientSimUvContent[i];
                    var hostPlatformUv = hostPlatformUvContent[i];

                    if (!MathExtensions.ApproximatelyEqual(clientSimUv, hostPlatformUv, k_MaxFloatDelta))
                    {
                        var valueMismatchStr = $"UV{c}[{i}] Value mismatch ("
                                               + clientSimUv + " != "
                                               + hostPlatformUv + ")";
                        // TODO: XRQA-1027 - re-enable diff logging once resolved
                        // diffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
                    }
                }
            }
        }
    }

    private void DiffSmrBone(string clientSimName, string clientSimId, string clientSimKey, TransformNode clientSimBone, TransformNode hostPlatformBone)
    {
        // check each transform property
        // 1.1. position
        var clientSimPosition = clientSimBone.position;
        var hostPlatformPosition = hostPlatformBone.position;

        if (!MathExtensions.ApproximatelyEqual(clientSimPosition, hostPlatformPosition, k_MaxFloatDelta))
        {
            var valueMismatchStr = $"SkinnedMeshRenderer Bone {clientSimBone.name}({clientSimBone.gameObjectInstanceId}) Position mismatch ("
                                   + clientSimPosition + " != "
                                   + hostPlatformPosition + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        // 1.2. rotation
        var clientSimRotation = clientSimBone.rotation;
        var hostPlatformRotation = hostPlatformBone.rotation;

        if (!MathExtensions.ApproximatelyEqual(clientSimRotation, hostPlatformRotation))
        {
            var valueMismatchStr = $"SkinnedMeshRenderer Bone {clientSimBone.name}({clientSimBone.gameObjectInstanceId}) Rotation mismatch ("
                                   + clientSimRotation.ToString(k_DecimalFormatter) + " != "
                                   + hostPlatformRotation.ToString(k_DecimalFormatter) + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }

        // 1.3. scale
        var clientSimScale = clientSimBone.localScale;
        var hostPlatformScale = hostPlatformBone.localScale;

        if (!MathExtensions.ApproximatelyEqual(clientSimScale, hostPlatformScale, k_MaxFloatDelta))
        {
            var valueMismatchStr = $"SkinnedMeshRenderer Bone {clientSimBone.name}({clientSimBone.gameObjectInstanceId}) Scale mismatch ("
                                   + clientSimScale + " != "
                                   + hostPlatformScale + ")";
            DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{clientSimKey}: {valueMismatchStr}");
        }
    }

    private void DiffSkinnedMeshRenderers(string clientSimName, string clientSimId, string clientSimKey,
        SkinnedMeshRendererNode clientSimSmrProperty, SkinnedMeshRendererNode hostPlatformSmrProperty)
    {
        // 1. check bones

        // 1.1 check root bone
        DiffSmrBone(clientSimName, clientSimId, clientSimKey, clientSimSmrProperty.rootBone, hostPlatformSmrProperty.rootBone);

        // 1.2 check bones array
        var clientSimBones = clientSimSmrProperty.bones;
        var hostPlatformBones = hostPlatformSmrProperty.bones;

        if (CheckEquals("SkinnedMeshRenderer Bones count",
                clientSimBones.Length, hostPlatformBones.Length, clientSimName, clientSimId, clientSimKey))
        {
            for (var i = 0; i < clientSimBones.Length; i++)
                DiffSmrBone(clientSimName, clientSimId, clientSimKey, clientSimBones[i], hostPlatformBones[i]);
        }

        // 2. check shared Mesh
        var clientSimSharedMesh = clientSimSmrProperty.sharedMesh;
        var hostPlatformSharedMesh = hostPlatformSmrProperty.sharedMesh;

        DiffMeshes(clientSimName, clientSimId, clientSimKey, clientSimSharedMesh, hostPlatformSharedMesh);

        // 3. check skin quality
        CheckEquals("SkinnedMeshRenderer quality",
            clientSimSmrProperty.quality, hostPlatformSmrProperty.quality, clientSimName, clientSimId, clientSimKey);
    }

    /// <summary>
    /// Compares a clientSim layer RectTransform to hostPlatform layer Transform properties. RectTransform components are converted to Transform on
    /// hostPlatform layer GameObjects.
    /// </summary>
    /// <param name="clientSimName">clientSim GO name</param>
    /// <param name="clientSimId">clientSim GO PolySpatialInstanceId</param>
    /// <param name="propertyKey">property key used to index RectTransform instance in state validator collections</param>
    /// <param name="clientSimRectTransform">clientSim layer RectTransform instance</param>
    /// <param name="hostPlatformNode">hostPlatform layer GameObjectNode instance</param>
    private void DiffRectTransform(string clientSimName, string clientSimId, string propertyKey, RectTransform clientSimRectTransform, GameObjectNode hostPlatformNode)
    {
        var clientSimTransform = clientSimRectTransform.transform;

        var transformedProperties = k_TransformedProperties[k_RectTransformProperty];
        foreach (var transformedProperty in transformedProperties)
        {
            if (!hostPlatformNode.properties.TryGetValue(transformedProperty, out var hostPlatformProperty))
            {
                DiffSb.AppendLine(
                    $"'{clientSimName}'({clientSimId}).{propertyKey}: {transformedProperty} is missing on hostPlatform GO");
                continue;
            }


            Object clientSimProperty = transformedProperty switch
            {
                k_LocalPositionProperty => clientSimTransform.localPosition,
                k_LocalRotationProperty => clientSimTransform.localRotation,
                k_LocalScaleProperty => clientSimTransform.localScale,
                _ => null
            };

            DiffProperty(clientSimName, clientSimId, propertyKey, clientSimProperty, hostPlatformProperty);
        }
    }

    /// <summary>
    /// Checks that the given clientSim layer GameObject has a property that transforms to a different component(s) when creating its
    /// hostPlatform GameObject. The hostPlatform GameObject property should be one of the mapped properties in <see cref="k_TransformedProperties"/>
    /// </summary>
    /// <param name="clientSimNode">clientSim layer GameObjectNode instance</param>
    /// <param name="hostPlatformPropertyKey">property key used to track hostPlatform layer properties in state validator collections</param>
    /// <returns>True if transformation is expected and mapped, false otherwise</returns>
    private bool IsOriginalPropertyTransformed(GameObjectNode clientSimNode, string hostPlatformPropertyKey)
    {
        foreach (var kvp in k_TransformedProperties)
        {
            var clientSimPropertyKey = kvp.Key;
            var transformedProperties = kvp.Value;
            if (transformedProperties.Contains(hostPlatformPropertyKey) && clientSimNode.properties.ContainsKey(clientSimPropertyKey))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks that a clientSim layer GameObject property conversion is expected. Several clientSim GameObject properties are converted to different components
    /// for hostPlatform layer GameObjects.
    /// </summary>
    /// <param name="clientSimName">clientSim GO name</param>
    /// <param name="clientSimId">clientSim GO PolySpatialInstanceId</param>
    /// <param name="propertyKey">property key used to index component instances in state validator collections</param>
    /// <param name="clientSimProperty">clientSim layer GameObject property instance as an Object</param>
    /// <param name="hostPlatformNode">hostPlatform layer GameObjectNode instance</param>
    private void DiffTransformedProperty(string clientSimName, string clientSimId, string propertyKey, Object clientSimProperty, GameObjectNode hostPlatformNode)
    {
        switch (propertyKey)
        {
            // Rect Transforms converted to Transform
            case k_RectTransformProperty:
            {
                var clientSimRectTransform = clientSimProperty as RectTransform;
                DiffRectTransform(clientSimName, clientSimId, propertyKey, clientSimRectTransform, hostPlatformNode);
                break;
            }
            // TODO - XRQA-1050: determine for all UGUI components their mapping to hostPlatform GO components and compare their values
            // No canvas property on hostPlatform GOs; expecting hostPlatform GOs to have MeshFilter and MeshRenderer components
            case k_CanvasRendererProperty:
            {
                // Check the hostPlatform has expected transformed components
                var transformedProperties = k_TransformedProperties[k_CanvasRendererProperty];
                foreach (var transformedProperty in transformedProperties)
                {
                    if (!hostPlatformNode.properties.TryGetValue(transformedProperty, out var hostPlatformProperty))
                    {
                        DiffSb.AppendLine(
                            $"'{clientSimName}'({clientSimId}): UGUI ({propertyKey}) clientSim GO Detected; hostPlatform missing property {transformedProperty}");
                    }
                }

                break;
            }
        }
    }

    /// <summary>
    /// Checks that every clientSim layer GameObject is linked to a hostPlatform layer GameObject via a valid
    /// PolySpatialInstanceId. For each pair, properties are also compared.
    /// Generates diff string line for each failed validation.
    /// </summary>
    /// <param name="clientSimState">collection of all clientSim layer clientSimIds and GameObjectNodes found</param>
    /// <param name="hostPlatformState">collection of all hostPlatform layer clientSimIds and GameObjectNodes found.</param>
    private void DiffGameObjects(
        Dictionary<PolySpatialInstanceID, GameObjectNode> clientSimState,
        Dictionary<PolySpatialInstanceID, GameObjectNode> hostPlatformState)
    {
        foreach (var (clientSimId, hostPlatformNode) in hostPlatformState)
        {
            if (clientSimId.ToString() == PolySpatialInstanceID.None.ToString() ||
                hostPlatformNode.clientSimId.ToString() == PolySpatialInstanceID.None.ToString())
            {
                DiffSb.AppendLine($"'{hostPlatformNode.hostPlatformName}'({hostPlatformNode.hostPlatformId}): clientSim ID is None");
                continue;
            }

            if (!clientSimState.ContainsKey(hostPlatformNode.clientSimId))
            {
                DiffSb.AppendLine($"'{hostPlatformNode.clientSimName}'({hostPlatformNode.clientSimId}): Missing from clientSim");
            }
        }

        foreach (var (clientSimId, clientSimNode) in clientSimState)
        {
            if (!hostPlatformState.TryGetValue(clientSimId, out var hostPlatformNode))
            {
                DiffSb.AppendLine(
                    $"'{clientSimNode.clientSimName}'({clientSimNode.clientSimId}): Missing from hostPlatform");
                continue;
            }

            // Diff all tracked GO properties
            DiffProperties(clientSimNode, hostPlatformNode);
        }
    }

    /// <summary>
    /// Compares each clientSim and hostPlatform GameObject property (may sometimes be Component instances).
    /// Generates diff sting line for each failed validation.
    /// </summary>
    /// <param name="clientSimNode">clientSim layer GameObjectNode instance</param>
    /// <param name="hostPlatformNode">HostPlatform layer GameObjectNode instance</param>
    private void DiffProperties(GameObjectNode clientSimNode, GameObjectNode hostPlatformNode)
    {
        foreach (var propertyKey in hostPlatformNode.properties.Keys.Where(propertyKey => !clientSimNode.properties.ContainsKey(propertyKey)))
        {
            if(!IsOriginalPropertyTransformed(clientSimNode, propertyKey))
                DiffSb.AppendLine($"'{clientSimNode.clientSimName}'({clientSimNode.clientSimId}): #### clientSim GO missing property {propertyKey}");
        }

        foreach (var (clientSimKey, clientSimProperty) in clientSimNode.properties)
        {
            if (k_TransformedProperties.ContainsKey(clientSimKey))
            {
                DiffTransformedProperty(clientSimNode.clientSimName, clientSimNode.clientSimId.ToString(), clientSimKey, clientSimProperty, hostPlatformNode);
            }
            else if (!hostPlatformNode.properties.TryGetValue(clientSimKey, out var hostPlatformProperty))
            {
                DiffSb.AppendLine($"'{clientSimNode.clientSimName}'({clientSimNode.clientSimId}): HostPlatform GO missing property {clientSimKey}");
            }
            else
            {
                DiffProperty(clientSimKey, clientSimNode.clientSimName, clientSimNode.clientSimId.ToString(), clientSimProperty, hostPlatformProperty);
            }
        }
    }

    /// <summary>
    /// Converts generic property instances to specific property types and then compares property attributes.
    /// Generates diff string line for each failed validation.
    /// </summary>
    /// <param name="propertyKey">GameObjectNode property name</param>
    /// <param name="clientSimName">clientSim layer GameObject Nam</param>
    /// <param name="clientSimId">clientSim layer GameObject's PolySpatialInstancneId</param>
    /// <param name="clientSimProperty">clientSim layer property instance</param>
    /// <param name="hostPlatformProperty">HostPlatform layer property instance</param>
    private void DiffProperty(string propertyKey, string clientSimName, string clientSimId, Object clientSimProperty,
        Object hostPlatformProperty)
    {
        switch (clientSimProperty)
        {
            case string clientSimStrProperty:
                var hostPlatformStrProperty = hostPlatformProperty as string;
                if(clientSimStrProperty != hostPlatformStrProperty)
                    DiffSb.AppendLine(
                        $"'{clientSimName}'({clientSimId}).{propertyKey}: Value mismatch ({clientSimStrProperty} != {hostPlatformStrProperty})");
                break;
            case Vector3 clientSimVector3Property:
                var hostPlatformVector3Property = hostPlatformProperty is Vector3 ? (Vector3) hostPlatformProperty : default;
                if(!MathExtensions.ApproximatelyEqual(clientSimVector3Property, hostPlatformVector3Property, k_MaxFloatDelta))
                    DiffSb.AppendLine(
                        $"'{clientSimName}'({clientSimId}).{propertyKey}: Value mismatch ({clientSimVector3Property} != {hostPlatformVector3Property})");
                break;
            case Quaternion clientSimQuaternionProperty:
                var hostPlatformQuaternionProperty = hostPlatformProperty is Quaternion ? (Quaternion) hostPlatformProperty : default;
                if(!MathExtensions.ApproximatelyEqual(clientSimQuaternionProperty, hostPlatformQuaternionProperty))
                    DiffSb.AppendLine(
                        $"'{clientSimName}'({clientSimId}).{propertyKey}: Value mismatch ({clientSimQuaternionProperty} != {hostPlatformQuaternionProperty})");
                break;
            case Mesh clientSimMeshProperty:
                var hostPlatformMeshProperty = hostPlatformProperty as Mesh;

                // check that hostPlatform GO Mesh exists
                if (hostPlatformMeshProperty == null)
                {
                    var valueMismatchStr = "Value mismatch ("
                                           + clientSimMeshProperty + " != null )";
                    DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{propertyKey}: {valueMismatchStr}");
                    break;
                }

                DiffMeshes(clientSimName, clientSimId.ToString(), propertyKey, clientSimMeshProperty,
                    hostPlatformMeshProperty);
                break;
            case Material[] clientSimMaterialsProperty:
                var hostPlatformMaterialsProperty = hostPlatformProperty as Material[] ?? Array.Empty<Material>();
                // first, check we have the same number of materials:
                if (clientSimMaterialsProperty.Length != hostPlatformMaterialsProperty.Length)
                {
                    var valueMismatchStr = "Materials count mismatch ("
                                           + clientSimMaterialsProperty.Length + " != "
                                           + hostPlatformMaterialsProperty.Length + ")";
                    DiffSb.AppendLine($"'{clientSimName}'({clientSimId}).{propertyKey}: {valueMismatchStr}");
                } else if (clientSimMaterialsProperty.Length > 0)
                {
                    // go through each material and compare them
                    for (int i = 0; i < clientSimMaterialsProperty.Length; i++)
                    {
                        var clientSimMaterial = clientSimMaterialsProperty[i];
                        var backinMaterial = hostPlatformMaterialsProperty[i];

                        DiffMaterials(clientSimName, clientSimId.ToString(), propertyKey, clientSimMaterial,
                            backinMaterial);
                    }
                }
                break;
            case SkinnedMeshRendererNode clientSimSmrProperty:
                var hostPlatformSmrProperty = hostPlatformProperty as SkinnedMeshRendererNode;

                DiffSkinnedMeshRenderers(clientSimName, clientSimId.ToString(), propertyKey, clientSimSmrProperty,
                    hostPlatformSmrProperty);

                break;
            default:
                Debug.Log($"Unsupported GameObjectProperty type: {clientSimProperty.GetType()}");
                break;
        }
    }

    public StringBuilder GenerateStateDiff(
        Dictionary<PolySpatialInstanceID, GameObjectNode> clientSimState,
        Dictionary<PolySpatialInstanceID, GameObjectNode> hostPlatformState)
    {
        DiffSb.Clear();
        if (clientSimState == null && hostPlatformState == null)
            return DiffSb;

        if (clientSimState == null)
        {
            DiffSb.AppendLine("Diff detected; clientSim state is null");
        } else if (hostPlatformState == null)
        {
            DiffSb.AppendLine("Diff detected; hostPlatform state is null");
        }
        else
        {
            DiffGameObjects(clientSimState, hostPlatformState);
        }

        return DiffSb;
    }

    /// <summary>
    /// Compares 2 clientSim layer property objects and determines if any diff is present between the two.
    /// Both objects are expected to be from the clientSim layer and, in contrast to clientSim/hostPlatform comparison,
    /// no property conversion is expected.
    /// Note that because this method reuses general state validation methods, DiffSb is cleared and utilized to
    /// capture diffs between properties.
    /// </summary>
    /// <param name="clientSimName">Name of the clientSim objects to compare</param>
    /// <param name="clientSimId">PolySpatialInstanceId string linked to the objects to compare</param>
    /// <param name="propertyKey">GameObjectNode.properties key linked to the objects to compare</param>
    /// <param name="previousCSProperty">clientSim property object captured in a previous frame</param>
    /// <param name="currentCSProperty">clientSim property object captured in the current frame</param>
    /// <returns>true if a diff is detected between objects, false otherwise</returns>
    private bool ClientSimPropertyChangeDetected(
        string clientSimName, string clientSimId, string propertyKey, Object previousCSProperty, Object currentCSProperty)
    {
        DiffSb.Clear();

        switch (previousCSProperty)
        {
            case string prevCSStrProperty:
                var curCSStrProperty = currentCSProperty as string;
                if (prevCSStrProperty != curCSStrProperty)
                    return true;
                break;
            case Vector3 prevCSVector3Property:
                var currentCSVector3Property = currentCSProperty is Vector3 ? (Vector3) currentCSProperty : default;
                if (!MathExtensions.ApproximatelyEqual(prevCSVector3Property, currentCSVector3Property,
                        k_MaxFloatDelta))
                    return true;
                break;
            case Quaternion prevCSQuaternionProperty:
                var hostPlatformQuaternionProperty = currentCSProperty is Quaternion ? (Quaternion) currentCSProperty : default;
                if (!MathExtensions.ApproximatelyEqual(prevCSQuaternionProperty, hostPlatformQuaternionProperty))
                    return true;
                break;
            case Mesh prevCSMeshProperty:
                var currentCSMeshProperty = currentCSProperty as Mesh;

                // check that hostPlatform GO Mesh exists
                if (currentCSMeshProperty == null)
                    return true;

                DiffMeshes(clientSimName, clientSimId, propertyKey, prevCSMeshProperty, currentCSMeshProperty);
                break;
            case Material[] prevCSMaterialsProperty:
                var currentCSMaterialsProperty = currentCSProperty as Material[] ?? Array.Empty<Material>();
                // first, check we have the same number of materials:
                if (prevCSMaterialsProperty.Length != currentCSMaterialsProperty.Length)
                    return true;

                if (prevCSMaterialsProperty.Length > 0)
                {
                    // go through each material and compare them
                    for (var i = 0; i < prevCSMaterialsProperty.Length; i++)
                    {
                        var prevCSMaterial = prevCSMaterialsProperty[i];
                        var currentCSMaterial = currentCSMaterialsProperty[i];

                        DiffMaterials(clientSimName, clientSimId, propertyKey, prevCSMaterial, currentCSMaterial);
                    }
                }
                break;
            case SkinnedMeshRendererNode prevCSSmrProperty:
                var currentCSSmrProperty = currentCSProperty as SkinnedMeshRendererNode;
                if (currentCSSmrProperty == null)
                    return true;

                DiffSkinnedMeshRenderers(clientSimName, clientSimId.ToString(), propertyKey, prevCSSmrProperty, currentCSSmrProperty);
                break;
            case RectTransform prevCSRectTransformProperty:
                var currentCSRectTransformProperty = currentCSProperty as RectTransform;

                if (prevCSRectTransformProperty == null && currentCSRectTransformProperty == null)
                    return false;
                if (prevCSRectTransformProperty == null || currentCSRectTransformProperty == null)
                    return true;

                var prevPos = prevCSRectTransformProperty.position;
                var prevRot = prevCSRectTransformProperty.rotation;
                var prevScale = prevCSRectTransformProperty.localScale;
                var curPos = currentCSRectTransformProperty.position;
                var curRot = currentCSRectTransformProperty.rotation;
                var curScale = currentCSRectTransformProperty.localScale;
                if (!(prevPos.Equals(curPos) && prevRot.Equals(curRot) && prevScale.Equals(curScale)))
                    return true;
                break;
            default:
                Debug.Log($"Unsupported ClientSim GameObjectProperty type: {previousCSProperty.GetType()}");
                break;
        }

        var diffDetected = DiffSb.Length > 0;
        DiffSb.Clear();
        return diffDetected;
    }

    /// <summary>
    /// Compares 2 clientSim state collections to determine if a change is present between the states. Though similar
    /// to clientSim to hostPlatform state comparison, this alternative method is needed as there are no expected
    /// component conversions between clientSim-clientSim state comparisons, whereas clientSim-hostPlatform state
    /// comparison expects conversions among other known diffs.
    /// </summary>
    /// <param name="previousClientSimState">A previously generated clientSim state dictionary</param>
    /// <param name="currentClientSimState">The current clientSim state dictionary</param>
    /// <returns>true if a change is detected between the 2 state dictionaries, false otherwise</returns>
    public bool ClientSimChangeDetected(
        Dictionary<PolySpatialInstanceID, GameObjectNode> previousClientSimState,
        Dictionary<PolySpatialInstanceID, GameObjectNode> currentClientSimState)
    {
        if (previousClientSimState == null && currentClientSimState == null)
            return false;

        // clientSim state detected if:
        // 1. prevCS is null and currentCS is not
        // 2. prevCS is not null and currentCS is null
        if (previousClientSimState == null || currentClientSimState == null)
            return true;
        // 3. collection content mismatch
        if (previousClientSimState.Any(kpv => !currentClientSimState.ContainsKey(kpv.Key))
            || currentClientSimState.Any(kpv => !previousClientSimState.ContainsKey(kpv.Key)))
            return true;
        // 4. keys mismatch
        if (previousClientSimState.Keys.Except(currentClientSimState.Keys).ToList().Count > 0)
            return true;
        // 5. GO mismatch
        foreach (var key in previousClientSimState.Keys)
        {
            var prevCSGO = previousClientSimState[key];
            var curCSGO = currentClientSimState[key];

            if (!prevCSGO.Equivalent(curCSGO))
            {
                Debug.Log($"ClientSim diff detected because GO collections do not match");
                return true;
            }

            foreach (var propertyKey in prevCSGO.properties.Keys)
            {
                var prevCSProperty = prevCSGO.properties[propertyKey];
                var curCSProperty = curCSGO.properties[propertyKey];
                if (ClientSimPropertyChangeDetected(
                        prevCSGO.clientSimName,
                        key.ToString(),
                        propertyKey,
                        prevCSProperty,
                        curCSProperty))
                    return true;
            }

        }

        return false;
    }
}
