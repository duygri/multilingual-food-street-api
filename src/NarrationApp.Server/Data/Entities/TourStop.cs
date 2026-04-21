namespace NarrationApp.Server.Data.Entities;

public sealed class TourStop
{
    public int Id { get; set; }

    public int TourId { get; set; }

    public int PoiId { get; set; }

    public int Sequence { get; set; }

    public int RadiusMeters { get; set; }

    public Tour? Tour { get; set; }

    public Poi? Poi { get; set; }
}
