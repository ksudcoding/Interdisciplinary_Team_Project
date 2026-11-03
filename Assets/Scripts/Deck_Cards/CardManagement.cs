using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class CardPileManager : MonoBehaviour
{
    [Header("3D Pile Settings")]
    [Tooltip("Drag your 3D card prefab here (used for the pile).")]
    public GameObject pileCardPrefab;
    
    [Header("Hand / UI Card Settings")]
    [Tooltip("Drag your separate UI/Hand card prefab here.")]
    public GameObject uiCardPrefab;

    [Header("Pile Layout Settings")]
    public int spawnCount = 5; 
    public float verticalSpacing = 0.3f;

    [Header("Hand / Slot References")]
    [Tooltip("Drag PlayerHand (where the card flies to initially) here.")]
    public Transform playerHand;
    [Tooltip("Drag CardSlot (which has the CardSlotManager script attached) here.")]
    public CardSlotManager cardSlotManager;

    [Header("Animation Settings")]
    public float moveDuration = 0.25f;

    private List<GameObject> pileItems = new List<GameObject>();

    void Start()
    {
        SpawnPile();
    }

    void SpawnPile()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = transform.position + new Vector3(0f, i * verticalSpacing, 0f);
            GameObject card = Instantiate(pileCardPrefab, spawnPos, Quaternion.identity);
            
            if (card.GetComponent<Collider>() == null)
            {
                card.AddComponent<BoxCollider>();
            }

            MakeCardUnlit(card);
            pileItems.Add(card);
        }
    }

    void MakeCardUnlit(GameObject card)
    {
        // Makes any card completely unaffected by scene lighting
        Renderer[] renderers = card.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                Shader unlitShader = Shader.Find("Unlit/Texture");
                if (unlitShader != null && mat.HasProperty("_MainTex") && mat.mainTexture != null)
                {
                    Texture tex = mat.mainTexture;
                    mat.shader = unlitShader;
                    mat.mainTexture = tex;
                }
                else
                {
                    Shader unlitColorShader = Shader.Find("Unlit/Color");
                    if (unlitColorShader != null)
                    {
                        Color col = mat.HasProperty("_Color") ? mat.color : Color.white;
                        mat.shader = unlitColorShader;
                        if (mat.HasProperty("_Color")) mat.color = col;
                    }
                }
            }
        }
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (pileItems.Count > 0)
                {
                    GameObject topCard = pileItems[pileItems.Count - 1];

                    if (hit.collider.gameObject == topCard)
                    {
                        DrawTopCard();
                    }
                }
            }
        }
    }

    void DrawTopCard()
    {
        GameObject drawnCard = pileItems[pileItems.Count - 1];
        pileItems.RemoveAt(pileItems.Count - 1);

        Collider col = drawnCard.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (playerHand != null && cardSlotManager != null)
        {
            StartCoroutine(SmoothMoveToHand(drawnCard, playerHand));
        }
        else
        {
            Debug.LogWarning("PlayerHand or CardSlotManager is missing references in the Inspector!");
            Destroy(drawnCard);
        }

        Debug.Log($"Card drawn! Cards remaining in pile: {pileItems.Count}");
    }

    IEnumerator SmoothMoveToHand(GameObject card, Transform flightTarget)
    {
        float elapsed = 0f;
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);

            if (card == null) yield break;

            card.transform.position = Vector3.Lerp(startPos, flightTarget.position, t);
            card.transform.rotation = Quaternion.Slerp(startRot, flightTarget.rotation, t);

            yield return null;
        }

        // Flight complete: destroy the flying pile card
        if (card != null)
        {
            Destroy(card);
        }

        // Instantiate the UICard in the hand slot and force it to be completely unlit
        if (cardSlotManager != null && uiCardPrefab != null)
        {
            GameObject newUICard = Instantiate(uiCardPrefab);
            MakeCardUnlit(newUICard); // <-- This ensures scene lights ignore the UICard!
            cardSlotManager.AddCardToHand(newUICard);
        }
        else if (cardSlotManager != null)
        {
            Debug.LogWarning("UICard Prefab slot is empty in CardPileManager inspector!");
        }
    }
}