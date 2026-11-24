using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameCardManager : MonoBehaviour
{
    [Header("Turn & Phase Management")]
    public TurnState currentTurn = TurnState.PlayerTurn;
    public TurnPhase currentPhase = TurnPhase.PlayPhase;
    public int turnCounter = 1;
    public TextMeshPro turnDisplayText;

    [Header("Input Blocking")]
    public GameObject playStopper;

    [Header("Sub-Controllers & References")]
    public CardDeckAndRules deckRules;
    public CardDiscardController discardController;
    public Position playerPositionScript;
    public CardSlotManager cardSlotManager;
    public BotController botController;

    [Header("Spawn Markers & Hand References")]
    public Transform deckSpawnMarker;
    public Transform playerHandHolder;

    [Header("Prefabs & Fallbacks")]
    public GameObject defaultUICardPrefab;
    public GameObject flyingCardFallbackPrefab;
    public List<CardPrefabEntry> specificCardPrefabs = new List<CardPrefabEntry>();

    [Header("Hand Limits (Rules)")]
    public int maxPotentialCards = 4;
    public int maxActionCards = 2;

    [Header("Deck Counts & Reshuffle")]
    public bool applyExactTableCountsOnStart = true;
    public bool autoReshuffleDiscardsWhenEmpty = true;

    [Header("Player State")]
    public List<GameCard> playerPotentialHand = new List<GameCard>();
    public List<GameCard> playerActionHand = new List<GameCard>();
    public bool playerSkipNextTurn = false;
    public bool playerLockedFromForwardMoveThisTurn = false;
    public int playerTetrodotoxinCount = 0;

    private bool isEndingTurn = false;
    private bool isPlayerPieceMoving = false;
    private Coroutine moveWaitCoroutine;
    private readonly System.Random rng = new System.Random();
    private int nextGeneratedCardId = 1;

    public int MaxTotalHandCards => Mathf.Max(0, maxPotentialCards) + Mathf.Max(0, maxActionCards);

    private static readonly (string name, GameCardType type, int count)[] ExactDeckCounts =
    {
        ("Propagation", GameCardType.Potential, 1),
        ("Strong stimulus", GameCardType.Potential, 1),
        ("Na+ open", GameCardType.Potential, 1),
        ("Na+ Channel Open Peach", GameCardType.Potential, 1),
        ("Na+ closed", GameCardType.Potential, 1),
        ("Na+ Channel Closed Blue", GameCardType.Potential, 1),
        ("Na+ inactive", GameCardType.Potential, 1),
        ("K+ open", GameCardType.Potential, 1),
        ("K+ Channel Open Magenta", GameCardType.Potential, 1),
        ("K+ closed", GameCardType.Potential, 1),
        ("K+ Channel Closed", GameCardType.Potential, 1),

        ("Weak stimulus", GameCardType.Action, 1),
        ("Caffeine", GameCardType.Action, 1),
        ("Alcohol", GameCardType.Action, 1),
        ("Tetrodotoxin", GameCardType.Action, 1),
        ("Absolute refractory period", GameCardType.Action, 1),
        ("Absolute Refractory Pink", GameCardType.Action, 1),
        ("Relative refractory period", GameCardType.Action, 1),
        ("Myelinated axon", GameCardType.Action, 1),
        ("Yellow Myelinated Axon", GameCardType.Action, 1),
        ("Demyelinated axon", GameCardType.Action, 1),
        ("Yellow Demyelinated Axon", GameCardType.Action, 1),
        ("Large axon", GameCardType.Action, 1),
        ("Yellow Large Axon", GameCardType.Action, 1),
        ("Narrow axon", GameCardType.Action, 1),
        ("Yellow Narrow Axon", GameCardType.Action, 1)
    };

    void Awake()
    {
        if (playerPotentialHand == null) playerPotentialHand = new List<GameCard>();
        if (playerActionHand == null) playerActionHand = new List<GameCard>();
        if (specificCardPrefabs == null) specificCardPrefabs = new List<CardPrefabEntry>();

        if (deckRules == null) deckRules = GetComponent<CardDeckAndRules>() ?? gameObject.AddComponent<CardDeckAndRules>();
        if (discardController == null) discardController = GetComponent<CardDiscardController>() ?? gameObject.AddComponent<CardDiscardController>();

        discardController.cardManager = this;
        discardController.deckRules = deckRules;
        discardController.playerHandHolder = playerHandHolder;
    }

    void Start()
    {
        SetPlayStopperActive(true);
        if (deckRules != null)
        {
            deckRules.InitializeAndShuffleDecks();
            if (applyExactTableCountsOnStart)
            {
                BuildExactTableDecksAndShuffle();
            }
        }
        StartCoroutine(SetupGameRoutine());
    }

    [ContextMenu("Build Exact Table Decks (11 Potential / 15 Action) & Shuffle")]
    public void BuildExactTableDecksAndShuffle()
    {
        if (deckRules == null) return;

        if (deckRules.potentialDrawDeck == null) deckRules.potentialDrawDeck = new List<GameCard>();
        if (deckRules.actionDrawDeck == null) deckRules.actionDrawDeck = new List<GameCard>();
        if (deckRules.potentialDiscardPile == null) deckRules.potentialDiscardPile = new List<GameCard>();
        if (deckRules.actionDiscardPile == null) deckRules.actionDiscardPile = new List<GameCard>();

        deckRules.potentialDrawDeck.Clear();
        deckRules.actionDrawDeck.Clear();
        deckRules.potentialDiscardPile.Clear();
        deckRules.actionDiscardPile.Clear();
        nextGeneratedCardId = 1;

        foreach (var entry in ExactDeckCounts)
        {
            int qty = Mathf.Max(0, entry.count);
            for (int i = 0; i < qty; i++)
            {
                GameCard card = new GameCard
                {
                    card_Id = $"{(entry.type == GameCardType.Potential ? "POT" : "ACT")}_{nextGeneratedCardId++:000}",
                    cardName = entry.name,
                    type = entry.type,
                    drawnOnTurn = -1
                };

                if (entry.type == GameCardType.Potential)
                    deckRules.potentialDrawDeck.Add(card);
                else
                    deckRules.actionDrawDeck.Add(card);
            }
        }

        ShuffleCards(deckRules.potentialDrawDeck);
        ShuffleCards(deckRules.actionDrawDeck);
    }

    public GameCard DrawCardWithAutoReshuffle(GameCardType cardType)
    {
        if (deckRules == null) return null;

        List<GameCard> drawPile = cardType == GameCardType.Potential
            ? deckRules.potentialDrawDeck
            : deckRules.actionDrawDeck;

        List<GameCard> discardPile = cardType == GameCardType.Potential
            ? deckRules.potentialDiscardPile
            : deckRules.actionDiscardPile;

        if (drawPile == null)
        {
            drawPile = new List<GameCard>();
            if (cardType == GameCardType.Potential) deckRules.potentialDrawDeck = drawPile;
            else deckRules.actionDrawDeck = drawPile;
        }

        if (drawPile.Count == 0 && autoReshuffleDiscardsWhenEmpty && discardPile != null && discardPile.Count > 0)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            ShuffleCards(drawPile);
        }

        if (drawPile.Count == 0)
        {
            return deckRules.DrawFromDeck(cardType);
        }

        int lastIdx = drawPile.Count - 1;
        GameCard drawn = drawPile[lastIdx];
        drawPile.RemoveAt(lastIdx);
        return drawn;
    }

    private void ShuffleCards(List<GameCard> list)
    {
        if (list == null) return;
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            GameCard temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private static bool IsStartingCardName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name.Equals("Weak stimulus", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Strong stimulus", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Potential Joker", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesRequiredPotentialCard(string playedName, string requiredName)
    {
        if (string.IsNullOrWhiteSpace(playedName) || string.IsNullOrWhiteSpace(requiredName))
            return false;

        if (playedName.Equals(requiredName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (requiredName.Equals("Na+ Channel OPEN", StringComparison.OrdinalIgnoreCase))
        {
            return playedName.Equals("Na+ open", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("Na+ Channel Open Pink", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("Na+ Channel Open Peach", StringComparison.OrdinalIgnoreCase);
        }

        if (requiredName.Equals("Na+ Channel OPEN AND K+ Channel CLOSED", StringComparison.OrdinalIgnoreCase))
        {
            return playedName.Equals("Na+ open", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("Na+ Channel Open Pink", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("Na+ Channel Open Peach", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("K+ closed", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("K+ Channel Closed", StringComparison.OrdinalIgnoreCase);
        }

        if (requiredName.Equals("Na+ Channel INACTIVE AND K+ Channel OPEN", StringComparison.OrdinalIgnoreCase))
        {
            return playedName.Equals("Na+ inactive", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("Na+ Channel Inactive Magenta", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("K+ open", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("K+ Channel Open Magenta", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("K+ Channel Open Blue", StringComparison.OrdinalIgnoreCase);
        }

        if (requiredName.Equals("Na+ Channel CLOSED AND K+ Channel OPEN", StringComparison.OrdinalIgnoreCase))
        {
            return playedName.Equals("Na+ closed", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("Na+ Channel Closed Blue", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("K+ open", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("K+ Channel Open Magenta", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("K+ Channel Open Blue", StringComparison.OrdinalIgnoreCase);
        }

        if (requiredName.Equals("Na+ Channel CLOSED AND K+ Channel CLOSED", StringComparison.OrdinalIgnoreCase))
        {
            return playedName.Equals("Na+ closed", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("Na+ Channel Closed Blue", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("K+ closed", StringComparison.OrdinalIgnoreCase) ||
                   playedName.Equals("K+ Channel Closed", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    void Update()
    {
        if (playerSkipNextTurn && discardController != null && discardController.isDiscardMode)
            discardController.ForceExitDiscardMode("Turn skipped");

        if (isPlayerPieceMoving && playerPositionScript != null &&
            playerPositionScript.currentIndex == playerPositionScript.targetIndex &&
            moveWaitCoroutine == null)
        {
            isPlayerPieceMoving = false;
            RefreshPlayStopper();
        }
    }

    public bool CanPlayerInteractNow()
    {
        bool moving = isPlayerPieceMoving ||
                      (playerPositionScript != null && playerPositionScript.currentIndex != playerPositionScript.targetIndex);

        return currentTurn == TurnState.PlayerTurn &&
               !isEndingTurn &&
               !moving &&
               !playerSkipNextTurn &&
               currentPhase != TurnPhase.RefillPhase;
    }

    public void SetPhaseFromDiscardMode(bool discardActive)
    {
        if (playerSkipNextTurn) discardActive = false;
        currentPhase = discardActive ? TurnPhase.DiscardPhase : TurnPhase.PlayPhase;
        RefreshPlayStopper();
        UpdateTurnUI();
    }

    public void SetPlayStopperActive(bool active)
    {
        if (playStopper != null && playStopper.activeSelf != active)
            playStopper.SetActive(active);
    }

    void RefreshPlayStopper()
    {
        SetPlayStopperActive(!CanPlayerInteractNow());
    }

    GameCard FindCardInHands(string cardName)
    {
        if (string.IsNullOrEmpty(cardName)) return null;
        GameCard pot = playerPotentialHand != null ? playerPotentialHand.Find(c => c != null && c.cardName == cardName) : null;
        if (pot != null) return pot;
        return playerActionHand != null ? playerActionHand.Find(c => c != null && c.cardName == cardName) : null;
    }

    public void EnforceHandLimitsAndSyncVisuals()
    {
        int potCap = Mathf.Max(0, maxPotentialCards);
        int actCap = Mathf.Max(0, maxActionCards);

        playerPotentialHand.RemoveAll(c => c == null);
        playerActionHand.RemoveAll(c => c == null);

        while (playerPotentialHand.Count > potCap)
        {
            int lastIdx = playerPotentialHand.Count - 1;
            GameCard extra = playerPotentialHand[lastIdx];
            playerPotentialHand.RemoveAt(lastIdx);
            if (deckRules != null && deckRules.potentialDrawDeck != null)
                deckRules.potentialDrawDeck.Add(extra);
        }

        while (playerActionHand.Count > actCap)
        {
            int lastIdx = playerActionHand.Count - 1;
            GameCard extra = playerActionHand[lastIdx];
            playerActionHand.RemoveAt(lastIdx);
            if (deckRules != null && deckRules.actionDrawDeck != null)
                deckRules.actionDrawDeck.Add(extra);
        }

        HashSet<string> allowedIds = new HashSet<string>();
        foreach (var c in playerPotentialHand)
            if (!string.IsNullOrEmpty(c.card_Id)) allowedIds.Add(c.card_Id);
        foreach (var c in playerActionHand)
            if (!string.IsNullOrEmpty(c.card_Id)) allowedIds.Add(c.card_Id);

        Transform holder = playerHandHolder != null ? playerHandHolder : (cardSlotManager != null ? cardSlotManager.transform : null);
        if (holder == null) return;

        HashSet<string> seenVisualIds = new HashSet<string>();
        int keptCount = 0;
        int maxTotal = potCap + actCap;

        for (int i = holder.childCount - 1; i >= 0; i--)
        {
            Transform child = holder.GetChild(i);
            if (child == null || child.gameObject == null) continue;

            UICard ui = child.GetComponent<UICard>() ?? child.GetComponentInChildren<UICard>();
            string cardId = (ui != null && ui.cardData != null && !string.IsNullOrEmpty(ui.cardData.card_Id))
                ? ui.cardData.card_Id
                : child.gameObject.name;

            bool isDuplicateId = !string.IsNullOrEmpty(cardId) && seenVisualIds.Contains(cardId);
            bool isNotInHand = allowedIds.Count > 0 && !string.IsNullOrEmpty(cardId) && !allowedIds.Contains(cardId);
            bool exceedsTotalCap = keptCount >= maxTotal;

            if (isDuplicateId || isNotInHand || exceedsTotalCap)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            else
            {
                if (!string.IsNullOrEmpty(cardId)) seenVisualIds.Add(cardId);
                keptCount++;
            }
        }
    }

    public bool EvaluateCardPlayability(string cardName, out string reason)
    {
        reason = string.Empty;

        if (currentTurn != TurnState.PlayerTurn) { reason = "not your turn"; return false; }
        if (playerSkipNextTurn) { reason = "turn skipped"; return false; }
        if (isEndingTurn || currentPhase == TurnPhase.RefillPhase) { reason = "turn ending/refill"; return false; }
        if (currentPhase != TurnPhase.PlayPhase || (discardController != null && discardController.isDiscardMode)) { reason = "not Play Phase"; return false; }
        if (isPlayerPieceMoving || (playerPositionScript != null && playerPositionScript.currentIndex != playerPositionScript.targetIndex)) { reason = "piece moving"; return false; }
        if (deckRules == null) { reason = "deck rules missing"; return false; }

        GameCard matchedCard = FindCardInHands(cardName);
        if (matchedCard == null) { reason = "not in hand"; return false; }
        if (matchedCard.drawnOnTurn == turnCounter) { reason = "drawn this turn"; return false; }
        if (playerPositionScript == null || playerPositionScript.positions == null || playerPositionScript.positions.Length == 0) { reason = "no player pos"; return false; }

        int maxIdx = playerPositionScript.positions.Length - 1;
        int currentIndex = Mathf.Clamp(playerPositionScript.currentIndex, 0, maxIdx);
        int nextIndex = Mathf.Min(currentIndex + 1, maxIdx);
        BoardStage currentStage = deckRules.GetStageAtIndex(currentIndex);
        BoardStage nextStage = deckRules.GetStageAtIndex(nextIndex);

        if (currentIndex == 0)
        {
            if (IsStartingCardName(cardName)) return true;
            reason = "need Weak/Strong Stim";
            return false;
        }

        if (cardName == "Alcohol" || cardName == "Absolute Refractory Period" ||
            cardName == "Relative Refractory Period" || cardName == "Demyelinated Axon" ||
            cardName == "Narrow Axon")
        {
            if (botController == null) { reason = "no opponent"; return false; }
            BoardStage bobStage = deckRules.GetStageAtIndex(botController.currentIndex);
            if (!deckRules.CanPlayActionSlowdownOnStage(cardName, bobStage))
            {
                reason = $"Bob in {bobStage}";
                return false;
            }
            return true;
        }

        if (playerLockedFromForwardMoveThisTurn && (matchedCard.type == GameCardType.Potential || cardName == "Caffeine"))
        {
            reason = "locked after Weak Stim";
            return false;
        }

        if (cardName == "Caffeine")
        {
            if (currentStage != BoardStage.PinkThreshold || nextStage != BoardStage.PinkThreshold) { reason = "need Pink stage"; return false; }
            if (currentIndex >= deckRules.lastPinkIndex) { reason = "at -55mV limit"; return false; }
            return true;
        }

        if (cardName == "Myelinated Axon" || cardName == "Large Axon")
        {
            if (nextStage != BoardStage.YellowPTrack) { reason = "need Yellow P next"; return false; }
            GameCard propCard = playerPotentialHand.Find(c => c != null && (c.cardName == "Propagation" || c.cardName == "Potential Joker") && c.drawnOnTurn != turnCounter);
            if (propCard == null) { reason = "need Propagation"; return false; }
            return true;
        }

        if (matchedCard.type == GameCardType.Potential)
        {
            if (cardName.Equals("Weak stimulus", StringComparison.OrdinalIgnoreCase)) { reason = "Hillock start only"; return false; }
            if (cardName.Equals("Potential Joker", StringComparison.OrdinalIgnoreCase)) return true;
            string required = deckRules.GetRequiredPotentialCardForNextSpace(nextStage);
            if (!MatchesRequiredPotentialCard(cardName, required)) { reason = $"need {required}"; return false; }
            return true;
        }

        reason = "invalid right now";
        return false;
    }

    IEnumerator SetupGameRoutine()
    {
        SetPlayStopperActive(true);
        if (botController == null) botController = FindAnyObjectByType<BotController>();

        yield return StartCoroutine(RefillPlayerHandRoutine(0));

        if (botController != null && deckRules != null)
        {
            botController.gameCardManager = this;
            yield return StartCoroutine(botController.RefillBobHandRoutine(
                deckRules.potentialDrawDeck,
                deckRules.actionDrawDeck,
                deckSpawnMarker,
                maxPotentialCards,
                maxActionCards));
        }

        currentTurn = (UnityEngine.Random.value < 0.5f) ? TurnState.PlayerTurn : TurnState.BotTurn;
        turnCounter = 1;
        StartTurn();
    }

    void StartTurn()
    {
        isEndingTurn = false;
        isPlayerPieceMoving = false;
        playerLockedFromForwardMoveThisTurn = false;
        if (discardController != null) discardController.ResetTurnDiscards();
        EnforceHandLimitsAndSyncVisuals();

        if (currentTurn == TurnState.PlayerTurn)
        {
            if (playerSkipNextTurn)
            {
                if (discardController != null) discardController.ForceExitDiscardMode("Turn skipped");
                playerSkipNextTurn = false;
                RefreshPlayStopper();
                StartCoroutine(EndTurnRefillAndPassRoutine(true));
                return;
            }

            currentPhase = TurnPhase.PlayPhase;
            RefreshPlayStopper();
            UpdateTurnUI();
            if (discardController != null) discardController.RefreshAllCardVisuals();
        }
        else
        {
            RefreshPlayStopper();
            StartCoroutine(RunBotTurnRoutine());
        }
    }

    public void EndPlayerTurn()
    {
        if (currentTurn != TurnState.PlayerTurn || isEndingTurn || isPlayerPieceMoving) return;
        isEndingTurn = true;
        if (discardController != null) discardController.ResetTurnDiscards();
        RefreshPlayStopper();
        StartCoroutine(EndTurnRefillAndPassRoutine(true));
    }

    public void OnPlayerEndTurnButton() => EndPlayerTurn();

    public void ProcessCardPlay(string cardName)
    {
        if (playerSkipNextTurn)
        {
            if (discardController != null) discardController.ForceExitDiscardMode("Turn skipped");
            return;
        }

        string reason;
        if (!EvaluateCardPlayability(cardName, out reason))
        {
            Debug.LogWarning($"'{cardName}': {reason}");
            return;
        }

        GameCard matchedCard = FindCardInHands(cardName);
        if (matchedCard == null) return;

        GameObject visualObj = discardController != null ? discardController.FindVisualForCard(matchedCard) : null;
        TryPlayCard(matchedCard, visualObj);
    }

    void TryPlayCard(GameCard card, GameObject visualObj)
    {
        if (card == null || playerPositionScript == null || playerPositionScript.positions == null || playerPositionScript.positions.Length == 0 || deckRules == null)
            return;

        int maxIdx = playerPositionScript.positions.Length - 1;
        int currentIndex = Mathf.Clamp(playerPositionScript.currentIndex, 0, maxIdx);
        int nextIndex = Mathf.Min(currentIndex + 1, maxIdx);
        BoardStage nextStage = deckRules.GetStageAtIndex(nextIndex);

        if (currentIndex == 0)
        {
            if (card.cardName.Equals("Weak stimulus", StringComparison.OrdinalIgnoreCase))
            {
                playerPositionScript.targetIndex = Mathf.Min(deckRules.startIndex, maxIdx);
                playerLockedFromForwardMoveThisTurn = true;
                ConsumePlayedCard(card, visualObj, true);
                BeginMoveLock();
            }
            else if (card.cardName.Equals("Strong stimulus", StringComparison.OrdinalIgnoreCase) ||
                     card.cardName.Equals("Potential Joker", StringComparison.OrdinalIgnoreCase))
            {
                playerPositionScript.targetIndex = Mathf.Min(deckRules.startIndex, maxIdx);
                ConsumePlayedCard(card, visualObj, true);
                BeginMoveLock();
            }
            return;
        }

        if (card.cardName == "Alcohol" || card.cardName == "Absolute Refractory Period" ||
            card.cardName == "Relative Refractory Period" || card.cardName == "Demyelinated Axon" ||
            card.cardName == "Narrow Axon")
        {
            ApplyActionPenaltyToBot(card.cardName);
            ConsumePlayedCard(card, visualObj, false);
            return;
        }

        if (card.cardName == "Caffeine")
        {
            playerPositionScript.targetIndex = Mathf.Min(currentIndex + 2, deckRules.lastPinkIndex, maxIdx);
            ConsumePlayedCard(card, visualObj, false);
            BeginMoveLock();
            return;
        }

        if ((card.cardName == "Myelinated Axon" || card.cardName == "Large Axon") && nextStage == BoardStage.YellowPTrack)
        {
            GameCard propCard = playerPotentialHand.Find(c => c != null && (c.cardName == "Propagation" || c.cardName == "Potential Joker") && c.drawnOnTurn != turnCounter);
            if (propCard == null) return;

            GameObject propVisual = discardController != null ? discardController.FindVisualForCard(propCard) : null;
            ConsumePlayedCard(propCard, propVisual, true);
            ConsumePlayedCard(card, visualObj, false);
            playerPositionScript.targetIndex = Mathf.Min(currentIndex + 2, maxIdx);
            BeginMoveLock();
            return;
        }

        if (card.type == GameCardType.Potential)
        {
            playerPositionScript.targetIndex = nextIndex;
            ConsumePlayedCard(card, visualObj, true);
            BeginMoveLock();
        }
    }

    void ConsumePlayedCard(GameCard card, GameObject visualObj, bool isPotential)
    {
        if (card == null) return;

        if (isPotential)
        {
            GameCard inHand = playerPotentialHand.Find(c => c != null && c.card_Id == card.card_Id)
                              ?? playerPotentialHand.Find(c => c != null && c.cardName == card.cardName);
            if (inHand != null) playerPotentialHand.Remove(inHand);
            if (deckRules != null && deckRules.potentialDiscardPile != null) deckRules.potentialDiscardPile.Add(card);
        }
        else
        {
            GameCard inHand = playerActionHand.Find(c => c != null && c.card_Id == card.card_Id)
                              ?? playerActionHand.Find(c => c != null && c.cardName == card.cardName);
            if (inHand != null) playerActionHand.Remove(inHand);
            if (deckRules != null && deckRules.actionDiscardPile != null) deckRules.actionDiscardPile.Add(card);
        }

        GameObject targetVisual = visualObj != null ? visualObj : (discardController != null ? discardController.FindVisualForCard(card) : null);
        if (targetVisual != null)
        {
            if (cardSlotManager != null)
                cardSlotManager.PlayCard(targetVisual);
            else
                Destroy(targetVisual);
        }

        EnforceHandLimitsAndSyncVisuals();
    }

    void ApplyActionPenaltyToBot(string cardName)
    {
        if (botController == null || deckRules == null) return;

        if (cardName == "Alcohol")
            botController.targetIndex = Mathf.Max(botController.currentIndex - 2, deckRules.startIndex);
        else if (cardName == "Relative Refractory Period")
        {
            if (botController.bobPotentialHand != null && !botController.bobPotentialHand.Exists(c => c != null && c.cardName == "Strong Stimulus") && !botController.botSkipNextTurn)
                botController.botSkipNextTurn = true;
        }
        else if (!botController.botSkipNextTurn)
            botController.botSkipNextTurn = true;
    }

    void BeginMoveLock()
    {
        isPlayerPieceMoving = true;
        SetPlayStopperActive(true);
        if (moveWaitCoroutine != null) StopCoroutine(moveWaitCoroutine);
        moveWaitCoroutine = StartCoroutine(WaitForPlayerMoveRoutine());
    }

    IEnumerator WaitForPlayerMoveRoutine()
    {
        yield return new WaitForSeconds(0.05f);
        float timeout = 12f;
        float elapsed = 0f;

        while (playerPositionScript != null && elapsed < timeout)
        {
            bool idxOk = playerPositionScript.currentIndex == playerPositionScript.targetIndex;
            bool posOk = true;
            if (playerPositionScript.positions != null &&
                playerPositionScript.targetIndex >= 0 &&
                playerPositionScript.targetIndex < playerPositionScript.positions.Length)
            {
                posOk = Vector3.Distance(playerPositionScript.transform.position, playerPositionScript.positions[playerPositionScript.targetIndex]) <= 0.05f;
            }
            if (idxOk && posOk) break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        moveWaitCoroutine = null;
        isPlayerPieceMoving = false;
        RefreshPlayStopper();
        CheckWinCondition();
    }

    IEnumerator EndTurnRefillAndPassRoutine(bool wasPlayerTurn)
    {
        isEndingTurn = true;
        currentPhase = TurnPhase.RefillPhase;
        if (discardController != null) discardController.ForceExitDiscardMode();
        RefreshPlayStopper();
        UpdateTurnUI();

        if (wasPlayerTurn)
        {
            yield return StartCoroutine(RefillPlayerHandRoutine(turnCounter));
            yield return new WaitForSeconds(0.4f);
            currentTurn = TurnState.BotTurn;
        }
        else
        {
            if (botController != null && deckRules != null)
            {
                yield return StartCoroutine(botController.RefillBobHandRoutine(
                    deckRules.potentialDrawDeck,
                    deckRules.actionDrawDeck,
                    deckSpawnMarker,
                    maxPotentialCards,
                    maxActionCards));
            }
            yield return new WaitForSeconds(0.4f);
            currentTurn = TurnState.PlayerTurn;
        }

        turnCounter++;
        StartTurn();
    }

    IEnumerator RefillPlayerHandRoutine(int drawTurnTag)
    {
        if (deckRules == null) yield break;

        int potCap = Mathf.Max(0, maxPotentialCards);
        int actCap = Mathf.Max(0, maxActionCards);

        while (playerPotentialHand.Count < potCap && (playerPotentialHand.Count + playerActionHand.Count) < MaxTotalHandCards)
        {
            GameCard drawn = DrawCardWithAutoReshuffle(GameCardType.Potential);
            if (drawn == null) break;
            drawn.drawnOnTurn = drawTurnTag;
            playerPotentialHand.Add(drawn);
            SpawnAndFlyCardToPlayer(drawn);
            yield return new WaitForSeconds(0.15f);
        }

        while (playerActionHand.Count < actCap && (playerPotentialHand.Count + playerActionHand.Count) < MaxTotalHandCards)
        {
            GameCard drawn = DrawCardWithAutoReshuffle(GameCardType.Action);
            if (drawn == null) break;

            if (drawn.cardName == "Tetrodotoxin")
            {
                playerTetrodotoxinCount++;
                if (deckRules.actionDiscardPile != null) deckRules.actionDiscardPile.Add(drawn);
                if (playerTetrodotoxinCount >= 3) yield break;
                continue;
            }

            drawn.drawnOnTurn = drawTurnTag;
            playerActionHand.Add(drawn);
            SpawnAndFlyCardToPlayer(drawn);
            yield return new WaitForSeconds(0.15f);
        }

        EnforceHandLimitsAndSyncVisuals();
    }

    void SpawnAndFlyCardToPlayer(GameCard cardData)
    {
        if (cardData == null) return;
        if ((playerPotentialHand.Count + playerActionHand.Count) > MaxTotalHandCards) return;

        GameObject prefabToUse = (flyingCardFallbackPrefab != null) ? flyingCardFallbackPrefab : defaultUICardPrefab;
        if (prefabToUse == null) return;

        Vector3 startPos = (deckSpawnMarker != null) ? deckSpawnMarker.position : Vector3.zero;
        GameObject flyingObj = Instantiate(prefabToUse, startPos, Quaternion.identity);
        flyingObj.name = $"Flying_{cardData.card_Id}";
        StartCoroutine(FlyCardRoutine(flyingObj, cardData));
    }

    IEnumerator FlyCardRoutine(GameObject flyingObj, GameCard cardData)
    {
        if (cardData == null)
        {
            if (flyingObj != null) Destroy(flyingObj);
            yield break;
        }

        Vector3 startPos = (flyingObj != null) ? flyingObj.transform.position : Vector3.zero;
        Vector3 targetPos = (playerHandHolder != null) ? playerHandHolder.position : startPos + new Vector3(0f, 1.5f, -2.5f);

        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (flyingObj == null) break;
            flyingObj.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (flyingObj != null) Destroy(flyingObj);

        bool stillInHand = (playerPotentialHand != null && playerPotentialHand.Exists(c => c != null && c.card_Id == cardData.card_Id)) ||
                           (playerActionHand != null && playerActionHand.Exists(c => c != null && c.card_Id == cardData.card_Id));

        if (!stillInHand || (playerPotentialHand.Count + playerActionHand.Count) > MaxTotalHandCards)
            yield break;

        if (defaultUICardPrefab != null)
        {
            GameObject uiCard = Instantiate(defaultUICardPrefab, targetPos, Quaternion.identity);
            uiCard.name = cardData.card_Id;

            UICard uiCardScript = uiCard.GetComponent<UICard>();
            if (uiCardScript != null)
                uiCardScript.InitializeCard(cardData, this);
            else
            {
                CardDisplay display = uiCard.GetComponent<CardDisplay>();
                if (display != null) display.SetCardName(cardData.cardName);
            }

            if (playerHandHolder != null)
                uiCard.transform.SetParent(playerHandHolder, false);

            if (cardSlotManager != null)
                cardSlotManager.AddCardToHand(uiCard);

            EnforceHandLimitsAndSyncVisuals();
        }
    }

    IEnumerator RunBotTurnRoutine()
    {
        SetPlayStopperActive(true);

        if (botController != null && botController.botSkipNextTurn)
        {
            botController.botSkipNextTurn = false;
            yield return StartCoroutine(EndTurnRefillAndPassRoutine(false));
            yield break;
        }

        currentPhase = TurnPhase.PlayPhase;
        UpdateTurnUI();
        yield return new WaitForSeconds(0.6f);

        if (botController != null && deckRules != null)
            yield return StartCoroutine(botController.ExecuteBotTurnLogic(deckRules.potentialDiscardPile, deckRules.actionDiscardPile));

        yield return StartCoroutine(EndTurnRefillAndPassRoutine(false));
    }

    void CheckWinCondition()
    {
        if (playerPositionScript != null && playerPositionScript.positions != null &&
            playerPositionScript.targetIndex >= playerPositionScript.positions.Length - 1)
            Debug.Log("Player wins!");

        if (botController != null && botController.positions != null &&
            botController.targetIndex >= botController.positions.Length - 1)
            Debug.Log("Bob wins!");
    }

    public void UpdateTurnUI()
    {
        if (turnDisplayText == null) return;
        string owner = (currentTurn == TurnState.PlayerTurn) ? "Player" : "Bob";
        string phase = discardController != null && discardController.isDiscardMode
            ? $"Discard ({discardController.potentialsDiscardedThisTurn}/2P {discardController.actionsDiscardedThisTurn}/1A)"
            : currentPhase.ToString();
        turnDisplayText.text = $"{owner} - {phase}";
    }
}