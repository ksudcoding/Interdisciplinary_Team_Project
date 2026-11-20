using System;
using System.Collections.Generic;
using UnityEngine;

public class CardDeckController : MonoBehaviour
{
    public enum CardCategory
    {
        Potential,
        Action
    }

    [Serializable]
    public struct DeckCardDefinition
    {
        public string cardName;
        public CardCategory category;
        public int count;

        public DeckCardDefinition(string name, CardCategory cat, int qty)
        {
            cardName = name;
            category = cat;
            count = qty;
        }
    }

    [Header("Deck Configuration")]
    public bool buildAndShuffleOnAwake = true;
    public bool autoReshuffleWhenEmpty = true;

    [Header("Runtime Piles (Read-Only View)")]
    [SerializeField] private List<string> drawPile = new List<string>();
    [SerializeField] private List<string> playedOrDiscardPile = new List<string>();

    public int DrawCount => drawPile.Count;
    public int PlayedCount => playedOrDiscardPile.Count;
    public int TotalConfiguredCards { get; private set; }

    public static readonly List<DeckCardDefinition> DefaultDeckRecipe = new List<DeckCardDefinition>
    {
        // Potential cards (122)
        new DeckCardDefinition("Propagation", CardCategory.Potential, 24),
        new DeckCardDefinition("Strong stimulus", CardCategory.Potential, 12),
        new DeckCardDefinition("Na+ open", CardCategory.Potential, 18),
        new DeckCardDefinition("Na+ closed", CardCategory.Potential, 14),
        new DeckCardDefinition("Na+ inactive", CardCategory.Potential, 14),
        new DeckCardDefinition("K+ open", CardCategory.Potential, 26),
        new DeckCardDefinition("K+ closed", CardCategory.Potential, 14),

        // Action cards (61)
        new DeckCardDefinition("Tetrodotoxin", CardCategory.Action, 6),
        new DeckCardDefinition("Caffeine", CardCategory.Action, 8),
        new DeckCardDefinition("Alcohol", CardCategory.Action, 8),
        new DeckCardDefinition("Large axon", CardCategory.Action, 9),
        new DeckCardDefinition("Narrow axon", CardCategory.Action, 4),
        new DeckCardDefinition("Myelinated axon", CardCategory.Action, 9),
        new DeckCardDefinition("Demyelinated axon", CardCategory.Action, 4),
        new DeckCardDefinition("Weak stimulus", CardCategory.Action, 6),
        new DeckCardDefinition("Absolute refractory period", CardCategory.Action, 4),
        new DeckCardDefinition("Relative refractory period", CardCategory.Action, 4)
    };

    private System.Random rng = new System.Random();

    void Awake()
    {
        if (buildAndShuffleOnAwake)
        {
            RebuildAndShuffleFullDeck();
        }
    }

    public void RebuildAndShuffleFullDeck()
    {
        drawPile.Clear();
        playedOrDiscardPile.Clear();

        int total = 0;
        for (int i = 0; i < DefaultDeckRecipe.Count; i++)
        {
            DeckCardDefinition def = DefaultDeckRecipe[i];
            int qty = Mathf.Max(0, def.count);
            total += qty;

            for (int c = 0; c < qty; c++)
            {
                drawPile.Add(def.cardName);
            }
        }

        TotalConfiguredCards = total;
        ShuffleList(drawPile);
        Debug.Log($"[CardDeckController] Built and shuffled full deck with {drawPile.Count} cards.");
    }

    public string DrawCard()
    {
        if (drawPile.Count == 0)
        {
            if (autoReshuffleWhenEmpty && playedOrDiscardPile.Count > 0)
            {
                ReshufflePlayedPile();
            }
            else
            {
                Debug.LogWarning("[CardDeckController] No cards left in draw pile or played pile to reshuffle.");
                return null;
            }
        }

        int lastIndex = drawPile.Count - 1;
        string drawnCard = drawPile[lastIndex];
        drawPile.RemoveAt(lastIndex);
        return drawnCard;
    }

    public void MarkCardPlayedOrDiscarded(string cardName)
    {
        if (string.IsNullOrWhiteSpace(cardName)) return;
        playedOrDiscardPile.Add(cardName);
    }

    public void ReshufflePlayedPile()
    {
        if (playedOrDiscardPile.Count == 0)
        {
            Debug.LogWarning("[CardDeckController] Played/discard pile is empty; nothing to reshuffle.");
            return;
        }

        drawPile.AddRange(playedOrDiscardPile);
        playedOrDiscardPile.Clear();
        ShuffleList(drawPile);
        Debug.Log($"[CardDeckController] Reshuffled {drawPile.Count} cards back into the draw pile.");
    }

    private void ShuffleList(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            string temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}