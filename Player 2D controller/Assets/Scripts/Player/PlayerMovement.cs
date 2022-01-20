using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hello there, this player controller script was created as a portfolio to get into game industry.
/// You are free to use this code as you wish. Props to Sebastian Lague and Tarodev as they inspired me to do this project.
/// Version: 1.0
/// Date: 2020
/// Created by: Biebras
/// </summary>

[RequireComponent(typeof(PlayerCollision))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Walking")]
    [SerializeField] private float maxMove = 13;
    [SerializeField] private float acceleration = 90;
    [SerializeField] private float deceleration = 60f;

    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 3;
    [SerializeField] private float timeToJumpApex = .4f;

    [Header("Falling")]
    [SerializeField] private float minFallSpeed = 5;
    [SerializeField] private float maxFallSpeed = 20;

    private Vector2 velocity;
    private float gravity;
    private float jumpSpeed;
    private Vector2 rawMovement;
    private Vector2 lastPosition;
    private Vector2 furthestPoint;
    private float horizontalSpeed;
    private float verticalSpeed;
    private bool canJump;

    private Transform _transform;
    private PlayerCollision playerCollision;
    private PlayerInput playerInput;

    private void Awake()
    {
        _transform = transform;
        playerCollision = GetComponent<PlayerCollision>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        lastPosition = _transform.position;

        SetGravity();
        SetJumpSpeed();

        playerInput.onJumpPressed += JumpPressed;
    }

    private void SetGravity()
    {
        gravity = 2 * jumpHeight / Mathf.Pow(timeToJumpApex, 2);
    }

    private void SetJumpSpeed()
    {
        jumpSpeed = gravity * timeToJumpApex;
    }

    private void Update()
    {
        CalculateVelocity();

        CalculateGravity();

        Walk();

        Jump();

        ClampSpeedY();

        Move(velocity);
    }

    public Vector2 GetVelocity()
    {
        return velocity;
    }

    public Vector2 GetFurthestPoint()
    {
        return furthestPoint;
    }

    private void CalculateVelocity()
    {
        velocity = ((Vector2)_transform.position - lastPosition) / Time.deltaTime;
        lastPosition = _transform.position;
    }

    private void CalculateGravity()
    {
        if(!playerCollision.downCollision.colliding)
            verticalSpeed -= gravity * Time.deltaTime;
    }

    private void Walk()
    {
        var input = playerInput.GetHorizontalInput();

        if (input != 0)
        {
            horizontalSpeed += input * acceleration * Time.deltaTime;
            horizontalSpeed = Mathf.Clamp(horizontalSpeed, -maxMove, maxMove);
        }
        else
        {
            horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, 0, deceleration * Time.deltaTime);
        }
    }

    private void JumpPressed()
    {
        if(playerCollision.downCollision.rayHit)
            canJump = true;
    }

    private void Jump()
    {
        if (!canJump || !playerCollision.downCollision.colliding)
            return;

        verticalSpeed = jumpSpeed;
        canJump = false;
    }

    private void ClampSpeedY()
    {
        if(verticalSpeed < 0)
            verticalSpeed = Mathf.Clamp(verticalSpeed, -maxFallSpeed, -minFallSpeed);
    }

    private void Move(Vector3 velocity)
    {
        var pos = _transform.position;
        rawMovement = new Vector2(horizontalSpeed, verticalSpeed);
        var move = rawMovement * Time.deltaTime;
        furthestPoint = (Vector2)pos + move;

        playerCollision.HandleCollisions(furthestPoint, ref move, rawMovement);
        
        if (playerCollision.IsVerticalliColliding())
        {
            verticalSpeed = 0;
        }

        _transform.position += (Vector3)move;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(furthestPoint, Vector2.one);
    }

    private void OnDestroy()
    {
        playerInput.onJumpPressed -= JumpPressed;
    }
}
