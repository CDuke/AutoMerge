using System;
using System.Diagnostics;
using AutoMerge.Prism;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge.Base
{
	/// <summary> 
	/// Team Explorer plugin common base class. 
	/// </summary> 
	public abstract class TeamExplorerBase : BindableBase, IDisposable
	{
		private bool _disposed;
		
		#region Members

		private bool _contextSubscribed;

		#endregion

		/// <summary> 
		/// Get/set the service provider. 
		/// </summary> 
		public IServiceProvider ServiceProvider
		{
			get { return _serviceProvider; }
			set
			{
				// Unsubscribe from Team Foundation context changes 
				if (_serviceProvider != null)
				{
					UnsubscribeContextChanges();
				}

				_serviceProvider = value;

				// Subscribe to Team Foundation context changes 
				if (_serviceProvider != null)
				{
					SubscribeContextChanges();
				}
			}
		}
		private IServiceProvider _serviceProvider;

		/// <summary> 
		/// Get the requested service from the service provider. 
		/// </summary> 
		public T GetService<T>()
		{
			Debug.Assert(ServiceProvider != null, "GetService<T> called before service provider is set");
			if (ServiceProvider != null)
			{
				return (T)ServiceProvider.GetService(typeof(T));
			}

			return default(T);
		}

		/// <summary> 
		/// Show a notification in the Team Explorer window. 
		/// </summary> 
		protected Guid ShowNotification(string message, NotificationType type)
		{
			var teamExplorer = GetService<ITeamExplorer>();
			if (teamExplorer != null)
			{
				var guid = Guid.NewGuid();
				teamExplorer.ShowNotification(message, type, NotificationFlags.None, null, guid);
				return guid;
			}

			return Guid.Empty;
		}

		#region IDisposable

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				UnsubscribeContextChanges();
			}

			_disposed = true;
		}

		/// <summary> 
		/// Dispose. 
		/// </summary> 
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		#region Team Foundation Context

		/// <summary> 
		/// Subscribe to context changes. 
		/// </summary> 
		protected void SubscribeContextChanges()
		{
			Debug.Assert(ServiceProvider != null, "ServiceProvider must be set before subscribing to context changes");
			if (ServiceProvider == null || _contextSubscribed)
			{
				return;
			}

			var tfContextManager = GetService<ITeamFoundationContextManager>();
			if (tfContextManager != null)
			{
				tfContextManager.ContextChanged += ContextChanged;
				_contextSubscribed = true;
			}
		}

		/// <summary> 
		/// Unsubscribe from context changes. 
		/// </summary> 
		protected void UnsubscribeContextChanges()
		{
			if (ServiceProvider == null || !_contextSubscribed)
			{
				return;
			}

			var tfContextManager = GetService<ITeamFoundationContextManager>();
			if (tfContextManager != null)
			{
				tfContextManager.ContextChanged -= ContextChanged;
			}
		}

		/// <summary>
		/// ContextChanged event handler.
		/// </summary>
		protected virtual void ContextChanged(object sender, ContextChangedEventArgs e)
		{
		}

		/// <summary> 
		/// Get the current Team Foundation context. 
		/// </summary> 
		protected ITeamFoundationContext CurrentContext
		{
			get
			{
				var tfContextManager = GetService<ITeamFoundationContextManager>();
				if (tfContextManager != null)
				{
					return tfContextManager.CurrentContext;
				}

				return null;
			}
		}

		#endregion
	}
}
