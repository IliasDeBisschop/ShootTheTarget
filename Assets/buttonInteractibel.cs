using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class buttonInteractibel : MonoBehaviour
{
    [Tooltip("Waarvandaan de raycast wordt gestart (bijv. controller of camera). Als leeg wordt deze transform gebruikt.)")]
    public Transform rayOrigin;
    public float maxDistance = 10f;
    public LayerMask hitLayers = ~0;
    [SerializeField] private Text scoreText;   
    [SerializeField] private string scoreObjectName = "score Text"; // naam van het UI-object  

    // --- toegevoegd voor zichtbare ray ---
    public bool useLineRenderer = false;        // Als true: zichtbaar in Game-view via LineRenderer
    public bool showDebugRay = true;            // Debug.DrawRay (Scene view)
    public Color rayColor = Color.green;
    public float showDuration = 0.2f;           // Hoe lang de ray zichtbaar blijft
    private LineRenderer lineRenderer;
    private Coroutine hideCoroutine;
    // --- einde toegevoegd ---

    // Camera die gebruikt wordt voor UI raycasts (optioneel). Als null -> Camera.main
    public Camera uiCamera;
    private TextMeshProUGUI scoreTMP;

    [Tooltip("OVR trigger knop die gebruikt wordt (kan in de Inspector aangepast worden)")]
    public OVRInput.Button ovrTriggerButton = OVRInput.Button.PrimaryIndexTrigger;

    void Start()
    {
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

        if (useLineRenderer)
            EnsureLineRenderer();
    }

    void Update()
    {
        if (TriggerPressed())
        {
            DoRaycast();
        }
    }

    bool TriggerPressed()
    {
        // Oculus OVR input (kan in Inspector aangepast worden)
        return OVRInput.GetDown(ovrTriggerButton) || OVRInput.Get(ovrTriggerButton);
    }

    void DoRaycast()
    {
        Transform origin = rayOrigin != null ? rayOrigin : transform;
        Ray ray = new Ray(origin.position, origin.forward);

        // Debug.DrawRay (Scene view). Blijft zichtbaar voor showDuration seconden.
        if (showDebugRay)
            Debug.DrawRay(origin.position, origin.forward * maxDistance, rayColor, showDuration);

        Vector3 lineEnd = origin.position + origin.forward * maxDistance;

        // Eerst proberen UI (GraphicRaycaster). Zo krijgen UI elementen prioriteit boven physics-objecten.
        if (RaycastUI(ray, out GameObject uiHit, out Vector3 uiHitPoint))
        {
            lineEnd = uiHitPoint;

            // Vind Button op het hit-object of in een van de parents en trigger onClick
            Button hitButton = uiHit.GetComponentInParent<Button>();
            if (hitButton != null)
            {
                buttonClicked(hitButton);                

            }
            else
            {
                Debug.Log("05 UI hit (geen Button component gevonden): " + uiHit.name);
            }
        }

        // Toon altijd de lijn, tot aan het UI hit-punt of maxDistance
        if (useLineRenderer)
            ShowLine(origin.position, lineEnd);
    }

    // Zorg dat er een LineRenderer beschikbaar is
    void EnsureLineRenderer()
    {
        if (lineRenderer != null) return;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.005f;
            lineRenderer.endWidth = 0.005f;
            lineRenderer.useWorldSpace = true;
            // Eenvoudig onbelicht materiaal zodat kleur altijd zichtbaar is
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = rayColor;
            lineRenderer.material = mat;
        }

        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = rayColor;
        lineRenderer.enabled = false;
    }

    void ShowLine(Vector3 from, Vector3 to)
    {
        EnsureLineRenderer();
        if (lineRenderer == null) return;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);
        lineRenderer.enabled = true;
        hideCoroutine = StartCoroutine(HideLineAfterSeconds(showDuration));
    }

    IEnumerator HideLineAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (lineRenderer != null)
            lineRenderer.enabled = false;
        hideCoroutine = null;
    }

    // Raycast naar UI elementen via alle GraphicRaycasters in de scene.
    // Retourneert true als er een UI element geraakt is; geeft het GameObject en een wereld-positie terug.
    bool RaycastUI(Ray ray, out GameObject hitGO, out Vector3 hitWorldPoint)
    {
        hitGO = null;
        hitWorldPoint = ray.origin + ray.direction * maxDistance;

        if (EventSystem.current == null)
            return false;

        Camera cam = uiCamera != null ? uiCamera : Camera.main;
        if (cam == null)
            return false;

        // PointerEventData heeft een schermpositie — projecteer een punt op de ray naar schermruimte.
        Vector3 samplePointWorld = ray.origin + ray.direction * (maxDistance * 0.5f);
        Vector2 screenPos = cam.WorldToScreenPoint(samplePointWorld);

        PointerEventData ped = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        GraphicRaycaster[] raycasters = FindObjectsOfType<GraphicRaycaster>();
        foreach (var gr in raycasters)
        {
            results.Clear();
            gr.Raycast(ped, results);
            if (results != null && results.Count > 0)
            {
                // Kies de eerste hit (bovenaan)
                var rr = results[0];
                hitGO = rr.gameObject;

                // Probeer wereld-coördinaat te bepalen:
                // Als RaycastResult.worldPosition is ingevuld, gebruik dat; anders probeer snijpunt met RectTransform-plane.
                if (rr.worldPosition != Vector3.zero)
                {
                    hitWorldPoint = rr.worldPosition;
                }
                else
                {
                    var rt = hitGO.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Plane p = new Plane(rt.forward, rt.position);
                        if (p.Raycast(ray, out float enter))
                            hitWorldPoint = ray.GetPoint(enter);
                    }
                }

                // Gebruik ExecuteHierarchy zodat parent handlers (bv. Button op parent) ook worden aangeroepen
                ExecuteEvents.ExecuteHierarchy(hitGO, ped, ExecuteEvents.pointerClickHandler);

                return true;
            }
        }

        return false;
    }

    void buttonClicked(Button hitButton)
    {
        Debug.Log("05 Button clicked: " + hitButton.gameObject.name);
        if (hitButton.gameObject.name == "Score Button")
        {
            string display = "Score: 0";
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
        }
        else if (hitButton.gameObject.name == "Reset Button")
        {
            // Herlaad de huidige scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}