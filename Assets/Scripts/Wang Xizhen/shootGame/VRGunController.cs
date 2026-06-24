using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class VRGunController : MonoBehaviour
{
    [Header("Gun State")]
    public GameObject gunVisualRoot;
    public GameObject crosshairObject;

    [Header("VR Input")]
    public InputActionReference toggleGunAction;
    public InputActionReference shootAction;

    [Header("Editor Test")]
    public bool allowKeyboardTest = true;
    public KeyCode testToggleKey = KeyCode.G;
    public KeyCode testShootKey = KeyCode.Mouse0;

    [Header("Shooting")]
    public Transform muzzlePoint;
    public float shootingRange = 30f;
    public LayerMask shootLayerMask = ~0;

    [Header("Bullet Visual")]
    public GameObject bulletVisualPrefab;
    public float bulletSpeed = 30f;

    [Header("Audio")]
    public AudioSource gunAudioSource;
    public AudioClip shootSound;
    public AudioClip noAmmoSound;
    public float soundVolume = 1f;

    [Header("Crosshair Feedback")]
    public Transform crosshairTransform;
    public Renderer crosshairRenderer;
    public Color crosshairNormalColor = Color.white;
    public Color crosshairHitColor = Color.red;
    public float crosshairNormalScale = 0.04f;
    public float crosshairFireScale = 0.07f;
    public float crosshairFeedbackTime = 0.12f;

    [Header("Recoil")]
    public Transform recoilRoot;
    public float recoilBackDistance = 0.04f;
    public float recoilUpAngle = 4f;
    public float recoilReturnTime = 0.08f;

    [Header("Dynamic Crosshair")]
    public bool updateCrosshairByRaycast = true;
    public float crosshairSurfaceOffset = 0.02f;
    public Transform playerCamera;

    private bool gunEquipped;
    private Vector3 recoilStartLocalPosition;
    private Quaternion recoilStartLocalRotation;
    private Coroutine crosshairRoutine;
    private Coroutine recoilRoutine;

    private void OnEnable()
    {
        if (toggleGunAction != null && toggleGunAction.action != null)
        {
            toggleGunAction.action.Enable();
        }

        if (shootAction != null && shootAction.action != null)
        {
            shootAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (toggleGunAction != null && toggleGunAction.action != null)
        {
            toggleGunAction.action.Disable();
        }

        if (shootAction != null && shootAction.action != null)
        {
            shootAction.action.Disable();
        }
    }

    private void Start()
    {
        if (recoilRoot == null && gunVisualRoot != null)
        {
            recoilRoot = gunVisualRoot.transform;
        }

        if (recoilRoot != null)
        {
            recoilStartLocalPosition = recoilRoot.localPosition;
            recoilStartLocalRotation = recoilRoot.localRotation;
        }

        if (crosshairTransform != null)
        {
            crosshairTransform.localScale = Vector3.one * crosshairNormalScale;
        }

        if (crosshairRenderer != null)
        {
            crosshairRenderer.material.color = crosshairNormalColor;
        }

        SetGunEquipped(false);
    }

    private void Update()
    {
        if (WasTogglePressed())
        {
            ToggleGun();
        }

        if (gunEquipped)
        {
            UpdateCrosshairPosition();
        }

        if (WasShootPressed())
        {
            TryShoot();
        }
    }
    private bool WasTogglePressed()
    {
        if (toggleGunAction != null &&
            toggleGunAction.action != null &&
            toggleGunAction.action.WasPressedThisFrame())
        {
            return true;
        }

        if (allowKeyboardTest && Application.isEditor && Input.GetKeyDown(testToggleKey))
        {
            return true;
        }

        return false;
    }

    private bool WasShootPressed()
    {
        if (shootAction != null &&
            shootAction.action != null &&
            shootAction.action.WasPressedThisFrame())
        {
            return true;
        }

        if (allowKeyboardTest && Application.isEditor && Input.GetKeyDown(testShootKey))
        {
            return true;
        }

        return false;
    }

    private void ToggleGun()
    {
        SetGunEquipped(!gunEquipped);
    }

    private void SetGunEquipped(bool equipped)
    {
        gunEquipped = equipped;

        if (gunVisualRoot != null)
        {
            gunVisualRoot.SetActive(gunEquipped);
        }

        if (crosshairObject != null)
        {
            crosshairObject.SetActive(gunEquipped);
        }
    }

    private void TryShoot()
    {
        if (!gunEquipped)
        {
            return;
        }

        if (ShootingGameManager.Instance == null)
        {
            Debug.LogWarning("ShootingGameManager is missing.");
            return;
        }

        if (!ShootingGameManager.Instance.CanShoot())
        {
            PlayNoAmmoSound();
            return;
        }

        if (!ShootingGameManager.Instance.UseAmmo())
        {
            PlayNoAmmoSound();
            return;
        }

        bool hitTarget = false;
        Vector3 bulletEndPoint = muzzlePoint.position + muzzlePoint.forward * shootingRange;

        if (muzzlePoint != null)
        {
            Ray ray = new Ray(muzzlePoint.position, muzzlePoint.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, shootingRange, shootLayerMask, QueryTriggerInteraction.Ignore))
            {
                bulletEndPoint = hit.point;

                ShootingTarget target = hit.collider.GetComponentInParent<ShootingTarget>();

                if (target != null)
                {
                    bool validHit = target.Hit();

                    if (validHit)
                    {
                        hitTarget = true;
                        ShootingGameManager.Instance.AddScore();
                    }
                }
            }
        }

        SpawnBulletVisual(bulletEndPoint);
        PlayShootSound();
        PlayCrosshairFeedback(hitTarget);
        PlayRecoil();

        ShootingGameManager.Instance.CheckEndAfterShot();
    }

    private void SpawnBulletVisual(Vector3 endPoint)
    {
        if (bulletVisualPrefab == null || muzzlePoint == null)
        {
            return;
        }

        GameObject bullet = Instantiate(
            bulletVisualPrefab,
            muzzlePoint.position,
            Quaternion.identity
        );

        BulletVisual bulletVisual = bullet.GetComponent<BulletVisual>();

        if (bulletVisual != null)
        {
            bulletVisual.Init(muzzlePoint.position, endPoint, bulletSpeed);
        }
    }

    private void PlayShootSound()
    {
        if (gunAudioSource != null && shootSound != null)
        {
            gunAudioSource.PlayOneShot(shootSound, soundVolume);
        }
    }

    private void PlayNoAmmoSound()
    {
        if (gunAudioSource != null && noAmmoSound != null)
        {
            gunAudioSource.PlayOneShot(noAmmoSound, soundVolume);
        }
    }

    private void PlayCrosshairFeedback(bool hitTarget)
    {
        if (crosshairRoutine != null)
        {
            StopCoroutine(crosshairRoutine);
        }

        crosshairRoutine = StartCoroutine(CrosshairFeedbackRoutine(hitTarget));
    }

    private IEnumerator CrosshairFeedbackRoutine(bool hitTarget)
    {
        if (crosshairTransform != null)
        {
            crosshairTransform.localScale = Vector3.one * crosshairFireScale;
        }

        if (crosshairRenderer != null)
        {
            crosshairRenderer.material.color = hitTarget ? crosshairHitColor : crosshairNormalColor;
        }

        yield return new WaitForSeconds(crosshairFeedbackTime);

        if (crosshairTransform != null)
        {
            crosshairTransform.localScale = Vector3.one * crosshairNormalScale;
        }

        if (crosshairRenderer != null)
        {
            crosshairRenderer.material.color = crosshairNormalColor;
        }
    }

    private void PlayRecoil()
    {
        if (recoilRoot == null)
        {
            return;
        }

        if (recoilRoutine != null)
        {
            StopCoroutine(recoilRoutine);
        }

        recoilRoutine = StartCoroutine(RecoilRoutine());
    }

    private IEnumerator RecoilRoutine()
    {
        recoilRoot.localPosition =
            recoilStartLocalPosition + Vector3.back * recoilBackDistance;

        recoilRoot.localRotation =
            recoilStartLocalRotation * Quaternion.Euler(-recoilUpAngle, 0f, 0f);

        float timer = 0f;

        while (timer < recoilReturnTime)
        {
            timer += Time.deltaTime;
            float t = timer / recoilReturnTime;

            recoilRoot.localPosition = Vector3.Lerp(
                recoilRoot.localPosition,
                recoilStartLocalPosition,
                t
            );

            recoilRoot.localRotation = Quaternion.Slerp(
                recoilRoot.localRotation,
                recoilStartLocalRotation,
                t
            );

            yield return null;
        }

        recoilRoot.localPosition = recoilStartLocalPosition;
        recoilRoot.localRotation = recoilStartLocalRotation;
    }
    private void OnDrawGizmos()
{
    if (muzzlePoint == null)
    {
        return;
    }

    Gizmos.color = Color.green;
    Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * shootingRange);
    Gizmos.DrawSphere(muzzlePoint.position, 0.03f);
}

    private void UpdateCrosshairPosition()
    {
        if (!updateCrosshairByRaycast)
        {
            return;
        }

        if (crosshairTransform == null || muzzlePoint == null)
        {
            return;
        }

        Vector3 targetPosition;
        Vector3 targetNormal = -muzzlePoint.forward;

        Ray ray = new Ray(muzzlePoint.position, muzzlePoint.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, shootingRange, shootLayerMask, QueryTriggerInteraction.Ignore))
        {
            targetPosition = hit.point + hit.normal * crosshairSurfaceOffset;
            targetNormal = hit.normal;
        }
        else
        {
            targetPosition = muzzlePoint.position + muzzlePoint.forward * shootingRange;
        }

        crosshairTransform.position = targetPosition;

        if (playerCamera != null)
        {
            Vector3 directionToCamera = crosshairTransform.position - playerCamera.position;

            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                crosshairTransform.rotation = Quaternion.LookRotation(directionToCamera.normalized);
            }
        }
    }
}