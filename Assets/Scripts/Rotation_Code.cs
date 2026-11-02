using UnityEngine;
using UnityEngine.InputSystem
public class Rotation_Code : MonoBehaviour
{



    public GameObject target;
    public float rotationSpeed = 20f;
    public float distanceFromTarget = 10f;
    public float offsetY = 5f;

    public float scrollSensitvity = 1f;
    
    public float maxLimit = 25f;
    public float minLimit = 1f;
    public float scrollSensitivity = 1f;
    public float myNumber = 10f;

    void Start()
    {
        Vector3 startPosition = target.transform.position - (Vector3.forward * distanceFromTarget);
        startPosition.y += offsetY;
        transform.position = startPosition;

    }
    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(target.transform.position, Vector3.up, rotationSpeed * Time.deltaTime);
        transform.LookAt(target.transform.position);

        float scrollInput = Mouse.current.scroll.ReadValue().y;
        myNumber += scrollInput * scrollSensitivity;
        myNumber = Mathf.Clamp(myNumber, minLimit, maxLimit);

    }

}
