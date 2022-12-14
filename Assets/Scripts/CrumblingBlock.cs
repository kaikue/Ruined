using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrumblingBlock : MonoBehaviour
{
    public Sprite crumbleSprite;
    public AudioClip crumbleSound;
    public AudioClip fallSound;
    public AudioClip landSound;
    [HideInInspector]
    public bool falling = false;
    [HideInInspector]
    public bool activated = false;
    //public bool fallen = false;

    private const float CRUMBLE_TIME = 1;

    private ParticleSystem ps;
    private AudioSource audioSrc;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    private void Start()
    {
        ps = GetComponent<ParticleSystem>();
        audioSrc = GetComponent<AudioSource>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Crumble()
    {
        if (activated)
        {
            return;
        }
        activated = true;
        StartCoroutine(CrtCrumble());
    }

    private IEnumerator CrtCrumble()
    {
        sr.sprite = crumbleSprite;
        ps.Play();
        ParticleSystem.EmissionModule em = ps.emission;
        em.enabled = true;
        audioSrc.PlayOneShot(crumbleSound);

        yield return new WaitForSeconds(CRUMBLE_TIME);

        falling = true;
        audioSrc.PlayOneShot(fallSound);
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject collider = collision.collider.gameObject;
        if (collider.layer == LayerMask.NameToLayer("Tiles"))
        {
            //falling = false;
            //fallen = true;
            audioSrc.PlayOneShot(landSound);
            //TODO land particles
            //rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        CrumblingBlock crumble = collider.GetComponent<CrumblingBlock>();
        if (crumble != null && falling)// && collision.contacts[0].normal.y < 0)
        {
            crumble.Crumble();
        }
    }
}
