using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public class EffectData
{
    public float3 Origin;
    public float Speed, Distance, StartTime;
    public EffectsJobParralel Job;
    public JobHandle Handle;
    public AudioSource source;
}

[BurstCompile]
public struct EffectsJobs : IJob
{
    [ReadOnly] public NativeArray<float3> Vertices;
    [ReadOnly] public float3 CenterPosition;
    [ReadOnly] public float Radius;

    [WriteOnly] public NativeArray<float3> SpawnPositions;

    public void Execute()
    {
        int numPositions = 50;
        float angleStep = 2 * Mathf.PI / numPositions;

        for (int i = 0; i < numPositions; i++)
        {
            float angle = i * angleStep;
            float3 position = CenterPosition + new float3(math.cos(angle), 0, math.sin(angle)) * Radius;

            float minDistanceSqr = float.MaxValue;
            float nearestVertexHeight = 0;

            for (int j = 0; j < Vertices.Length; j++)
            {
                float3 vertex = Vertices[j];

                float distanceSqr = math.distancesq(position.xz, vertex.xz);

                bool picker = distanceSqr < minDistanceSqr && vertex.y < 1.7f;
                minDistanceSqr = math.select(minDistanceSqr, distanceSqr, picker);
                nearestVertexHeight = math.select(nearestVertexHeight, vertex.y, picker);
            }

            SpawnPositions[i] = new float3(position.x, nearestVertexHeight, position.z);
        }
    }
}

[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
public struct EffectsJobParralel : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float3> Vertices, Normals;
    public float3 CenterPosition;
    public float Radius;

    [WriteOnly]
    public NativeArray<float3> SpawnPositions, SpawnDirection;

    public void Execute(int index)
    {
        var val = index * 2 * math.PI / 50;
        float3 position = CenterPosition + new float3(math.cos(val), 0, math.sin(val)) * Radius;

        float minDistanceSqr = float.MaxValue;
        float nearestVertexHeight = 0;
        float3 nearestVertexNormal = float3.zero; // Add this for nearest vertex normal
        float len = Vertices.Length;

        for (int j = 0; j < len; j++)
        {
            float3 vertex = Vertices[j];
            float3 normal = Normals[j]; // Add this for vertex normal

            float distanceSqr = math.distancesq(position.xz, vertex.xz);
            float vertY = vertex.y;
            bool picker = distanceSqr < minDistanceSqr && vertY < 1.7f;
            minDistanceSqr = math.select(minDistanceSqr, distanceSqr, picker);
            nearestVertexHeight = math.select(nearestVertexHeight, vertY, picker);
            nearestVertexNormal = math.select(nearestVertexNormal, normal, picker); // Add this for nearest vertex normal
        }

        SpawnPositions[index] = new float3(position.x, nearestVertexHeight, position.z);
        SpawnDirection[index] = nearestVertexNormal; // Add this for output normal
    }
}