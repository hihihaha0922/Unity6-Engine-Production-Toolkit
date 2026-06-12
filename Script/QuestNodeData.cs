using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NodeConnection
{
    public string targetNodeGuid;
    public string choiceText;
}

public class QuestNodeData : ScriptableObject
{
    public string nodeGuid;
    public string nodeTitle = "New Dialogue";
    [TextArea(3, 5)] public string dialogueText = "Type dialogue lines here...";

    // Tracks where the box is visually positioned on your screen canvas grid
    public Vector2 canvasPosition;

    // List of lines connecting this node to other nodes
    public List<NodeConnection> connections = new List<NodeConnection>();
}