using System;

namespace UnityEngine.XR.VisionOS.InputDevices
{
    [Flags]
    public enum VisionOSSpatialPointerModifierKeys : ushort
    {
        CapsLock = 1,
        Control = 2,
        Alt = 4,
        Command = 8,
        Option = 16,
        Shift = 32,
        NumericPad = 64,
        FunctionKey = 128,
    }
}
