using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Your original movement script, extended with: running (hold Shift), no movement while
/// downed or while a menu is open, and synced facing/walking so other players see your animation.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runMultiplier = 1.6f;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkingSprites;
    [SerializeField] private float secondsPerSprite = 0.12f;

    private SpriteRenderer spriteRenderer;
    private Health health;
    private int walkingSpriteIndex;
    private float spriteTimer;
    private Sprite[] attackSprites;
    private float attackAnimationStart;
    private const float AttackFrameDuration = 0.05f;

    // Owner writes, everyone reads, so remote players animate correctly.
    private readonly NetworkVariable<bool> facingLeft = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> isMoving = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "Farm")
        {
            return;
        }

        if (IsSpawned && !IsOwner)
        {
            // Remote player: copy the owner's facing and walking state.
            float dir = facingLeft.Value ? -1f : 1f;
            UpdateSprite(isMoving.Value ? new Vector2(dir, 0f) : Vector2.zero);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (FarmUI.Blocking || (health != null && health.IsDead))
        {
            UpdateSprite(Vector2.zero);
            PublishState(Vector2.zero);
            return;
        }

        Vector2 movement = Vector2.zero;

        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) movement.x -= 1f;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) movement.x += 1f;
        if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) movement.y -= 1f;
        if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) movement.y += 1f;

        float speed = moveSpeed * (keyboard.leftShiftKey.isPressed ? runMultiplier : 1f);
        transform.position += (Vector3)(movement.normalized * speed * Time.deltaTime);
        UpdateSprite(movement);
        PublishState(movement);
    }

    private void PublishState(Vector2 movement)
    {
        if (!IsSpawned || !IsOwner) return;
        bool moving = movement.sqrMagnitude > 0f;
        if (isMoving.Value != moving) isMoving.Value = moving;
        if (facingLeft.Value != spriteRenderer.flipX) facingLeft.Value = spriteRenderer.flipX;
    }

    private void UpdateSprite(Vector2 movement)
    {
        if (movement.x != 0f)
        {
            spriteRenderer.flipX = movement.x < 0f;
        }

        if (attackSprites != null)
        {
            float elapsed = Time.time - attackAnimationStart;
            float duration = attackSprites.Length * AttackFrameDuration;
            if (elapsed < duration)
            {
                int frame = Mathf.Min(Mathf.FloorToInt(elapsed / AttackFrameDuration), attackSprites.Length - 1);
                spriteRenderer.sprite = attackSprites[frame];
                return;
            }

            attackSprites = null;
        }

        if (movement.sqrMagnitude == 0f)
        {
            walkingSpriteIndex = 0;
            spriteTimer = 0f;

            if (idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
            else if (walkingSprites != null && walkingSprites.Length > 0)
            {
                spriteRenderer.sprite = walkingSprites[0];
            }

            return;
        }

        if (walkingSprites == null || walkingSprites.Length == 0)
        {
            return;
        }

        if (secondsPerSprite > 0f)
        {
            spriteTimer += Time.deltaTime;
            if (spriteTimer >= secondsPerSprite)
            {
                int framesToAdvance = Mathf.FloorToInt(spriteTimer / secondsPerSprite);
                walkingSpriteIndex = (walkingSpriteIndex + framesToAdvance) % walkingSprites.Length;
                spriteTimer %= secondsPerSprite;
            }
        }

        spriteRenderer.sprite = walkingSprites[walkingSpriteIndex];
    }

    public void PlayAttackAnimation(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return;
        attackSprites = sprites;
        attackAnimationStart = Time.time;
    }
}
