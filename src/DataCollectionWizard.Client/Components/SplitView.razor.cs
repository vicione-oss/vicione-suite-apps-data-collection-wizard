using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

#pragma warning disable 414

namespace DataCollectionWizard.Client.Components;

public sealed partial class SplitView : ComponentBase
{
    private bool _isFirstRender = true;
    private int _leftSideContentWidth;
    private int _leftSideGhostContentWidth;
    private bool _mouseDown;

    [Parameter]
    public bool IsLive { get; set; } = true;
    [Parameter, EditorRequired]
    public RenderFragment LeftSideContent { get; set; } = default!;
    [Parameter]
    public int LeftSideContentWidth { get; set; }
    [Parameter]
    public uint LeftSideMinWidth { get; set; } = 25;
    [Parameter]
    public EventCallback OnPositionChanged { get; set; }
    [Parameter, EditorRequired]
    public RenderFragment RightSideContent { get; set; } = default!;
    [Parameter]
    public int SplitterWidth { get; set; } = 10;

    private void OnMouseDown(MouseEventArgs args)
        => _mouseDown = true;

    private void OnMouseMove(MouseEventArgs args)
    {
        if (args.Buttons == 0)
            OnMouseUp(args);

        if (_mouseDown)
        {
            var mouseX = Convert.ToInt32(args.ClientX);

            if (IsLive)
            {
                _leftSideContentWidth = mouseX < LeftSideMinWidth ? (int)LeftSideMinWidth : mouseX;
                InvokeAsync(StateHasChanged);
            }
            else
            {
                _leftSideGhostContentWidth = mouseX < LeftSideMinWidth ? (int)LeftSideMinWidth : mouseX;
            }
        }
    }

    private void OnMouseUp(MouseEventArgs args)
    {
        _mouseDown = false;

        if (!IsLive)
        {
            _leftSideContentWidth = _leftSideGhostContentWidth;
            InvokeAsync(StateHasChanged);
            OnPositionChanged.InvokeAsync();
        }
    }

    protected override void OnParametersSet()
    {
        if (_isFirstRender)
        {
            _leftSideContentWidth = LeftSideContentWidth;
            _leftSideGhostContentWidth = LeftSideContentWidth;
            _isFirstRender = false;
        }
    }
}
