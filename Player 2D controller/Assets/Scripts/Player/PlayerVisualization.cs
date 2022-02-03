using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisualization : MonoBehaviour
{
    [SerializeField] private Transform _flipObject;
    [SerializeField] private ParticleSystem _walkParticles;
    [SerializeField] private ParticleSystem _jumpParticles;

    private string _currentAnimation;
    private string _lastAnimation;
    private bool _lastFlip;
    private Vector2 _walkParticleStartSize;
    private ParticleSystem.MainModule _walkParticlesMain;

    private PlayerMovement _playerMovement;
    private PlayerCollision _playerCollision;
    private AnimationController _animation;
    private SpriteRenderer _spriteRenderer;
    private ScreenShakeManager _screenShake;

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _playerCollision = GetComponent<PlayerCollision>();
        _animation = GetComponent<AnimationController>();
        _spriteRenderer = _animation.GetSpriteRenderer();
    }

    private void Start()
    {
        _screenShake = ScreenShakeManager.s_singleton;

        _walkParticlesMain = _walkParticles.main;
        _walkParticleStartSize.x = _walkParticlesMain.startSize.constantMin;
        _walkParticleStartSize.y = _walkParticlesMain.startSize.constantMax;

        _playerMovement.OnDash += OnDash;
        _playerMovement.OnJump += OnJump;
    }


    private void Update()
    {
        WalkParticles();
        IDLEAnimation();
        WalkAnimation();
        JumpAnimation();
        WallAnimation();
        FlipSprite();
        SetAnimation();
    }

    private void OnJump()
    {
        _jumpParticles.Play();
    }

    private void OnDash()
    {
        _screenShake.PlayCameraShake("Normal Shake");
    }

    private void IDLEAnimation()
    {
        _currentAnimation = "IDLE";
    }

    private void WalkParticles()
    {
        var velocity = _playerMovement.GetRawVelocity();
        var downCol = _playerCollision.DownCollision.Colliding;

        if (!downCol)
        {
            _walkParticlesMain.startSize = 0;
            return;
        }
            
        if (velocity.x != 0)
        {
            var val = _walkParticleStartSize;
            _walkParticlesMain.startSize = new ParticleSystem.MinMaxCurve(val.y, val.x);
        }
        else
            _walkParticlesMain.startSize = 0;
    }

    private void WalkAnimation()
    {
        var velocity = _playerMovement.GetRawVelocity();
        var downCollision = _playerCollision.DownCollision.Colliding;

        if (!downCollision)
            return;

        if (velocity.x != 0)
            _currentAnimation = "Walk";
    }

    private void JumpAnimation()
    {
        var velocity = _playerMovement.GetRawVelocity();
        var downCollision = _playerCollision.DownCollision.Colliding;

        if (downCollision)
            return;

        if (velocity.y > .1f)
            _currentAnimation = "Jump";
        if (velocity.y < -.1f)
            _currentAnimation = "Fall";
    }

    private void WallAnimation()
    {
        if (_playerMovement.IsOnWall())
            _currentAnimation = "Wall";
    }

    private void FlipSprite()
    {
        var velocity = _playerMovement.GetRawVelocity();

        if (velocity.x > .1f)
        {
            FlipSpriteToLeft(false);
            FlipObject(false);
            _lastFlip = false;
            return;
        }

        if (velocity.x < -.1f)
        {
            FlipSpriteToLeft(true);
            FlipObject(false);
            _lastFlip = true;
            return;
        }

        FlipSpriteToLeft(_lastFlip);
        FlipObject(_lastFlip);
    }

    private void FlipObject(bool leftSide)
    {
        var scale = _flipObject.localScale;
        scale.x = leftSide ? 1 : -1;

        _flipObject.localScale = scale;
    }

    private void FlipSpriteToLeft(bool leftSide)
    {
        _spriteRenderer.flipX = leftSide;
    }

    private void SetAnimation()
    {
        if (_currentAnimation == _lastAnimation)
            return;

        _animation.PlayAnimation(_currentAnimation);
        _lastAnimation = _currentAnimation;
    }

    private void OnDestroy()
    {
        _playerMovement.OnDash -= OnDash;
        _playerMovement.OnJump -= OnJump;
    }
}
