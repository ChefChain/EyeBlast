//
//  UnityMain.swift
//  Unity-RealityOS
//
//  Created by Peter Kuhn on 6/30/23.
//

import SwiftUI
import CompositorServices

@_silgen_name("UnityVisionOS_OnInputEvent")
private func UnityVisionOS_OnInputEvent(_ eventCount: Int32, _ eventsPointer: UnsafePointer<SpatialPointerEvent>?)

@main
struct MyApp: App {
    @UIApplicationDelegateAdaptor
    var swiftUIdelegate: UnitySwiftUIAppDelegate
    
    static let maxPointers = 2
    static var currentEvents: Set<SpatialEventCollection.Event.ID> = .init()
    static var existingEvents: [SpatialEventCollection.Event.ID: Bool] = .init()
    static var existingIndices: [SpatialEventCollection.Event.ID: Int32] = .init()
    static var spatialPointerEvents: [SpatialPointerEvent] = .init(repeating: .init(), count: maxPointers)

    struct UnityContentConfiguration: CompositorLayerConfiguration {
        let singlePass: Bool
        let enableFoveation: Bool
        
        func makeConfiguration(
                capabilities: LayerRenderer.Capabilities,
                configuration: inout LayerRenderer.Configuration
            ) {
                let supportsFoveation = capabilities.supportsFoveation
                let supportedLayouts = capabilities.supportedLayouts(options: supportsFoveation ?
                                                        [.foveationEnabled] : [])


                configuration.layout = supportedLayouts.contains(.layered) ? .layered : .dedicated
                configuration.isFoveationEnabled = supportsFoveation


                configuration.colorFormat = .rgba8Unorm_srgb
                configuration.depthFormat = .depth32Float_stencil8
                if singlePass == true
                {
                    configuration.layout = .layered
                } else
                {
                    configuration.layout = .dedicated
                }
                
                configuration.isFoveationEnabled = enableFoveation
                configuration.generateFlippedRasterizationRateMaps = enableFoveation
            }
    }
    
    @State private var immersionStyle: ImmersionStyle = .full
    
    var body: some Scene {
        ImmersiveSpace(id: "CompositorSpace") {
            let _ = UnityLibrary.GetInstance()
            let unityClass = NSClassFromString("UnityVisionOS") as? NSObject.Type
            let singlePass = Bool(truncating: unityClass?.perform(Selector(("getSinglePass"))).takeRetainedValue() as! NSNumber)
            let configuration = UnityContentConfiguration(singlePass: singlePass, enableFoveation: VisionOSEnableFoveation)
            CompositorLayer(configuration:configuration) { layerRenderer in
                unityClass?.perform(Selector(("setLayerRenderer:")), with: layerRenderer)

                let unityBridge = NSClassFromString("UnityVisionOSNativeBridge") as? NSObject.Type
                unityBridge?.perform(Selector(("setImmersiveSpaceReady")))

                layerRenderer.onSpatialEvent = { eventCollection in
                    var count = 0
                    // Clear out existing state for events which are no longer being tracked
                    Self.currentEvents.removeAll()
                    for event in eventCollection {
                        Self.currentEvents.insert(event.id)
                    }

                    Self.existingEvents = Self.existingEvents.filter { Self.currentEvents.contains($0.key) }
                    Self.existingIndices = Self.existingIndices.filter { Self.currentEvents.contains($0.key) }

                    // TODO: use pinch ID to handle the case where the first pinch ends before the second
                    for event in eventCollection
                    {
                        if (count > 1)
                        {
                            break
                        }
                        
                        let pose = event.inputDevicePose
                        let ray = event.selectionRay
                        if (pose == nil || ray == nil)
                        {
                            continue
                        }

                        if Self.existingIndices[event.id] == nil {
                            var newIndex = 0
                            var foundIndex = true
                            while foundIndex {
                                foundIndex = false
                                for (_, index) in Self.existingIndices {
                                    if index == newIndex {
                                        newIndex += 1
                                        foundIndex = true
                                    }
                                }
                            }

                            Self.existingIndices[event.id] = Int32(newIndex)
                        }
                        
                        let existingEvent = Self.existingEvents[event.id] ?? false
                        let phase = event.phase
                        let pose3D = pose!.pose3D
                        let sendPointerEvent: SpatialPointerEvent = .init(
                            interactionId: Self.existingIndices[event.id]!,
                            interactionRayOrigin: ConvertDouble3PositionToUnityVector3(ray!.origin.vector),
                            interactionRayDirection: ConvertDouble3PositionToUnityVector3(ray!.direction.vector),
                            inputDevicePosition: ConvertDouble3PositionToUnityVector3(pose3D.position.vector),
                            inputDeviceRotation: ConvertDouble4RotationToUnityVector4(pose3D.rotation.vector),
                            modifierKeys: UInt16(event.modifierKeys.rawValue),
                            kind: ConvertKindToUInt8(event.kind),
                            phase: ConverPhaseToUInt8(event.phase, existingEvent)
                        )

                        Self.existingEvents[event.id] = phase == .active
                        Self.spatialPointerEvents[count] = sendPointerEvent
                        count = count + 1
                    }

                    let events = [SpatialPointerEvent].init(Self.spatialPointerEvents[0..<count]);
                    let _ = events.withUnsafeBufferPointer { buffer in
                        UnityVisionOS_OnInputEvent(Int32(count), buffer.baseAddress)
                    }
                }
            }
        }.immersionStyle(selection: $immersionStyle, in: .full)
    }
    
    func ConvertKindToUInt8(_ kind: SpatialEventCollection.Event.Kind) -> UInt8 {
        // VisionOSSpatialPointerKind values match SpatialEventCollection.Event.Kind 1:1
        switch(kind) {
        case .touch: return 0
        case .directPinch: return 1
        case .indirectPinch: return 2
        case .pointer: return 3
        @unknown default:
            return UInt8.max
        }
    }
    
    func ConverPhaseToUInt8(_ phase: SpatialEventCollection.Event.Phase, _ existingEvent: Bool) -> UInt8 {
        switch(phase) {
            // VisionOSSpatialPointerPhase.Began or VisionOSSpatialPointerPhase.Moved, based on whether there is an existing event with the same id
        case .active: return existingEvent ? 2 : 1
            // VisionOSSpatialPointerPhase.Ended
        case .ended: return 3
            // VisionOSSpatialPointerPhase.Cancelled
        case .cancelled: return 4
            // VisionOSSpatialPointerPhase.None
        @unknown default:
            return 0
        }
    }
    
    func ConvertDouble3PositionToUnityVector3(_ position: simd_double3) -> UnityVector3 {
        // Flip the Z-coordinate to go between ARKit and Unity worldspaces.
        return UnityVector3(x: Float(position.x), y: Float(position.y), z: -Float(position.z))
    }
    
    func ConvertDouble4RotationToUnityVector4(_ rotation: simd_double4) -> UnityVector4 {
        // Flip the Z-coordinate and W-coordinate to go between ARKit and Unity worldspaces.
        return UnityVector4(x: Float(rotation.x), y: Float(rotation.y), z: -Float(rotation.z), w: -Float(rotation.w))
    }
}

struct UnityVector3
{
    private var _x: Float;
    private var _y: Float;
    private var _z: Float;
    
    internal init(x: Float, y: Float, z: Float) {
        _x = x
        _y = y
        _z = z
    }
    
    internal init() {
        _x = 0
        _y = 0
        _z = 0
    }
}

struct UnityVector4
{
    private var _x: Float;
    private var _y: Float;
    private var _z: Float;
    private var _w: Float;
    
    internal init(x: Float, y: Float, z: Float, w: Float) {
        _x = x
        _y = y
        _z = z
        _w = w
    }
    
    internal init() {
        _x = 0
        _y = 0
        _z = 0
        _w = 0
    }
}

struct SpatialPointerEvent
{
    private var _interactionId: Int32
    private var _interactionRayOrigin: UnityVector3
    private var _interactionRayDirection: UnityVector3
    private var _inputDevicePosition: UnityVector3
    private var _inputDeviceRotation: UnityVector4
    private var _modifierKeys: UInt16
    private var _kind: UInt8
    private var _phase: UInt8

    init(interactionId: Int32, interactionRayOrigin: UnityVector3, interactionRayDirection: UnityVector3, inputDevicePosition: UnityVector3, inputDeviceRotation: UnityVector4, modifierKeys: UInt16, kind: UInt8, phase: UInt8)
    {
        _interactionId = interactionId
        _interactionRayOrigin = interactionRayOrigin
        _interactionRayDirection = interactionRayDirection
        _inputDevicePosition = inputDevicePosition
        _inputDeviceRotation = inputDeviceRotation
        _modifierKeys = modifierKeys
        _kind = kind
        _phase = phase
    }

    internal init() {
      _interactionId = 0
      _interactionRayOrigin = UnityVector3()
      _interactionRayDirection = UnityVector3()
      _inputDevicePosition = UnityVector3()
      _inputDeviceRotation = UnityVector4()
      _modifierKeys = 0
      _kind = 0
      _phase = 0
    }
}

class UnitySwiftUIAppDelegate: NSObject, UIApplicationDelegate
{
    var unity: UnityLibrary
    
    override init() {
        unity = UnityLibrary.GetInstance()!

        super.init()

//        let api = PolySpatialRealityKitAccess.getApiData()
//        let size = PolySpatialRealityKitAccess.getApiSize()
//        SetPolySpatialNativeAPIImplementation(api, size)
//
//        PolySpatialRealityKitAccess.register()
    }
    
    func application(_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey : Any]? = nil) -> Bool {
        
        var args = CommandLine.arguments
//        args.append("-batchmode")

        unity.run(args: args)
        return true
    }

    func application(
            _ application: UIApplication,
            configurationForConnecting connectingSceneSession: UISceneSession,
            options: UIScene.ConnectionOptions
        ) -> UISceneConfiguration {
       let configuration = UISceneConfiguration(
           name: nil,
           sessionRole: connectingSceneSession.role)
       configuration.delegateClass = UnitySwiftUISceneDelegate.self
       return configuration
   }
}

@available(iOS 13.0, *)
class UnitySwiftUISceneDelegate: UIResponder, UISceneDelegate {
    var unity: UnityLibrary
    
    override init() {
        unity = UnityLibrary.GetInstance()!

        super.init()
    }
    
    func sceneDidBecomeActive(_ scene: UIScene) {
        unity.didBecomeActive();
    }

    func sceneWillResignActive(_ scene: UIScene) {
        unity.willResignActive();
    }

    func sceneDidEnterBackground(_ scene: UIScene) {
        unity.didEnterBackground()
    }
    
    func sceneWillEnterForeground(_ scene: UIScene) {
        unity.willEnterForeground()
    }
}
