using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityTracker : MonoBehaviour
{
    public int averageSize = 7;

    [NonSerialized] 
    public Vector3 velocity;
    public float speed
    {
        get { return velocity.magnitude; }
    }

    private Vector3 lastPos;
    private Transform thisTransform;
    private Queue<Vector3> velocityHistory;

    void Start()
    {
        velocityHistory = new Queue<Vector3>(averageSize);

        thisTransform = transform;
        lastPos = thisTransform.position;
    }

    void Update()
    {
        Vector3 newVelocity = (thisTransform.position - lastPos) / Time.deltaTime;
        velocityHistory.Enqueue(newVelocity);

        if (velocityHistory.Count > averageSize)
        {
            velocityHistory.Dequeue();
        }

        velocity = Vector3.zero;
        foreach (Vector3 v in velocityHistory)
        {
            velocity += v;
        }

        if (velocityHistory.Count > 0)
        {
            velocity /= velocityHistory.Count;
        }

        lastPos = thisTransform.position;
    }
}
