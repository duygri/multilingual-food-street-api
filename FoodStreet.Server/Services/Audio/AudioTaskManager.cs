using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using FoodStreet.Server.Constants;
using FoodStreet.Server.Hubs;
using FoodStreet.Server.Services.Interfaces;
using PROJECT_C_.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodStreet.Server.Services.Audio
{
    public class AudioTaskInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Status { get; set; } = PoiAudioStatuses.Queued;
        public int Progress { get; set; } = 0;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AudioTaskManager : BackgroundService
    {
        private readonly ConcurrentDictionary<string, AudioTaskInfo> _tasks = new();
        private readonly Channel<AudioTaskInfo> _queue;
        private readonly SemaphoreSlim _semaphore;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AudioTaskManager> _logger;

        public event Action<AudioTaskInfo>? OnTaskChanged;

        public AudioTaskManager(IServiceProvider serviceProvider, ILogger<AudioTaskManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _queue = Channel.CreateUnbounded<AudioTaskInfo>();
            // Max 3 concurrent generation tasks
            _semaphore = new SemaphoreSlim(3, 3);
        }

        public string EnqueueTask(int locationId, string locationName)
        {
            var info = new AudioTaskInfo
            {
                LocationId = locationId,
                LocationName = locationName,
                Message = "Waiting in queue..."
            };
            
            _tasks.TryAdd(info.Id, info);
            _queue.Writer.TryWrite(info);
            NotifyUpdate(info);
            
            return info.Id;
        }

        public IEnumerable<AudioTaskInfo> GetAllTasks()
        {
            return _tasks.Values.OrderByDescending(t => t.CreatedAt);
        }

        private void NotifyUpdate(AudioTaskInfo info)
        {
            OnTaskChanged?.Invoke(info);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[AudioTaskManager] Background Engine Started with Max Concurrency = 3.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var taskInfo = await _queue.Reader.ReadAsync(stoppingToken);

                // Wait for available slot (Semaphore=3)
                await _semaphore.WaitAsync(stoppingToken);

                // Fire and forget (parallel)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        taskInfo.Status = PoiAudioStatuses.Running;
                        taskInfo.Message = "Synthesizing translations...";
                        taskInfo.Progress = 10;
                        NotifyUpdate(taskInfo);

                        using var scope = _serviceProvider.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var ttsService = scope.ServiceProvider.GetRequiredService<ITtsService>();

                        var poi = await db.Locations
                            .Include(l => l.Translations)
                            .FirstOrDefaultAsync(l => l.Id == taskInfo.LocationId, stoppingToken);

                        if (poi != null && !string.IsNullOrEmpty(poi.Description))
                        {
                            poi.AudioStatus = PoiAudioStatuses.Running;
                            await db.SaveChangesAsync(stoppingToken);

                            var hubContext = scope.ServiceProvider.GetService<IHubContext<NotificationHub>>();
                            var translator = scope.ServiceProvider.GetRequiredService<FoodStreet.Server.Services.Audio.GoogleTranslator>();
                            
                            string[] targetLangs = { "en-US", "ja-JP", "zh-CN", "ko-KR" };
                            int step = 80 / (targetLangs.Length + 1); // +1 for Vietnamese itself
                            var now = DateTime.UtcNow;

                            // Vietnamese (Original) Audio
                            taskInfo.Message = "Synthesizing vi-VN...";
                            NotifyUpdate(taskInfo);
                            _ = await ttsService.TextToSpeechAsync(poi.Description, "vi-VN");
                            taskInfo.Progress += step;
                            NotifyUpdate(taskInfo);

                            foreach (var lang in targetLangs)
                            {
                                taskInfo.Message = $"Translating & Synthesizing {lang}...";
                                NotifyUpdate(taskInfo);

                                // 1. Translate Name and Description
                                string transName = await translator.TranslateTextAsync(poi.Name, lang.Substring(0, 2));
                                string transDesc = await translator.TranslateTextAsync(poi.Description, lang.Substring(0, 2));

                                // 2. Synthesize Audio
                                string? audioUrl = await ttsService.TextToSpeechAsync(transDesc, lang);
                                
                                // 3. Upsert Localization
                                var translation = poi.Translations.FirstOrDefault(t => t.LanguageCode == lang);
                                if (translation == null)
                                {
                                    poi.Translations.Add(new PROJECT_C_.Models.LocationTranslation 
                                    { 
                                        LanguageCode = lang, 
                                        Name = transName, 
                                        Description = transDesc,
                                        TtsScript = transDesc,
                                        AudioUrl = audioUrl,
                                        IsFallback = (lang == "en-US"),
                                        GeneratedAt = now
                                    });
                                }
                                else
                                {
                                    translation.Name = transName;
                                    translation.Description = transDesc;
                                    translation.TtsScript = transDesc;
                                    translation.AudioUrl = audioUrl;
                                    translation.GeneratedAt = now;
                                }
                                
                                taskInfo.Progress += step;
                                NotifyUpdate(taskInfo);
                            }
                            
                            poi.AudioStatus = PoiAudioStatuses.Ready;
                            await db.SaveChangesAsync(stoppingToken);

                            // Send Socket Notification
                            if (hubContext != null && !string.IsNullOrEmpty(poi.OwnerId))
                            {
                                var notifMsg = $"Hệ thống AI đã dịch và tạo Audio xong cho POI \"{poi.Name}\" bằng 5 ngôn ngữ!";
                                var notification = new PROJECT_C_.Models.Notification
                                {
                                    UserId = poi.OwnerId,
                                    Title = "🎧 Đã tạo xong Audio Thuyết Minh",
                                    Message = notifMsg,
                                    Type = PROJECT_C_.Models.NotificationType.POI_AudioReady,
                                    RelatedId = poi.Id,
                                    SenderName = "Audio AI Engine"
                                };
                                db.Notifications.Add(notification);
                                await db.SaveChangesAsync(stoppingToken);

                                await hubContext.Clients.Group(NotificationHubGroups.User(poi.OwnerId)).SendAsync("ReceiveNotification", new
                                {
                                    notification.Id,
                                    notification.Title,
                                    notification.Message,
                                    Type = notification.Type.ToString(),
                                    notification.CreatedAt,
                                    notification.RelatedId,
                                    notification.SenderName
                                }, cancellationToken: stoppingToken);

                                await hubContext.SendRealtimeToUserAsync(
                                    poi.OwnerId,
                                    NotificationHubEvents.AudioReady,
                                    new RealtimeActivityMessage
                                    {
                                        EntityType = "poi",
                                        EntityId = poi.Id,
                                        Status = PoiAudioStatuses.Ready,
                                        Title = "Audio đã sẵn sàng",
                                        Message = notifMsg,
                                        TriggeredBy = "Audio AI Engine"
                                    },
                                    stoppingToken);
                            }

                            if (hubContext != null)
                            {
                                await hubContext.SendRealtimeToPoiAsync(
                                    poi.Id,
                                    NotificationHubEvents.AudioReady,
                                    new RealtimeActivityMessage
                                    {
                                        EntityType = "poi",
                                        EntityId = poi.Id,
                                        Status = PoiAudioStatuses.Ready,
                                        Title = "Audio đã sẵn sàng",
                                        Message = $"Audio cho POI \"{poi.Name}\" đã được sinh xong.",
                                        TriggeredBy = "Audio AI Engine"
                                    },
                                    stoppingToken);
                            }
                        }

                        taskInfo.Status = PoiAudioStatuses.Ready;
                        taskInfo.Progress = 100;
                        taskInfo.Message = "Audio processing complete.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"[AudioTaskManager] Failed processing task {taskInfo.Id}");
                        taskInfo.Status = PoiAudioStatuses.Failed;
                        taskInfo.Message = ex.Message;

                        try
                        {
                            using var failureScope = _serviceProvider.CreateScope();
                            var db = failureScope.ServiceProvider.GetRequiredService<AppDbContext>();
                            var poi = await db.Locations.FirstOrDefaultAsync(l => l.Id == taskInfo.LocationId, stoppingToken);
                            if (poi != null)
                            {
                                poi.AudioStatus = PoiAudioStatuses.Failed;
                                await db.SaveChangesAsync(stoppingToken);
                            }
                        }
                        catch (Exception nestedEx)
                        {
                            _logger.LogWarning(nestedEx, "[AudioTaskManager] Failed to persist failed status for location {LocationId}", taskInfo.LocationId);
                        }
                    }
                    finally
                    {
                        NotifyUpdate(taskInfo);
                        _semaphore.Release();
                    }
                }, stoppingToken);
            }
        }
    }
}
