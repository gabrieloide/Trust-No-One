using System.Collections.Generic;
using UnityEngine;

namespace VisualNovelSystem
{
    [CreateAssetMenu(fileName = "NewStoryGraph", menuName = "Visual Novel/Story Graph")]
    public class StoryGraph : ScriptableObject
    {
        [SerializeField]
        public string graphTitle = "Visual Novel Story";

        [SerializeField]
        public string entryNodeGuid = "";

        [SerializeField]
        public List<StoryNodeData> nodes = new List<StoryNodeData>();

        [SerializeField]
        public List<NodeLinkData> nodeLinks = new List<NodeLinkData>();

        [SerializeField]
        public StoryBlackboard blackboard = new StoryBlackboard();

        public StoryNodeData GetStartNode()
        {
            if (!string.IsNullOrEmpty(entryNodeGuid))
            {
                var startNode = nodes.Find(n => n.guid == entryNodeGuid);
                if (startNode != null) return startNode;
            }

            return nodes.Find(n => n.nodeType == StoryNodeType.Start);
        }

        public StoryNodeData GetNodeByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            return nodes.Find(n => n.guid == guid);
        }

        public string GetNextNodeGuid(string currentGuid, string portIdentifier = "output")
        {
            var link = nodeLinks.Find(l => l.baseNodeGuid == currentGuid && l.portIdentifier == portIdentifier);
            return link != null ? link.targetNodeGuid : null;
        }

        public List<NodeLinkData> GetOutgoingLinks(string nodeGuid)
        {
            return nodeLinks.FindAll(l => l.baseNodeGuid == nodeGuid);
        }
    }
}
