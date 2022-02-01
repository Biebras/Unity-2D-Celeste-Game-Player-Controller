using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform checkpoint;

    private Transform _transform;
    private PlayerCollision playerCollision;

    private void Awake()
    {
        _transform = transform;
        playerCollision = GetComponent<PlayerCollision>();
    }

    private void Start()
    {
        _transform.position = checkpoint.position;

        playerCollision.onPlayerTriggerInteractables += OnPlayerTriggerInteractables;   
    }

    private void OnPlayerTriggerInteractables(GameObject hit)
    {
        if(hit.CompareTag("Spykes"))
            _transform.position = checkpoint.position;
    }

    private void OnDestroy()
    {
        playerCollision.onPlayerTriggerInteractables -= OnPlayerTriggerInteractables;
    }
}
