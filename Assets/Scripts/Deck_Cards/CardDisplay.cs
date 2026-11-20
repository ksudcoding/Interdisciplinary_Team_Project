using UnityEngine;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    public TextMeshPro cardNameText;
    public string cardName;

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
    }
}