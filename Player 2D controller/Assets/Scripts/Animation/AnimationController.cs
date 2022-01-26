using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Animation
{
    public string name;
    public Sprite[] sprites;
    public float fps;
    public bool flipX;
    public bool flipY;
}

public class AnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animation[] animations;
    [SerializeField] private bool playOnStart; 

    private void Start()
    {
        if(playOnStart)
        {
            Animation startAnimation = animations[0];
            PlayAnimation(startAnimation.name);
        }
    }

    public SpriteRenderer GetSpriteRenderer()
    {
        return spriteRenderer;
    }

    public bool AnimationExist(string name)
    {
        Animation animation = Array.Find(animations, s => s.name == name);

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
        Animation animation = Array.Find(animations, s => s.name == name);

        if (animation == null)
            throw new Exception("Couldn't find animation with the name: " + name);

        return animation;
    }


    private IEnumerator StartAnimation(Animation animation)
    {
        spriteRenderer.flipX = animation.flipX;
        spriteRenderer.flipY = animation.flipY;

        var frames = animation.sprites.Length;
        var invertTime = animation.fps / frames;
        var time = Mathf.Pow(invertTime, -1);

        while (true)
        {
            for (int i = 0; i < frames; i++)
            {
                spriteRenderer.sprite = animation.sprites[i];
                yield return new WaitForSeconds(time);
            }

            if (frames == 1)
                break;
        }
    }
}
