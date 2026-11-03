using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// One farm tile. Growth runs on the host; the stage is a NetworkVariable so everyone sees the same crop.
/// Stage 0 = empty soil, last stage = ready to harvest. Monsters can trample planted crops.
/// Needs: NetworkObject, SpriteRenderer, trigger Collider2D. The setup menu creates these for you.
/// </summary>
public class CropPlot : NetworkBehaviour
{
    public static readonly List<CropPlot> All = new List<CropPlot>();

    public Sprite[] stageSprites;          // planted stages: grass, flower, ripe wheat
    public float secondsPerStage = 10f;
    public int baseHarvestValue = 10;

    [SerializeField] private SpriteRenderer cropRenderer;
    private readonly NetworkVariable<int> stage = new NetworkVariable<int>(0);
    private float growTimer;

    private int RipeStage { get { return stageSprites != null ? stageSprites.Length : 0; } }
    public int Stage { get { return stage.Value; } }

    public string Prompt
    {
        get
        {
            if (stage.Value == 0) return "E: Plant seeds";
            if (stage.Value >= RipeStage) return "E: Harvest";
            return "Growing...";
        }
    }

    private void Awake()
    {
        if (cropRenderer == null)
        {
            Transform visual = transform.Find("CropVisual");
            if (visual != null) cropRenderer = visual.GetComponent<SpriteRenderer>();
        }

        if (cropRenderer == null) Debug.LogError("CropPlot requires a CropVisual SpriteRenderer.", this);
    }

    public override void OnNetworkSpawn()
    {
        All.Add(this);
        stage.OnValueChanged += HandleStageChanged;
        Refresh();
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
        stage.OnValueChanged -= HandleStageChanged;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (stage.Value <= 0 || stage.Value >= RipeStage) return;

        float multiplier = TeamWallet.Instance != null ? TeamWallet.Instance.GrowthMultiplier : 1f;
        growTimer += Time.deltaTime;
        if (growTimer >= secondsPerStage * multiplier)
        {
            growTimer = 0f;
            stage.Value++;
        }
    }

    // Any player can ask; the host decides what happens.
    [ServerRpc(RequireOwnership = false)]
    public void InteractServerRpc()
    {
        if (stage.Value == 0)
        {
            stage.Value = 1;
            growTimer = 0f;
            FeedbackClientRpc(0);
        }
        else if (stage.Value >= RipeStage)
        {
            int bonus = TeamWallet.Instance != null ? TeamWallet.Instance.YieldBonus : 0;
            if (TeamWallet.Instance != null) TeamWallet.Instance.Add(baseHarvestValue + bonus, 0);
            stage.Value = 0;
            FeedbackClientRpc(1);
        }
    }

    // Called by monsters (host only).
    public void Trample()
    {
        if (!IsServer || stage.Value == 0) return;
        stage.Value = 0;
        growTimer = 0f;
        FeedbackClientRpc(2);
    }

    [ClientRpc]
    private void FeedbackClientRpc(int kind)
    {
        if (kind == 0)
        {
            FX.Burst(transform.position, new Color(0.6f, 0.4f, 0.2f), 6, 0.1f, 1.5f);
            AudioManager.Play("plant", 0.5f);
        }
        else if (kind == 1)
        {
            FX.Burst(transform.position, new Color(1f, 0.85f, 0.3f), 12, 0.12f, 3f);
            AudioManager.Play("harvest", 0.6f);
        }
        else
        {
            FX.Burst(transform.position, new Color(0.4f, 0.25f, 0.15f), 10, 0.12f, 2.5f);
            AudioManager.Play("hit", 0.5f);
        }
    }

    private void HandleStageChanged(int previous, int current) { Refresh(); }

    private void Refresh()
    {
        if (cropRenderer == null) return;
        if (stage.Value <= 0 || stageSprites == null || stageSprites.Length == 0)
        {
            cropRenderer.sprite = null;
            return;
        }

        int i = Mathf.Clamp(stage.Value - 1, 0, stageSprites.Length - 1);
        cropRenderer.sprite = stageSprites[i];
    }
}
