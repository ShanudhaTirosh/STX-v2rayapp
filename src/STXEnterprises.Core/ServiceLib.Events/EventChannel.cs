using System;
using System.Collections.Generic;
using System.Threading;
using ServiceLib.Common;

namespace ServiceLib.Events;

public sealed class EventChannel<T>
{
	private sealed class Subscription : IDisposable
	{
		private readonly EventChannel<T> _channel;

		private Action<T>? _handler;

		public Subscription(EventChannel<T> channel, Action<T> handler)
		{
			_channel = channel;
			_handler = handler;
		}

		public void Dispose()
		{
			Action<T> action = Interlocked.Exchange(ref _handler, null);
			if (action == null)
			{
				return;
			}
			using (_channel._gate.EnterScope())
			{
				_channel._handlers.Remove(action);
			}
		}
	}

	private readonly Lock _gate = new Lock();

	private readonly List<Action<T>> _handlers = new List<Action<T>>();

	public IDisposable Subscribe(Action<T> handler)
	{
		ArgumentNullException.ThrowIfNull(handler, "handler");
		using (_gate.EnterScope())
		{
			_handlers.Add(handler);
		}
		return new Subscription(this, handler);
	}

	public void Publish(T value)
	{
		Action<T>[] array;
		using (_gate.EnterScope())
		{
			if (_handlers.Count == 0)
			{
				return;
			}
			array = _handlers.ToArray();
		}
		Action<T>[] array2 = array;
		foreach (Action<T> action in array2)
		{
			try
			{
				action(value);
			}
			catch (Exception ex)
			{
				Logging.SaveLog("EventChannel handler", ex);
			}
		}
	}

	public void Publish()
	{
		if (typeof(T) != typeof(Unit))
		{
			throw new InvalidOperationException("Publish() without value is only valid for EventChannel<Unit>.");
		}
		Publish((T)(object)Unit.Default);
	}
}
