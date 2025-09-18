using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class ExperimentComputeMesh : MonoBehaviour
{
    [Range(-10f, 10f)]
    public float Isolevel;

    [Range(0, 32)] 
    public int Case;

    [Range(0, 100)] 
    public int Depth;

    [Range(0.0000000000000001f, 1)]
    public float lerpSpeed;

    [Range(0, 10f)]
    public float gizmosCooldown;
    float gizmosTime;
    

    [SerializeField] bool interpolate;
    [SerializeField] bool truncate;
    [SerializeField] bool randomVertexColoring;


    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 color;
        public Vector2 uv0;
    }

    GraphicsBuffer rays; 
    GraphicsBuffer raysIndexes;
    Vector3[] raysCPU; 
    int[] raysIndexesCPU;
    
    

    [SerializeField] Material material;
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Bounds bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000)); // set your value

    Mesh mesh;
    Mesh Wesh;
    GraphicsBuffer indexBuffer;
    GraphicsBuffer vertexBuffer;
    float simulationTime;

    private int vertexCount = 4608;
    private int indexCount  = 4608;

    void Start()
    {
        var meshFilter = this.gameObject.AddComponent<MeshFilter>();
        var meshRenderer = this.gameObject.AddComponent<MeshRenderer>();

        mesh = CreateMesh();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;

        makeDaRays();

        indexBuffer = mesh.GetIndexBuffer();
        vertexBuffer = mesh.GetVertexBuffer(0);
    }

    void OnDestroy()
    {
        indexBuffer.Dispose();
        indexBuffer = null; // for GC

        vertexBuffer.Dispose();
        vertexBuffer = null; // for GC

        Destroy(mesh);
        mesh = null; // for GC
    }

    private void Update()
    {
        var kernel = computeShader.FindKernel("CSMain");
        computeShader.GetKernelThreadGroupSizes(kernel, out var x, out _, out _);
        var groups = (indexCount + (int)x - 1) / (int)x;

        if(Depth * 32 * 9 != vertexCount) {
            mesh = CreateMesh();
            var meshFilter = this.gameObject.GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            indexBuffer = mesh.GetIndexBuffer();
            vertexBuffer = mesh.GetVertexBuffer(0);
        }

        computeShader.SetFloat("Time", simulationTime);
        computeShader.SetFloat("Isolevel", Isolevel);
        computeShader.SetFloat("lerpSpeed", lerpSpeed);
        computeShader.SetInt("Case", Case);
        computeShader.SetInt("Depth", Depth);
        computeShader.SetInt("Settings", (interpolate ? 1 : 0) | (truncate ? 2 : 0) | (randomVertexColoring ? 4 : 0));
        computeShader.SetVector("offset", this.transform.position);
        computeShader.SetBuffer(kernel, "IndexBuffer", indexBuffer);
        computeShader.SetBuffer(kernel, "VertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kernel, "rays", rays);
        computeShader.SetBuffer(kernel, "raysIndexes", raysIndexes);
        computeShader.Dispatch(kernel, groups, 1, 1); 

        simulationTime += Time.deltaTime;
        
    }

    Mesh CreateMesh()
    {
        vertexCount = Depth * 32 * 9;
        indexCount = Depth * 32 * 9;
        var mesh = new Mesh();
        mesh.name = "TestComputeMesh";

        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured; // for access as StructuredBuffer from compute shaders
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Structured; // for access as StructuredBuffer from compute shaders

        mesh.bounds = bounds;

        var vertexLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
        };
        mesh.SetVertexBufferParams(vertexCount, vertexLayout);

        var initialVertices = new Vertex[vertexCount];

        mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
        var indices = new int[indexCount]; 
        for(int i = 0; i < vertexCount; i++) {
            initialVertices[i] = new Vertex { position = new Vector3(0f, 0f, 0f), normal = new Vector3(0f, 0f, -1f), color = Vector4.one / 2, uv0 = new Vector2(0, 0) };
            indices[i] = i;
        }
        mesh.SetVertexBufferData(initialVertices, 0, 0, vertexCount);

        mesh.SetIndexBufferData(indices, 0, 0, indexCount);

        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount), MeshUpdateFlags.DontRecalculateBounds);

        mesh.RecalculateNormals();

        return mesh;
    }

    void makeDaRays() {
        var initalRays = new Vector3[12];
        float phi = 1.618033988749894f;
        initalRays[0] = new Vector3(-1, phi, 0);
        initalRays[1] = new Vector3(1, phi, 0);
        initalRays[2] = new Vector3(-1, -phi, 0);
        initalRays[3] = new Vector3(1, -phi, 0);
        initalRays[4] = new Vector3(0, -1, phi);
        initalRays[5] = new Vector3(0, 1, phi);
        initalRays[6] = new Vector3(0, -1, -phi);
        initalRays[7] = new Vector3(0, 1, -phi);
        initalRays[8] = new Vector3(phi, 0, -1);
        initalRays[9] = new Vector3(phi, 0, 1);
        initalRays[10] = new Vector3(-phi, 0, -1);
        initalRays[11] = new Vector3(-phi, 0, 1);

        var initialRays = new Vertex[12];
        for(int i = 0; i < 12; i++) {
            initalRays[i].Normalize();
            initialRays[i] = new Vertex { position = initalRays[i], normal = new Vector3(0f, 0f, -1f), color = Vector4.zero, uv0 = new Vector2(0, 0) };
        }

        var initialRaysIndexes = new int[] {
            0, 11, 5, 
            0, 5, 1,
            0, 1, 7,
            0, 7, 10,
            0, 10, 11,
            1, 5, 9,
            5, 11, 4,
            11, 10, 2,
            10, 7, 6,
            7, 1, 8,
            3, 9, 4,
            3, 4, 2,
            3, 2, 6,
            3, 6, 8,
            3, 8, 9,
            4, 9, 5,
            2, 4, 11,
            6, 2, 10,
            8, 6, 7,
            9, 8, 1,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0 
        };

        Wesh = new Mesh();
        Wesh.name = "Icosahedron";

        Wesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured; // for access as StructuredBuffer from compute shaders
        Wesh.indexBufferTarget |= GraphicsBuffer.Target.Structured; // for access as StructuredBuffer from compute shaders

        Wesh.bounds = bounds;

        var vertexLayout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
        };

        Wesh.SetVertexBufferParams(12, vertexLayout);
        Wesh.SetIndexBufferParams(96, IndexFormat.UInt32);

        Wesh.SetVertexBufferData(initialRays, 0, 0, 12);
        Wesh.SetIndexBufferData(initialRaysIndexes, 0, 0, 96);

        Wesh.subMeshCount = 1;
        Wesh.SetSubMesh(0, new SubMeshDescriptor(0, 96), MeshUpdateFlags.DontRecalculateBounds);

        Wesh.RecalculateNormals();

        rays = Wesh.GetVertexBuffer(0);
        raysIndexes = Wesh.GetIndexBuffer();
        raysCPU = initalRays;
        raysIndexesCPU = initialRaysIndexes;
    }

    float Sample(Vector3 pos) {

        float x = pos.x;
        float y = pos.y;
        float z = pos.z;
        float d = Mathf.Sqrt(x * x + y * y + z * z);
        return -y - z;//Mathf.Sin(d) + Mathf.Sin(x) - Mathf.Sin(y) - y;
    }

    float Magnitude(Vector3 pos) {
        float x = pos.x;
        float y = pos.y;
        float z = pos.z;
        return Mathf.Sqrt(x * x + y * y + z * z);
    }

    void OnDrawGizmosSelected() {
        makeDaRays();
        Vector3 offset = this.gameObject.transform.position;
        Gizmos.color = new Color (1f, 1f, 1f, 0.75f);
        float scale = 0.1f;
        for(int i = 0; i <= Depth; i++) {
            for(int j = 0; j < 20; j++) {
                Gizmos.DrawLine(offset + raysCPU[raysIndexesCPU[j * 3]] * scale, offset + raysCPU[raysIndexesCPU[j * 3 + 1]] * scale);
                Gizmos.DrawLine(offset + raysCPU[raysIndexesCPU[j * 3 + 2]] * scale, offset + raysCPU[raysIndexesCPU[j * 3 + 1]] * scale);
                Gizmos.DrawLine(offset + raysCPU[raysIndexesCPU[j * 3 + 2]] * scale, offset + raysCPU[raysIndexesCPU[j * 3]] * scale);
            }
            scale += Magnitude(raysCPU[0] * scale - raysCPU[1] * scale);
        }
        Gizmos.color = new Color(0.75f, 0.75f, 0.0f, 0.75f);
        for(int i = 0; i < 12; i++) {
            Gizmos.DrawRay(offset, offset + raysCPU[i] * Depth * 100);
        }
        Color green = new Color(0f, 1f, 0f, 0.75f);
        Color red = new Color(1f, 0f, 0f, 0.75f);
        scale = 0.05f;
        for(int i = 0; i <= Depth + 1; i++) {
            for(int j = 0; j < 12; j++) {
                float size = Sample(raysCPU[j] * scale) / Isolevel;
                if(size >= 1f) { Gizmos.color = green; } 
                else { Gizmos.color = red; }
                size = Mathf.Clamp(size, 0.2f, 0.3f) * Mathf.Sqrt(scale);
                Gizmos.DrawCube(offset + raysCPU[j] * scale, new Vector3(size, size, size));
            }
            scale += Magnitude(raysCPU[0] * scale - raysCPU[1] * scale);
        }
    }
}