namespace NarrationApp.Mobile.Features.Home;

public sealed record VisitorTourStopItem(
    int Sequence,
    string PoiId,
    string PoiName,
    string Summary,
    string StateLabel,
    string DistanceLabel,
    bool IsCompleted,
    bool IsCurrent,
    bool IsNext);
