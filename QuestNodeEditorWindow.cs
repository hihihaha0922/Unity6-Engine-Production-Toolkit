using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

public class QuestNodeEditorWindow : EditorWindow
{
    private VisualElement _canvas;
    private List<VisualElement> _visualNodes = new List<VisualElement>();
    private VisualElement _linkingSourceNode = null;

    // PERSISTENT STORAGE TRACKING
    private QuestGraphAsset _activeGraphAsset;
    private ObjectField _assetTargetSelectorField;

    [MenuItem("Larian Tools/Quest & Dialogue Editor")]
    public static void ShowWindow()
    {
        QuestNodeEditorWindow wnd = GetWindow<QuestNodeEditorWindow>();
        wnd.titleContent = new GUIContent("Quest Editor");
        wnd.minSize = new Vector2(900, 650);
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        _visualNodes.Clear();
        _linkingSourceNode = null;

        // 1. TOP CONTROL TOOLBAR
        VisualElement toolbar = new VisualElement();
        toolbar.style.height = 35;
        toolbar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        toolbar.style.flexDirection = FlexDirection.Row;
        toolbar.style.paddingTop = 4;
        toolbar.style.paddingBottom = 4;

        Button addNodeBtn = new Button(() => CreateNodeVisual(new Vector2(150, 150), "Node_" + _canvas.childCount, "Narrator", "Type line..."))
        {
            text = "➕ Add Dialogue Node"
        };
        toolbar.Add(addNodeBtn);

        // Active Save File Database Field Selector Slot
        _assetTargetSelectorField = new ObjectField("Active Graph File:") { objectType = typeof(QuestGraphAsset) };
        _assetTargetSelectorField.style.width = 300;
        _assetTargetSelectorField.style.marginLeft = 20;
        _assetTargetSelectorField.RegisterValueChangedCallback(evt => {
            _activeGraphAsset = evt.newValue as QuestGraphAsset;
        });
        toolbar.Add(_assetTargetSelectorField);

        Button saveBtn = new Button(SaveGraphToDataAsset) { text = "💾 Save Asset" };
        saveBtn.style.backgroundColor = new Color(0.15f, 0.35f, 0.15f);
        toolbar.Add(saveBtn);

        Button loadBtn = new Button(LoadGraphFromDataAsset) { text = "🔄 Load Asset" };
        toolbar.Add(loadBtn);

        root.Add(toolbar);

        // 2. VIEWPORT CANVAS
        _canvas = new VisualElement();
        _canvas.style.flexGrow = 1;
        _canvas.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        _canvas.generateVisualContent += DrawConnectionWires;

        _canvas.RegisterCallback<ContextClickEvent>(evt => {
            CreateNodeVisual(evt.localMousePosition, "Node_" + _canvas.childCount, "NPC", "");
        });

        root.Add(_canvas);
    }

    // UPDATED: Generates a much longer, taller, and feature-rich node canvas box card
    private VisualElement CreateNodeVisual(Vector2 position, string id, string speaker, string dialogue)
    {
        VisualElement nodeBox = new VisualElement();
        nodeBox.style.position = Position.Absolute;
        nodeBox.style.left = position.x;
        nodeBox.style.top = position.y;

        // FIX: Expanded width and size profile to prevent clipping text strings
        nodeBox.style.width = 280;
        nodeBox.style.backgroundColor = new Color(0.23f, 0.23f, 0.23f);

        nodeBox.style.borderTopWidth = 1; nodeBox.style.borderBottomWidth = 1;
        nodeBox.style.borderLeftWidth = 1; nodeBox.style.borderRightWidth = 1;
        nodeBox.style.borderTopColor = Color.gray; nodeBox.style.borderBottomColor = Color.gray;
        nodeBox.style.borderLeftColor = Color.gray; nodeBox.style.borderRightColor = Color.gray;
        nodeBox.style.borderTopLeftRadius = 6; nodeBox.style.borderTopRightRadius = 6;
        nodeBox.style.borderBottomLeftRadius = 6; nodeBox.style.borderBottomRightRadius = 6;

        // Title Header Banner
        Label nodeHeader = new Label("Dialogue Node Beat");
        nodeHeader.style.backgroundColor = new Color(0.12f, 0.33f, 0.53f);
        nodeHeader.style.paddingTop = 5; nodeHeader.style.paddingBottom = 5;
        nodeHeader.style.paddingLeft = 6; nodeHeader.style.paddingRight = 6;
        nodeHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        nodeBox.Add(nodeHeader);

        // FIELD 1: Unique Identification Handle Address Key
        TextField titleField = new TextField("Node ID: ") { value = id };
        titleField.name = "node-id-field";
        titleField.style.paddingTop = 3; titleField.style.paddingBottom = 3;
        nodeBox.Add(titleField);

        // FIELD 2: NEW SPEAKER ATTRIBUTION DROP FIELD CONTROL
        TextField speakerField = new TextField("Speaker Name: ") { value = speaker };
        speakerField.name = "speaker-field";
        speakerField.style.paddingTop = 3; speakerField.style.paddingBottom = 3;
        // Visual polish tint for the speaker identity label slot layout
        speakerField.Q("unity-text-input").style.color = new Color(0.9f, 0.7f, 0.2f);
        nodeBox.Add(speakerField);

        // FIELD 3: Dialogue Sentence Block Main Text Box Area
        TextField textField = new TextField("Dialogue Text: ") { value = dialogue, multiline = true };
        textField.name = "dialogue-field";
        textField.style.height = 80; // Taller row field block tracking profile dimensions

        // Enable complete word wrapping formatting
        textField.style.whiteSpace = WhiteSpace.Normal;
        VisualElement internalInputBox = textField.Q("unity-text-input");
        if (internalInputBox != null) internalInputBox.style.whiteSpace = WhiteSpace.Normal;

        textField.style.paddingTop = 3; textField.style.paddingBottom = 3;
        nodeBox.Add(textField);

        // FIELD 4: Flow Routing Wire Connector Interactive Button Component
        Button linkBtn = new Button() { text = "🔗 Link to Next Choice Node" };
        linkBtn.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f);
        linkBtn.style.marginTop = 6;
        linkBtn.style.height = 24;

        if (nodeBox.userData == null)
        {
            nodeBox.userData = new List<VisualElement>();
        }

        linkBtn.clicked += () => {
            if (_linkingSourceNode == null)
            {
                _linkingSourceNode = nodeBox;
                linkBtn.text = "🟡 Select Branch Target Node...";
                linkBtn.style.backgroundColor = new Color(0.45f, 0.35f, 0.1f);
            }
            else if (_linkingSourceNode == nodeBox)
            {
                CancelLinkingMode(linkBtn);
            }
            else
            {
                List<VisualElement> links = _linkingSourceNode.userData as List<VisualElement>;
                if (!links.Contains(nodeBox)) links.Add(nodeBox);

                _linkingSourceNode = null;
                _canvas.MarkDirtyRepaint();
                ResetAllLinkButtonLabels();
            }
        };
        nodeBox.Add(linkBtn);

        // ==========================================
        // NEW FEATURE: RIGHT-CLICK TO DELETE NODE
        // ==========================================
        nodeBox.RegisterCallback<ContextClickEvent>(evt => {
            // Create a native Unity dropdown context menu popup
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("❌ Delete Node"), false, () =>
            {
                // 1. Remove this node from the master tracking window list array
                _visualNodes.Remove(nodeBox);

                // 2. Clear out any wire line linkages pointing to this node from other boxes
                foreach (VisualElement otherNode in _visualNodes)
                {
                    List<VisualElement> targets = otherNode.userData as List<VisualElement>;
                    if (targets != null && targets.Contains(nodeBox))
                    {
                        targets.Remove(nodeBox);
                    }
                }

                // 3. Remove the physical box graphic card element from the editor layout canvas view
                _canvas.Remove(nodeBox);

                // 4. Trigger an immediate repaint frame call to redraw vector connection paths cleanly
                _canvas.MarkDirtyRepaint();
            });

            menu.ShowAsContext();
            evt.StopPropagation(); // Prevents spawning the generic blank canvas menu instead
        });
        // ==========================================

        MakeNodeDraggable(nodeBox);
        _visualNodes.Add(nodeBox);
        _canvas.Add(nodeBox);

        return nodeBox;
    }

    private void CancelLinkingMode(Button btn)
    {
        _linkingSourceNode = null;
        btn.text = "🔗 Link to Next Choice Node";
        btn.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f);
    }

    private void ResetAllLinkButtonLabels()
    {
        foreach (var node in _visualNodes)
        {
            Button btn = node.Q<Button>();
            if (btn != null && _linkingSourceNode == null)
            {
                btn.text = "🔗 Link to Next Choice Node";
                btn.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f);
            }
        }
    }

    // ==========================================
    // NEW PIPELINE: PERSISTENT STORAGE WRITEBACK (SAVE)
    // ==========================================
    private void SaveGraphToDataAsset()
    {
        if (_activeGraphAsset == null)
        {
            EditorUtility.DisplayDialog("Missing File Target", "Please right-click in your project window, create a 'Larian Tools -> Quest Graph Asset' file, and drag it into the selector header slot before hitting Save!", "Understood");
            return;
        }

        // Clean out stale values inside data array arrays from previous saves
        _activeGraphAsset.allNodes.Clear();

        // Dictionary mapper pathway tracks guid associations cleanly
        Dictionary<VisualElement, QuestNodeData> dataMap = new Dictionary<VisualElement, QuestNodeData>();

        // Phase 1: Compile all baseline visual block structures down into individual file assets
        foreach (VisualElement visualNode in _visualNodes)
        {
            QuestNodeData nodeData = ScriptableObject.CreateInstance<QuestNodeData>();
            nodeData.nodeGuid = System.Guid.NewGuid().ToString(); // Generate unique system string address key
            nodeData.nodeTitle = visualNode.Q<TextField>("node-id-field").value;
            nodeData.dialogueText = visualNode.Q<TextField>("dialogue-field").value;

            // Injecting speaker value metadata explicitly down into persistent asset structures
            nodeData.name = nodeData.nodeTitle + " (" + visualNode.Q<TextField>("speaker-field").value + ")";

            nodeData.canvasPosition = new Vector2(visualNode.style.left.value.value, visualNode.style.top.value.value);

            // Append instance asset objects natively into the Master tracking container structural tree file configuration
            _activeGraphAsset.allNodes.Add(nodeData);
            AssetDatabase.AddObjectToAsset(nodeData, _activeGraphAsset);

            dataMap.Add(visualNode, nodeData);
        }

        // Phase 2: Resolve wiring relationship connections arrays 
        foreach (VisualElement visualNode in _visualNodes)
        {
            QuestNodeData sourceData = dataMap[visualNode];
            List<VisualElement> linkedTargets = visualNode.userData as List<VisualElement>;

            if (linkedTargets != null)
            {
                foreach (VisualElement targetVisual in linkedTargets)
                {
                    if (dataMap.TryGetValue(targetVisual, out QuestNodeData targetData))
                    {
                        NodeConnection conn = new NodeConnection();
                        conn.targetNodeGuid = targetData.nodeGuid;
                        conn.choiceText = "Branch Path Link";
                        sourceData.connections.Add(conn);
                    }
                }
            }
        }

        EditorUtility.SetDirty(_activeGraphAsset);
        AssetDatabase.SaveAssets();
        Debug.Log("[Quest Engine] Successfully serialized and locked complete dialogue branching graph web permanently onto hard disk sectors!");
    }

    // ==========================================
    // NEW PIPELINE: PERSISTENT HARD DRIVE HARVESTING (LOAD)
    // ==========================================
    private void LoadGraphFromDataAsset()
    {
        if (_activeGraphAsset == null) return;

        // Clear existing visual objects out of current layout viewport workspace active spaces
        _canvas.Clear();
        _visualNodes.Clear();

        Dictionary<string, VisualElement> guidToVisualMap = new Dictionary<string, VisualElement>();
        Dictionary<VisualElement, QuestNodeData> visualToDataMap = new Dictionary<VisualElement, QuestNodeData>();

        // Step 1: Respawn all block panels onto their cached coordinate markers
        foreach (QuestNodeData nodeData in _activeGraphAsset.allNodes)
        {
            if (nodeData == null) continue;

            // Extract values out of the data asset parameters to fill inputs fields
            string speakerName = nodeData.name.Contains("(") ? nodeData.name.Split('(', ')')[1] : "Unknown";

            VisualElement visualNode = CreateNodeVisual(nodeData.canvasPosition, nodeData.nodeTitle, speakerName, nodeData.dialogueText);

            guidToVisualMap.Add(nodeData.nodeGuid, visualNode);
            visualToDataMap.Add(visualNode, nodeData);
        }

        // Step 2: Redraw vector line curve tracking paths exactly as they were linked previously
        foreach (var pair in visualToDataMap)
        {
            VisualElement visualNode = pair.Key;
            QuestNodeData nodeData = pair.Value;
            List<VisualElement> connectionWireList = visualNode.userData as List<VisualElement>;

            foreach (NodeConnection connectionData in nodeData.connections)
            {
                if (guidToVisualMap.TryGetValue(connectionData.targetNodeGuid, out VisualElement targetVisualElement))
                {
                    if (!connectionWireList.Contains(targetVisualElement))
                    {
                        connectionWireList.Add(targetVisualElement);
                    }
                }
            }
        }

        _canvas.MarkDirtyRepaint();
        Debug.Log("[Quest Engine] Successfully compiled, extracted, and rendered saved node layout diagrams from disk data storage asset!");
    }

    private void DrawConnectionWires(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;
        painter.strokeColor = new Color(0.12f, 0.53f, 0.85f, 0.8f);
        painter.lineWidth = 3f;

        foreach (VisualElement node in _visualNodes)
        {
            List<VisualElement> targets = node.userData as List<VisualElement>;
            if (targets == null) continue;

            foreach (VisualElement target in targets)
            {
                Vector2 startPos = new Vector2(node.style.left.value.value + 140, node.style.top.value.value + 165);
                Vector2 endPos = new Vector2(target.style.left.value.value + 140, target.style.top.value.value);

                float startTangentX = startPos.x;
                float startTangentY = startPos.y + 60;
                float endTangentX = endPos.x;
                float endTangentY = endPos.y - 60;

                painter.BeginPath();
                painter.MoveTo(startPos);
                painter.BezierCurveTo(new Vector2(startTangentX, startTangentY), new Vector2(endTangentX, endTangentY), endPos);
                painter.Stroke();
            }
        }
    }

    private void MakeNodeDraggable(VisualElement node)
    {
        var pointerDown = false;
        Vector2 pointerStartPos = Vector2.zero;
        Vector2 nodeStartPos = Vector2.zero;

        node.RegisterCallback<PointerDownEvent>(evt => {
            if (evt.button == 0)
            {
                pointerDown = true;
                pointerStartPos = evt.position;
                nodeStartPos = new Vector2(node.style.left.value.value, node.style.top.value.value);
                node.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            }
        });

        node.RegisterCallback<PointerMoveEvent>(evt => {
            if (pointerDown && node.HasPointerCapture(evt.pointerId))
            {
                Vector2 pointerDelta = (Vector2)evt.position - pointerStartPos;
                node.style.left = nodeStartPos.x + pointerDelta.x;
                node.style.top = nodeStartPos.y + pointerDelta.y;
                _canvas.MarkDirtyRepaint();
                evt.StopPropagation();
            }
        });

        node.RegisterCallback<PointerUpEvent>(evt => {
            if (pointerDown && node.HasPointerCapture(evt.pointerId))
            {
                pointerDown = false;
                node.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            }
        });
    }
}