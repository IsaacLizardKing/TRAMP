using UnityEngine;

public class ScreenMeshManager : MonoBehaviour
{
    // Runtime Settings

    [SerializeField] int ScreenMeshesWide = 3;
    [SerializeField] int ScreenMeshesTall = 3;
    (int x, int y) ScreenMeshes = (x: 3, y: 3);
    (int x, int y) lstScreenMeshes = (x: 3, y: 3);

    [Range(-10f, 10f)]
    public float Isolevel;

    [Range(0.0000000000000001f, 1)]
    public float lerpSpeed;

    [Range(0, 0.785398163f)]
    public float stepAngle;


    [SerializeField] bool interpolate;
    [SerializeField] bool randomVertexColoring;
    [SerializeField] Material material;
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Camera Cam;
    [SerializeField] Transform playerMovement;
    [SerializeField] GameObject ScreenMesh;

    //Setup Settings
    [Range(1, 100)]
    public int Depth = 10;
    int lstDepth = 10;

    [Range(0, 100)]
    public int Subdivisions = 20;
    int lstSubvdivisions = 20;
    

    GameObject[] Meshes; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        ScreenMeshes = (x: ScreenMeshesWide, y: ScreenMeshesTall);
        Generate();
        lstScreenMeshes = ScreenMeshes;
        lstDepth = Depth;
        lstSubvdivisions = Subdivisions;
    }

    void Generate() {
        if(!(Meshes is null)) { 
            foreach(GameObject a in Meshes) {
                Destroy(a);
            }
        }
        int numMeshes = ScreenMeshes.x * ScreenMeshes.y;
        Meshes = new GameObject[numMeshes];
        for (int i = 0; i < numMeshes; i++) {
            Meshes[i] = Instantiate(ScreenMesh, playerMovement.transform.position, Quaternion.identity, Cam.transform);
            Meshes[i].name = $"{i}";
            ExperimentComputeMesh computeMesh = Meshes[i].GetComponent<ExperimentComputeMesh>();
            SetParams(computeMesh);
            SetBounds(i % ScreenMeshes.x, (int)(i / ScreenMeshes.x), computeMesh);
            computeMesh.enabled = true;
            Meshes[i].SetActive(true);
        }
    }

    void SetParams(ExperimentComputeMesh mesh) {
        mesh.computeShader = computeShader;
        mesh.material = material;
        mesh.Cam = Cam;
        mesh.playerMovement = playerMovement;
        mesh.Depth = Depth;
        mesh.Subdivisions = Subdivisions;
        mesh.Isolevel = Isolevel;
        mesh.lerpSpeed = lerpSpeed;
        mesh.stepAngle = stepAngle;
    }

    void SetBounds(int x, int y, ExperimentComputeMesh mesh) {
        mesh.screenMeshRight = (float) x / ScreenMeshes.x;
        mesh.screenMeshLeft = (float) (x + 1) / ScreenMeshes.x;
        mesh.screenMeshBottom = (float) y / ScreenMeshes.y;
        mesh.screenMeshTop = (float) (y + 1) / ScreenMeshes.y;
    }

    // Update is called once per frame
    void Update() {
        ScreenMeshes = (x: ScreenMeshesWide, y: ScreenMeshesTall);
        if(ScreenMeshes != lstScreenMeshes || Depth != lstDepth || Subdivisions != lstSubvdivisions) {
            Generate();
            lstScreenMeshes = ScreenMeshes;
            lstDepth = Depth;
            lstSubvdivisions = Subdivisions;
        }

    }
}
