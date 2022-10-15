using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blinker : MonoBehaviour
{
    public Sprite[] sprites;
    public float frameTime = 0.4f;

    private float frameTimer = 0;
    private int spriteIndex = 0;
    private SpriteRenderer sr;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = sprites[0];
    }

    private void Update()
    {
        frameTimer += Time.deltaTime;
        if (frameTimer >= frameTime)
		{
            frameTimer = 0;
            spriteIndex++;
            if (spriteIndex >= sprites.Length)
			{
                spriteIndex = 0;
			}
            sr.sprite = sprites[spriteIndex];
        }
    }
}
