using System.Timers;
using DataCollectionWizard.Client.Models.DeviceTree;
using ViciOne.Ui.TreeEditor.Builder.Interface;

namespace DataCollectionWizard.Client.Services;

internal sealed partial class DeviceTreeAdapter
{
    private readonly List<NodeBase> _selected = [];
    private readonly System.Timers.Timer _selectionChangedTimer = new()
    {
        AutoReset = false,
        Enabled = false,
        Interval = 10,
    };

    private void OnSelectionChanged(ITreeNode node)
    {
        if (node is not NodeBase bNode)
            return;

        if (bNode.Selected && !_selected.Contains(bNode))
            _selected.Add(bNode);
        else if (!bNode.Selected)
            _selected.Remove(bNode);

        _selectionChangedTimer.Stop();
        _selectionChangedTimer.Start();
    }

    private void OnSelectionChangedTimerElapsed(object? _1, ElapsedEventArgs _2)
        => SelectionChanged?.Invoke();
}
