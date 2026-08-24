using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DataCollectionWizard.Client.Components;

/// <summary>
/// The shared "count the figure up to its new value" helper - see wwwroot/js/count-up.js.
/// </summary>
/// <remarks>
/// Three components want it and none of them contains the others, so it cannot live beside any one of them as
/// collocated JS. The path and the call are named once here rather than spelled out in each.
/// </remarks>
internal static class CountUp
{
    /// <summary>
    /// Where the module is served from. Import it once per component, in the first render.
    /// </summary>
    public static string ModulePath { get; } =
        $"./_content/{typeof(CountUp).Assembly.GetName().Name}/js/count-up.js";

    /// <summary>
    /// Animates every <c>[data-counts]</c> element inside <paramref name="container"/> from what it currently
    /// shows to what the attribute now says.
    /// </summary>
    public static ValueTask AnimateAsync(IJSObjectReference? module, ElementReference container)
        => module is null ? ValueTask.CompletedTask : module.InvokeVoidAsync("animateCounts", container);
}
