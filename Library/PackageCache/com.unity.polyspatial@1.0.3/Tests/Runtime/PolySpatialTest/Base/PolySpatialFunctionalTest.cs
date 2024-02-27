using System.Collections;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial;
using UnityEngine;
using UnityEngine.TestTools;


namespace Tests.Runtime.PolySpatialTest.Base
{
    /// <summary>
    /// A PolySpatial functional test base class, used for testing PolySpatial components such as PolySpatialMeshRenderer and
    /// PolySpatialLight in an integrated environment. It contains shared variables and default values
    /// for common attributes and properties, as well as helper methods for functional testing.
    /// </summary>
    public class PolySpatialFunctionalTest: PolySpatialTest
    {
        protected readonly TestSceneBuilder k_TestSceneBuilder;

        public PolySpatialFunctionalTest(bool enableVisibleScene=false)
        {
            k_TestSceneBuilder = new TestSceneBuilder(enableVisibleScene);
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            // Note: if visible Scene disabled, this call will be a no-op
            k_TestSceneBuilder.BuildVisibleScene();
            yield return null;
        }

        [UnityTearDown]
        public new IEnumerator TearDown()
        {
            k_TestSceneBuilder.DestroyVisibleScene();
            yield return new WaitForDone(
                2f,
                () => k_TestSceneBuilder.CameraGameObject == null && k_TestSceneBuilder.LightGameObject == null);
        }

        /// <summary>
        /// Creates a new QMR GameObject with no extra attributes and validates the GO was created (simulation only)
        /// along with any expected resources added to it.
        /// </summary>
        /// <param name="testGameObjectName">QMR GO name value</param>
        /// <param name="mesh">Optional Mesh to set on the QMR GO</param>
        /// <param name="materials">Optional PolySpatialMaterial array to assign to the QMR GO</param>
        protected void CreateAndValidateQMRGameObject(
            string testGameObjectName,
            Mesh mesh=null,
            Material[] materials=null)
        {
            m_TestGameObject = TestSceneActions.CreatePolySpatialGameObject(
                out m_TestPolySpatialMeshRenderer,
                testGameObjectName,
                mesh,
                materials);

            // 1. Validate the sim GO was created
            TestSceneValidations.PolySpatialGameObjectCreated(m_TestGameObject);

            // 2. Validate the target mesh was set on the sim GO
            if (mesh != null)
                TestSceneValidations.GameObjectHasComponent(m_TestGameObject, mesh);

            // 3. Validate materials are set
            TestSceneValidations.PolySpatialMeshRendererHasMaterials(m_TestPolySpatialMeshRenderer, materials);
        }
    }
}
