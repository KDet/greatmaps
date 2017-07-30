using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;

namespace GMap.NET.WindowsForms
{
	/// <summary>
	/// GMap.NET marker
	/// </summary>
	[Serializable]
#if !PocketPC
	public class GMapToolTip : ISerializable, IDisposable
#else
   public class GMapToolTip: IDisposable
#endif
	{
		public static readonly StringFormat DefaultFormat = new StringFormat();
#if !PocketPC
		public static readonly Font DefaultFont = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold, GraphicsUnit.Pixel);
#else
		public static readonly Font DefaultFont = new Font(FontFamily.GenericSansSerif, 6, FontStyle.Bold);
#endif
#if !PocketPC
		public static readonly Pen DefaultStroke = new Pen(Color.FromArgb(140, Color.MidnightBlue));
#else
      public static readonly Pen DefaultStroke = new Pen(Color.MidnightBlue);
#endif
#if !PocketPC
		public static readonly Brush DefaultFill = new SolidBrush(Color.FromArgb(222, Color.AliceBlue));
#else
		public static readonly Brush DefaultFill = new System.Drawing.SolidBrush(Color.AliceBlue);
#endif
		public static readonly Brush DefaultForeground = new SolidBrush(Color.Navy);

		[NonSerialized] private readonly StringFormat _format = DefaultFormat;
		[NonSerialized] private Font _font = DefaultFont;
		[NonSerialized] private Pen _stroke = DefaultStroke;
		[NonSerialized] private Brush _fill = DefaultFill;
		[NonSerialized] private Brush _foreground = DefaultForeground;
		private Size _textPadding = new Size(10, 10);
		private bool _disposed;

		protected GMapMarker _marker;

		public GMapMarker Marker
		{
			get { return _marker; }
			internal set { _marker = value; }
		}

		public Point Offset { get; set; }

		public StringFormat Format
		{
			get { return _format; }
		}

		public Font Font
		{
			get { return _font; }
			set { _font = value; }
		}

		/// <summary>
		/// Specifies how the outline is painted
		/// </summary>		
		public Pen Stroke
		{
			get { return _stroke; }
			set { _stroke = value; }
		}

		/// <summary>
		/// Background color
		/// </summary>	
		public Brush Fill
		{
			get { return _fill; }
			set { _fill = value; }
		}

		/// <summary>
		/// Text foreground
		/// </summary>
		public Brush Foreground
		{
			get { return _foreground; }
			set { _foreground = value; }
		}

		/// <summary>
		/// Text padding
		/// </summary>
		public Size TextPadding
		{
			get { return _textPadding; }
			set { _textPadding = value; }
		}

		static GMapToolTip()
		{
			DefaultStroke.Width = 2;
#if !PocketPC
			DefaultStroke.LineJoin = LineJoin.Round;
			DefaultStroke.StartCap = LineCap.RoundAnchor;
#endif
#if !PocketPC
			DefaultFormat.LineAlignment = StringAlignment.Center;
#endif
			DefaultFormat.Alignment = StringAlignment.Center;
		}

		public GMapToolTip(GMapMarker marker)
		{
			Marker = marker;
			Offset = new Point(14, -44);
		}

		public virtual void OnRender(Graphics g)
		{
			var st = g.MeasureString(Marker.ToolTipText, Font).ToSize();
			var rect = new RectangleF(Marker.ToolTipPosition.X, Marker.ToolTipPosition.Y - st.Height,
				st.Width + TextPadding.Width, st.Height + TextPadding.Height);
			rect.Offset(Offset.X, Offset.Y);
			g.DrawLine(Stroke, Marker.ToolTipPosition.X, Marker.ToolTipPosition.Y, rect.X, rect.Y + rect.Height/2);
			g.FillRectangle(Fill, rect);
			g.DrawRectangle(Stroke, rect.X, rect.Y, rect.Width, rect.Height);
#if PocketPC
			rect.Offset(0, (rect.Height - st.Height) / 2);
#endif
			g.DrawString(Marker.ToolTipText, Font, Foreground, rect, Format);
		}

		public void Dispose()
		{
			if (!_disposed)
				_disposed = true;
		}

#if !PocketPC

		#region ISerializable Members

		/// <summary>
		/// Initializes a new instance of the <see cref="GMapToolTip"/> class.
		/// </summary>
		/// <param name="info">The info.</param>
		/// <param name="context">The context.</param>
		protected GMapToolTip(SerializationInfo info, StreamingContext context)
		{
			Offset = Extensions.GetStruct(info, "Offset", Point.Empty);
			TextPadding = Extensions.GetStruct(info, "TextPadding", new Size(10, 10));
		}

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
			info.AddValue("Offset", Offset);
			info.AddValue("TextPadding", TextPadding);
		}

		#endregion

#endif
	}
}