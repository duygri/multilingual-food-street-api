using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Seed;

public sealed partial class DataSeeder
{
    private async Task SeedToursAndQrCodesAsync(CancellationToken cancellationToken)
    {
        var tourStopSlugs = SeedBusStopTour.StopSlugs;
        var poiIdsBySlug = await dbContext.Pois
            .Where(poi => tourStopSlugs.Contains(poi.Slug))
            .ToDictionaryAsync(poi => poi.Slug, poi => poi.Id, cancellationToken);

        if (tourStopSlugs.Any(slug => !poiIdsBySlug.ContainsKey(slug)))
        {
            logger.LogWarning("Skipped bus-stop tour seed because one or more tour POIs are missing.");
            return;
        }

        var tour = await dbContext.Tours
            .Include(item => item.Stops)
            .OrderBy(item => item.Id)
            .FirstOrDefaultAsync(item => item.Title == SeedBusStopTour.Title, cancellationToken);

        if (tour is null)
        {
            tour = new Tour
            {
                Title = SeedBusStopTour.Title,
                Description = SeedBusStopTour.Description,
                EstimatedMinutes = SeedBusStopTour.EstimatedMinutes,
                CoverImage = SeedBusStopTour.CoverImage,
                Status = TourStatus.Published
            };

            dbContext.Tours.Add(tour);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded sample bus-stop tour.");
        }

        var existingSequences = tour.Stops
            .Select(stop => stop.Sequence)
            .ToHashSet();

        for (var index = 0; index < tourStopSlugs.Count; index++)
        {
            var sequence = index + 1;
            if (existingSequences.Contains(sequence))
            {
                continue;
            }

            dbContext.TourStops.Add(new TourStop
            {
                TourId = tour.Id,
                PoiId = poiIdsBySlug[tourStopSlugs[index]],
                Sequence = sequence,
                RadiusMeters = AppConstants.DefaultTourStopRadiusMeters
            });
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded sample bus-stop tour stops.");
        }

        var seededQrCodes = SeedBusStopTour.QrCodes.Select(item => item.Code).ToArray();
        var qrCodesByCode = await dbContext.QrCodes
            .Where(qr => seededQrCodes.Contains(qr.Code))
            .ToDictionaryAsync(qr => qr.Code, cancellationToken);

        foreach (var qrDefinition in SeedBusStopTour.QrCodes)
        {
            if (!qrCodesByCode.TryGetValue(qrDefinition.Code, out var qrCode))
            {
                dbContext.QrCodes.Add(new QrCode
                {
                    Code = qrDefinition.Code,
                    TargetType = "tour",
                    TargetId = tour.Id,
                    LocationHint = qrDefinition.LocationHint
                });
                continue;
            }

            qrCode.TargetType = "tour";
            qrCode.TargetId = tour.Id;
            qrCode.LocationHint = qrDefinition.LocationHint;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded sample bus-stop QR codes.");
        }

        var poiListQrCode = await dbContext.QrCodes
            .SingleOrDefaultAsync(qr => qr.Code == SeedPoiListQrCode.Code, cancellationToken);

        if (poiListQrCode is null)
        {
            dbContext.QrCodes.Add(new QrCode
            {
                Code = SeedPoiListQrCode.Code,
                TargetType = "poi_list",
                TargetId = 0,
                LocationHint = SeedPoiListQrCode.LocationHint
            });
        }
        else
        {
            poiListQrCode.TargetType = "poi_list";
            poiListQrCode.TargetId = 0;
            poiListQrCode.LocationHint = SeedPoiListQrCode.LocationHint;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded sample POI list QR code.");
        }
    }

    private static SeedTourDefinition SeedBusStopTour { get; } = new(
        "Tuyến xe buýt Khánh Hội - Vĩnh Hội - Xóm Chiếu",
        "Tour mẫu dành cho khách bắt đầu từ các điểm dừng xe buýt ở phường Khánh Hội, Vĩnh Hội và Xóm Chiếu. Quét QR tại trạm là có thể nghe ngay điểm đầu tiên, sau đó tiếp tục khám phá các quán ăn nổi bật quanh phố Vĩnh Khánh.",
        45,
        "https://down-vn.img.susercontent.com/vn-11134513-7r98o-lstpv5wpypxgb0",
        [
            "bun-thit-nuong-co-nga",
            "bun-ca-chau-doc-di-tu",
            "oc-oanh-vinh-khanh",
            "chili-lau-nuong-tu-chon",
            "lang-quan-vinh-khanh",
            "ot-xiem-quan-vinh-khanh"
        ],
        [
            new("BUS-KHANH-HOI-TOUR", "Điểm dừng xe buýt phường Khánh Hội"),
            new("BUS-VINH-HOI-TOUR", "Điểm dừng xe buýt phường Vĩnh Hội"),
            new("BUS-XOM-CHIEU-TOUR", "Điểm dừng xe buýt phường Xóm Chiếu")
        ]);

    private static SeedTourQrDefinition SeedPoiListQrCode { get; } =
        new("VINH-KHANH-POI-LIST", "QR danh sách POI ẩm thực Vĩnh Khánh");

    private sealed record SeedTourDefinition(
        string Title,
        string Description,
        int EstimatedMinutes,
        string CoverImage,
        IReadOnlyList<string> StopSlugs,
        IReadOnlyList<SeedTourQrDefinition> QrCodes);

    private sealed record SeedTourQrDefinition(string Code, string LocationHint);
}
