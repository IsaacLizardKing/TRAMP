using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class ExperimentComputeMesh : MonoBehaviour
{
    [Range(-1f, 10f)]
    public float Isolevel;

    [Range(0, 31)] 
    public int Case;


    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 color;
        public Vector2 uv0;
    }

    [SerializeField] Material material;
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Bounds bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000)); // set your value

    Mesh mesh;
    GraphicsBuffer indexBuffer;
    GraphicsBuffer vertexBuffer;
    float simulationTime;

    const int vertexCount = 18;
    const int indexCount = 18;

    void Start()
    {
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

        computeShader.SetFloat("Time", simulationTime);
        computeShader.SetFloat("Isolevel", Isolevel);
        computeShader.SetInt("Case", Case);
        computeShader.SetBuffer(kernel, "IndexBuffer", indexBuffer);
        computeShader.SetBuffer(kernel, "VertexBuffer", vertexBuffer);
        computeShader.Dispatch(kernel, groups, 1, 1);

        simulationTime += Time.deltaTime;
        
    }

    Mesh CreateMesh()
    {
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
}