using Amazon.GenAI.Abstractions.ChatHistory;
using Amazon.GenAI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Amazon.GenAI.Pages;

public class PageBase : ComponentBase
{
    [Inject]
    protected IJSRuntime? JsRuntime { get; set; }
    public event EventHandler<int>? Resize;

    protected readonly ChatMessageHistory MessageHistory = new();
    protected Status Status = Status.Default;

    protected string? BuiltinPrompt;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
       //     await JsRuntime!.InvokeAsync<string>("resizeListener", DotNetObjectReference.Create(this));
        }
    }

    [JSInvokable]
    public void GetMainWidth(int jsMainWidth, int jsMainHeight)
    {
       // this.Resize?.Invoke(this, jsMainWidth);
    }

    protected void UpdatedBrowserWidth(object sender, int width)
    {
        InvokeAsync(this.StateHasChanged);
    }

    protected async Task UpdatePage()
    {
        await InvokeAsync(() =>
        {
            Status = Status.Default;
            JsRuntime!.InvokeVoidAsync("scrollToElement", "chatMessages");
            StateHasChanged();
            return Task.CompletedTask;
        });
    }
}