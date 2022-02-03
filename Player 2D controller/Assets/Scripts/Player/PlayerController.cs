using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform _checkpoint;

    private Transform _transform;
    private PlayerCollision _playerCollision;

    private void Awake()
    {
        _transform = transform;
        _playerCollision = GetComponent<PlayerCollision>();
    }

    private void Start()
    {
        _transform.position = _checkpoint.position;

        _playerCollision.OnPlayerTriggerInteractables += OnPlayerTriggerInteractables;   
    }

    private void OnPlayerTriggerInteractables(GameObject hit)
    {
        if(hit.CompareTag("Spykes"))
            _transform.position = _checkpoint.position;
    }

    private void OnDestroy()
    {
        _playerCollision.OnPlayerTriggerInteractables -= OnPlayerTriggerInteractables;
    }
}
