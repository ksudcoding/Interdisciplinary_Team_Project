using System.Collections;
using UnityEngine;

public class Position : MonoBehaviour
{
    public Vector3[] positions = new Vector3[]
    {
        new Vector3(26.071f, 0.0f, 3.91f),
        new Vector3(23.735f, 0.0f, 3.726f),
        new Vector3(21.311f, 0.0f, 3.322f),
        new Vector3(18.938f, 0.0f, 2.911f),
        new Vector3(16.55f, 0.0f, 2.588f),
        new Vector3(14.17f, 0.0f, 2.206f),
        new Vector3(11.797f, 0.0f, 1.831f),
        new Vector3(9.395f, 0.0f, 1.368f),
        new Vector3(6.956f, 0.0f, 1.118f),
        new Vector3(5.246f, 0.0f, -3.304f),
        new Vector3(3.561f, 0.0f, -5.811f),
        new Vector3(1.908f, 0.0f, -8.254f),
        new Vector3(0.06f, 0.0f, -10.81f),
        new Vector3(-1.7f, 0.0f, -13.53f),
        new Vector3(-3.58f, 0.0f, -11.64f),
        new Vector3(-6.68f, 0.0f, -8.28f),
        new Vector3(-9.3f, 0.0f, -5.42f),
        new Vector3(-11.38f, 0.0f, -2.46f),
        new Vector3(-13.27f, 0.0f, -0.31f),
        new Vector3(-15.58f, 0.0f, 1.36f),
        new Vector3(-18.72f, 0.0f, 3.66f),
        new Vector3(-21.29f, 0.0f, 4.78f),
        new Vector3(-22.99f, 0.0f, 5.59f),
        new Vector3(-24.79f, 0.0f, 6.59f),
        new Vector3(-26.63f, 0.0f, 7.3f),
        new Vector3(-28.49f, 0.0f, 6.56f),
        new Vector3(-30.5f, 0.0f, 5.49f),
        new Vector3(-32.31f, 0.0f, 4.59f),
        new Vector3(-34.49f, 0.0f, 3.97f),
        new Vector3(-32.278f, 0.0f, 16.179f),
        new Vector3(-29.171f, 0.0f, 16.179f),
        new Vector3(-26.171f, 0.0f, 16.179f),
        new Vector3(-23.171f, 0.0f, 16.179f),
        new Vector3(-20.171f, 0.0f, 16.179f),
        new Vector3(-16.921f, 0.0f, 16.179f),
        new Vector3(-13.921f, 0.0f, 16.179f),
        new Vector3(-10.671f, 0.0f, 16.179f),
        new Vector3(-7.421f, 0.0f, 16.179f),
        new Vector3(-4.421f, 0.0f, 16.179f),
        new Vector3(-1.171f, 0.0f, 16.179f),
        new Vector3(1.829f, 0.0f, 16.179f),
        new Vector3(5.079f, 0.0f, 16.179f),
        new Vector3(8.22f, 0.0f, 16.179f),
        new Vector3(11.47f, 0.0f, 16.179f),
        new Vector3(14.47f, 0.0f, 16.179f),
        new Vector3(17.72f, 0.0f, 16.179f),
        new Vector3(20.86f, 0.0f, 16.179f),
        new Vector3(23.98f, 0.0f, 16.179f),
        new Vector3(29.1f, 0.0f, 16.179f)
    };

    [Header("Movement Settings")]
    public float liftHeight = 1.5f;     
    public float moveSpeed = 10.0f;      

    [Header("Board Position Status")]
    public int currentIndex = 0;         // Shows the tile the token is currently on
    public int targetIndex = 0;          // Change this in the Inspector to your desired destination tile
    
    private bool isMoving = false;       
    private int lastObservedTargetIndex = 0;

    void Start()
    {
        if (positions.Length > 0)
        {
            transform.position = positions[currentIndex];
            targetIndex = currentIndex;
            lastObservedTargetIndex = currentIndex;
        }
    }

    void Update()
    {
        // If you change targetIndex in the Inspector while it's NOT moving, trigger the walk sequence
        if (targetIndex != lastObservedTargetIndex && !isMoving)
        {
            targetIndex = Mathf.Clamp(targetIndex, 0, positions.Length - 1);

            if (targetIndex != currentIndex)
            {
                StartCoroutine(MoveSequenceRoutine(targetIndex));
            }
            
            lastObservedTargetIndex = targetIndex;
        }
    }

    IEnumerator MoveSequenceRoutine(int destinationIndex)
    {
        isMoving = true;

        // Walk step-by-step until currentIndex matches the desired destinationIndex
        while (currentIndex != destinationIndex)
        {
            int nextStepIndex = (destinationIndex > currentIndex) ? currentIndex + 1 : currentIndex - 1;

            yield return StartCoroutine(MoveStepRoutine(positions[nextStepIndex]));
            
            currentIndex = nextStepIndex; // Update current tile progress
        }

        isMoving = false;
    }

    IEnumerator MoveStepRoutine(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;

        // Step 1: Move UP
        Vector3 liftPos = new Vector3(startPos.x, startPos.y + liftHeight, startPos.z);
        while (Vector3.Distance(transform.position, liftPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, liftPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // Step 2: Move ACROSS (Maintaining elevated Y)
        Vector3 acrossPos = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        while (Vector3.Distance(transform.position, acrossPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, acrossPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // Step 3: Move DOWN to final target position
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
    }
}