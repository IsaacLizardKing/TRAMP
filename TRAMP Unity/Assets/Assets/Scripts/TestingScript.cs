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
    
    [Range(0, 100)] 
    public int Subdivisions;

    [Range(0.0000000000000001f, 1)]
    public float lerpSpeed;

    [Range(0, 1f)]
    public float CullCushion;
    
    
    

    [SerializeField] bool interpolate;
    [SerializeField] bool truncate;
    [SerializeField] bool randomVertexColoring;
    [SerializeField] bool volumeRules;
    [SerializeField] bool facingRules;
    [SerializeField] bool Square;
    [SerializeField] bool Icosahedron;
    [SerializeField] bool Culling;


    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 color;
        public Vector2 uv0;
    }

    GraphicsBuffer rays;
    GraphicsBuffer raysIndices;
    Vector3[] raysCPU;
    int[] rcpuvrc; // raysCPU vertices' reference count
    int[] raysIndicesCPU;
    int tris;
    

    [SerializeField] Material material;
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Bounds bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000)); // set your value
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] Camera Cam;

    Mesh mesh;
    Mesh Wesh;
    GraphicsBuffer indexBuffer;
    GraphicsBuffer vertexBuffer;
    float simulationTime;

    private int vertexCount = 4608;
    private int indexCount  = 4608;

    private int triCount;
    private int vertCount;

    void Start()
    {

        mesh = CreateMesh();
        Debug.Log(meshFilter);
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;

        makeDaRays();

        indexBuffer = mesh.GetIndexBuffer();
        vertexBuffer = mesh.GetVertexBuffer(0);
    }

    void Awake()
    {

        makeDaRays();
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
        makeDaRays();
        var kernel = computeShader.FindKernel("CSMain");
        computeShader.GetKernelThreadGroupSizes(kernel, out var x, out _, out _);
        var groups = (triCount + (int)x - 1) / (int)x;

        if(Depth * 64 * 9 != vertexCount) {
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
        computeShader.SetInt("Settings", (interpolate ? 1 : 0) | (truncate ? 2 : 0) | (randomVertexColoring ? 4 : 0) | (volumeRules ? 8 : 0) | (facingRules ? 16 : 0));
        computeShader.SetVector("offset", this.transform.position);
        computeShader.SetBuffer(kernel, "IndexBuffer", indexBuffer);
        computeShader.SetBuffer(kernel, "VertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kernel, "rays", rays);
        computeShader.SetBuffer(kernel, "raysIndices", raysIndices);
        computeShader.Dispatch(kernel, groups, 1, 1); 

        simulationTime += Time.deltaTime;
        
    }

    Mesh CreateMesh()
    {
        makeDaRays();
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
        Debug.Log("meow4");
        Debug.Log(indexBuffer);

        return mesh;
    }

    void makeDaRays() {
        Vector3[] vertsImport;
        int[] indicesImport;
        int[] tempRcpuvrc;
        if(Square) {
            vertsImport = SquareV();
            indicesImport = SquareI();
            triCount = 12;
            vertCount = 8;
            tempRcpuvrc = rcpuvrcSqr();
        }
        else if(Icosahedron) {
            vertsImport = IcosahedronV();
            indicesImport = IcosahedronI();
            triCount = 20;
            vertCount = 12;
            tempRcpuvrc = rcpuvrcIco();
        } else {
            vertsImport = IcosahedronV();
            indicesImport = IcosahedronI();
            triCount = 20;
            vertCount = 12;
            tempRcpuvrc = rcpuvrcIco();
        }

        raysCPU = new Vector3[vertsImport.Length + 3 * Subdivisions];
        for(int i = 0; i < vertsImport.Length; i++) { raysCPU[i] = vertsImport[i]; }
        
        rcpuvrc = new int[vertsImport.Length + 3 * Subdivisions]; 
        for(int i = 0; i < tempRcpuvrc.Length; i++) { rcpuvrc[i] = tempRcpuvrc[i]; }
        
        raysIndicesCPU = new int[indicesImport.Length + 9 * Subdivisions];

        for(int i = 0; i < indicesImport.Length; i++) { raysIndicesCPU[i] = indicesImport[i]; }

        if(culling){
            triCount += cullMaster(0) / 3 - triCount;
            vertCount += cullMasterV(0) - vertCount;
        }

        for(int i = 0, io = triCount * 3, v = vertCount; i < Subdivisions; i += 1, v += 3, io += 9){
            subdivideTriangle(v, i * 3, io);
            triCount += 3;
            vertCount += 3;
        }
        if(Culling){
            triCount = cullMaster(0) / 3;
            vertCount = cullMasterV(0);
        }

        var initialRays = new Vertex[raysCPU.Length];
        for(int i = 0; i < initialRays.Length; i++) {
            raysCPU[i].Normalize();
            initialRays[i] = new Vertex { position = raysCPU[i], normal = new Vector3(0f, 0f, -1f), color = Vector4.zero, uv0 = new Vector2(0, 0) };
        }
        
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

        Wesh.SetVertexBufferParams(initialRays.Length, vertexLayout);
        Wesh.SetIndexBufferParams(raysIndicesCPU.Length, IndexFormat.UInt32);

        Wesh.SetVertexBufferData(initialRays, 0, 0, initialRays.Length);
        Wesh.SetIndexBufferData(raysIndicesCPU, 0, 0, raysIndicesCPU.Length);

        Wesh.subMeshCount = 1;
        Wesh.SetSubMesh(0, new SubMeshDescriptor(0, raysIndicesCPU.Length), MeshUpdateFlags.DontRecalculateBounds);

        Wesh.RecalculateNormals();

        rays = Wesh.GetVertexBuffer(0);
        raysIndices = Wesh.GetIndexBuffer();
    }

    void alterRaysIndices(int indexPos, int newVal){
        rcpuvrc[raysIndicesCPU[indexPos]] -= 1;
        raysIndicesCPU[indexPos] = newVal;
        rcpuvrc[newVal] += 1;
    }

    void subdivideTriangle(int vertexPos, int TriIndexPos, int indexPos) {
        raysCPU[vertexPos] = (raysCPU[raysIndicesCPU[TriIndexPos]] + raysCPU[raysIndicesCPU[TriIndexPos + 1]]) / 2;
        raysCPU[vertexPos].Normalize();
        raysCPU[vertexPos + 1] = (raysCPU[raysIndicesCPU[TriIndexPos + 1]] + raysCPU[raysIndicesCPU[TriIndexPos + 2]]) / 2;
        raysCPU[vertexPos + 1].Normalize();
        raysCPU[vertexPos + 2] = (raysCPU[raysIndicesCPU[TriIndexPos]] + raysCPU[raysIndicesCPU[TriIndexPos + 2]]) / 2;
        raysCPU[vertexPos + 2].Normalize();

        rcpuvrc[0] += 9; // compensates for all of these new triangles subtracting from 0's reference counter;
        alterRaysIndices(indexPos, raysIndicesCPU[TriIndexPos]);
        alterRaysIndices(indexPos + 1, vertexPos + 2);
        alterRaysIndices(indexPos + 2, vertexPos);
        alterRaysIndices(indexPos + 3, vertexPos);
        alterRaysIndices(indexPos + 4, raysIndicesCPU[TriIndexPos + 1]);
        alterRaysIndices(indexPos + 5, vertexPos + 1);
        alterRaysIndices(indexPos + 6, vertexPos + 1);
        alterRaysIndices(indexPos + 7, raysIndicesCPU[TriIndexPos + 2]);
        alterRaysIndices(indexPos + 8, vertexPos + 2);
        alterRaysIndices(TriIndexPos, vertexPos);
        alterRaysIndices(TriIndexPos + 1, vertexPos + 2);
        alterRaysIndices(TriIndexPos + 2, vertexPos + 1);
    }

//-----------------------------------------------------------//
//                                                           //
//                       Culling Tools                       //
//                                                           //
//-----------------------------------------------------------//

//--------------------- Triangle Culling --------------------//

    // shifts triangles down one, only called if the triangle on the left is nullified
    void cullShift(int TriIndexPos) {
        if(TriIndexPos + 5 < raysIndicesCPU.Length){
            raysIndicesCPU[TriIndexPos] = raysIndicesCPU[TriIndexPos + 3];
            raysIndicesCPU[TriIndexPos + 1] = raysIndicesCPU[TriIndexPos + 4];
            raysIndicesCPU[TriIndexPos + 2] = raysIndicesCPU[TriIndexPos + 5];
            raysIndicesCPU[TriIndexPos + 3] = 0;
            raysIndicesCPU[TriIndexPos + 4] = 0;
            raysIndicesCPU[TriIndexPos + 5] = 0;
        } else {
            cullFR(TriIndexPos);
        }
    }

    // nullifies a triangle, and subtracts one from each referenced vertices' reference count tracker (rcpuvrc)
    void cullFR(int TriIndexPos) {
        rcpuvrc[raysIndicesCPU[TriIndexPos]] -= 1;
        rcpuvrc[raysIndicesCPU[TriIndexPos + 1]] -= 1;
        rcpuvrc[raysIndicesCPU[TriIndexPos + 2]] -= 1;
        raysIndicesCPU[TriIndexPos] = 0;
        raysIndicesCPU[TriIndexPos + 1] = 0;
        raysIndicesCPU[TriIndexPos + 2] = 0;
    }

    // compaction tool for checking if a set of indices are just null data (ie. all are equal to 0)
    bool cullNoTri(int TriIndexPos) {
        return raysIndicesCPU[TriIndexPos] + raysIndicesCPU[TriIndexPos + 1] + raysIndicesCPU[TriIndexPos + 2] == 0;
    }

    // tests if a point is in view
    bool cullPoint(Vector3 pos) {
        var screenPos = Cam.WorldToScreenPoint(pos * 100000);
        var aspect = (float) Cam.pixelWidth / (float) Cam.pixelHeight;
        var onScreenX = screenPos.x / (float) Cam.pixelWidth;
        var onScreenY = screenPos.y / (float) Cam.pixelHeight;
        return onScreenX > -CullCushion * aspect
            && onScreenX < 1 + CullCushion * aspect
            && onScreenY > -CullCushion * (1 / aspect) 
            && onScreenY < 1 + CullCushion * (1 / aspect) 
            && screenPos.z > 0;
    }

    // tests to see if the center of the triangle is in view, then the vertices. returns true if any are in view
    bool cullFRTri(int TriIndexPos) {
        return !(cullPoint((raysCPU[raysIndicesCPU[TriIndexPos]] + raysCPU[raysIndicesCPU[TriIndexPos + 1]] + raysCPU[raysIndicesCPU[TriIndexPos + 2]]) / 3)
            || cullPoint(raysCPU[raysIndicesCPU[TriIndexPos]])
            || cullPoint(raysCPU[raysIndicesCPU[TriIndexPos + 1]])
            || cullPoint(raysCPU[raysIndicesCPU[TriIndexPos + 2]]));
    }

    // Collapses the list of triangles in, such that all of the useless data is at the end, 
    // returns num triangles after the current one
    int cullCollapse(int TriIndexPos) {
        var trisAfter = 0;
        if(TriIndexPos + 5 < triCount * 3){
            trisAfter = cullCollapse(TriIndexPos + 3);
            if(cullNoTri(TriIndexPos)){
                cullShift(TriIndexPos);
                cullCollapse(TriIndexPos + 3);
            }
        }
        if(cullNoTri(TriIndexPos)) {
            return trisAfter;
        } else {
            return 1 + trisAfter;
        }
    }

    // Recursively culls triangles
    void cullCheck(int TriIndexPos) {
        if(TriIndexPos + 2 < triCount * 3){
            if(cullFRTri(TriIndexPos)){
                cullFR(TriIndexPos);
            }
            cullCheck(TriIndexPos + 3);
        }
    }

    // performs one cull pass, returns the last free spot in the index array 
    int cullMaster(int TriIndexPos) {
        cullCheck(TriIndexPos);
        return cullCollapse(TriIndexPos) * 3 + TriIndexPos;
    }



//---------------------- Vertex Culling ---------------------//

    int cullVollapse(int vertIndex) {
        var vertsAfter = 0;
        if(vertIndex < vertCount - 1) {
            vertsAfter = cullVollapse(vertIndex + 1);
            if(rcpuvrc[vertIndex] <= 0) {
                raysCPU[vertIndex] = raysCPU[vertIndex + 1];
                rcpuvrc[vertIndex] = rcpuvrc[vertIndex + 1];
                rcpuvrc[vertIndex + 1] = 0;
                cullVollapse(vertIndex + 1);
            }
        }
        if(rcpuvrc[vertIndex] != 0) {
            return 1 + vertsAfter;
        } else {
            return vertsAfter;
        }
    }

    void cullVerts(int vertIndex, int cullCompensation = 0) {
        if(vertIndex < vertCount) {
            if(rcpuvrc[vertIndex] <= 0) {
                for(int i = 0; i < raysIndicesCPU.Length; i++) {
                    if(raysIndicesCPU[i] > vertIndex - cullCompensation) {
                        raysIndicesCPU[i] -= 1;
                    }
                }
                cullVerts(vertIndex + 1, cullCompensation + 1);
            } else {
                cullVerts(vertIndex + 1, cullCompensation);
            }
        }
    }

    int cullMasterV(int vertIndex) {
        cullVerts(vertIndex);
        return cullVollapse(vertIndex);
    }


//------------------------------------------------------------//
//                                                            //
//                           Gizmos                           //
//                                                            //
//------------------------------------------------------------//

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
        makeDaRays();
        Vector3 offset = this.gameObject.transform.position;
        Gizmos.color = new Color (1f, 1f, 1f, 0.75f);
        float scale = 0.1f;
        for(int i = 0; i < Depth; i++) {
            for(int j = 0; j < raysIndicesCPU.Length / 3; j++) {
                Gizmos.DrawLine(offset + raysCPU[raysIndicesCPU[j * 3]] * scale, offset + raysCPU[raysIndicesCPU[j * 3 + 1]] * scale);
                Gizmos.DrawLine(offset + raysCPU[raysIndicesCPU[j * 3 + 2]] * scale, offset + raysCPU[raysIndicesCPU[j * 3 + 1]] * scale);
                Gizmos.DrawLine(offset + raysCPU[raysIndicesCPU[j * 3 + 2]] * scale, offset + raysCPU[raysIndicesCPU[j * 3]] * scale);
            }
            scale += Magnitude(raysCPU[0] * scale - raysCPU[1] * scale);
        }
        Gizmos.color = new Color(0.75f, 0.75f, 0.0f, 0.75f);
        for(int i = 0; i < raysCPU.Length; i++) {
            Gizmos.DrawRay(offset, offset + raysCPU[i] * Depth * 100);
        }
        Color green = new Color(0f, 1f, 0f, 0.75f);
        Color red = new Color(1f, 0f, 0f, 0.75f);
        scale = 0.1f;
        for(int i = 0; i < Depth; i++) {
            for(int j = 0; j < raysCPU.Length; j++) {
                float size = Sample(raysCPU[j] * scale) / Isolevel;
                if(size >= 1f) { Gizmos.color = green; } 
                else { Gizmos.color = red; }
                size = Mathf.Min(0.2f * scale, 10f);
                //Gizmos.DrawCube(offset + raysCPU[j] * scale, new Vector3(size, size, size));
            }
            scale += Magnitude(raysCPU[0] * scale - raysCPU[1] * scale);
        }
    }
    Vector3[] IcosahedronV() {
        float phi = 1.618033988749894f;
        return new Vector3[] { new Vector3(-1, phi, 0), new Vector3(1, phi, 0), new Vector3(-1, -phi, 0), new Vector3(1, -phi, 0), new Vector3(0, -1, phi), new Vector3(0, 1, phi), new Vector3(0, -1, -phi), new Vector3(0, 1, -phi), new Vector3(phi, 0, -1), new Vector3(phi, 0, 1), new Vector3(-phi, 0, -1), new Vector3(-phi, 0, 1) };
    }
    int[] IcosahedronI() {
        return new int[] { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 1, 5, 9, 0, 10, 11, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
    }
    int[] rcpuvrcIco() {
        return new int[] { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 };
    }
    Vector3[] SquareV() {
        return new Vector3[] { new Vector3(1, 1, 1), new Vector3(-1, 1, 1), new Vector3(1, -1, 1), new Vector3(1, 1, -1), new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(-1, 1, -1), new Vector3(-1, -1, 1) };
    }
    int[] SquareI() {
        return new int[] { 0, 1, 2, 0, 2, 3, 0, 3, 1, 4, 5, 6, 4, 6, 7, 4, 7, 5, 5, 6, 3, 3, 5, 2, 5, 2, 7, 2, 7, 1, 7, 1, 6, 1, 6, 3 };
    }
    int[] rcpuvrcSqr() {
        return new int[] { 4, 4, 4, 4, 4, 4, 4, 4 };
    }
}