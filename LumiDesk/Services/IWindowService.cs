namespace LumiDesk.Services;

public interface IWindowService
{
    const double MinPanelWidth = 320;
    const double MaxPanelWidth = 520;
    const double CollapsedPanelWidth = 40;

    void SetTopMost(bool topMost);
    void ShowWindow();
    void HideWindow();
    void ToggleWindow();
    void SetWidth(double width);
    void AnimateWidth(double width, Action? onCompleted = null);
    void SetOpacity(double opacity);
    double GetCurrentWidth();
}
