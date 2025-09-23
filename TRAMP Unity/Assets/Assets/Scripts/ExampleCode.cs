using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class SampleComputeMesh : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 color;
        public Vector2 uv0;
    }

    [Range(0, 32)] 
    public int Case;

    [SerializeField] Material material;
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Bounds bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000)); // set your value

    Mesh mesh;
    GraphicsBuffer indexBuffer;
    GraphicsBuffer vertexBuffer;
    float simulationTime;

    const int vertexCount = 9;
    const int indexCount = 9;

    void Start()
    {
        var meshFilter = this.gameObject.AddComponent<MeshFilter>();
        var meshRenderer = this.gameObject.AddComponent<MeshRenderer>();

        mesh = CreateMesh();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;

        indexBuffer = mesh.GetIndexBuffer();
        Debug.Log(indexBuffer);
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
        computeShader.SetInt("Case", Case);
        computeShader.SetBuffer(kernel, "IndexBuffer", indexBuffer);
        computeShader.SetBuffer(kernel, "VertexBuffer", vertexBuffer);
        computeShader.Dispatch(kernel, groups, 1, 1);

        simulationTime += Time.deltaTime;
        Debug.Log(mesh.vertices);
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

        var initialVertices = new Vertex[vertexCount] {
            new Vertex { position = new Vector3(1f, 0f, 0.5f), normal = new Vector3(0f, 0f, -1f), color = Vector4.one / 2, uv0 = new Vector2(0, 0) },
            new Vertex { position = new Vector3(0.25f, 0.5f, 1f), normal = new Vector3(0f, 0f, -1f), color = Vector4.one / 3, uv0 = new Vector2(0, 0) },
            new Vertex { position = new Vector3(0.75f, 0.5f, 1f), normal = new Vector3(0f, 0f, -1f), color = Vector4.one, uv0 = new Vector2(0, 0) },
            new Vertex { position = new Vector3(1f, 0f, 0.5f), normal = new Vector3(0f, 0f, 1f), color = Vector4.one / 2, uv0 = new Vector2(0, 0) },
            new Vertex { position = new Vector3(0.25f, 0.5f, 0f), normal = new Vector3(0f, 0f, 1f), color = Vector4.one / 3, uv0 = new Vector2(0, 0) },
            new Vertex { position = new Vector3(0.25f, 0.5f, 1f), normal = new Vector3(0f, 0f, 1f), color = Vector4.one, uv0 = new Vector2(0, 0) },
            new Vertex { position = new Vector3(0.5f, 0f, 0f), normal = new Vector3(0f, 0f, 1f), color = Vector4.one / 2, uv0 = new Vector2(0, 0) },
            new Vertex { position = new Vector3(0.25f, 0.5f, 0f), normal = new Vector3(0f, 0f, 1f), color = Vector4.one / 3, uv0 = new Vector2(0, 0) },
            new Vertex { position = new Vector3(1f, 0f, 0.5f), normal = new Vector3(0f, 0f, 1f), color = Vector4.one, uv0 = new Vector2(0, 0) }
        };
        mesh.SetVertexBufferData(initialVertices, 0, 0, vertexCount);

        mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
        var indices = new int[indexCount] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }; 
        mesh.SetIndexBufferData(indices, 0, 0, indexCount);

        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount), MeshUpdateFlags.DontRecalculateBounds);

        mesh.RecalculateNormals();

        return mesh;
    }
    
    void OnDrawGizmos() {
        Vector3[] samplePoints = new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(0.5f, 1f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0.5f, 1f, 1f),
            new Vector3(1f, 0f, 1f)
        };
        
        Vector3 offset = this.gameObject.transform.position;
        Gizmos.color = new Color (1f, 1f, 1f, 0.75f);
        
        Gizmos.DrawLine(samplePoints[0] + offset, samplePoints[1] + offset);
        Gizmos.DrawLine(samplePoints[1] + offset, samplePoints[2] + offset);
        Gizmos.DrawLine(samplePoints[0] + offset, samplePoints[2] + offset);
        Gizmos.DrawLine(samplePoints[3] + offset, samplePoints[4] + offset);
        Gizmos.DrawLine(samplePoints[4] + offset, samplePoints[5] + offset);
        Gizmos.DrawLine(samplePoints[3] + offset, samplePoints[5] + offset);
        Gizmos.DrawLine(samplePoints[0] + offset, samplePoints[3] + offset);
        Gizmos.DrawLine(samplePoints[1] + offset, samplePoints[4] + offset);
        Gizmos.DrawLine(samplePoints[2] + offset, samplePoints[5] + offset);

        
        Color green = new Color(0f, 1f, 0f, 0.75f);
        Color red = new Color(1f, 0f, 0f, 0.75f);
        int TempCase = Case;
        for(int i = 0; i < 6; i++) {
            if((TempCase & 1) == 1) { Gizmos.color = green; } 
            else { Gizmos.color = red; }
            Gizmos.DrawCube(offset + samplePoints[i], new Vector3(0.2f, 0.2f, 0.2f));
            TempCase = TempCase >> 1;
        }
    }
}

// https://discussions.unity.com/t/rendering-directly-from-compute-buffers/1559466/5
// From Arithmetica
