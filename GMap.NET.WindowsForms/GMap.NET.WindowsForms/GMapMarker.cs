using System;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Forms;
using GMap.NET.WindowsForms.ToolTips;

namespace GMap.NET.WindowsForms
{
	public delegate void MarkerClick(GMapMarker item, MouseEventArgs e);

	public delegate void MarkerEnter(GMapMarker item);

	public delegate void MarkerLeave(GMapMarker item);

	/// <summary>
	/// Mode of tooltip
	/// </summary>
	public enum MarkerTooltipMode
	{
		OnMouseOver,
		Never,
		Always
	}

	/// <summary>
	/// GMap.NET marker
	/// </summary>
	[Serializable]
#if !PocketPC
	public abstract class GMapMarker : ISerializable, IDisposable
#else
   public class GMapMarker: IDisposable
#endif
	{
#if PocketPC
      static readonly System.Drawing.Imaging.ImageAttributes attr = new System.Drawing.Imaging.ImageAttributes();

      static GMapMarker()
      {
         attr.SetColorKey(Color.White, Color.White);
      }
#endif
		private PointLatLng _position;
		private PointF _offset;
		private RectangleF _area;
		private bool _isMouseOver;
		private bool _disposed;
		private bool _isHitTestVisible = true;

		protected string _toolTipText;
		protected GMapOverlay _overlay;
		protected MarkerTooltipMode _toolTipMode = MarkerTooltipMode.OnMouseOver;
		protected bool Visible = true;
		protected GMapToolTip _toolTip;

		public GMapOverlay Overlay
		{
			get { return _overlay; }
			internal set { _overlay = value; }
		}

		public PointLatLng Position
		{
			get { return _position; }
			set
			{
				if (_position != value)
				{
					_position = value;
					if (IsVisible)
						if (Overlay != null && Overlay.Control != null)
							Overlay.Control.UpdateMarkerLocalPosition(this);
				}
			}
		}

		public object Tag { get; set; }

		public PointF Offset
		{
			get { return _offset; }
			set
			{
				if (_offset != value)
				{
					_offset = value;
					if (IsVisible)
						if (Overlay != null && Overlay.Control != null)
							Overlay.Control.UpdateMarkerLocalPosition(this);
				}
			}
		}

		/// <summary>
		/// Marker position in local coordinates, internal only, do not set it manualy
		/// </summary>
		public PointF LocalPosition
		{
			get { return _area.Location; }
			set
			{
				if (_area.Location != value)
				{
					_area.Location = value;
					if (Overlay != null && Overlay.Control != null)
						if (!Overlay.Control.HoldInvalidation)
							Overlay.Control.Invalidate();
				}
			}
		}

		/// <summary>
		/// ToolTip position in local coordinates
		/// </summary>
		public PointF ToolTipPosition
		{
			get
			{
				var ret = _area.Location;
				ret.Offset(-Offset.X, -Offset.Y);
				return ret;
			}
		}

		public SizeF Size
		{
			get { return _area.Size; }
			set { _area.Size = value; }
		}

		public RectangleF LocalArea
		{
			get { return _area; }
		}

		public virtual GMapToolTip ToolTip
		{
			get { return _toolTip; }
		}

		public MarkerTooltipMode ToolTipMode
		{
			get { return _toolTipMode; }
		}

		public virtual string ToolTipText
		{
			get { return _toolTipText; }
			set
			{
				if (ToolTip == null && !string.IsNullOrEmpty(value))
				{
#if !PocketPC
					_toolTip = new GMapRoundedToolTip(this);
#else
					_toolTip = new GMapToolTip(this);
#endif
				}
				_toolTipText = value;
			}
		}

		/// <summary>
		/// Is marker visible
		/// </summary>
		public bool IsVisible
		{
			get { return Visible; }
			set
			{
				if (value != Visible)
				{
					Visible = value;
					if (Overlay != null && Overlay.Control != null)
					{
						if (Visible)
							Overlay.Control.UpdateMarkerLocalPosition(this);
						else
						{
							if (Overlay.Control.IsMouseOverMarker)
							{
								Overlay.Control.IsMouseOverMarker = false;
#if !PocketPC
								Overlay.Control.RestoreCursorOnLeave();
#endif
							}
						}
						{
							if (!Overlay.Control.HoldInvalidation)
								Overlay.Control.Invalidate();
						}
					}
				}
			}
		}

		/// <summary>
		/// If true, marker will be rendered even if it's outside current view
		/// </summary>
		public bool DisableRegionCheck { get; set; }

		/// <summary>
		/// can maker receive input
		/// </summary>
		public bool IsHitTestVisible
		{
			get { return _isHitTestVisible; }
			set { _isHitTestVisible = value; }
		}

		/// <summary>
		/// Is mouse over marker
		/// </summary>
		public bool IsMouseOver
		{
			get { return _isMouseOver; }
			internal set { _isMouseOver = value; }
		}

		protected GMapMarker(PointLatLng pos)
		{
			Position = pos;
		}

		public virtual void OnRender(Graphics g)
		{
			//
		}

		public virtual void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				Tag = null;
				if (ToolTip != null)
				{
					_toolTipText = null;
					ToolTip.Dispose();
					_toolTip = null;
				}
			}
		}

#if PocketPC
		protected void DrawImageUnscaled(Graphics g, Bitmap inBmp, int x, int y)
		{
			g.DrawImage(inBmp, new Rectangle(x, y, inBmp.Width, inBmp.Height), 0, 0, inBmp.Width, inBmp.Height, GraphicsUnit.Pixel, attr);
		}
#endif
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
			info.AddValue("Position", Position);
			info.AddValue("Tag", Tag);
			info.AddValue("Offset", Offset);
			info.AddValue("Area", _area);
			info.AddValue("ToolTip", ToolTip);
			info.AddValue("ToolTipMode", ToolTipMode);
			info.AddValue("ToolTipText", ToolTipText);
			info.AddValue("Visible", IsVisible);
			info.AddValue("DisableregionCheck", DisableRegionCheck);
			info.AddValue("IsHitTestVisible", IsHitTestVisible);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GMapMarker"/> class.
		/// </summary>
		/// <param name="info">The info.</param>
		/// <param name="context">The context.</param>
		protected GMapMarker(SerializationInfo info, StreamingContext context)
		{
			Position = Extensions.GetStruct(info, "Position", PointLatLng.Empty);
			Tag = Extensions.GetValue<object>(info, "Tag", null);
			Offset = Extensions.GetStruct(info, "Offset", Point.Empty);
			_area = Extensions.GetStruct(info, "Area", Rectangle.Empty);

			_toolTip = Extensions.GetValue<GMapToolTip>(info, "ToolTip", null);
			if (ToolTip != null) ToolTip.Marker = this;

			_toolTipMode = Extensions.GetStruct(info, "ToolTipMode", MarkerTooltipMode.OnMouseOver);
			ToolTipText = info.GetString("ToolTipText");
			IsVisible = info.GetBoolean("Visible");
			DisableRegionCheck = info.GetBoolean("DisableregionCheck");
			IsHitTestVisible = info.GetBoolean("IsHitTestVisible");
		}

		#endregion

#endif
	}
}