using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestartArea : MonoBehaviour
{
    public static event System.Action OnPlayerEnterRestartArea;

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Player")
            OnPlayerEnterRestartArea();
    }
}