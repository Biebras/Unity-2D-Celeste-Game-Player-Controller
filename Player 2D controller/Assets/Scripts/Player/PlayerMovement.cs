using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hello there, this player controller script was created as a portfolio to get into game industry.
/// You are free to use this code as you wish. Props to Sebastian Lague and Tarodev as they inspired me to do this project.
/// Version: 1.1
/// Date: 2020
/// Created by: Biebras
/// </summary>

[RequireComponent(typeof(PlayerCollision))]
public class PlayerMovement : MonoBehaviour
{
    public event Action onDash;

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

    [Header("Dash")]
    [SerializeField] private float dashDistance = 3f;
    [SerializeField] private float dashDuration = 0.1f;
    [SerializeField] private float ySpeedAdterDash = 10;

    [Header("Falling")]
    [SerializeField] private float minFallSpeed = 0;
    [SerializeField] private float maxFallSpeed = 40;

    public Vector2 velocity;
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
    private float externalHorizontalSpeed;
    private float externalVerticalSpeed;
    private float jumpBufferTimeLeft;
    private float coyoteJumpTimeLeft;
    private float wallStickTimeLeft;
    private float wallGrabTimeLeft;
    private float dashTimer;
    private bool canWallJump;
    private bool isWallJumpInProgress;
    private bool dashJustEnded;
    private bool canDash = false;
    private Dictionary<GameObject, PlatformController> platforms = new Dictionary<GameObject, PlatformController>();

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
        playerInput.onDashPressed += PlayerInput_onDashPressed;
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

        if(!isWallJumpInProgress)
            Walk();

        Jump();

        HandleWallMovement();

        Dash();

        HandleExternalForces();

        ClampSpeedY();

        Move();
    }

    public Vector2 GetVelocity()
    {
        return velocity;
    }

    public Vector2 GetRawVelocity()
    {
        return rawMovement;
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
        if (!playerCollision.downCollision.colliding && !CanCoyoteJump())
            verticalSpeed -= gravity * Time.deltaTime;

        if (playerCollision.IsVerticalliColliding())
            verticalSpeed = 0;
    }

    #endregion

    #region Walk

    private void Walk()
    {
        var input = playerInput.GetHorizontalInput();

        if (input != 0)
            horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, maxMove * input, acceleration * Time.deltaTime);
        else
            horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, 0, deceleration * Time.deltaTime);
    }

    #endregion

    #region Jump

    private void OnJumpPressed()
    {
        jumpBufferTimeLeft = jumpBuffer;
        wallGrabJumpTimer = wallGrabJumpApexTime;

        if(IsOnWall())
            canWallJump = true;
    }

    private void OnJumpReleased()
    {
        if(verticalSpeed > minJumpSpeed)
            verticalSpeed = minJumpSpeed;
    }

    private void CoyoteJump()
    {
        if (playerCollision.downCollision.colliding)
        {
            coyoteJumpTimeLeft = coyoteJump;
            return;
        }

        coyoteJumpTimeLeft -= Time.deltaTime;
    }

    private void JumpBuffer()
    {
        if (verticalSpeed > 0)
            jumpBufferTimeLeft = 0;

        jumpBufferTimeLeft -= Time.deltaTime;
    }

    private bool CanJump()
    {
        return jumpBufferTimeLeft > 0;
    }

    private bool CanCoyoteJump()
    {
        return coyoteJumpTimeLeft > 0;
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
            coyoteJumpTimeLeft = 0;
        }
    }

    #endregion

    #region Wall

    public bool IsOnWall()
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
            isWallJumpInProgress = false;
    }

    private void WallSlide()
    {
        wallStickTimeLeft -= Time.deltaTime;

        if (!IsOnWall() && wallStickTimeLeft <= 0)
            return;

        isWallJumpInProgress = true;    

        if (verticalSpeed < 0)
            verticalSpeed = -wallSlide;
    }

    private void WallJump(CollisionInfo collision)
    {
        if(IsOnWall() && !playerInput.IsGrabPressed() && verticalSpeed < 0)
        {
            if(playerInput.IsJumpPressed() && canWallJump && collision.lastHit)
            {
                horizontalSpeed = wallJump.x * -collision.raycastInfo.rayDirection.x;
                verticalSpeed = wallJump.y;
                wallStickTimeLeft = wallStickTime;
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
        wallGrabTimeLeft -= Time.deltaTime;

        if (playerCollision.downCollision.colliding)
            wallGrabTimeLeft = wallGrabTime;

        if (CanGrab() && collision != null && wallGrabTimeLeft > 0)
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

    #region Dash

    private void PlayerInput_onDashPressed()
    {
        if(canDash)
        {
            dashTimer = dashDuration;
            onDash?.Invoke();
        }
    }

    private void Dash()
    {
        var inputX = playerInput.GetHorizontalInput();
        var inputY = playerInput.GetVerticalInput();

        if (inputX == 0 && inputY == 0)
            inputX = 1;

        Vector2 dir = new Vector2(inputX, inputY).normalized;

        if (playerCollision.downCollision.colliding)
            canDash = true;

        dashTimer -= Time.deltaTime;

        if (dashTimer <= 0)
        {
            if(!dashJustEnded)
            {
                verticalSpeed = ySpeedAdterDash * inputY;
                dashJustEnded = true;
            }

            return;
        }

        var furthestPoint = GetDashFurthestPoint(dir);
        var hit = playerCollision.GetDashHitPos(dir, furthestPoint);
        var distnace = Vector2.Distance(_transform.position, hit);
        var clampDistance = Mathf.Clamp(distnace, 0, dashDistance);
        var velocity = clampDistance / dashDuration * dir;
        horizontalSpeed = velocity.x;
        verticalSpeed = velocity.y;
        canDash = false;
        dashJustEnded = false;
    }

    private Vector2 GetDashFurthestPoint(Vector2 dir)
    {
        return (Vector2)_transform.position + dir * dashDistance;
    }

    #endregion

    #region External forces

    private void HandleExternalForces()
    {
        Platforms();
    }

    private void Platforms()
    {
        GameObject obj;
        PlatformController platform;
        var coll = playerCollision.OverlapPlatform(out obj);
        externalHorizontalSpeed = 0;
        externalVerticalSpeed = 0;

        if (!coll)
            return;

        if(!platforms.ContainsKey(obj))
        {
            platform = obj.GetComponent<PlatformController>();
            platforms.Add(obj, platform);
        }

        platform = platforms[obj];

        if (!playerCollision.downCollision.colliding && verticalSpeed == 0 && playerCollision.platformDownCollision.rayHit)
            playerCollision.ForceVerticalReposition(playerCollision.downCollision);

        var rawVelocity = platform.GetRawVelocity();
        externalHorizontalSpeed = rawVelocity.x;
        externalVerticalSpeed = rawMovement.y;
        return;
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
        var totalMovement = new Vector2(rawMovement.x + externalHorizontalSpeed, rawMovement.y + externalVerticalSpeed);
        var move = totalMovement * Time.deltaTime;
        furthestPoint = (Vector2)pos + move;

        playerCollision.HandleCollisions(furthestPoint, ref move, totalMovement);

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
