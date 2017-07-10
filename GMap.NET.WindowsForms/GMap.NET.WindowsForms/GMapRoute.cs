﻿
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace GMap.NET.WindowsForms
{
	/// <summary>
	/// GMap.NET route
	/// </summary>
	[Serializable]
#if !PocketPC
	public class GMapRoute : MapRoute, ISerializable, IDeserializationCallback, IDisposable
#else
    public class GMapRoute : MapRoute, IDisposable
#endif
	{
		private GMapOverlay _overlay;

		public GMapOverlay Overlay
		{
			get { return _overlay; }
			internal set { _overlay = value; }
		}

		private bool _visible = true;

		/// <summary>
		/// is marker visible
		/// </summary>
		public bool IsVisible
		{
			get { return _visible; }
			set
			{
				if (value != _visible)
				{
					_visible = value;

					if (Overlay != null && Overlay.Control != null)
					{
						if (_visible)
						{
							Overlay.Control.UpdateRouteLocalPosition(this);
						}
						else
						{
							if (Overlay.Control.IsMouseOverRoute)
							{
								Overlay.Control.IsMouseOverRoute = false;
#if !PocketPC
								Overlay.Control.RestoreCursorOnLeave();
#endif
							}
						}

						{
							if (!Overlay.Control.HoldInvalidation)
							{
								Overlay.Control.Invalidate();
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// can receive input
		/// </summary>
		public bool IsHitTestVisible = false;

		private bool _isMouseOver;

		/// <summary>
		/// is mouse over
		/// </summary>
		public bool IsMouseOver
		{
			get { return _isMouseOver; }
			internal set { _isMouseOver = value; }
		}

#if !PocketPC
		/// <summary>
		/// Indicates whether the specified point is contained within this System.Drawing.Drawing2D.GraphicsPath
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		internal bool IsInside(int x, int y)
		{
			if (_graphicsPath != null)
			{
				return _graphicsPath.IsOutlineVisible(x, y, Stroke);
			}

			return false;
		}

		private GraphicsPath _graphicsPath;

		internal void UpdateGraphicsPath()
		{
			if (_graphicsPath == null)
			{
				_graphicsPath = new GraphicsPath();
			}
			else
			{
				_graphicsPath.Reset();
			}

			{
				for (var i = 0; i < LocalPoints.Count; i++)
				{
					var p2 = LocalPoints[i];

					if (i == 0)
					{
						_graphicsPath.AddLine(p2.X, p2.Y, p2.X, p2.Y);
					}
					else
					{
						var p = _graphicsPath.GetLastPoint();
						_graphicsPath.AddLine(p.X, p.Y, p2.X, p2.Y);
					}
				}
			}
		}
#endif

		public virtual void OnRender(Graphics g)
		{
#if !PocketPC
			if (IsVisible)
			{
				if (_graphicsPath != null)
				{
					g.DrawPath(Stroke, _graphicsPath);
				}
			}
#else
            if (IsVisible)
            {
                Point[] pnts = new Point[LocalPoints.Count];
                for (int i = 0; i < LocalPoints.Count; i++)
                {
                    Point p2 = new Point((int)LocalPoints[i].X, (int)LocalPoints[i].Y);
                    pnts[pnts.Length - 1 - i] = p2;
                }

                if (pnts.Length > 1)
                {
                    g.DrawLines(Stroke, pnts);
                }
            }
#endif
		}

#if !PocketPC
		public static readonly Pen DefaultStroke = new Pen(Color.FromArgb(144, Color.MidnightBlue));
#else
        public static readonly Pen DefaultStroke = new Pen(Color.MidnightBlue);
#endif

		/// <summary>
		/// specifies how the outline is painted
		/// </summary>
		[NonSerialized] public Pen Stroke = DefaultStroke;

		public readonly List<GPoint> LocalPoints = new List<GPoint>();

		static GMapRoute()
		{
#if !PocketPC
			DefaultStroke.LineJoin = LineJoin.Round;
#endif
			DefaultStroke.Width = 5;
		}

		public GMapRoute(string name)
			: base(name)
		{

		}

		public GMapRoute(IEnumerable<PointLatLng> points, string name)
			: base(points, name)
		{

		}

#if !PocketPC

		#region ISerializable Members

		// Temp store for de-serialization.
		private GPoint[] _deserializedLocalPoints;

		/// <summary>
		/// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
		/// <param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
		/// <exception cref="T:System.Security.SecurityException">
		/// The caller does not have the required permission.
		/// </exception>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("Visible", IsVisible);
			info.AddValue("LocalPoints", LocalPoints.ToArray());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GMapRoute"/> class.
		/// </summary>
		/// <param name="info">The info.</param>
		/// <param name="context">The context.</param>
		protected GMapRoute(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			//this.Stroke = Extensions.GetValue<Pen>(info, "Stroke", new Pen(Color.FromArgb(144, Color.MidnightBlue)));
			IsVisible = Extensions.GetStruct(info, "Visible", true);
			_deserializedLocalPoints = Extensions.GetValue<GPoint[]>(info, "LocalPoints");
		}

		#endregion

		#region IDeserializationCallback Members

		/// <summary>
		/// Runs when the entire object graph has been de-serialized.
		/// </summary>
		/// <param name="sender">The object that initiated the callback. The functionality for this parameter is not currently implemented.</param>
		public override void OnDeserialization(object sender)
		{
			base.OnDeserialization(sender);

			// Accounts for the de-serialization being breadth first rather than depth first.
			LocalPoints.AddRange(_deserializedLocalPoints);
			LocalPoints.Capacity = Points.Count;
		}

		#endregion

#endif

		#region IDisposable Members

		private bool _disposed;

		public virtual void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;

				LocalPoints.Clear();

#if !PocketPC
				if (_graphicsPath != null)
				{
					_graphicsPath.Dispose();
					_graphicsPath = null;
				}
#endif
				Clear();
			}
		}

		#endregion
	}

	public delegate void RouteClick(GMapRoute item, MouseEventArgs e);

	public delegate void RouteEnter(GMapRoute item);

	public delegate void RouteLeave(GMapRoute item);
}