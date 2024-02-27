using System;
using System.Collections.Generic;
using Tests.Runtime.Functional.Components;
using Unity.Collections;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;

namespace Tests.Runtime.Functional
{
    /// <summary>
    /// This class implements a command handler that allows for before/after callbacks around
    /// certain message sends.
    ///
    /// If you look at <see cref="ComponentTestBase"/> you can see how it's used to insert
    /// itself into the active pipeline.
    ///
    /// Usage example:
    /// <example>
    ///
    /// wrapper.OnBeforeAssetsDeletedCalled = (assetIds) =>
    /// {
    ///     ....Test expected pre conditions...
    /// };
    ///
    ///
    /// wrapper.OnAfterAssetsDeletedCalled = (assetIds) =>
    /// {
    ///     ....Test expected post conditions...
    /// };
    /// </example>
    ///
    /// </summary>
    class PolySpatialCorePlatformTestWrapper : IPolySpatialCommandHandler, IPolySpatialCommandDispatcher
    {
        public IPolySpatialCommandHandler NextHandler { get; set; }

        public unsafe void HandleCommand(PolySpatialCommand cmd, int argCount, void** argValues, int* argSizes)
        {
            if (cmd == PolySpatialCommand.DeleteAsset)
            {
                PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialAssetID* aid);
                var buf = PolySpatialUtils.GetNativeArrayForBuffer<PolySpatialAssetID>(aid, 1);

                OnBeforeAssetsDeletedCalled?.Invoke(buf);
                NextHandler?.HandleCommand(cmd, argCount, argValues, argSizes);
                OnAfterAssetsDeletedCalled?.Invoke(buf);
            }
            else if (cmd == PolySpatialCommand.SetVolumeCameraData)
            {
                PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialInstanceID* id, out PolySpatialVolumeCameraData* cameraData);
                OnBeforeVolumeCameraChangedCalled?.Invoke(*id, *cameraData);
                NextHandler?.HandleCommand(cmd, argCount, argValues, argSizes);
                OnAfterVolumeCameraChangedCalled?.Invoke(*id, *cameraData);
            }
            else if (cmd == PolySpatialCommand.SetParticleSystemData)
            {
                var changeList = PolySpatialArgs.ExtractChangeListSerializedFromArgs<PolySpatialParticleSystemData>(argCount, argValues, argSizes);
                OnBeforeParticleSystemChangedCalled?.Invoke(changeList);
                NextHandler?.HandleCommand(cmd, argCount, argValues, argSizes);
                OnAfterParticleSystemChangedCalled?.Invoke(changeList);
            }
            else
            {
                NextHandler?.HandleCommand(cmd, argCount, argValues, argSizes);
            }
        }

        public Action<NativeArray<PolySpatialAssetID>> OnBeforeAssetsDeletedCalled;
        public Action<NativeArray<PolySpatialAssetID>> OnAfterAssetsDeletedCalled;

        public Action<PolySpatialInstanceID, PolySpatialVolumeCameraData> OnBeforeVolumeCameraChangedCalled;
        public Action<PolySpatialInstanceID, PolySpatialVolumeCameraData> OnAfterVolumeCameraChangedCalled;

        public Action<IChangeList<PolySpatialParticleSystemData>> OnBeforeParticleSystemChangedCalled;
        public Action<IChangeList<PolySpatialParticleSystemData>> OnAfterParticleSystemChangedCalled;

        public PolySpatialCorePlatformTestWrapper(IPolySpatialCommandHandler backendHandler)
        {
            NextHandler = backendHandler;
        }
    }
}
