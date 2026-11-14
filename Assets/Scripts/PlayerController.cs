using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {
    public Transform cam; public float walk = 3f, run = 5.2f, jump = 6.2f, gravity = -20f, sens = 2f;
    public bool controlsEnabled;
    CharacterController cc; Health hp; float pitch, vy;
    public bool Moving { get; private set; }

    void Awake() { cc = GetComponent<CharacterController>(); hp = GetComponent<Health>(); sens = PlayerPrefs.GetFloat("sens", 2f); }

    public void ResetLook() { pitch = 0; vy = 0; if (cam != null) cam.localEulerAngles = Vector3.zero; }

    void Update() {
        bool mob = Application.isMobilePlatform;
        if (!controlsEnabled || hp == null || !hp.Alive) { Moving = false; return; }
        Vector2 look = mob ? TouchInput.Look : new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * sens;
        transform.Rotate(0, look.x, 0);
        pitch = Mathf.Clamp(pitch - look.y, -89f, 89f);
        cam.localEulerAngles = new Vector3(pitch, 0, 0);
        Vector2 mv = mob ? TouchInput.Move : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 d = transform.right * mv.x + transform.forward * mv.y;
        if (d.sqrMagnitude > 1f) d.Normalize();
        Moving = d.sqrMagnitude > .01f;
        float sp = (!mob && Input.GetKey(KeyCode.LeftShift)) ? walk : run;
        if (mob && mv.magnitude < .55f) sp = walk;
        bool jmp = Input.GetKeyDown(KeyCode.Space) || TouchInput.JumpDown;
        if (cc.isGrounded) { vy = -1f; if (jmp) vy = jump; } else vy += gravity * Time.deltaTime;
        cc.Move((d * sp + Vector3.up * vy) * Time.deltaTime);
    }

    public void AddRecoil(Vector2 r) { transform.Rotate(0, r.x, 0); pitch = Mathf.Clamp(pitch - r.y, -89f, 89f); }
}
