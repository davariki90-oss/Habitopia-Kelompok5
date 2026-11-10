// HABITOPIA - DEMO DARURAT UNITY 6 | LARGE-TEXT / PRESENTATION UI v3
// Tempel ke Assets/_Game/Scripts/HabitopiaDemo.cs
// Tambahkan ke GameObject kosong bernama HabitopiaApp pada HomeScene.
// Unity UI (uGUI) diperlukan. Tidak memerlukan TextMeshPro atau aset eksternal.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class HabitopiaHabit
{
    public string id;
    public string name;
    public string completedDate;
    public string rewardedDate;
}

[Serializable]
public class HabitopiaDay
{
    public string date;
    public int completed;
    public int total;
    public int rewardsClaimed;
}

[Serializable]
public class HabitopiaSave
{
    public int coins = 20;
    public int xp = 0;
    public int petTaps = 0;
    public List<HabitopiaHabit> habits = new List<HabitopiaHabit>();
    public List<HabitopiaDay> history = new List<HabitopiaDay>();
    public List<int> owned = new List<int>();
    public int[] slots = new int[] { 0, -1, 3, -1, -1, -1 };
}

public class HabitopiaDemo : MonoBehaviour
{
    private const int MaxHabits = 5;
    private readonly string[] furnitureNames = { "Poster", "Window", "Clock", "Plant", "Rug", "Lamp", "Bookcase", "Cushion" };
    private readonly int[] prices = { 0, 30, 30, 0, 25, 35, 45, 25 };
    private readonly bool[] isWallItem = { true, true, true, false, false, false, false, false };
    private readonly Vector2[] slotPositions = {
        new Vector2(-260, 240), new Vector2(260, 240),
        new Vector2(-320, -225), new Vector2(-110, -270),
        new Vector2(115, -260), new Vector2(325, -210)
    };

    private readonly Color cream = Hex("#F8F2E5");
    private readonly Color ink = Hex("#314C43");
    private readonly Color forest = Hex("#43866E");
    private readonly Color mint = Hex("#C6E8D5");
    private readonly Color pale = Hex("#E6D8B9");
    private readonly Color gold = Hex("#F7D886");

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform content;
    private RectTransform roomRect;
    private Font uiFont;
    private HabitopiaSave save;
    private string saveFile;
    private Text statusText;
    private Text coinsText;
    private Text levelText;
    private Text petDialogue;
    private Image petImage;
    private InputField newHabitInput;
    private int selectedItem = -1;
    private bool resetPending = false;
    private int page = 0; // 0 Home, 1 Habits, 2 Shop, 3 Stats
    private string message = "Welcome to Habitopia!";
    private Sprite whiteSprite;
    private Sprite[] petSprites;
    private Sprite[] furnitureSprites;

    private static Color Hex(string hex)
    {
        Color color;
        return ColorUtility.TryParseHtmlString(hex, out color) ? color : Color.white;
    }

    private void Awake()
    {
        saveFile = Path.Combine(Application.persistentDataPath, "habitopia_save.json");
        LoadData();
        BuildSprites();
        MakeFont();
        EnsureEventSystem();
        BuildCanvas();
        ShowPage(0);
    }

    private string Today() { return DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }

    private void LoadData()
    {
        try
        {
            if (File.Exists(saveFile)) save = JsonUtility.FromJson<HabitopiaSave>(File.ReadAllText(saveFile));
        }
        catch (Exception ex) { Debug.LogWarning("Cannot read save: " + ex.Message); }
        if (save == null) save = new HabitopiaSave();
        if (save.habits == null) save.habits = new List<HabitopiaHabit>();
        if (save.history == null) save.history = new List<HabitopiaDay>();
        if (save.owned == null) save.owned = new List<int>();
        if (save.slots == null || save.slots.Length != 6) save.slots = new[] { 0, -1, 3, -1, -1, -1 };
        if (save.habits.Count == 0 && !File.Exists(saveFile))
        {
            save.habits.Add(NewHabit("Drink Water"));
            save.habits.Add(NewHabit("Study 30 Minutes"));
            save.habits.Add(NewHabit("Exercise"));
            save.owned.Add(0);
            save.owned.Add(3);
            SaveData();
        }
    }

    private HabitopiaHabit NewHabit(string name)
    {
        return new HabitopiaHabit { id = Guid.NewGuid().ToString("N"), name = name, completedDate = "", rewardedDate = "" };
    }

    private void SaveData()
    {
        try { File.WriteAllText(saveFile, JsonUtility.ToJson(save, true)); }
        catch (Exception ex) { Debug.LogError("Cannot save Habitopia: " + ex.Message); }
    }

    private void ResetDemo()
    {
        if (!resetPending)
        {
            resetPending = true;
            message = "Reset all demo data? Press RESET DEMO once more.";
            ShowPage(3);
            return;
        }
        resetPending = false;
        save = new HabitopiaSave();
        save.habits.Add(NewHabit("Drink Water"));
        save.habits.Add(NewHabit("Study 30 Minutes"));
        save.habits.Add(NewHabit("Exercise"));
        save.owned.Add(0);
        save.owned.Add(3);
        selectedItem = -1;
        SaveData();
        message = "Demo reset. Ready for presentation!";
        ShowPage(0);
    }

    private HabitopiaDay GetTodayRecord()
    {
        string day = Today();
        HabitopiaDay record = save.history.Find(x => x.date == day);
        if (record == null)
        {
            record = new HabitopiaDay { date = day, total = save.habits.Count };
            save.history.Add(record);
        }
        return record;
    }

    private int CompletedCount()
    {
        int result = 0;
        foreach (HabitopiaHabit h in save.habits) if (h.completedDate == Today()) result++;
        return result;
    }

    private void UpdateRecord()
    {
        HabitopiaDay record = GetTodayRecord();
        record.completed = CompletedCount();
        record.total = save.habits.Count;
        SaveData();
    }

    private void ToggleHabit(string id)
    {
        HabitopiaHabit habit = save.habits.Find(x => x.id == id);
        if (habit == null) return;
        bool isDone = habit.completedDate == Today();
        habit.completedDate = isDone ? "" : Today();
        if (!isDone && habit.rewardedDate != Today())
        {
            HabitopiaDay record = GetTodayRecord();
            if (record.rewardsClaimed < 5)
            {
                save.xp += 10;
                save.coins += 10;
                record.rewardsClaimed++;
                habit.rewardedDate = Today();
                message = "+10 XP  +10 COINS!";
            }
            else message = "Habit completed! Daily reward limit reached.";
        }
        else message = isDone ? "Checklist updated." : "Completed again. Reward already claimed.";
        UpdateRecord();
        ShowPage(1);
    }

    private void AddHabit()
    {
        if (newHabitInput == null) return;
        string name = newHabitInput.text.Trim();
        if (name.Length < 2 || name.Length > 32) { message = "Use a habit name with 2-32 characters."; ShowPage(1); return; }
        if (save.habits.Count >= MaxHabits) { message = "Demo limit: maximum 5 habits."; ShowPage(1); return; }
        save.habits.Add(NewHabit(name));
        message = "New habit added.";
        UpdateRecord();
        ShowPage(1);
    }

    private void DeleteHabit(string id)
    {
        save.habits.RemoveAll(x => x.id == id);
        message = "Habit deleted.";
        UpdateRecord();
        ShowPage(1);
    }

    private int Level() { return save.xp / 100 + 1; }

    private int Streak()
    {
        // Demo: >=70% daily completion, starting today or yesterday.
        int streak = 0;
        DateTime d = DateTime.Today;
        for (int offset = 0; offset < 365; offset++)
        {
            string date = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            HabitopiaDay r = save.history.Find(x => x.date == date);
            bool complete = r != null && r.total > 0 && r.completed * 10 >= r.total * 7;
            if (complete) streak++;
            else if (offset != 0) break;
            d = d.AddDays(-1);
        }
        return streak;
    }

    private void MakeFont()
    {
        try { uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (uiFont == null) { try { uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 18);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        // StandaloneInputModule needs Active Input Handling: Both or Old.
    }

    private void BuildCanvas()
    {
        GameObject c = new GameObject("HabitopiaCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = c.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scale = c.GetComponent<CanvasScaler>();
        scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1080, 1920);
        scale.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scale.matchWidthOrHeight = 0.5f;
        canvasRect = c.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
    }

    private RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    private RectTransform Panel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform r = Rect(parent, name, pos, size);
        Image i = r.gameObject.AddComponent<Image>();
        i.sprite = whiteSprite;
        i.type = Image.Type.Simple;
        i.color = color;
        i.raycastTarget = false;
        return r;
    }

    private Text Label(Transform parent, string name, string contentText, Vector2 pos, Vector2 size, int fontSize, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        RectTransform r = Rect(parent, name, pos, size);
        Text t = r.gameObject.AddComponent<Text>();
        t.font = uiFont;
        // Readability-first: Unity's 1080-wide reference canvas is downscaled in a
        // narrow 9:16 Game preview. Keep legible minimum sizes and shorten labels.
        // Best Fit is retained only to prevent longer user-provided habit names from clipping.
        int desired = Mathf.Clamp(Mathf.RoundToInt(fontSize * 1.90f), 54, 105);
        int minimum = fontSize >= 48 ? 62 : 44;
        if (size.y < 58) // Compact item captions and labels
        {
            desired = Mathf.Min(desired, 65);
            minimum = 40;
        }
        t.fontSize = desired;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = minimum;
        t.resizeTextMaxSize = desired;
        t.text = contentText;
        t.color = color;
        t.alignment = anchor;
        t.fontStyle = FontStyle.Bold;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.raycastTarget = false;
        return t;
    }

    private Button Action(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color color, System.Action callback, int fontSize = 35)
    {
        RectTransform r = Rect(parent, name, pos, size);
        Image bg = r.gameObject.AddComponent<Image>();
        bg.sprite = whiteSprite;
        bg.color = color;
        Button b = r.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = Hex("#EAF9F0");
        cb.pressedColor = Hex("#B5D9C5");
        cb.selectedColor = Color.white;
        b.colors = cb;
        if (callback != null) b.onClick.AddListener(() => callback());
        Label(r, "Text", text, Vector2.zero, size - new Vector2(8, 8), fontSize, ink);
        return b;
    }

    private void ShowPage(int nextPage)
    {
        page = nextPage;
        if (content != null) Destroy(content.gameObject);
        content = Panel(canvasRect, "Screen", Vector2.zero, new Vector2(1080, 1920), cream);
        Panel(content, "Header", new Vector2(0, 864), new Vector2(1080, 192), mint);
        Label(content, "Logo", "HABITOPIA", new Vector2(-285, 878), new Vector2(470, 100), 57, ink, TextAnchor.MiddleLeft);
        levelText = Label(content, "Level", "LV " + Level(), new Vector2(170, 881), new Vector2(170, 80), 29, ink);
        coinsText = Label(content, "Coins", save.coins + " COINS", new Vector2(410, 881), new Vector2(225, 80), 27, ink);
        statusText = Label(content, "Status", message, new Vector2(0, 710), new Vector2(990, 110), 31, ink);
        switch (page)
        {
            case 0: BuildHome(); break;
            case 1: BuildHabits(); break;
            case 2: BuildShop(); break;
            case 3: BuildStats(); break;
        }
        BuildNavigation();
    }

    private void BuildNavigation()
    {
        Panel(content, "BottomBar", new Vector2(0, -856), new Vector2(1080, 204), mint);
        string[] names = { "ROOM", "HABITS", "SHOP", "STATS" };
        for (int i = 0; i < 4; i++)
        {
            int captured = i;
            Action(content, "Nav_" + names[i], names[i], new Vector2(-393 + i * 262, -856),
                new Vector2(232, 108), i == page ? gold : cream, () => { resetPending = false; message = ""; ShowPage(captured); }, 31);
        }
    }

    private void BuildHome()
    {
        Label(content, "RoomTitle", "MY LITTLE ROOM", new Vector2(0, 590), new Vector2(800, 82), 50, ink);
        roomRect = Panel(content, "Room", new Vector2(0, 31), new Vector2(960, 990), Hex("#EAD6B5"));
        Panel(roomRect, "Wall", new Vector2(0, 165), new Vector2(930, 635), Hex("#F6E7CF"));
        Panel(roomRect, "Floor", new Vector2(0, -310), new Vector2(930, 300), Hex("#CFAF8A"));
        Panel(roomRect, "FloorEdge", new Vector2(0, -151), new Vector2(930, 15), Hex("#B48F68"));
        // A simple window illustration in the center above the pet.
        Panel(roomRect, "WindowFrame", new Vector2(0, 285), new Vector2(250, 190), Hex("#BEA47E"));
        Panel(roomRect, "Sky", new Vector2(0, 285), new Vector2(221, 162), Hex("#B8DCE4"));
        Panel(roomRect, "WindowVertical", new Vector2(0, 285), new Vector2(10, 162), Hex("#F4E4C8"));
        Panel(roomRect, "WindowHorizontal", new Vector2(0, 285), new Vector2(221, 10), Hex("#F4E4C8"));
        // Decorative shadow text removed: tiny unreadable marks on mobile preview.
        RectTransform p = Rect(roomRect, "Pet", new Vector2(0, -35), new Vector2(245, 245));
        petImage = p.gameObject.AddComponent<Image>();
        petImage.sprite = petSprites[CompletedCount() == 0 ? 1 : (CompletedCount() >= save.habits.Count && save.habits.Count > 0 ? 2 : 0)];
        petImage.preserveAspect = true;
        p.gameObject.AddComponent<Button>().onClick.AddListener(() => {
            save.petTaps++; SaveData(); message = "MOCHI IS HAPPY!  TAPS: " + save.petTaps; ShowPage(0);
        });
        Label(roomRect, "PetName", "MOCHI", new Vector2(0, 135), new Vector2(360, 68), 32, ink);
        for (int slot = 0; slot < 6; slot++) CreateSlot(slot);
        string mode = selectedItem >= 0 ? "TAP SLOT: " + furnitureNames[selectedItem].ToUpperInvariant() : "DRAG ITEMS TO MOVE";
        Label(content, "Instruction", mode, new Vector2(0, -519), new Vector2(1010, 92), 30, ink);
        if (selectedItem >= 0)
        {
            Action(content, "CancelPlacement", "CANCEL PLACEMENT", new Vector2(0, -615), new Vector2(485, 84), gold, () => { selectedItem = -1; ShowPage(0); }, 27);
        }
        else
        {
            int n = CompletedCount();
            Label(content, "HomeProgress", "HABITS " + n + "/" + save.habits.Count + "     XP " + save.xp,
                new Vector2(0, -620), new Vector2(920, 95), 35, ink);
        }
        // Navigasi ke scene 3D, gunakan hanya jika Room3DScene sudah ada di Build Profiles.
        Action(content, "Open3DRoom", "VIEW 3D ROOM", new Vector2(0, -710),
            new Vector2(435, 70), mint, () => UnityEngine.SceneManagement.SceneManager.LoadScene("Room3DScene"), 27);
    }

    private string ShortFurnitureName(int id)
    {
        // Compact slot badges: longer names are shown in full in the Shop.
        switch (id)
        {
            case 1: return "WINDOW";
            case 6: return "BOOKS";
            case 7: return "PILLOW";
            default: return furnitureNames[id].ToUpperInvariant();
        }
    }

    private void CreateSlot(int index)
    {
        bool wall = index < 2;
        int furnitureId = save.slots[index];
        Color bg = selectedItem >= 0 && isWallItem[selectedItem] == wall ? Hex("#D2EACF") : Hex("#EBDCC3");
        RectTransform holder = Panel(roomRect, "Slot" + index, slotPositions[index], new Vector2(169, 163), bg);
        Image hit = holder.GetComponent<Image>();
        hit.raycastTarget = true;
        Button button = holder.gameObject.AddComponent<Button>();
        int captured = index;
        button.onClick.AddListener(() => PlaceSelected(captured));
        if (furnitureId < 0)
        {
            Label(holder, "Empty", wall ? "+ WALL" : "+ FLOOR", Vector2.zero, new Vector2(156, 93), 29, ink);
        }
        else
        {
            RectTransform art = Rect(holder, "FurnitureArt", new Vector2(0, 19), new Vector2(108, 108));
            Image img = art.gameObject.AddComponent<Image>();
            img.sprite = furnitureSprites[furnitureId];
            img.preserveAspect = true;
            img.raycastTarget = true;
            HabitopiaDecorationDrag drag = art.gameObject.AddComponent<HabitopiaDecorationDrag>();
            drag.Configure(this, index, roomRect);
            Label(holder, "FurnitureName", ShortFurnitureName(furnitureId),
                new Vector2(0, -62), new Vector2(166, 51), 28, ink);
        }
    }

    private void PlaceSelected(int slot)
    {
        if (selectedItem < 0) return;
        if (isWallItem[selectedItem] != (slot < 2)) { message = "That furniture does not fit this slot."; ShowPage(0); return; }
        if (!save.owned.Contains(selectedItem)) return;
        int oldSlot = Array.IndexOf(save.slots, selectedItem);
        if (oldSlot >= 0 && oldSlot != slot) save.slots[oldSlot] = -1;
        save.slots[slot] = selectedItem;
        message = furnitureNames[selectedItem] + " placed!";
        selectedItem = -1;
        SaveData();
        ShowPage(0);
    }

    public void DragFurniture(Vector2 screenPos, int fromSlot)
    {
        if (fromSlot < 0 || fromSlot >= 6 || roomRect == null) return;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(roomRect, screenPos, null, out local)) return;
        int best = -1;
        float closest = 125f;
        for (int i = 0; i < 6; i++)
        {
            if ((i < 2) != (fromSlot < 2)) continue;
            float distance = Vector2.Distance(local, slotPositions[i]);
            if (distance < closest) { closest = distance; best = i; }
        }
        if (best >= 0 && best != fromSlot)
        {
            int temp = save.slots[best];
            save.slots[best] = save.slots[fromSlot];
            save.slots[fromSlot] = temp;
            SaveData();
            message = "Decoration moved!";
        }
        else message = "Drop furniture on a matching slot.";
        ShowPage(0);
    }

    private void BuildHabits()
    {
        Label(content, "Title", "DAILY QUESTS", new Vector2(0, 580), new Vector2(930, 95), 55, ink);
        Label(content, "Sub", "HABITS = XP + COINS", new Vector2(0, 499), new Vector2(970, 85), 31, ink);
        for (int i = 0; i < save.habits.Count && i < MaxHabits; i++)
        {
            HabitopiaHabit habit = save.habits[i];
            float y = 340 - i * 185;
            bool completed = habit.completedDate == Today();
            RectTransform row = Panel(content, "HabitRow" + i, new Vector2(0, y), new Vector2(950, 158), completed ? mint : Hex("#EEE4D2"));
            string id = habit.id;
            Action(row, "Toggle", completed ? "DONE" : "CHECK", new Vector2(-349, 0), new Vector2(166, 91),
                completed ? gold : Hex("#B7DABE"), () => ToggleHabit(id), 27);
            Label(row, "Name", habit.name, new Vector2(-22, 0), new Vector2(458, 105), 30, ink, TextAnchor.MiddleLeft);
            Action(row, "Delete", "X", new Vector2(402, 0), new Vector2(78, 80), Hex("#E9C8BF"), () => DeleteHabit(id), 31);
        }
        Label(content, "AddLabel", "ADD A HABIT (MAX 5)", new Vector2(-145, -620), new Vector2(680, 58), 30, ink);
        CreateHabitInput(new Vector2(-145, -695));
        Action(content, "Add", "ADD", new Vector2(390, -695), new Vector2(150, 86), gold, AddHabit, 33);
        Label(content, "QuestInfo", "FIRST CHECK: +10 XP +10 COINS",
            new Vector2(0, -778), new Vector2(990, 71), 26, ink);
    }

    private void CreateHabitInput(Vector2 pos)
    {
        RectTransform r = Panel(content, "NewHabitInput", pos, new Vector2(760, 86), Color.white);
        Image background = r.GetComponent<Image>();
        background.raycastTarget = true;
        newHabitInput = r.gameObject.AddComponent<InputField>();
        newHabitInput.characterLimit = 32;
        Text label = Label(r, "ValueText", "", Vector2.zero, new Vector2(712, 75), 28, ink, TextAnchor.MiddleLeft);
        Text placeholder = Label(r, "Placeholder", "e.g. Read 10 pages", Vector2.zero, new Vector2(712, 75), 28,
            Hex("#A09A8D"), TextAnchor.MiddleLeft);
        newHabitInput.textComponent = label;
        newHabitInput.placeholder = placeholder;
        newHabitInput.lineType = InputField.LineType.SingleLine;
        newHabitInput.targetGraphic = background;
    }

    private void BuildShop()
    {
        Label(content, "Title", "COZY SHOP", new Vector2(0, 590), new Vector2(900, 85), 56, ink);
        Label(content, "Hint", "BUY > PLACE IN ROOM", new Vector2(0, 511), new Vector2(1030, 75), 29, ink);
        for (int index = 0; index < furnitureNames.Length; index++)
        {
            int column = index % 2;
            int row = index / 2;
            int fi = index;
            float x = column == 0 ? -246 : 246;
            float y = 310 - row * 245;
            RectTransform card = Panel(content, "Item" + index, new Vector2(x, y), new Vector2(457, 224), Hex("#EEE4D2"));
            RectTransform art = Rect(card, "Icon", new Vector2(-138, 29), new Vector2(128, 128));
            Image image = art.gameObject.AddComponent<Image>();
            image.sprite = furnitureSprites[index];
            image.preserveAspect = true;
            image.raycastTarget = false;
            Label(card, "Name", furnitureNames[index].ToUpperInvariant(), new Vector2(79, 59), new Vector2(270, 48), 28, ink);
            bool owned = save.owned.Contains(index);
            string buttonLabel = owned ? "PLACE" : prices[index] + " COINS";
            Action(card, "BuyButton", buttonLabel, new Vector2(90, -40), new Vector2(237, 89), owned ? mint : gold,
                () => SelectOrBuy(fi), 25);
            Label(card, "Type", isWallItem[index] ? "WALL" : "FLOOR", new Vector2(-132, -77), new Vector2(152, 55), 27, ink);
        }
    }

    private void SelectOrBuy(int index)
    {
        if (!save.owned.Contains(index))
        {
            if (save.coins < prices[index]) { message = "Not enough Coins. Complete daily quests!"; ShowPage(2); return; }
            save.coins -= prices[index];
            save.owned.Add(index);
            SaveData();
        }
        selectedItem = index;
        message = "Choose a matching " + (isWallItem[index] ? "WALL" : "FLOOR") + " slot.";
        ShowPage(0);
    }

    private void BuildStats()
    {
        Label(content, "Title", "YOUR JOURNEY", new Vector2(0, 575), new Vector2(880, 93), 57, ink);
        RectTransform summary = Panel(content, "Summary", new Vector2(0, 350), new Vector2(935, 285), mint);
        Label(summary, "Stats", "LEVEL " + Level() + "     XP " + save.xp + "\nCOINS " + save.coins + "     STREAK " + Streak() + "D",
            Vector2.zero, new Vector2(895, 230), 39, ink);
        Label(content, "Week", "LAST 7 DAYS", new Vector2(0, 115), new Vector2(800, 80), 42, ink);
        for (int offset = 6; offset >= 0; offset--)
        {
            DateTime date = DateTime.Today.AddDays(-offset);
            string key = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            HabitopiaDay d = save.history.Find(x => x.date == key);
            float fraction = d != null && d.total > 0 ? Mathf.Clamp01((float)d.completed / d.total) : 0f;
            float y = 65 - (6 - offset) * 114;
            Label(content, "Date" + offset, date.ToString("ddd dd", CultureInfo.InvariantCulture),
                new Vector2(-359, y), new Vector2(210, 68), 29, ink);
            Panel(content, "BarBg" + offset, new Vector2(52, y), new Vector2(500, 49), Hex("#E9DFC9"));
            if (fraction > 0)
                Panel(content, "Bar" + offset, new Vector2(-198 + 250 * fraction, y), new Vector2(500 * fraction, 49), forest);
            Label(content, "Pct" + offset, Mathf.RoundToInt(fraction * 100) + "%", new Vector2(397, y), new Vector2(115, 58), 28, ink);
        }
        Action(content, "ResetDemo", resetPending ? "CONFIRM RESET DEMO" : "RESET DEMO",
            new Vector2(0, -709), new Vector2(680, 76), Hex("#E9C8BF"), ResetDemo, 27);
    }

    // Draw tiny original pixel-art illustrations at runtime. No image assets needed.
    private void BuildSprites()
    {
        whiteSprite = SolidSprite(Color.white);
        petSprites = new Sprite[3];
        for (int i = 0; i < 3; i++) petSprites[i] = DrawPet(i);
        furnitureSprites = new Sprite[furnitureNames.Length];
        for (int i = 0; i < furnitureSprites.Length; i++) furnitureSprites[i] = DrawFurniture(i);
    }

    private Sprite SolidSprite(Color c)
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new[] { c, c, c, c }); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f));
    }

    private Sprite SpriteFrom(Color32[] p, int width, int height)
    {
        Texture2D t = new Texture2D(width, height, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.SetPixels32(p); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, width, height), new Vector2(.5f, .5f), width);
    }

    private void Dot(Color32[] p, int w, int h, int x, int y, Color32 col)
    {
        if (x >= 0 && y >= 0 && x < w && y < h) p[y * w + x] = col;
    }

    private void Fill(Color32[] p, int w, int h, int x, int y, int rw, int rh, Color32 col)
    {
        for (int iy = y; iy < y + rh; iy++) for (int ix = x; ix < x + rw; ix++) Dot(p, w, h, ix, iy, col);
    }

    private Sprite DrawPet(int mood)
    {
        int w = 40, h = 40;
        Color32[] p = new Color32[w * h];
        Color32 outline = Hex("#426F60"), body = Hex("#92DAB3"), shadow = Hex("#61BC99");
        Color32 dark = Hex("#244B3E"), blush = Hex("#ECA5A2"), leaf = Hex("#5FAF65");
        // Ears, feet, body, sprout.
        Fill(p, w, h, 8, 5, 9, 7, outline); Fill(p, w, h, 9, 6, 7, 7, body);
        Fill(p, w, h, 23, 5, 9, 7, outline); Fill(p, w, h, 24, 6, 7, 7, body);
        for (int y = 4; y <= 31; y++)
        for (int x = 4; x <= 35; x++)
        {
            float dx = (x - 19.5f) / 16.0f;
            float dy = (y - 18f) / 14.8f;
            float d = dx * dx + dy * dy;
            if (d < 1.09f) Dot(p, w, h, x, y, outline);
            if (d < .91f) Dot(p, w, h, x, y, y < 10 ? shadow : body);
        }
        Fill(p, w, h, 11, 29, 7, 5, shadow); Fill(p, w, h, 24, 29, 7, 5, shadow);
        Fill(p, w, h, 18, 32, 3, 5, leaf); Fill(p, w, h, 20, 34, 7, 3, leaf);
        Fill(p, w, h, 10, 18, 5, 3, blush); Fill(p, w, h, 27, 18, 5, 3, blush);
        if (mood == 1) // calm
        {
            Fill(p, w, h, 13, 18, 5, 1, dark); Fill(p, w, h, 25, 18, 5, 1, dark);
            Fill(p, w, h, 19, 14, 5, 1, dark);
        }
        else
        {
            Fill(p, w, h, 14, 17, 3, 4, dark); Fill(p, w, h, 26, 17, 3, 4, dark);
            if (mood == 2) { Fill(p, w, h, 19, 13, 6, 2, dark); Dot(p, w, h, 18, 14, dark); Dot(p, w, h, 25, 14, dark); }
            else Fill(p, w, h, 20, 14, 3, 1, dark);
        }
        return SpriteFrom(p, w, h);
    }

    private Sprite DrawFurniture(int type)
    {
        int w = 32, h = 32;
        Color32[] p = new Color32[w * h];
        Color32 dark = Hex("#5B6656"), wood = Hex("#AA7956"), light = Hex("#F8DAA0"), green = Hex("#6DAD75"), sky = Hex("#9ED5DD"), rose = Hex("#E6A7AC");
        switch (type)
        {
            case 0: // poster
                Fill(p,w,h,5,5,22,22,wood); Fill(p,w,h,8,8,16,16,light);
                Fill(p,w,h,13,12,7,7,green); Fill(p,w,h,12,10,9,2,green); break;
            case 1: // window
                Fill(p,w,h,4,4,24,24,wood); Fill(p,w,h,7,7,18,18,sky);
                Fill(p,w,h,15,7,3,18,light); Fill(p,w,h,7,15,18,3,light); break;
            case 2: // clock
                for (int y=3;y<29;y++) for(int x=3;x<29;x++)
                { float d=(x-16)*(x-16)+(y-16)*(y-16); if(d<169) Dot(p,w,h,x,y,wood); if(d<121) Dot(p,w,h,x,y,light); }
                Fill(p,w,h,15,16,2,7,dark); Fill(p,w,h,16,15,7,2,dark); break;
            case 3: // plant
                Fill(p,w,h,10,5,13,10,wood); Fill(p,w,h,12,15,9,13,green);
                Fill(p,w,h,5,17,22,5,green); Fill(p,w,h,14,21,5,9,green); break;
            case 4: // rug
                Fill(p,w,h,3,9,26,15,wood); Fill(p,w,h,5,11,22,11,rose);
                Fill(p,w,h,11,14,10,5,light); break;
            case 5: // lamp
                Fill(p,w,h,13,3,6,9,wood); Fill(p,w,h,15,11,3,12,dark);
                Fill(p,w,h,7,22,18,7,light); Fill(p,w,h,11,29,10,2,wood); break;
            case 6: // bookcase
                Fill(p,w,h,5,3,22,27,wood); Fill(p,w,h,8,6,16,8,light); Fill(p,w,h,8,17,16,9,light);
                Fill(p,w,h,9,6,3,8,rose); Fill(p,w,h,14,6,4,8,green); Fill(p,w,h,19,17,3,9,sky); break;
            case 7: // cushion
                Fill(p,w,h,4,10,24,15,wood); Fill(p,w,h,6,12,20,11,rose);
                Fill(p,w,h,14,13,4,9,light); Fill(p,w,h,10,16,12,3,light); break;
        }
        return SpriteFrom(p,w,h);
    }
}

// Component placed on furniture images; drag to compatible slots with snap-to-grid.
public class HabitopiaDecorationDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private HabitopiaDemo app;
    private int sourceSlot;
    private RectTransform room;
    private RectTransform rt;
    private Vector2 initialPosition;
    private Transform initialParent;
    private bool dragging;

    public void Configure(HabitopiaDemo owner, int slot, RectTransform targetRoom)
    {
        app = owner; sourceSlot = slot; room = targetRoom;
        rt = (RectTransform)transform;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (app == null || room == null) return;
        dragging = true;
        initialParent = transform.parent;
        initialPosition = rt.anchoredPosition;
        rt.SetParent(room, true);
        rt.SetAsLastSibling();
        OnDrag(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging || room == null) return;
        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(room, e.position, e.pressEventCamera, out local))
            rt.anchoredPosition = local;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        if (app != null) app.DragFurniture(e.position, sourceSlot);
        // The room UI is rebuilt by DragFurniture. No need to restore the icon.
    }
}
