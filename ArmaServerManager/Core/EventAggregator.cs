using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ArmaServerManager.Core;

public interface IEventAggregator
{
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    void Publish<TEvent>(TEvent eventToPublish) where TEvent : class;
}

public class EventAggregator : IEventAggregator
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        _subscribers.AddOrUpdate(
            eventType,
            new List<Delegate> { handler },
            (key, existing) =>
            {
                lock (existing)
                {
                    existing.Add(handler);
                }
                return existing;
            });
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }
    }

    public void Publish<TEvent>(TEvent eventToPublish) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            List<Delegate> handlersCopy;
            lock (handlers)
            {
                handlersCopy = handlers.ToList();
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    ((Action<TEvent>)handler)(eventToPublish);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error publishing event: {ex.Message}");
                }
            }
        }
    }
}

// Event definitions
public class ServerStartedEvent
{
    public string ServerId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class ServerStoppedEvent
{
    public string ServerId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class ModInstalledEvent
{
    public string WorkshopId { get; set; } = string.Empty;
    public string ModName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class ModRemovedEvent
{
    public string WorkshopId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class BackupCreatedEvent
{
    public string ServerId { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class UpdateAvailableEvent
{
    public string ItemName { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string NewVersion { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class ThemeChangedEvent
{
    public string Theme { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
