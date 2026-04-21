using System.Net;
using System.Net.Http.Json;
using NarrationApp.Mobile.Features.Home;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class TouristQrApiServiceTests
{
    [Fact]
    public async Task OpenAsync_PostsScanRequestWithDeviceIdHeader()
    {
        var service = CreateService((request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/qr/QR-001/scan", request.RequestUri!.AbsolutePath);
            Assert.Equal("device-123", Assert.Single(request.Headers.GetValues("X-Device-Id")));

            return Task.FromResult(CreateJsonResponse(new ApiResponse<QrCodeDto>
            {
                Succeeded = true,
                Data = new QrCodeDto
                {
                    Code = "QR-001",
                    TargetType = "poi",
                    TargetId = 7
                }
            }));
        });

        var qr = await service.OpenAsync("QR-001", "device-123");

        Assert.Equal("QR-001", qr.Code);
        Assert.Equal("poi", qr.TargetType);
        Assert.Equal(7, qr.TargetId);
    }

    [Fact]
    public async Task OpenAsync_FallsBackToResolveEndpointWhenScanFails()
    {
        var requestCount = 0;
        var service = CreateService((request, cancellationToken) =>
        {
            requestCount++;

            if (request.Method == HttpMethod.Post)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = JsonContent.Create(new ApiResponse<QrCodeDto>
                    {
                        Succeeded = false,
                        Message = "Scan failed."
                    })
                });
            }

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/qr/QR-TOUR-2", request.RequestUri!.AbsolutePath);

            return Task.FromResult(CreateJsonResponse(new ApiResponse<QrCodeDto>
            {
                Succeeded = true,
                Data = new QrCodeDto
                {
                    Code = "QR-TOUR-2",
                    TargetType = "tour",
                    TargetId = 2
                }
            }));
        });

        var qr = await service.OpenAsync("QR-TOUR-2", "device-456");

        Assert.Equal(2, requestCount);
        Assert.Equal("tour", qr.TargetType);
        Assert.Equal(2, qr.TargetId);
    }

    private static TouristQrApiService CreateService(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://10.0.2.2:5001/")
        };

        return new TouristQrApiService(httpClient);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload)
        };
    }

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }
}
