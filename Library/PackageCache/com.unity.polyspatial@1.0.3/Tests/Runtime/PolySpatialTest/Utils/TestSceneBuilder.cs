using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using UnityEngine;

namespace Tests.Runtime.PolySpatialTest.Utils
{
    /// <summary>
    /// This class assists with setting up a test Scene, specifically with enabling visual elements to render the Scene.
    /// Since some of the GameObjects created by this class are PolySpatial GameObjects, it maintains a count of created
    /// PolySpatial GameObjects and exposes said attribute so tests may take these GameObjects into consideration when
    /// checking all PolySpatial backing GameObjects.
    /// </summary>
    public class TestSceneBuilder
    {
        // start: these members are used for building a visible Scene
        private const string k_TestPolySpatialLightName = "PolySpatial Test Light";
        private const string k_TestCameraName = "Test Camera";
        private const float k_TestLightIntensity = 0.5f;
        private const float k_VisibleGameObjectZOffset = 2f;
        private readonly Color k_TestLightColor;
        private readonly bool k_EnableVisibleScene;
        private readonly Vector3 k_TestCameraPosition;
        private readonly Vector3 k_TestLightPosition;

        public GameObject CameraGameObject{ get; private set; }
        public GameObject LightGameObject { get; private set; }

        public int PolySpatialGameObjectCount =>
            // Note: currently only up to 1 PolySpatial GameObject is managed by this class; update this method
            // if we add more PolySpatial GameObjects.
            (m_PolySpatialLight != null) ? 1 : 0;

        private Camera m_Camera;
        private Light m_PolySpatialLight;
        // end: these members are used for building a visible Scene

        public TestSceneBuilder(bool enableVisibleScene)
        {
            k_EnableVisibleScene = enableVisibleScene;
            k_TestLightColor = Color.yellow;
            k_TestCameraPosition = new Vector3(0f, 0f, -1f);
            k_TestLightPosition = new Vector3(0f, 1.5f, 0f);
        }

        /// <summary>
        /// Sets up current Scene to enable visual rendering during test execution, allowing a user
        /// to see the Scene and its content (such as the test GameObject).
        /// </summary>
        public void BuildVisibleScene()
        {
            if (!k_EnableVisibleScene) return;

            // add a camera to the Scene and adjust position
            CameraGameObject = new GameObject(k_TestCameraName);
            m_Camera = CameraGameObject.AddComponent<Camera>();
            CameraGameObject.transform.position = k_TestCameraPosition;

            // add PolySpatialLight to allow for PolySpatial GameObject illumination
            LightGameObject = new GameObject(k_TestPolySpatialLightName);
            m_PolySpatialLight = LightGameObject.AddComponent<Light>();
            m_PolySpatialLight.type = LightType.Directional;
            // move the PolySpatialLight up above test GameObjects
            LightGameObject.transform.position = k_TestLightPosition;

            // set PolySpatialLight color and intensity
            m_PolySpatialLight.color = k_TestLightColor;
            m_PolySpatialLight.intensity = k_TestLightIntensity;
        }

        /// <summary>
        /// Moves the test GameObject instance into the Scene's camera's view.
        /// </summary>
        public void SetTestGameObjectInView(GameObject testGameObject)
        {
            if (!k_EnableVisibleScene) return;

            if(!m_Camera)
            {
                Debug.Log("No camera in Scene; test GameObject not moved into view.");
                return;
            }

            if (!testGameObject)
            {
                Debug.Log("No test GameObject found; test GameObject not moved into view.");
                return;
            }

            // get camera position
            var cameraPosition = CameraGameObject.transform.position;

            // put the test GameObject in front of the camera (z)
            testGameObject.transform.position = new Vector3(
                cameraPosition.x,
                cameraPosition.y,
                cameraPosition.z + k_VisibleGameObjectZOffset);
        }

        public void DestroyVisibleScene()
        {
            if (CameraGameObject)
            {
                Object.Destroy(CameraGameObject);
            }

            if (LightGameObject)
            {
                LightGameObject.DestroyAppropriately();
            }
        }
    }
}
