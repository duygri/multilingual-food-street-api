namespace FoodStreet.Client.DTOs
{
    public class MobileNativeMapResult
    {
        public bool Confirmed { get; set; }
        public double? SelectedLatitude { get; set; }
        public double? SelectedLongitude { get; set; }
        public string? SelectedLabel { get; set; }
    }
}
