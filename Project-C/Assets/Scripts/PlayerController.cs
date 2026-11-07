using System.Collections.Generic;
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
    private Sprite currentSourceSprite;
    private float attackAnimationStart;
    private const float AttackFrameDuration = 0.05f;
    private static readonly Dictionary<string, Sprite> customizedSprites = new Dictionary<string, Sprite>();
    private static readonly HashSet<int> unreadableTextureWarnings = new HashSet<int>();

    // Owner writes, everyone reads, so remote players animate correctly.
    private readonly NetworkVariable<bool> facingLeft = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> isMoving = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<int> hairColorIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<int> clothesColorIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
    }

    public override void OnNetworkSpawn()
    {
        hairColorIndex.OnValueChanged += OnAppearanceChanged;
        clothesColorIndex.OnValueChanged += OnAppearanceChanged;

        if (IsOwner)
        {
            hairColorIndex.Value = GameSettings.HairColorIndex;
            clothesColorIndex.Value = GameSettings.ClothesColorIndex;
        }

        ApplyAppearance();
    }

    public override void OnNetworkDespawn()
    {
        hairColorIndex.OnValueChanged -= OnAppearanceChanged;
        clothesColorIndex.OnValueChanged -= OnAppearanceChanged;
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
                SetPlayerSprite(idleSprite);
            }
            else if (walkingSprites != null && walkingSprites.Length > 0)
            {
                SetPlayerSprite(walkingSprites[0]);
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

        SetPlayerSprite(walkingSprites[walkingSpriteIndex]);
    }

    public void PlayAttackAnimation(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return;
        attackSprites = new Sprite[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            attackSprites[i] = CustomizeSprite(sprites[i]);
        }
        attackAnimationStart = Time.time;
    }

    private void OnAppearanceChanged(int previous, int current)
    {
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        if (spriteRenderer != null && currentSourceSprite != null)
        {
            spriteRenderer.sprite = CustomizeSprite(currentSourceSprite);
        }
    }

    private void SetPlayerSprite(Sprite source)
    {
        currentSourceSprite = source;
        spriteRenderer.sprite = CustomizeSprite(source);
    }

    private Sprite CustomizeSprite(Sprite source)
    {
        return CreateCustomizedSprite(source, hairColorIndex.Value, clothesColorIndex.Value);
    }

    public static Sprite CreateCustomizedSprite(Sprite source, int hairColorIndex, int clothesColorIndex)
    {
        if (source == null) return null;
        if (!source.texture.isReadable)
        {
            if (unreadableTextureWarnings.Add(source.texture.GetInstanceID()))
            {
                Debug.LogError("Player customization needs the player sprite texture's Read/Write setting enabled.", source);
            }
            return source;
        }

        int hair = Mathf.Clamp(hairColorIndex, 0, GameSettings.HairColors.Length - 1);
        int clothes = Mathf.Clamp(clothesColorIndex, 0, GameSettings.ClothesColors.Length - 1);
        string key = source.GetInstanceID() + ":" + hair + ":" + clothes;
        Sprite cached;
        if (customizedSprites.TryGetValue(key, out cached)) return cached;

        Rect rect = source.rect;
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);
        Color[] pixels = source.texture.GetPixels(
            Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), width, height);

        for (int y = 0; y < height; y++)
        {
            float verticalPosition = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;
                Color pixel = pixels[pixelIndex];
                if (pixel.a == 0f) continue;

                float hue, saturation, brightness;
                Color.RGBToHSV(pixel, out hue, out saturation, out brightness);

                if (verticalPosition >= 0.45f && saturation >= 0.2f &&
                    hue <= 0.15f && brightness >= 0.08f && brightness <= 0.72f)
                {
                    pixels[pixelIndex] = ShadeColor(GameSettings.HairColors[hair], brightness, 0.4f, pixel.a);
                }
                else if ((verticalPosition <= 0.28f && saturation <= 0.24f &&
                    brightness >= 0.42f && brightness <= 0.82f) ||
                    (verticalPosition <= 0.62f && saturation >= 0.3f &&
                    hue >= 0.68f && hue <= 0.92f && brightness >= 0.16f && brightness <= 0.65f))
                {
                    pixels[pixelIndex] = ShadeColor(GameSettings.ClothesColors[clothes], brightness, 0.65f, pixel.a);
                }
            }
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = source.texture.filterMode;
        texture.SetPixels(pixels);
        texture.Apply(false, true);

        Vector2 normalizedPivot = new Vector2(source.pivot.x / width, source.pivot.y / height);
        Sprite customized = Sprite.Create(texture, new Rect(0, 0, width, height), normalizedPivot,
            source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border);
        customized.name = source.name + "_Hair" + hair + "_Clothes" + clothes;
        customizedSprites.Add(key, customized);
        return customized;
    }

    private static Color ShadeColor(Color color, float brightness, float sourceBrightness, float alpha)
    {
        float shade = Mathf.Clamp(brightness / sourceBrightness, 0.55f, 1.35f);
        return new Color(
            Mathf.Clamp01(color.r * shade),
            Mathf.Clamp01(color.g * shade),
            Mathf.Clamp01(color.b * shade),
            alpha);
    }
}
