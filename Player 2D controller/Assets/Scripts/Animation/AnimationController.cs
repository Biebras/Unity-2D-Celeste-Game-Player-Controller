using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Animation
{
    public string name;
    public Sprite[] sprites;
    public float animationSpeed;
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
        while(true)
        {
            for (int i = 0; i < animation.sprites.Length; i++)
            {
                spriteRenderer.sprite = animation.sprites[i];
                yield return new WaitForSeconds(1 / animation.animationSpeed);
            }
        }
    }
}
