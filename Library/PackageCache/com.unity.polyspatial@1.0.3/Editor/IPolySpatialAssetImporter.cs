using UnityEditor.AssetImporters;

namespace Unity.PolySpatial.Internals
{
    public interface IPolySpatialAssetImporter
    {
        bool IsInterestedInAsset(string path);
        void OnImportAsset(AssetImportContext ctx);
    }
}