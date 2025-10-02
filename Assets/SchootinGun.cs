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
        firePoint = GetComponent<Transform>().Find("FirePoint");
        bulletPrefab = Resources.Load<GameObject>("BulletPrefab");
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
            
            UnityEngine.Debug.Log("shoot");

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            rb.velocity = firePoint.forward * bulletSpeed;


        }
    }
}
