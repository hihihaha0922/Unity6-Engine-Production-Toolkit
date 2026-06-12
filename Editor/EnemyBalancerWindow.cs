using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

// We include a dummy enum option for the filter dropdown so designers can view everything at once
public enum FilterFactionType { All, Enemy, Ally, Neutral }

public class EnemyBalancerWindow : EditorWindow
{
    private List<CharacterStats> _foundCharacters = new List<CharacterStats>();
    private VisualElement _rowsContainer;

    // SEARCH & FILTER STATE VARIABLES
    private string _currentSearchQuery = "";
    private FilterFactionType _currentFilter = FilterFactionType.All;

    [MenuItem("Larian Tools/Character Stat Balancer")]
    public static void ShowWindow()
    {
        EnemyBalancerWindow wnd = GetWindow<EnemyBalancerWindow>();
        wnd.titleContent = new GUIContent("Character Balancer");
        wnd.minSize = new Vector2(650, 450);
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        // Title Header
        Label title = new Label("Master Character Balance Dashboard");
        title.style.fontSize = 18;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 10;
        title.style.marginTop = 10;
        root.Add(title);

        // ==========================================
        // NEW: SEARCH & FILTER CONTROL BAR PANEL
        // ==========================================
        VisualElement filterBar = new VisualElement();
        filterBar.style.flexDirection = FlexDirection.Row;
        filterBar.style.marginBottom = 8;
        filterBar.style.paddingTop = 4;
        filterBar.style.paddingBottom = 4;
        filterBar.style.paddingLeft = 4;
        filterBar.style.paddingRight = 4;
        filterBar.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        filterBar.style.borderTopLeftRadius = 4;
        filterBar.style.borderTopRightRadius = 4;
        filterBar.style.borderBottomLeftRadius = 4;
        filterBar.style.borderBottomRightRadius = 4;

        // 1. Text Search Box Field
        TextField searchField = new TextField("🔍 Search Name:");
        searchField.style.flexGrow = 1;
        searchField.style.marginRight = 10;
        searchField.RegisterValueChangedCallback(evt =>
        {
            _currentSearchQuery = evt.newValue.Trim().ToLower();
            RefreshDisplayedRows(); // Live update rows as user types!
        });
        filterBar.Add(searchField);

        // 2. Faction Filter Dropdown Selection
        EnumField factionFilterField = new EnumField("Filter Faction:", FilterFactionType.All);
        factionFilterField.style.width = 220;
        factionFilterField.RegisterValueChangedCallback(evt =>
        {
            _currentFilter = (FilterFactionType)evt.newValue;
            RefreshDisplayedRows(); // Live update rows when selection changes!
        });
        filterBar.Add(factionFilterField);

        root.Add(filterBar);
        // ==========================================

        // Master Control Action Buttons
        VisualElement headerButtons = new VisualElement();
        headerButtons.style.flexDirection = FlexDirection.Row;
        headerButtons.style.marginBottom = 10;

        Button loadButton = new Button(LoadCharacterDataFromProjectFolders);
        loadButton.text = "🔄 Reload Assets from Disk";
        loadButton.style.height = 30;
        loadButton.style.flexGrow = 1;
        headerButtons.Add(loadButton);

        Button saveButton = new Button(SaveChangesToDisk);
        saveButton.text = "💾 Save Master Balance Sheet";
        saveButton.style.height = 30;
        saveButton.style.flexGrow = 1;
        saveButton.style.backgroundColor = new Color(0.15f, 0.35f, 0.15f);
        headerButtons.Add(saveButton);

        root.Add(headerButtons);

        // Append column titles label matrix
        root.Add(CreateCharacterTableHeaderLabels());

        // Setup Viewport Scrollbox container
        ScrollView scrollView = new ScrollView();
        _rowsContainer = new VisualElement();
        scrollView.Add(_rowsContainer);
        root.Add(scrollView);

        // Initial launch data loading sequence
        LoadCharacterDataFromProjectFolders();
    }

    // Step 1: Hard Drive File Scanning Data Sweep
    private void LoadCharacterDataFromProjectFolders()
    {
        _foundCharacters.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                CharacterStats stats = prefab.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    _foundCharacters.Add(stats);
                }
            }
        }

        // Once data is loaded fresh into memory cache list arrays, render the UI rows
        RefreshDisplayedRows();
    }

    // Step 2: Live UI Redraw Evaluation (This handles sorting/filtering display states)
    private void RefreshDisplayedRows()
    {
        _rowsContainer.Clear();

        foreach (CharacterStats stats in _foundCharacters)
        {
            // CHECK FILTER 1: Does it match our text query search string?
            if (!string.IsNullOrEmpty(_currentSearchQuery))
            {
                if (!stats.characterName.ToLower().Contains(_currentSearchQuery) &&
                    !stats.gameObject.name.ToLower().Contains(_currentSearchQuery))
                {
                    continue; // Skip rendering this item row, it doesn't match text search
                }
            }

            // CHECK FILTER 2: Does it match our Faction Type filter setting?
            if (_currentFilter != FilterFactionType.All)
            {
                // Convert current state into string names to safely compare the shared enums
                if (stats.faction.ToString() != _currentFilter.ToString())
                {
                    continue; // Skip rendering this item row, it belongs to a different team faction
                }
            }

            // If it survives both filter tests, build its row inside our interface layout!
            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(stats.gameObject);
            if (sourcePrefab == null) sourcePrefab = stats.gameObject;

            BuildDataGridRow(stats, sourcePrefab);
        }
    }

    private void BuildDataGridRow(CharacterStats stats, GameObject sourcePrefab)
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginBottom = 4;
        row.style.paddingTop = 4;
        row.style.paddingBottom = 4;
        row.style.paddingLeft = 4;

        // Visual design faction background tint application logic
        if (stats.faction == FactionType.Enemy)
            row.style.backgroundColor = new Color(0.25f, 0.15f, 0.15f); // Red Tint
        else if (stats.faction == FactionType.Ally)
            row.style.backgroundColor = new Color(0.15f, 0.2f, 0.3f);   // Blue Tint
        else
            row.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);    // Grey Slate

        // Column 1: Asset Target Link Button
        Button inspectBtn = new Button(() => { EditorGUIUtility.PingObject(sourcePrefab); Selection.activeObject = sourcePrefab; });
        inspectBtn.text = sourcePrefab.name;
        inspectBtn.style.width = 130;
        row.Add(inspectBtn);

        // Column 2: Faction Dropdown Component Selection
        EnumField factionField = new EnumField(stats.faction);
        factionField.style.width = 90;
        factionField.RegisterValueChangedCallback(evt =>
        {
            stats.faction = (FactionType)evt.newValue;
            RefreshDisplayedRows(); // Instantly update view tracking configurations layout colors
        });
        row.Add(factionField);

        // Column 3: Name Input Field
        TextField nameField = new TextField();
        nameField.value = stats.characterName;
        nameField.style.width = 110;
        nameField.RegisterValueChangedCallback(evt => stats.characterName = evt.newValue);
        row.Add(nameField);

        // Column 4: Health Input Box
        IntegerField healthField = new IntegerField();
        healthField.value = stats.maxHealth;
        healthField.style.width = 80;
        healthField.RegisterValueChangedCallback(evt => stats.maxHealth = evt.newValue);
        row.Add(healthField);

        // Column 5: Speed Input Box
        FloatField speedField = new FloatField();
        speedField.value = stats.movementSpeed;
        speedField.style.width = 80;
        speedField.RegisterValueChangedCallback(evt => stats.movementSpeed = evt.newValue);
        row.Add(speedField);

        // Column 6: Attack Power Input Box
        IntegerField damageField = new IntegerField();
        damageField.value = stats.attackDamage;
        damageField.style.width = 80;
        damageField.RegisterValueChangedCallback(evt => stats.attackDamage = evt.newValue);
        row.Add(damageField);

        _rowsContainer.Add(row);
    }

    private void SaveChangesToDisk()
    {
        foreach (CharacterStats stats in _foundCharacters)
        {
            EditorUtility.SetDirty(stats);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Character Balancer] Successfully synchronized and saved all modified configurations back to project assets!");
    }

    private VisualElement CreateCharacterTableHeaderLabels()
    {
        VisualElement header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.paddingBottom = 4;
        header.style.borderBottomColor = Color.gray;
        header.style.borderBottomWidth = 1;

        string[] labels = { "Asset File", "Faction Team", "Display Name", "Max Health", "Speed", "Damage" };
        int[] widths = { 130, 90, 110, 80, 80, 80 };

        for (int i = 0; i < labels.Length; i++)
        {
            Label lbl = new Label(labels[i]);
            lbl.style.width = widths[i];
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(lbl);
        }

        return header;
    }
}