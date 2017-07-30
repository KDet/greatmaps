using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Forms;

namespace GMap.NET.ObjectModel
{
	public class ObservableCollectionThreadSafe<T> : ObservableRangeCollection<T>
	{
		private NotifyCollectionChangedEventHandler _collectionChanged;

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			// Be nice - use BlockReentrancy like MSDN said
			using (BlockReentrancy())
			{
				if (_collectionChanged != null)
				{
					var delegates = _collectionChanged.GetInvocationList();

					// Walk thru invocation list
					foreach (NotifyCollectionChangedEventHandler handler in delegates)
					{
#if !PocketPC
						var dispatcherObject = handler.Target as Control;

						// If the subscriber is a DispatcherObject and different thread
						if (dispatcherObject != null && dispatcherObject.InvokeRequired)
						{
							// Invoke handler in the target dispatcher's thread
							dispatcherObject.Invoke(handler, this, e);
						}
						else // Execute handler as is 
						{
							_collectionChanged(this, e);
						}
#else
	// If the subscriber is a DispatcherObject and different thread
                  if(handler != null)
                  {
                     // Invoke handler in the target dispatcher's thread
                     handler.Invoke(handler, e);
                  }
                  else // Execute handler as is 
                  {
                     _collectionChanged(this, e);
                  }
#endif
					}
				}
			}
		}

		public ObservableCollectionThreadSafe(): base()
		{			
		}
		public ObservableCollectionThreadSafe(IEnumerable<T> collection)
			: base(collection)
		{
		}

		public override event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add { _collectionChanged += value; }
			remove { _collectionChanged -= value; }
		}
	}
}