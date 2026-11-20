using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InGameConsoleWindow : MonoBehaviour
{
    private struct LogEntry
    {
        public string rawKey;
        public string body;
        public LogType type;
        public int repeatCount;
    }

    [Header("UI References")]
    public TMP_Text consoleText;
    public GameObject panelRoot;

    [Header("Left Black Box Layout")]
    public bool dockPanelToLeftEdge = true;
    public float panelWidth = 260f;
    public float panelLeftMargin = 10f;
    public float panelTopMargin = 10f;
    public float panelBottomMargin = 110f;
    public bool ensureRectMask2D = true;

    [Header("Text Inside Box")]
    public bool fitTextRectToPanel = true;
    [Range(0f, 24f)]
    public float horizontalPadding = 8f;
    [Range(0f, 24f)]
    public float verticalPadding = 8f;
    [Range(3, 80)]
    public int maxLines = 22;
    [Min(6f)]
    public float fontSize = 11f;
    [Range(-40f, 20f)]
    public float lineSpacing = -8f;
    [Tooltip("Inserts a line break onto the next line inside the box after this many characters (0 = disabled).")]
    [Range(0, 120)]
    public int wrapAtChars = 34;
    public bool condenseLongLogs = true;
    public bool collapseDuplicates = true;
    public bool wrapLongLines = true;
    public bool colorCodeLogTypes = true;
    public KeyCode toggleKey = KeyCode.BackQuote;

    [Header("Message Colors")]
    public string bobColorHex = "#5DADE2";
    public string playerPlayColorHex = "#58D68D";
    public string badMessageColorHex = "#EC7063";
    public string defaultColorHex = "#EAECEE";

    private readonly List<LogEntry> entries = new List<LogEntry>();
    private string cachedOutput = string.Empty;

    void Awake()
    {
        if (consoleText == null)
            consoleText = GetComponentInChildren<TMP_Text>();

        if (panelRoot == null && consoleText != null)
            panelRoot = consoleText.transform.parent != null ? consoleText.transform.parent.gameObject : consoleText.gameObject;

        ApplyBoxAndTextLayout();
    }

    void Start()
    {
        ApplyBoxAndTextLayout();
        RebuildText();
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleUnityLog;
        ApplyBoxAndTextLayout();
        RebuildText();
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleUnityLog;
    }

    void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey) && panelRoot != null)
        {
            panelRoot.SetActive(!panelRoot.activeSelf);
        }
    }

    void ApplyBoxAndTextLayout()
    {
        if (panelRoot != null)
        {
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            if (panelRect != null && dockPanelToLeftEdge)
            {
                panelRect.anchorMin = new Vector2(0f, 0f);
                panelRect.anchorMax = new Vector2(0f, 1f);
                panelRect.pivot = new Vector2(0f, 0.5f);
                panelRect.offsetMin = new Vector2(panelLeftMargin, panelBottomMargin);
                panelRect.offsetMax = new Vector2(panelLeftMargin + panelWidth, -panelTopMargin);
                panelRect.localScale = Vector3.one;
                panelRect.localRotation = Quaternion.identity;
            }

            if (ensureRectMask2D && panelRoot.GetComponent<RectMask2D>() == null)
            {
                panelRoot.AddComponent<RectMask2D>();
            }
        }

        if (consoleText == null) return;

        if (fitTextRectToPanel && panelRoot != null)
        {
            RectTransform textRect = consoleText.rectTransform;
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();

            if (textRect != null && panelRect != null)
            {
                if (textRect.parent != panelRect)
                    textRect.SetParent(panelRect, false);

                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.pivot = new Vector2(0f, 0f);
                textRect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
                textRect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
                textRect.localScale = Vector3.one;
                textRect.localRotation = Quaternion.identity;
            }
        }

        consoleText.alignment = TextAlignmentOptions.BottomLeft;
        consoleText.fontStyle = FontStyles.Normal;
        consoleText.fontSize = fontSize;
        consoleText.lineSpacing = lineSpacing;
        consoleText.textWrappingMode = wrapLongLines ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        consoleText.overflowMode = TextOverflowModes.Overflow;

        Canvas.ForceUpdateCanvases();
    }

    string CondenseMessage(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return string.Empty;

        string clean = msg.Trim().Replace("\r", " ").Replace("\n", " ");

        if (condenseLongLogs)
        {
            clean = clean.Replace("[BotController] ", "Bob: ");
            clean = clean.Replace("[Bob] ", "Bob: ");
            clean = clean.Replace("Bob played card ", "Bob played ");
            clean = clean.Replace("Bob discarded card ", "Bob disc ");
            clean = clean.Replace("Bob skipped turn", "Bob skip");
            clean = clean.Replace("[UICard] Cannot play ", "");
            clean = clean.Replace("[CardHover] Blocked ", "");
            clean = clean.Replace("[GameCardManager] Blocked ", "");
            clean = clean.Replace("You are on the Axon Hillock; play Weak Stimulus or Strong Stimulus to enter START.", "need Weak / Strong Stim");
            clean = clean.Replace("You are on the Axon Hillock, play Weak Stimulus or Strong Stimulus to enter START.", "need Weak / Strong Stim");
            clean = clean.Replace("need Weak/Strong Stim", "need Weak / Strong Stim");
            clean = clean.Replace("This card was drawn this turn and cannot be played until a later turn.", "drawn this turn");
            clean = clean.Replace("You are not in the Play Phase.", "not Play Phase");
            clean = clean.Replace("Absolute Refractory Period", "Abs Refractory");
            clean = clean.Replace("Relative Refractory Period", "Rel Refractory");
            clean = clean.Replace("Demyelinated Axon", "Demyelinated");
            clean = clean.Replace("Myelinated Axon", "Myelinated");
            clean = clean.Replace("Potential Joker", "Pot Joker");
            clean = clean.Replace("Strong Stimulus", "Strong Stim");
            clean = clean.Replace("Weak Stimulus", "Weak Stim");
            clean = clean.Replace("Na+ Channel OPEN AND K+ Channel CLOSED", "Na+ Open / K+ Closed");
            clean = clean.Replace("Na+ Channel INACTIVE AND K+ Channel OPEN", "Na+ Inact / K+ Open");
            clean = clean.Replace("Na+ Channel CLOSED AND K+ Channel", "Na+ Closed / K+");
            clean = clean.Replace("locked after Weak Stim", "locked (Weak Stim)");
        }

        return clean;
    }

    string WrapPlainText(string text, int maxWidthChars)
    {
        if (string.IsNullOrEmpty(text) || maxWidthChars <= 4 || text.Length <= maxWidthChars)
            return text;

        StringBuilder sb = new StringBuilder(text.Length + 8);
        int currentLen = 0;
        string[] words = text.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (string.IsNullOrEmpty(word)) continue;

            if (word.Length > maxWidthChars)
            {
                for (int offset = 0; offset < word.Length; offset += maxWidthChars)
                {
                    int chunkLen = Mathf.Min(maxWidthChars, word.Length - offset);
                    if (currentLen > 0)
                    {
                        sb.Append('\n');
                        currentLen = 0;
                    }
                    sb.Append(word.Substring(offset, chunkLen));
                    currentLen = chunkLen;
                }
                continue;
            }

            int needed = (currentLen == 0) ? word.Length : currentLen + 1 + word.Length;
            if (needed > maxWidthChars && currentLen > 0)
            {
                sb.Append('\n');
                sb.Append(word);
                currentLen = word.Length;
            }
            else
            {
                if (currentLen > 0)
                {
                    sb.Append(' ');
                    currentLen++;
                }
                sb.Append(word);
                currentLen += word.Length;
            }
        }

        return sb.ToString();
    }

    void HandleUnityLog(string condition, string stackTrace, LogType type)
    {
        string shortened = CondenseMessage(condition);
        if (string.IsNullOrEmpty(shortened)) return;

        if (collapseDuplicates && entries.Count > 0)
        {
            int lastIdx = entries.Count - 1;
            LogEntry last = entries[lastIdx];
            if (last.rawKey == shortened && last.type == type)
            {
                last.repeatCount++;
                entries[lastIdx] = last;
                RebuildText();
                return;
            }
        }

        entries.Add(new LogEntry
        {
            rawKey = shortened,
            body = shortened,
            type = type,
            repeatCount = 1
        });

        while (entries.Count > maxLines)
        {
            entries.RemoveAt(0);
        }

        RebuildText();
    }

    public void LogBotMove(string actionSummary, bool isWarning = false)
    {
        if (string.IsNullOrWhiteSpace(actionSummary)) return;
        string formatted = actionSummary.TrimStart().StartsWith("Bob")
            ? actionSummary.Trim()
            : $"Bob: {actionSummary.Trim()}";
        HandleUnityLog(formatted, string.Empty, isWarning ? LogType.Warning : LogType.Log);
    }

    public void LogToScreen(string message, LogType type = LogType.Log)
    {
        HandleUnityLog(message, string.Empty, type);
    }

    public void ClearWindow()
    {
        entries.Clear();
        cachedOutput = string.Empty;
        if (consoleText != null)
        {
            consoleText.text = string.Empty;
            Canvas.ForceUpdateCanvases();
            consoleText.ForceMeshUpdate(true, true);
        }
    }

    string ResolveEntryColor(LogEntry entry)
    {
        string lower = entry.body.ToLowerInvariant();
        bool isBob = lower.StartsWith("bob:") || lower.StartsWith("bob ");
        bool isBad = entry.type == LogType.Warning ||
                     entry.type == LogType.Error ||
                     entry.type == LogType.Exception ||
                     entry.type == LogType.Assert ||
                     lower.Contains("need ") ||
                     lower.Contains("not your turn") ||
                     lower.Contains("turn skipped") ||
                     lower.Contains("drawn this turn") ||
                     lower.Contains("not play phase") ||
                     lower.Contains("locked") ||
                     lower.Contains("max ");

        if (isBob) return bobColorHex;
        if (isBad) return badMessageColorHex;
        if (entry.type == LogType.Log) return playerPlayColorHex;
        return defaultColorHex;
    }

    string FormatEntry(LogEntry entry)
    {
        string text = entry.body;

        if (entry.repeatCount > 1)
            text = $"{text} (x{entry.repeatCount})";

        if (wrapAtChars > 0)
            text = WrapPlainText(text, wrapAtChars);

        if (colorCodeLogTypes)
        {
            string hex = ResolveEntryColor(entry);
            return $"<color={hex}>{text}</color>";
        }

        return text;
    }

    void RebuildText()
    {
        if (consoleText == null) return;

        ApplyBoxAndTextLayout();

        List<string> lines = new List<string>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            lines.Add(FormatEntry(entries[i]));
        }

        string joined = string.Join("\n", lines);
        if (joined == cachedOutput) return;

        cachedOutput = joined;
        consoleText.text = cachedOutput;
        Canvas.ForceUpdateCanvases();
        consoleText.ForceMeshUpdate(true, true);
    }
}