using UnityEngine;
using UnityEngine.InputSystem;

public class CamaraTerceraPersona : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Órbita y Distancia")]
    public float distance = 3.5f;
    public float minDistance = 1.1f;
    public float maxDistance = 6f;
    public float minPitch = -25f;
    public float maxPitch = 65f;
    public float mouseSensitivity = 0.15f;
    public float gamepadSensitivity = 140f;

    [Header("Colisión y Suavizado")]
    public float cameraRadius = 0.18f;
    public LayerMask collisionLayers = ~0;
    public float zoomInSpeed = 30f;
    public float zoomOutSpeed = 10f;

    private float _yaw;
    private float _pitch = 18f;
    private float _currentDistance;
    private InputAction _lookAction;
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

    private void Awake()
    {
        _lookAction = new InputAction("Look", binding: "<Mouse>/delta");
        _lookAction.AddBinding("<Gamepad>/rightStick");
        _currentDistance = distance;
    }

    private void OnEnable() => _lookAction.Enable();
    private void OnDisable() => _lookAction.Disable();

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
            _yaw = target.eulerAngles.y;

        _currentDistance = distance;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isLocked;
        }

        // Permite acercar/alejar con la rueda del ratón dentro del cofre
        if (Mouse.current != null)
        {
            float scrollY = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.01f)
            {
                distance = Mathf.Clamp(distance - Mathf.Sign(scrollY) * 0.4f, minDistance, maxDistance);
            }
        }

        Vector2 lookInput = _lookAction.ReadValue<Vector2>();
        bool isGamepad = _lookAction.activeControl?.device is Gamepad;
        float sensitivity = isGamepad ? gamepadSensitivity * Time.deltaTime : mouseSensitivity;

        _yaw += lookInput.x * sensitivity;
        _pitch -= lookInput.y * sensitivity;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 pivotPosition = target.position + targetOffset;
        Vector3 desiredDirection = rotation * Vector3.back;

        float targetDistance = distance;
        int hitCount = Physics.SphereCastNonAlloc(pivotPosition, cameraRadius, desiredDirection, _hitBuffer, distance, collisionLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hitBuffer[i];
            // Ignora por completo al propio jugador y todos sus huesos/mallas hijas
            if (hit.transform == target || hit.transform.IsChildOf(target)) continue;

            float safeDist = Mathf.Clamp(hit.distance - 0.15f, minDistance, distance);
            if (safeDist < targetDistance)
            {
                targetDistance = safeDist;
            }
        }

        float smoothSpeed = targetDistance < _currentDistance ? zoomInSpeed : zoomOutSpeed;
        _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, smoothSpeed * Time.deltaTime);

        transform.position = pivotPosition + desiredDirection * _currentDistance;
        transform.rotation = rotation;
    }
}
