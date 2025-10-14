using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class colliding : MonoBehaviour
{
    [SerializeField] private float innerRadius = 0.02515134f;   // afstand voor bullseye (10 punten)
    [SerializeField] private float outerRadius = 0.2858795f;    // afstand voor buitenrand (1 punt)
    [SerializeField, Range(2, 10)] private int rings = 10;      // aantal ringen (10 -> 10..1)

    // nieuwe velden
    private Rigidbody rb;
    private Vector3 previousPosition;
    private bool hitProcessed = false;

    [SerializeField] private Text scoreText;                    // optionele reference in inspector
    [SerializeField] private string scoreObjectName = "score Text"; // naam van het UI-object

    // audio: mp3/AudioClip om af te spelen bij vernietiging
    [SerializeField] private AudioClip destroyClip;
    [SerializeField, Range(0f, 1f)] private float destroyVolume = 1f;

    // prefab dat gespawned wordt als de kogel iets raakt
    [SerializeField] private GameObject spawnOnHit;

    private TextMeshProUGUI scoreTMP;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // voorkom tunneling bij hoge snelheid
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        previousPosition = transform.position;

        // als geen reference in inspector, probeer automatisch te vinden op naam
        if (scoreText == null)
        {
            var go = GameObject.Find(scoreObjectName);
            if (go != null)
            {
                scoreText = go.GetComponent<Text>();
                if (scoreText == null)
                {
                    scoreTMP = go.GetComponent<TextMeshProUGUI>();
                }
            }
        }
        else
        {
            // indien scoreText gevuld in inspector, geen TMP nodig (maar controleer alsnog TMP)
            if (scoreText == null)
            {
                var go = GameObject.Find(scoreObjectName);
                if (go != null)
                    scoreTMP = go.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private void FixedUpdate()
    {
        if (hitProcessed) return;

        Vector3 currentPosition = transform.position;
        Vector3 move = currentPosition - previousPosition;
        float moveDist = move.magnitude;

        if (moveDist > 0.001f)
        {
            // raycast tussen oude en nieuwe positie om snelle penetratie te detecteren
            RaycastHit[] hits = Physics.RaycastAll(previousPosition, move.normalized, moveDist);
            foreach (var h in hits)
            {
                if (h.collider != null && h.collider.CompareTag("Target"))
                {
                    Debug.Log($"[colliding] Raycast hit {h.collider.name}");
                    // geef ook de normale door zodat de splat correct op het oppervlak kan oriënteren
                    ProcessHit(h.point, h.collider.transform, h.normal);
                    break;
                }
            }
        }

        previousPosition = currentPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[colliding] OnCollisionEnter with {collision.gameObject.name} tag={collision.gameObject.tag}");
        try
        {
            if (collision.gameObject.CompareTag("Gun") || collision.gameObject.CompareTag("Splat")) return;
            if (!collision.gameObject.CompareTag("Target"))
            {
                Debug.Log($"[colliding] non-Target, destroying bullet. {collision.gameObject.name}, tag={collision.gameObject.tag}");

                if (!hitProcessed)
                {
                    hitProcessed = true;
                    // bepaal raakpunt en normale (indien beschikbaar) zodat de splat correct georiënteerd wordt
                    Vector3 impactPoint = transform.position;
                    Vector3 impactNormal = Vector3.zero;
                    if (collision.contacts != null && collision.contacts.Length > 0)
                    {
                        impactPoint = collision.contacts[0].point;
                        impactNormal = collision.contacts[0].normal;
                    }
                    else
                    {
                        impactPoint = transform.position;
                        impactNormal = (impactPoint - collision.transform.position).normalized;
                    }
                    if (impactNormal == Vector3.zero) impactNormal = collision.transform.up != Vector3.zero ? collision.transform.up : Vector3.up;

                    // spawn prefab bij impact en parent aan het object zodat het meebeweegt
                    Vector3 spawnPos = impactPoint + impactNormal * 0.01f;
                    SpawnOnHit(spawnPos, Quaternion.identity, collision.transform, impactNormal);

                    // speel geluid bij impact (zelfde destroyClip wordt gebruikt)
                    if (destroyClip != null && destroyVolume > 0f)
                    {
                        AudioSource.PlayClipAtPoint(destroyClip, transform.position, destroyVolume);
                    }
                    Destroy(gameObject);
                }

                return;
            }
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tag 'Target' niet gedefinieerd.");
            return;
        }

        if (hitProcessed) return;

        Transform targetT = collision.transform;
        Vector3 pivot = targetT.position;
        Vector3 contactPoint = (collision.contacts != null && collision.contacts.Length > 0)
            ? collision.contacts[0].point
            : pivot;

        // gebruik contact normal indien beschikbaar zodat splat flush op het oppervlak komt
        Vector3 contactNormal = (collision.contacts != null && collision.contacts.Length > 0)
            ? collision.contacts[0].normal
            : (contactPoint - pivot).normalized;

        ProcessHit(contactPoint, targetT, contactNormal);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[colliding] OnTriggerEnter with {other.gameObject.name} tag={other.gameObject.tag}");
        try
        {
            if (!other.CompareTag("Target")) {
                Debug.Log($"[colliding] Trigger enter with non-Target, destroying bullet. {other.gameObject.name}");

                if (!hitProcessed)
                {
                    hitProcessed = true;
                    // bepaal closest point en een redelijke normale zodat de splat correct georiënteerd wordt
                    Vector3 closestPoint = other.ClosestPoint(transform.position);
                    Vector3 approxNormal = (closestPoint - other.transform.position).normalized;
                    if (approxNormal == Vector3.zero) approxNormal = other.transform.up != Vector3.zero ? other.transform.up : Vector3.up;

                    Vector3 spawnPos = closestPoint + approxNormal * 0.01f;
                    // parent aan het object zodat splat meebeweegt
                    SpawnOnHit(spawnPos, Quaternion.identity, other.transform, approxNormal);

                    if (destroyClip != null && destroyVolume > 0f)
                    {
                        AudioSource.PlayClipAtPoint(destroyClip, transform.position, destroyVolume);
                    }
                    Destroy(gameObject);
                }

                return;
            }
        }
        catch (UnityException)
        {
            Debug.Log("Tag 'Target' niet gedefinieerd.");
            return;
        }
        Debug.Log($"[colliding] Trigger enter with Target {other.gameObject.name}");
        if (hitProcessed) return;

        Transform targetT = other.transform;
        Vector3 pivot = targetT.position;
        Vector3 hitPoint = other.ClosestPoint(transform.position);

        // ClosestPoint geeft geen normale; bereken een redelijke normale richting van het doel naar raakpunt
        Vector3 hitNormal = (hitPoint - pivot).normalized;
        if (hitNormal == Vector3.zero) hitNormal = targetT.up; // fallback

        ProcessHit(hitPoint, targetT, hitNormal);
    }

    // centrale hit-verwerking
    private void ProcessHit(Vector3 hitPoint, Transform targetT, Vector3 hitNormal)
    {
        if (hitProcessed) return;
        hitProcessed = true;

        Vector3 pivot = targetT.position;
        float distance = Vector3.Distance(pivot, hitPoint);
        int points = DistanceToPoints(distance);

        Debug.Log($"distance: {distance}  points: {points}");

        // spawn prefab precies op het raakpunt
        // als geen normale is meegegeven, probeer uit positie tov pivot te berekenen
        if (hitNormal == Vector3.zero) hitNormal = (hitPoint - pivot).normalized;
        if (hitNormal == Vector3.zero) hitNormal = targetT.up; // laatste fallback

        // kleine offset langs normale om z-fighting te vermijden
        Vector3 spawnPos = hitPoint + hitNormal * 0.01f;
        // oriënteer splat zodat de quad vlak op het oppervlak ligt (forward = normale)
        // veel quads in Unity wijzen met +Z naar voren; probeer -hitNormal als de quad 'voor' naar -Z tekent
        Quaternion spawnRot = Quaternion.LookRotation(-hitNormal, targetT.up);
        // maak splat child van het target zodat hij mee beweegt/roteren met het doel
        SpawnOnHit(spawnPos, spawnRot, targetT, hitNormal);

        // lees huidige score uit de UI, tel erbij en update
        int current = GetDisplayedScore();
        int total = current + points;
        string display = $"Score: {total}";

        if (scoreText != null)
        {
            scoreText.text = display;
        }
        else if (scoreTMP != null)
        {
            scoreTMP.text = display;
        }
        else
        {
            // fallback: probeer alsnog te vinden
            var go = GameObject.Find(scoreObjectName);
            if (go != null)
            {
                var t = go.GetComponent<Text>();
                if (t != null) t.text = display;
                else
                {
                    var tt = go.GetComponent<TextMeshProUGUI>();
                    if (tt != null) tt.text = display;
                }
            }
        }

        // speel geluid (mp3 als AudioClip) vlak voor vernietiging
        if (destroyClip != null && destroyVolume > 0f)
        {
            Debug.Log("[colliding] Playing destroyClip");
            AudioSource.PlayClipAtPoint(destroyClip, transform.position, destroyVolume);
        }
        else
        {
            Debug.LogWarning("[colliding] destroyClip ontbreekt of volume is 0");
        }

        Destroy(gameObject);
    }

    // helper: haal het getal uit teksten als "Score: 123" of anders extract digits
    private int GetDisplayedScore()
    {
        string text = null;
        if (scoreText != null) text = scoreText.text;
        else if (scoreTMP != null) text = scoreTMP.text;
        else
        {
            var go = GameObject.Find(scoreObjectName);
            if (go != null)
            {
                var t = go.GetComponent<Text>();
                if (t != null) text = t.text;
                else
                {
                    var tt = go.GetComponent<TextMeshProUGUI>();
                    if (tt != null) text = tt.text;
                }
            }
        }

        if (string.IsNullOrEmpty(text)) return 0;

        // verwacht "Score: X" - probeer na ':' te parsen
        var parts = text.Split(':');
        if (parts.Length >= 2)
        {
            string numberPart = parts[1].Trim();
            if (int.TryParse(numberPart, out int v)) return v;
        }

        // fallback: extracteer eerste integer uit de string
        var m = Regex.Match(text, @"-?\d+");
        if (m.Success && int.TryParse(m.Value, out int fallback)) return fallback;

        return 0;
    }

    private int DistanceToPoints(float distance)
    {
        // Valideer radii
        if (innerRadius <= 0f) innerRadius = 0.0001f;
        if (outerRadius <= innerRadius)
        {
            // fallback: als outer niet juist is ingesteld, geef 10 bij binnen inner, anders 1
            return distance <= innerRadius ? 10 : 1;
        }

        if (distance <= innerRadius) return 10;
        if (distance > outerRadius) return 0; // buiten doel -> geen punten

        float step = (outerRadius - innerRadius) / (rings - 1);
        for (int i = 0; i < rings; i++)
        {
            float threshold = innerRadius + i * step;
            if (distance <= threshold)
            {
                return 10 - i; // i=0 -> 10, i=9 -> 1 (voor rings=10)
            }
        }

        return 0;
    }

    // helper om prefab te spawnen (doet niets als prefab null is)
    // normal: als niet Vector3.zero, dan wordt de rotatie aangepast zodat prefab's +Z op -normal wijst (quad vlak op oppervlak)
    private void SpawnOnHit(Vector3 position, Quaternion rotation, Transform parent = null, Vector3 normal = default(Vector3))
    {
        if (spawnOnHit == null) return;

        GameObject go;
        if (parent == null) go = Instantiate(spawnOnHit, position, rotation);
        else go = Instantiate(spawnOnHit, position, rotation, parent);

        // maak 50% kleiner
        go.transform.localScale = go.transform.localScale * 0.75f;

        // als normale is meegegeven, forceer rotatie zodat quad op het oppervlak ligt
        if (normal != Vector3.zero)
        {
            // veronderstel dat prefab's forward (+Z) de 'front' van de splat is.
            // We willen dat de quad vlak op het oppervlak ligt, dus forward moet -normal zijn.
            Vector3 up = (parent != null) ? parent.up : Vector3.up;
            Quaternion rot = Quaternion.LookRotation(-normal, up);

            // voeg een random z-rotatie toe
            float randomZ = Random.Range(0f, 360f);
            rot *= Quaternion.Euler(0f, 0f, randomZ);

            go.transform.rotation = rot;
        }

        // geef het gespawnte object en alle kinderen de tag "Splat" (negeer als tag niet bestaat)
        try
        {
            // include inactive children ook
            var transforms = go.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                t.gameObject.tag = "Splat";
            }
        }
        catch (UnityException)
        {
            Debug.LogWarning("[colliding 01] Tag 'Splat' niet gedefinieerd. Maak deze aan via Edit → Project Settings → Tags and Layers.");
        }
        // vernietig het gespawnte object na 30 seconden om op te ruimen
        Destroy(go, 30f);
    }
}
