using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Springboard : MonoBehaviour
{
    public float bounceForce = 15;
    private const float DISABLE_TIME = 0.5f;
    private Collider2D coll;

    private void Start()
    {
        coll = GetComponent<Collider2D>();
    }

    public void Bounce()
    {
        //TODO animation and sound
        StartCoroutine(CrtDeactivate());
    }

    private IEnumerator CrtDeactivate()
    {
        coll.enabled = false;
        yield return new WaitForSeconds(DISABLE_TIME);
        coll.enabled = true;
    }
}
