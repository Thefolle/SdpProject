using UnityEngine;

public class ObserverMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform Camera;
    public float speed = 20f;

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        float y = Camera.position.y;

        Vector3 move = transform.right * x + transform.forward * z;
        if (y >= 500f)
            controller.Move(move * speed * 20f * Time.deltaTime);
        else if (y >= 70f && y < 500f)
            controller.Move(move * speed * 10f * Time.deltaTime);
        else
            controller.Move(move * speed * 2f * Time.deltaTime);
    }
}
