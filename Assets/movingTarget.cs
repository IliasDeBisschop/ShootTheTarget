using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class movingTarget : MonoBehaviour
{
    public Transform pointB;
    public float speed = 2f;
    private Vector3 pointA;
    private bool movingToB = true;
    // Start is called before the first frame update
    void Start()
    {
        pointA = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (movingToB)
        {
            UnityEngine.Debug.LogWarning("Moving to B");
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, pointB.position, step);
        }
        else
        {
            UnityEngine.Debug.LogWarning("Moving to A");
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, pointA, step);
        }
    }

}
