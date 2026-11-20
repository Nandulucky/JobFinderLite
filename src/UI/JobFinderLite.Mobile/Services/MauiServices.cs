using JobFinderLite.Application.Interfaces;

namespace JobFinderLite.Mobile.Services;

public class MauiShellNavigationService : INavigationService
{
    public async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (parameters == null || parameters.Count == 0)
        {
            await Shell.Current.GoToAsync(route);
        }
        else
        {
            await Shell.Current.GoToAsync(route, parameters);
        }
    }

    public async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}

public class MauiDialogService : IDialogService
{
    public async Task DisplayAlertAsync(string title, string message, string cancel)
    {
        if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlertAsync(title, message, cancel);
        }
    }

    public async Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel)
    {
        if (Shell.Current != null)
        {
            return await Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
        }
        return false;
    }
}

public class MauiDispatcherService : IDispatcherService
{
    public Task DispatchAsync(Action action)
    {
        if (MainThread.IsMainThread)
        {
            action();
            return Task.CompletedTask;
        }
        return MainThread.InvokeOnMainThreadAsync(action);
    }

    public Task<T> DispatchAsync<T>(Func<T> func)
    {
        if (MainThread.IsMainThread)
        {
            return Task.FromResult(func());
        }
        return MainThread.InvokeOnMainThreadAsync(func);
    }
}
