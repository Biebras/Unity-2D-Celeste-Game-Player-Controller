using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisualization : MonoBehaviour
{
    [SerializeField] private ParticleSystem _walkParticles;
    [SerializeField] private ParticleSystem _jumpParticles;
    [SerializeField] private float _walkSoundDelay = 0.25f;

    private string _currentAnimation;
    private string _lastAnimation;
    private bool _lastFlip;
    private float _walkSoundTimer;
    private Vector2 _walkParticleStartSize;
    private ParticleSystem.MainModule _walkParticlesMain;

    private PlayerMovement _playerMovement;
    private PlayerCollision _playerCollision;
    private AnimationController _animation;
    private SpriteRenderer _spriteRenderer;
    private ScreenShakeManager _screenShake;
    private AudioManager _audioManager;

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
        _audioManager = AudioManager.s_singleton;

        _walkParticlesMain = _walkParticles.main;
        _walkParticleStartSize.x = _walkParticlesMain.startSize.constantMin;
        _walkParticleStartSize.y = _walkParticlesMain.startSize.constantMax;

        _playerMovement.OnDash += OnDash;
        _playerMovement.OnJump += OnJump;
        _playerMovement.OnLand += OnLand;
        _playerCollision.OnPlayerTriggerInteractables += OnPlayerTriggerInteractables;
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

    private void OnPlayerTriggerInteractables(GameObject hit)
    {
        if(hit.CompareTag("Spykes"))
        {
            _audioManager.Play("Death");
        }
    }

    private void OnLand()
    {
        _jumpParticles.Play();
        _audioManager.Play("Land");
    }

    private void OnJump()
    {
        _jumpParticles.Play();
        _audioManager.Play("Jump");
    }

    private void OnDash()
    {
        _screenShake.PlayCameraShake("Normal Shake");
        _audioManager.Play("Dash");
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

        if (velocity.x == 0)
        {
            _walkSoundTimer = 0;
            return;
        }

        _currentAnimation = "Walk";
        _walkSoundTimer -= Time.deltaTime;

        if (_walkSoundTimer <= 0)
        {
            _audioManager.CheckThenPlay("Footstep");
            _walkSoundTimer = _walkSoundDelay;
        }
        
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
            _lastFlip = false;
            return;
        }

        if (velocity.x < -.1f)
        {
            FlipSpriteToLeft(true);
            _lastFlip = true;
            return;
        }

        FlipSpriteToLeft(_lastFlip);
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
        _playerMovement.OnLand -= OnLand;
        _playerCollision.OnPlayerTriggerInteractables -= OnPlayerTriggerInteractables;
    }
}
