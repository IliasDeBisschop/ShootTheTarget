using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class buttonVi : MonoBehaviour
{
    // knop die je wilt gebruiken om te togglen (standaard: One)
    public OVRInput.Button toggleButton = OVRInput.Button.One;

    // houd bij of het object zichtbaar is
    private bool isVisible = false;

    // renderers en canvassen in children (inclusief inactive)
    private Renderer[] renderers;
    private Canvas[] canvases;

    void Start()
    {
        // verzamel alle renderer en canvas componenten in children zodat we zichtbaarheid kunnen togglen
        renderers = GetComponentsInChildren<Renderer>(true);
        canvases = GetComponentsInChildren<Canvas>(true);
        SetVisibility(isVisible);
    }

    void Update()
    {
        // wanneer de ingestelde knop net is ingedrukt, togglen we zichtbaarheid
        if (OVRInput.GetDown(toggleButton))
        {
            isVisible = !isVisible;
            SetVisibility(isVisible);
        }
    }

    private void SetVisibility(bool visible)
    {
        // zet alle renderers aan/uit
        if (renderers != null)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = visible;
        }

        // zet alle canvases aan/uit (UI)
        if (canvases != null)
        {
            foreach (var c in canvases)
                if (c != null) c.enabled = visible;
        }
    }
}
