namespace DevBar;

public enum PositionStrategy
{
    HugLeft,
    HugAppsCluster,
    CenteredInDeadspace,
}

public static class OverlayPositioner
{
    public static (double Left, double Top) Compute(
        RECT taskbarRect,
        double overlayWidth,
        double overlayHeight,
        double dpiScale,
        PositionStrategy strategy)
    {
        if (dpiScale <= 0) dpiScale = 1.0;

        var tbLeft = taskbarRect.Left / dpiScale;
        var tbTop = taskbarRect.Top / dpiScale;
        var tbWidth = taskbarRect.Width / dpiScale;
        var tbHeight = taskbarRect.Height / dpiScale;

        return strategy switch
        {
            PositionStrategy.HugAppsCluster => HugLeft(tbLeft, tbTop, tbHeight, overlayHeight),
            PositionStrategy.CenteredInDeadspace => HugLeft(tbLeft, tbTop, tbHeight, overlayHeight),
            _ => HugLeft(tbLeft, tbTop, tbHeight, overlayHeight),
        };
    }

    private static (double Left, double Top) HugLeft(
        double tbLeft, double tbTop, double tbHeight, double overlayHeight)
    {
        const double leftPadding = 8;
        var left = tbLeft + leftPadding;
        var top = tbTop + (tbHeight - overlayHeight) / 2;
        return (left, top);
    }
}
