using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.VFX;

public class RaycastEffectManager : MonoBehaviour
{
    public static RaycastEffectManager Instance { get; private set; }

    public List<EffectData> SpawnData;

    bool isInitialized = false;
    public float maxDistance = 4, Speed = 1.5f;
    public int particleAmount = 50;
    public NativeArray<Vector3> SpawnPoints;

    public VisualEffect vfx;
    GraphicsBuffer buffer;
    int spawnID, bufferID, particleCountID, particleCount;

    void Awake()
    {
        Instance = this;

        spawnID = Shader.PropertyToID("OnSpawnParticle");
        bufferID = Shader.PropertyToID("SpawnPositions");
        particleCountID = Shader.PropertyToID("Count");
    }

    private void Update()
    {
        var count = SpawnData.Count;

        // Perform a single raycast using RaycastCommand and wait for it to complete
        // Setup the command and result buffers
        NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(particleAmount * count, Allocator.TempJob);
        var commands = new NativeArray<RaycastCommand>(particleAmount * count, Allocator.TempJob);
        SpawnPoints = new NativeArray<Vector3>(particleAmount * count, Allocator.Temp);

        for (int i = 0; i < count; i++)
        {
            // Set the data of the first command
            Vector3 origin = (Vector3)SpawnData[i].Origin + new Vector3(0, 0.1f, 0);

            for (int j = 0; j < particleAmount; j++)
            {
                Vector3 direction = Vector3.forward;
                commands[(i * particleAmount) + j] = new RaycastCommand(origin, direction, QueryParameters.Default);
            }
        }

        // Schedule the batch of raycasts.
        JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 1, 1);

        // Wait for the batch processing job to complete
        handle.Complete();

        // Copy the result. If batchedHit.collider is null there was no hit
        for(int i = 0; i < results.Length; i++)
        {
            RaycastHit result = results[i];

            if (result.collider != null)
            {
                SpawnPoints[i] = result.point;
            }
        }

        // Dispose the buffers
        results.Dispose();
        commands.Dispose();
    }

    private void LateUpdate()
    {
        particleCount = particleAmount * SpawnData.Count;

        vfx.SetInt(particleCountID, particleCount);

        SpawnPoints = new NativeArray<Vector3>(particleCount, Allocator.Temp);
        buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount, 12);

        for (int i = 0; i < SpawnData.Count; i++)
        {
            SpawnData[i].Handle.Complete();
            NativeArray<Vector3>.Copy(SpawnPoints, 0, SpawnPoints, i * particleAmount, particleAmount);
            SpawnData[i].Job.SpawnPositions.Dispose();
        }

        buffer.SetData(SpawnPoints);

        vfx.SetGraphicsBuffer(bufferID, buffer);
        vfx.SendEvent(spawnID);

        SpawnPoints.Dispose();
    }
}
