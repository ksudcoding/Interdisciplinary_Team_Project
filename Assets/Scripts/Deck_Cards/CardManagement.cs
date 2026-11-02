using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class CardPileManager : MonoBehaviour
{
    [Header("3D Prefab Settings")]
    [Tooltip("Drag your 3D card prefab here.")]
    public GameObject prefabToSpawn;
    
    [Header("Pile Settings")]
    public int spawnCount = 5; 
    public float verticalSpacing = 0.3f;

    [Header("Player Hand Settings")]
    [Tooltip("Drag PlayerHand (child of Main Camera) here.")]
    public Transform playerHand;
    public float moveDuration = 0.25f;

    [Header("UI Hand Settings")]
    [Tooltip("(Optional) Drag your existing Canvas here, or leave blank to auto-generate.")]
    public Canvas targetCanvas;
    [Tooltip("(Optional) Drag your custom UI Card prefab here (must NOT have a Canvas component on it!). Leave blank for default white card.")]
    public GameObject uiCardPrefab;

    [Header("UI Alignment Settings")]
    [Tooltip("How cards align inside the bottom hand panel (e.g., LowerCenter, LowerLeft, LowerRight).")]
    public TextAnchor handAlignment = TextAnchor.LowerCenter;

    private Transform uiCardHandPanel;
    private List<GameObject> pileItems = new List<GameObject>();

    void Start()
    {
        SetupUIHand();
        SpawnPile();
    }

    void SetupUIHand()
    {
        // If no canvas was assigned, look for one in the scene or create one automatically
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
            if (targetCanvas == null)
            {
                GameObject canvasObj = new GameObject("RuntimeCanvas");
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        // Create a fixed-size hand panel locked dead-center at the bottom of the screen
        Transform existingPanel = targetCanvas.transform.Find("PlayerCardHand");
        if (existingPanel != null)
        {
            uiCardHandPanel = existingPanel;
        }
        else
        {
            GameObject panelObj = new GameObject("PlayerCardHand");
            panelObj.transform.SetParent(targetCanvas.transform, false);
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 20f); 
            panelRect.sizeDelta = new Vector2(700f, 160f); 

            HorizontalLayoutGroup layoutGroup = panelObj.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 15f;
            layoutGroup.childAlignment = handAlignment; 

            uiCardHandPanel = panelObj.transform;
        }
    }

    void SpawnPile()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = transform.position + new Vector3(0f, i * verticalSpacing, 0f);
            GameObject card = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            
            if (card.GetComponent<Collider>() == null)
            {
                card.AddComponent<BoxCollider>();
            }

            pileItems.Add(card);
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

        if (playerHand != null)
        {
            StartCoroutine(SmoothMoveToHand(drawnCard, playerHand));
        }
        else
        {
            Destroy(drawnCard);
            SpawnUICard();
        }

        Debug.Log($"Card drawn! Cards remaining in pile: {pileItems.Count}");
    }

    IEnumerator SmoothMoveToHand(GameObject card, Transform targetHand)
    {
        float elapsed = 0f;
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);

            if (card == null) yield break;

            card.transform.position = Vector3.Lerp(startPos, targetHand.position, t);
            card.transform.rotation = Quaternion.Slerp(startRot, targetHand.rotation, t);

            yield return null;
        }

        if (card != null)
        {
            Destroy(card);
        }

        SpawnUICard();
    }

    void SpawnUICard()
    {
        if (uiCardHandPanel == null) return;

        GameObject newUICard;

        if (uiCardPrefab != null)
        {
            // PASSING 'false' HERE FOR WORLD POSITION STAYS FIXES THE OFFSET BUG!
            newUICard = Instantiate(uiCardPrefab, uiCardHandPanel, false);
        }
        else
        {
            newUICard = new GameObject("UICard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            newUICard.transform.SetParent(uiCardHandPanel, false);
            
            RectTransform rt = newUICard.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 150f);

            Image img = newUICard.GetComponent<Image>();
            img.color = Color.white;
        }

        // Force proper layout rect transforms
        RectTransform cardRect = newUICard.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.localScale = Vector3.one;
            cardRect.anchoredPosition3D = Vector3.zero;
        }

        Debug.Log("UI Card successfully added into bottom center layout!");
    }
}