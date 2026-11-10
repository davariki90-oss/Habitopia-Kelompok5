// Habitopia - prototipe ruang 3D tambahan untuk UTS.
// Letakkan file ini di Assets/_Game/Scripts/HabitopiaRoom3D.cs.
// Di Scene Room3DScene: buat Empty GameObject 'Room3DBuilder' lalu Add Component HabitopiaRoom3D.
// Semua objek geometri 3D dibangun saat Play untuk percobaan awal.
using UnityEngine;
using UnityEngine.SceneManagement;

public class HabitopiaRoom3D : MonoBehaviour
{
    private Camera roomCamera;
    private Renderer backWallRenderer;
    private Renderer leftWallRenderer;
    private Renderer wallTrimRenderer;
    private Transform pet;
    private float orbitAngle = 0f;
    private float petReactionUntil = 0f;
    private int petTapCount = 0;
    private string message = "Tap Mochi! Explore the 3D room.";
    private readonly Vector3 petBase = new Vector3(0f, 0.78f, -0.35f);
    private readonly Color mint = new Color(0.42f, 0.79f, 0.64f);
    private readonly Color cream = new Color(0.96f, 0.88f, 0.74f);
    private Material greenMat;
    private Material blushMat;
    private Material inkMat;

    private void Start()
    {
        // Ini adalah scene 3D terpisah; jangan meletakkan script ini pada HomeScene 2D.
        roomCamera = Camera.main;
        if (roomCamera == null)
        {
            GameObject c = new GameObject("Main Camera");
            c.tag = "MainCamera";
            roomCamera = c.AddComponent<Camera>();
            c.AddComponent<AudioListener>();
        }
        roomCamera.orthographic = false;
        roomCamera.fieldOfView = 57f;
        roomCamera.clearFlags = CameraClearFlags.SolidColor;
        roomCamera.backgroundColor = new Color(0.79f, 0.90f, 0.83f);
        CameraOrbit();

        greenMat = Mat("Mochi Mint", mint);
        blushMat = Mat("Blush", new Color(0.98f, 0.60f, 0.62f));
        inkMat = Mat("Ink", new Color(0.13f, 0.30f, 0.26f));

        Box("Floor_3D", new Vector3(0f, -0.20f, 0f), new Vector3(8f, 0.4f, 6f), new Color(0.73f, 0.58f, 0.43f));
        backWallRenderer = Box("BackWall_3D", new Vector3(0f, 2f, 3f), new Vector3(8f, 4.4f, 0.24f), cream).GetComponent<Renderer>();
        leftWallRenderer = Box("LeftWall_3D", new Vector3(-4f, 2f, 0f), new Vector3(0.24f, 4.4f, 6f), new Color(0.88f, 0.78f, 0.67f)).GetComponent<Renderer>();
        wallTrimRenderer = Box("WallTrim", new Vector3(0f, 0.20f, 2.81f), new Vector3(7.8f, 0.20f, 0.12f), new Color(0.70f, 0.54f, 0.39f)).GetComponent<Renderer>();
        Box("Carpet_3D", new Vector3(0f, 0.035f, -0.65f), new Vector3(2.3f, 0.08f, 1.7f), new Color(0.92f, 0.59f, 0.60f));
        Box("Desk_3D", new Vector3(2.35f, 0.72f, 1.75f), new Vector3(1.55f, 0.18f, 0.65f), new Color(0.56f, 0.39f, 0.28f));
        Box("DeskLegA", new Vector3(1.8f, 0.36f, 1.75f), new Vector3(0.14f, 0.72f, 0.14f), new Color(0.56f, 0.39f, 0.28f));
        Box("DeskLegB", new Vector3(2.9f, 0.36f, 1.75f), new Vector3(0.14f, 0.72f, 0.14f), new Color(0.56f, 0.39f, 0.28f));
        Box("Bed_3D", new Vector3(-2.45f, 0.35f, 1.25f), new Vector3(1.75f, 0.7f, 2.0f), new Color(0.88f, 0.63f, 0.54f));
        Box("BedSheet", new Vector3(-2.45f, 0.75f, 1.25f), new Vector3(1.62f, 0.12f, 1.75f), new Color(0.87f, 0.92f, 0.77f));
        Box("Pillow", new Vector3(-2.45f, 0.84f, 1.90f), new Vector3(1.0f, 0.16f, 0.46f), new Color(0.99f, 0.96f, 0.91f));
        Cylinder("Pot", new Vector3(2.50f, 1.02f, 1.75f), new Vector3(0.32f, 0.25f, 0.32f), new Color(0.79f, 0.48f, 0.35f));
        Sphere("Plant_Leaf1", new Vector3(2.3f, 1.40f, 1.70f), new Vector3(0.39f, 0.55f, 0.30f), new Color(0.36f, 0.64f, 0.42f));
        Sphere("Plant_Leaf2", new Vector3(2.72f, 1.49f, 1.79f), new Vector3(0.32f, 0.56f, 0.31f), new Color(0.42f, 0.71f, 0.50f));

        // Mochi 3D memakai mesh Sphere + Collider, bukan sprite 2D.
        GameObject petRoot = new GameObject("Mochi_3D_Pet");
        petRoot.AddComponent<Habitopia3DPetMarker>();
        petRoot.transform.position = petBase;
        pet = petRoot.transform;
        ChildSphere("Body_3D", pet, Vector3.zero, new Vector3(1.26f, 1.38f, 1.14f), greenMat);
        ChildSphere("LeftEye", pet, new Vector3(-0.25f, 0.13f, -0.48f), Vector3.one * 0.12f, inkMat);
        ChildSphere("RightEye", pet, new Vector3(0.25f, 0.13f, -0.48f), Vector3.one * 0.12f, inkMat);
        ChildSphere("LeftCheek", pet, new Vector3(-0.39f, -0.08f, -0.43f), Vector3.one * 0.15f, blushMat);
        ChildSphere("RightCheek", pet, new Vector3(0.39f, -0.08f, -0.43f), Vector3.one * 0.15f, blushMat);
        ChildSphere("Sprout", pet, new Vector3(0f, 0.72f, 0f), new Vector3(0.25f, 0.28f, 0.25f), Mat("Sprout", new Color(0.28f, 0.66f, 0.35f)));

        GameObject lightObj = new GameObject("Directional Light 3D");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.6f;
        light.color = new Color(1f, 0.96f, 0.87f);
        lightObj.transform.rotation = Quaternion.Euler(48f, -25f, 0f);
        RenderSettings.ambientLight = new Color(0.74f, 0.76f, 0.70f);
        CameraOrbit(); // Refresh wall visibility after creating all meshes.
        // Kontrol dibuat melalui OnGUI agar teks tombol selalu tergambar pada layar portrait.
    }

    private Material Mat(string name, Color c)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material m = new Material(shader);
        m.name = name;
        m.color = c;
        return m;
    }

    private GameObject Primitive(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = material;
        return go;
    }

    private GameObject Box(string name, Vector3 pos, Vector3 scale, Color color) =>
        Primitive(PrimitiveType.Cube, name, pos, scale, Mat(name + " Mat", color));

    private GameObject Sphere(string name, Vector3 pos, Vector3 scale, Color color) =>
        Primitive(PrimitiveType.Sphere, name, pos, scale, Mat(name + " Mat", color));

    private GameObject Cylinder(string name, Vector3 pos, Vector3 scale, Color color) =>
        Primitive(PrimitiveType.Cylinder, name, pos, scale, Mat(name + " Mat", color));

    private void ChildSphere(string name, Transform parent, Vector3 offset, Vector3 scale, Material material)
    {
        GameObject go = Primitive(PrimitiveType.Sphere, name, parent.position + offset, scale, material);
        go.transform.SetParent(parent, true);
    }

    private void CameraOrbit()
    {
        if (roomCamera == null) return;
        orbitAngle = Mathf.Repeat(orbitAngle, 360f);
        float radians = orbitAngle * Mathf.Deg2Rad;
        Vector3 camPos = new Vector3(8.5f * Mathf.Sin(radians), 5.7f, -10.5f * Mathf.Cos(radians));
        roomCamera.transform.position = camPos;
        roomCamera.transform.LookAt(new Vector3(0f, 1.0f, 0.6f));
        // For a cut-away diorama, hide a wall if the orbit camera is behind it.
        // Meshes are not deleted; they become visible again when camera returns.
        if (backWallRenderer != null) backWallRenderer.enabled = camPos.z <= 3.0f;
        if (wallTrimRenderer != null) wallTrimRenderer.enabled = camPos.z <= 3.0f;
        if (leftWallRenderer != null) leftWallRenderer.enabled = camPos.x >= -4.0f;
    }

    private void Update()
    {
        if (pet != null)
        {
            float bounce = Time.time < petReactionUntil ? Mathf.Abs(Mathf.Sin(Time.time * 13f)) * 0.22f : Mathf.Sin(Time.time * 2.2f) * 0.045f;
            pet.position = petBase + Vector3.up * bounce;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) { orbitAngle = Mathf.Repeat(orbitAngle - 15f, 360f); CameraOrbit(); }
        if (Input.GetKeyDown(KeyCode.RightArrow)) { orbitAngle = Mathf.Repeat(orbitAngle + 15f, 360f); CameraOrbit(); }
        if (Input.GetKeyDown(KeyCode.Escape)) BackToHome();

        bool click = Input.GetMouseButtonDown(0);
        Vector2 clickPos = Input.mousePosition;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            click = true;
            clickPos = Input.GetTouch(0).position;
        }
        if (click && roomCamera != null && clickPos.y > Screen.height * 0.18f && clickPos.y < Screen.height * 0.85f)
        {
            RaycastHit hit;
            Ray ray = roomCamera.ScreenPointToRay(clickPos);
            if (Physics.Raycast(ray, out hit, 100f) && hit.collider.GetComponentInParent<Habitopia3DPetMarker>() != null)
            {
                petTapCount++;
                petReactionUntil = Time.time + 1.5f;
                message = "Mochi senang! Disentuh: " + petTapCount;
                RefreshMessage();
            }
        }
    }

    private void BackToHome()
    {
        if (Application.CanStreamedLevelBeLoaded("HomeScene")) SceneManager.LoadScene("HomeScene");
        else
        {
            message = "HomeScene belum masuk Build Profiles > Scene List.";
            RefreshMessage();
        }
    }

    private void RefreshMessage()
    {
        // OnGUI membaca message pada setiap frame, tidak perlu mengubah komponen teks.
    }

    // UI darurat yang responsif: IMGUI ditarik langsung setiap frame.
    // Ini sengaja tidak bergantung pada ukuran RectTransform, Text, atau Canvas Scaler.
    // Semua koordinat dihitung dari pixel Game View sehingga tidak bertumpuk di 9:16.
    private GUIStyle CreateTextStyle(int fontSize, TextAnchor alignment, bool wrap = false)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = alignment;
        style.wordWrap = wrap;
        style.clipping = TextClipping.Clip;
        style.normal.textColor = new Color(0.13f, 0.29f, 0.25f, 1f);
        style.padding = new RectOffset(1, 1, 1, 1);
        return style;
    }

    private void DrawFlatPanel(Rect rectangle, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rectangle, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private void DrawControl(Rect rectangle, string label, System.Action handler, GUIStyle textStyle)
    {
        DrawFlatPanel(rectangle, new Color(0.97f, 0.85f, 0.59f, 1f));
        // GUI.Button mengurus input. Label tergambar terpisah untuk menghindari
        // masalah teks kosong/terpotong pada tombol versi uGUI sebelumnya.
        GUIStyle hitAreaStyle = GUIStyle.none;
        bool clicked = GUI.Button(rectangle, GUIContent.none, hitAreaStyle);
        GUI.Label(rectangle, label, textStyle);
        if (clicked) handler();
    }

    private void OnGUI()
    {
        if (!isActiveAndEnabled) return;
        float w = Screen.width;
        float h = Screen.height;
        if (w < 100f || h < 160f) return;

        Rect safe = Screen.safeArea;
        float safeLeft = Mathf.Max(0f, safe.xMin);
        float safeRight = Mathf.Max(0f, w - safe.xMax);
        float safeTop = Mathf.Max(0f, h - safe.yMax); // IMGUI mulai dari atas
        float safeBottom = Mathf.Max(0f, safe.yMin);
        float usableWidth = Mathf.Max(120f, w - safeLeft - safeRight);

        float headerHeight = Mathf.Clamp(h * 0.16f, 96f, 155f);
        float footerHeight = Mathf.Clamp(h * 0.11f, 73f, 124f);
        Color panelColor = new Color(0.79f, 0.91f, 0.84f, 1f);
        DrawFlatPanel(new Rect(0f, 0f, w, safeTop + headerHeight), panelColor);
        DrawFlatPanel(new Rect(0f, h - safeBottom - footerHeight, w, safeBottom + footerHeight), panelColor);

        int titleSize = Mathf.Clamp(Mathf.RoundToInt(usableWidth * 0.052f), 17, 30);
        int messageSize = Mathf.Clamp(Mathf.RoundToInt(usableWidth * 0.038f), 13, 24);
        GUIStyle titleStyle = CreateTextStyle(titleSize, TextAnchor.MiddleCenter);
        GUIStyle messageStyle = CreateTextStyle(messageSize, TextAnchor.MiddleCenter, true);
        GUI.Label(new Rect(safeLeft + 8f, safeTop + 8f, usableWidth - 16f, 37f),
            "HABITOPIA  |  3D ROOM", titleStyle);
        GUI.Label(new Rect(safeLeft + 10f, safeTop + 49f, usableWidth - 20f, headerHeight - 52f),
            message, messageStyle);

        float horizontalPad = Mathf.Clamp(usableWidth * 0.035f, 8f, 22f);
        float gap = Mathf.Clamp(usableWidth * 0.020f, 6f, 16f);
        float buttonWidth = (usableWidth - horizontalPad * 2f - gap * 2f) / 3f;
        float buttonHeight = Mathf.Clamp(footerHeight * 0.62f, 47f, 72f);
        float y = h - safeBottom - footerHeight + (footerHeight - buttonHeight) / 2f;
        int buttonFontSize = Mathf.Clamp(Mathf.RoundToInt(buttonWidth * 0.145f), 12, 22);
        GUIStyle buttonTextStyle = CreateTextStyle(buttonFontSize, TextAnchor.MiddleCenter);
        DrawControl(new Rect(safeLeft + horizontalPad, y, buttonWidth, buttonHeight), "LEFT",
            () => { orbitAngle -= 30f; CameraOrbit(); }, buttonTextStyle);
        DrawControl(new Rect(safeLeft + horizontalPad + buttonWidth + gap, y, buttonWidth, buttonHeight), "BACK 2D",
            BackToHome, buttonTextStyle);
        DrawControl(new Rect(safeLeft + horizontalPad + (buttonWidth + gap) * 2f, y, buttonWidth, buttonHeight), "RIGHT",
            () => { orbitAngle += 30f; CameraOrbit(); }, buttonTextStyle);
    }

}

// Marker used to recognize 3D pet collider on Raycast.
public class Habitopia3DPetMarker : MonoBehaviour { }
