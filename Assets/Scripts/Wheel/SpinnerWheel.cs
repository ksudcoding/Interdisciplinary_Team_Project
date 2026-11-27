using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinnerWheel : MonoBehaviour
{
    [Serializable]
    public struct WheelColorSlice
    {
        public string colorName;
        [Tooltip("Start angle in degrees (0-360)")]
        public float startAngle;
        [Tooltip("End angle in degrees (0-360, wrapping past 360 is supported e.g. 330 to 30)")]
        public float endAngle;
    }

    public enum StartingStimulusChoice
    {
        None,
        WeakStimulus,
        StrongStimulus
    }

    [Header("Spinner Target & Visibility")]
    [Tooltip("Leave empty to rotate this GameObject")]
    public Transform wheelTransform;
    [Tooltip("Parent/root object containing both the wheel and arrow (defaults to wheelTransform's GameObject)")]
    public GameObject wheelRootObject;
    [Tooltip("Optional extra objects (such as a separate arrow root) to hide/move with the wheel")]
    public List<GameObject> extraObjectsToDismiss = new List<GameObject>();
    [Tooltip("The Arrow object. Leave empty to auto-find a child named 'Arrow' under the wheel.")]
    public GameObject arrowObject;
    [Tooltip("Safety net: active scene objects with these names are also hidden when the wheel is dismissed.")]
    public string[] hideByName = new string[] { "Wheel", "Arrow" };
    [Tooltip("Optional position/target to move the dismissed object(s) to")]
    public Transform postSpinAnchor;
    public bool hideWheelAfterSpin = true;
    public float postSpinDelay = 0.75f;

    [Header("Spin Settings")]
    public float minSpinDuration = 2.0f;
    public float maxSpinDuration = 3.5f;
    public int minExtraRotations = 4;
    public int maxExtraRotations = 8;
    public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Set to None so the wheel can't be spun manually during setup")]
    public KeyCode spinKey = KeyCode.None;

    [Header("Color Lookup")]
    [Tooltip("Added to the landed angle before the colour is looked up. 180 swaps Yellow and Pink. 0 = no change. Use 90 or 270 if the split is sideways.")]
    public float angleOffset = 180f;

    [Header("Color Slices (0-360 deg)")]
    public List<WheelColorSlice> colorSlices = new List<WheelColorSlice>
    {
        new WheelColorSlice { colorName = "Yellow", startAngle = 0f, endAngle = 180f },
        new WheelColorSlice { colorName = "Pink", startAngle = 180f, endAngle = 360f }
    };

    public bool IsSpinning { get; private set; }
    public bool HasCompletedInitialSpin { get; private set; }
    public float LastLandedAngle { get; private set; }
    public string LastLandedColor { get; private set; }
    public StartingStimulusChoice LastStartingStimulus { get; private set; }

    public event Action<StartingStimulusChoice, string, float> OnInitialSpinCompleted;

    private Coroutine activeSpinCoroutine;
    private float currentZAngle;

    void Awake()
    {
        if (wheelTransform == null)
            wheelTransform = transform;

        if (wheelRootObject == null && wheelTransform != null)
            wheelRootObject = wheelTransform.gameObject;

        if (arrowObject == null)
        {
            Transform searchRoot = wheelRootObject != null ? wheelRootObject.transform : wheelTransform;
            foreach (Transform t in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals("Arrow", StringComparison.OrdinalIgnoreCase))
                {
                    arrowObject = t.gameObject;
                    break;
                }
            }
        }

        currentZAngle = NormalizeAngle(wheelTransform.localEulerAngles.z);
    }

    void Update()
    {
        if (!IsSpinning && spinKey != KeyCode.None && Input.GetKeyDown(spinKey) && !HasCompletedInitialSpin)
            SpinWheel();
    }

    [ContextMenu("Spin Wheel")]
    public void SpinWheel()
    {
        if (IsSpinning) return;
        if (activeSpinCoroutine != null) StopCoroutine(activeSpinCoroutine);
        activeSpinCoroutine = StartCoroutine(SpinRoutine(null, false, "Manual Spin"));
    }

    public IEnumerator SpinForStartingStimulusRoutine(
        Action<StartingStimulusChoice> onComplete = null,
        bool dismissAfterSpin = false,
        string spinnerOwnerLabel = "Player Spin")
    {
        while (IsSpinning)
            yield return null;

        HasCompletedInitialSpin = false;
        LastStartingStimulus = StartingStimulusChoice.None;

        SetWheelAssemblyActive(true);

        if (activeSpinCoroutine != null)
            StopCoroutine(activeSpinCoroutine);

        activeSpinCoroutine = StartCoroutine(SpinRoutine(onComplete, dismissAfterSpin, spinnerOwnerLabel));

        // Wait on the flag, not the coroutine object (a stopped coroutine never resumes the waiter)
        float safety = 15f;
        while (!HasCompletedInitialSpin && safety > 0f)
        {
            safety -= Time.deltaTime;
            yield return null;
        }
    }

    // Wheel root, the rotating wheel, the arrow and any extras - all treated as one unit
    private void ForEachAssemblyObject(Action<GameObject> action)
    {
        HashSet<GameObject> seen = new HashSet<GameObject>();

        void Visit(GameObject g)
        {
            if (g != null && seen.Add(g)) action(g);
        }

        Visit(wheelRootObject);
        if (wheelTransform != null) Visit(wheelTransform.gameObject);
        Visit(arrowObject);
        if (extraObjectsToDismiss != null)
        {
            for (int i = 0; i < extraObjectsToDismiss.Count; i++)
                Visit(extraObjectsToDismiss[i]);
        }
    }

    public void SetWheelAssemblyActive(bool active)
    {
        ForEachAssemblyObject(g =>
        {
            if (g.activeSelf != active) g.SetActive(active);
        });
    }

    /// <summary>Hides the wheel AND arrow together, regardless of the hideWheelAfterSpin tickbox.</summary>
    public void HideWheelCompletely()
    {
        List<string> hidden = new List<string>();

        // 1. Everything this script knows about (root, wheel, arrow, extras)
        ForEachAssemblyObject(g =>
        {
            if (g.activeSelf)
            {
                g.SetActive(false);
                hidden.Add(g.name);
            }
        });

        // 2. Safety net: any active scene object with one of these names (catches a wheel
        //    that isn't assigned in the Inspector, or a duplicate)
        if (hideByName != null)
        {
            for (int n = 0; n < hideByName.Length; n++)
            {
                if (string.IsNullOrWhiteSpace(hideByName[n])) continue;

                GameObject found;
                int guard = 8;
                while (guard-- > 0 && (found = GameObject.Find(hideByName[n])) != null)
                {
                    found.SetActive(false);
                    hidden.Add(found.name);
                }
            }
        }

        Debug.Log(hidden.Count > 0
            ? $"[SpinnerWheel] Hid: {string.Join(", ", hidden)}"
            : "[SpinnerWheel] HideWheelCompletely ran but nothing was active to hide.");
    }

    public void DismissWheel()
    {
        if (postSpinAnchor != null)
        {
            if (wheelRootObject != null)
            {
                wheelRootObject.transform.position = postSpinAnchor.position;
                wheelRootObject.transform.rotation = postSpinAnchor.rotation;
            }

            if (extraObjectsToDismiss != null)
            {
                for (int i = 0; i < extraObjectsToDismiss.Count; i++)
                {
                    GameObject obj = extraObjectsToDismiss[i];
                    if (obj != null)
                    {
                        obj.transform.position = postSpinAnchor.position;
                        obj.transform.rotation = postSpinAnchor.rotation;
                    }
                }
            }
        }

        if (hideWheelAfterSpin)
            SetWheelAssemblyActive(false);
    }

    private IEnumerator SpinRoutine(Action<StartingStimulusChoice> onComplete, bool dismissAfterSpin, string spinnerOwnerLabel)
    {
        IsSpinning = true;

        float duration = UnityEngine.Random.Range(minSpinDuration, maxSpinDuration);
        int extraTurns = UnityEngine.Random.Range(minExtraRotations, maxExtraRotations + 1);
        float randomFinalOffset = UnityEngine.Random.Range(0f, 360f);

        float startZ = currentZAngle;
        float totalDeltaZ = (extraTurns * 360f) + randomFinalOffset;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = spinCurve.Evaluate(t);

            float angleNow = startZ + (totalDeltaZ * curvedT);
            Vector3 euler = wheelTransform.localEulerAngles;
            euler.z = NormalizeAngle(angleNow);
            wheelTransform.localEulerAngles = euler;

            yield return null;
        }

        currentZAngle = NormalizeAngle(startZ + totalDeltaZ);
        Vector3 finalEuler = wheelTransform.localEulerAngles;
        finalEuler.z = currentZAngle;
        wheelTransform.localEulerAngles = finalEuler;

        LastLandedAngle = currentZAngle;
        LastLandedColor = GetColorAtAngle(LastLandedAngle);

        StartingStimulusChoice stimulus = MapColorToStimulus(LastLandedColor);
        LastStartingStimulus = stimulus;
        HasCompletedInitialSpin = true;
        IsSpinning = false;
        activeSpinCoroutine = null;

        string stimulusLabel = stimulus == StartingStimulusChoice.StrongStimulus
            ? "Strong stimulus"
            : stimulus == StartingStimulusChoice.WeakStimulus
                ? "Weak stimulus"
                : LastLandedColor;

        string ownerTag = string.IsNullOrWhiteSpace(spinnerOwnerLabel) ? "Spinner" : spinnerOwnerLabel;
        Debug.Log($"[SpinnerWheel - {ownerTag}] Landed at angle: {LastLandedAngle:F1}° | Color: {LastLandedColor} | Stimulus: {stimulusLabel}");

        if (dismissAfterSpin && postSpinDelay > 0f)
            yield return new WaitForSeconds(postSpinDelay);

        if (dismissAfterSpin)
            DismissWheel();

        onComplete?.Invoke(stimulus);
        OnInitialSpinCompleted?.Invoke(stimulus, LastLandedColor, LastLandedAngle);
    }

    private StartingStimulusChoice MapColorToStimulus(string colorName)
    {
        if (string.IsNullOrWhiteSpace(colorName))
            return StartingStimulusChoice.WeakStimulus;

        if (colorName.Trim().Equals("Yellow", StringComparison.OrdinalIgnoreCase))
            return StartingStimulusChoice.StrongStimulus;

        if (colorName.Trim().Equals("Pink", StringComparison.OrdinalIgnoreCase))
            return StartingStimulusChoice.WeakStimulus;

        return StartingStimulusChoice.WeakStimulus;
    }

    private string GetColorAtAngle(float angle)
    {
        float a = NormalizeAngle(angle + angleOffset);

        for (int i = 0; i < colorSlices.Count; i++)
        {
            WheelColorSlice slice = colorSlices[i];
            float start = NormalizeAngle(slice.startAngle);
            float end = NormalizeAngle(slice.endAngle);

            if (Mathf.Approximately(slice.startAngle, 0f) && Mathf.Approximately(slice.endAngle, 360f))
                return slice.colorName;

            if (start <= end)
            {
                if (a >= start && a < end)
                    return slice.colorName;
            }
            else
            {
                if (a >= start || a < end)
                    return slice.colorName;
            }
        }

        return "Unknown";
    }

    private static float NormalizeAngle(float degrees)
    {
        float mod = degrees % 360f;
        return mod < 0f ? mod + 360f : mod;
    }
}