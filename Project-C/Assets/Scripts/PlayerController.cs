using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkingSprites;
    [SerializeField] private float secondsPerSprite = 0.12f;

    private SpriteRenderer spriteRenderer;
    private int walkingSpriteIndex;
    private float spriteTimer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "Farm")
        {
            return;
        }

        if (IsSpawned && !IsOwner)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        Vector2 movement = Vector2.zero;

        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
        {
            movement.x -= 1f;
        }

        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
        {
            movement.x += 1f;
        }

        if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
        {
            movement.y -= 1f;
        }

        if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
        {
            movement.y += 1f;
        }

        transform.position += (Vector3)(movement.normalized * moveSpeed * Time.deltaTime);
        UpdateSprite(movement);
    }

    private void UpdateSprite(Vector2 movement)
    {
        if (movement.x != 0f)
        {
            spriteRenderer.flipX = movement.x < 0f;
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
}
