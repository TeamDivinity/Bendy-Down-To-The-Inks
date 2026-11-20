using UnityEngine;
using System;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] public float mouseSensitivity = 2f;
    [SerializeField] private float minVerticalAngle = -90f;
    [SerializeField] private float maxVerticalAngle = 90f;
    [SerializeField] private float cameraHeightFromBottom = 1.7f;
    [SerializeField] private float crouchCameraOffset = -0.3f;

    [Header("Controls")]
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Camera Bobbing")]
    [SerializeField] private float bobSmoothSpeed = 10f;
    [SerializeField] private float idleBobAmp = 0.05f;
    [SerializeField] private float idleBobFreq = 1f;
    [SerializeField] private float walkBobAmp = 0.1f;
    [SerializeField] private float walkBobFreq = 10f;
    [SerializeField] private float runBobAmp = 0.2f;
    [SerializeField] private float runBobFreq = 15f;
    [SerializeField] private float crouchBobAmp = 0.02f;
    [SerializeField] private float crouchBobFreq = 5f;
    [Range(0f, 1f)] [SerializeField] private float bobTiltIntensity = 0.3f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float crouchSmoothSpeed = 8f;

    [Header("Footstep Sounds")]
    [SerializeField] private SurfaceSound[] surfaces;
    [SerializeField] private AudioClip[] defaultFootstepClips;
    [SerializeField] private float walkStepStride = 1.0f;
    [SerializeField] private float runStepStride = 0.6f;
    [SerializeField] private float crouchStepStride = 1.2f;
    [SerializeField] private float footstepVolume = 1f;
    [SerializeField] private float footstepRange = 0.5f;

    [Header("Light Settings")]
    [SerializeField] private Transform lightTransform;
    [SerializeField] private float lightRotationSmoothSpeed = 8f;
    [SerializeField] private Vector3 lightPositionOffset = new Vector3(0.2f, 0f, 0.2f);

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 25f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float staminaRegenDelay = 1f;
    public float StaminaPercent => currentStamina / maxStamina;
    
    [Header("Stamina UI")]
    [SerializeField] private RectTransform staminaBar;
    [SerializeField] private float uiSmoothSpeed = 8f;

    // ==================== ИЗМЕНЁННЫЕ ПОЛЯ ====================
    [Header("Handle & Overlay Camera")]
    [Tooltip("Объект, который будет повторять поворот камеры без эффекта 'тряски' (bobbing). Может находиться вне иерархии игрока.")]
    [SerializeField] private Transform handleTransform;

    [Tooltip("Локальное смещение handle относительно игрока (как если бы он был дочерним).")]
    [SerializeField] private Vector3 handlePositionOffset = Vector3.zero;

    [Tooltip("Дополнительная камера, которая полностью копирует трансформацию основной камеры (включая бобинг)")]
    [SerializeField] private Camera overlayCamera;
    // =========================================================

    private CharacterController controller;
    private AudioSource audioSource;
    private Vector3 velocity;
    private Vector3 currentHandleLocalPos;
    private float horizontalRotation;
    private float verticalRotation;
    private bool isGrounded;
    private Vector3 initialCameraLocalPos;
    private Quaternion currentLightRotation;
    private float bobTimer;
    private float currentBobAmp;
    private float currentBobFreq;
    private float originalHeight;
    private Vector3 originalCenter;
    private bool isCrouching;
    private float currentControllerHeight;
    private Vector3 currentControllerCenter;
    private float distanceAccumulator = 0f;
    private float currentMoveSpeed = 0f;
    private Quaternion targetCameraRotation;
    private float currentStamina;
    private float lastStaminaChangeTime;
    private bool sprintLocked;   
    private bool wasRunning;     
    private Vector2 currentBarScale;

    // Храним «чистый» поворот камеры (без влияния бобинга)
    private Quaternion pureCameraRotation;

    [Serializable]
    public class SurfaceSound
    {
        public string surfaceTag;
        public AudioClip[] clips;
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraTransform = mainCam.transform;
        }
        if (cameraTransform != null)
            initialCameraLocalPos = cameraTransform.localPosition;

        if (lightTransform != null)
        {
            currentLightRotation = lightTransform.rotation;
        }

        originalHeight = controller.height;
        originalCenter = controller.center;
        currentControllerHeight = originalHeight;
        currentControllerCenter = originalCenter;
        currentHandleLocalPos = handlePositionOffset;
        isCrouching = false;
        horizontalRotation = transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
        verticalRotation = cameraTransform ? cameraTransform.localEulerAngles.x : 0f;
        if (verticalRotation > 180f) verticalRotation -= 360f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentStamina = maxStamina;
        lastStaminaChangeTime = Time.time;
        sprintLocked = false;
        wasRunning = false;

        if (staminaBar != null)
        {
            staminaBar.pivot = new Vector2(0f, 0.5f);
            currentBarScale = staminaBar.localScale;
            staminaBar.localScale = new Vector3(1f, 1f, 1f);
        }

        // Инициализация overlay-камеры
        if (overlayCamera != null && cameraTransform != null)
        {
            overlayCamera.transform.SetPositionAndRotation(cameraTransform.position, cameraTransform.rotation);
        }
    }

    private void Update()
    {
        if (Pause.IsPaused) return; 
        
        HandleCrouch();
        HandleCameraRotation();
        HandleStamina();
        UpdateStaminaUI();
        HandleMovement();
        HandleFootsteps();
        HandleCameraBob();
        HandleLight();
        SyncHandleTransform();  // <-- новый метод

        // Синхронизация overlay-камеры (каждый кадр, включая бобинг)
        if (overlayCamera != null && cameraTransform != null)
        {
            overlayCamera.transform.SetPositionAndRotation(cameraTransform.position, cameraTransform.rotation);
        }
    }

    /// <summary>
    /// Обновляет положение и вращение handle, позволяя ему находиться вне иерархии.
    /// </summary>
    private void SyncHandleTransform()
    {
        if (handleTransform == null) return;

        // Целевое локальное смещение с учётом приседания
        Vector3 targetLocalPos = handlePositionOffset;
        if (isCrouching)
            targetLocalPos += Vector3.up * crouchCameraOffset;

        // Плавное изменение локальной позиции (та же скорость, что у камеры)
        currentHandleLocalPos = Vector3.Lerp(currentHandleLocalPos, targetLocalPos, Time.deltaTime * bobSmoothSpeed);

        // Мировая позиция handle
        Vector3 targetWorldPos = transform.position + transform.rotation * currentHandleLocalPos;
        handleTransform.position = targetWorldPos;

        // Вращение: горизонтальный поворот игрока + вертикальный поворот камеры (без бобинга)
        Quaternion targetWorldRot = transform.rotation * Quaternion.Euler(verticalRotation, 0f, 0f);
        handleTransform.rotation = targetWorldRot;
    }

    private void HandleStamina()
    {
        if (!Input.GetKey(runKey))
        {
            sprintLocked = false;
        }
        float inputMagnitude = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        bool isMoving = inputMagnitude > 0.1f;
        bool wantsToRun = Input.GetKey(runKey) && !isCrouching && !sprintLocked && isMoving;
        bool canSprint = currentStamina > 0;
        bool isActuallyRunning = wantsToRun && canSprint && isGrounded;

        if (isActuallyRunning)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina < 0) currentStamina = 0;
            lastStaminaChangeTime = Time.time;
            if (currentStamina <= 0)
            {
                sprintLocked = true;
            }
        }
        else
        {
            if (wasRunning)
            {
                lastStaminaChangeTime = Time.time;
            }
            if (Time.time - lastStaminaChangeTime >= staminaRegenDelay && currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                if (currentStamina > maxStamina) currentStamina = maxStamina;
            }
        }
        wasRunning = isActuallyRunning;
    }

    private bool CanSprint()
    {
        return currentStamina > 0 && !isCrouching && isGrounded && !sprintLocked;
    }

    private void UpdateStaminaUI()
    {
        if (staminaBar == null) return;
        float staminaPercent = currentStamina / maxStamina;
        float targetScaleX = Mathf.Clamp01(staminaPercent);
        currentBarScale.x = Mathf.Lerp(currentBarScale.x, targetScaleX, Time.deltaTime * uiSmoothSpeed);
        if (targetScaleX == 0 && currentBarScale.x < 0.01f)
            currentBarScale.x = 0;
        else if (targetScaleX == 1 && currentBarScale.x > 0.99f)
            currentBarScale.x = 1;
        staminaBar.localScale = new Vector3(currentBarScale.x, 1f, 1f);
    }

    private void HandleCrouch()
    {
        if (controller == null) return;
        bool wantCrouch = Input.GetKey(crouchKey);
        if (wantCrouch && !isCrouching)
        {
            isCrouching = true;
        }
        else if (!wantCrouch && isCrouching)
        {
            if (!CheckCeiling())
            {
                isCrouching = false;
            }
        }
        float targetHeight = isCrouching ? crouchHeight : originalHeight;
        Vector3 targetCenter = new Vector3(0, targetHeight / 2f, 0);
        float heightDiff = Mathf.Abs(targetHeight - currentControllerHeight);
        if (heightDiff > 0.001f)
        {
            currentControllerHeight = Mathf.Lerp(currentControllerHeight, targetHeight, Time.deltaTime * crouchSmoothSpeed);
            currentControllerCenter = Vector3.Lerp(currentControllerCenter, targetCenter, Time.deltaTime * crouchSmoothSpeed);
            controller.height = currentControllerHeight;
            controller.center = currentControllerCenter;
        }
        else
        {
            controller.height = targetHeight;
            controller.center = targetCenter;
            currentControllerHeight = targetHeight;
            currentControllerCenter = targetCenter;
        }
    }

    private bool CheckCeiling()
    {
        float checkDistance = originalHeight - currentControllerHeight + 0.1f;
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(rayStart, Vector3.up, checkDistance))
        {
            return true;
        }
        return false;
    }

    private void HandleCameraRotation()
    {
        if (cameraTransform == null) return;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        horizontalRotation += mouseX;
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);

        transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);

        // Чистый поворот камеры (без бобинга) для света и других целей
        pureCameraRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        if (isCrouching)
        {
            currentMoveSpeed = crouchSpeed;
        }
        else
        {
            bool isRunning = Input.GetKey(runKey) && CanSprint();
            currentMoveSpeed = isRunning ? runSpeed : walkSpeed;
        }

        controller.Move(move * currentMoveSpeed * Time.deltaTime);

        if (Input.GetKeyDown(jumpKey) && isGrounded && !isCrouching)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleFootsteps()
    {
        if (!isGrounded || audioSource == null)
            return;

        float inputMagnitude = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        if (inputMagnitude > 0.1f && currentMoveSpeed > 0.1f)
        {
            distanceAccumulator += currentMoveSpeed * Time.deltaTime;
            float stepThreshold = walkStepStride;
            if (isCrouching)
            {
                stepThreshold = crouchStepStride;
            }
            else if (Input.GetKey(runKey) && CanSprint())
            {
                stepThreshold = runStepStride;
            }

            if (distanceAccumulator >= stepThreshold)
            {
                string surfaceTag = GetSurfaceTag();
                PlayFootstepSound(surfaceTag);
                distanceAccumulator = 0f;
            }
        }
        else
        {
            distanceAccumulator = 0f;
        }
    }

    private string GetSurfaceTag()
    {
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * (controller.height * 0.1f);
        if (Physics.Raycast(rayStart, Vector3.down, out hit, footstepRange))
        {
            if (hit.collider != null && !string.IsNullOrEmpty(hit.collider.tag))
            {
                return hit.collider.tag;
            }
        }
        return "Untagged";
    }

    private void PlayFootstepSound(string surfaceTag)
    {
        AudioClip[] clipsToPlay = defaultFootstepClips;
        if (surfaces != null)
        {
            foreach (SurfaceSound surface in surfaces)
            {
                if (!string.IsNullOrEmpty(surface.surfaceTag) &&
                    surface.surfaceTag.Equals(surfaceTag, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (surface.clips != null && surface.clips.Length > 0)
                    {
                        clipsToPlay = surface.clips;
                    }
                    break;
                }
            }
        }

        if (clipsToPlay == null || clipsToPlay.Length == 0)
            return;

        AudioClip clip = clipsToPlay[Random.Range(0, clipsToPlay.Length)];
        audioSource.PlayOneShot(clip, footstepVolume);
    }

    private void HandleCameraBob()
    {
        if (cameraTransform == null) return;

        float inputMagnitude = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        bool isMoving = inputMagnitude > 0.1f && isGrounded;
        bool isRunning = !isCrouching && Input.GetKey(runKey) && CanSprint() && isMoving;

        float targetAmp = idleBobAmp;
        float targetFreq = idleBobFreq;
        if (isMoving)
        {
            if (isCrouching)
            {
                targetAmp = crouchBobAmp;
                targetFreq = crouchBobFreq;
            }
            else if (isRunning)
            {
                targetAmp = runBobAmp;
                targetFreq = runBobFreq;
            }
            else
            {
                targetAmp = walkBobAmp;
                targetFreq = walkBobFreq;
            }
        }

        currentBobAmp = Mathf.Lerp(currentBobAmp, targetAmp, Time.deltaTime * bobSmoothSpeed);
        currentBobFreq = Mathf.Lerp(currentBobFreq, targetFreq, Time.deltaTime * bobSmoothSpeed);

        if (isMoving)
        {
            bobTimer += Time.deltaTime * currentBobFreq;
        }
        else
        {
            bobTimer += Time.deltaTime * idleBobFreq;
        }

        float yOffset = Mathf.Sin(bobTimer) * currentBobAmp;
        float xOffset = Mathf.Cos(bobTimer) * (currentBobAmp * 1.0f);
        float tiltAngle = Mathf.Sin(bobTimer) * currentBobAmp * bobTiltIntensity * 10f;

        float cameraYOffset = isCrouching ? crouchCameraOffset : 0f;
        float targetCameraY = cameraHeightFromBottom + cameraYOffset + yOffset;
        Vector3 newPos = initialCameraLocalPos;
        newPos.y = targetCameraY;
        newPos.x += xOffset;
        if (isRunning)
            newPos.z += Mathf.Sin(bobTimer) * (currentBobAmp * 0.2f);

        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newPos, Time.deltaTime * bobSmoothSpeed);
        Quaternion tiltRotation = Quaternion.Euler(0, 0, tiltAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f) * tiltRotation;
    }

    private void HandleLight()
    {
        if (lightTransform == null) return;

        float cameraYOffset = isCrouching ? crouchCameraOffset : 0f;
        float targetLightY = cameraHeightFromBottom + cameraYOffset;
        Vector3 targetLightPos = transform.position + Vector3.up * targetLightY;
        targetLightPos += transform.right * lightPositionOffset.x;
        targetLightPos += transform.forward * lightPositionOffset.z;
        targetLightPos += Vector3.up * lightPositionOffset.y;

        lightTransform.position = Vector3.Lerp(lightTransform.position, targetLightPos, Time.deltaTime * bobSmoothSpeed);
        currentLightRotation = Quaternion.Slerp(currentLightRotation, pureCameraRotation, Time.deltaTime * lightRotationSmoothSpeed);
        lightTransform.rotation = currentLightRotation;
    }
}