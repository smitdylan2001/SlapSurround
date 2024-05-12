using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    OVRSceneManager sceneManager;

    void Awake()
    {
        sceneManager = FindAnyObjectByType<OVRSceneManager>();

        sceneManager.SceneModelLoadedSuccessfully += OnSceneModelLoadedSuccessfully;
    }

    private void OnApplicationQuit()
    {
        sceneManager.SceneModelLoadedSuccessfully -= OnSceneModelLoadedSuccessfully;
    }

    private void OnSceneModelLoadedSuccessfully()
    {
        Debug.Log("Scene Model Loaded Successfully", gameObject);
        EffectsManager.Instance.Initialize();
    }
}
