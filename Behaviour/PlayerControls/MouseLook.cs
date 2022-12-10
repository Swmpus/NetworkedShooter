using UnityEngine;

public class MouseLook : MonoBehaviour
{
    private float minY = -80;
    private float maxY = 90;
    private float verticalSensitivity = 4f;
    private float horizontalSensitivity = 2f;
    private Vector2 mouseMove;
    private Transform cam;
    private Rigidbody rb;
    CoreController gameController;

    void Start()
    {
        gameController = GameObject.Find("GameMaster").GetComponent<CoreController>();
        rb = GetComponent<Rigidbody>();
        cam = gameObject.transform.GetChild(0);
    }

    public void ReceiveInput(Vector2 mouseChange)
    {
        mouseMove = mouseChange;
    }

    private void LateUpdate()
    {
        rb.MoveRotation(Quaternion.Euler(rb.rotation.eulerAngles + Vector3.up * mouseMove.x * horizontalSensitivity * Time.fixedDeltaTime));
    }

    void Update()
    {
        float verticalRotation = Mathf.Clamp(mouseMove.y * verticalSensitivity * Time.deltaTime, minY, maxY);
        cam.rotation = Quaternion.Euler(cam.rotation.eulerAngles + Vector3.left * verticalRotation);
    }

    internal void fire()
    {
        gameController.SendFire(cam.position, cam.rotation.eulerAngles);
    }
}
