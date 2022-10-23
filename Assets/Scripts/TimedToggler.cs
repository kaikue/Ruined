using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedToggler : MonoBehaviour
{
    public bool startActive = true;
    public float toggleTime = 3;
    private bool active;

    private const float INACTIVE_ALPHA = 0.4f;

    private SpriteRenderer sr;
    private Collider2D coll;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        coll = GetComponent<Collider2D>();
        StartCoroutine(ToggleLoop());

        active = !startActive; //Lol
        Toggle();
    }

    private IEnumerator ToggleLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(toggleTime);
            Toggle();
        }
    }

    private void Toggle()
    {
        active = !active;
        coll.enabled = active;
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, active ? 1 : INACTIVE_ALPHA);
    }
}
