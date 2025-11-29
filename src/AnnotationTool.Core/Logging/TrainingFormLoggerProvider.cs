using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;

namespace AnnotationTool.Core.Logging
{
	public class TrainingFormLoggerProvider : ILoggerProvider
	{
		private readonly ITrainingLogBridge bridge;
		private readonly ConcurrentDictionary<string, TrainingFormLogger> loggers =
			new ConcurrentDictionary<string, TrainingFormLogger>();

		private bool disposed;

		public TrainingFormLoggerProvider(ITrainingLogBridge bridge)
		{
			this.bridge = bridge;
		}

		public ILogger CreateLogger(string categoryName)
		{
			if (disposed)
				return NullLogger.Instance; // prevents writing to disposed form

			return loggers.GetOrAdd(categoryName, _ => new TrainingFormLogger(bridge, () => disposed));
		}

		public void Dispose()
		{
			disposed = true;
			loggers.Clear();
		}

		private class TrainingFormLogger : ILogger
		{
			private readonly ITrainingLogBridge bridge;
			private readonly Func<bool> isDisposed;

			public TrainingFormLogger(ITrainingLogBridge bridge, Func<bool> isDisposed)
			{
				this.bridge = bridge;
				this.isDisposed = isDisposed;
			}

			public IDisposable BeginScope<TState>(TState state) => null;

			public bool IsEnabled(LogLevel logLevel)
			{
				// Only log useful stuff to training window
				return logLevel >= LogLevel.Information;
			}

			public void Log<TState>(
				LogLevel logLevel,
				EventId eventId,
				TState state,
				Exception exception,
				Func<TState, Exception, string> formatter)
			{
				if (isDisposed() || !IsEnabled(logLevel))
					return;

				var message = formatter(state, exception);

				// Thread-safe UI updates must be handled inside bridge
				bridge.Append(message);
			}
		}
	}
}
