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
    private AudioSource audioSource;
    public float bulletSpeed = 20f;

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

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            
            // Check if Rigidbody exists, if not add one
            if (rb == null)
            {
                UnityEngine.Debug.LogWarning("Bullet prefab missing Rigidbody component. Adding one dynamically.");
                rb = bullet.AddComponent<Rigidbody>();
            }
            
            // Check if BoxCollider exists, if not add one
            BoxCollider boxCollider = bullet.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                UnityEngine.Debug.LogWarning("Bullet prefab missing BoxCollider component. Adding one dynamically.");
                boxCollider = bullet.AddComponent<BoxCollider>();
            }
            
            rb.velocity = firePoint.forward * bulletSpeed;
        }
    }
}
