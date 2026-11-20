using UnityEngine;

public class CardHover : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverOffsetY = 0.35f;
    public float hoverScaleMultiplier = 1.15f;
    public float hoverSpeed = 15f;
    public Vector3 targetEulerRotation = new Vector3(-90f, 0f, -180f);

    [Header("Card Base Scale")]
    public Vector3 defaultCardScale = new Vector3(1f, 0.01f, 1f);

    [Header("Highlight / Outline Object")]
    public GameObject outlineChildObject;

    [Header("Audio Settings")]
    public AudioSource hoverAudioSource;

    private Vector3 targetSlotPosition;
    private Vector3 baseCardScale;
    private int baseSortingOrder;
    private Renderer[] cardRenderers;
    private bool isHovered = false;
    private CardSlotManager slotManager;
    private UICard uiCard;
    private GameCardManager gameCardManager;

    void Awake()
    {
        cardRenderers = GetComponentsInChildren<Renderer>();
        uiCard = GetComponent<UICard>() ?? GetComponentInChildren<UICard>();
        gameCardManager = FindAnyObjectByType<GameCardManager>();

        baseCardScale = SanitizeScale(transform.localScale, defaultCardScale);
        targetSlotPosition = transform.localPosition;

        if (hoverAudioSource == null)
            hoverAudioSource = GetComponent<AudioSource>();

        if (outlineChildObject != null)
            outlineChildObject.SetActive(false);
    }

    Vector3 SanitizeScale(Vector3 candidate, Vector3 fallback)
    {
        if (Mathf.Abs(candidate.x) < 0.0001f || Mathf.Abs(candidate.y) < 0.0001f || Mathf.Abs(candidate.z) < 0.0001f)
            return fallback;
        return candidate;
    }

    public void Initialize(CardSlotManager manager, Vector3 position, int sortingOrder, Vector3 passedScale)
    {
        slotManager = manager;
        if (gameCardManager == null) gameCardManager = FindAnyObjectByType<GameCardManager>();

        baseCardScale = SanitizeScale(passedScale, defaultCardScale);
        transform.localScale = baseCardScale;
        SetSlotPosition(position, sortingOrder);
    }

    public void SetSlotPosition(Vector3 newPosition, int sortingOrder)
    {
        targetSlotPosition = newPosition;
        baseSortingOrder = sortingOrder;

        if (!isHovered)
        {
            ApplySortingOrder(baseSortingOrder);
            transform.localPosition = targetSlotPosition;
            transform.localScale = baseCardScale;
        }
    }

    void Update()
    {
        Vector3 destinationPos = isHovered ? targetSlotPosition + new Vector3(0f, hoverOffsetY, 0f) : targetSlotPosition;
        Vector3 destinationScale = isHovered ? baseCardScale * hoverScaleMultiplier : baseCardScale;

        transform.localPosition = Vector3.Lerp(transform.localPosition, destinationPos, Time.deltaTime * hoverSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, destinationScale, Time.deltaTime * hoverSpeed);
        transform.localEulerAngles = targetEulerRotation;

        if (isHovered && Input.GetMouseButtonDown(1))
        {
            if (gameCardManager == null) gameCardManager = FindAnyObjectByType<GameCardManager>();
            if (gameCardManager != null && gameCardManager.discardController != null)
            {
                gameCardManager.discardController.RegisterRightClickForDiscardMode();
            }
        }
    }

    void OnMouseEnter()
    {
        isHovered = true;
        ApplySortingOrder(999);
        if (outlineChildObject != null) outlineChildObject.SetActive(true);
        if (hoverAudioSource != null && hoverAudioSource.clip != null) hoverAudioSource.Play();
    }

    void OnMouseExit()
    {
        isHovered = false;
        ApplySortingOrder(baseSortingOrder);
        if (outlineChildObject != null) outlineChildObject.SetActive(false);
        transform.localScale = baseCardScale;
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(1))
            return;

        if (uiCard == null) uiCard = GetComponent<UICard>() ?? GetComponentInChildren<UICard>();
        if (uiCard != null)
        {
            uiCard.TryActivateCard();
        }
    }

    void ApplySortingOrder(int order)
    {
        if (cardRenderers == null) return;
        foreach (Renderer rend in cardRenderers)
        {
            if (rend != null) rend.sortingOrder = order;
        }
    }
}