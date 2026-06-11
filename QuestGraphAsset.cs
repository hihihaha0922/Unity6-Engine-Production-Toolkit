using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestGraph", menuName = "Larian Tools/Quest Graph Asset")]
public class QuestGraphAsset : ScriptableObject
{
    // Holds every node belonging to this specific dialogue tree
    public List<QuestNodeData> allNodes = new List<QuestNodeData>();
}