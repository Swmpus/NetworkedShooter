using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    private float runningSpeed = 5f;
    private Vector2 horizontalInput;
    private Rigidbody rb;
    private float distToGround;
    private CoreController cc;
    private int movementUpdateRate = 10;
    private int maxCountdownToUpdate;
    private int actualCountdown;

    private void Start()
    {
        float framerate = 1f / Time.fixedDeltaTime;
        maxCountdownToUpdate = (int)framerate / movementUpdateRate;
        actualCountdown = maxCountdownToUpdate;
        rb = GetComponent<Rigidbody>();
        distToGround = GetComponent<Collider>().bounds.extents.y;
        cc = GameObject.Find("GameMaster").GetComponent<CoreController>();
    }

    public void ReceiveInput(Vector2 input)
    {
        horizontalInput = input;
    }

    private bool isGrounded()
    {
        return Physics.Raycast(transform.position, -Vector3.up, distToGround + 0.1f);
    }

    private void FixedUpdate()
    {
        Vector3 newVelocity = (horizontalInput.x * transform.right + horizontalInput.y * transform.forward) * runningSpeed;
        if (isGrounded()) {
            rb.velocity = newVelocity;
        } else {
            rb.velocity = new Vector3(newVelocity.x, rb.velocity.y, newVelocity.z);
        }
        if (actualCountdown == 0) {
            cc.SendMove(transform.position, transform.rotation.eulerAngles);
            actualCountdown = maxCountdownToUpdate;
        } else {
            actualCountdown -= 1;
        }
    }
}
