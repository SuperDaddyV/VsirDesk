using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace UniDesk;

public partial class WidgetCard : UserControl
{
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(WidgetCard),
            new PropertyMetadata(string.Empty));

    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    public static readonly DependencyProperty IsLockedProperty =
        DependencyProperty.Register(
            nameof(IsLocked),
            typeof(bool),
            typeof(WidgetCard),
            new PropertyMetadata(true, OnIsLockedChanged));

    private static void OnIsLockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WidgetCard card)
        {
            card.UpdateDragHandleVisibility();
        }
    }

    public bool IsDragging
    {
        get => (bool)GetValue(IsDraggingProperty);
        set => SetValue(IsDraggingProperty, value);
    }

    public static readonly DependencyProperty IsDraggingProperty =
        DependencyProperty.Register(
            nameof(IsDragging),
            typeof(bool),
            typeof(WidgetCard),
            new PropertyMetadata(false, OnIsDraggingChanged));

    private static void OnIsDraggingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WidgetCard card)
        {
            if ((bool)e.NewValue)
            {
                card.CardBorder.Opacity = 0.6;
            }
            else
            {
                card.CardBorder.Opacity = 1.0;
            }
        }
    }

    public WidgetCard()
    {
        InitializeComponent();
        UpdateDragHandleVisibility();
    }

    private void UpdateDragHandleVisibility()
    {
        if (IsLocked)
        {
            DragHandle.Visibility = Visibility.Collapsed;
            ResizeThumb.Visibility = Visibility.Collapsed;
        }
        else
        {
            DragHandle.Visibility = Visibility.Visible;
            ResizeThumb.Visibility = Visibility.Visible;
        }
    }

    private void OnLockButtonClick(object sender, RoutedEventArgs e)
    {
        IsLocked = !IsLocked;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsLocked || IsDragging)
        {
            return;
        }

        IsDragging = true;
        DragDrop.DoDragDrop(this, this, DragDropEffects.Move);
        IsDragging = false;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(WidgetCard)) is WidgetCard droppedCard && droppedCard != this)
        {
            var parent = this.Parent as ItemsControl;
            if (parent != null)
            {
                var items = parent.Items;
                var sourceIndex = items.IndexOf(droppedCard);
                var targetIndex = items.IndexOf(this);

                if (sourceIndex >= 0 && targetIndex >= 0 && sourceIndex != targetIndex)
                {
                    RaiseWidgetDroppedEvent(droppedCard, this, sourceIndex, targetIndex);
                }
            }
        }
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(WidgetCard)) is WidgetCard droppedCard && droppedCard != this)
        {
            CardBorder.Background = (Brush)FindResource("AccentBrush");
        }
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        CardBorder.Background = (Brush)FindResource("SecondaryBackgroundBrush");
    }

    private void OnResizeDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (IsLocked)
        {
            return;
        }

        var newHeight = Height + e.VerticalChange;
        if (newHeight >= 40 && newHeight <= 600)
        {
            Height = newHeight;
        }
    }

    public static readonly RoutedEvent WidgetDroppedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(WidgetDropped),
            RoutingStrategy.Bubble,
            typeof(EventHandler<WidgetDroppedEventArgs>),
            typeof(WidgetCard));

    public event EventHandler<WidgetDroppedEventArgs> WidgetDropped
    {
        add => AddHandler(WidgetDroppedEvent, value);
        remove => RemoveHandler(WidgetDroppedEvent, value);
    }

    private void RaiseWidgetDroppedEvent(WidgetCard source, WidgetCard target, int sourceIndex, int targetIndex)
    {
        var args = new WidgetDroppedEventArgs(WidgetDroppedEvent, source, target, sourceIndex, targetIndex);
        RaiseEvent(args);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape && IsDragging)
        {
            IsDragging = false;
            e.Handled = true;
        }
    }
}

public class WidgetDroppedEventArgs : RoutedEventArgs
{
    public WidgetCard SourceCard { get; }
    public WidgetCard TargetCard { get; }
    public int SourceIndex { get; }
    public int TargetIndex { get; }

    public WidgetDroppedEventArgs(RoutedEvent routedEvent, WidgetCard source, WidgetCard target, int sourceIndex, int targetIndex)
        : base(routedEvent)
    {
        SourceCard = source;
        TargetCard = target;
        SourceIndex = sourceIndex;
        TargetIndex = targetIndex;
    }
}