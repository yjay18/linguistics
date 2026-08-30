using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Linguistics.App.Controls;

public enum PaperStageLayer
{
    Backdrop,
    PaperWash,
    SupportingCast,
    AmbientPieces,
    TapedLabel,
    ForegroundSilhouettes,
    Subject,
    ReactionBurst,
    VerdictCard,
}

public enum PaperAnchorLine
{
    None,
    Head,
    Shoulder,
    Waist,
    Foot,
}

public sealed class PaperStage : Panel
{
    public static readonly AttachedProperty<PaperStageLayer> LayerProperty =
        AvaloniaProperty.RegisterAttached<PaperStage, Control, PaperStageLayer>(
            "Layer",
            PaperStageLayer.Backdrop);

    public static readonly AttachedProperty<PaperAnchorLine> AnchorProperty =
        AvaloniaProperty.RegisterAttached<PaperStage, Control, PaperAnchorLine>(
            "Anchor",
            PaperAnchorLine.None);

    public static readonly AttachedProperty<double> AnchorXProperty =
        AvaloniaProperty.RegisterAttached<PaperStage, Control, double>(
            "AnchorX",
            0.5);

    public static readonly AttachedProperty<double> AnchorOffsetXProperty =
        AvaloniaProperty.RegisterAttached<PaperStage, Control, double>(
            "AnchorOffsetX");

    public static readonly AttachedProperty<double> AnchorOffsetYProperty =
        AvaloniaProperty.RegisterAttached<PaperStage, Control, double>(
            "AnchorOffsetY");

    public static readonly AttachedProperty<ITransform?> LayerTransformProperty =
        AvaloniaProperty.RegisterAttached<PaperStage, Control, ITransform?>(
            "LayerTransform");

    static PaperStage()
    {
        LayerProperty.Changed.AddClassHandler<Control>((control, _) => InvalidateParentStage(control));
        AnchorProperty.Changed.AddClassHandler<Control>((control, _) => InvalidateParentStage(control));
        AnchorXProperty.Changed.AddClassHandler<Control>((control, _) => InvalidateParentStage(control));
        AnchorOffsetXProperty.Changed.AddClassHandler<Control>((control, _) => InvalidateParentStage(control));
        AnchorOffsetYProperty.Changed.AddClassHandler<Control>((control, _) => InvalidateParentStage(control));
        LayerTransformProperty.Changed.AddClassHandler<Control>((control, _) =>
        {
            control.RenderTransform = GetLayerTransform(control);
            InvalidateParentStage(control);
        });
    }

    public static PaperStageLayer GetLayer(Control control) => control.GetValue(LayerProperty);

    public static void SetLayer(Control control, PaperStageLayer value) =>
        control.SetValue(LayerProperty, value);

    public static PaperAnchorLine GetAnchor(Control control) => control.GetValue(AnchorProperty);

    public static void SetAnchor(Control control, PaperAnchorLine value) =>
        control.SetValue(AnchorProperty, value);

    public static double GetAnchorX(Control control) => control.GetValue(AnchorXProperty);

    public static void SetAnchorX(Control control, double value) =>
        control.SetValue(AnchorXProperty, value);

    public static double GetAnchorOffsetX(Control control) => control.GetValue(AnchorOffsetXProperty);

    public static void SetAnchorOffsetX(Control control, double value) =>
        control.SetValue(AnchorOffsetXProperty, value);

    public static double GetAnchorOffsetY(Control control) => control.GetValue(AnchorOffsetYProperty);

    public static void SetAnchorOffsetY(Control control, double value) =>
        control.SetValue(AnchorOffsetYProperty, value);

    public static ITransform? GetLayerTransform(Control control) =>
        control.GetValue(LayerTransformProperty);

    public static void SetLayerTransform(Control control, ITransform? value) =>
        control.SetValue(LayerTransformProperty, value);

    public static int GetLayerZIndex(PaperStageLayer layer) => (int)layer;

    public static double GetAnchorRatio(PaperAnchorLine anchor) => anchor switch
    {
        PaperAnchorLine.Head => 0.18,
        PaperAnchorLine.Shoulder => 0.34,
        PaperAnchorLine.Waist => 0.58,
        PaperAnchorLine.Foot => 0.88,
        _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "Choose a named anchor line."),
    };

    internal static Rect CalculateAnchoredBounds(
        Size stageSize,
        Size childSize,
        PaperAnchorLine anchor,
        double anchorX,
        Vector offset)
    {
        if (anchor == PaperAnchorLine.None)
        {
            return new Rect(stageSize);
        }

        var contentAnchorRatio = anchor switch
        {
            PaperAnchorLine.Head => 0,
            PaperAnchorLine.Shoulder => 0.25,
            PaperAnchorLine.Waist => 0.58,
            PaperAnchorLine.Foot => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null),
        };
        var x = Math.Clamp(anchorX, 0, 1) * stageSize.Width - (childSize.Width / 2) + offset.X;
        var y = GetAnchorRatio(anchor) * stageSize.Height -
                (contentAnchorRatio * childSize.Height) +
                offset.Y;
        return new Rect(new Point(x, y), childSize);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desiredWidth = 0d;
        var desiredHeight = 0d;
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width);
            desiredHeight = Math.Max(desiredHeight, child.DesiredSize.Height);
        }

        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            child.ZIndex = GetLayerZIndex(GetLayer(child));
            child.RenderTransform = GetLayerTransform(child);

            var anchor = GetAnchor(child);
            if (anchor == PaperAnchorLine.None)
            {
                child.Arrange(new Rect(finalSize));
                continue;
            }

            var childSize = new Size(
                Math.Min(child.DesiredSize.Width, finalSize.Width),
                Math.Min(child.DesiredSize.Height, finalSize.Height));
            child.Arrange(CalculateAnchoredBounds(
                finalSize,
                childSize,
                anchor,
                GetAnchorX(child),
                new Vector(GetAnchorOffsetX(child), GetAnchorOffsetY(child))));
        }

        return finalSize;
    }

    private static void InvalidateParentStage(Control control) =>
        (control.Parent as PaperStage)?.InvalidateArrange();
}
