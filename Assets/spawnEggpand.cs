using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spawnEggpand : MonoBehaviour
{
    [SerializeField] private GameObject eggplantPrefab;
    [SerializeField] private int spawnCount = 30;
    [SerializeField] private float verticalSpacing = 0.5f;
    [SerializeField] private float horizontalOffset = 0.2f; // kleine horizontale offset
    [SerializeField] private float initialScale = 0.4f; // initial scale of spawned eggplants

    void Start()
    {
        SpawnHere();
    }

    [ContextMenu("Spawn Here")]
    public void SpawnHere()
    {
        if (eggplantPrefab == null)
        {
            Debug.LogWarning("eggplantPrefab not assigned in inspector.");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            // spawn at this object's position, slightly offset in Y so they don't overlap exactly
            Vector3 spawnPos = transform.position + Vector3.up * (i * verticalSpacing);

            // kleine willekeurige horizontale offset (X en Z)
            Vector3 horiz = new Vector3(
                Random.Range(-horizontalOffset, horizontalOffset),
                0f,
                Random.Range(-horizontalOffset, horizontalOffset)
            );
            spawnPos += horiz;

            GameObject instance = Instantiate(eggplantPrefab, spawnPos, Quaternion.identity, transform);

            // make the spawned instance 50% smaller (nu 30% in huidige code)
            instance.transform.localScale = instance.transform.localScale * initialScale;

            // add a collider if the prefab doesn't have one
            if (instance.GetComponent<Collider>() == null)
            {
                var col = instance.AddComponent<CapsuleCollider>();
                // optional: adjust collider size/center to better match the model
                col.height = 1f;
                col.radius = 0.25f;
                col.center = Vector3.up * 0.5f;
            }

            // add a Rigidbody if the prefab doesn't have one
            if (instance.GetComponent<Rigidbody>() == null)
            {
                var rb = instance.AddComponent<Rigidbody>();
                rb.mass = 1f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }
    }

    void Update()
    {
        
    }
}