#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-click setup for the killer gameplay prototype:
/// generates the placeholder sprite, the Killer / Target / Corpse / Blood / Detective
/// prefabs and the KillerPrototype scene. Run via: Tools > Game Jam > Setup Killer Prototype Scene.
/// Re-running is safe; existing assets are overwritten.
/// </summary>
public static class KillerPrototypeSetup
{
    private const string SpritePath = "Assets/Art/Prototype/WhiteSquare.png";
    private const string PrefabFolder = "Assets/Prefabs/Prototype";
    private const string ScenePath = "Assets/Scenes/KillerPrototype.unity";
    private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

    [MenuItem("Tools/Game Jam/Setup Killer Prototype Scene")]
    public static void Setup()
    {
        Sprite square = EnsureSquareSprite();
        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        Debug.Assert(actions != null, $"Missing input actions asset at {InputActionsPath}");

        Directory.CreateDirectory(PrefabFolder);

        GameObject corpse = CreateAndSavePrefab(CreateCorpse(square), $"{PrefabFolder}/Corpse.prefab");
        GameObject blood = CreateAndSavePrefab(CreateBlood(square), $"{PrefabFolder}/Blood.prefab");
        GameObject target = CreateAndSavePrefab(CreateTarget(square), $"{PrefabFolder}/KillTarget.prefab");
        GameObject killer = CreateAndSavePrefab(CreateKiller(actions, square, corpse, blood), $"{PrefabFolder}/Killer.prefab");
        GameObject detective = CreateAndSavePrefab(CreateDetective(square), $"{PrefabFolder}/Detective.prefab");

        BuildScene(killer, target, detective, square);

        Debug.Log("[KillerPrototypeSetup] Done. Open 'Assets/Scenes/KillerPrototype.unity' and press Play. " +
                  "Controls: WASD/Arrows move, LeftShift sprint, Space kill. " +
                  "The detective patrols the cyan route; edit 'Patrol Points' children to reshape it.");
    }

    private static Sprite EnsureSquareSprite()
    {
        if (!File.Exists(SpritePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));

            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath);
        }

        // Make sure it imports as a single sprite regardless of project defaults.
        if (AssetImporter.GetAtPath(SpritePath) is TextureImporter importer &&
            importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    private static GameObject CreateKiller(InputActionAsset actions, Sprite square, GameObject corpse, GameObject blood)
    {
        var go = new GameObject("Killer");

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = new Color(0.18f, 0.18f, 0.22f);
        renderer.sortingOrder = 5;

        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        KillerInput input = go.AddComponent<KillerInput>();
        SetField(input, "inputActions", actions);

        go.AddComponent<Stamina>();
        go.AddComponent<KillerIdentity>();

        KillerMovement movement = go.AddComponent<KillerMovement>();
        SetField(movement, "input", input);

        KillerKill kill = go.AddComponent<KillerKill>();
        SetField(kill, "input", input);
        SetField(kill, "corpsePrefab", corpse);
        SetField(kill, "bloodPrefab", blood);

        KillerDebugHud hud = go.AddComponent<KillerDebugHud>();
        SetField(hud, "stamina", go.GetComponent<Stamina>());
        SetField(hud, "movement", movement);
        SetField(hud, "kill", kill);

        return go;
    }

    private static GameObject CreateTarget(Sprite square)
    {
        var go = new GameObject("KillTarget");

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = new Color(0.2f, 0.75f, 0.4f);
        renderer.sortingOrder = 5;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.4f;

        go.AddComponent<KillTarget>();

        return go;
    }

    private static GameObject CreateCorpse(Sprite square)
    {
        var go = new GameObject("Corpse");

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = new Color(0.55f, 0.55f, 0.55f);
        renderer.sortingOrder = 1;
        go.transform.localScale = new Vector3(0.9f, 0.6f, 1f);

        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.4f;
        collider.isTrigger = true;

        Evidence evidence = go.AddComponent<Evidence>();
        SetField(evidence, "type", (int)EvidenceType.Corpse);
        return go;
    }

    private static GameObject CreateBlood(Sprite square)
    {
        var go = new GameObject("Blood");

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = new Color(0.45f, 0.04f, 0.04f);
        renderer.sortingOrder = 0;
        go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.35f;
        collider.isTrigger = true;

        Evidence evidence = go.AddComponent<Evidence>();
        SetField(evidence, "type", (int)EvidenceType.Blood);
        return go;
    }

    private static GameObject CreateDetective(Sprite square)
    {
        var go = new GameObject("Detective");

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = new Color(0.15f, 0.35f, 0.85f);
        renderer.sortingOrder = 5;

        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        go.AddComponent<DetectivePatrol>();
        go.AddComponent<DetectiveDetection>();
        go.AddComponent<DetectiveInvestigation>();
        go.AddComponent<DetectiveKillerDetection>();
        go.AddComponent<DetectiveChase>();
        go.AddComponent<DetectiveDebugHud>();

        return go;
    }

    private static void BuildScene(GameObject killerPrefab, GameObject targetPrefab, GameObject detectivePrefab, Sprite square)
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var cameraGo = new GameObject("Main Camera");
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        cameraGo.AddComponent<UniversalAdditionalCameraData>();

        // Global 2D light (URP)
        var lightGo = new GameObject("Global Light 2D");
        Light2D light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        // Floor (placeholder room)
        var floor = new GameObject("Floor");
        SpriteRenderer floorRenderer = floor.AddComponent<SpriteRenderer>();
        floorRenderer.sprite = square;
        floorRenderer.color = new Color(0.42f, 0.4f, 0.36f);
        floorRenderer.sortingOrder = -10;
        floor.transform.localScale = new Vector3(26f, 16f, 1f);
        floor.transform.position = Vector3.zero;

        // Killer
        GameObject killer = (GameObject)PrefabUtility.InstantiatePrefab(killerPrefab);
        killer.transform.position = Vector3.zero;

        // Targets
        Vector3[] positions =
        {
            new Vector3(-4f, 2.5f, 0f),
            new Vector3(3f, -2f, 0f),
            new Vector3(5f, 3f, 0f),
        };
        foreach (Vector3 position in positions)
        {
            GameObject target = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);
            target.transform.position = position;
        }

        // Detective + prototype patrol route (temporary positions, final warehouse TBD).
        GameObject detective = (GameObject)PrefabUtility.InstantiatePrefab(detectivePrefab);
        detective.transform.position = new Vector3(12f, 0f, 0f);

        var routeParent = new GameObject("Patrol Points");
        Vector3[] routePositions =
        {
            new Vector3(9f, 5f, 0f),
            new Vector3(9f, -5f, 0f),
            new Vector3(-9f, -5f, 0f),
            new Vector3(-9f, 5f, 0f),
        };
        Transform[] waypoints = new Transform[routePositions.Length];
        for (int i = 0; i < routePositions.Length; i++)
        {
            var point = new GameObject($"PatrolPoint_{(char)('A' + i)}");
            point.transform.SetParent(routeParent.transform);
            point.transform.position = routePositions[i];
            waypoints[i] = point.transform;
        }

        DetectivePatrol patrol = detective.GetComponent<DetectivePatrol>();
        SerializedObject serialized = new SerializedObject(patrol);
        SerializedProperty routeProperty = serialized.FindProperty("waypoints");
        routeProperty.arraySize = waypoints.Length;
        for (int i = 0; i < waypoints.Length; i++)
        {
            routeProperty.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

        // Build settings: keep existing entries and add the prototype scene first.
        var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!buildScenes.Exists(s => s.path == ScenePath))
        {
            buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
    }

    private static GameObject CreateAndSavePrefab(GameObject instance, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        return prefab;
    }

    private static void SetField(Object target, string fieldName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetField(Object target, string fieldName, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
