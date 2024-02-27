using System.Collections.Generic;
using Unity.XR.CoreUtils.Editor.BuildingBlocks;

namespace UnityEditor.PolySpatial.BuildingBlocks
{
    [BuildingBlockItem(Priority = k_SectionPriority)]
    internal class SetupBuildingBlocksSectionPolySpatial : IBuildingBlockSection
    {
        const string k_SectionId = "Setup";
        public string SectionId => k_SectionId;

        const string k_SectionLightIconPath = "Packages/com.unity.polyspatial/Editor/BuildingBlocks/Icons/SectionIcon/SetupSectionIconLight.png";
        const string k_SectionDarkIconPath = "Packages/com.unity.polyspatial/Editor/BuildingBlocks/Icons/SectionIcon/SetupSectionIconDark.png";
        public string SectionIconPath => EditorGUIUtility.isProSkin ? k_SectionDarkIconPath : k_SectionLightIconPath;
        const int k_SectionPriority = 1;

        readonly IBuildingBlock[] m_PolySpatialBuildingBlocksElementIds = new IBuildingBlock[]
        {
            VolumeCameraBuildingBlock.instance
        };

        public IEnumerable<IBuildingBlock> GetBuildingBlocks()
        {
            return m_PolySpatialBuildingBlocksElementIds;
        }
    }
}
