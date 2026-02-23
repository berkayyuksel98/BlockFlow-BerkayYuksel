#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Unity üst menüsüne "BlockFlow/Level Editor" olarak eklenen görsel level tasarım editörü
// Şekil tabanlı blok yerleştirme, JSON kayıt/yükleme/düzenleme ve silme
public class LevelEditorWindow : EditorWindow
{
    #region Fields

    private int rows = 6, columns = 6;
    private float timeLimit = 60f;
    private LevelData levelData;
    private string currentJsonPath;

    private int shapeIndex;
    private string[] shapeNames = new string[0];
    private BlockShapeData selectedShape;
    private BlockColor selectedColor = BlockColor.Red;
    private BlockType selectedType = BlockType.Normal;
    private MovementAxis selectedAxis = MovementAxis.Horizontal;
    private readonly List<RawBehaviourEntry> pendingBehaviours = new List<RawBehaviourEntry>();
    private BlockBehaviourType newBehaviourType = BlockBehaviourType.Ice;
    private int iceRequiredExitCount = 1;

    private const float CellSize = 54f, CellPad = 3f;
    private Vector2 leftScroll, gridScroll;
    private Vector2Int hoveredCell = new Vector2Int(-1, -1);

    private enum EditorMode { Block, Exit }
    private EditorMode editorMode       = EditorMode.Block;
    private BlockColor exitColor        = BlockColor.Red;
    private int        exitSize         = 1;
    private ExitSide   hoverBorderSide  = ExitSide.Top;
    private int        hoverBorderIndex = -1;

    private readonly Dictionary<string, BlockShapeData> shapes = new Dictionary<string, BlockShapeData>();

    private static readonly Dictionary<BlockColor, Color> colors = new Dictionary<BlockColor, Color>
    {
        { BlockColor.Red,    new Color(0.86f, 0.27f, 0.27f) },
        { BlockColor.Blue,   new Color(0.27f, 0.47f, 0.87f) },
        { BlockColor.Green,  new Color(0.27f, 0.72f, 0.38f) },
        { BlockColor.Yellow, new Color(0.96f, 0.82f, 0.22f) },
        { BlockColor.Purple, new Color(0.66f, 0.27f, 0.86f) },
    };

    #endregion

    #region Init & Lifecycle

    [MenuItem("BlockFlow/Level Editor")]
    public static void OpenWindow()
    {
        var window = GetWindow<LevelEditorWindow>("BlockFlow Level Editor");
        window.minSize = new Vector2(820f, 500f);
        window.Show();
    }

    private void OnEnable() { wantsMouseMove = true; RebuildGrid(); RefreshShapeCache(); }
    private void OnFocus() { RefreshShapeCache(); }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        leftScroll = EditorGUILayout.BeginScrollView(leftScroll, GUILayout.Width(268), GUILayout.ExpandHeight(true));
        DrawLeftPanel();
        EditorGUILayout.EndScrollView();

        Rect sep = GUILayoutUtility.GetRect(2f, 0f, GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(sep, new Color(0f, 0f, 0f, 0.40f));

        gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawGrid();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Sol Panel
    private void DrawLeftPanel()
    {
        editorMode = (EditorMode)GUILayout.Toolbar((int)editorMode, new[] { "Blok Modu", "Exit Modu" });
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Grid Ayarları", EditorStyles.boldLabel);
        rows      = Mathf.Clamp(EditorGUILayout.IntField("Satır",    rows),    2, 20);
        columns   = Mathf.Clamp(EditorGUILayout.IntField("Sütun",    columns), 2, 20);
        timeLimit = EditorGUILayout.FloatField("Süre (sn)", timeLimit);
        EditorGUILayout.Space(8);

        if (editorMode == EditorMode.Block)
        {
            EditorGUILayout.LabelField("Blok Araçları", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (shapeNames.Length > 0)
            {
                EditorGUI.BeginChangeCheck();
                shapeIndex = EditorGUILayout.Popup("Şekil", shapeIndex, shapeNames);
                if (EditorGUI.EndChangeCheck()) shapes.TryGetValue(shapeNames[shapeIndex], out selectedShape);
            }
            else { EditorGUILayout.LabelField("Şekil", "(bulunamadı)"); }
            if (GUILayout.Button("Yenile", GUILayout.Width(52))) RefreshShapeCache();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            selectedColor = (BlockColor)EditorGUILayout.EnumPopup("Renk",  selectedColor);
            selectedType  = (BlockType) EditorGUILayout.EnumPopup("Tip",   selectedType);
            if (selectedType == BlockType.SingleAxis) selectedAxis = (MovementAxis)EditorGUILayout.EnumPopup("Eksen", selectedAxis);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Davranışlar", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            newBehaviourType = (BlockBehaviourType)EditorGUILayout.EnumPopup(newBehaviourType);
            if (GUILayout.Button("+ Ekle", GUILayout.Width(60))) AddPendingBehaviour(newBehaviourType);
            EditorGUILayout.EndHorizontal();
            for (int bi = pendingBehaviours.Count - 1; bi >= 0; bi--) DrawBehaviourEntry(bi);
            EditorGUILayout.Space(8);
            DrawBlockList();
        }
        else
        {
            EditorGUILayout.LabelField("Çıkış Araçları", EditorStyles.boldLabel);
            exitColor = (BlockColor)EditorGUILayout.EnumPopup("Renk",  exitColor);
            exitSize  = Mathf.Clamp(EditorGUILayout.IntField("Boyut", exitSize), 1, Mathf.Max(rows, columns));
            EditorGUILayout.Space(8);
            DrawExitList();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.HelpBox(
            editorMode == EditorMode.Block
                ? "Sol Tık → Blok Yerleştir   Sağ Tık → Sil"
                : "Kenara Sol Tık → Çıkış Ekle   Sağ Tık → Sil",
            MessageType.Info);
        EditorGUILayout.Space(6);
        string title = string.IsNullOrEmpty(currentJsonPath) ? "● Kaydedilmemiş" : Path.GetFileNameWithoutExtension(currentJsonPath);
        EditorGUILayout.LabelField(title, EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Yeni"))   NewLevel();
        if (GUILayout.Button("Aç"))     OpenJson();
        if (GUILayout.Button("Kaydet")) SaveLevel();
        EditorGUILayout.EndHorizontal();
    }
    private void DrawBlockList()
    {
        if (levelData == null || levelData.Blocks.Count == 0) return;
        EditorGUILayout.LabelField("Bloklar (" + levelData.Blocks.Count + ")", EditorStyles.boldLabel);
        for (int i = levelData.Blocks.Count - 1; i >= 0; i--)
        {
            var b = levelData.Blocks[i];
            EditorGUILayout.BeginHorizontal();
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = colors.TryGetValue(b.Color, out var c) ? c : Color.gray;
            GUILayout.Box(GUIContent.none, GUILayout.Width(14), GUILayout.Height(14));
            GUI.backgroundColor = prev;
            string tag = string.IsNullOrEmpty(b.ShapeId) ? "?" : b.ShapeId;
            GUILayout.Label("(" + b.GridPosition.x + "," + b.GridPosition.y + ") " + tag, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("✕", GUILayout.Width(22))) { levelData.Blocks.RemoveAt(i); Repaint(); }
            EditorGUILayout.EndHorizontal();
        }
    }

    // Yeni behaviour ekler; aynı tip tekrar eklenemez
    private void AddPendingBehaviour(BlockBehaviourType type)
    {
        if (pendingBehaviours.Exists(b => b.Type == type)) return;
        string dataJson = type == BlockBehaviourType.Ice
            ? JsonUtility.ToJson(new IceBehaviourData { RequiredExitCount = iceRequiredExitCount })
            : "{}";
        pendingBehaviours.Add(new RawBehaviourEntry { Type = type, DataJson = dataJson });
    }

    // Davranış satırını ve tipo özgü ayarları çizer
    private void DrawBehaviourEntry(int index)
    {
        var entry = pendingBehaviours[index];
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(entry.Type.ToString(), GUILayout.Width(40));
        if (entry.Type == BlockBehaviourType.Ice)
        {
            iceRequiredExitCount = EditorGUILayout.IntField("Gereken:", Mathf.Max(1, iceRequiredExitCount));
            pendingBehaviours[index] = new RawBehaviourEntry
            {
                Type     = entry.Type,
                DataJson = JsonUtility.ToJson(new IceBehaviourData { RequiredExitCount = iceRequiredExitCount }),
            };
        }
        if (GUILayout.Button("✕", GUILayout.Width(22))) pendingBehaviours.RemoveAt(index);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGrid()
    {
        if (levelData == null) return;
        const float B = 30f; // kenar şerit genişliği (çıkış noktaları için)
        float gx   = 0f, gy = 0f; // grid başlangıç koordinatları (alan içinde)
        float gridW = columns * (CellSize + CellPad) + CellPad;
        float gridH = rows    * (CellSize + CellPad) + CellPad;
        Rect  area  = GUILayoutUtility.GetRect(gridW + B * 2 + 4f, gridH + B * 2 + 4f);
        gx = area.x + B;
        gy = area.y + B;
        Event e       = Event.current;
        bool  inBlock = editorMode == EditorMode.Block;

        // ── Grid Hücreleri ──────────────────────────────────────────────────
        for (int row = 0; row < rows; row++)
        for (int col = 0; col < columns; col++)
        {
            var  pos  = new Vector2Int(col, row);
            // Y ekseni ters: row=0 sol alt köşe
            Rect cell = new Rect(gx + CellPad + col * (CellSize + CellPad), gy + CellPad + (rows - 1 - row) * (CellSize + CellPad), CellSize, CellSize);
            bool hover        = inBlock && cell.Contains(e.mousePosition);
            BlockData blockAt = GetBlockAt(pos);
            bool preview      = inBlock && IsPreviewCell(pos);

            Color bg;
            if (blockAt != null)
            {
                bg = colors.TryGetValue(blockAt.Color, out var bc) ? bc : Color.gray;
                bool hasIce = blockAt.Behaviours != null && blockAt.Behaviours.Exists(b => b.Type == BlockBehaviourType.Ice);
                if (hasIce) bg = Color.Lerp(bg, new Color(0.60f, 0.88f, 1f), 0.45f);
            }
            else if (preview) { var p = colors.TryGetValue(selectedColor, out var pc) ? pc : Color.gray; bg = new Color(p.r, p.g, p.b, 0.35f); }
            else               { bg = hover ? new Color(0.33f, 0.33f, 0.33f) : new Color(0.19f, 0.19f, 0.19f); }

            EditorGUI.DrawRect(cell, bg);
            { Color bc2 = blockAt != null ? new Color(0, 0, 0, 0.55f) : new Color(0.45f, 0.45f, 0.45f, 0.25f);
              EditorGUI.DrawRect(new Rect(cell.x, cell.y, cell.width, 1f), bc2);
              EditorGUI.DrawRect(new Rect(cell.x, cell.yMax - 1f, cell.width, 1f), bc2);
              EditorGUI.DrawRect(new Rect(cell.x, cell.y, 1f, cell.height), bc2);
              EditorGUI.DrawRect(new Rect(cell.xMax - 1f, cell.y, 1f, cell.height), bc2); }

            if (blockAt != null)
            {
                string lbl = string.IsNullOrEmpty(blockAt.ShapeId) ? "?" : blockAt.ShapeId;
                bool hasIceLbl = blockAt.Behaviours != null && blockAt.Behaviours.Exists(b => b.Type == BlockBehaviourType.Ice);
                if (hasIceLbl)
                {
                    var ie = blockAt.Behaviours.Find(b => b.Type == BlockBehaviourType.Ice);
                    var id = string.IsNullOrEmpty(ie.DataJson) ? new IceBehaviourData() : JsonUtility.FromJson<IceBehaviourData>(ie.DataJson);
                    lbl += "\nICE:" + id.RequiredExitCount;
                }
                else if (blockAt.Type == BlockType.SingleAxis) lbl += blockAt.MovementAxis == MovementAxis.Horizontal ? "\n↔" : "\n↕";
                GUI.Label(cell, lbl, CellLabel());
            }

            if (inBlock && e.type == EventType.MouseDown && hover)
            {
                if (e.button == 0) { PlaceBlockAt(pos);  e.Use(); Repaint(); }
                if (e.button == 1) { RemoveBlockAt(pos); e.Use(); Repaint(); }
            }
            if (inBlock && e.type == EventType.MouseMove && hover) { hoveredCell = pos; Repaint(); }
        }
        DrawExitBorders(area, gx, gy, B, e);

        if (e.type == EventType.MouseLeaveWindow) { hoveredCell = new Vector2Int(-1, -1); hoverBorderIndex = -1; Repaint(); }
    }

    #endregion

    #region Blok Yerleştirme ve Silme
    private void PlaceBlockAt(Vector2Int pivot) //Blok yerleştirme işlemini yapar.
    {
        if (selectedShape == null) { Debug.LogWarning("Blok şekli seçilmedi!"); return; }
        foreach (var offset in selectedShape.Cells)
        {
            Vector2Int c = pivot + offset;
            if (c.x < 0 || c.x >= columns || c.y < 0 || c.y >= rows) return; // grid dışına çıkma kontrolü
        }
        foreach (var offset in selectedShape.Cells) RemoveBlockAt(pivot + offset); //bloğun kaplayacağı hücreleri temizleyelim.
        levelData.Blocks.Add(new BlockData
        {
            GridPosition = pivot,
            ShapeId = selectedShape.ShapeId,
            Color = selectedColor,
            Type = selectedType,
            MovementAxis = selectedType == BlockType.SingleAxis ? selectedAxis : MovementAxis.Free,
            Behaviours = new List<RawBehaviourEntry>(pendingBehaviours),
        });
    }

    private void RemoveBlockAt(Vector2Int pos)  //Blok silme işlemini yapar.
    {
        if (levelData == null) return;
        for (int i = levelData.Blocks.Count - 1; i >= 0; i--)
            if (OccupiesCell(levelData.Blocks[i], pos)) { levelData.Blocks.RemoveAt(i); return; }
    }

    #endregion

    #region Kaydet / Yükle
    private void NewLevel() { currentJsonPath = null; RebuildGrid(); }

    private void OpenJson()
    {
        string defaultDir = Path.Combine(Application.dataPath, "_Project", "Level", "Data");
        if (!Directory.Exists(defaultDir)) defaultDir = Application.dataPath;
        string path = EditorUtility.OpenFilePanel("Level JSON Aç", defaultDir, "json");
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        var data = JsonUtility.FromJson<LevelData>(File.ReadAllText(path));
        if (data == null) return;
        levelData = data; rows = data.Rows; columns = data.Columns; timeLimit = data.TimeLimit;
        currentJsonPath = path; Repaint();
    }

    private void SaveLevel()
    {
        if (levelData == null) return;
        if (string.IsNullOrEmpty(currentJsonPath))
        {
            string dir = Path.Combine(Application.dataPath, "_Project", "Resources", "Levels");
            Directory.CreateDirectory(dir);
            currentJsonPath = EditorUtility.SaveFilePanel("Level Kaydet", dir, "Level_1", "json");
            if (string.IsNullOrEmpty(currentJsonPath)) return;
        }
        levelData.Rows = rows; levelData.Columns = columns; levelData.TimeLimit = timeLimit;
        File.WriteAllText(currentJsonPath, JsonUtility.ToJson(levelData, true));
        AssetDatabase.Refresh();
    }
    #endregion

    #region Yardımcı Metodlar
    private void RebuildGrid() //yeni bir level data oluşturur
    {
        levelData = new LevelData { Rows = rows, Columns = columns, TimeLimit = timeLimit };
    }

    private void RefreshShapeCache() //Projede bulunan tüm BlockShapeData assetlerini bulup ve shapes dictionarysini günceller
    {
        shapes.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:BlockShapeData"))
        {
            var a = AssetDatabase.LoadAssetAtPath<BlockShapeData>(AssetDatabase.GUIDToAssetPath(guid));
            if (a != null && !string.IsNullOrEmpty(a.ShapeId)) shapes[a.ShapeId] = a;
        }
        shapeNames = new string[shapes.Count];
        shapes.Keys.CopyTo(shapeNames, 0);
        shapeIndex = Mathf.Clamp(shapeIndex, 0, Mathf.Max(0, shapeNames.Length - 1));
        if (shapeNames.Length > 0) shapes.TryGetValue(shapeNames[shapeIndex], out selectedShape);
        Repaint();
    }

    private BlockShapeData FindShape(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        shapes.TryGetValue(id, out var s); return s;
    }

    private BlockData GetBlockAt(Vector2Int gp) //belirtilen grid pozisyonunda blok varsa döner yoksa null döner
    {
        if (levelData == null) return null;
        foreach (var block in levelData.Blocks) if (OccupiesCell(block, gp)) return block;
        return null;
    }

    private bool OccupiesCell(BlockData block, Vector2Int pos)
    {
        var shape = FindShape(block.ShapeId);
        if (shape?.Cells != null && shape.Cells.Count > 0)
        {
            foreach (var o in shape.Cells) if (block.GridPosition + o == pos) return true;
            return false;
        }
        return block.GridPosition == pos; // şekil bulunamazsa yalnızca pivot'a bak
    }

    private bool IsPreviewCell(Vector2Int gp)
    {
        if (selectedShape?.Cells == null || hoveredCell.x < 0) return false;
        foreach (var o in selectedShape.Cells) if (hoveredCell + o == gp) return true;
        return false;
    }

    private static GUIStyle CellLabel() => new GUIStyle(EditorStyles.miniLabel)
    { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true, normal = { textColor = Color.white } };

    #endregion

    #region Çıkış Noktaları

    private void DrawExitBorders(Rect area, float gx, float gy, float B, Event e)
    {
        if (levelData == null) return;
        bool exitMode = editorMode == EditorMode.Exit;

        // Slot arkaplanları (yalnızca çıkış modunda)
        if (exitMode)
        {
            for (int s = 0; s < 4; s++)
            {
                int count = s < 2 ? columns : rows;
                for (int idx = 0; idx < count; idx++)
                {
                    if (GetExitAt((ExitSide)s, idx) != null) continue;
                    bool inRange = hoverBorderSide == (ExitSide)s && hoverBorderIndex >= 0 &&
                                   idx >= hoverBorderIndex && idx < hoverBorderIndex + exitSize;
                    EditorGUI.DrawRect(BorderSlotRect((ExitSide)s, idx, gx, gy, B, area),
                        inRange ? new Color(0.6f, 0.6f, 0.6f, 0.28f) : new Color(0.3f, 0.3f, 0.3f, 0.15f));
                }
            }
        }

        // mevcut çıkışları çiz
        if (levelData.Exits != null)
        {
            foreach (var exit in levelData.Exits)
            {
                Color col = colors.TryGetValue(exit.Color, out var c) ? c : Color.gray;
                Rect  r   = ExitBarRect(exit, gx, gy, B, area);
                EditorGUI.DrawRect(r, new Color(col.r, col.g, col.b, 0.90f));
                { Color bc2 = new Color(0, 0, 0, 0.55f);
                  EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), bc2);
                  EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), bc2);
                  EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), bc2);
                  EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), bc2); }
                if (exit.Size > 1) GUI.Label(r, exit.Size.ToString(), CellLabel());
            }
        }

        // Hover önizlemesi
        if (exitMode && hoverBorderIndex >= 0)
        {
            int maxCount = (hoverBorderSide == ExitSide.Top || hoverBorderSide == ExitSide.Bottom) ? columns : rows;
            int previewSize = Mathf.Clamp(exitSize, 1, maxCount - hoverBorderIndex);
            bool overlaps = false;
            for (int i = hoverBorderIndex; i < hoverBorderIndex + previewSize; i++)
                if (GetExitAt(hoverBorderSide, i) != null) { overlaps = true; break; }
            if (!overlaps)
            {
                var dummy = new ExitData { Side = hoverBorderSide, StartIndex = hoverBorderIndex, Size = previewSize };
                Color pc  = colors.TryGetValue(exitColor, out var pCol) ? pCol : Color.gray;
                Rect  pr  = ExitBarRect(dummy, gx, gy, B, area);
                EditorGUI.DrawRect(pr, new Color(pc.r, pc.g, pc.b, 0.38f));
                { Color bc2 = new Color(pc.r, pc.g, pc.b, 0.75f);
                  EditorGUI.DrawRect(new Rect(pr.x, pr.y, pr.width, 1f), bc2);
                  EditorGUI.DrawRect(new Rect(pr.x, pr.yMax - 1f, pr.width, 1f), bc2);
                  EditorGUI.DrawRect(new Rect(pr.x, pr.y, 1f, pr.height), bc2);
                  EditorGUI.DrawRect(new Rect(pr.xMax - 1f, pr.y, 1f, pr.height), bc2); }
            }
        }

        if (!exitMode) return;

        // Mouse olayları
        for (int s = 0; s < 4; s++)
        {
            int count = s < 2 ? columns : rows;
            for (int idx = 0; idx < count; idx++)
            {
                Rect slot = BorderSlotRect((ExitSide)s, idx, gx, gy, B, area);
                if (!slot.Contains(e.mousePosition)) continue;
                if (e.type == EventType.MouseMove) { hoverBorderSide = (ExitSide)s; hoverBorderIndex = idx; Repaint(); }
                if (e.type == EventType.MouseDown)
                {
                    var ex = GetExitAt((ExitSide)s, idx);
                    if (e.button == 0) { if (ex != null) levelData.Exits.Remove(ex); else PlaceExit((ExitSide)s, idx); }
                    if (e.button == 1 && ex != null) levelData.Exits.Remove(ex);
                    e.Use(); Repaint();
                }
            }
        }
    }

    // Çıkışın kenar şeridindeki tam boyutlu dikdörtgenini hesaplar
    private Rect ExitBarRect(ExitData exit, float gx, float gy, float B, Rect area)
    {
        const float pad = 3f;
        float w = exit.Size * CellSize + (exit.Size - 1) * CellPad;
        float h = exit.Size * CellSize + (exit.Size - 1) * CellPad;
        float x = gx + CellPad + exit.StartIndex * (CellSize + CellPad);
        float y = gy + CellPad + (rows - exit.StartIndex - exit.Size) * (CellSize + CellPad);
        switch (exit.Side)
        {
            case ExitSide.Top:    return new Rect(x, area.y + pad, w, B - pad * 2);
            case ExitSide.Bottom: return new Rect(x, gy + rows * (CellSize + CellPad) + CellPad + pad, w, B - pad * 2);
            case ExitSide.Left:   return new Rect(area.x + pad, y, B - pad * 2, h);
            default:              return new Rect(gx + columns * (CellSize + CellPad) + CellPad + pad, y, B - pad * 2, h); // Right
        }
    }

    // Kenar boyunca tek bir hücrenin slot dikdörtgenini döndürür
    private Rect BorderSlotRect(ExitSide side, int idx, float gx, float gy, float B, Rect area)
    {
        const float pad = 2f;
        float cx = gx + CellPad + idx * (CellSize + CellPad);
        float cy = gy + CellPad + (rows - 1 - idx) * (CellSize + CellPad);
        switch (side)
        {
            case ExitSide.Top:    return new Rect(cx, area.y + pad, CellSize, B - pad * 2);
            case ExitSide.Bottom: return new Rect(cx, gy + rows * (CellSize + CellPad) + CellPad + pad, CellSize, B - pad * 2);
            case ExitSide.Left:   return new Rect(area.x + pad, cy, B - pad * 2, CellSize);
            default:              return new Rect(gx + columns * (CellSize + CellPad) + CellPad + pad, cy, B - pad * 2, CellSize); // Right
        }
    }

    // Belirtilen kenar ve indeks konumunda çıkış varsa döndürür; yoksa null
    private ExitData GetExitAt(ExitSide side, int index)
    {
        if (levelData?.Exits == null) return null;
        foreach (var ex in levelData.Exits)
            if (ex.Side == side && index >= ex.StartIndex && index < ex.StartIndex + ex.Size) return ex;
        return null;
    }

    // Yeni çıkış ekler; sınır dışı veya çakışma varsa işlem yapmaz
    private void PlaceExit(ExitSide side, int startIndex)
    {
        int max  = (side == ExitSide.Top || side == ExitSide.Bottom) ? columns : rows;
        int size = Mathf.Clamp(exitSize, 1, max - startIndex);
        for (int i = startIndex; i < startIndex + size; i++)
            if (GetExitAt(side, i) != null) return;
        levelData.Exits.Add(new ExitData { Color = exitColor, Side = side, StartIndex = startIndex, Size = size });
    }

    // Çıkış noktası listesini sol panelde çizer
    private void DrawExitList()
    {
        if (levelData?.Exits == null || levelData.Exits.Count == 0) return;
        EditorGUILayout.LabelField("Çıkışlar (" + levelData.Exits.Count + ")", EditorStyles.boldLabel);
        string[] sideLabel = { "Üst", "Alt", "Sol", "Sağ" };
        for (int i = levelData.Exits.Count - 1; i >= 0; i--)
        {
            var ex = levelData.Exits[i];
            EditorGUILayout.BeginHorizontal();
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = colors.TryGetValue(ex.Color, out var c) ? c : Color.gray;
            GUILayout.Box(GUIContent.none, GUILayout.Width(14), GUILayout.Height(14));
            GUI.backgroundColor = prev;
            GUILayout.Label(sideLabel[(int)ex.Side] + "  [" + ex.StartIndex + "~" + (ex.StartIndex + ex.Size - 1) + "]  " + ex.Color, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("✕", GUILayout.Width(22))) { levelData.Exits.RemoveAt(i); Repaint(); }
            EditorGUILayout.EndHorizontal();
        }
    }

    #endregion
}

#endif
