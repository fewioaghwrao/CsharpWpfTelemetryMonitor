using Microsoft.Extensions.Options;
using System;

namespace WpfApp.Tests;

public sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
{
    private T _currentValue;
    private event Action<T, string?>? _listeners;

    public TestOptionsMonitor(T currentValue)
    {
        _currentValue = currentValue;
    }

    public T CurrentValue => _currentValue;

    public T Get(string? name) => _currentValue;

    public IDisposable OnChange(Action<T, string?> listener)
    {
        _listeners += listener;
        return new Unsubscriber(() => _listeners -= listener);
    }

    public void Set(T newValue, string? name = null)
    {
        _currentValue = newValue;
        _listeners?.Invoke(_currentValue, name);
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public Unsubscriber(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _dispose();
            _disposed = true;
        }
    }
}