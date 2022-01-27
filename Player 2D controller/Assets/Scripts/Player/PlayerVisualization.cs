using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisualization : MonoBehaviour
{
    private string currentAnimation;
    private string lastAnimation;
    private bool lastFlip;

    private PlayerMovement playerMovement;
    private PlayerCollision playerCollision;
    private new AnimationController animation;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerCollision = GetComponent<PlayerCollision>();
        animation = GetComponent<AnimationController>();
        spriteRenderer = animation.GetSpriteRenderer();
    }

    private void Update()
    {
        IDLEAnimation();
        WalkAnimation();
        JumpAnimation();
        WallAnimation();
        FlipSprite();
        SetAnimation();
    }

    private void IDLEAnimation()
    {
        currentAnimation = "IDLE";
    }

    private void WalkAnimation()
    {
        var velocity = playerMovement.GetRawVelocity();
        var downCollision = playerCollision.downCollision.colliding;

        if (!downCollision)
            return;

        if (velocity.x != 0)
            currentAnimation = "Walk";
    }

    private void JumpAnimation()
    {
        var velocity = playerMovement.GetRawVelocity();
        var downCollision = playerCollision.downCollision.colliding;

        if (downCollision)
            return;

        if (velocity.y > .1f)
            currentAnimation = "Jump";
        if (velocity.y < -.1f)
            currentAnimation = "Fall";
    }

    private void WallAnimation()
    {
        if (playerMovement.IsOnWall())
            currentAnimation = "Wall";
    }

    private void FlipSprite()
    {
        var velocity = playerMovement.GetRawVelocity();

        if (velocity.x > .1f)
        {
            FlipSpriteToLeft(false);
            lastFlip = false;
            return;
        }

        if (velocity.x < -.1f)
        {
            FlipSpriteToLeft(true);
            lastFlip = true;
            return;
        }

        FlipSpriteToLeft(lastFlip);
    }

    private void FlipSpriteToLeft(bool leftSide)
    {
        spriteRenderer.flipX = leftSide;
    }

    private void SetAnimation()
    {
        if (currentAnimation == lastAnimation)
            return;

        animation.PlayAnimation(currentAnimation);
        lastAnimation = currentAnimation;
    }
}
