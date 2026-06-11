# Unity 6 Engine Pipeline & Production Toolkit 🚀

A native, dockable studio editor suite built entirely from scratch in **Unity 6 (C# / .NET)** utilizing modern **UI Toolkit (UITK)** workflows. This project optimizes asset delivery bottlenecks, streamlines character balance adjustments, and empowers narrative designers via visual automation pipelines.

---

## 🛠️ The Workflow Automation Tools

### 1. Asset Integrity & Validation Scanner
Designed to eliminate production runtime exceptions by auditing underlying file architecture safely before scene loading.

* **The Workflow Fix:** Prevents `NullReferenceException` loops by scanning uninstantiated prefab parameters for unlinked dependencies or missing script components.
* **Technical Implementation:** Utilizes **C# Reflection** to read class metadata safely alongside Unity's **`SerializedObject`** API data cache streams. Features custom instance ID hashing to eliminate false-positive diagnostics and includes a one-click automated purge via **`PrefabUtility`** to erase corrupted metadata configs.


https://github.com/user-attachments/assets/506b517f-9319-4d27-9a7e-6d3a83669a87


---

### 2. Centralized Character Stat Balancer
An internal spreadsheet-style dashboard allowing designers to track, isolate, and bulk-update character data matrices without moving into external database sheets.

* **The Workflow Fix:** Eliminates human input error and reduces time spent context-switching between Unity and Excel.
* **Technical Implementation:** Uses structural C# class inheritance to dynamically aggregate active project dataset parameters into a single view model. Powered by custom UI Toolkit sorting blocks, live search strings, and quick faction dropdown enum masks. Writes modifications directly using `EditorUtility.SetDirty`.


https://github.com/user-attachments/assets/c509459c-ec1a-46fe-a2e7-d3d31088300e

---

### 3. Branching Quest & Dialogue Node Editor
An interactive node graph canvas built for non-technical writers to stitch together complex, branching RPG narrative layers.

* **The Workflow Fix:** Moves storytelling from dense text scripts into a spatial visual diagram layout, complete with an immediate execution test player framework.
* **Technical Implementation:** Features a dedicated architecture split:
  1. **Editor Presentation:** Renders a clean visual node network via UI Toolkit vector drawing callbacks to construct real-time bezier connection links.
  2. **Data Serialization:** Translates node wire paths into automated structural array fields mapped to custom `ScriptableObject` assets saved natively on the hard drive. Includes character name text isolation algorithms and an injection player loop.



https://github.com/user-attachments/assets/fa8d751b-3990-4280-8c9f-03385bca4729





## 💾 Core Systems & Architecture Overview

```csharp
// Example Architecture: Data abstraction between UITK Editor Views and Disk Serialization
public class DialoguePlayer : MonoBehaviour 
{
    // The engine reads lightweight serialized structures without needing editor canvas layouts loaded
    public QuestGraphAsset dialogueGraph; 
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueBodyText;

    public void DisplayNode(QuestNodeData node) 
    {
        // Programmatic dynamic evaluation loop used by runtime systems
        string speakerName = node.name.Contains("(") ? node.name.Split('(', ')')[1] : "NPC";
        speakerNameText.text = speakerName;
        dialogueBodyText.text = node.dialogueText;
    }
}

---

## ⚙️ Development Environment
* **Engine:** Unity 6 LTS
* **Language/Framework:** C# / .NET 8 / UI Toolkit (UITK)
* **Architecture Style:** Data-Driven Tool Design, Serialization Buffer Isolation
