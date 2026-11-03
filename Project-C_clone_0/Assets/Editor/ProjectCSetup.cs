#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click setup: Project C > Run Full Setup.
/// Creates the Monster / Fence / Turret / FarmManagers prefabs (in Assets/Resources),
/// registers them as network prefabs, adds the new components to the Player prefab,
/// adds crop plots, a shop and a crafting bench to the Farm scene, sets up the Pixel Perfect Camera,
/// and fixes sprite import settings (Point filter, no compression).
/// Safe to run more than once. Commit to Git afterwards.
/// </summary>
public static class ProjectCSetup
{
    private const int PPU = 32;                 // Cainos art is 32 pixels per unit
    private const int RefX = 640, RefY = 360;   // reference resolution (3x at 1080p, 6x at 4K)

    [MenuItem("Project C/Run Full Setup")]
    public static void RunAll()
    {
        Directory.CreateDirectory("Assets/Resources");
        FixSprites();
        CreatePrefabs();
        SetupPlayerPrefab();
        RegisterNetworkPrefabs();
        SetupFarmScene();
        SetupCamerasInAllScenes();
        EditorUtility.DisplayDialog("Project C", "Setup finished.\n\nNext: open Lobby, press Play, choose 'Play Solo / Same PC', then Start Farm.", "OK");
    }

    // ------------------------------------------------------------ Sprites (pixel perfect)

    [MenuItem("Project C/Fix Sprite Import Settings")]
    public static void FixSprites()
    {
        string[] roots = { "Assets/Cainos", "Assets/Warrior free set" };
        int fixedCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", roots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null || imp.textureType != TextureImporterType.Sprite) continue;

            bool changed = false;
            if (imp.filterMode != FilterMode.Point) { imp.filterMode = FilterMode.Point; changed = true; }
            if (imp.textureCompression != TextureImporterCompression.Uncompressed) { imp.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; changed = true; }
            // Warrior sheet is 100 PPU by default; match everything to the Cainos grid.
            if (imp.spritePixelsPerUnit != PPU) { imp.spritePixelsPerUnit = PPU; changed = true; }
            if (changed) { imp.SaveAndReimport(); fixedCount++; }
        }
        QualitySettings.antiAliasing = 0;
        Debug.Log("Project C: fixed import settings on " + fixedCount + " sprites.");
    }

    // ------------------------------------------------------------ Cameras

    private static void SetupCamerasInAllScenes()
    {
        string[] scenes = { "Assets/Scenes/Menu.unity", "Assets/Scenes/Lobby.unity", "Assets/Scenes/Farm.unity" };
        foreach (string path in scenes)
        {
            if (!File.Exists(path)) continue;
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Camera cam = Camera.main;
            if (cam != null) ConfigureCamera(cam);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void ConfigureCamera(Camera cam)
    {
        PixelPerfectCamera pp = cam.GetComponent<PixelPerfectCamera>();
        if (pp == null) pp = cam.gameObject.AddComponent<PixelPerfectCamera>();

        cam.orthographic = true;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        cam.allowDynamicResolution = false;

        pp.assetsPPU = PPU;
        pp.refResolutionX = RefX;
        pp.refResolutionY = RefY;
        pp.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping; // keeps UI text sharp (no upscale render texture)
        pp.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
        EditorUtility.SetDirty(cam);
    }

    // ------------------------------------------------------------ Prefabs

    private static GameObject SavePrefab(GameObject go, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static Sprite FirstSprite(string folder, string nameContains)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Sprite " + nameContains, new[] { folder }))
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
            {
                Sprite s = o as Sprite;
                if (s != null) return s;
            }
        }
        return null;
    }

    private static Sprite SolidSprite(string name, Color color, int size = 16)
    {
        string path = "Assets/Resources/" + name + ".png";
        if (!File.Exists(path))
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    tex.SetPixel(x, y, edge ? color * 0.6f : color);
                }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = size;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void CreatePrefabs()
    {
        // Monster: a tinted placeholder you can replace with real art later.
        {
            GameObject go = new GameObject("Monster");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SolidSprite("MonsterSprite", new Color(0.75f, 0.35f, 0.8f));
            sr.sortingOrder = 10;
            go.transform.localScale = Vector3.one * 1.2f;
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0; rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            BoxCollider2D col = go.AddComponent<BoxCollider2D>(); col.size = new Vector2(0.8f, 0.8f);
            go.AddComponent<NetworkObject>();
            NetworkTransform nt = go.AddComponent<NetworkTransform>();
            go.AddComponent<Health>();
            go.AddComponent<Monster>();
            SavePrefab(go, "Assets/Resources/Monster.prefab");
        }

        // Fence
        {
            GameObject go = new GameObject("Fence");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SolidSprite("FenceSprite", new Color(0.6f, 0.42f, 0.25f));
            sr.sortingOrder = 5;
            BoxCollider2D col = go.AddComponent<BoxCollider2D>(); col.size = new Vector2(1f, 1f);
            go.AddComponent<NetworkObject>();
            Health h = go.AddComponent<Health>(); h.maxHealth = 20;
            go.AddComponent<Fence>();
            SavePrefab(go, "Assets/Resources/Fence.prefab");
        }

        // Turret
        {
            GameObject go = new GameObject("Turret");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SolidSprite("TurretSprite", new Color(0.45f, 0.55f, 0.7f));
            sr.sortingOrder = 6;
            BoxCollider2D col = go.AddComponent<BoxCollider2D>(); col.size = new Vector2(1f, 1f);
            go.AddComponent<NetworkObject>();
            go.AddComponent<Turret>();
            SavePrefab(go, "Assets/Resources/Turret.prefab");
        }

        // FarmManagers: one networked object holding every host-run system.
        {
            GameObject go = new GameObject("FarmManagers");
            go.AddComponent<NetworkObject>();
            go.AddComponent<TimeController>();
            go.AddComponent<TeamWallet>();
            go.AddComponent<MonsterSpawner>();
            go.AddComponent<GameManager>();
            SavePrefab(go, "Assets/Resources/FarmManagers.prefab");
        }

        // Crop plot (placed by hand in the Farm scene by SetupFarmScene)
        {
            GameObject go = new GameObject("CropPlot");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.5f;
            go.AddComponent<NetworkObject>();
            CropPlot plot = go.AddComponent<CropPlot>();
            plot.stageSprites = new[]
            {
                SolidSprite("SoilSprite", new Color(0.45f, 0.3f, 0.2f)),
                SolidSprite("Crop1Sprite", new Color(0.55f, 0.75f, 0.35f)),
                SolidSprite("Crop2Sprite", new Color(0.4f, 0.8f, 0.3f)),
                SolidSprite("CropRipeSprite", new Color(0.98f, 0.82f, 0.3f)),
            };
            SavePrefab(go, "Assets/Prefabs/CropPlot.prefab");
        }
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------ Player prefab

    private static void SetupPlayerPrefab()
    {
        string path = "Assets/Prefabs/Player.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        if (root.GetComponent<Health>() == null) root.AddComponent<Health>().maxHealth = 12;
        if (root.GetComponent<PlayerCombat>() == null) root.AddComponent<PlayerCombat>();
        if (root.GetComponent<PlayerInteractor>() == null) root.AddComponent<PlayerInteractor>();

        // Make the owner's movement sync to other players (not the host's copy).
        NetworkTransform nt = root.GetComponent<NetworkTransform>();
        if (nt != null)
        {
            SerializedObject so = new SerializedObject(nt);
            SerializedProperty mode = so.FindProperty("AuthorityMode");
            if (mode != null) { mode.enumValueIndex = 1; so.ApplyModifiedPropertiesWithoutUndo(); } // 1 = Owner
        }

        // Players must collide with trigger stations but not each other: fine as-is (triggers + rigidbody gravity 0).
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ------------------------------------------------------------ Network prefabs list

    private static void RegisterNetworkPrefabs()
    {
        NetworkPrefabsList list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
        if (list == null) { Debug.LogWarning("Project C: DefaultNetworkPrefabs.asset not found; add the new prefabs by hand."); return; }

        string[] paths =
        {
            "Assets/Resources/Monster.prefab", "Assets/Resources/Fence.prefab", "Assets/Resources/Turret.prefab",
            "Assets/Resources/FarmManagers.prefab", "Assets/Prefabs/CropPlot.prefab",
        };
        foreach (string p in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (prefab == null) continue;
            bool exists = false;
            foreach (NetworkPrefab np in list.PrefabList) if (np.Prefab == prefab) exists = true;
            if (!exists) list.Add(new NetworkPrefab { Prefab = prefab });
        }
        EditorUtility.SetDirty(list);
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------ Farm scene

    private static void SetupFarmScene()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Farm.unity", OpenSceneMode.Single);

        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "ProjectC_Generated") Object.DestroyImmediate(root);

        GameObject parent = new GameObject("ProjectC_Generated");

        GameObject spawn = new GameObject("PlayerSpawn");
        spawn.transform.SetParent(parent.transform);
        spawn.transform.position = new Vector3(0, 0, 0);

        // 3 x 4 grid of crop plots to the left of the spawn.
        // Scene-placed network objects spawn on the host automatically when the scene loads.
        GameObject plotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CropPlot.prefab");
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                GameObject plot = (GameObject)PrefabUtility.InstantiatePrefab(plotPrefab, parent.transform);
                plot.transform.position = new Vector3(-6f + x * 1.5f, 2.5f - y * 1.5f, 0);
                plot.name = "CropPlot_" + x + "_" + y;
            }
        }

        MakeStation(parent.transform, "Shop", StationType.Shop, new Vector3(5f, 3f, 0), new Color(0.98f, 0.82f, 0.4f));
        MakeStation(parent.transform, "CraftingBench", StationType.Crafting, new Vector3(5f, -1.5f, 0), new Color(0.62f, 0.8f, 0.92f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void MakeStation(Transform parent, string name, StationType type, Vector3 pos, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(2f, 1.5f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SolidSprite(name + "Sprite", color, 16);
        sr.sortingOrder = 4;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);
        go.AddComponent<Station>().type = type;
    }

    // ------------------------------------------------------------ Packages

    [MenuItem("Project C/Add Required Packages")]
    public static void AddPackages()
    {
        UnityEditor.PackageManager.Client.Add("com.unity.services.multiplayer");
        UnityEditor.PackageManager.Client.Add("com.unity.services.leaderboards");
        UnityEditor.PackageManager.Client.Add("com.unity.services.authentication");
        Debug.Log("Project C: requested Multiplayer Services, Leaderboards and Authentication. Then link the project in Project Settings > Services.");
    }
}
#endif
