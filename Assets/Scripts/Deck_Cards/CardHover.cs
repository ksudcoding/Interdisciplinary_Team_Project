using UnityEngine;
using System.Collections.Generic;

public class CardHover : MonoBehaviour
{
    [Header("Hover Settings")]
    [Tooltip("How high the card moves up vertically when hovered.")]
    public float hoverOffsetY = 0.35f;
    [Tooltip("How fast the card animates up and down.")]
    public float hoverSpeed = 15f;
    [Tooltip("The correct facing rotation to maintain while hovering.")]
    public Vector3 targetEulerRotation = new Vector3(-90f, 180f, 0f);

    [Header("Highlight / Outline Settings")]
    [Tooltip("Editable color tint applied to the card when hovered (acts as an outline/highlight).")]
    public Color outlineColor = Color.yellow;

    private Color[] originalColors;
    private Renderer[] cardRenderers;
    private Vector3 targetSlotPosition;
    private int baseSortingOrder;
    private bool isHovered = false;
    private CardSlotManager slotManager;

    void Awake()
    {
        cardRenderers = GetComponentsInChildren<Renderer>();
        
        // Save original material colors to revert when unhovered
        List<Color> colorList = new List<Color>();
        foreach (Renderer rend in cardRenderers)
        {
            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty("_Color"))
                    colorList.Add(mat.color);
                else
                    colorList.Add(Color.white);
            }
        }
        originalColors = colorList.ToArray();
    }

    public void Initialize(CardSlotManager manager, Vector3 position, int sortingOrder)
    {
        slotManager = manager;
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
        }
    }

    void Update()
    {
        Vector3 destination = isHovered ? targetSlotPosition + new Vector3(0f, hoverOffsetY, 0f) : targetSlotPosition;

        transform.localPosition = Vector3.Lerp(transform.localPosition, destination, Time.deltaTime * hoverSpeed);
        transform.localEulerAngles = targetEulerRotation;
    }

    void OnMouseEnter()
    {
        isHovered = true;
        ApplySortingOrder(999); // Bring to absolute front when hovered
        ApplyHighlight(true);
    }

    void OnMouseExit()
    {
        isHovered = false;
        ApplySortingOrder(baseSortingOrder);
        ApplyHighlight(false);
    }

    void OnMouseDown()
    {
        // When clicked, tell the Slot Manager to play this card and remove it from hand
        if (slotManager != null)
        {
            slotManager.PlayCard(gameObject);
        }
    }

    void ApplyHighlight(bool highlight)
    {
        int colorIdx = 0;
        foreach (Renderer rend in cardRenderers)
        {
            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    mat.color = highlight ? outlineColor : originalColors[colorIdx];
                }
                colorIdx++;
            }
        }
    }

    void ApplySortingOrder(int order)
    {
        if (cardRenderers != null)
        {
            foreach (Renderer rend in cardRenderers)
            {
                rend.sortingOrder = order;
            }
        }
    }
}