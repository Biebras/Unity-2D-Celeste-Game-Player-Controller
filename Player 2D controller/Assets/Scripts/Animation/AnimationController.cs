using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Animation
{
    public string Name;
    public Sprite[] Sprites;
    public float Fps;
    public bool FlipX;
    public bool FlipY;
}

public class AnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Animation[] _animations;
    [SerializeField] private bool _playOnStart; 

    private void Start()
    {
        if(_playOnStart)
        {
            Animation startAnimation = _animations[0];
            PlayAnimation(startAnimation.Name);
        }
    }

    public SpriteRenderer GetSpriteRenderer()
    {
        return _spriteRenderer;
    }

    public bool AnimationExist(string name)
    {
        Animation animation = Array.Find(_animations, s => s.Name == name);

        return animation != null;
    }

    public void PlayAnimation(string name)
    {
        Animation animation = GetAnimation(name);
        StopAllCoroutines();
        StartCoroutine(StartAnimation(animation));
    }

    private Animation GetAnimation(string name)
    {
        Animation animation = Array.Find(_animations, s => s.Name == name);

        if (animation == null)
            throw new Exception("Couldn't find animation with the name: " + name);

        return animation;
    }


    private IEnumerator StartAnimation(Animation animation)
    {
        _spriteRenderer.flipX = animation.FlipX;
        _spriteRenderer.flipY = animation.FlipY;

        var frames = animation.Sprites.Length;
        var time = frames / animation.Fps;

        while (true)
        {
            for (int i = 0; i < frames; i++)
            {
                _spriteRenderer.sprite = animation.Sprites[i];
                yield return new WaitForSeconds(time);
            }

            if (frames == 1)
                break;
        }
    }
}
