using UnityEngine;
using System.Collections.Generic;

public class CardSlotManager : MonoBehaviour
{
    [Header("Array / Layout Settings")]
    [Tooltip("The horizontal distance between each card in the hand.")]
    public float cardSpacing = 1.2f;

    [Tooltip("If true, the array of cards centers itself as you draw more. If false, it builds strictly from left to right.")]
    public bool centerHand = true;

    [Header("Card Transform Settings")]
    [Tooltip("The exact scale stats applied to every card in the hand.")]
    public Vector3 cardScale = new Vector3(2.569168f, 0.03f, 4f);
    [Tooltip("The rotation applied to cards in the hand.")]
    public Vector3 cardEulerRotation = new Vector3(90f, 90f, 270f);

    private List<GameObject> handCards = new List<GameObject>();
    private GameCardManager gameCardManager;

    void Start()
    {
        // Automatically locate the GameCardManager in the scene
        gameCardManager = FindObjectOfType<GameCardManager>();
        if (gameCardManager == null)
        {
            Debug.LogWarning("[CardSlotManager] GameCardManager not found in the scene!");
        }
    }

    public void AddCardToHand(GameObject card)
    {
        if (card == null) return;

        card.transform.SetParent(transform);

        // Ensure the card has a CardHover component attached
        CardHover hoverScript = card.GetComponent<CardHover>();
        if (hoverScript == null)
        {
            hoverScript = card.AddComponent<CardHover>();
        }

        handCards.Add(card);
        UpdateHandLayout();
    }

    /// <summary>
    /// Triggered when a card is clicked. Executes card action, removes from array, and updates hand.
    /// </summary>
    public void PlayCard(GameObject card)
    {
        if (handCards.Contains(card))
        {
            // 1. Get the card's name from its CardDisplay component and process effect
            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display != null && gameCardManager != null)
            {
                Debug.Log($"[Card System] Playing card: {display.cardName}. Executing card action!");
                gameCardManager.ProcessCardPlay(display.cardName);
            }
            else
            {
                Debug.Log($"[Card System] Playing card: {card.name}. Executing card action!");
            }

            // Remove from tracking array and destroy the card object
            handCards.Remove(card);
            Destroy(card);

            // Re-index and slide remaining cards in the hand array
            UpdateHandLayout();
        }
    }

    public void RemoveCardFromHand(GameObject card)
    {
        if (handCards.Contains(card))
        {
            handCards.Remove(card);
            UpdateHandLayout();
        }
    }
    // Add this method inside your CardSlotManager class:
    public void AddCardToHandWithAnimation(GameObject cardInstance)
    {
        // Call your existing method to slot it into hand layout
        AddCardToHand(cardInstance);

        // Optional: Add a smooth lerp/flight coroutine from center deck to hand position if desired,
        // or rely on standard layout group transition. Instantiating at the deck marker already handles the start position!
    }
    private void UpdateHandLayout()
    {
        int totalCards = handCards.Count;

        for (int i = 0; i < totalCards; i++)
        {
            if (handCards[i] != null)
            {
                float xOffset = 0f;

                if (centerHand)
                {
                    xOffset = (i - (totalCards - 1) / 2f) * cardSpacing;
                }
                else
                {
                    xOffset = i * cardSpacing;
                }

                Vector3 slotPos = new Vector3(xOffset, 0f, 0f);
                int baseSortingOrder = i * 10; // Spaced out sorting layers to prevent mesh fighting

                CardHover hoverScript = handCards[i].GetComponent<CardHover>();
                if (hoverScript != null)
                {
                    hoverScript.Initialize(this, slotPos, baseSortingOrder, cardScale);
                }
                else
                {
                    handCards[i].transform.localPosition = slotPos;
                }

                handCards[i].transform.localRotation = Quaternion.Euler(cardEulerRotation);
                handCards[i].transform.localScale = cardScale;
            }
        }
    }
}