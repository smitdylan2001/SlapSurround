using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlapDetector : MonoBehaviour
{
    public float minVelocity = 1.0f, speedMultiplier = 1.3f;

    static float time;

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.CompareTag("Hands") && time + 2 < Time.time)
        {
            var vel = collision.collider.GetComponent<VelocityTracker>().speed * speedMultiplier;

            Debug.Log("Slap Detected with velocity: " + vel);
            if(vel > minVelocity)
            {
                EffectsManager.Instance.AddObject(collision.contacts[0].point, vel);
                time = Time.time;
            }
        }
    }
}