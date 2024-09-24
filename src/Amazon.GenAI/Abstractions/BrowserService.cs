using Microsoft.JSInterop;

namespace Amazon.GenAI.Abstractions;

public class BrowserService
{
    private IJSRuntime _js = null;
    public event EventHandler<int>? Resize;
    public async void Init(IJSRuntime js)
    {
        // enforce single invocation            
        if (_js != null) return;
        
        _js = js;
        await _js.InvokeAsync<string>("resizeListener", DotNetObjectReference.Create(this));
    }

    [JSInvokable]
    public void GetMainWidth(int jsMainWidth, int jsMainHeight)
    {
        this.Resize?.Invoke(this, jsMainWidth);
    }
}