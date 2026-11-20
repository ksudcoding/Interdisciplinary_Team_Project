using UnityEngine;
using UnityEngine.EventSystems;

public class UICard : MonoBehaviour, IPointerClickHandler
{
    [Header("Card Info")]
    public string cardName;
    public GameCard cardData;

    [Header("References")]
    public GameCardManager gameCardManager;
    public CardDisplay cardDisplay;
    public Renderer cubeRenderer;

    [Header("Playability Cube Colors")]
    public Color playableColor = new Color(0.18f, 0.85f, 0.32f, 1f);
    public Color discardModeColor = new Color(0.95f, 0.65f, 0.12f, 1f);
    public Color unplayableColor = new Color(0.90f, 0.22f, 0.22f, 1f);

    private MaterialPropertyBlock propBlock;
    private Color? lastAppliedColor;
    private int lastActivationFrame = -1;
    private int lastRightClickFrame = -1;

    void Awake()
    {
        if (cardDisplay == null) cardDisplay = GetComponent<CardDisplay>() ?? GetComponentInChildren<CardDisplay>();
        if (gameCardManager == null) gameCardManager = FindAnyObjectByType<GameCardManager>();
        if (cubeRenderer == null)
        {
            Transform cubeChild = transform.Find("Cube");
            if (cubeChild != null) cubeRenderer = cubeChild.GetComponent<Renderer>();
        }
        propBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        RefreshPlayabilityVisual(force: true);
    }

    void Update()
    {
        RefreshPlayabilityVisual(force: false);
    }

    public void InitializeCard(GameCard data, GameCardManager manager)
    {
        cardData = data;
        cardName = data != null ? data.cardName : cardName;
        gameCardManager = manager != null ? manager : gameCardManager;

        if (cardDisplay != null && !string.IsNullOrEmpty(cardName))
            cardDisplay.SetCardName(cardName);

        RefreshPlayabilityVisual(force: true);
    }

    public void RefreshPlayabilityVisual(bool force = false)
    {
        if (cubeRenderer == null) return;
        if (gameCardManager == null) gameCardManager = FindAnyObjectByType<GameCardManager>();

        string resolvedName = !string.IsNullOrEmpty(cardName)
            ? cardName
            : (cardData != null ? cardData.cardName : string.Empty);

        Color targetColor = unplayableColor;

        if (gameCardManager != null && !string.IsNullOrEmpty(resolvedName))
        {
            bool inDiscardMode = gameCardManager.discardController != null &&
                                 gameCardManager.discardController.isDiscardMode &&
                                 !gameCardManager.playerSkipNextTurn;

            if (inDiscardMode && gameCardManager.CanPlayerInteractNow())
            {
                targetColor = discardModeColor;
            }
            else
            {
                string ignoredReason;
                bool playable = gameCardManager.EvaluateCardPlayability(resolvedName, out ignoredReason);
                targetColor = playable ? playableColor : unplayableColor;
            }
        }

        if (!force && lastAppliedColor.HasValue && lastAppliedColor.Value == targetColor)
            return;

        lastAppliedColor = targetColor;
        cubeRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", targetColor);
        propBlock.SetColor("_BaseColor", targetColor);
        cubeRenderer.SetPropertyBlock(propBlock);
    }

    void TriggerRightClickOnce()
    {
        if (Time.frameCount == lastRightClickFrame) return;
        lastRightClickFrame = Time.frameCount;

        if (gameCardManager == null) gameCardManager = FindAnyObjectByType<GameCardManager>();
        if (gameCardManager != null && gameCardManager.discardController != null)
            gameCardManager.discardController.RegisterRightClickForDiscardMode();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
        {
            TriggerRightClickOnce();
            return;
        }

        TryActivateCard();
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(1))
        {
            TriggerRightClickOnce();
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        TryActivateCard();
    }

    public void TryActivateCard()
    {
        if (Time.frameCount == lastActivationFrame) return;
        lastActivationFrame = Time.frameCount;

        if (gameCardManager == null) gameCardManager = FindAnyObjectByType<GameCardManager>();
        if (gameCardManager == null) return;

        string resolvedName = !string.IsNullOrEmpty(cardName)
            ? cardName
            : (cardData != null ? cardData.cardName : string.Empty);

        if (string.IsNullOrEmpty(resolvedName)) return;

        if (gameCardManager.playerSkipNextTurn)
        {
            if (gameCardManager.discardController != null)
                gameCardManager.discardController.ForceExitDiscardMode("Turn skipped");
            Debug.LogWarning($"'{resolvedName}': turn skipped");
            RefreshPlayabilityVisual(force: true);
            return;
        }

        if (gameCardManager.discardController != null && gameCardManager.discardController.isDiscardMode)
        {
            gameCardManager.discardController.TryDiscardCardByName(resolvedName, gameObject);
            RefreshPlayabilityVisual(force: true);
            return;
        }

        string reason;
        if (!gameCardManager.EvaluateCardPlayability(resolvedName, out reason))
        {
            Debug.LogWarning($"'{resolvedName}': {reason}");
            RefreshPlayabilityVisual(force: true);
            return;
        }

        gameCardManager.ProcessCardPlay(resolvedName);
        RefreshPlayabilityVisual(force: true);
    }
}