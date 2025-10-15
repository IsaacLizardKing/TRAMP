using UnityEngine;

[ExecuteInEditMode]
public class Icosahedron : MonoBehaviour {
    public Vector3[] vertices;
    public int[] indices;
    
    void Start() {
        
        float phi = 1.618033988749894f;
        vertices = new Vector3[] {
            new Vector3(-1, phi, 0).normalized,
            new Vector3(1, phi, 0).normalized,
            new Vector3(-1, -phi, 0).normalized,
            new Vector3(1, -phi, 0).normalized,
            new Vector3(0, -1, phi).normalized,
            new Vector3(0, 1, phi).normalized,
            new Vector3(0, -1, -phi).normalized,
            new Vector3(0, 1, -phi).normalized,
            new Vector3(phi, 0, -1).normalized,
            new Vector3(phi, 0, 1).normalized,
            new Vector3(-phi, 0, -1).normalized,
            new Vector3(-phi, 0, 1).normalized,
            Vector3.zero,
            Vector3.zero,
            Vector3.zero
        };

        indices = new int[] {
            0, 5, 11, 
            0, 1, 5,
            0, 1, 7,
            0, 7, 10,
            0, 10, 11,
            1, 5, 9,
            5, 4, 11,
            2, 10, 11,
            6, 7, 10,
            1, 7, 8,
            3, 4, 9,
            2, 3, 4,
            2, 3, 6,
            3, 6, 8,
            3, 8, 9,
            4, 5, 9,
            2, 4, 11,
            2, 6, 10,
            6, 7, 8,
            1, 8, 9,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0
        };
        subdivideTriangle(12, 0, 60);
    }

    void subdivideTriangle(int vertexPos, int TriIndexPos, int indexPos) {
        vertices[vertexPos] = vertices[indices[TriIndexPos]] + vertices[indices[TriIndexPos] + 1];
        vertices[vertexPos + 1] = vertices[indices[TriIndexPos] + 1] + vertices[indices[TriIndexPos] + 2];
        vertices[vertexPos + 2] = vertices[indices[TriIndexPos]] + vertices[indices[TriIndexPos] + 2];
        indices[indexPos] = vertexPos;
        indices[indexPos + 1] = vertexPos + 2;
        indices[indexPos + 2] = indices[TriIndexPos];
        indices[indexPos + 3] = vertexPos;
        indices[indexPos + 4] = vertexPos + 1;
        indices[indexPos + 5] = indices[TriIndexPos + 1];
        indices[indexPos + 6] = vertexPos + 1;
        indices[indexPos + 7] = vertexPos + 2;
        indices[indexPos + 8] = indices[TriIndexPos + 2];
        indices[TriIndexPos] = vertexPos;
        indices[TriIndexPos + 1] = vertexPos + 1;
        indices[TriIndexPos + 2] = vertexPos + 2;
    }

    void Update() {}
}