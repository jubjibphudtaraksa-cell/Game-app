using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {
    public Transform cam; public float walk = 3.2f, run = 5.4f, jump = 6.5f, gravity = -20f, sens = 2f;
    CharacterController cc; float pitch, vy; public bool Moving { get; private set; }
    public bool controlsEnabled = true;
    void Awake(){ cc = GetComponent<CharacterController>(); Cursor.lockState = CursorLockMode.Locked; }
    void Update() {
        if (!controlsEnabled) return;
        transform.Rotate(0, Input.GetAxis("Mouse X") * sens, 0);
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * sens, -89, 89);
        cam.localEulerAngles = new Vector3(pitch, 0, 0);
        Vector3 d = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");
        Moving = d.sqrMagnitude > .01f;
        float sp = Input.GetKey(KeyCode.LeftShift) ? walk : run; // Shift = เดินเงียบ
        if (cc.isGrounded) { vy = -1; if (Input.GetKeyDown(KeyCode.Space)) vy = jump; } else vy += gravity * Time.deltaTime;
        cc.Move((d.normalized * sp + Vector3.up * vy) * Time.deltaTime);
    }
    public void AddRecoil(Vector2 r){ transform.Rotate(0, r.x, 0); pitch = Mathf.Clamp(pitch - r.y, -89, 89); }
}
