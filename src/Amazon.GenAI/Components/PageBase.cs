using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Amazon.GenAI.Components;

public class PageBase : ComponentBase
{
    [Inject]
    protected IJSRuntime? JsRuntime { get; set; }
    public event EventHandler<int>? Resize;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime!.InvokeAsync<string>("resizeListener", DotNetObjectReference.Create(this));
        }
    }

    [JSInvokable]
    public void GetMainWidth(int jsMainWidth, int jsMainHeight)
    {
        this.Resize?.Invoke(this, jsMainWidth);
    }

    protected void UpdatedBrowserWidth(object sender, int width)
    {
        InvokeAsync(this.StateHasChanged);
    }
}