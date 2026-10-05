using ERP.App.Services;
using Microsoft.AspNetCore.Components;

namespace ERP.App.Components;

public abstract class AuthAwareComponentBase : ComponentBase, IDisposable
{
    [Inject]
    protected AppSessionService AppSession { get; set; } = null!;

    protected bool IsAuthenticated => AppSession.IsAuthenticated;

    protected override void OnInitialized()
    {
        AppSession.AuthStateChanged += OnAuthStateChanged;
    }

    private void OnAuthStateChanged() => InvokeAsync(StateHasChanged);

    public virtual void Dispose()
    {
        AppSession.AuthStateChanged -= OnAuthStateChanged;
        GC.SuppressFinalize(this);
    }
}
