using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class HighResComputeMesh : MonoBehaviour
{
    [Range(-10f, 10f)]
    public float Isolevel;

    [Range(0, 32)] 
    public int Case;

    [Range(0, 100)] 
    public int Depth;

    [Range(0.0000000000000001f, 1)]
    public float lerpSpeed;
    

    [SerializeField] bool interpolate;
    [SerializeField] bool truncate;
    [SerializeField] bool randomVertexColoring;
    [SerializeField] bool volumeRules;
    [SerializeField] bool facingRules;


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

    [SerializeField] Material material;
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Bounds bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000)); // set your value

    Mesh mesh;
    [SerializeField] Mesh Wesh;
    GraphicsBuffer indexBuffer;
    GraphicsBuffer vertexBuffer;
    float simulationTime;

    private int vertexCount = 4608;
    private int indexCount  = 4608;

    void Start()
    {
        rays = Wesh.GetVertexBuffer(0);
        raysIndexes = Wesh.GetIndexBuffer();
        Wesh.name = "gorboobol";

        var meshFilter = this.gameObject.AddComponent<MeshFilter>();
        var meshRenderer = this.gameObject.AddComponent<MeshRenderer>();

        mesh = CreateMesh();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;

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

        if(Depth * 64 * 9 != vertexCount) {
            mesh = CreateMesh();
            var meshFilter = this.gameObject.GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            indexBuffer = mesh.GetIndexBuffer();
            vertexBuffer = mesh.GetVertexBuffer(0);
        }
        if(Wesh.name != "gorboobol") {
            rays = Wesh.GetVertexBuffer(0);
            raysIndexes = Wesh.GetIndexBuffer();
            Wesh.name = "gorboobol";
        }

        computeShader.SetFloat("Time", simulationTime);
        computeShader.SetFloat("Isolevel", Isolevel);
        computeShader.SetFloat("lerpSpeed", lerpSpeed);
        computeShader.SetInt("Case", Case);
        computeShader.SetInt("Depth", Depth);
        computeShader.SetInt("Settings", (interpolate ? 1 : 0) | (truncate ? 2 : 0) | (randomVertexColoring ? 4 : 0) | (volumeRules ? 8 : 0) | (facingRules ? 16 : 0));
        computeShader.SetVector("offset", this.transform.position);
        computeShader.SetBuffer(kernel, "IndexBuffer", indexBuffer);
        computeShader.SetBuffer(kernel, "VertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kernel, "rays", Wesh.GetVertexBuffer(0));
        computeShader.SetBuffer(kernel, "raysIndexes", Wesh.GetIndexBuffer());
        computeShader.Dispatch(kernel, groups, 1, 1); 

        simulationTime += Time.deltaTime;
        
    }

    Mesh CreateMesh()
    {
        vertexCount = Depth * 64 * 9;
        indexCount = Depth * 64 * 9;
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

    float Sample(Vector3 pos) {

        float x = pos.x;
        float y = pos.y;
        float z = pos.z;
        float d = Mathf.Sqrt(x * x + y * y + z * z);
        return Mathf.Sin(d) - Mathf.Sin(x) + Mathf.Sin(y) - y * 0.01f;
    }

    float Magnitude(Vector3 pos) {
        float x = pos.x;
        float y = pos.y;
        float z = pos.z;
        return Mathf.Sqrt(x * x + y * y + z * z);
    }

    void OnDrawGizmosSelected() {
        
    }
}