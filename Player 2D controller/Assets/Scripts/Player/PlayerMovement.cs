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
    public event Action OnDash;
    public event Action OnJump;

    [Header("Walking")]
    [SerializeField] private float _maxMove = 13;
    [SerializeField] private float _acceleration = 180;
    [SerializeField] private float _deceleration = 90;

    [Header("Jumping")]
    [SerializeField] private float _maxJumpHeight = 5;
    [SerializeField] private float _minJumpHeight = 1.7f;
    [SerializeField] private float _timeToJumpApex = .3f;
    [SerializeField] private float _jumpBuffer = 0.2f;
    [SerializeField] private float _coyoteJump = 0.09f;

    [Header("Wall")]
    [SerializeField] private float _wallSlide = 6;
    [SerializeField] private float _wallClimb = 3f;
    [SerializeField] private float _wallStickTime = 0.2f;

    [Header("Wall Grabed")]
    [SerializeField] private float _wallGrabTime = 5;
    [SerializeField] private float _grabDistance = 0.2f;
    [SerializeField] private float _wallGrabJumpApexTime = 0.15f;
    [SerializeField] private Vector2 _topEdgeClimbJump = new Vector2(8, 12);
    [SerializeField] private Vector2 _wallJump = new Vector2(12, 30);

    [Header("Dash")]
    [SerializeField] private float _dashDistance = 3f;
    [SerializeField] private float _dashDuration = 0.1f;
    [SerializeField] private float _ySpeedAdterDash = 10;

    [Header("Falling")]
    [SerializeField] private float _minFallSpeed = 0;
    [SerializeField] private float _maxFallSpeed = 40;

    private Vector2 _velocity;
    private float _gravity;
    private float _maxJumpSpeed;
    private float _minJumpSpeed;
    private float _wallGrabJumpSpeed;
    private float _wallGrabJumpTimer;
    private Vector2 _rawMovement;
    private Vector2 _lastPosition;
    private Vector2 _furthestPoint;
    private float _horizontalSpeed;
    private float _verticalSpeed;
    private float _externalHorizontalSpeed;
    private float _externalVerticalSpeed;
    private float _jumpBufferTimeLeft;
    private float _coyoteJumpTimeLeft;
    private float _wallStickTimeLeft;
    private float _wallGrabTimeLeft;
    private float _dashTimer;
    private bool _canWallJump;
    private bool _isWallJumpInProgress;
    private bool _dashJustEnded;
    private bool _canDash = false;
    private Dictionary<GameObject, PlatformController> _platforms = new Dictionary<GameObject, PlatformController>();

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
        _lastPosition = _transform.position;

        SetGravity();
        SetJumpSpeed();

        playerInput.onJumpPressed += OnJumpPressed;
        playerInput.onJumpReleased += OnJumpReleased;
        playerInput.onDashPressed += PlayerInput_onDashPressed;
    }

    #region Start Functions

    private void SetGravity()
    {
        _gravity = 2 * _maxJumpHeight / Mathf.Pow(_timeToJumpApex, 2);
    }

    private void SetJumpSpeed()
    {
        _maxJumpSpeed = _gravity * _timeToJumpApex;
        _minJumpSpeed = Mathf.Sqrt(2 * _gravity * _minJumpHeight);
        _wallGrabJumpSpeed = _gravity * _wallGrabJumpApexTime;
    }

    #endregion

    private void Update()
    {
        CalculateVelocity();

        CalculateGravity();

        if(!_isWallJumpInProgress)
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
        return _velocity;
    }

    public Vector2 GetRawVelocity()
    {
        return _rawMovement;
    }

    public Vector2 GetFurthestPoint()
    {
        return _furthestPoint;
    }

    #region Gravity and Velocity

    private void CalculateVelocity()
    {
        _velocity = ((Vector2)_transform.position - _lastPosition) / Time.deltaTime;
        _lastPosition = _transform.position;
    }

    private void CalculateGravity()
    {
        if (!playerCollision.DownCollision.Colliding && !CanCoyoteJump())
            _verticalSpeed -= _gravity * Time.deltaTime;

        if (playerCollision.IsVerticalliColliding())
            _verticalSpeed = 0;
    }

    #endregion

    #region Walk

    private void Walk()
    {
        var input = playerInput.GetHorizontalInput();

        if (input != 0)
            _horizontalSpeed = Mathf.MoveTowards(_horizontalSpeed, _maxMove * input, _acceleration * Time.deltaTime);
        else
            _horizontalSpeed = Mathf.MoveTowards(_horizontalSpeed, 0, _deceleration * Time.deltaTime);
    }

    #endregion

    #region Jump

    private void OnJumpPressed()
    {
        _jumpBufferTimeLeft = _jumpBuffer;
        _wallGrabJumpTimer = _wallGrabJumpApexTime;

        if(IsOnWall())
            _canWallJump = true;
    }

    private void OnJumpReleased()
    {
        if(_verticalSpeed > _minJumpSpeed)
            _verticalSpeed = _minJumpSpeed;
    }

    private void CoyoteJump()
    {
        if (playerCollision.DownCollision.Colliding)
        {
            _coyoteJumpTimeLeft = _coyoteJump;
            return;
        }

        _coyoteJumpTimeLeft -= Time.deltaTime;
    }

    private void JumpBuffer()
    {
        if (_verticalSpeed > 0 && playerCollision.DownCollision.Colliding)
            _jumpBufferTimeLeft = 0;

        _jumpBufferTimeLeft -= Time.deltaTime;
    }

    private bool CanJump()
    {
        return _jumpBufferTimeLeft > 0;
    }

    private bool CanCoyoteJump()
    {
        return _coyoteJumpTimeLeft > 0;
    }

    private void Jump()
    {
        var downColl = playerCollision.DownCollision.Colliding;

        CoyoteJump();
        JumpBuffer();

        if (CanJump() && (downColl || CanCoyoteJump()))
        {
            OnJump.Invoke();
        }

        if (CanJump() && downColl)
            _verticalSpeed = _maxJumpSpeed;

        if (CanJump() && CanCoyoteJump())
        {
            _verticalSpeed = _maxJumpSpeed;
            _coyoteJumpTimeLeft = 0;
        }
    }

    #endregion

    #region Wall

    public bool IsOnWall()
    {
        return !playerCollision.DownCollision.Colliding && playerCollision.IsHorizontallyColliding();
    }

    private bool CanGrab()
    {
        var right = playerCollision.RightCollision;
        var left = playerCollision.LeftCollision;

        var rightDistance = right.Distance < _grabDistance && right.RayHit;
        var leftDistance = left.Distance < _grabDistance && left.RayHit;
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

        if (_verticalSpeed <= 0)
            _isWallJumpInProgress = false;
    }

    private void WallSlide()
    {
        _wallStickTimeLeft -= Time.deltaTime;

        if (!IsOnWall() && _wallStickTimeLeft <= 0)
            return;

        _isWallJumpInProgress = true;    

        if (_verticalSpeed < 0)
            _verticalSpeed = -_wallSlide;
    }

    private void WallJump(CollisionInfo collision)
    {
        if(IsOnWall() && !playerInput.IsGrabPressed() && _verticalSpeed < 0)
        {
            if(playerInput.IsJumpPressed() && _canWallJump && collision.LastHit)
            {
                _horizontalSpeed = _wallJump.x * -collision.RaycastInfo.RayDirection.x;
                _verticalSpeed = _wallJump.y;
                _wallStickTimeLeft = _wallStickTime;
                _canWallJump = false;
            }
        }
    }

    private void WallGrabJump(CollisionInfo collision, float input)
    {
        _wallGrabJumpTimer -= Time.deltaTime;

        if (CanGrab() && collision != null)
        {
            if(playerInput.IsJumpPressed() && input != 0)
            {
                if (collision.RaycastInfo.RayDirection.x != input)
                {
                    _horizontalSpeed = _wallJump.x * input;
                    _verticalSpeed = _wallJump.y;
                    return;
                }
            }
            
            if (playerInput.IsJumpPressed() && _wallGrabJumpTimer > 0)
            {
                _verticalSpeed = _wallGrabJumpSpeed;
            }
        }
    }

    private void WallGrab(CollisionInfo collision, float input)
    {
        _wallGrabTimeLeft -= Time.deltaTime;

        if (playerCollision.DownCollision.Colliding)
            _wallGrabTimeLeft = _wallGrabTime;

        if (CanGrab() && collision != null && _wallGrabTimeLeft > 0)
        {
            if(collision.FirstHit && collision.HitCount == 1)
            {
                _verticalSpeed = _topEdgeClimbJump.y;
                _horizontalSpeed = _topEdgeClimbJump.x * collision.RaycastInfo.RayDirection.x;
                return;
            }

            _verticalSpeed = input * _wallClimb;
            _horizontalSpeed = 0;
        }    
    }

    #endregion

    #region Dash

    private void PlayerInput_onDashPressed()
    {
        if(_canDash)
        {
            _dashTimer = _dashDuration;
            OnDash?.Invoke();
        }
    }

    private void Dash()
    {
        var inputX = playerInput.GetHorizontalInput();
        var inputY = playerInput.GetVerticalInput();

        if (inputX == 0 && inputY == 0)
            inputX = 1;

        Vector2 dir = new Vector2(inputX, inputY).normalized;

        if (playerCollision.DownCollision.Colliding)
            _canDash = true;

        _dashTimer -= Time.deltaTime;

        if (_dashTimer <= 0)
        {
            if(!_dashJustEnded)
            {
                _verticalSpeed = _ySpeedAdterDash * inputY;
                _dashJustEnded = true;
            }

            return;
        }

        var furthestPoint = GetDashFurthestPoint(dir);
        var hit = playerCollision.GetDashHitPos(dir, furthestPoint);
        var distnace = Vector2.Distance(_transform.position, hit);
        var clampDistance = Mathf.Clamp(distnace, 0, _dashDistance);
        var velocity = clampDistance / _dashDuration * dir;
        _horizontalSpeed = velocity.x;
        _verticalSpeed = velocity.y;
        _canDash = false;
        _dashJustEnded = false;
    }

    private Vector2 GetDashFurthestPoint(Vector2 dir)
    {
        return (Vector2)_transform.position + dir * _dashDistance;
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
        _externalHorizontalSpeed = 0;
        _externalVerticalSpeed = 0;

        if (!coll)
            return;

        if(!_platforms.ContainsKey(obj))
        {
            platform = obj.GetComponent<PlatformController>();
            _platforms.Add(obj, platform);
        }

        platform = _platforms[obj];

        if (!playerCollision.DownCollision.Colliding && _verticalSpeed == 0 && playerCollision.PlatformDownCollision.RayHit)
            playerCollision.ForceVerticalReposition(playerCollision.DownCollision);

        var rawVelocity = platform.GetRawVelocity();
        _externalHorizontalSpeed = rawVelocity.x;
        _externalVerticalSpeed = _rawMovement.y;
        return;
    }

    #endregion

    private void ClampSpeedY()
    {
        if(_verticalSpeed < 0)
            _verticalSpeed = Mathf.Clamp(_verticalSpeed, -_maxFallSpeed, -_minFallSpeed);
    }

    private void Move()
    {
        var pos = _transform.position;
        _rawMovement = new Vector2(_horizontalSpeed, _verticalSpeed);
        var totalMovement = new Vector2(_rawMovement.x + _externalHorizontalSpeed, _rawMovement.y + _externalVerticalSpeed);
        var move = totalMovement * Time.deltaTime;
        _furthestPoint = (Vector2)pos + move;

        playerCollision.HandleCollisions(_furthestPoint, ref move, totalMovement);

        _transform.position += (Vector3)move;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(_furthestPoint, Vector2.one);
    }

    private void OnDestroy()
    {
        playerInput.onJumpPressed -= OnJumpPressed;
        playerInput.onJumpReleased -= OnJumpReleased;
    }
}
