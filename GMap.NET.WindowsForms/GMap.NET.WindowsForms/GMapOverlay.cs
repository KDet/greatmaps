﻿
using System;
using System.Drawing;
using System.Runtime.Serialization;
using GMap.NET.ObjectModel;

namespace GMap.NET.WindowsForms
{
	/// <summary>
	/// GMap.NET overlay
	/// </summary>
	[Serializable]
#if !PocketPC
	public class GMapOverlay : ISerializable, IDeserializationCallback, IDisposable
#else
   public class GMapOverlay: IDisposable
#endif
	{
		private bool _isVisibile = true;

		/// <summary>
		/// is overlay visible
		/// </summary>
		public bool IsVisibile
		{
			get { return _isVisibile; }
			set
			{
				if (value != _isVisibile)
				{
					_isVisibile = value;

					if (Control != null)
					{
						if (_isVisibile)
						{
							Control.HoldInvalidation = true;
							{
								ForceUpdate();
							}
							Control.Refresh();
						}
						else
						{
							if (Control.IsMouseOverMarker)
							{
								Control.IsMouseOverMarker = false;
							}

							if (Control.IsMouseOverPolygon)
							{
								Control.IsMouseOverPolygon = false;
							}

							if (Control.IsMouseOverRoute)
							{
								Control.IsMouseOverRoute = false;
							}
#if !PocketPC
							Control.RestoreCursorOnLeave();
#endif

							if (!Control.HoldInvalidation)
							{
								Control.Invalidate();
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// overlay Id
		/// </summary>
		public string Id;

		/// <summary>
		/// list of markers, should be thread safe
		/// </summary>
		public readonly ObservableCollectionThreadSafe<GMapMarker> Markers = new ObservableCollectionThreadSafe<GMapMarker>();

		/// <summary>
		/// list of routes, should be thread safe
		/// </summary>
		public readonly ObservableCollectionThreadSafe<GMapRoute> Routes = new ObservableCollectionThreadSafe<GMapRoute>();

		/// <summary>
		/// list of polygons, should be thread safe
		/// </summary>
		public readonly ObservableCollectionThreadSafe<GMapPolygon> Polygons =
			new ObservableCollectionThreadSafe<GMapPolygon>();

		private GMapControl _control;

		public GMapControl Control
		{
			get { return _control; }
			internal set { _control = value; }
		}

		public GMapOverlay()
		{
			CreateEvents();
		}

		public GMapOverlay(string id)
		{
			Id = id;
			CreateEvents();
		}

		private void CreateEvents()
		{
			Markers.CollectionChanged += Markers_CollectionChanged;
			Routes.CollectionChanged += Routes_CollectionChanged;
			Polygons.CollectionChanged += Polygons_CollectionChanged;
		}

		private void ClearEvents()
		{
			Markers.CollectionChanged -= Markers_CollectionChanged;
			Routes.CollectionChanged -= Routes_CollectionChanged;
			Polygons.CollectionChanged -= Polygons_CollectionChanged;
		}

		public void Clear()
		{
			Markers.Clear();
			Routes.Clear();
			Polygons.Clear();
		}

		private void Polygons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (GMapPolygon obj in e.NewItems)
				{
					if (obj != null)
					{
						obj.Overlay = this;
						if (Control != null)
						{
							Control.UpdatePolygonLocalPosition(obj);
						}
					}
				}
			}

			if (Control != null)
			{
				if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
				{
					if (Control.IsMouseOverPolygon)
					{
						Control.IsMouseOverPolygon = false;
#if !PocketPC
						Control.RestoreCursorOnLeave();
#endif
					}
				}

				if (!Control.HoldInvalidation)
				{
					Control.Invalidate();
				}
			}
		}

		private void Routes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (GMapRoute obj in e.NewItems)
				{
					if (obj != null)
					{
						obj.Overlay = this;
						if (Control != null)
						{
							Control.UpdateRouteLocalPosition(obj);
						}
					}
				}
			}

			if (Control != null)
			{
				if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
				{
					if (Control.IsMouseOverRoute)
					{
						Control.IsMouseOverRoute = false;
#if !PocketPC
						Control.RestoreCursorOnLeave();
#endif
					}
				}

				if (!Control.HoldInvalidation)
				{
					Control.Invalidate();
				}
			}
		}

		private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (GMapMarker obj in e.NewItems)
				{
					if (obj != null)
					{
						obj.Overlay = this;
						if (Control != null)
						{
							Control.UpdateMarkerLocalPosition(obj);
						}
					}
				}
			}

			if (Control != null)
			{
				if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
				{
					if (Control.IsMouseOverMarker)
					{
						Control.IsMouseOverMarker = false;
#if !PocketPC
						Control.RestoreCursorOnLeave();
#endif
					}
				}

				if (!Control.HoldInvalidation)
				{
					Control.Invalidate();
				}
			}
		}

		/// <summary>
		/// updates local positions of objects
		/// </summary>
		internal void ForceUpdate()
		{
			if (Control != null)
			{
				foreach (var obj in Markers)
				{
					if (obj.IsVisible)
					{
						Control.UpdateMarkerLocalPosition(obj);
					}
				}

				foreach (var obj in Polygons)
				{
					if (obj.IsVisible)
					{
						Control.UpdatePolygonLocalPosition(obj);
					}
				}

				foreach (var obj in Routes)
				{
					if (obj.IsVisible)
					{
						Control.UpdateRouteLocalPosition(obj);
					}
				}
			}
		}

		/// <summary>
		/// renders objects/routes/polygons
		/// </summary>
		/// <param name="g"></param>
		public virtual void OnRender(Graphics g)
		{
			if (Control != null)
			{
				if (Control.RoutesEnabled)
				{
					foreach (var r in Routes)
					{
						if (r.IsVisible)
						{
							r.OnRender(g);
						}
					}
				}

				if (Control.PolygonsEnabled)
				{
					foreach (var r in Polygons)
					{
						if (r.IsVisible)
						{
							r.OnRender(g);
						}
					}
				}

				if (Control.MarkersEnabled)
				{
					// markers
					foreach (var m in Markers)
					{
						//if(m.IsVisible && (m.DisableRegionCheck || Control.Core.currentRegion.Contains(m.LocalPosition.X, m.LocalPosition.Y)))
						if (m.IsVisible || m.DisableRegionCheck)
						{
							m.OnRender(g);
						}
					}

					// tooltips above
					foreach (var m in Markers)
					{
						//if(m.ToolTip != null && m.IsVisible && Control.Core.currentRegion.Contains(m.LocalPosition.X, m.LocalPosition.Y))
						if (m.ToolTip != null && m.IsVisible)
						{
							if (!string.IsNullOrEmpty(m.ToolTipText) &&
							    (m.ToolTipMode == MarkerTooltipMode.Always ||
							     (m.ToolTipMode == MarkerTooltipMode.OnMouseOver && m.IsMouseOver)))
							{
								m.ToolTip.OnRender(g);
							}
						}
					}
				}
			}
		}

#if !PocketPC

		#region ISerializable Members

		/// <summary>
		/// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
		/// <param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
		/// <exception cref="T:System.Security.SecurityException">
		/// The caller does not have the required permission.
		/// </exception>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Id", Id);
			info.AddValue("IsVisible", IsVisibile);

			var markerArray = new GMapMarker[Markers.Count];
			Markers.CopyTo(markerArray, 0);
			info.AddValue("Markers", markerArray);

			var routeArray = new GMapRoute[Routes.Count];
			Routes.CopyTo(routeArray, 0);
			info.AddValue("Routes", routeArray);

			var polygonArray = new GMapPolygon[Polygons.Count];
			Polygons.CopyTo(polygonArray, 0);
			info.AddValue("Polygons", polygonArray);
		}

		private GMapMarker[] _deserializedMarkerArray;
		private GMapRoute[] _deserializedRouteArray;
		private GMapPolygon[] _deserializedPolygonArray;

		/// <summary>
		/// Initializes a new instance of the <see cref="GMapOverlay"/> class.
		/// </summary>
		/// <param name="info">The info.</param>
		/// <param name="context">The context.</param>
		protected GMapOverlay(SerializationInfo info, StreamingContext context)
		{
			Id = info.GetString("Id");
			IsVisibile = info.GetBoolean("IsVisible");

			_deserializedMarkerArray = Extensions.GetValue(info, "Markers", new GMapMarker[0]);
			_deserializedRouteArray = Extensions.GetValue(info, "Routes", new GMapRoute[0]);
			_deserializedPolygonArray = Extensions.GetValue(info, "Polygons", new GMapPolygon[0]);

			CreateEvents();
		}

		#endregion

		#region IDeserializationCallback Members

		/// <summary>
		/// Runs when the entire object graph has been deserialized.
		/// </summary>
		/// <param name="sender">The object that initiated the callback. The functionality for this parameter is not currently implemented.</param>
		public void OnDeserialization(object sender)
		{
			// Populate Markers
			foreach (var marker in _deserializedMarkerArray)
			{
				marker.Overlay = this;
				Markers.Add(marker);
			}

			// Populate Routes
			foreach (var route in _deserializedRouteArray)
			{
				route.Overlay = this;
				Routes.Add(route);
			}

			// Populate Polygons
			foreach (var polygon in _deserializedPolygonArray)
			{
				polygon.Overlay = this;
				Polygons.Add(polygon);
			}
		}

		#endregion

#endif

		#region IDisposable Members

		private bool _disposed;

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;

				ClearEvents();

				foreach (var m in Markers)
				{
					m.Dispose();
				}

				foreach (var r in Routes)
				{
					r.Dispose();
				}

				foreach (var p in Polygons)
				{
					p.Dispose();
				}

				Clear();
			}
		}

		#endregion
	}
}