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
            new Vector3(-phi, 0, 1).normalized
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
            1, 8, 9
        };
    }
    void Update() {}
}