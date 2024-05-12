using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance { get; private set; }

    public List<EffectData> SpawnData;

    bool isInitialized = false, hasUpdated = false;
    public float maxDistance = 4, Speed = 1.5f;
    public int particleAmount = 50;
    public NativeArray<float3> Vertices;
    public NativeArray<float3> Normals;
    public NativeArray<float3> SpawnPoints;
    public VisualEffect vfx;
    public AudioClip[] clips;
    GraphicsBuffer buffer;
    float previousRadius = 0;
    int spawnID, bufferID, particleCountID, particleCount;
    MeshFilter MeshFilter;

    void Awake()
    {
        Instance = this;

        spawnID = Shader.PropertyToID("OnSpawnParticle");
        bufferID = Shader.PropertyToID("SpawnPositions");
        particleCountID = Shader.PropertyToID("Count");
    }

    public async void Initialize()
    {
        await Task.Delay(200);

        var objects = FindObjectsOfType<OVRSceneVolumeMeshFilter>();

        NativeList<float3> verts = new NativeList<float3>(Allocator.TempJob);
        NativeList<float3> norms = new NativeList<float3>(Allocator.TempJob);

        foreach (var obj in objects)
        {
            if (obj.CompareTag("Environment"))
            {
                var mf = obj.GetComponent<MeshFilter>();
                if (!mf) mf = obj.GetComponentInChildren<MeshFilter>();
                if (!mf) continue;
                MeshFilter = mf;

                var trans = mf.transform;

                await Task.Delay(1);
                for (int i = 0; i < mf.mesh.vertices.Length; i++)
                {
                    float3 vertex = mf.mesh.vertices[i];
                    float3 normal = mf.mesh.normals[i]; // Add this for normals

                    vertex *= (float3)trans.localScale;
                    vertex = math.rotate(trans.rotation, vertex);
                    vertex += (float3)trans.position;

                    normal = math.rotate(trans.rotation, normal);
                    normal = math.normalize(normal); // Add this for normals

                    verts.Add(vertex);
                    norms.Add(normal); // Add this for normals
                }
            }
        }
        await Task.Delay(1);
        Vertices = new NativeArray<float3>(verts.Length, Allocator.Persistent);
        Normals = new NativeArray<float3>(verts.Length, Allocator.Persistent);

        SpawnPoints = new NativeArray<float3>(particleAmount, Allocator.Persistent);

        NativeArray<float3>.Copy(verts.AsArray(), Vertices);
        NativeArray<float3>.Copy(norms.AsArray(), Normals); // Add this for normals

        verts.Dispose();
        norms.Dispose();
        if (Vertices.Length > 0) isInitialized = true;
    }

    public void AddObject(Vector3 origin, float speed)
    {
        if (!isInitialized) return;

        SpawnData.Add(new EffectData
        {
            Origin = origin,
            StartTime = Time.time,
            Speed = speed,
            Distance = maxDistance,
            source = new GameObject("AudioSource").AddComponent<AudioSource>()
        });

        SpawnData[SpawnData.Count - 1].source.clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        SpawnData[SpawnData.Count - 1].source.loop = false;
        SpawnData[SpawnData.Count - 1].source.dopplerLevel = 0;
        SpawnData[SpawnData.Count - 1].source.spatialBlend = 1;
        SpawnData[SpawnData.Count - 1].source.Play();
    }

    private void OnApplicationQuit()
    {
        if(SpawnPoints.IsCreated) SpawnPoints.Dispose();
        if(Vertices.IsCreated) Vertices.Dispose();
    }

    void Update()
    {
        if(!isInitialized || SpawnData.Count == 0)
        {
            return;
        }

        for (int i = 0; i < SpawnData.Count; i++)
        {
            var data = SpawnData[i];

            float radius = ((Time.time - data.StartTime) * data.Speed) % data.Distance;

            if (radius < data.CurrentDistance)
            {
                // Play the audio source
                if(!data.source.isPlaying) data.source.Play();
            }

            data.CurrentDistance = radius;

            data.Job = new EffectsJobParralel
            {
                Vertices = Vertices,
                Normals = this.Normals,
                CenterPosition = data.Origin,
                Radius = radius,
                SpawnPositions = new NativeArray<float3>(particleAmount, Allocator.TempJob),
                SpawnDirection = new NativeArray<float3>(particleAmount, Allocator.TempJob)
            };

            data.Handle = data.Job.Schedule(particleAmount, particleAmount/2);
        }

        JobHandle.ScheduleBatchedJobs();
        hasUpdated = true;
    }

    private void LateUpdate()
    {
        if (!isInitialized || !hasUpdated || SpawnData.Count == 0)
        {
            return;
        }

        particleCount = particleAmount * SpawnData.Count;

        vfx.SetInt(particleCountID, particleCount);

        SpawnPoints = new NativeArray<float3>(particleCount * 2, Allocator.Temp);
        buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount * 2, 12);

        int positionIndex = 0;
        int directionIndex = particleCount;

        for (int i = 0; i < SpawnData.Count; i++)
        {
            SpawnData[i].Handle.Complete();
            NativeArray<float3>.Copy(SpawnData[i].Job.SpawnPositions, 0, SpawnPoints, positionIndex, particleAmount);
            NativeArray<float3>.Copy(SpawnData[i].Job.SpawnDirection, 0, SpawnPoints, particleCount, particleAmount);
            SpawnData[i].Job.SpawnPositions.Dispose();
            SpawnData[i].Job.SpawnDirection.Dispose();

            positionIndex += particleAmount;
            directionIndex += particleAmount;
        }

        buffer.SetData(SpawnPoints);

        vfx.SetGraphicsBuffer(bufferID, buffer);
        vfx.SendEvent(spawnID);

        SpawnPoints.Dispose();

        hasUpdated = false;
    }
}