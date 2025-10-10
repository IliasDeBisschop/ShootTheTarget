using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class SchootinGun : MonoBehaviour
{
    public OVRInput.Button shootButton;
    public GameObject bulletPrefab;
    public Transform firePoint;
    private OVRGrabbable grabable;
    public AudioSource audioSource;
    public AudioClip shootClip; // Voeg een AudioClip toe voor het mp3-geluid
    public float bulletSpeed = 20f;

    // --- toegevoegd: cooldown tussen schoten (seconden) ---
    public float shootCooldown = 0.5f; // stel in in Inspector
    private float nextFireTime = 0f;

    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Debug.Log("Hello from Unity Debug!");
        System.Diagnostics.Debug.WriteLine("Hello from System Debug!");

        grabable = GetComponent<OVRGrabbable>();
        
        // Only find FirePoint if not set in Inspector
        if (firePoint == null)
        {
            firePoint = GetComponent<Transform>().Find("FirePoint");
        }
        
        // Only load from Resources if not set in Inspector
        if (bulletPrefab == null)
        {
            bulletPrefab = Resources.Load<GameObject>("BulletPrefab");
        }
        
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(shootButton, OVRInput.Controller.RTouch))
        {
            UnityEngine.Debug.Log("01Right hand button pressed!");
            UnityEngine.Debug.Log("01 grabable.grabbedBy", grabable.grabbedBy);
        }
       
        if (grabable.isGrabbed && OVRInput.GetDown(shootButton, grabable.grabbedBy.Controller))
        {
            // Check cooldown
            if (Time.time < nextFireTime)
            {
                // Optioneel: log of speel een 'klik' geluid om aan te geven dat je nog niet kunt schieten
                UnityEngine.Debug.Log("Nog in cooldown, niet schieten");
                return;
            }

            // Zet volgende schiettijd
            nextFireTime = Time.time + shootCooldown;

            // Check if required components are available
            if (bulletPrefab == null)
            {
                UnityEngine.Debug.LogError("BulletPrefab is not assigned!");
                return;
            }
            
            if (firePoint == null)
            {
                UnityEngine.Debug.LogError("FirePoint is not assigned!");
                return;
            }
            
            UnityEngine.Debug.Log("shoot");

            // Instantiate bullet at firePoint position with firePoint rotation
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            // Zorg dat de kogel zichtbaar is
            Renderer bulletRenderer = bullet.GetComponent<Renderer>();
            if (bulletRenderer != null)
            {
                bulletRenderer.enabled = true;
            }

            // Zet useGravity aan op de hoofd-Rigidbody en eventuele child Rigidbodies
            if (rb != null)
            {
                rb.useGravity = true;
                foreach (var childRb in bullet.GetComponentsInChildren<Rigidbody>(true))
                {
                    if (childRb != null) childRb.useGravity = true;
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Bullet has no Rigidbody, cannot enable gravity.");
            }

            // Make sure the bullet rotates to face the direction it's traveling
            Vector3 shootDirection = firePoint.forward;
            bullet.transform.rotation = Quaternion.LookRotation(shootDirection);

            // Set velocity in the forward direction of the firePoint
            if (rb != null)
            {
                rb.velocity = shootDirection * bulletSpeed;
            }

            // --- Schakel specifiek de componenten "colliding" en "autoDespane" in (case-insensitive) ---
            foreach (MonoBehaviour mb in bullet.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                string tn = mb.GetType().Name.ToLower();
                if (tn == "colliding" || tn == "autodespane")
                {
                    mb.enabled = true;
                }
            }

            // --- Schakel BoxCollider(s) en alle Collider(s) in de kinderen in ---
            BoxCollider[] boxColliders = bullet.GetComponentsInChildren<BoxCollider>(true);
            foreach (var bc in boxColliders)
            {
                if (bc != null) bc.enabled = true;
            }

            Collider[] colliders = bullet.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders)
            {
                if (c != null) c.enabled = true;
            }
            
            // Speel het mp3-geluid af als shootClip is ingesteld
            if (audioSource != null && shootClip != null)
            {
                UnityEngine.Debug.Log("Playing shoot sound");
                audioSource.PlayOneShot(shootClip);
            }
        }
    }
}
