using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePlayer : MonoBehaviour
{
    [Header("Data Asset")]
    public QuestGraphAsset dialogueGraph; // Drag your Act1_Dialogue file here!

    [Header("UI Canvas Elements")]
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueBodyText;
    public Transform choiceButtonContainer;
    public GameObject buttonPrefab;

    private QuestNodeData _currentNode;

    // Call this method from an interaction script or a UI button to start the conversation loop!
    public void StartDialogue()
    {
        if (dialogueGraph != null && dialogueGraph.allNodes.Count > 0)
        {
            // Start at the very first node in your visual tree layout
            DisplayNode(dialogueGraph.allNodes[0]);
        }
    }

    public void DisplayNode(QuestNodeData node)
    {
        _currentNode = node;

        // 1. Update the subtitle text elements
        // Extracts the speaker name out of the asset title formatting string wrapper
        string speakerName = node.name.Contains("(") ? node.name.Split('(', ')')[1] : "NPC";
        speakerNameText.text = speakerName;
        dialogueBodyText.text = node.dialogueText;

        // 2. Clear out old choice buttons from the previous dialogue step
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // 3. Generate interactive choice buttons for each outward branching wire path link
        foreach (NodeConnection connection in node.connections)
        {
            GameObject btnObj = Instantiate(buttonPrefab, choiceButtonContainer);

            // FIXED: Hunt for the TextMeshProUGUI component instead of the legacy Text component
            TextMeshProUGUI buttonText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = connection.choiceText; // e.g., "Branch Path Link"
            }

            // Target search hook: Look up the next data block matching the connection's string GUID address key
            QuestNodeData nextNode = dialogueGraph.allNodes.Find(n => n.nodeGuid == connection.targetNodeGuid);

            // Hook up the button click event listener to jump down the story map path array smoothly
            btnObj.GetComponent<Button>().onClick.AddListener(() => DisplayNode(nextNode));
        }
    }
}