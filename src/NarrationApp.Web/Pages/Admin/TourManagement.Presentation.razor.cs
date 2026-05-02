using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Models;

namespace NarrationApp.Web.Pages.Admin;

public partial class TourManagement
{
    private string GetRowClass(TourDto tour) => !_isCreateMode && _selectedTour?.Id == tour.Id ? "tour-table__row--active" : string.Empty;
    private static StatusTone GetTourStatusTone(TourStatus status) => status switch { TourStatus.Published => StatusTone.Good, TourStatus.Draft => StatusTone.Neutral, TourStatus.Archived => StatusTone.Warn, _ => StatusTone.Info };
    private static string GetTourStatusLabel(TourStatus status) => status switch { TourStatus.Published => "Published", TourStatus.Draft => "Draft", TourStatus.Archived => "Archived", _ => status.ToString() };
}
