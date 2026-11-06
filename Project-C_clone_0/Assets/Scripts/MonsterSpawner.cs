using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Host-only. Spawns a wave of monsters during each night, scaled by night number,
/// number of players and difficulty. At dawn, leftover monsters vanish and players recover.
/// Needs a prefab called "Monster" in a Resources folder (the setup menu makes it).
/// </summary>
public class MonsterSpawner : NetworkBehaviour
{
    private const float SpawnBoundaryInset = 0.5f;

    private TimeController time;
    private GameObject prefab;
    private Bounds mapBounds;
    private bool hasMapBounds;
    private bool wasNight;
    private int spawned;
    private int budget;
    private int nightNumber;
    private float interval;
    private float nextSpawn;

    public override void OnNetworkSpawn()
    {
        time = GetComponent<TimeController>();
        prefab = Resources.Load<GameObject>("Monster");
        if (IsServer && prefab == null) Debug.LogError("MonsterSpawner: no 'Monster' prefab in a Resources folder. Run Project C > Run Full Setup.");
        if (IsServer) hasMapBounds = TryGetMapBounds(out mapBounds);
        if (IsServer && !hasMapBounds) Debug.LogError("MonsterSpawner: could not find the Farm scene's four boundary squares; monster spawning is disabled.");
    }

    private void Update()
    {
        if (!IsServer || time == null || prefab == null || !hasMapBounds) return;

        bool night = time.IsNight.Value;
        if (night && !wasNight) BeginNight();
        if (!night && wasNight) EndNight();
        wasNight = night;

        if (night && spawned < budget && !GameManager.IsOver && Time.time >= nextSpawn)
        {
            nextSpawn = Time.time + interval;
            SpawnOne();
        }
    }

    private void BeginNight()
    {
        nightNumber = time.NightsSurvived.Value + 1;
        int players = Mathf.Max(1, NetworkManager.ConnectedClientsIds.Count);
        float count = (4f + 3f * (nightNumber - 1)) * (0.7f + 0.3f * players) * GameSettings.MonsterCountMultiplier;
        budget = Mathf.Max(1, Mathf.RoundToInt(count));
        spawned = 0;
        interval = Mathf.Max(0.5f, time.NightDuration * 0.7f / budget);
        nextSpawn = Time.time + 2f;
    }

    private void EndNight()
    {
        foreach (Monster m in Monster.All.ToArray())
        {
            if (m != null && m.NetworkObject.IsSpawned) m.NetworkObject.Despawn(true);
        }
        if (GameManager.Instance != null && !GameManager.IsOver) GameManager.Instance.OnDawn();
    }

    private void SpawnOne()
    {
        // Pick a random living player and spawn on a ring just outside the camera view.
        var candidates = new System.Collections.Generic.List<Vector3>();
        foreach (NetworkClient c in NetworkManager.ConnectedClientsList)
        {
            if (c.PlayerObject == null) continue;
            Health h = c.PlayerObject.GetComponent<Health>();
            if (h != null && !h.IsDead) candidates.Add(c.PlayerObject.transform.position);
        }
        if (candidates.Count == 0) return;

        Vector3 center = candidates[Random.Range(0, candidates.Count)];
        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(12f, 15f);
        Vector3 pos = center + (Vector3)offset;
        pos.x = Mathf.Clamp(pos.x, mapBounds.min.x + SpawnBoundaryInset, mapBounds.max.x - SpawnBoundaryInset);
        pos.y = Mathf.Clamp(pos.y, mapBounds.min.y + SpawnBoundaryInset, mapBounds.max.y - SpawnBoundaryInset);

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        Health health = go.GetComponent<Health>();
        health.maxHealth = (5 + nightNumber * 2) * 2;

        Monster monster = go.GetComponent<Monster>();
        monster.speed = Mathf.Min(3.4f, 2.0f + 0.15f * nightNumber);
        monster.moneyReward = 4 + nightNumber;

        go.GetComponent<NetworkObject>().Spawn(true);
        spawned++;
    }

    private static bool TryGetMapBounds(out Bounds bounds)
    {
        bounds = default;
        BoxCollider2D leftWall = null;
        BoxCollider2D rightWall = null;
        BoxCollider2D bottomWall = null;
        BoxCollider2D topWall = null;

        foreach (BoxCollider2D wall in FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if (wall == null || !wall.enabled || !wall.gameObject.activeInHierarchy) continue;

            Bounds wallBounds = wall.bounds;
            if (wallBounds.size.y > 25f)
            {
                if (leftWall == null || wallBounds.center.x < leftWall.bounds.center.x) leftWall = wall;
                if (rightWall == null || wallBounds.center.x > rightWall.bounds.center.x) rightWall = wall;
            }
            else if (wallBounds.size.x > 25f)
            {
                if (bottomWall == null || wallBounds.center.y < bottomWall.bounds.center.y) bottomWall = wall;
                if (topWall == null || wallBounds.center.y > topWall.bounds.center.y) topWall = wall;
            }
        }

        if (leftWall == null || rightWall == null || bottomWall == null || topWall == null) return false;

        Bounds left = leftWall.bounds;
        Bounds right = rightWall.bounds;
        Bounds bottom = bottomWall.bounds;
        Bounds top = topWall.bounds;
        float minX = left.max.x;
        float maxX = right.min.x;
        float minY = bottom.max.y;
        float maxY = top.min.y;
        if (minX >= maxX || minY >= maxY) return false;

        bounds.SetMinMax(new Vector3(minX, minY, 0f), new Vector3(maxX, maxY, 0f));
        return true;
    }
}
