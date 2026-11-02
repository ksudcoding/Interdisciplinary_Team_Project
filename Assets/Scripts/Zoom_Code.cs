using UnityEngine;

public class Zoom_Code : MonoBehaviour
{


    public float maxFOV;
    public float minFOV;
    public float sensitivity;
    public float FOV;




    // Update is called once per frame
    void Update()
    {
        //FOV = Camera.main.fieldOfView;
        //FOV += (Input.GetAxis("Mouse ScrollWheel") * sensitivity) * -1;
        //FOV = Mathf.Clamp(FOV, minFOV, maxFOV);
        //Camera.main.fieldOfView = FOV;
    }
}
