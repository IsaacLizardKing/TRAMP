using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeMeshManager : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 color;
        public Vector2 uv0;
    }

    [SerializeField] ExperimentComputeMesh[] meshes;



    void Start()
    {
        // Read:
        // https://gpuopen.com/learn/optimizing-gpu-occupancy-resource-usage-large-thread-groups/
    }

    void OnDestroy()
    {
        
    }

    private void Update()
    {
        
    }
}

/*
Audre Lorde (1984, 45):
"It is axiomatic that if we do not define ourselves for ourselves, we will be defined by others - for their use and to our detriment"

Patricia Hill Collins (1990, 17):
"By Identifying my position as a participant in and observer of my own community, 
I run the risk of being discredited as being too subjective and hence less scholarly.
But by being and advocate for my material , I valideate the epistemological stance that
I claim is fundamental for Black feminist thought."

Alice Walker (1983, 264):
"The gift of loneliness is sometimes a radical vision of society or one's people that has
not previously been taken into account"

Alice Walker (1983, 13):
"In my own work I write not only what I want to read - Understanding fully and indelibly
that if I don't do it no one else is so vitally interested, or capable of doing it to my
satisfaction - I write all the things I should have been able to read"

Note to self:
Write all the things you should have been able to read. Read what there is to read about
queerness, about neurodivergence, and write about us.
*/