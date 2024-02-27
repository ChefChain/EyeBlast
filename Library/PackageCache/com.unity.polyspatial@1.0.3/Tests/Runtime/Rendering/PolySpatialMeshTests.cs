using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.PolySpatial.Internals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityObject = UnityEngine.Object;

namespace Tests.Runtime.Functional.Rendering
{
    public class PolySpatialMeshTests
    {
        public enum TestMeshType
        {
            Local,
            Persistent,
            Resource
        };

        static Vector3[] s_PlaneVertices =
        {
            new(-0.5f, 0.5f, 0f),
            new(0.5f, 0.5f, 0f),
            new(0.5f, -0.5f, 0f),
            new(-0.5f, -0.5f, 0f)
        };

        static Vector2[] s_PlaneUVs =
        {
            new(-0.5f, 0.5f),
            new(0.5f, 0.5f),
            new(0.5f, -0.5f),
            new(-0.5f, -0.5f)
        };

        static List<List<int[]>> s_TriangleIndices = new()
        {
            new()
            {
                new[] {0, 1, 3, 3, 1, 2}
            },
            new()
            {
                new[] {0, 1, 3},
                new[] {3, 1, 2}
            },
            new List<int[]>() {},
        };

        GameObject m_GameObject;
        GameObject m_CubePrimitive;
        Mesh m_Mesh;

        static void ConvertAndCheckMesh(Mesh mesh)
        {
            var representation = new AssetRepresentation(mesh);
            MeshConversionHelpers.ConvertMeshAssetToPolySpatialMesh(representation, (assetId, convertedMesh) =>
             {
                 Assert.IsTrue(convertedMesh.subMeshes.HasValue);
                 Assert.AreEqual(mesh.subMeshCount, convertedMesh.subMeshes.Value.Length,
                     "Unexpected number of sub meshes encountered.");

                 if (mesh.indexFormat == IndexFormat.UInt16)
                 {
                     Assert.IsTrue(convertedMesh.indices16.HasValue);
                     Assert.IsFalse(convertedMesh.indices32.HasValue);
                 }
                 else
                 {
                     Assert.IsFalse(convertedMesh.indices16.HasValue);
                     Assert.IsTrue(convertedMesh.indices32.HasValue);
                 }

                 var convertedSubMeshes = convertedMesh.subMeshes.Value;
                 var convertedSubMeshesIndices16 = convertedMesh.indices16.HasValue ? convertedMesh.indices16.Value : default;
                 var convertedSubMeshesIndices32 = convertedMesh.indices32.HasValue ? convertedMesh.indices32.Value : default;
                 for (var j = 0; j < mesh.subMeshCount; j++)
                 {
                     var indexCount = mesh.GetIndexCount(j);
                     Assert.AreEqual(convertedSubMeshes[j].indexCount, (int)indexCount,
                         "Sub mesh index count doesn't match.");

                     var subMeshIndices = mesh.GetIndices(j);
                     for (var i = 0; i < indexCount; i++)
                     {
                         if (mesh.indexFormat == IndexFormat.UInt16)
                         {
                             Assert.AreEqual(subMeshIndices[i], convertedSubMeshesIndices16[convertedSubMeshes[j].indexStart + i],
                                 "Converted sub mesh index is incorrect.");
                         }
                         else
                         {
                             Assert.AreEqual(subMeshIndices[i], (int) convertedSubMeshesIndices32[convertedSubMeshes[j].indexStart + i],
                                 "Converted sub mesh index is incorrect.");
                         }
                     }
                 }

                 var bindposes = mesh.bindposes;
                 var boneWeights = mesh.GetAllBoneWeights();
                 var bonesPerVertex = mesh.GetBonesPerVertex();

                 Assert.AreEqual(bindposes.Length > 0, convertedMesh.bindPoses.HasValue);
                 Assert.AreEqual(boneWeights.Length > 0, convertedMesh.boneWeights.HasValue);
                 Assert.AreEqual(bonesPerVertex.Length > 0, convertedMesh.bonesPerVertex.HasValue);

                 // Test bind poses and bone weights conversion.
                 if (convertedMesh.bindPoses.HasValue ||
                     convertedMesh.boneWeights.HasValue ||
                     convertedMesh.bonesPerVertex.HasValue)
                 {
                     Assert.IsTrue(convertedMesh.bindPoses.HasValue);
                     var convertedBindPoses = convertedMesh.bindPoses.Value;
                     Assert.AreEqual(bindposes.Length, convertedBindPoses.Length);
                     for (var a = 0; a < bindposes.Length; a++)
                     {
                         Assert.AreEqual(bindposes[a], convertedBindPoses[a]);
                     }

                     Assert.IsTrue(convertedMesh.boneWeights.HasValue);
                     var convertedBoneWeights = convertedMesh.boneWeights.Value;
                     Assert.AreEqual(boneWeights.Length, convertedBoneWeights.Length);
                     for (var b = 0; b < boneWeights.Length; b++)
                     {
                         Assert.AreEqual(boneWeights[b], convertedBoneWeights[b].ToUnity());
                     }

                     Assert.IsTrue(convertedMesh.bonesPerVertex.HasValue);
                     var convertedBonesPerVertex = convertedMesh.bonesPerVertex.Value;
                     Assert.AreEqual(bonesPerVertex.Length, convertedBonesPerVertex.Length);
                     for (var c = 0; c < bonesPerVertex.Length; c++)
                     {
                         Assert.AreEqual(bonesPerVertex[c], convertedBonesPerVertex[c]);
                     }
                 }
             });
        }

        [Test]
        public void Test_TriangleMesh_Converts_To_PolySpatialMesh([ValueSource(nameof(s_TriangleIndices))]List<int[]> indices)
        {
            m_Mesh = new Mesh();
            m_Mesh.vertices = s_PlaneVertices;
            m_Mesh.subMeshCount = indices.Count;

            for (var i = 0; i < m_Mesh.subMeshCount; i++)
            {
                m_Mesh.SetIndices(indices[i], MeshTopology.Triangles, i);
            }

            ConvertAndCheckMesh(m_Mesh);
        }

        [Test]
        public void Test_TriangleMesh32_Converts_To_PolySpatialMesh([ValueSource(nameof(s_TriangleIndices))]List<int[]> indices)
        {
            m_Mesh = new Mesh();
            m_Mesh.indexFormat = IndexFormat.UInt32;
            m_Mesh.vertices = s_PlaneVertices;
            m_Mesh.subMeshCount = indices.Count;

            for (var i = 0; i < m_Mesh.subMeshCount; i++)
            {
                m_Mesh.SetIndices(indices[i], MeshTopology.Triangles, i);
            }

            ConvertAndCheckMesh(m_Mesh);
        }

        static BoneWeight[] CreateBoneWeights()
        {
            var boneWeights = new BoneWeight[4];
            SetBoneWeight(ref boneWeights[0], 0, 0, 0, 0, 1, 0, 0, 0);
            SetBoneWeight(ref boneWeights[1], 2, 1, 0, 3, 0.25f, 0.25f, 0.25f, 0.25f);
            SetBoneWeight(ref boneWeights[2], 1, 2, 0, 0, 0.9f, 0.1f, 0, 0);
            SetBoneWeight(ref boneWeights[3], 0, 1, 2, 3, 0.5f, 0.3f, 0.125f, 0.075f);
            return boneWeights;
        }

        static void SetBoneWeight(ref BoneWeight boneWeight,
            int index0,
            int index1,
            int index2,
            int index3,
            float weight0,
            float weight1,
            float weight2,
            float weight3)
        {
            boneWeight.boneIndex0 = index0;
            boneWeight.boneIndex1 = index1;
            boneWeight.boneIndex2 = index2;
            boneWeight.boneIndex3 = index3;
            boneWeight.weight0 = weight0;
            boneWeight.weight1 = weight1;
            boneWeight.weight2 = weight2;
            boneWeight.weight3 = weight3;
        }

        static Matrix4x4[] CreateBindPoses()
        {
            var bindPoses = new Matrix4x4[2];

            bindPoses[0] = Matrix4x4.identity;

            var translation = new Vector3(0, 5, 0);
            var eulerAngles = new Vector3(90, 0, 10);
            var scale = new Vector3(0, 0, 5);

            var rotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
            bindPoses[1] = Matrix4x4.TRS(translation, rotation, scale);

            return bindPoses;
        }

        [Test]
        public void Test_BoneWeightsAndBindPoses_Converts_To_PolySpatialMesh()
        {
            m_Mesh = new Mesh();
            m_Mesh.vertices = s_PlaneVertices;
            m_Mesh.subMeshCount = s_TriangleIndices[1].Count;

            for (var i = 0; i < m_Mesh.subMeshCount; i++)
            {
                m_Mesh.SetIndices(s_TriangleIndices[1][i], MeshTopology.Triangles, i);
            }

            m_Mesh.bindposes = CreateBindPoses();
            m_Mesh.boneWeights = CreateBoneWeights();

            ConvertAndCheckMesh(m_Mesh);
        }

        [Test]
        public void Test_Mesh_With_Null_UV0_Converts_To_PolySpatialMesh()
        {
            var mesh = CreateMeshWithBlankUV0();
            ConvertAndCheckMesh(mesh);
        }

        [Test]
        public void Test_Mesh_With_Partial_Index_Coverage_Converts_To_PolySpatialMesh()
        {
            m_Mesh = new Mesh();
            m_Mesh.vertices = s_PlaneVertices;

            // Use an index buffer of size 100, but only use the six elements at the end.
            const int kIndexBufferSize = 100;
            m_Mesh.SetIndexBufferParams(kIndexBufferSize, IndexFormat.UInt32);
            var indices = s_TriangleIndices[0][0];
            var indexStart = kIndexBufferSize - indices.Length;
            m_Mesh.SetIndexBufferData(indices, 0, indexStart, indices.Length, MeshUpdateFlags.Default);
            m_Mesh.SetSubMesh(0, new(indexStart, indices.Length, MeshTopology.Triangles), MeshUpdateFlags.Default);

            ConvertAndCheckMesh(m_Mesh);
        }

        [UnityTest]
        public IEnumerator Test_Mesh_With_Null_UV0_Converts_To_UnitySceneGraph()
        {
            var mesh = CreateMeshWithBlankUV0();
            m_GameObject = new GameObject("PolySpatialMeshTestUV");
            m_GameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            m_GameObject.AddComponent<MeshRenderer>();

            // Wait a frame for mesh to be processed
            yield return null;
        }

        Mesh CreateMeshWithBlankUV0()
        {
            m_Mesh = new Mesh();
            m_Mesh.vertices = s_PlaneVertices;
            m_Mesh.SetIndices(s_TriangleIndices[0][0], MeshTopology.Triangles, 0);
            m_Mesh.uv = null;
            m_Mesh.uv2 = s_PlaneUVs;
            m_Mesh.RecalculateNormals();
            m_Mesh.RecalculateTangents();
            return m_Mesh;
        }

#if UNITY_EDITOR
        static IEnumerable<TestMeshType> TestMeshTypeEnum()
        {
            yield return TestMeshType.Local;
            yield return TestMeshType.Persistent;
            // The resource test is currently failing, due to ObjectDispatcher not tracking resources correctly [LXR-1071].
            // When this is fixed, enable the test.
            // yield return TestMeshType.Resource;
        }

        Mesh CreateMesh()
        {
            m_Mesh = new Mesh();
            m_Mesh.vertices = s_PlaneVertices;
            m_Mesh.subMeshCount = s_TriangleIndices[1].Count;
            return m_Mesh;
        }

        [UnityTest]
        public IEnumerator Test_Mesh_Asset_Changes_Are_Tracked([ValueSource(nameof(TestMeshTypeEnum))] TestMeshType testMeshType)
        {
            var testAssetPath = "Assets/Temp/TestMesh.asset";
            var testAssetFolder = Path.GetDirectoryName(testAssetPath);
            Mesh mesh = null;
            switch (testMeshType)
            {
                case TestMeshType.Local:
                    mesh = CreateMesh();
                    break;
                case TestMeshType.Resource:
                    m_CubePrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mesh = m_CubePrimitive.GetComponent<MeshFilter>().sharedMesh;
                    break;
                case TestMeshType.Persistent:
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(testAssetFolder), Path.GetFileName(testAssetFolder));
                    mesh = CreateMesh();
                    AssetDatabase.CreateAsset(mesh, testAssetPath);
                    Resources.UnloadAsset(mesh);
                    mesh = AssetDatabase.LoadAssetAtPath<Mesh>(testAssetPath);
                    break;
            }

            Assert.IsNotNull(mesh);

            m_GameObject = new GameObject("PolySpatialMeshTestAssetChanges");
            m_GameObject.AddComponent<MeshRenderer>();
            m_GameObject.AddComponent<MeshFilter>().sharedMesh = mesh;

            yield return null;

            var assetId = PolySpatialCore.LocalAssetManager.GetRegisteredAssetID(mesh);
            Assert.AreNotEqual(PolySpatialAssetID.InvalidAssetID, assetId);

            var convertedMesh = UnitySceneGraphAssetManager.Shared.GetRegisteredResource<Mesh>(assetId);

            Assert.AreEqual(mesh.vertexCount, convertedMesh.vertexCount);

            mesh.vertices = mesh.vertices.Concat(new[] { Vector3.one }).ToArray();

            yield return null;

            assetId = PolySpatialCore.LocalAssetManager.GetRegisteredAssetID(mesh);
            Assert.AreNotEqual(PolySpatialAssetID.InvalidAssetID, assetId);

            convertedMesh = UnitySceneGraphAssetManager.Shared.GetRegisteredResource<Mesh>(assetId);

            Assert.AreEqual(mesh.vertexCount, convertedMesh.vertexCount);

            m_GameObject.DestroyAppropriately();
            m_Mesh.DestroyAssetIrrecoverably();
            if (testMeshType == TestMeshType.Persistent)
            {
                AssetDatabase.DeleteAsset(testAssetPath);
                AssetDatabase.DeleteAsset(testAssetFolder);
            }

            yield return null;
        }
#endif

        [TearDown]
        public void TearDown()
        {
            if (m_CubePrimitive)
                m_CubePrimitive.DestroyAppropriately();

            if (m_GameObject)
                m_GameObject.DestroyAppropriately();

            if (m_Mesh)
                m_Mesh.DestroyAssetIrrecoverably();
        }
    }
}
