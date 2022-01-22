using UnityEngine;

public class CameraRotationAndZoom : MonoBehaviour
{
    public float mouseSensitivity = 100f;

    public Transform Observer;

    float xRotation = 0f;

    public float panSpeed = 20f;

    public float minimumY = 40;
    public float maximumY = 800;

    public bool toggleRot = true;

    public GameObject Panel;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            toggleRot = !toggleRot;
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            if(Panel != null)
            {
                bool isActive = Panel.activeSelf;
                Panel.SetActive(!isActive);
            }
        }

        //Quaternion rot = transform.rotation;
        if (toggleRot == true)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;

            xRotation = Mathf.Clamp(xRotation, -15f, 90f);

            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            Observer.Rotate(Vector3.up * mouseX);
        }

        // move the camera up and down with keys
        Vector3 pos = transform.position;
        if (Input.GetKey(KeyCode.Space))
        {
            if (pos.y >= 70f)
                pos.y += panSpeed * 10f * Time.deltaTime;
            else
                pos.y += panSpeed * 2f * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (pos.y >= 70f)
                pos.y -= panSpeed * 10f * Time.deltaTime;
            else
                pos.y -= panSpeed * 2f * Time.deltaTime;
        }
        pos.y = Mathf.Clamp(pos.y, minimumY, maximumY);
        transform.position = pos;
    }
}
