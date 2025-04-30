using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    

    private CharacterController controller;
    private Vector3 moveDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogWarning("No CharacterController found. Please add one to the Player.");
        }
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D or ←/→
        float moveZ = Input.GetAxis("Vertical");   // W/S or ↑/↓

        moveDirection = new Vector3(moveX, 0f, moveZ);

        if (controller != null)
        {
            controller.Move(moveDirection * moveSpeed * Time.deltaTime);
        }
        else
        {
            // fallback if no CharacterController (use Transform instead)
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}

