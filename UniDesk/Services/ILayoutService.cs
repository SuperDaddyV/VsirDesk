using UniDesk.Models;

namespace UniDesk.Services;

public interface ILayoutService
{
    string GetDefaultLayout();
    string SerializeLayout(List<WidgetLayout> layout);
    List<WidgetLayout>? DeserializeLayout(string json);
    void SaveLayout(string layout);
    string? LoadLayout();
    void ResetToDefault();
    List<WidgetLayout> LoadOrGetDefault();
}
