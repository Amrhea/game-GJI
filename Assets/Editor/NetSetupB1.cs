using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEngine.InputSystem;

/// <summary>
/// B1 networking setup — one-click editor utility.
/// Menu: Tools > Game Jam > Setup B1 Network Scene
/// Creates Assets/Prefabs/Net/NetworkPlayer.prefab (via the normal Prefab workflow)
/// and Assets/Scenes/NetTestScene.unity containing the single bootstrap object
/// (NetworkManager + UnityTransport + LanBootstrap + player prefab registration).
/// No GUIDs are hardcoded — components are added via AddComponent so Unity's
/// normal import pipeline assigns package GUIDs from the Package Manager.
/// </summary>
public static class NetSetupB1
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Net/NetworkPlayer.prefab";
    private const string ScenePath = "Assets/Scenes/NetTestScene.unity";
    private const string PlaceholderSpritePath = "Assets/Prefabs/Net/NetworkPlayerPlaceholder.sprite";

    [MenuItem("Tools/Game Jam/Setup B1 Network Scene")]
    public static void Setup()
    {
        EnsureFolders();

        var playerPrefab = CreatePlayerPrefab();
        CreateNetworkScene(playerPrefab);

        AssetDatabase.SaveAssets();
        Debug.Log($"[NetSetupB1] Done.\nPlayer prefab: {PlayerPrefabPath}\nTest scene: {ScenePath} (add it to Build Settings if you build a player)");
    }

    /// <summary>
    /// Opens the Multiplayer Play Mode window (com.unity.multiplayer.playmode,
    /// built-in with Unity 6.6 as "Play Mode Scenarios").
    /// Useful for adding an additional (client) editor instance at Play.
    /// </summary>
    [MenuItem("Tools/Game Jam/Open Multiplayer Play Mode Window")]
    private static void OpenMppmWindow()
    {
        // Unity 6.6 ships MPPM built-in; try the known menu paths without
        // hardcoding any window type or GUID.
        string[] paths =
        {
            "Window/Play Mode/Scenarios",
            "Window/Play Mode/Active Scenario",
            "Window/General/Multiplayer Play Mode",
        };
        foreach (var path in paths)
        {
            if (EditorApplication.ExecuteMenuItem(path))
            {
                return;
            }
        }
        Debug.LogWarning("[NetSetupB1] No MPPM window found — open Window > Play Mode > Scenarios manually (MPPM is built-in in Unity 6.6).");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Net"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Net");
        }
    }

    private static GameObject CreatePlayerPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (existing != null && HasUsableSprite(existing))
        {
            return existing;
        }
        if (existing != null)
        {
            // Old broken prefab (Unity 6 no longer has the builtin Square.psd sprite):
            // destroy and rebuild so the player is actually visible.
            AssetDatabase.DeleteAsset(PlayerPrefabPath);
        }

        var go = new GameObject("NetworkPlayer");

        // Visible placeholder representation (2D project).
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreatePlaceholderSprite();
        sr.color = new Color(0.35f, 0.75f, 1f); // placeholder color only

        // 2D physics (matches the top-down jam movement approach).
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        go.AddComponent<BoxCollider2D>();

        // NGO networking: identity + owner-authoritative transform sync.
        go.AddComponent<NetworkObject>();
        var nt = go.AddComponent<NetworkTransform>();
        nt.AuthorityMode = NetworkTransform.AuthorityModes.Owner;

        // Owner-only input movement (reuses project InputSystem_Actions asset).
        var movement = go.AddComponent<NetworkPlayerMovement>();
        var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/Settings/InputSystem_Actions.inputactions");
        if (inputAsset == null)
        {
            Debug.LogError("[NetSetupB1] InputSystem_Actions.inputactions not found at Assets/Settings/ — assign it manually on the NetworkPlayer prefab.");
        }
        else
        {
            var so = new SerializedObject(movement);
            so.FindProperty("inputActions").objectReferenceValue = inputAsset;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        return PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
    }

    private static bool HasUsableSprite(GameObject prefab)
    {
        var sr = prefab.GetComponent<SpriteRenderer>();
        return sr != null && sr.sprite != null;
    }

    /// <summary>
    /// Build a small white square sprite asset in the project.
    /// Unity 6 removed the legacy builtin "Square.psd" resource, so we generate
    /// our own (PNG -> Sprite) once and reuse it for the placeholder player.
    /// </summary>
    private static Sprite GetOrCreatePlaceholderSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        if (existing != null)
        {
            return existing;
        }

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        texture.SetPixels(pixels);
        texture.Apply();

        // Persist the texture as a PNG so the prefab references a real asset.
        var pngPath = PlaceholderSpritePath.Replace(".sprite", ".png");
        File.WriteAllBytes(pngPath, ImageConversion.EncodeToPNG(texture));
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(pngPath);

        var tex2d = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
        if (tex2d == null)
        {
            Debug.LogError($"[NetSetupB1] Failed to import placeholder texture at {pngPath}");
            return null;
        }

        const float pixelsPerUnit = 32f; // 32px texture -> 1 world unit.
        var sprite = Sprite.Create(
            tex2d,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
        AssetDatabase.CreateAsset(sprite, PlaceholderSpritePath);
        return sprite;
    }

    private static void CreateNetworkScene(GameObject playerPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera for the test scene.
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();

        // The single bootstrap object: NetworkManager + UnityTransport + UI controls.
        var bootstrap = new GameObject("NetworkBootstrap");
        bootstrap.AddComponent<NetworkManager>();
        var utp = bootstrap.AddComponent<UnityTransport>();
        bootstrap.AddComponent<LanBootstrap>();

        // Register the player prefab + wire the transport (normal NetworkManager
        // inspector workflow). AddComponent does NOT auto-assign NetworkTransport;
        // it must be set explicitly or StartHost/StartClient fail with
        // "No transport has been selected!".
        var nm = bootstrap.GetComponent<NetworkManager>();
        var so = new SerializedObject(nm);
        var playerPrefabProp = so.FindProperty("NetworkConfig.PlayerPrefab");
        if (playerPrefabProp != null)
        {
            playerPrefabProp.objectReferenceValue = playerPrefab;
        }
        var transportProp = so.FindProperty("NetworkConfig.NetworkTransport");
        if (transportProp != null)
        {
            transportProp.objectReferenceValue = utp;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath);
    }
}
