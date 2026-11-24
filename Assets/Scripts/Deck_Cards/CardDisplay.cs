using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CardDisplay : MonoBehaviour
{
    [Serializable]
    public struct CardArtworkEntry
    {
        public string cardName;
        public Texture artworkTexture;
    }

    [Header("Text")]
    public TextMeshPro cardNameText;
    public string cardName;

    [Header("Card Cube Artwork")]
    public MeshRenderer artworkMeshRenderer;
    public Texture fallbackTexture;
    public List<CardArtworkEntry> cardArtworks = new List<CardArtworkEntry>();

    private readonly HashSet<string> warnedMissingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string[]> CardAliasesToFileKeys =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Weak Stimulus", new[] { "WEAK STIM (STARTER CARD)" } },
            { "Weak Stim", new[] { "WEAK STIM (STARTER CARD)" } },
            { "Strong Stimulus", new[] { "strong stimulus joker card yellow" } },
            { "Strong Stim", new[] { "strong stimulus joker card yellow" } },
            { "Strong Stimulus Joker", new[] { "strong stimulus joker card yellow" } },
            { "Action Joker", new[] { "propagation card joker yellow", "strong stimulus joker card yellow" } },
            { "Potential Joker", new[] { "propagation card joker yellow", "strong stimulus joker card yellow" } },
            { "Pot Joker", new[] { "propagation card joker yellow", "strong stimulus joker card yellow" } },
            { "Propagation Joker", new[] { "propagation card joker yellow" } },
            { "Propagation", new[] { "propagation card joker yellow" } },

            { "Alcohol", new[] { "ALCAHOL" } },
            { "Alcahol", new[] { "ALCAHOL" } },
            { "Coffee", new[] { "COFFEE" } },
            { "Caffeine", new[] { "COFFEE" } },
            { "Tetrodotoxin", new[] { "TETRODOXIN (DEATH)" } },

            { "Absolute Refractory Period", new[] { "ABSOLUTE REFRAC", "ABSOLUTE REFRAC PINK" } },
            { "Abs Refractory", new[] { "ABSOLUTE REFRAC", "ABSOLUTE REFRAC PINK" } },
            { "Absolute Refractory Pink", new[] { "ABSOLUTE REFRAC PINK" } },
            { "Relative Refractory Period", new[] { "RELATIVE REFRAC BLUE" } },
            { "Rel Refractory", new[] { "RELATIVE REFRAC BLUE" } },
            { "Relative Refractory Blue", new[] { "RELATIVE REFRAC BLUE" } },

            { "Myelinated Axon", new[] { "MYELINATE AXON" } },
            { "Myelinated", new[] { "MYELINATE AXON" } },
            { "Yellow Myelinated Axon", new[] { "yellow myelinated axon" } },
            { "Demyelinated Axon", new[] { "DEMYELINATED AXON" } },
            { "Demyelinated", new[] { "DEMYELINATED AXON" } },
            { "Yellow Demyelinated Axon", new[] { "yellow demyelianted axon" } },
            { "Large Axon", new[] { "LARGE AXON" } },
            { "Yellow Large Axon", new[] { "yellow alrge axon" } },
            { "Narrow Axon", new[] { "NARROW AXON" } },
            { "Yellow Narrow Axon", new[] { "ywllow narrow axon" } },

            { "K+ closed", new[] { "k+ channel closed", "k+channel closed" } },
            { "K+ Channel Closed", new[] { "k+ channel closed", "k+channel closed" } },
            { "K+ open", new[] { "k + channel open magenta", "k+ channel open blue" } },
            { "K+ Channel Open Blue", new[] { "k+ channel open blue" } },
            { "K+ Channel Open Magenta", new[] { "k + channel open magenta" } },

            { "Na+ inactive", new[] { "na+ channel inactive magenta" } },
            { "Na+ Channel Inactive Magenta", new[] { "na+ channel inactive magenta" } },
            { "Na+ closed", new[] { "na+ channel closed" } },
            { "Na+ Channel Closed", new[] { "na+ channel closed" } },
            { "Na+ Channel Closed Blue", new[] { "na+ channel closed blue" } },
            { "Na+ open", new[] { "na channel open pink", "na channel open peacch" } },
            { "Na+ Channel Open", new[] { "na channel open pink", "na channel open peacch" } },
            { "Na+ Channel Open Pink", new[] { "na channel open pink" } },
            { "Na+ Channel Open Peach", new[] { "na channel open peacch" } },
            { "Na+ Channel OPEN AND K+ Channel CLOSED", new[] { "na channel open pink", "na channel open peacch" } },
            { "Na+ Channel INACTIVE AND K+ Channel OPEN", new[] { "na+ channel inactive magenta", "k + channel open magenta" } },
            { "Na+ Channel CLOSED AND K+ Channel OPEN", new[] { "na+ channel closed", "k+ channel open blue" } },
            { "Na+ Channel CLOSED AND K+ Channel CLOSED", new[] { "na+ channel closed", "k+ channel closed" } }
        };

    public void SetCardName(string nameToSet)
    {
        cardName = nameToSet;

        if (cardNameText == null)
            cardNameText = GetComponentInChildren<TextMeshPro>();

        if (cardNameText != null)
        {
            cardNameText.text = cardName;

            MeshRenderer textMeshRenderer = cardNameText.GetComponent<MeshRenderer>();
            if (textMeshRenderer != null)
                textMeshRenderer.sortingOrder = 9999;
        }

        ApplyArtwork(cardName);
    }

    public void ApplyArtwork(string lookupName)
    {
        if (artworkMeshRenderer == null)
        {
            Transform cubeChild = transform.Find("Cube");
            if (cubeChild != null)
                artworkMeshRenderer = cubeChild.GetComponent<MeshRenderer>();

            if (artworkMeshRenderer == null)
                artworkMeshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (artworkMeshRenderer == null)
            return;

        Texture matchedTexture = FindBestTexture(lookupName);

        if (matchedTexture != null)
            artworkMeshRenderer.material.mainTexture = matchedTexture;
    }

    private Texture FindBestTexture(string lookupName)
    {
        if (string.IsNullOrWhiteSpace(lookupName))
            return fallbackTexture;

        string targetNorm = NormalizeKey(lookupName);

        if (cardArtworks != null)
        {
            for (int i = 0; i < cardArtworks.Count; i++)
            {
                CardArtworkEntry entry = cardArtworks[i];
                if (!string.IsNullOrWhiteSpace(entry.cardName) &&
                    entry.artworkTexture != null &&
                    NormalizeKey(entry.cardName) == targetNorm)
                {
                    return entry.artworkTexture;
                }
            }

            for (int i = 0; i < cardArtworks.Count; i++)
            {
                CardArtworkEntry entry = cardArtworks[i];
                if (!string.IsNullOrWhiteSpace(entry.cardName) && entry.artworkTexture != null)
                {
                    string entryNorm = NormalizeKey(entry.cardName);
                    if (entryNorm == targetNorm)
                        return entry.artworkTexture;
                }
            }
        }

        if (CardAliasesToFileKeys.TryGetValue(lookupName.Trim(), out string[] aliasKeys) && cardArtworks != null)
        {
            for (int a = 0; a < aliasKeys.Length; a++)
            {
                string aliasKeyNorm = NormalizeKey(aliasKeys[a]);
                for (int i = 0; i < cardArtworks.Count; i++)
                {
                    CardArtworkEntry entry = cardArtworks[i];
                    if (entry.artworkTexture != null && NormalizeKey(entry.cardName) == aliasKeyNorm)
                        return entry.artworkTexture;
                }
            }
        }

        if (warnedMissingNames.Add(lookupName))
            Debug.LogWarning($"[CardDisplay] No matching artwork found for '{lookupName}'. Add it to Card Artworks or share the exact string.");

        return fallbackTexture;
    }

    private static string NormalizeKey(string value)
    {
        return value.Trim().ToLowerInvariant().Replace("  ", " ");
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Fill Card Artworks From Folder")]
    private void AutoFillCardArtworksFromFolder()
    {
        string[] candidateFolders = { "Assets/Assets/Cards", "Assets/Cards" };
        string foundFolder = null;

        foreach (string folder in candidateFolders)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                foundFolder = folder;
                break;
            }
        }

        if (foundFolder == null)
        {
            Debug.LogWarning("[CardDisplay] Could not find 'Assets/Assets/Cards' or 'Assets/Cards'.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { foundFolder });
        Dictionary<string, Texture> texturesByNormName = new Dictionary<string, Texture>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(path);
            if (tex != null)
            {
                string norm = NormalizeKey(fileNameWithoutExt);
                if (!texturesByNormName.ContainsKey(norm))
                    texturesByNormName[norm] = tex;
            }
        }

        cardArtworks.Clear();
        int matchedCount = 0;

        foreach (KeyValuePair<string, string[]> pair in CardAliasesToFileKeys)
        {
            Texture matchedTex = null;
            foreach (string candidateFileKey in pair.Value)
            {
                if (texturesByNormName.TryGetValue(NormalizeKey(candidateFileKey), out Texture tex))
                {
                    matchedTex = tex;
                    break;
                }
            }

            if (matchedTex != null)
            {
                cardArtworks.Add(new CardArtworkEntry
                {
                    cardName = pair.Key,
                    artworkTexture = matchedTex
                });
                matchedCount++;
            }
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"[CardDisplay] Populated {matchedCount} card entries from '{foundFolder}'.");
    }
#endif
}