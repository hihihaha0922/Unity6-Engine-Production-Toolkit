using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class AssetScannerWindow : EditorWindow
{
    // =========================================================================
    // PART 1: THE CONTAINER VARIABLE
    // =========================================================================
    // This variable holds a live reference to the visual UI panel area where our 
    // generated rows will be spawned. Keeping it at class level means we can 
    // easily run .Clear() or .Add() from any method when the user runs a scan.
    private VisualElement _resultsContainer;

    // =========================================================================
    // PART 2: REGISTERING THE WINDOW IN UNITY
    // =========================================================================
    // [MenuItem] creates a shortcut at the very top of your Unity Editor menu bar.
    // When a developer clicks it, this static method executes, allocates a native
    // dockable window panel, styles its title tab, and locks in its minimum size.
    [MenuItem("Larian Tools/Asset Scanner")]
    public static void ShowWindow()
    {
        AssetScannerWindow wnd = GetWindow<AssetScannerWindow>();
        wnd.titleContent = new GUIContent("Asset Scanner");
        wnd.minSize = new Vector2(400, 300);
    }

    // =========================================================================
    // PART 3: CREATING THE NATIVE USER INTERFACE (UI)
    // =========================================================================
    // Unity 6 automatically invokes this method to render the UI layout whenever 
    // the window panel is opened or refreshed. It uses Unity's modern UI Toolkit (UITK).
    public void CreateGUI()
    {
        // 3a. Capture the master layout root surface of our dockable window window
        VisualElement root = rootVisualElement;

        // 3b. Construct and style our tool's main title header
        Label title = new Label("Project Broken Asset Scanner");
        title.style.fontSize = 18;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 10;
        title.style.marginTop = 10;
        root.Add(title);

        // 3c. Build our main "Scan" button and link its click event directly to our scan method
        Button scanButton = new Button(ScanForBrokenAssets);
        scanButton.text = "Scan Project Assets";
        scanButton.style.height = 35;
        scanButton.style.backgroundColor = new Color(0.2f, 0.4f, 0.2f); // Darker Green Theme
        root.Add(scanButton);

        // 3d. Add a ScrollView wrapper so if we find 100+ errors, they don't clip off the screen edge
        ScrollView scrollView = new ScrollView();
        scrollView.style.marginTop = 15;

        // 3e. Instantiate our clean results panel box and drop it inside the scrollable container
        _resultsContainer = new VisualElement();
        scrollView.Add(_resultsContainer);
        root.Add(scrollView);
    }

    // =========================================================================
    // PART 4: THE CORE SEARCH AND DATA CRUNCHING ENGINE
    // =========================================================================
    // This method handles the low-level hard drive scanning. It queries the engine databases,
    // loads data files into temporary processing memory, and reads individual script variable slots.
    private void ScanForBrokenAssets()
    {
        // 4a. Wipe out any old visual error rows from previous clicks so we start fresh
        _resultsContainer.Clear();

        // 4b. Request the AssetDatabase to run a global search for all files matching the type "Prefab"
        // This returns an array of absolute unique alphanumeric identifiers (GUIDs)
        string[] assetGuids = AssetDatabase.FindAssets("t:Prefab");

        // Track how many total broken components we uncover during the process loop
        int brokenCount = 0;

        // 4c. The Processing Loop: Evaluate every single prefab asset found in the project directory
        foreach (string guid in assetGuids)
        {
            // Turn the encrypted GUID code back into a readable file location path (e.g., "Assets/prefabs/Test.prefab")
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // Load the actual blueprint file off your hard drive disk into temporary processing memory
            GameObject targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            // Critical fallback safety check: If an asset file is corrupt or unreadable, ignore it and continue
            if (targetPrefab == null) continue;

            // Extract every component (Scripts, Rigidbody, Colliders) attached to this object or any children underneath it
            Component[] components = targetPrefab.GetComponentsInChildren<Component>(true);

            foreach (Component comp in components)
            {
                // CRITICAL VALIDATION 1: The Dead Script Component Check
                // If a script file was completely deleted from your folders but its old empty component 
                // container layout shell remains attached to the prefab asset, the component variable evaluates to null.
                if (comp == null)
                {
                    // FIXED: Passed targetPrefab instead of assetPath string
                    DisplayMissingReferenceResult(targetPrefab, "Missing/Missing Script Component attached!");
                    brokenCount++;
                    continue;
                }

                // CRITICAL VALIDATION 2: The Severed Reference Link Check
                // We wrap our active component inside a "SerializedObject". This invokes Unity's Reflection API, 
                // allowing our code to step inside private/public variables like a program inspector window.
                SerializedObject serializedObj = new SerializedObject(comp);
                SerializedProperty property = serializedObj.GetIterator();

                // Loop through every single variable property exposed within this script component
                while (property.NextVisible(true))
                {
                    // Target slots expecting an Object Reference (Textures, Materials, GameObjects) that currently hold null
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == null &&
                        property.hasMultipleDifferentValues == false)
                    {
                        // THE CRITICAL HOOK: A clean unassigned blank slot has an internal Instance ID tracking value of 0.
                        // But a slot that USED to hold a real link that got broken/deleted retains its old broken tracking ID value.
                        // This exact line ensures we bypass safe unassigned properties and target ONLY dangerous broken targets!
                        if (serializedObj.FindProperty(property.propertyPath).objectReferenceInstanceIDValue != 0)
                        {
                            // FIXED: Passed targetPrefab instead of assetPath string
                            DisplayMissingReferenceResult(targetPrefab, $"Property '{property.displayName}' has a MISSING reference link inside component [{comp.GetType().Name}]");
                            brokenCount++;
                        }
                    }
                }
            }
        }

        // 4d. Post-Scan Completion: If our evaluation loop found zero targets, notify the developer with a clean message
        if (brokenCount == 0)
        {
            Label cleanLabel = new Label(" Clear! No broken asset references detected in your project prefabs.");
            cleanLabel.style.color = new Color(0.4f, 0.9f, 0.4f); // Neon green text tint
            cleanLabel.style.marginTop = 10;
            _resultsContainer.Add(cleanLabel);
        }
    }

    // =========================================================================
    // PART 5: THE DYNAMIC UI ROW POPULATOR (WITH INTEGRATED AUTO-FIX HOOK)
    // =========================================================================
    // This helper method constructs an individual horizontal layout row box for each error found,
    // colors it dark red, drops the localized descriptive text, and attaches action buttons.
    private void DisplayMissingReferenceResult(GameObject brokenObject, string description)
    {
        // 5a. Build a row box container styled to layout its child objects horizontally side-by-side
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginBottom = 6;

        // 5b. Style padding bounds to nudge the elements inward cleanly from the background borders
        row.style.paddingTop = 4;
        row.style.paddingBottom = 4;
        row.style.paddingLeft = 6;
        row.style.paddingRight = 6;
        row.style.backgroundColor = new Color(0.25f, 0.2f, 0.2f); // Deep muted red warning row background

        // 5c. Print the specific problem description text
        Label descLabel = new Label($"[{brokenObject.name}] -> {description}");
        descLabel.style.flexGrow = 1; // FlexGrow = 1 forces the text to fill all remaining horizontal window width
        descLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        row.Add(descLabel);

        // 5d. ADVANCED UPGRADE: THE AUTO-FIX AUTOMATION ENGINE SHORTCUT
        // If the current issue being rendered is a missing script corpse, we append a special blue execution action button
        if (description.Contains("Script Component"))
        {
            Button fixButton = new Button(() =>
            {
                // Execute our structural cleaning method directly on this asset file
                FixMissingScriptsOnPrefab(brokenObject);

                // Auto-refresh the scanning engine right away so the fixed row immediately vanishes out of our window!
                ScanForBrokenAssets();
            });
            fixButton.text = "🔧 Clean Scripts";
            fixButton.style.width = 110;
            fixButton.style.backgroundColor = new Color(0.15f, 0.3f, 0.4f); // Blueprint Blue Tint
            fixButton.style.marginRight = 5;
            row.Add(fixButton);
        }

        // 5e. Build the native "Locate Asset" navigation utility button
        Button pingButton = new Button(() =>
        {
            // Call internal engine window managers to flash ("Ping") the asset within your Project folder tree
            EditorGUIUtility.PingObject(brokenObject);

            // Instantly highlight and set the Inspector focus right onto this specific object file
            Selection.activeObject = brokenObject;
        });
        pingButton.text = "Locate Asset";
        pingButton.style.width = 100;
        row.Add(pingButton);

        // 5f. Append our completely assembled structural data row box straight into the scroll container window
        _resultsContainer.Add(row);
    }

    // =========================================================================
    // PART 6: THE PIPELINE CLEANING AND FILE READ/WRITE UTILITY
    // =========================================================================
    // This utility executes when the "Clean Scripts" button is pushed. It unlocks the hard drive file structure,
    // modifies the prefab asset blueprint metadata directly, saves changes back down, and handles memory overhead cleanup.
    private void FixMissingScriptsOnPrefab(GameObject prefabAsset)
    {
        // 6a. Discover the literal address string on your computer for this asset game object
        string path = AssetDatabase.GetAssetPath(prefabAsset);

        // 6b. Check out and open the underlying structural data contents of that prefab file for asset writing
        GameObject contents = PrefabUtility.LoadPrefabContents(path);

        // 6c. Command Unity's low-level engine utility data structures to strip out every missing script object
        int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(contents);

        // 6d. If components were wiped out, save the cleanly restructured blueprint hierarchy back down onto the file disk
        if (removedCount > 0)
        {
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            Debug.Log($"[Auto-Fix Done] Successfully purged {removedCount} broken missing scripts from your file: {prefabAsset.name}");
        }

        // 6e. Flush processing overhead: Close and unload the modified data structures cleanly out of system RAM
        PrefabUtility.UnloadPrefabContents(contents);
    }
}