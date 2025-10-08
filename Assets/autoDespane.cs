using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class autoDespane : MonoBehaviour
{
    public float seconds = 5f;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, seconds);
    }
}
