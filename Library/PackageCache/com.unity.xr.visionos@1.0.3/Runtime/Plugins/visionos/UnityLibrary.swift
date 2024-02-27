import Foundation
import UnityFramework

 class UnityLibrary: UIResponder, UIApplicationDelegate, UnityFrameworkListener {

    public static var instance : UnityLibrary?

    private let unityFramework: UnityFramework

    public var view: UIView? {
        get {
            return self.unityFramework.appController()?.rootView
        }
    }

    public static func GetInstance() -> UnityLibrary? {
        if UnityLibrary.instance == nil {
            UnityLibrary.instance = UnityLibrary()
        }
        return UnityLibrary.instance!
    }

    private static func loadUnityFramework() -> UnityFramework? {
        let bundlePath = Bundle.main.bundlePath + "/Frameworks/UnityFramework.framework"
        let bundle = Bundle(path: bundlePath)
        if bundle?.isLoaded == false {
            bundle?.load()
        }

        let unityFramework = bundle?.principalClass?.getInstance()
        if unityFramework?.appController() == nil {
            let machineHeader = UnsafeMutablePointer<MachHeader>.allocate(capacity: 1)
            machineHeader.pointee = _mh_execute_header
            unityFramework!.setExecuteHeader(machineHeader)
        }

        return unityFramework
    }

    internal override init() {
        self.unityFramework = UnityLibrary.loadUnityFramework()!
        //self.unityFramework.setDataBundleId("com.unity3d.framework")

        super.init()

        self.unityFramework.register(self)
    }

    public func run(args: [String]) {

        // Passing to Unity requires re-creating the standard argv array.
        let argv = UnsafeMutablePointer<UnsafeMutablePointer<Int8>?>.allocate(capacity: args.count)

        for i in 0..<args.count {
            if let cp = strdup(args[i]) {
                argv[i] = cp
            }
        }
        
        unityFramework.runEmbedded(withArgc: Int32(args.count), argv: argv, appLaunchOpts: nil)
    }

    public func show() {
        self.unityFramework.showUnityWindow()
    }

    public func show(controller: UIViewController) {
        self.unityFramework.showUnityWindow()
        if let view = self.view {
            controller.view?.addSubview(view)
        }
    }

    public func unload() {
        self.unityFramework.unloadApplication()
    }

    internal func unityDidUnload(_ notification: Notification!) {
        unityFramework.unregisterFrameworkListener(self)
        UnityLibrary.instance = nil;
    }

    public func didBecomeActive() {
        unityFramework.appController().applicationDidBecomeActive(UIApplication.shared)
    }

    public func willResignActive() {
        unityFramework.appController().applicationWillResignActive(UIApplication.shared)
    }

    public func didEnterBackground() {        
        unityFramework.appController().applicationDidEnterBackground(UIApplication.shared)
    }

    public func willEnterForeground() {
        unityFramework.appController().applicationWillEnterForeground(UIApplication.shared)
    }
}
