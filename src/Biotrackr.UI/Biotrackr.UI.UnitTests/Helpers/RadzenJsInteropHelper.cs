using Bunit;
using Radzen.Blazor.Rendering;

namespace Biotrackr.UI.UnitTests.Helpers;

public static class RadzenJsInteropHelper
{
    /// <summary>
    /// Sets up bUnit JSInterop mocks for Radzen Blazor chart and gauge components.
    /// RadzenChart calls <c>InvokeAsync&lt;Rect&gt;("Radzen.createChart")</c> and
    /// GaugeBase calls <c>InvokeAsync&lt;Rect&gt;("Radzen.createGauge")</c> during
    /// OnAfterRenderAsync. Since Rect is a reference type, Loose mode returns null,
    /// causing NRE when Radzen accesses rect.Width/Height. This sets up planned
    /// invocations that return non-null Rect instances.
    /// </summary>
    public static void SetupRadzenChartInterop(this BunitJSInterop jsInterop)
    {
        var defaultRect = new Rect { Width = 300, Height = 300 };

        jsInterop.Setup<Rect>("Radzen.createChart", _ => true).SetResult(defaultRect);
        jsInterop.Setup<Rect>("Radzen.createGauge", _ => true).SetResult(defaultRect);
        jsInterop.Setup<Rect>("Radzen.createResizable", _ => true).SetResult(defaultRect);
    }
}
