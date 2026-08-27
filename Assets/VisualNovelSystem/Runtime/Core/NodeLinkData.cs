using System;
using UnityEngine;

namespace VisualNovelSystem
{
    [Serializable]
    public class NodeLinkData
    {
        public string baseNodeGuid;
        public string portIdentifier; // e.g. "output", "choice_0", "choice_1", "true", "false"
        public string targetNodeGuid;

        public NodeLinkData() { }

        public NodeLinkData(string baseGuid, string portId, string targetGuid)
        {
            baseNodeGuid = baseGuid;
            portIdentifier = portId;
            targetNodeGuid = targetGuid;
        }
    }
}
