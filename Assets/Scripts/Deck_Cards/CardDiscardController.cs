using UnityEngine;
using UnityEngine.EventSystems;

public class CardDiscardController : MonoBehaviour
{
    public const int MAX_POTENTIAL_DISCARDS = 2;
    public const int MAX_ACTION_DISCARDS = 1;

    [Header("Right-Click Settings")]
    public float doubleRightClickMaxDelay = 0.4f;

    [Header("References")]
    public GameCardManager cardManager;
    public CardDeckAndRules deckRules;
    public Transform playerHandHolder;

    [Header("Discard State")]
    public bool isDiscardMode = false;
    public int potentialsDiscardedThisTurn = 0;
    public int actionsDiscardedThisTurn = 0;

    private float lastRightClickTime = -10f;

    void Update()
    {
        if (cardManager != null && cardManager.playerSkipNextTurn && isDiscardMode)
        {
            ForceExitDiscardMode("Turn skipped");
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            RegisterRightClickForDiscardMode();
        }
    }

    public void RegisterRightClickForDiscardMode()
    {
        if (cardManager == null) cardManager = FindAnyObjectByType<GameCardManager>();
        if (cardManager == null || cardManager.playerSkipNextTurn || !cardManager.CanPlayerInteractNow())
        {
            ForceExitDiscardMode("Locked");
            return;
        }

        if (isDiscardMode)
        {
            lastRightClickTime = -10f;
            SetDiscardMode(false);
            return;
        }

        float now = Time.unscaledTime;
        if (now - lastRightClickTime <= doubleRightClickMaxDelay)
        {
            lastRightClickTime = -10f;
            SetDiscardMode(true);
        }
        else
        {
            lastRightClickTime = now;
        }
    }

    public void SetDiscardMode(bool active)
    {
        if (cardManager == null) cardManager = FindAnyObjectByType<GameCardManager>();
        if (active && (cardManager == null || cardManager.playerSkipNextTurn || !cardManager.CanPlayerInteractNow()))
            return;

        isDiscardMode = active;
        if (cardManager != null)
        {
            cardManager.SetPhaseFromDiscardMode(isDiscardMode);
        }

        RefreshAllCardVisuals();
        Debug.Log(isDiscardMode ? "Discard ON" : "Discard OFF");
    }

    public void ForceExitDiscardMode(string reason = "")
    {
        bool wasActive = isDiscardMode;
        isDiscardMode = false;
        lastRightClickTime = -10f;

        if (cardManager != null)
        {
            cardManager.SetPhaseFromDiscardMode(false);
        }

        RefreshAllCardVisuals();
        if (wasActive)
        {
            Debug.Log(string.IsNullOrEmpty(reason) ? "Discard OFF" : $"Discard OFF ({reason})");
        }
    }

    public void RefreshAllCardVisuals()
    {
        UICard[] allCards = FindObjectsByType<UICard>(FindObjectsSortMode.None);
        if (allCards == null) return;

        foreach (UICard uiCard in allCards)
        {
            if (uiCard != null) uiCard.RefreshPlayabilityVisual(force: true);
        }
    }

    public void ResetTurnDiscards()
    {
        isDiscardMode = false;
        potentialsDiscardedThisTurn = 0;
        actionsDiscardedThisTurn = 0;
        lastRightClickTime = -10f;
    }

    public bool TryDiscardCardByName(string cardName, GameObject visualObj = null)
    {
        if (string.IsNullOrEmpty(cardName)) return false;
        if (cardManager == null) cardManager = FindAnyObjectByType<GameCardManager>();
        if (deckRules == null && cardManager != null) deckRules = cardManager.deckRules;
        if (cardManager == null || deckRules == null) return false;

        if (cardManager.playerSkipNextTurn)
        {
            ForceExitDiscardMode("Turn skipped");
            return false;
        }

        if (!cardManager.CanPlayerInteractNow())
            return false;

        GameCard candidate = (cardManager.playerPotentialHand != null ? cardManager.playerPotentialHand.Find(c => c != null && c.cardName == cardName) : null)
                             ?? (cardManager.playerActionHand != null ? cardManager.playerActionHand.Find(c => c != null && c.cardName == cardName) : null);

        if (candidate == null && visualObj != null)
        {
            UICard uiCard = visualObj.GetComponent<UICard>() ?? visualObj.GetComponentInChildren<UICard>();
            if (uiCard != null && uiCard.cardData != null)
            {
                candidate = (cardManager.playerPotentialHand != null ? cardManager.playerPotentialHand.Find(c => c != null && c.card_Id == uiCard.cardData.card_Id) : null)
                            ?? (cardManager.playerActionHand != null ? cardManager.playerActionHand.Find(c => c != null && c.card_Id == uiCard.cardData.card_Id) : null);
            }
        }

        if (candidate == null) return false;

        if (candidate.type == GameCardType.Potential)
        {
            if (potentialsDiscardedThisTurn >= MAX_POTENTIAL_DISCARDS)
            {
                Debug.LogWarning("Max 2 Pot discards");
                return false;
            }
            if (cardManager.playerPotentialHand != null) cardManager.playerPotentialHand.Remove(candidate);
            if (deckRules.potentialDiscardPile != null) deckRules.potentialDiscardPile.Add(candidate);
            potentialsDiscardedThisTurn++;
            Debug.Log($"Discarded {candidate.cardName} ({potentialsDiscardedThisTurn}/2P)");
        }
        else
        {
            if (actionsDiscardedThisTurn >= MAX_ACTION_DISCARDS)
            {
                Debug.LogWarning("Max 1 Act discard");
                return false;
            }
            if (cardManager.playerActionHand != null) cardManager.playerActionHand.Remove(candidate);
            if (deckRules.actionDiscardPile != null) deckRules.actionDiscardPile.Add(candidate);
            actionsDiscardedThisTurn++;
            Debug.Log($"Discarded {candidate.cardName} ({actionsDiscardedThisTurn}/1A)");
        }

        GameObject targetVisual = visualObj != null ? visualObj : FindVisualForCard(candidate);
        if (targetVisual != null)
        {
            if (cardManager.cardSlotManager != null)
                cardManager.cardSlotManager.PlayCard(targetVisual);
            else
                Destroy(targetVisual);
        }

        cardManager.EnforceHandLimitsAndSyncVisuals();
        cardManager.UpdateTurnUI();
        return true;
    }

    public GameObject FindVisualForCard(GameCard card)
    {
        if (card == null) return null;

        Transform holder = playerHandHolder != null ? playerHandHolder : (cardManager != null ? cardManager.playerHandHolder : null);
        if (holder != null)
        {
            int count = holder.childCount;
            for (int i = 0; i < count; i++)
            {
                if (holder == null || i >= holder.childCount) break;
                Transform child = holder.GetChild(i);
                if (child == null || child.gameObject == null || !child.gameObject.activeInHierarchy) continue;

                UICard uiCard = child.GetComponent<UICard>();
                if (uiCard != null && uiCard.cardData != null && uiCard.cardData.card_Id == card.card_Id)
                    return child.gameObject;

                if (child.name == card.card_Id || (!string.IsNullOrEmpty(card.cardName) && child.name.Contains(card.cardName)))
                    return child.gameObject;
            }
        }

        UICard[] allCards = FindObjectsByType<UICard>(FindObjectsSortMode.None);
        if (allCards == null) return null;

        foreach (UICard uiCard in allCards)
        {
            if (uiCard == null || uiCard.gameObject == null || !uiCard.gameObject.activeInHierarchy) continue;
            if ((uiCard.cardData != null && uiCard.cardData.card_Id == card.card_Id) ||
                (!string.IsNullOrEmpty(card.cardName) && uiCard.cardName == card.cardName) ||
                uiCard.gameObject.name == card.card_Id)
            {
                return uiCard.gameObject;
            }
        }

        return null;
    }
}