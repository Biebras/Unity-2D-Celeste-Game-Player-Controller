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
    [SerializeField] private float acceleration = 180;
    [SerializeField] private float deceleration = 90;

    [Header("Jumping")]
    [SerializeField] private float maxJumpHeight = 5;
    [SerializeField] private float minJumpHeight = 1.7f;
    [SerializeField] private float timeToJumpApex = .3f;
    [SerializeField] private float jumpBuffer = 0.2f;
    [SerializeField] private float coyoteJump = 0.09f;

    [Header("Wall")]
    [SerializeField] private float wallSlide = 6;
    [SerializeField] private float wallClimb = 3f;
    [SerializeField] private float wallStickTime = 0.2f;

    [Header("Wall Grabed")]
    [SerializeField] private float wallGrabTime = 5;
    [SerializeField] private float grabDistance = 0.2f;
    [SerializeField] private float wallGrabJumpApexTime = 0.15f;
    [SerializeField] private Vector2 topEdgeClimbJump = new Vector2(8, 12);
    [SerializeField] private Vector2 wallJump = new Vector2(12, 30);

    [Header("Falling")]
    [SerializeField] private float minFallSpeed = 0;
    [SerializeField] private float maxFallSpeed = 40;

    private Vector2 velocity;
    private float gravity;
    private float maxJumpSpeed;
    private float minJumpSpeed;
    private float wallGrabJumpSpeed;
    private float wallGrabJumpTimer;
    private Vector2 rawMovement;
    private Vector2 lastPosition;
    private Vector2 furthestPoint;
    private float horizontalSpeed;
    private float verticalSpeed;
    private float jumpBufferTimer;
    private float coyoteJumpTimer;
    private float wallStickTimer;
    private float wallGrabTimer;
    private bool canWallJump;
    private bool isWallJumping;

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

        playerInput.onJumpPressed += OnJumpPressed;
        playerInput.onJumpReleased += OnJumpReleased;
    }

    #region Start Functions

    private void SetGravity()
    {
        gravity = 2 * maxJumpHeight / Mathf.Pow(timeToJumpApex, 2);
    }

    private void SetJumpSpeed()
    {
        maxJumpSpeed = gravity * timeToJumpApex;
        minJumpSpeed = Mathf.Sqrt(2 * gravity * minJumpHeight);
        wallGrabJumpSpeed = gravity * wallGrabJumpApexTime;
    }

    #endregion

    private void Update()
    {
        CalculateVelocity();

        CalculateGravity();

        if(!isWallJumping)
            Walk();

        Jump();

        HandleWallMovement();

        ClampSpeedY();

        Move();
    }

    public Vector2 GetVelocity()
    {
        return velocity;
    }

    public Vector2 GetFurthestPoint()
    {
        return furthestPoint;
    }

    #region Gravity and Velocity

    private void CalculateVelocity()
    {
        velocity = ((Vector2)_transform.position - lastPosition) / Time.deltaTime;
        lastPosition = _transform.position;
    }

    private void CalculateGravity()
    {
        if(!playerCollision.downCollision.colliding && !CanCoyoteJump())
            verticalSpeed -= gravity * Time.deltaTime;
    }

    #endregion

    private void Walk()
    {
        var input = playerInput.GetHorizontalInput();

        if (input != 0)
            horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, maxMove * input, acceleration * Time.deltaTime);
        else
            horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, 0, deceleration * Time.deltaTime);
    }

    #region Jump

    private void CoyoteJump()
    {
        if (playerCollision.downCollision.colliding)
        {
            coyoteJumpTimer = coyoteJump;
            return;
        }

        coyoteJumpTimer -= Time.deltaTime;
    }

    private void JumpBuffer()
    {
        if (verticalSpeed > 0)
            jumpBufferTimer = 0;

        jumpBufferTimer -= Time.deltaTime;
    }

    private bool CanJump()
    {
        return jumpBufferTimer > 0;
    }

    private bool CanCoyoteJump()
    {
        return coyoteJumpTimer > 0;
    }

    private void OnJumpPressed()
    {
        jumpBufferTimer = jumpBuffer;
        wallGrabJumpTimer = wallGrabJumpApexTime;
        canWallJump = true;
    }

    private void OnJumpReleased()
    {
        if(verticalSpeed > minJumpSpeed)
            verticalSpeed = minJumpSpeed;
    }

    private void Jump()
    {
        CoyoteJump();
        JumpBuffer();

        if (CanJump() && playerCollision.downCollision.colliding)
            verticalSpeed = maxJumpSpeed;

        if (CanJump() && CanCoyoteJump())
        {
            verticalSpeed = maxJumpSpeed;
            coyoteJumpTimer = 0;
        }
    }

    #endregion

    #region Wall

    private bool IsOnWall()
    {
        return !playerCollision.downCollision.colliding && playerCollision.IsHorizontallyColliding();
    }

    private bool CanGrab()
    {
        var right = playerCollision.rightCollision;
        var left = playerCollision.leftCollision;

        var rightDistance = right.distance < grabDistance && right.rayHit;
        var leftDistance = left.distance < grabDistance && left.rayHit;
        var grabPressed = playerInput.IsGrabPressed();

        return (rightDistance || leftDistance) && grabPressed;
    }

    private void HandleWallMovement()
    {
        var inputY = playerInput.GetVerticalInput();
        var inputX = playerInput.GetHorizontalInput();
        var collision = playerCollision.GetClosestHorizontal();

        WallSlide();
        WallJump(collision);
        WallGrab(collision, inputY);
        WallGrabJump(collision, inputX);

        if (verticalSpeed <= 0)
            isWallJumping = false;
    }

    private void WallSlide()
    {
        wallStickTimer -= Time.deltaTime;

        if (!IsOnWall() && wallStickTimer <= 0)
            return;

        isWallJumping = true;    

        if (verticalSpeed < 0)
            verticalSpeed = -wallSlide;
    }

    private void WallJump(CollisionInfo collision)
    {
        if(IsOnWall() && !playerInput.IsGrabPressed() && verticalSpeed < 0)
        {
            if(playerInput.IsJumpPressed() && canWallJump)
            {
                horizontalSpeed = wallJump.x * -collision.raycastInfo.rayDirection.x;
                verticalSpeed = wallJump.y;
                wallStickTimer = wallStickTime;
                canWallJump = false;
            }
        }
    }

    private void WallGrabJump(CollisionInfo collision, float input)
    {
        wallGrabJumpTimer -= Time.deltaTime;

        if (CanGrab() && collision != null)
        {
            if(playerInput.IsJumpPressed() && input != 0)
            {
                if (collision.raycastInfo.rayDirection.x != input)
                {
                    horizontalSpeed = wallJump.x * input;
                    verticalSpeed = wallJump.y;
                    return;
                }
            }
            
            if (playerInput.IsJumpPressed() && wallGrabJumpTimer > 0)
            {
                verticalSpeed = wallGrabJumpSpeed;
            }
        }
    }

    private void WallGrab(CollisionInfo collision, float input)
    {
        wallGrabTimer -= Time.deltaTime;

        if (playerCollision.downCollision.colliding)
            wallGrabTimer = wallGrabTime;

        if (CanGrab() && collision != null && wallGrabTimer > 0)
        {
            if(collision.firstHit && collision.hitCount == 1)
            {
                verticalSpeed = topEdgeClimbJump.y;
                horizontalSpeed = topEdgeClimbJump.x * collision.raycastInfo.rayDirection.x;
                return;
            }

            verticalSpeed = input * wallClimb;
            horizontalSpeed = 0;
        }    
    }

    #endregion

    private void ClampSpeedY()
    {
        if(verticalSpeed < 0)
            verticalSpeed = Mathf.Clamp(verticalSpeed, -maxFallSpeed, -minFallSpeed);
    }

    private void Move()
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
        playerInput.onJumpPressed -= OnJumpPressed;
        playerInput.onJumpReleased -= OnJumpReleased;
    }
}
