namespace LinnetServer.Services;

public class ChannelGroupsState
{
    public event Action? OnChanged;

    public void NotifyChanged() => OnChanged?.Invoke();
}
