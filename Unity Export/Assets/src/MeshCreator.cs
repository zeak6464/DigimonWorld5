#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;

public class MeshCreator : Editor
{

    [MenuItem("Assets/Create/Quad Mesh", false, 10000)]
    public static void Create()
    {
        Mesh mesh = BuildQuad(1, 1);
        string name = "Quad Mesh";
        mesh.name = name;
        AssetDatabase.CreateAsset(mesh, String.Format("Assets/src/Resources/{0}.asset", name));
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = mesh;
    }

    private static Mesh BuildQuad(float width, float height)
    {
        Mesh mesh = new Mesh();

        // Setup vertices
        Vector3[] newVertices = new Vector3[4];
        float halfHeight = height * 0.5f;
        float halfWidth = width * 0.5f;
        newVertices[0] = new Vector3(-halfWidth, -halfHeight, 0);
        newVertices[1] = new Vector3(-halfWidth, halfHeight, 0);
        newVertices[2] = new Vector3(halfWidth, -halfHeight, 0);
        newVertices[3] = new Vector3(halfWidth, halfHeight, 0);

        // Setup UVs
        Vector2[] newUVs = new Vector2[newVertices.Length];
        newUVs[0] = new Vector2(0, 0);
        newUVs[1] = new Vector2(0, 1);
        newUVs[2] = new Vector2(1, 0);
        newUVs[3] = new Vector2(1, 1);

        // Setup triangles
        int[] newTriangles = new int[] { 0, 1, 2, 3, 2, 1 };

        // Setup normals
        Vector3[] newNormals = new Vector3[newVertices.Length];
        for (int i = 0; i < newNormals.Length; i++)
        {
            newNormals[i] = Vector3.forward;
        }

        // Create quad
        mesh.vertices = newVertices;
        mesh.uv = newUVs;
        mesh.triangles = newTriangles;
        mesh.normals = newNormals;

        return mesh;
    }
}

#endif