using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveTarged : MonoBehaviour
{
    public float speed = 5f;
    public Transform pointA;
    private Vector3 startPosition;
    private bool movingToPointA = true;
    public float reachThreshold = 0.01f; // tolerantie om "bereikt" te detecteren

    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (pointA == null) return;

        Vector3 target = movingToPointA ? pointA.position : startPosition;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < reachThreshold)
        {
            movingToPointA = !movingToPointA;
        }
    }
}
