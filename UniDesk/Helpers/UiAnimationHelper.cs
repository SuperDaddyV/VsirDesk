using System.Windows;
using System.Windows.Media.Animation;

namespace UniDesk.Helpers;

public static class UiAnimationHelper
{
    public const int DefaultDurationMs = 350;

    public static void AnimateDouble(
        DependencyObject target,
        DependencyProperty property,
        double toValue,
        int durationMs = DefaultDurationMs,
        Action? onCompleted = null)
    {
        var fromValue = (double)target.GetValue(property);
        if (Math.Abs(fromValue - toValue) < 0.5)
        {
            target.SetValue(property, toValue);
            onCompleted?.Invoke();
            return;
        }

        var animation = new DoubleAnimation(fromValue, toValue, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        if (onCompleted != null)
        {
            animation.Completed += (_, _) => onCompleted();
        }

        if (target is Animatable animatable)
        {
            animatable.BeginAnimation(property, animation);
        }
        else
        {
            target.SetValue(property, toValue);
            onCompleted?.Invoke();
        }
    }
}
