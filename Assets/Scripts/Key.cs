using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour
{
    public void Collect()
    {
        Destroy(gameObject);
    }
}
