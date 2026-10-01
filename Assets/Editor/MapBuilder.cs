using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapBuilder
{
    const int   TerrainSize = 500;   // 500 x 500 m
    const float TerrainHeight = 60f;
    const int   HeightRes = 257;

    // Районы по X: 0-200 город | 200-240 дорога-сквер | 240-410 лес с дорогой | 410-500 тюрьма
    const float CityEnd = 200f;
    const float ForestStart = 240f;
    const float PrisonStart = 420f;

    static Material _asphalt, _line, _sidewalk, _building, _roofRed, _roofDark, _wood,
                   _woodDark, _brick, _concrete, _metalFence, _grass, _glass, _stone,
                   _concreteWall, _metal, _sign, _light, _woodWall, _door, _windows,
                   _red, _white, _grey, _dark, _sand, _leafDark, _trunk, _pine;

    [MenuItem("Tools/Anchorage/Build City Map")]
    public static void Build()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Debug.Log("[MapBuilder] === НАЧАЛО ГЕНЕРАЦИИ ===");

        CreateMaterials();
        ClearScene();
        MapRoot = Group("MAP");
        DontDestroyOnLoadInEditor(MapRoot);
        BuildTerrain();
        BuildLighting();

        City();
        ParkBand();
        Forest();
        Countryside();
        Prison();

        SaveScene();
        AssetDatabase.SaveAssets();
        Debug.Log($"[MapBuilder] === ГОТОВО за {sw.Elapsed.TotalMinutes:F1} мин ===");
    }

    // ---------------- сцена / сохранение ----------------

    static void ClearScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Debug.Log("[MapBuilder] Сцена очищена");
    }

    static void SaveScene()
    {
        string path = "Assets/Scene.unity";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
        Debug.Log("[MapBuilder] Сцена сохранена: " + path);
    }

    // ---------------- материалы ----------------

    static Material Mat(string name, Color c, float smooth = 0.1f, float metallic = 0f)
    {
        string dir = "Assets/Generated";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string p = $"{dir}/M_{name}.mat";
        string guid = AssetDatabase.AssetPathToGUID(p);
        Material m = null;
        if (!string.IsNullOrEmpty(guid))
            m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, p);
        }
        m.shader = Shader.Find("Standard");
        m.color = c;
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Glossiness", smooth);
        return m;
    }

    static void CreateMaterials()
    {
        _asphalt   = Mat("Asphalt",  new Color(0.16f, 0.16f, 0.17f), 0.05f);
        _line      = Mat("Line",     new Color(0.85f, 0.85f, 0.75f), 0.05f);
        _sidewalk  = Mat("Sidewalk", new Color(0.55f, 0.55f, 0.54f), 0.05f);
        _building  = Mat("Building", new Color(0.72f, 0.70f, 0.66f), 0.05f);
        _roofRed   = Mat("RoofRed",  new Color(0.55f, 0.16f, 0.13f), 0.05f);
        _roofDark  = Mat("RoofDark", new Color(0.20f, 0.20f, 0.22f), 0.1f);
        _wood      = Mat("Wood",     new Color(0.45f, 0.30f, 0.18f), 0.05f);
        _woodDark  = Mat("WoodDark", new Color(0.28f, 0.18f, 0.11f), 0.05f);
        _brick     = Mat("Brick",    new Color(0.58f, 0.30f, 0.24f), 0.05f);
        _concrete  = Mat("Concrete", new Color(0.62f, 0.61f, 0.58f), 0.06f);
        _grass     = Mat("Grass",    new Color(0.24f, 0.45f, 0.18f), 0.03f);
        _glass     = Mat("Glass",    new Color(0.35f, 0.50f, 0.60f), 0.85f, 0.4f);
        _stone     = Mat("Stone",    new Color(0.50f, 0.48f, 0.46f), 0.05f);
        _concreteWall = Mat("ConcreteWall", new Color(0.58f, 0.58f, 0.56f), 0.06f);
        _metal     = Mat("Metal",    new Color(0.55f, 0.57f, 0.60f), 0.4f, 0.7f);
        _metalFence= Mat("MetalFence", new Color(0.30f, 0.32f, 0.34f), 0.3f, 0.6f);
        _light     = Mat("Light",    new Color(1f, 0.95f, 0.80f), 0.1f);
        _woodWall  = Mat("WoodWall", new Color(0.62f, 0.45f, 0.30f), 0.05f);
        _door      = Mat("Door",     new Color(0.35f, 0.22f, 0.12f), 0.05f);
        _windows   = Mat("Windows",  new Color(0.30f, 0.38f, 0.45f), 0.4f);
        _red       = Mat("Red",      new Color(0.75f, 0.15f, 0.12f), 0.1f);
        _white     = Mat("White",    new Color(0.92f, 0.92f, 0.90f), 0.1f);
        _grey      = Mat("Grey",     new Color(0.35f, 0.36f, 0.38f), 0.1f);
        _dark      = Mat("Dark",     new Color(0.12f, 0.12f, 0.13f), 0.1f);
        _sand      = Mat("Sand",     new Color(0.60f, 0.54f, 0.42f), 0.05f);
        _leafDark  = Mat("LeafDark", new Color(0.13f, 0.30f, 0.12f), 0.03f);
        _trunk     = Mat("Trunk",    new Color(0.30f, 0.21f, 0.14f), 0.05f);
        _pine      = Mat("Pine",     new Color(0.10f, 0.26f, 0.13f), 0.03f);
        Debug.Log("[MapBuilder] Материалы созданы");
    }

    // ---------------- террейн ----------------

    static TerrainData _terrainData;
    static Terrain _terrain;

    static void BuildTerrain()
    {
        string dir = "Assets/Generated";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string tdPath = dir + "/Terrain500.asset";

        var td = AssetDatabase.LoadAssetAtPath<TerrainData>(tdPath);
        if (td == null)
        {
            td = new TerrainData();
            AssetDatabase.CreateAsset(td, tdPath);
        }

        td.heightmapResolution = HeightRes;
        td.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
        td.baseMapResolution = 512;

        int res = td.heightmapResolution;
        var heights = new float[res, res];
        float cell = TerrainSize / (res - 1);

        for (int z = 0; z < res; z++)
        {
            float wz = z * cell;
            for (int x = 0; x < res; x++)
            {
                float wx = x * cell;
                heights[z, x] = 0.5f + Noise(wx, wz) * 0.02f; // почти плоско, мелкие неровности
            }
        }

        // сглаживание зон под дороги/застройку не требуется — карта почти плоская
        td.SetHeights(0, 0, heights);

        // 1 текстурный слой (прототип, чтобы не быть серым)
        var layers = new TerrainLayer[1];
        string tlPath = dir + "/GroundLayer.terrainlayer";
        var tl = AssetDatabase.LoadAssetAtPath<TerrainLayer>(tlPath);
        if (tl == null)
        {
            tl = new TerrainLayer();
            AssetDatabase.CreateAsset(tl, tlPath);
        }
        tl.diffuseTexture = null; // цвет по умолчанию
        layers[0] = tl;
        td.terrainLayers = layers;

        var go = new GameObject("Terrain");
        go.tag = "Terrain";
        _terrain = go.AddComponent<Terrain>();
        var col = go.AddComponent<TerrainCollider>();
        _terrainData = td;
        _terrain.terrainData = td;
        col.terrainData = td;
        go.transform.position = new Vector3(0, 0, 0);

        Debug.Log("[MapBuilder] Terrain 500x500 создан");
    }

    static float Noise(float x, float z)
    {
        float n = Mathf.PerlinNoise(x * 0.01f, z * 0.01f) - 0.5f;
        n += (Mathf.PerlinNoise(x * 0.05f, z * 0.05f) - 0.5f) * 0.3f;
        return n;
    }

    // ---------------- освещение ----------------

    static void BuildLighting()
    {
        var sun = new GameObject("Sun");
        var l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.shadows = LightShadows.Soft;
        l.intensity = 1.1f;
        l.color = new Color(1f, 0.96f, 0.88f);
        sun.transform.rotation = Quaternion.Euler(50, -35, 0);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.45f, 0.55f, 0.70f);
        RenderSettings.ambientEquatorColor = new Color(0.40f, 0.42f, 0.40f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.22f, 0.20f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 200f;
        RenderSettings.fogEndDistance = 700f;
        RenderSettings.fogColor = new Color(0.55f, 0.62f, 0.72f);

        Debug.Log("[MapBuilder] Освещение настроено");
    }

    // ---------------- хелперы создания объектов ----------------

    static Transform Group(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    static void DontDestroyOnLoadInEditor(Transform t) { /* no-op в редакторе */ }

    static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat,
        Transform parent = null, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent ? parent : MapRoot);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = size;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        return go;
    }

    static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat, float rotY,
        Transform parent = null, bool collider = true)
    {
        var go = Box(name, pos, size, mat, parent, collider);
        go.transform.localRotation = Quaternion.Euler(0, rotY, 0);
        return go;
 }

    static GameObject Cylinder(string name, Vector3 pos, float radius, float height, Material mat,
        Transform parent = null, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent ? parent : MapRoot);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        return go;
    }

    static GameObject Sphere(string name, Vector3 pos, float radius, Material mat,
        Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent ? parent : MapRoot);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * radius * 2;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        return go;
    }

    static Transform MapRoot;

    // высота поверхности террейна
    static float GroundY(float x, float z)
    {
        if (_terrain != null) return _terrain.SampleHeight(new Vector3(x, 0, z));
        return 0f;
    }

    static System.Random Rng = new System.Random(7411);
    static float Rand(float a, float b) => a + (float)Rng.NextDouble() * (b - a);
    static int   RandInt(int a, int b) => Rng.Next(a, b + 1);
    static bool  Chance(float p) => Rng.NextDouble() < p;

    // ---------------- ГЕРОИ БИЛДА: части карты ----------------

    // ================= ГОРОД (0..200) =================

    static void City()
    {
        var root = Group("City");

        // Дороги: главная магистраль по Z (центр города Z=100), поперечные улицы
        float mainZ = 100f;
        // главная улица Восток-Запад (вдоль X) на Z=100
        RoadEW(root, 0, CityEnd, mainZ, 12f, "MainStreet");

        // поперечные улицы вдоль Z
        float[] crossX = { 30f, 80f, 130f, 180f };
        foreach (float x in crossX)
            RoadNS(root, x, 10f, 190f, 9f, "CrossStreet_" + x);

        // тротуары вдоль главной
        SidewalkEW(root, 0, CityEnd, mainZ, 12f, 3.5f);
        foreach (float x in crossX)
            SidewalkNS(root, x, 10f, 190f, 9f, 2.5f);

        // разметка главной улицы
        MarkingDashed(root, new Vector3(0, 0.02f, mainZ), CityEnd, 6f, true, "MainMarks");

        // ФОНАРИ вдоль главной улицы, обе стороны
        for (float x = 10f; x < CityEnd; x += 25f)
        {
            StreetLamp(root, new Vector3(x, 0, mainZ + 9f));
            StreetLamp(root, new Vector3(x + 12f, 0, mainZ - 9f));
        }
        // фонари на перекрёстках
        foreach (float x in crossX)
        {
            StreetLamp(root, new Vector3(x + 7f, 0, mainZ + 7f));
        }

        // ключевые здания
        Courthouse(root, new Vector3(55f, 0, mainZ + 40f));
        PoliceStation(root, new Vector3(105f, 0, mainZ + 40f));
        Shop(root, new Vector3(155f, 0, mainZ + 40f));

        // жилая застройка вокруг (кварталы)
        CityBlocks(root);

        // парк-сквер в центре перекрёстка у суда
        CityPark(root, new Vector3(55f, 0, mainZ - 40f));

        // припаркованные машины у зданий
        ParkedCars(root);

        // урны и скамейки на тротуаре
        StreetFurniture(root, mainZ);

        // забор по периметру города
        Debug.Log("[MapBuilder] Город построен");
    }

    // дороги как плоские "ленты" поверх террейна
    static void RoadEW(Transform parent, float x0, float x1, float z, float width, string name)
    {
        float len = x1 - x0;
        float cx = (x0 + x1) * 0.5f;
        Box(name, new Vector3(cx, 0.05f, z), new Vector3(len, 0.1f, width), _asphalt, parent);
    }

    static void RoadNS(Transform parent, float x, float z0, float z1, float width, string name)
    {
        float len = z1 - z0;
        float cz = (z0 + z1) * 0.5f;
        Box(name, new Vector3(x, 0.05f, cz), new Vector3(width, 0.1f, len), _asphalt, parent);
    }

    static void SidewalkEW(Transform parent, float x0, float x1, float z, float roadW, float swW)
    {
        float len = x1 - x0;
        float cx = (x0 + x1) * 0.5f;
        Box("Sidewalk", new Vector3(cx, 0.10f, z + roadW * 0.5f + swW * 0.5f), new Vector3(len, 0.12f, swW), _sidewalk, parent);
        Box("Sidewalk", new Vector3(cx, 0.10f, z - roadW * 0.5f - swW * 0.5f), new Vector3(len, 0.12f, swW), _sidewalk, parent);
    }

    static void SidewalkNS(Transform parent, float x, float z0, float z1, float roadW, float swW)
    {
        float len = z1 - z0;
        float cz = (z0 + z1) * 0.5f;
        Box("Sidewalk", new Vector3(x + roadW * 0.5f + swW * 0.5f, 0.10f, cz), new Vector3(swW, 0.12f, len), _sidewalk, parent);
        Box("Sidewalk", new Vector3(x - roadW * 0.5f - swW * 0.5f, 0.10f, cz), new Vector3(swW, 0.12f, len), _sidewalk, parent);
    }

    static void MarkingDashed(Transform parent, Vector3 start, float length, float step, bool alongX, string name)
    {
        for (float d = 3f; d < length - 3f; d += step)
        {
            var p = alongX
                ? new Vector3(start.x + d, start.y + 0.005f, start.z)
                : new Vector3(start.x, start.y + 0.005f, start.z + d);
            Box("Mark", p, alongX ? new Vector3(2.5f, 0.02f, 0.35f) : new Vector3(0.35f, 0.02f, 2.5f), _line, parent, false);
        }
    }

    static void StreetLamp(Transform parent, Vector3 pos)
    {
        var g = Group("StreetLamp", parent);
        g.localPosition = new Vector3(pos.x, GroundY(pos.x, pos.z), pos.z);

        // основание
        Cylinder("Base", new Vector3(0, 0.15f, 0), 0.45f, 0.3f, _concrete, g);
        // столб
        Cylinder("Pole", new Vector3(0, 2.75f, 0), 0.12f, 5.5f, _metal, g);
        // кронштейн
        Box("Arm", new Vector3(0, 5.35f, 0.7f), new Vector3(0.12f, 0.12f, 1.4f), _metal, g, false);
        // плафон
        Box("Lamp", new Vector3(0, 5.25f, 1.35f), new Vector3(0.55f, 0.25f, 0.9f), _light, g, false);

        // тёплое свечение
        var lt = g.gameObject.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.range = 14f;
        lt.intensity = 1.6f;
        lt.color = new Color(1f, 0.87f, 0.65f);
        lt.transform.localPosition = new Vector3(0, 5.0f, 1.2f);
        lt.shadows = LightShadows.None;
    }

    // ---------- ЗДАНИЕ СУДА ----------
    static void Courthouse(Transform parent, Vector3 pos)
    {
        var g = Group("Courthouse", parent);
        g.localPosition = pos;

        // ступени
        Box("Step1", new Vector3(0, 0.15f, -12.0f), new Vector3(18f, 0.3f, 3f), _stone, g);
        Box("Step2", new Vector3(0, 0.40f, -10.8f), new Vector3(15f, 0.3f, 2f), _stone, g);
        Box("Step3", new Vector3(0, 0.65f, -10.0f), new Vector3(13f, 0.3f, 2f), _stone, g);

        // основной корпус
        Box("MainHall", new Vector3(0, 5f, 0), new Vector3(24f, 9f, 18f), _building, g);
        // колонны по фасаду
        for (int i = 0; i < 6; i++)
        {
            float x = -10f + i * 4f;
            Cylinder("Column" + i, new Vector3(x, 5.5f, -10.5f), 0.55f, 9f, _white, g);
            Sphere("CapTop" + i, new Vector3(x, 10.2f, -10.5f), 0.6f, _white, g);
        }
        // фронтон (треугольная крыша со ступенчатой имитацией)
        Box("Frieze", new Vector3(0, 9.5f, -10.2f), new Vector3(24f, 1.0f, 1.6f), _white, g);
        // треугольник фронтона из двух наклонных плит
        var t1 = Box("PedL", new Vector3(-4.9f, 11.2f, -10.2f), new Vector3(15f, 0.7f, 1.5f), _white, g, false);
        t1.transform.localRotation = Quaternion.Euler(0, 0, 14f);
        var t2 = Box("PedR", new Vector3(4.9f, 11.2f, -10.2f), new Vector3(15f, 0.7f, 1.5f), _white, g, false);
        t2.transform.localRotation = Quaternion.Euler(0, 0, -14f);

        // крыша плоская с парапетом
        Box("Roof", new Vector3(0, 9.55f, 0), new Vector3(25f, 0.5f, 19f), _roofDark, g);
        // купол
        Cylinder("DomeBase", new Vector3(0, 10.1f, 2f), 3.2f, 0.6f, _stone, g);
        Sphere("Dome", new Vector3(0, 11.5f, 2f), 3.0f, _grey, g);
        Cylinder("DomeTip", new Vector3(0, 15.0f, 2f), 0.25f, 1.2f, _metal, g);

        // окна основного зала (передний фасад между колоннами, чуть выше)
        for (int i = 0; i < 5; i++)
        {
            float x = -8.5f + i * 4.25f;
            Box("Win" + i, new Vector3(x, 5.5f, -9.3f), new Vector3(1.8f, 3.2f, 0.15f), _glass, g, false);
        }
        // окна боковые
        for (int i = 0; i < 3; i++)
        {
            Box("WinL" + i, new Vector3(-12.05f, 5.5f, -5f + i * 5f), new Vector3(0.15f, 3.2f, 1.8f), _glass, g, false);
            Box("WinR" + i, new Vector3(12.05f, 5.5f, -5f + i * 5f), new Vector3(0.15f, 3.2f, 1.8f), _glass, g, false);
        }

        // центральные двери
        Box("DoorL", new Vector3(-1.1f, 2.1f, -9.15f), new Vector3(2.0f, 4.2f, 0.3f), _door, g);
        Box("DoorR", new Vector3(1.1f, 2.1f, -9.15f), new Vector3(2.0f, 4.2f, 0.3f), _door, g);

        // флагшток с флагом
        Cylinder("FlagPole", new Vector3(0, 4f, -13.5f), 0.1f, 8f, _metal, g);
        Box("Flag", new Vector3(1.0f, 6.8f, -13.5f), new Vector3(2.0f, 1.2f, 0.05f), _red, g, false);

        Debug.Log("[MapBuilder] Суд построен");
    }

    // ---------- ПОЛИЦИЯ ----------
    static void PoliceStation(Transform parent, Vector3 pos)
    {
        var g = Group("PoliceStation", parent);
        g.localPosition = pos;

        Box("Base", new Vector3(0, 0.2f, -11f), new Vector3(16f, 0.4f, 4f), _concrete, g);
        Box("Step", new Vector3(0, 0.45f, -9.6f), new Vector3(10f, 0.3f, 2f), _concrete, g);

        // двухэтажный корпус
        Box("Floor1", new Vector3(0, 2.4f, 0), new Vector3(18f, 4.4f, 14f), _brick, g);
        Box("Floor2", new Vector3(0, 6.8f, 0), new Vector3(18f, 4.4f, 14f), _brick, g);
        Box("Band", new Vector3(0, 4.7f, 0), new Vector3(18.2f, 0.4f, 14.2f), _concrete, g);
        Box("Roof", new Vector3(0, 9.2f, 0), new Vector3(18.6f, 0.4f, 14.6f), _roofDark, g);

        // вход с козырьком
        Box("Canopy", new Vector3(0, 3.6f, -8.4f), new Vector3(6f, 0.25f, 3f), _metal, g, false);
        Cylinder("CanSup1", new Vector3(-2.5f, 1.9f, -9.3f), 0.1f, 3.4f, _metal, g, false);
        Cylinder("CanSup2", new Vector3(2.5f, 1.9f, -9.3f), 0.1f, 3.4f, _metal, g, false);

        // окна 1 этаж
        for (int i = 0; i < 5; i++)
        {
            float x = -7.5f + i * 3.75f;
            if (Mathf.Abs(x) < 2f) continue;
            Box("W1_" + i, new Vector3(x, 2.4f, -7.05f), new Vector3(2.2f, 2.0f, 0.15f), _glass, g, false);
        }
        // окна 2 этаж
        for (int i = 0; i < 5; i++)
        {
            float x = -7.5f + i * 3.75f;
            Box("W2_" + i, new Vector3(x, 6.8f, -7.05f), new Vector3(2.2f, 2.2f, 0.15f), _glass, g, false);
        }

        // дверь
        Box("Door", new Vector3(0, 1.5f, -7.05f), new Vector3(3.2f, 3.0f, 0.3f), _dark, g);

        // надпись "POLICE" — блоки-буквы имитация (синяя полоса + белые блоки)
        Box("SignBand", new Vector3(0, 9.0f, -7.1f), new Vector3(14f, 0.8f, 0.2f), _dark, g, false);
        Box("P1", new Vector3(-5.5f, 9.0f, -7.25f), new Vector3(0.6f, 0.5f, 0.1f), _white, g, false);
        Box("P2", new Vector3(-4.7f, 9.0f, -7.25f), new Vector3(0.6f, 0.5f, 0.1f), _white, g, false);
        Box("P3", new Vector3(5.0f, 9.0f, -7.25f), new Vector3(0.6f, 0.5f, 0.1f), _white, g, false);
        Box("P4", new Vector3(5.8f, 9.0f, -7.25f), new Vector3(0.6f, 0.5f, 0.1f), _white, g, false);

        // мигалка на крыше (красно-синяя)
        Box("BeaconR", new Vector3(-1.2f, 9.6f, 0), new Vector3(1.4f, 0.5f, 1.4f), _red, g, false);
        Box("BeaconB", new Vector3(1.2f, 9.6f, 0), new Vector3(1.4f, 0.5f, 1.4f), _glass, g, false);

        // полицейские машины у входа
        PoliceCar(g, new Vector3(-8f, 0, -14f), 5f);
        PoliceCar(g, new Vector3(8f, 0, -14f), 185f);

        // парковочная зона
        Box("PD_Park", new Vector3(0, 0.04f, -16.5f), new Vector3(24f, 0.08f, 7f), _asphalt, g);
        MarkingDashed(g, new Vector3(-11f, 0.09f, -16.5f), 22f, 4f, true, "PD_ParkMarks");

        Debug.Log("[MapBuilder] Полиция построена");
    }

    // ---------- МАГАЗИН ----------
    static void Shop(Transform parent, Vector3 pos)
    {
        var g = Group("Shop", parent);
        g.localPosition = pos;

        Box("Floor", new Vector3(0, 0.2f, -10f), new Vector3(14f, 0.4f, 3f), _concrete, g);

        // одноэтажный корпус со стеклянным фасадом
        Box("Body", new Vector3(0, 2.2f, 0), new Vector3(14f, 4.4f, 12f), _building, g);
        // стеклянная витрина
        Box("Storefront", new Vector3(0, 2.2f, -6.05f), new Vector3(11f, 3.4f, 0.3f), _glass, g, false);
        // рама витрины
        Box("FrameL", new Vector3(-6.2f, 2.2f, -6.1f), new Vector3(1f, 4.0f, 0.4f), _metal, g, false);
        Box("FrameR", new Vector3(6.2f, 2.2f, -6.1f), new Vector3(1f, 4.0f, 0.4f), _metal, g, false);
        Box("FrameTop", new Vector3(0, 4.3f, -6.1f), new Vector3(11f, 0.5f, 0.4f), _metal, g, false);

        // козырёк
        Box("Awning", new Vector3(0, 4.6f, -7.3f), new Vector3(12.5f, 0.3f, 2.5f), _red, g, false);

        // вывеска
        Box("Sign", new Vector3(0, 5.3f, -6.2f), new Vector3(13f, 1.1f, 0.3f), _white, g, false);

        // плоская крыша + кондиционеры
        Box("Roof", new Vector3(0, 4.55f, 0), new Vector3(14.5f, 0.35f, 12.5f), _roofDark, g);
        Box("AC1", new Vector3(-4f, 5.1f, 3f), new Vector3(1.5f, 1.0f, 1.5f), _grey, g, false);
        Box("AC2", new Vector3(4f, 5.1f, 3f), new Vector3(1.5f, 1.0f, 1.5f), _grey, g, false);

        // дверь
        Box("Door", new Vector3(0, 1.5f, -6.2f), new Vector3(2.4f, 3.0f, 0.3f), _glass, g);

        // парковка перед магазином
        Box("Parking", new Vector3(0, 0.04f, -14f), new Vector3(20f, 0.08f, 8f), _asphalt, g);
        MarkingDashed(g, new Vector3(-9.5f, 0.09f, -14f), 19f, 4.5f, true, "ShopParkMarks");
        // фонари парковки
        StreetLamp(g, new Vector3(-8f, 0, -18f));
        StreetLamp(g, new Vector3(8f, 0, -18f));

        // корзины/стеллажи у входа
        Box("Rack1", new Vector3(-4.5f, 0.7f, -8.2f), new Vector3(1.2f, 1.4f, 0.8f), _grey, g);
        Box("Rack2", new Vector3(4.5f, 0.7f, -8.2f), new Vector3(1.2f, 1.4f, 0.8f), _grey, g);

        Debug.Log("[MapBuilder] Магазин построен");
    }

    // ---------- машины ----------
    static System.Collections.Generic.List<Color> _carColors;

    static void PoliceCar(Transform parent, Vector3 pos, float rotY)
    {
        var mat = new Color(0.9f, 0.9f, 0.92f);
        var g = Group("PoliceCar", parent);
        g.localPosition = pos;
        g.localRotation = Quaternion.Euler(0, rotY, 0);
        CarBody(g, new Color(0.08f, 0.09f, 0.12f), 0.12f);
        // полицейская раскраска — белые двери
        Box("Livery", new Vector3(0, 0.75f, 0), new Vector3(2.05f, 0.6f, 2.4f), _white, g, false);
        // проблесковые маячки
        Box("BeacB", new Vector3(-0.45f, 1.35f, 0), new Vector3(0.7f, 0.22f, 0.35f), _red, g, false);
        Box("BeacR", new Vector3(0.45f, 1.35f, 0), new Vector3(0.7f, 0.22f, 0.35f), _glass, g, false);
    }

    static void Car(Transform parent, Vector3 pos, float rotY, Color c)
    {
        var g = Group("Car", parent);
        g.localPosition = pos;
        g.localRotation = Quaternion.Euler(0, rotY, 0);
        CarBody(g, c, 0.2f);
    }

    static void CarBody(Transform g, Color c, float smooth)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c; m.SetFloat("_Glossiness", smooth); m.SetFloat("_Metallic", 0.5f);
        _runtimeMats.Add(m);

        Box("Body", new Vector3(0, 0.55f, 0), new Vector3(2.0f, 0.5f, 4.4f), m, g, false);
        Box("Cabin", new Vector3(0, 1.0f, -0.2f), new Vector3(1.8f, 0.55f, 2.2f), m, g, false);
        // стёкла
        Box("GlassF", new Vector3(0, 1.0f, 0.9f), new Vector3(1.6f, 0.45f, 0.1f), _glass, g, false);
        Box("GlassB", new Vector3(0, 1.0f, -1.3f), new Vector3(1.6f, 0.45f, 0.1f), _glass, g, false);
        Box("GlassL", new Vector3(-0.92f, 1.0f, -0.2f), new Vector3(0.08f, 0.42f, 1.9f), _glass, g, false);
        Box("GlassR", new Vector3(0.92f, 1.0f, -0.2f), new Vector3(0.08f, 0.42f, 1.9f), _glass, g, false);
        // колёса
        Wheel(g, new Vector3(-0.95f, 0.32f, 1.4f));
        Wheel(g, new Vector3(0.95f, 0.32f, 1.4f));
        Wheel(g, new Vector3(-0.95f, 0.32f, -1.4f));
        Wheel(g, new Vector3(0.95f, 0.32f, -1.4f));
        // фары
        Box("HeadL", new Vector3(-0.6f, 0.55f, 2.16f), new Vector3(0.35f, 0.15f, 0.06f), _light, g, false);
        Box("HeadR", new Vector3(0.6f, 0.55f, 2.16f), new Vector3(0.35f, 0.15f, 0.06f), _light, g, false);
    }

    static readonly System.Collections.Generic.List<Material> _runtimeMats = new System.Collections.Generic.List<Material>();

    static void Wheel(Transform g, Vector3 p)
    {
        var w = Cylinder("Wheel", p, 0.32f, 0.28f, _dark, g, false);
        w.transform.localRotation = Quaternion.Euler(0, 0, 90);
    }

    static void ParkedCars(Transform parent)
    {
        var park = Group("ParkedCars", parent);
        Color[] palette =
        {
            new Color(0.75f,0.75f,0.78f), new Color(0.20f,0.22f,0.25f),
            new Color(0.55f,0.10f,0.10f), new Color(0.15f,0.30f,0.55f),
            new Color(0.85f,0.70f,0.25f), new Color(0.35f,0.55f,0.35f)
        };
        int i = 0;
        // вдоль главной улицы
        for (float x = 15f; x < 195f; x += 12f)
        {
            if (x > 40f && x < 70f) continue;  // зона суда
            if (x > 90f && x < 120f) continue; // зона полиции
            if (x > 140f && x < 175f) continue; // зона магазина
            Car(park, new Vector3(x, 0, 100f - 5.2f), 90f, palette[i++ % palette.Length]);
            if (Chance(0.6f)) Car(park, new Vector3(x + 6, 0, 100f + 5.2f), 270f, palette[i++ % palette.Length]);
        }
        // у магазина
        Car(park, new Vector3(148f, 0, 126f - 14f + 4f), 0f, palette[i++ % palette.Length]);
        Car(park, new Vector3(162f, 0, 112f), 0f, palette[i++ % palette.Length]);
    }

    // ---------- городские кварталы ----------
    static void CityBlocks(Transform parent)
    {
        var root = Group("CityBlocks", parent);
        float mainZ = 100f;

        // кварталы между улицами: x-границы [0..30..80..130..180..200], z: 10..88 и 112..190
        float[] xEdges = { 0f, 30f, 80f, 130f, 180f, 200f };

        for (int bi = 0; bi < xEdges.Length - 1; bi++)
        {
            float x0 = xEdges[bi] + 7f, x1 = xEdges[bi + 1] - 7f;
            if (x1 - x0 < 10f) continue;

            // верхняя сторона (z от 15 до 85)
            FillBlock(root, x0, x1, 18f, 82f, mainZ + 1);
            // нижняя сторона (z от 118 до 185)
            FillBlock(root, x0, x1, 118f, 185f, mainZ + 1);
        }
    }

    static void FillBlock(Transform root, float x0, float x1, float z0, float z1, float mainZ)
    {
        // пропускаем зоны ключевых зданий (суд 55/140, полиция 105/140, магазин 155/140)
        bool court  = Overlap(x0, x1, 40f, 70f) && Overlap(z0, z1, 125f, 165f);
        bool police = Overlap(x0, x1, 93f, 120f) && Overlap(z0, z1, 125f, 165f);
        bool shop   = Overlap(x0, x1, 145f, 170f) && Overlap(z0, z1, 125f, 165f);
        bool park   = Overlap(x0, x1, 40f, 70f) && Overlap(z0, z1, 35f, 75f);
        if (court || police || shop) return;

        // сетка домиков в квартале
        float step = 17f;
        int rows = Mathf.Max(1, (int)((z1 - z0) / step));
        int cols = Mathf.Max(1, (int)((x1 - x0) / step));

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float px = x0 + c * step + step * 0.5f;
                float pz = z0 + r * step + step * 0.5f;
                if (px > x1 - 6f || pz > z1 - 6f) continue;
                if (Chance(0.12f)) continue; // пустыри

                if (park && ((px - 55f) * (px - 55f) + (pz - 55f) * (pz - 55f)) < 400f) continue;

                float h = Rand(6f, 14f);
                CityHouse(root, new Vector3(px, 0, pz), h, Rand(0f, 360f));
            }
        }

        // деревья на пустырях
        if (Chance(0.7f))
        {
            for (int t = 0; t < RandInt(2, 5); t++)
            {
                float tx = Rand(x0, x1), tz = Rand(z0, z1);
                Tree(root, new Vector3(tx, 0, tz), Rand(3.5f, 6f), false);
            }
        }
    }

    static bool Overlap(float a0, float a1, float b0, float b1) => !(a1 < b0 || b1 < a0);

    static void CityHouse(Transform parent, Vector3 pos, float height, float rotY)
    {
        var g = Group("CityHouse", parent);
        g.localPosition = pos;
        g.localRotation = Quaternion.Euler(0, rotY, 0);

        float w = Rand(8f, 13f), d = Rand(7f, 11f);
        int floors = Mathf.Max(1, (int)(height / 3.5f));
        Color[] wallC =
        {
            new Color(0.72f,0.70f,0.66f), new Color(0.62f,0.64f,0.60f),
            new Color(0.80f,0.76f,0.70f), new Color(0.55f,0.52f,0.50f),
            new Color(0.68f,0.60f,0.52f)
        };
        var wm = new Material(Shader.Find("Standard"));
        wm.color = wallC[RandInt(0, wallC.Length - 1)];
        wm.SetFloat("_Glossiness", 0.05f);
        _runtimeMats.Add(wm);

        Box("Body", new Vector3(0, height * 0.5f, 0), new Vector3(w, height, d), wm, g);
        // плоская крыша с парапетом
        Box("Roof", new Vector3(0, height + 0.2f, 0), new Vector3(w + 0.6f, 0.4f, d + 0.6f), _roofDark, g);
        // окна по 2 фасадам
        int rowsW = Mathf.Max(1, floors);
        for (int f = 0; f < rowsW; f++)
        {
            float y = 1.8f + f * 3.4f;
            if (y > height - 1.2f) break;
            int n = Mathf.Max(2, (int)(w / 4f));
            for (int i = 0; i < n; i++)
            {
                float x = -w * 0.5f + (i + 0.5f) * (w / n);
                Box("Win" + f + "_" + i, new Vector3(x, y, d * 0.5f + 0.03f), new Vector3(1.2f, 1.6f, 0.1f), _windows, g, false);
            }
        }
        // входная дверь
        Box("Door", new Vector3(0, 1.05f, d * 0.5f + 0.05f), new Vector3(1.4f, 2.1f, 0.15f), _door, g, false);
        // пожарная лестница на заднем фасаде
        if (floors > 2 && Chance(0.5f))
        {
            for (int f = 1; f <= floors; f++)
            {
                float y = 1.8f + (f - 1) * 3.4f;
                Box("Lad" + f, new Vector3(w * 0.25f, y, -d * 0.5f - 0.3f), new Vector3(2.5f, 0.1f, 0.5f), _metal, g, false);
            }
        }
        // кондиционер / вентиляция
        if (Chance(0.4f))
            Box("AC", new Vector3(Rand(-w * 0.3f, w * 0.3f), height + 0.8f, 0), new Vector3(1.2f, 0.8f, 1.2f), _grey, g, false);
    }

    // ---------- дерево ----------
    static void Tree(Transform parent, Vector3 pos, float height, bool pine)
    {
        var g = Group("Tree", parent);
        g.localPosition = new Vector3(pos.x, GroundY(pos.x, pos.z), pos.z);

        float trunkH = height * 0.45f;
        Cylinder("Trunk", new Vector3(0, trunkH * 0.5f, 0), height * 0.035f, trunkH, _trunk, g, false);

        if (pine)
        {
            // ёлка из 3 конусов
            Cylinder("C1", new Vector3(0, trunkH + 0.7f, 0), height * 0.30f, 1.6f, _pine, g, false);
            Cylinder("C2", new Vector3(0, trunkH + 2.2f, 0), height * 0.23f, 1.5f, _pine, g, false);
            Cylinder("C3", new Vector3(0, trunkH + 3.6f, 0), height * 0.15f, 1.4f, _pine, g, false);
            g.localScale = new Vector3(1, height * 0.22f, 1);
        }
        else
        {
            // лиственное: несколько сфер кроны
            float cr = height * 0.22f;
            var m = Chance(0.5f) ? _leafDark : _grass;
            Sphere("Leaf1", new Vector3(0, trunkH + cr * 0.6f, 0), cr * 1.3f, m, g);
            Sphere("Leaf2", new Vector3(cr * 0.7f, trunkH + cr * 1.1f, cr * 0.3f), cr, m, g);
            Sphere("Leaf3", new Vector3(-cr * 0.6f, trunkH + cr * 1.0f, -cr * 0.5f), cr * 0.9f, m, g);
            Sphere("Leaf4", new Vector3(0.1f, trunkH + cr * 1.4f, cr * 0.5f), cr * 0.7f, m, g);
        }
    }

    // ---------- парковый сквер ----------
    static void CityPark(Transform parent, Vector3 pos)
    {
        var g = Group("CityPark", parent);
        g.localPosition = pos;

        // газон
        Box("Lawn", new Vector3(0, 0.06f, 0), new Vector3(30f, 0.1f, 40f), _grass, g, false);
        // дорожки (крест)
        Box("PathH", new Vector3(0, 0.12f, 0), new Vector3(30f, 0.02f, 2.5f), _sand, g, false);
        Box("PathV", new Vector3(0, 0.12f, 0), new Vector3(2.5f, 0.02f, 40f), _sand, g, false);

        // фонтан в центре
        var f = Group("Fountain", g);
        Cylinder("FBasin", new Vector3(0, 0.4f, 0), 3.5f, 0.8f, _stone, f);
        Cylinder("FWater", new Vector3(0, 0.55f, 0), 3.1f, 0.3f, _glass, f, false);
        Cylinder("FPillar", new Vector3(0, 1.2f, 0), 0.5f, 2.4f, _stone, f);
        Sphere("FTop", new Vector3(0, 2.8f, 0), 0.5f, _stone, f);

        // деревья по периметру
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f;
            Tree(g, new Vector3(Mathf.Cos(a) * 13f, 0, Mathf.Sin(a) * 17f), Rand(4f, 6f), false);
        }

        // скамейки вдоль дорожек
        for (int i = -2; i <= 2; i++)
        {
            if (i == 0) continue;
            Bench(g, new Vector3(i * 6f, 0, 4.5f), 0f);
            Bench(g, new Vector3(i * 6f, 0, -4.5f), 180f);
        }

        // урны
        for (int i = -1; i <= 1; i += 2)
        {
            Cylinder("Bin", new Vector3(i * 4.5f, 0.45f, 3.2f), 0.35f, 0.9f, _dark, g);
        }

        // фонари парка
        StreetLamp(g, new Vector3(-12f, 0, 0));
        StreetLamp(g, new Vector3(12f, 0, 0));
    }

    static void Bench(Transform parent, Vector3 pos, float rotY)
    {
        var g = Group("Bench", parent);
        g.localPosition = pos;
        g.localRotation = Quaternion.Euler(0, rotY, 0);
        Box("Seat", new Vector3(0, 0.45f, 0), new Vector3(2.2f, 0.1f, 0.55f), _wood, g, false);
        Box("Back", new Vector3(0, 0.8f, -0.25f), new Vector3(2.2f, 0.5f, 0.08f), _wood, g, false);
        Box("LegL", new Vector3(-0.9f, 0.22f, 0), new Vector3(0.1f, 0.44f, 0.45f), _metal, g, false);
        Box("LegR", new Vector3(0.9f, 0.22f, 0), new Vector3(0.1f, 0.44f, 0.45f), _metal, g, false);
    }

    static void StreetFurniture(Transform parent, float mainZ)
    {
        var g = Group("StreetFurniture", parent);
        // урны и скамейки вдоль главной
        for (float x = 20f; x < 190f; x += 40f)
        {
            Cylinder("Bin", new Vector3(x, 0.55f, mainZ + 8.2f), 0.35f, 0.9f, _dark, g);
            Bench(g, new Vector3(x + 8f, 0, mainZ + 8.5f), 180f);
            Bench(g, new Vector3(x + 20f, 0, mainZ - 8.5f), 0f);
        }
        // гидранты
        for (float x = 45f; x < 180f; x += 55f)
        {
            Cylinder("Hydrant", new Vector3(x, 0.35f, mainZ - 8.5f), 0.22f, 0.7f, _red, g);
        }
    }

    // ================= ДОРОГА-СКВЕР (200..240) =================

    static void ParkBand()
    {
        var root = Group("ParkBand");

        // грунт полосы
        Box("Ground", new Vector3(220f, 0.03f, 250f), new Vector3(40f, 0.06f, 500f), _grass, root, false);

        // дорога сквозь сквер (по центру Z=250? нет — дорога вдоль X на Z=250 не верно;
        // дорога-магистраль продолжается по Z=100? По макету: город | дорога | лес | дорога | деревня | тюрьма.
        // Продлеваем главную дорогу из города в лес вдоль Z=100
        RoadEW(root, CityEnd, ForestStart, 100f, 12f, "Highway1");
        MarkingDashed(root, new Vector3(CityEnd, 0.02f, 100f), ForestStart - CityEnd, 6f, true, "HW1Marks");

        // вторая дорога на юг (объездная) на Z=350 для разнообразия
        RoadEW(root, CityEnd, ForestStart, 350f, 9f, "Highway2");
        MarkingDashed(root, new Vector3(CityEnd, 0.02f, 350f), ForestStart - CityEnd, 6f, true, "HW2Marks");

        // деревья в сквере
        for (int i = 0; i < 60; i++)
        {
            float x = Rand(203f, 237f), z = Rand(10f, 490f);
            // не ставим деревья на дороге
            if (Mathf.Abs(z - 100f) < 10f || Mathf.Abs(z - 350f) < 8f) continue;
            Tree(root, new Vector3(x, 0, z), Rand(4f, 7f), Chance(0.4f));
        }

        // кусты (маленькие сферы)
        for (int i = 0; i < 40; i++)
        {
            float x = Rand(202f, 238f), z = Rand(15f, 485f);
            if (Mathf.Abs(z - 100f) < 10f || Mathf.Abs(z - 350f) < 8f) continue;
            Sphere("Bush", new Vector3(x, 0.35f, z), Rand(0.5f, 1.0f), _leafDark, root);
        }

        // фонари вдоль шоссе (реже)
        for (float x = 205f; x < ForestStart; x += 30f)
        {
            StreetLamp(root, new Vector3(x, 0, 100f + 8f));
        }

        Debug.Log("[MapBuilder] Полоса-сквер построена");
    }

    // ================= ЛЕС (240..420) =================

    static void Forest()
    {
        var root = Group("Forest");

        // лесная дорога — извилистая, из сегментов вдоль X на Z около 250 (между шоссе)
        // Основная дорога сквозь лес: Z от 350 к 250 кривой
        ForestRoad(root);

        // густой лес: сетка с джиттером
        for (float x = 244f; x < 416f; x += 6.5f)
        {
            for (float z = 8f; z < 492f; z += 6.5f)
            {
                float jx = x + Rand(-2.5f, 2.5f), jz = z + Rand(-2.5f, 2.5f);
                // пропускаем дороги
                if (OnForestRoad(jx, jz)) continue;
                if (Chance(0.75f))
                {
                    Tree(root, new Vector3(jx, 0, jz), Rand(5f, 10f), Chance(0.65f));
                }
                else if (Chance(0.3f))
                {
                    Sphere("Bush", new Vector3(jx, 0.3f, jz), Rand(0.5f, 1.2f), _leafDark, root);
                }
            }
        }

        // поваленные брёвна, камни
        for (int i = 0; i < 30; i++)
        {
            float x = Rand(245f, 415f), z = Rand(15f, 485f);
            if (OnForestRoad(x, z)) continue;
            if (Chance(0.5f))
            {
                var log = Cylinder("Log", new Vector3(x, 0.4f, z), 0.4f, 3f, _trunk, root);
                log.transform.localRotation = Quaternion.Euler(0, Rand(0, 360), 90);
            }
            else
            {
                Sphere("Rock", new Vector3(x, 0.25f, z), Rand(0.5f, 1.4f), _stone, root);
            }
        }

        Debug.Log("[MapBuilder] Лес построен");
    }

    // лесная дорога: сегменты по точкам, S-образная
    static readonly Vector2[] ForestRoadPts =
    {
        new Vector2(240f, 350f), new Vector2(265f, 335f), new Vector2(290f, 345f),
        new Vector2(315f, 330f), new Vector2(340f, 345f), new Vector2(365f, 335f),
        new Vector2(390f, 350f), new Vector2(415f, 345f), new Vector2(430f, 350f)
    };

    static void ForestRoad(Transform root)
    {
        for (int i = 0; i < ForestRoadPts.Length - 1; i++)
        {
            var a = ForestRoadPts[i];
            var b = ForestRoadPts[i + 1];
            // сегмент дороги: куб, ориентированный от a к b
            var mid = (a + b) * 0.5f;
            float len = Vector2.Distance(a, b);
            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            var seg = Box("FRoadSeg" + i, new Vector3(mid.x, 0.05f, mid.y), new Vector3(len + 2f, 0.1f, 8f), _asphalt, root);
            seg.transform.rotation = Quaternion.Euler(0, -ang, 0);
        }
        // грунтовая обочина
        for (int i = 0; i < ForestRoadPts.Length - 1; i++)
        {
            var a = ForestRoadPts[i];
            var b = ForestRoadPts[i + 1];
            var mid = (a + b) * 0.5f;
            float len = Vector2.Distance(a, b);
            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            var seg = Box("FShoulder" + i, new Vector3(mid.x, 0.04f, mid.y), new Vector3(len + 2.5f, 0.08f, 11f), _sand, root, false);
            seg.transform.rotation = Quaternion.Euler(0, -ang, 0);
        }
    }

    static bool OnForestRoad(float x, float z)
    {
        // расстояние до полилинии дороги
        for (int i = 0; i < ForestRoadPts.Length - 1; i++)
        {
            if (DistToSeg(new Vector2(x, z), ForestRoadPts[i], ForestRoadPts[i + 1]) < 6.5f)
                return true;
        }
        return false;
    }

    static float DistToSeg(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    // ================= ДЕРЕВНЯ + ТЮРЬМА (420..500) =================

    static void Countryside()
    {
        var root = Group("Countryside");

        // дорога от леса к деревне/тюрьме: от (430,350) до (460,350) прямо и дальше к тюрьме
        RoadEW(root, 415f, 470f, 350f, 8f, "VillageRoad1");
        MarkingDashed(root, new Vector3(415f, 0.02f, 350f), 55f, 6f, true, "VR1Marks");

        // дорога к тюрьме (на юг)
        RoadNS(root, 465f, 300f, 420f, 8f, "PrisonRoad");
        MarkingDashed(root, new Vector3(465f, 0.02f, 300f), 120f, 6f, false, "PRMarks");

        // деревенские домики вдоль дороги Z=350, X 420..460
        float[] houseX = { 428f, 436f, 444f, 452f };
        int hn = 1;
        foreach (float hx in houseX)
        {
            if (Chance(0.7f))
                VillageHouse(root, new Vector3(hx, 0, 350f - 20f), 0f, "VillageHouse_N" + hn);
            if (Chance(0.7f))
                VillageHouse(root, new Vector3(hx + 4f, 0, 350f + 20f), 180f, "VillageHouse_S" + hn);
            hn++;
        }

        // остановки вдоль дороги
        BusStop(root, new Vector3(425f, 0, 350f + 6.5f), 0f);
        BusStop(root, new Vector3(450f, 0, 350f - 6.5f), 180f);
        // остановка в лесной зоне на дороге
        BusStop(root, new Vector3(300f, 0, 345f + 6f), 0f);
        BusStop(root, new Vector3(370f, 0, 340f - 6f), 180f);
        // остановка на шоссе у города
        BusStop(root, new Vector3(212f, 0, 100f + 8f), 0f);

        // деревенские заборы
        WoodFence(root, new Vector3(424f, 0, 350f - 32f), 3f, 26f);
        WoodFence(root, new Vector3(440f, 0, 350f + 32f), 3f, 26f);

        // огороды (грядки)
        for (int i = 0; i < 4; i++)
        {
            float gx = 424f + i * 9f;
            for (int r = 0; r < 4; r++)
            {
                Box("GardenRow", new Vector3(gx, 0.15f, 362f + r * 1.5f), new Vector3(7f, 0.25f, 0.8f), _grass, root, false);
            }
        }

        // сеновал/амбар
        Barn(root, new Vector3(448f, 0, 390f));

        // деревья вокруг деревни
        for (int i = 0; i < 25; i++)
        {
            float x = Rand(420f, 500f), z = Rand(280f, 480f);
            if (Mathf.Abs(z - 350f) < 12f || Mathf.Abs(x - 465f) < 8f) continue;
            Tree(root, new Vector3(x, 0, z), Rand(4f, 7f), false);
        }

        Debug.Log("[MapBuilder] Деревня построена");
    }

    static void VillageHouse(Transform parent, Vector3 pos, float rotY, string name)
    {
        var g = Group(name, parent);
        g.localPosition = pos;
        g.localRotation = Quaternion.Euler(0, rotY, 0);

        // стены из бруса
        Box("Walls", new Vector3(0, 1.5f, 0), new Vector3(7f, 3f, 6f), _woodWall, g);
        // двускатная крыша (2 наклонные плиты)
        var r1 = Box("RoofL", new Vector3(0, 3.9f, -1.75f), new Vector3(7.6f, 0.25f, 4.2f), _roofRed, g, false);
        r1.transform.localRotation = Quaternion.Euler(35f, 0, 0);
        var r2 = Box("RoofR", new Vector3(0, 3.9f, 1.75f), new Vector3(7.6f, 0.25f, 4.2f), _roofRed, g, false);
        r2.transform.localRotation = Quaternion.Euler(-35f, 0, 0);
        // конёк
        Box("Ridge", new Vector3(0, 4.6f, 0), new Vector3(7.6f, 0.25f, 0.3f), _woodDark, g, false);

        // окно со ставнями
        Box("Window", new Vector3(0, 1.6f, 3.05f), new Vector3(1.4f, 1.4f, 0.12f), _glass, g, false);
        Box("ShutterL", new Vector3(-1.0f, 1.6f, 3.05f), new Vector3(0.5f, 1.4f, 0.1f), _woodDark, g, false);
        Box("ShutterR", new Vector3(1.0f, 1.6f, 3.05f), new Vector3(0.5f, 1.4f, 0.1f), _woodDark, g, false);
        // дверь
        Box("Door", new Vector3(2.2f, 1.05f, 3.05f), new Vector3(1.0f, 2.1f, 0.15f), _door, g, false);
        // труба
        Box("Chimney", new Vector3(-2f, 4.8f, 0), new Vector3(0.8f, 1.6f, 0.8f), _brick, g, false);
        // крылечко
        Box("Porch", new Vector3(2.2f, 0.15f, 4.0f), new Vector3(1.8f, 0.3f, 1.6f), _wood, g, false);

        // поленница
        if (Chance(0.6f))
        {
            Box("Woodpile", new Vector3(-4.5f, 0.5f, 2f), new Vector3(1.5f, 1.0f, 1.0f), _wood, g, false);
        }
    }

    static void BusStop(Transform parent, Vector3 pos, float rotY)
    {
        var g = Group("BusStop", parent);
        g.localPosition = pos;
        g.localRotation = Quaternion.Euler(0, rotY, 0);

        // навес
        Box("Roof", new Vector3(0, 2.6f, 0), new Vector3(4.5f, 0.15f, 1.8f), _roofDark, g, false);
        // опоры
        Cylinder("PoleL", new Vector3(-1.9f, 1.3f, 0.7f), 0.08f, 2.6f, _metal, g, false);
        Cylinder("PoleR", new Vector3(1.9f, 1.3f, 0.7f), 0.08f, 2.6f, _metal, g, false);
        // скамья
        Box("Bench", new Vector3(0, 0.45f, 0.4f), new Vector3(3.5f, 0.1f, 0.5f), _wood, g, false);
        // задняя стенка-стекло
        Box("BackGlass", new Vector3(0, 1.4f, -0.75f), new Vector3(4.4f, 2.2f, 0.08f), _glass, g, false);
        // табличка
        Box("Sign", new Vector3(2.4f, 1.9f, 0.5f), new Vector3(0.5f, 0.7f, 0.06f), _line, g, false);
        // урна
        Cylinder("Bin", new Vector3(-2.2f, 0.45f, 0.9f), 0.3f, 0.9f, _dark, g, false);
    }

    static void WoodFence(Transform parent, Vector3 pos, float w, float l)
    {
        var g = Group("WoodFence", parent);
        g.localPosition = pos;
        // жерди
        for (float d = -l / 2; d <= l / 2; d += 2.5f)
        {
            Box("Post", new Vector3(0, 0.9f, d), new Vector3(0.15f, 1.8f, 0.15f), _wood, g, false);
        }
        Box("Rail1", new Vector3(0, 1.4f, 0), new Vector3(0.08f, 0.12f, l), _wood, g, false);
        Box("Rail2", new Vector3(0, 0.6f, 0), new Vector3(0.08f, 0.12f, l), _wood, g, false);
    }

    static void Barn(Transform parent, Vector3 pos)
    {
        var g = Group("Barn", parent);
        g.localPosition = pos;

        Box("Walls", new Vector3(0, 2f, 0), new Vector3(10f, 4f, 8f), _woodDark, g);
        var r1 = Box("RoofL", new Vector3(0, 5.1f, -2.1f), new Vector3(10.6f, 0.3f, 5f), _roofDark, g, false);
        r1.transform.localRotation = Quaternion.Euler(38f, 0, 0);
        var r2 = Box("RoofR", new Vector3(0, 5.1f, 2.1f), new Vector3(10.6f, 0.3f, 5f), _roofDark, g, false);
        r2.transform.localRotation = Quaternion.Euler(-38f, 0, 0);
        // большие ворота
        Box("Gate", new Vector3(0, 1.6f, 4.05f), new Vector3(3.5f, 3.2f, 0.2f), _wood, g, false);
    }

    // ================= ТЮРЬМА (в конце, юг, центр около (465, 100)) =================

    static void Prison()
    {
        var root = Group("Prison");

        // асфальтированная площадка
        Box("Yard", new Vector3(465f, 0.04f, 100f), new Vector3(120f, 0.08f, 180f), _concrete, root, false);

        // главный корпус
        var main = Group("MainBlock", root);
        main.localPosition = new Vector3(465f, 0, 60f);

        // 3-этажный бетонный блок
        Box("Floor1", new Vector3(0, 2.2f, 0), new Vector3(70f, 4.4f, 20f), _concreteWall, main);
        Box("Floor2", new Vector3(0, 6.6f, 0), new Vector3(70f, 4.4f, 20f), _concreteWall, main);
        Box("Floor3", new Vector3(0, 11.0f, 0), new Vector3(70f, 4.4f, 20f), _concreteWall, main);
        Box("Roof", new Vector3(0, 13.4f, 0), new Vector3(71f, 0.5f, 21f), _roofDark, main);

        // решётчатые окна (тёмные с полосками) — фасад к югу (z+)
        for (int f = 0; f < 3; f++)
        {
            float y = 2.2f + f * 4.4f;
            for (int i = 0; i < 17; i++)
            {
                float x = -32f + i * 4f;
                Box("CellWin" + f + "_" + i, new Vector3(x, y, 10.05f), new Vector3(1.4f, 1.8f, 0.12f), _windows, main, false);
                // решётка
                Box("BarV1" + f + "_" + i, new Vector3(x - 0.35f, y, 10.12f), new Vector3(0.08f, 1.9f, 0.05f), _dark, main, false);
                Box("BarV2" + f + "_" + i, new Vector3(x, y, 10.12f), new Vector3(0.08f, 1.9f, 0.05f), _dark, main, false);
                Box("BarV3" + f + "_" + i, new Vector3(x + 0.35f, y, 10.12f), new Vector3(0.08f, 1.9f, 0.05f), _dark, main, false);
                Box("BarH" + f + "_" + i, new Vector3(x, y, 10.12f), new Vector3(1.0f, 0.08f, 0.05f), _dark, main, false);
            }
        }

        // вышки по углам территории
        Watchtower(root, new Vector3(412f, 0, 25f));
        Watchtower(root, new Vector3(518f, 0, 25f));
        Watchtower(root, new Vector3(412f, 0, 185f));
        Watchtower(root, new Vector3(518f, 0, 185f));

        // забор: двойной, бетонный + сетка
        float fx0 = 408f, fx1 = 522f, fz0 = 12f, fz1 = 198f;
        FenceWall(root, fx0, fz0, fx1, fz0); // север
        FenceWall(root, fx0, fz1, fx1, fz1); // юг
        FenceWallV(root, fx0, fz0, fx0, fz1); // запад
        FenceWallV(root, fx1, fz0, fx1, fz1); // восток

        // КПП у ворот
        Checkpoint(root, new Vector3(465f, 0, 195f));

        // ворота (сетчатые)
        var gate = Group("MainGate", root);
        Box("GateL", new Vector3(461f, 1.5f, 195f), new Vector3(8f, 3f, 0.3f), _metalFence, gate);
        Box("GateR", new Vector3(469f, 1.5f, 195f), new Vector3(8f, 3f, 0.3f), _metalFence, gate);

        // внутренние корпуса: столовая, блок Б
        var b2 = Group("BlockB", root);
        b2.localPosition = new Vector3(430f, 0, 100f);
        Box("B2Body", new Vector3(0, 3.5f, 0), new Vector3(16f, 7f, 12f), _concreteWall, b2);
        Box("B2Roof", new Vector3(0, 7.3f, 0), new Vector3(17f, 0.4f, 13f), _roofDark, b2);

        var b3 = Group("BlockC", root);
        b3.localPosition = new Vector3(500f, 0, 100f);
        Box("B3Body", new Vector3(0, 3.5f, 0), new Vector3(16f, 7f, 12f), _concreteWall, b3);
        Box("B3Roof", new Vector3(0, 7.3f, 0), new Vector3(17f, 0.4f, 13f), _roofDark, b3);

        // прогулочный двор с сеткой
        var yard = Group("RecYard", root);
        yard.localPosition = new Vector3(465f, 0, 150f);
        Box("RecFloor", new Vector3(0, 0.06f, 0), new Vector3(40f, 0.1f, 30f), _concrete, yard, false);
        // сетка-периметр двора
        MeshFence(yard, -20f, -15f, 20f, -15f);
        MeshFence(yard, -20f, 15f, 20f, 15f);
        MeshFenceV(yard, -20f, -15f, -20f, 15f);
        MeshFenceV(yard, 20f, -15f, 20f, 15f);
        // баскетбольное кольцо
        Cylinder("Hooppole", new Vector3(0, 1.75f, -13.5f), 0.1f, 3.5f, _metal, yard);
        Box("Backboard", new Vector3(0, 3.5f, -13.8f), new Vector3(1.8f, 1.2f, 0.1f), _white, yard, false);
        Cylinder("Hoop", new Vector3(0, 3.0f, -13.3f), 0.3f, 0.1f, _red, yard, false);

        // прожекторы на вышках уже в Watchtower; добавим направленные на стенах
        Floodlight(root, new Vector3(465f, 12f, 14f), 180f);
        Floodlight(root, new Vector3(430f, 12f, 14f), 180f);
        Floodlight(root, new Vector3(500f, 12f, 14f), 180f);

        // патрульная машина у КПП
        PoliceCar(root, new Vector3(455f, 0, 203f), 90f);

        Debug.Log("[MapBuilder] Тюрьма построена");
    }

    static void Watchtower(Transform root, Vector3 pos)
    {
        var g = Group("Watchtower", root);
        g.localPosition = pos;

        // 4 опоры
        Box("Leg1", new Vector3(-1.5f, 5f, -1.5f), new Vector3(0.4f, 10f, 0.4f), _woodDark, g);
        Box("Leg2", new Vector3(1.5f, 5f, -1.5f), new Vector3(0.4f, 10f, 0.4f), _woodDark, g);
        Box("Leg3", new Vector3(-1.5f, 5f, 1.5f), new Vector3(0.4f, 10f, 0.4f), _woodDark, g);
        Box("Leg4", new Vector3(1.5f, 5f, 1.5f), new Vector3(0.4f, 10f, 0.4f), _woodDark, g);
        // распорки
        Box("Brace1", new Vector3(0, 3f, 0), new Vector3(3.4f, 0.25f, 0.25f), _woodDark, g, false);
        Box("Brace2", new Vector3(0, 3f, 0), new Vector3(0.25f, 0.25f, 3.4f), _woodDark, g, false);
        // будка
        Box("Cab", new Vector3(0, 10.8f, 0), new Vector3(4.4f, 3f, 4.4f), _wood, g);
        // окна будки
        Box("CabWin1", new Vector3(0, 10.8f, 2.25f), new Vector3(3.6f, 1.6f, 0.1f), _glass, g, false);
        Box("CabWin2", new Vector3(2.25f, 10.8f, 0), new Vector3(0.1f, 1.6f, 3.6f), _glass, g, false);
        Box("CabWin3", new Vector3(-2.25f, 10.8f, 0), new Vector3(0.1f, 1.6f, 3.6f), _glass, g, false);
        // конусная крыша
        Cylinder("TowerRoof", new Vector3(0, 12.8f, 0), 3.0f, 1.2f, _roofDark, g, false);
        // лестница
        for (int i = 0; i < 9; i++)
        {
            Box("Ladder" + i, new Vector3(2.2f, 0.6f + i * 1.1f, 0), new Vector3(0.7f, 0.08f, 0.15f), _metal, g, false);
        }
        // прожектор
        Box("Spotlight", new Vector3(0, 12.2f, 2.3f), new Vector3(0.8f, 0.5f, 0.5f), _light, g, false);
        var pl = g.gameObject.AddComponent<Light>();
        pl.type = LightType.Spot;
        pl.range = 60f;
        pl.spotAngle = 70f;
        pl.intensity = 2.2f;
        pl.color = new Color(1f, 0.95f, 0.85f);
        pl.transform.localPosition = new Vector3(0, 11.5f, 1.5f);
        pl.transform.localRotation = Quaternion.Euler(50f, 0, 0);
        pl.shadows = LightShadows.None;
    }

    // забор-стена (вдоль X)
    static void FenceWall(Transform root, float x0, float z, float x1, float z1)
    {
        float len = x1 - x0;
        float cx = (x0 + x1) * 0.5f;
        var g = Group("FenceWall", root);
        // бетонные панели 3м высотой
        Box("Wall", new Vector3(cx, 1.5f, z), new Vector3(len, 3f, 0.4f), _concreteWall, g);
        // сетка сверху
        Box("Mesh", new Vector3(cx, 4.0f, z), new Vector3(len, 2.0f, 0.08f), _metalFence, g, false);
        // столбы
        for (float x = x0; x <= x1; x += 10f)
        {
            Box("Post", new Vector3(x, 2.5f, z), new Vector3(0.35f, 5f, 0.35f), _metal, g);
        }
        // колючка (маленькие тёмные спирали)
        for (float x = x0 + 2f; x < x1; x += 4f)
        {
            Sphere("Barb", new Vector3(x, 5.1f, z), 0.18f, _metal, g);
        }
    }

    // забор-стена (вдоль Z)
    static void FenceWallV(Transform root, float x, float z0, float x1, float z1)
    {
        float len = z1 - z0;
        float cz = (z0 + z1) * 0.5f;
        var g = Group("FenceWallV", root);
        Box("Wall", new Vector3(x, 1.5f, cz), new Vector3(0.4f, 3f, len), _concreteWall, g);
        Box("Mesh", new Vector3(x, 4.0f, cz), new Vector3(0.08f, 2.0f, len), _metalFence, g, false);
        for (float z = z0; z <= z1; z += 10f)
        {
            Box("Post", new Vector3(x, 2.5f, z), new Vector3(0.35f, 5f, 0.35f), _metal, g);
        }
        for (float z = z0 + 2f; z < z1; z += 4f)
        {
            Sphere("Barb", new Vector3(x, 5.1f, z), 0.18f, _metal, g);
        }
    }

    // сетчатый забор (внутренний, тонкий) вдоль X
    static void MeshFence(Transform root, float x0, float z, float x1, float z1)
    {
        float len = x1 - x0;
        float cx = (x0 + x1) * 0.5f;
        var g = Group("MeshFence", root);
        Box("Mesh", new Vector3(cx, 2.0f, z), new Vector3(len, 4f, 0.08f), _metalFence, g, false);
        for (float x = x0; x <= x1; x += 5f)
            Box("Post", new Vector3(x, 2.0f, z), new Vector3(0.15f, 4f, 0.15f), _metal, g, false);
    }

    static void MeshFenceV(Transform root, float x, float z0, float x1, float z1)
    {
        float len = z1 - z0;
        float cz = (z0 + z1) * 0.5f;
        var g = Group("MeshFenceV", root);
        Box("Mesh", new Vector3(x, 2.0f, cz), new Vector3(0.08f, 4f, len), _metalFence, g, false);
        for (float z = z0; z <= z1; z += 5f)
            Box("Post", new Vector3(x, 2.0f, z), new Vector3(0.15f, 4f, 0.15f), _metal, g, false);
    }

    static void Checkpoint(Transform root, Vector3 pos)
    {
        var g = Group("Checkpoint", root);
        g.localPosition = pos;
        // будка
        Box("Booth", new Vector3(-8f, 1.5f, 0), new Vector3(4f, 3f, 4f), _concreteWall, g);
        Box("BoothWin", new Vector3(-8f, 1.7f, 2.05f), new Vector3(2.5f, 1.2f, 0.1f), _glass, g, false);
        Box("BoothRoof", new Vector3(-8f, 3.3f, 0), new Vector3(4.6f, 0.3f, 4.6f), _roofDark, g);
        // шлагбаум
        Cylinder("Barmotor", new Vector3(-5.5f, 0.6f, 1.5f), 0.25f, 1.2f, _metal, g);
        var bar = Box("Barrier", new Vector3(-3.0f, 0.95f, 1.5f), new Vector3(5f, 0.15f, 0.15f), _red, g, false);
        bar.transform.localRotation = Quaternion.Euler(0, 0, 25f);
    }

    static void Floodlight(Transform root, Vector3 pos, float rotY)
    {
        var g = Group("Floodlight", root);
        g.localPosition = pos;
        g.localRotation = Quaternion.Euler(0, rotY, 0);
        Box("Head", new Vector3(0, 0, 0), new Vector3(1.0f, 0.8f, 0.5f), _light, g, false);
        var l = g.gameObject.AddComponent<Light>();
        l.type = LightType.Spot;
        l.range = 50f;
        l.spotAngle = 60f;
        l.intensity = 2.0f;
        l.color = new Color(1f, 0.95f, 0.85f);
        l.transform.localRotation = Quaternion.Euler(35f, 0, 0);
        l.shadows = LightShadows.None;
    }
}
