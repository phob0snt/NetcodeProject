using System.Collections.Generic;

namespace Unity.Multiplayer.Tools.NetworkProfiler.Editor
{
    internal interface ITreeViewItem
    {
        int id { get; }

        ITreeViewItem parent { get; }

        IEnumerable<ITreeViewItem> children { get; }

        bool hasChildren { get; }
    }
}