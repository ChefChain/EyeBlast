import UnityFramework
import ARKit
import SwiftUI
import PolySpatialRealityKit

@_silgen_name("SetPolySpatialNativeAPIImplementation")
private func SetPolySpatialNativeAPIImplementation(_ api: UnsafeRawPointer, _ size: Int32)

class UnityPolySpatialAppDelegate: NSObject, UIApplicationDelegate, ObservableObject
{
    var unity: UnityLibrary

    @Published private(set) public var handsAuthorized: Bool = false
    @Published private(set) public var worldSensingAuthorized: Bool = false

    private(set) public var bootConfig: [String: String]

    var uuidToScene: [UUID: UIScene] = [:]

    override init() {
        // read bootconfig
        bootConfig = .init()
        let bootconfigPath = Bundle.main.path(forResource: "Data/boot", ofType: "config")!
        let content = try! String(contentsOfFile: bootconfigPath, encoding: .ascii)
        for line in content.components(separatedBy: .newlines) {
            let parts = line.components(separatedBy: "=")
            if parts.count == 2 {
                bootConfig[parts[0]] = parts[1]
            }
        }

        unity = UnityLibrary.GetInstance()!

        super.init()

        struct Request {
            var isHandTracking: Bool
            var name: String
            var allow: () -> Void
        }

        for request in [
            Request(
                isHandTracking: true,
                name: "Hand Tracking",
                allow: {
                    self.handsAuthorized = true
                }
            ),
            Request(
                isHandTracking: false,
                name: "World Sensing",
                allow: {
                    self.worldSensingAuthorized = true
                }
            )
        ] {
#if false
            // Request authorization to track the user's hands.
            ARAuthorization.request(withTypes: [request.isHandTracking ? .handTracking : .worldSensing]) { success, results, error in
                if success {
                    results?.forEach({ result in
                        if result.authorizedType == (request.isHandTracking ? .handTracking : .worldSensing) {
                            switch result.authorizedStatus {
                            case .denied:
                                print("\(request.name) authorization has been denied. \(error?.localizedDescription ?? "")")
                            case .allowed:
                                print("\(request.name) authorization has been allowed.")
                                request.allow()
                            default:
                                break
                            }
                        }
                    })
                } else {
                    print("\(request.name) authorization failed. \(error?.localizedDescription ?? "")")
                }
            }
#endif
        }

        let api = PolySpatialRealityKitAccess.getApiData()
        let size = PolySpatialRealityKitAccess.getApiSize()
        SetPolySpatialNativeAPIImplementation(api, size)

        PolySpatialRealityKitAccess.register()
    }

    func application(_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey : Any]? = nil) -> Bool {
        var args = CommandLine.arguments
        args.append("-batchmode")

        unity.run(args: args)

        return true
    }

    func applicationDidBecomeActive(_ application: UIApplication) {
    }

    func application(_ application: UIApplication, configurationForConnecting connectingSceneSession: UISceneSession, options: UIScene.ConnectionOptions) -> UISceneConfiguration {
        let configuration = connectingSceneSession.configuration
        configuration.delegateClass = PolySpatialRealityKit.PolySpatialSceneDelegate.self
        return configuration
    }

    private func application(_ application: UIApplication, didDiscardSceneSession: UISceneSession) {
    }
}
