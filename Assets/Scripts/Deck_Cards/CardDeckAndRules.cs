using System.Collections.Generic;
using UnityEngine;

public enum GameCardType { Potential, Action }
public enum TurnState { PlayerTurn, BotTurn }
public enum TurnPhase { PlayPhase, DiscardPhase, RefillPhase }
public enum BoardStage { AxonHillock, PinkThreshold, PeachDepol, MagentaRepol, BlueHyperpol, GreenRest, YellowPTrack, Finish }

[System.Serializable]
public class GameCard
{
    public string card_Id;
    public string cardName;
    public GameCardType type;
    [Tooltip("Turn number when drawn. Cannot be played on the same turn.")]
    public int drawnOnTurn = -1;
}

[System.Serializable]
public class CardPrefabEntry
{
    public string cardName;
    public GameObject cardPrefab;
}

public class CardDeckAndRules : MonoBehaviour
{
    [Header("Board Zone Index Boundaries")]
    public int startIndex = 1;
    public int lastPinkIndex = 8;
    public int lastPeachIndex = 13;
    public int lastMagentaIndex = 20;
    public int lastBlueIndex = 27;
    public int greenRestIndex = 28;
    public int lastPTrackIndex = 47;

    [Header("Deck & Discard Piles")]
    public int potentialDeckCount;
    public int actionDeckCount;
    public List<GameCard> potentialDrawDeck = new List<GameCard>();
    public List<GameCard> actionDrawDeck = new List<GameCard>();
    public List<GameCard> potentialDiscardPile = new List<GameCard>();
    public List<GameCard> actionDiscardPile = new List<GameCard>();

    void Update()
    {
        potentialDeckCount = potentialDrawDeck.Count;
        actionDeckCount = actionDrawDeck.Count;
    }

    public void InitializeAndShuffleDecks()
    {
        potentialDrawDeck.Clear();
        actionDrawDeck.Clear();
        potentialDiscardPile.Clear();
        actionDiscardPile.Clear();

        AddPotentialCards("Weak Stimulus", 4);
        AddPotentialCards("Strong Stimulus", 4);
        AddPotentialCards("Na+ Channel OPEN", 8);
        AddPotentialCards("Na+ Channel OPEN AND K+ Channel CLOSED", 8);
        AddPotentialCards("Na+ Channel INACTIVE AND K+ Channel OPEN", 8);
        AddPotentialCards("Na+ Channel CLOSED AND K+ Channel OPEN", 8);
        AddPotentialCards("Na+ Channel CLOSED AND K+ Channel CLOSED", 6);
        AddPotentialCards("Propagation", 12);
        AddPotentialCards("Potential Joker", 2);

        AddActionCards("Caffeine", 4);
        AddActionCards("Alcohol", 4);
        AddActionCards("Myelinated Axon", 4);
        AddActionCards("Large Axon", 4);
        AddActionCards("Narrow Axon", 3);
        AddActionCards("Demyelinated Axon", 3);
        AddActionCards("Absolute Refractory Period", 4);
        AddActionCards("Relative Refractory Period", 4);
        AddActionCards("Tetrodotoxin", 4);
        AddActionCards("Action Joker", 2);

        ShuffleList(potentialDrawDeck);
        ShuffleList(actionDrawDeck);
    }

    void AddPotentialCards(string cardName, int count)
    {
        for (int i = 0; i < count; i++)
        {
            potentialDrawDeck.Add(new GameCard
            {
                card_Id = $"POT_{cardName.ToUpper().Replace(" ", "_")}_{i + 1}",
                cardName = cardName,
                type = GameCardType.Potential,
                drawnOnTurn = -1
            });
        }
    }

    void AddActionCards(string cardName, int count)
    {
        for (int i = 0; i < count; i++)
        {
            actionDrawDeck.Add(new GameCard
            {
                card_Id = $"ACT_{cardName.ToUpper().Replace(" ", "_")}_{i + 1}",
                cardName = cardName,
                type = GameCardType.Action,
                drawnOnTurn = -1
            });
        }
    }

    public GameCard DrawFromDeck(GameCardType type)
    {
        List<GameCard> deck = (type == GameCardType.Potential) ? potentialDrawDeck : actionDrawDeck;
        List<GameCard> discard = (type == GameCardType.Potential) ? potentialDiscardPile : actionDiscardPile;

        if (deck.Count == 0)
        {
            if (discard.Count == 0) return null;
            deck.AddRange(discard);
            discard.Clear();
            ShuffleList(deck);
        }

        GameCard top = deck[0];
        deck.RemoveAt(0);
        return top;
    }

    public BoardStage GetStageAtIndex(int index)
    {
        if (index <= 0) return BoardStage.AxonHillock;
        if (index <= lastPinkIndex) return BoardStage.PinkThreshold;
        if (index <= lastPeachIndex) return BoardStage.PeachDepol;
        if (index <= lastMagentaIndex) return BoardStage.MagentaRepol;
        if (index <= lastBlueIndex) return BoardStage.BlueHyperpol;
        if (index == greenRestIndex) return BoardStage.GreenRest;
        if (index <= lastPTrackIndex) return BoardStage.YellowPTrack;
        return BoardStage.Finish;
    }

    public string GetRequiredPotentialCardForNextSpace(BoardStage nextStage)
    {
        switch (nextStage)
        {
            case BoardStage.PinkThreshold:
                return "Na+ Channel OPEN";
            case BoardStage.PeachDepol:
                return "Na+ Channel OPEN AND K+ Channel CLOSED";
            case BoardStage.MagentaRepol:
                return "Na+ Channel INACTIVE AND K+ Channel OPEN";
            case BoardStage.BlueHyperpol:
                return "Na+ Channel CLOSED AND K+ Channel OPEN";
            case BoardStage.GreenRest:
                return "Na+ Channel CLOSED AND K+ Channel CLOSED";
            case BoardStage.YellowPTrack:
            case BoardStage.Finish:
                return "Propagation";
            default:
                return "Strong Stimulus";
        }
    }

    public bool CanPlayActionSlowdownOnStage(string actionCardName, BoardStage targetStage)
    {
        if (actionCardName == "Alcohol")
            return targetStage == BoardStage.PinkThreshold;

        if (actionCardName == "Absolute Refractory Period")
            return targetStage == BoardStage.PeachDepol || targetStage == BoardStage.MagentaRepol;

        if (actionCardName == "Relative Refractory Period")
            return targetStage == BoardStage.BlueHyperpol;

        if (actionCardName == "Demyelinated Axon" || actionCardName == "Narrow Axon")
            return targetStage == BoardStage.YellowPTrack;

        return false;
    }

    public void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}