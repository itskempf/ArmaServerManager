using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArmaServerManager.Core;

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

public class NotificationMessage
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);
}

public class NotificationService
{
    private readonly Queue<NotificationMessage> _messageQueue = new();
    private InfoBar? _currentInfoBar;
    private Timer? _dismissTimer;
    private bool _isProcessing;

    public event Action<NotificationMessage>? NotificationRequested;

    public void ShowInfo(string title, string message) => 
        QueueNotification(new NotificationMessage { Title = title, Message = message, Type = NotificationType.Info });

    public void ShowSuccess(string title, string message) => 
        QueueNotification(new NotificationMessage { Title = title, Message = message, Type = NotificationType.Success });

    public void ShowWarning(string title, string message) => 
        QueueNotification(new NotificationMessage { Title = title, Message = message, Type = NotificationType.Warning });

    public void ShowError(string title, string message) => 
        QueueNotification(new NotificationMessage { Title = title, Message = message, Type = NotificationType.Error });

    private void QueueNotification(NotificationMessage notification)
    {
        _messageQueue.Enqueue(notification);
        ProcessQueue();
    }

    private void ProcessQueue()
    {
        if (_messageQueue.Count > 0 && !_isProcessing)
        {
            _isProcessing = true;
            var notification = _messageQueue.Dequeue();
            NotificationRequested?.Invoke(notification);
            
            _dismissTimer?.Dispose();
            _dismissTimer = new Timer(_ => 
            {
                _isProcessing = false;
                SetCurrentInfoBar(null);
            }, null, notification.Duration, Timeout.InfiniteTimeSpan);
        }
    }

    public void SetCurrentInfoBar(InfoBar? infoBar)
    {
        _currentInfoBar = infoBar;
        if (infoBar == null)
        {
            _isProcessing = false;
            ProcessQueue();
        }
    }
    
    public void Dispose()
    {
        _dismissTimer?.Dispose();
    }
}