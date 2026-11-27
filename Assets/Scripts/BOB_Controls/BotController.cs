using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotController : MonoBehaviour
{
    [Header("Bob's Board Path (-1.2 Z Offset Applied)")]
    public Vector3[] positions = new Vector3[]
    {
        new Vector3(26.071f, 0.0f, 3.91f - 1.2f),
        new Vector3(23.735f, 0.0f, 3.726f - 1.2f),
        new Vector3(21.311f, 0.0f, 3.322f - 1.2f),
        new Vector3(18.938f, 0.0f, 2.911f - 1.2f),
        new Vector3(16.55f, 0.0f, 2.588f - 1.2f),
        new Vector3(14.17f, 0.0f, 2.206f - 1.2f),
        new Vector3(11.797f, 0.0f, 1.831f - 1.2f),
        new Vector3(9.395f, 0.0f, 1.368f - 1.2f),
        new Vector3(6.956f, 0.0f, 1.118f - 1.2f),
        new Vector3(5.246f, 0.0f, -3.304f - 1.2f),
        new Vector3(3.561f, 0.0f, -5.811f - 1.2f),
        new Vector3(1.908f, 0.0f, -8.254f - 1.2f),
        new Vector3(0.06f, 0.0f, -10.81f - 1.2f),
        new Vector3(-1.7f, 0.0f, -13.53f - 1.2f),
        new Vector3(-3.58f, 0.0f, -11.64f - 1.2f),
        new Vector3(-6.68f, 0.0f, -8.28f - 1.2f),
        new Vector3(-9.3f, 0.0f, -5.42f - 1.2f),
        new Vector3(-11.38f, 0.0f, -2.46f - 1.2f),
        new Vector3(-13.27f, 0.0f, -0.31f - 1.2f),
        new Vector3(-15.58f, 0.0f, 1.36f - 1.2f),
        new Vector3(-18.72f, 0.0f, 3.66f - 1.2f),
        new Vector3(-21.29f, 0.0f, 4.78f - 1.2f),
        new Vector3(-22.99f, 0.0f, 5.59f - 1.2f),
        new Vector3(-24.79f, 0.0f, 6.59f - 1.2f),
        new Vector3(-26.63f, 0.0f, 7.3f - 1.2f),
        new Vector3(-28.49f, 0.0f, 6.56f - 1.2f),
        new Vector3(-30.5f, 0.0f, 5.49f - 1.2f),
        new Vector3(-32.31f, 0.0f, 4.59f - 1.2f),
        new Vector3(-34.49f, 0.0f, 3.97f - 1.2f),
        new Vector3(-32.278f, 0.0f, 16.179f - 1.2f),
        new Vector3(-29.171f, 0.0f, 16.179f - 1.2f),
        new Vector3(-26.171f, 0.0f, 16.179f - 1.2f),
        new Vector3(-23.171f, 0.0f, 16.179f - 1.2f),
        new Vector3(-20.171f, 0.0f, 16.179f - 1.2f),
        new Vector3(-16.921f, 0.0f, 16.179f - 1.2f),
        new Vector3(-13.921f, 0.0f, 16.179f - 1.2f),
        new Vector3(-10.671f, 0.0f, 16.179f - 1.2f),
        new Vector3(-7.421f, 0.0f, 16.179f - 1.2f),
        new Vector3(-4.421f, 0.0f, 16.179f - 1.2f),
        new Vector3(-1.171f, 0.0f, 16.179f - 1.2f),
        new Vector3(1.829f, 0.0f, 16.179f - 1.2f),
        new Vector3(5.079f, 0.0f, 16.179f - 1.2f),
        new Vector3(8.22f, 0.0f, 16.179f - 1.2f),
        new Vector3(11.47f, 0.0f, 16.179f - 1.2f),
        new Vector3(14.47f, 0.0f, 16.179f - 1.2f),
        new Vector3(17.72f, 0.0f, 16.179f - 1.2f),
        new Vector3(20.86f, 0.0f, 16.179f - 1.2f),
        new Vector3(23.98f, 0.0f, 16.179f - 1.2f),
        new Vector3(29.1f, 0.0f, 16.179f - 1.2f)
    };

    [Header("Movement Settings")]
    public float liftHeight = 1.5f;
    public float moveSpeed = 5.0f;

    [Header("References")]
    public GameCardManager gameCardManager;
    public Transform bobHandHolder;
    public GameObject bobCardPrefab;

    [Header("Bob Hand Visuals")]
    [Tooltip("Multiplier on the card size/spacing copied from the player hand.")]
    public float bobCardScale = 1f;

    [Header("Board Position Status")]
    public int currentIndex = 0;
    public int targetIndex = 0;
    private bool isMoving = false;
    private int lastObservedTargetIndex = 0;

    [Header("Bob's Independent Hands")]
    public List<GameCard> bobPotentialHand = new List<GameCard>();
    public List<GameCard> bobActionHand = new List<GameCard>();
    private List<GameObject> activeBobVisualCards = new List<GameObject>();

    [Header("Status Flags")]
    public bool botSkipNextTurn = false;
    public int bobTetrodotoxinCount = 0;
    private int bobTurnCounter = 0;

    void Start()
    {
        if (gameCardManager == null)
            gameCardManager = FindAnyObjectByType<GameCardManager>();

        if (bobHandHolder == null)
        {
            GameObject marker = GameObject.Find("BobsHandPos");
            if (marker != null) bobHandHolder = marker.transform;
        }

        if (bobPotentialHand == null) bobPotentialHand = new List<GameCard>();
        if (bobActionHand == null) bobActionHand = new List<GameCard>();
        if (activeBobVisualCards == null) activeBobVisualCards = new List<GameObject>();

        if (positions != null && positions.Length > 0)
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, positions.Length - 1);
            transform.position = positions[currentIndex];
            targetIndex = currentIndex;
            lastObservedTargetIndex = currentIndex;
        }
    }

    void Update()
    {
        if (positions == null || positions.Length == 0) return;

        if (targetIndex != lastObservedTargetIndex && !isMoving)
        {
            targetIndex = Mathf.Clamp(targetIndex, 0, positions.Length - 1);
            if (targetIndex != currentIndex)
                StartCoroutine(MoveSequenceRoutine(targetIndex));
            lastObservedTargetIndex = targetIndex;
        }
    }

    IEnumerator MoveSequenceRoutine(int destinationIndex)
    {
        isMoving = true;
        while (currentIndex != destinationIndex)
        {
            int nextStepIndex = (destinationIndex > currentIndex) ? currentIndex + 1 : currentIndex - 1;
            nextStepIndex = Mathf.Clamp(nextStepIndex, 0, positions.Length - 1);
            yield return StartCoroutine(MoveStepRoutine(positions[nextStepIndex]));
            currentIndex = nextStepIndex;
        }
        isMoving = false;
    }

    IEnumerator MoveStepRoutine(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        Vector3 liftPos = new Vector3(startPos.x, startPos.y + liftHeight, startPos.z);
        while (Vector3.Distance(transform.position, liftPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, liftPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        Vector3 acrossPos = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        while (Vector3.Distance(transform.position, acrossPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, acrossPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
    }

    private static bool IsBobStartingCard(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name.Equals("Weak stimulus", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Strong stimulus", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Potential Joker", StringComparison.OrdinalIgnoreCase);
    }

    private static bool NameIs(string a, string b)
    {
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSlowdownCard(string n)
    {
        return NameIs(n, "Alcohol") ||
               NameIs(n, "Absolute refractory period") ||
               NameIs(n, "Absolute Refractory Pink") ||
               NameIs(n, "Relative refractory period") ||
               NameIs(n, "Demyelinated axon") ||
               NameIs(n, "Yellow Demyelinated Axon") ||
               NameIs(n, "Narrow axon") ||
               NameIs(n, "Yellow Narrow Axon");
    }

    private static bool IsAxonBoostCard(string n)
    {
        return NameIs(n, "Myelinated axon") ||
               NameIs(n, "Yellow Myelinated Axon") ||
               NameIs(n, "Large axon") ||
               NameIs(n, "Yellow Large Axon");
    }

    bool CanBobPlayPotentialCard(GameCard card, int idx, BoardStage nextStage, bool lockedFromForwardMove, out int resultingTargetIndex)
    {
        resultingTargetIndex = idx;
        if (card == null || gameCardManager == null || gameCardManager.deckRules == null || positions == null || positions.Length == 0)
            return false;

        int maxIdx = positions.Length - 1;
        int nextIdx = Mathf.Min(idx + 1, maxIdx);

        // Starting cards are handled by the opener in ExecuteBotTurnLogic
        if (idx == 0)
        {
            if (IsBobStartingCard(card.cardName))
            {
                int extraSpaces = NameIs(card.cardName, "Strong stimulus")
                    ? Mathf.Max(0, gameCardManager.strongStimulusExtraSpaces)
                    : 0;
                resultingTargetIndex = Mathf.Min(gameCardManager.deckRules.startIndex + extraSpaces, maxIdx);
                return true;
            }
            return false;
        }

        if (card.drawnOnTurn == bobTurnCounter)
            return false;

        if (lockedFromForwardMove)
            return false;

        if (NameIs(card.cardName, "Weak stimulus"))
            return false;

        if (NameIs(card.cardName, "Potential Joker"))
        {
            resultingTargetIndex = nextIdx;
            return true;
        }

        string required = gameCardManager.deckRules.GetRequiredPotentialCardForNextSpace(nextStage);
        if (GameCardManager.MatchesRequiredPotentialCard(card.cardName, required))
        {
            resultingTargetIndex = nextIdx;
            return true;
        }

        return false;
    }

    bool CanBobPlayActionCard(GameCard card, int idx, BoardStage currentStage, BoardStage nextStage, bool lockedFromForwardMove, out int resultingTargetIndex, out int pairedPropIndex)
    {
        resultingTargetIndex = idx;
        pairedPropIndex = -1;
        if (card == null || gameCardManager == null || gameCardManager.deckRules == null || positions == null || positions.Length == 0)
            return false;

        if (card.drawnOnTurn == bobTurnCounter)
            return false;

        int maxIdx = positions.Length - 1;
        CardDeckAndRules rules = gameCardManager.deckRules;

        if (NameIs(card.cardName, "Caffeine"))
        {
            if (lockedFromForwardMove || idx == 0) return false;
            if (currentStage != BoardStage.PinkThreshold || nextStage != BoardStage.PinkThreshold) return false;
            if (idx >= rules.lastPinkIndex) return false;

            resultingTargetIndex = Mathf.Min(idx + 2, rules.lastPinkIndex, maxIdx);
            return true;
        }

        if (IsAxonBoostCard(card.cardName))
        {
            if (lockedFromForwardMove || idx == 0) return false;
            if (nextStage != BoardStage.YellowPTrack) return false;

            for (int p = 0; p < bobPotentialHand.Count; p++)
            {
                GameCard prop = bobPotentialHand[p];
                if (prop != null && (NameIs(prop.cardName, "Propagation") || NameIs(prop.cardName, "Potential Joker")) && prop.drawnOnTurn != bobTurnCounter)
                {
                    pairedPropIndex = p;
                    resultingTargetIndex = Mathf.Min(idx + 2, maxIdx);
                    return true;
                }
            }
            return false;
        }

        if (IsSlowdownCard(card.cardName))
        {
            if (gameCardManager.playerPositionScript == null) return false;
            BoardStage playerStage = rules.GetStageAtIndex(gameCardManager.playerPositionScript.currentIndex);
            return rules.CanPlayActionSlowdownOnStage(card.cardName, playerStage);
        }

        return false;
    }

    public IEnumerator ExecuteBotTurnLogic(List<GameCard> potentialDiscard, List<GameCard> actionDiscard)
    {
        if (gameCardManager == null || gameCardManager.deckRules == null || positions == null || positions.Length == 0)
            yield break;

        if (potentialDiscard == null) potentialDiscard = gameCardManager.deckRules.potentialDiscardPile;
        if (actionDiscard == null) actionDiscard = gameCardManager.deckRules.actionDiscardPile;

        bobTurnCounter++;
        bool madeAnyPotentialPlay = false;
        bool madeAnyActionPlay = false;
        bool lockedFromForwardMove = false;
        CardDeckAndRules rules = gameCardManager.deckRules;
        int maxIdx = positions.Length - 1;

        // ---- OPENER: while Bob is still on the start space he must play his stimulus first ----
        while (isMoving) yield return null;

        if (currentIndex == 0)
        {
            GameCard starter = null;
            bool starterIsAction = false;

            // Prefer Strong stimulus / Joker (Potential), then Weak stimulus (Action)
            for (int p = 0; p < bobPotentialHand.Count; p++)
            {
                if (bobPotentialHand[p] != null && IsBobStartingCard(bobPotentialHand[p].cardName))
                {
                    starter = bobPotentialHand[p];
                    break;
                }
            }

            if (starter == null)
            {
                for (int a = 0; a < bobActionHand.Count; a++)
                {
                    if (bobActionHand[a] != null && IsBobStartingCard(bobActionHand[a].cardName))
                    {
                        starter = bobActionHand[a];
                        starterIsAction = true;
                        break;
                    }
                }
            }

            if (starter != null)
            {
                int extraSpaces = NameIs(starter.cardName, "Strong stimulus")
                    ? Mathf.Max(0, gameCardManager.strongStimulusExtraSpaces)
                    : 0;
                targetIndex = Mathf.Min(rules.startIndex + extraSpaces, maxIdx);

                if (starterIsAction)
                {
                    bobActionHand.Remove(starter);
                    if (actionDiscard != null) actionDiscard.Add(starter);
                    madeAnyActionPlay = true;
                }
                else
                {
                    bobPotentialHand.Remove(starter);
                    if (potentialDiscard != null) potentialDiscard.Add(starter);
                    madeAnyPotentialPlay = true;
                }

                // Weak stimulus locks forward movement for the rest of the turn (same rule as the player)
                if (NameIs(starter.cardName, "Weak stimulus"))
                    lockedFromForwardMove = true;

                gameCardManager.SendChatMessage($"Bob played {starter.cardName}");
                UpdateBobVisualHand();
                yield return new WaitForSeconds(0.7f);

                float safety = 12f;
                while ((isMoving || currentIndex != targetIndex) && safety > 0f)
                {
                    safety -= Time.deltaTime;
                    yield return null;
                }
            }
        }

        // ---- Potential cards ----
        for (int i = bobPotentialHand.Count - 1; i >= 0; i--)
        {
            while (isMoving) yield return null;
            if (i >= bobPotentialHand.Count) continue;

            GameCard card = bobPotentialHand[i];
            if (card == null) continue;

            int idx = Mathf.Clamp(currentIndex, 0, maxIdx);
            int nextIdx = Mathf.Min(idx + 1, maxIdx);
            BoardStage nextStage = rules.GetStageAtIndex(nextIdx);

            int nextTarget;
            if (CanBobPlayPotentialCard(card, idx, nextStage, lockedFromForwardMove, out nextTarget))
            {
                targetIndex = nextTarget;
                if (NameIs(card.cardName, "Weak stimulus"))
                    lockedFromForwardMove = true;

                Debug.Log($"Bob played {card.cardName}");

                bobPotentialHand.RemoveAt(i);
                if (potentialDiscard != null) potentialDiscard.Add(card);
                madeAnyPotentialPlay = true;
                UpdateBobVisualHand();
                yield return new WaitForSeconds(0.7f);
            }
        }

        // ---- Action cards ----
        for (int i = bobActionHand.Count - 1; i >= 0; i--)
        {
            while (isMoving) yield return null;
            if (i >= bobActionHand.Count) continue;

            GameCard card = bobActionHand[i];
            if (card == null) continue;

            int idx = Mathf.Clamp(currentIndex, 0, maxIdx);
            int nextIdx = Mathf.Min(idx + 1, maxIdx);
            BoardStage currentStage = rules.GetStageAtIndex(idx);
            BoardStage nextStage = rules.GetStageAtIndex(nextIdx);

            int nextTarget;
            int pairedPropIdx;
            if (CanBobPlayActionCard(card, idx, currentStage, nextStage, lockedFromForwardMove, out nextTarget, out pairedPropIdx))
            {
                if (NameIs(card.cardName, "Caffeine") || IsAxonBoostCard(card.cardName))
                {
                    string propName = string.Empty;
                    if (pairedPropIdx >= 0 && pairedPropIdx < bobPotentialHand.Count)
                    {
                        GameCard propCard = bobPotentialHand[pairedPropIdx];
                        propName = propCard != null ? propCard.cardName : string.Empty;
                        bobPotentialHand.RemoveAt(pairedPropIdx);
                        if (potentialDiscard != null && propCard != null) potentialDiscard.Add(propCard);
                    }

                    targetIndex = nextTarget;
                    Debug.Log(string.IsNullOrEmpty(propName)
                        ? $"Bob played {card.cardName}"
                        : $"Bob played {card.cardName} + {propName}");

                    bobActionHand.RemoveAt(i);
                    if (actionDiscard != null) actionDiscard.Add(card);
                    madeAnyActionPlay = true;
                    UpdateBobVisualHand();
                    yield return new WaitForSeconds(0.7f);
                }
                else
                {
                    Debug.Log($"Bob played {card.cardName}");
                    ApplyPenaltyToPlayer(card.cardName);
                    bobActionHand.RemoveAt(i);
                    if (actionDiscard != null) actionDiscard.Add(card);
                    madeAnyActionPlay = true;
                    UpdateBobVisualHand();
                    yield return new WaitForSeconds(0.7f);
                }
            }
        }

        if (!madeAnyPotentialPlay && bobPotentialHand.Count > 0)
        {
            int toDiscard = Mathf.Min(CardDiscardController.MAX_POTENTIAL_DISCARDS, bobPotentialHand.Count);
            for (int d = 0; d < toDiscard; d++)
            {
                GameCard discarded = bobPotentialHand[0];
                bobPotentialHand.RemoveAt(0);
                if (potentialDiscard != null && discarded != null)
                {
                    potentialDiscard.Add(discarded);
                    Debug.Log($"Bob disc {discarded.cardName}");
                }
            }
            UpdateBobVisualHand();
        }

        if (!madeAnyActionPlay && bobActionHand.Count > 0)
        {
            int toDiscard = Mathf.Min(CardDiscardController.MAX_ACTION_DISCARDS, bobActionHand.Count);
            for (int d = 0; d < toDiscard; d++)
            {
                GameCard discarded = bobActionHand[0];
                bobActionHand.RemoveAt(0);
                if (actionDiscard != null && discarded != null)
                {
                    actionDiscard.Add(discarded);
                    Debug.Log($"Bob discarded {discarded.cardName}");
                }
            }
            UpdateBobVisualHand();
        }

        while (isMoving) yield return null;
    }

    void ApplyPenaltyToPlayer(string cardName)
    {
        if (gameCardManager == null || gameCardManager.playerPositionScript == null || gameCardManager.deckRules == null) return;

        if (NameIs(cardName, "Alcohol"))
        {
            var playerPos = gameCardManager.playerPositionScript;
            playerPos.targetIndex = Mathf.Max(playerPos.currentIndex - 2, gameCardManager.deckRules.startIndex);
            Debug.LogWarning("Bob pushed you back 2");
        }
        else if (NameIs(cardName, "Relative refractory period"))
        {
            bool playerHasStrongStim = gameCardManager.playerPotentialHand != null &&
                                       gameCardManager.playerPotentialHand.Exists(c => c != null && NameIs(c.cardName, "Strong stimulus"));
            if (!playerHasStrongStim && !gameCardManager.playerSkipNextTurn)
            {
                gameCardManager.playerSkipNextTurn = true;
                Debug.LogWarning("Bob skipped your turn");
            }
        }
        else if (!gameCardManager.playerSkipNextTurn)
        {
            gameCardManager.playerSkipNextTurn = true;
            Debug.LogWarning("Bob skipped your turn");
        }
    }

    private GameCard DrawForBob(GameCardType cardType, CardDeckAndRules rules)
    {
        if (gameCardManager == null)
            gameCardManager = FindAnyObjectByType<GameCardManager>();

        if (gameCardManager != null)
            return gameCardManager.DrawCardWithAutoReshuffle(cardType);

        return rules != null ? rules.DrawFromDeck(cardType) : null;
    }

    public IEnumerator RefillBobHandRoutine(List<GameCard> potentialDeck, List<GameCard> actionDeck, Transform spawnMarker, int maxPotentials, int maxActions)
    {
        if (gameCardManager == null)
            gameCardManager = FindAnyObjectByType<GameCardManager>();

        if (gameCardManager == null || gameCardManager.deckRules == null) yield break;
        CardDeckAndRules rules = gameCardManager.deckRules;

        while (bobPotentialHand.Count < maxPotentials)
        {
            GameCard drawn = DrawForBob(GameCardType.Potential, rules);
            if (drawn == null) break;

            drawn.drawnOnTurn = bobTurnCounter;
            bobPotentialHand.Add(drawn);
            yield return StartCoroutine(FlyCardToBobHand(drawn, spawnMarker));
            yield return new WaitForSeconds(0.15f);
        }

        while (bobActionHand.Count < maxActions)
        {
            GameCard drawn = DrawForBob(GameCardType.Action, rules);
            if (drawn == null) break;

            if (NameIs(drawn.cardName, "Tetrodotoxin"))
            {
                bobTetrodotoxinCount++;
                if (rules.actionDiscardPile != null) rules.actionDiscardPile.Add(drawn);
                if (bobTetrodotoxinCount >= 3) yield break;
                continue;
            }

            drawn.drawnOnTurn = bobTurnCounter;
            bobActionHand.Add(drawn);
            yield return StartCoroutine(FlyCardToBobHand(drawn, spawnMarker));
            yield return new WaitForSeconds(0.15f);
        }

        UpdateBobVisualHand();
    }

    IEnumerator FlyCardToBobHand(GameCard cardData, Transform spawnMarker)
    {
        GameObject prefabToUse = (bobCardPrefab != null) ? bobCardPrefab : (gameCardManager != null ? gameCardManager.defaultUICardPrefab : null);
        if (prefabToUse == null) yield break;

        Vector3 startPos = (spawnMarker != null) ? spawnMarker.position : transform.position;
        GameObject cardInstance = Instantiate(prefabToUse, startPos, Quaternion.identity);

        Vector3 basePos = (bobHandHolder != null) ? bobHandHolder.position : transform.position + new Vector3(0, 0, 3f);
        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cardInstance == null) yield break;
            cardInstance.transform.position = Vector3.Lerp(startPos, basePos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (cardInstance != null) Destroy(cardInstance);
    }

    // Bob's physical cards live in fixed slots so they never shuffle or re-centre
    private GameObject[] bobSlotObjects = new GameObject[0];
    private GameCard[] bobSlotCards = new GameCard[0];

    /// <summary>
    /// Syncs Bob's physical cards with his hand data at BobsHandPos.
    /// Cards that are still in his hand are NOT touched or moved. Played/discarded cards are
    /// removed from their slot, and new cards are placed in the first empty slot.
    /// </summary>
    public void UpdateBobVisualHand()
    {
        if (gameCardManager == null)
            gameCardManager = FindAnyObjectByType<GameCardManager>();
        if (gameCardManager == null) return;

        List<GameCard> allBobCards = new List<GameCard>();
        foreach (GameCard c in bobPotentialHand) if (c != null) allBobCards.Add(c);
        foreach (GameCard c in bobActionHand) if (c != null) allBobCards.Add(c);

        int slotCount = Mathf.Max(gameCardManager.MaxTotalHandCards, allBobCards.Count);

        // Slot count only changes if hand limits change: start fresh in that case
        if (bobSlotObjects.Length != slotCount)
        {
            foreach (GameObject old in bobSlotObjects)
                if (old != null) Destroy(old);
            bobSlotObjects = new GameObject[slotCount];
            bobSlotCards = new GameCard[slotCount];
        }

        // 1. Remove visuals for cards that left Bob's hand (leave the slot empty)
        for (int i = 0; i < slotCount; i++)
        {
            if (bobSlotCards[i] != null && !allBobCards.Contains(bobSlotCards[i]))
            {
                if (bobSlotObjects[i] != null) Destroy(bobSlotObjects[i]);
                bobSlotObjects[i] = null;
                bobSlotCards[i] = null;
            }
        }

        // 2. Place any new cards into the first empty slot
        foreach (GameCard card in allBobCards)
        {
            if (Array.IndexOf(bobSlotCards, card) >= 0) continue;

            int slot = -1;
            for (int i = 0; i < slotCount; i++)
            {
                if (bobSlotCards[i] == null) { slot = i; break; }
            }
            if (slot < 0) break;

            GameObject spawned = SpawnBobCardInSlot(card, slot, slotCount);
            if (spawned == null) return;

            bobSlotCards[slot] = card;
            bobSlotObjects[slot] = spawned;
        }
    }

    private GameObject SpawnBobCardInSlot(GameCard cardData, int slot, int slotCount)
    {
        GameObject prefabToUse = (bobCardPrefab != null) ? bobCardPrefab : gameCardManager.defaultUICardPrefab;
        if (prefabToUse == null)
        {
            Debug.LogWarning("[BotController] No bobCardPrefab or defaultUICardPrefab assigned, can't show Bob's cards.");
            return null;
        }

        CardSlotManager slots = gameCardManager.cardSlotManager;
        Vector3 cardScale = (slots != null ? slots.cardScale : Vector3.one) * bobCardScale;
        Quaternion worldRot = (slots != null)
            ? slots.transform.rotation * Quaternion.Euler(slots.cardEulerRotation)
            : Quaternion.identity;
        Vector3 rightAxis = (slots != null) ? slots.transform.right : Vector3.right;
        float spacing = ((slots != null) ? slots.cardSpacing : 2.0f) * bobCardScale;

        Vector3 basePos = (bobHandHolder != null) ? bobHandHolder.position : transform.position + new Vector3(0, 0, 3f);
        Vector3 slotPos = basePos + rightAxis * ((slot - ((slotCount - 1) / 2.0f)) * spacing);

        GameObject cardInstance = Instantiate(prefabToUse, slotPos, worldRot);
        cardInstance.name = $"BOBS_CARD_{cardData.cardName}";

        // Display-only: no clicking, hovering, physics or animation
        UICard ui = cardInstance.GetComponent<UICard>();
        if (ui != null)
        {
            ui.enabled = false;
            Destroy(ui);
        }
        CardHover hover = cardInstance.GetComponent<CardHover>();
        if (hover != null) Destroy(hover);

        foreach (Collider col in cardInstance.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (Rigidbody rb in cardInstance.GetComponentsInChildren<Rigidbody>())
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
        foreach (Animator anim in cardInstance.GetComponentsInChildren<Animator>())
            anim.enabled = false;

        CardDisplay display = cardInstance.GetComponent<CardDisplay>() ?? cardInstance.GetComponentInChildren<CardDisplay>();
        if (display != null) display.SetCardName(cardData.cardName);

        if (bobHandHolder != null)
            cardInstance.transform.SetParent(bobHandHolder, true);
        cardInstance.transform.localScale = cardScale;

        return cardInstance;
    }
}