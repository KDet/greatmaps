using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using System.Threading;
using GMap.NET;
using GMap.NET.WindowsForms;
using OlapFramework.Data;


namespace OlapFormsFramework.Windows.Forms.Grid.Scatter
{
	[Serializable]
	public class OlapMapCircleMarker : GMapMarker
	{
		// Колір лінії якою перекреслюється "кружок" у випадку якщо одна з мір для нього інтерпольована
		private static readonly Color COLOR_INTERPOLATED_LINE = Color.Black;
		// Pen яким буде намальована  лінія навколо "кружка"
		private static readonly Pen PEN_ITEM_LINE = Pens.Black;
		// Pen яким перекреслюється "кружок" у випадку якщо одна з мір для нього інтерпольована
		private static readonly Pen PEN_INTERPOLATED_LINE = new Pen(COLOR_INTERPOLATED_LINE);
		// При підсвітці елементу означає кількість пікселів між краєм елементу і краєм підсвітки (ширина круга)
		private const float ITEM_HIGHLIGHT_WIDTH = 7;
		// При підсвітці поточного елементу - колір крайніх точок обідка що прилягають до краю елементу
		private static readonly Color COLOR_HIGHLIGHT_DARK = Color.Black;
		// При підсвітці поточного елементу - колір крайніх точок обідка що розміщені якнайдалі від краю елементу
		private static readonly Color COLOR_HIGHLIGHT_SURROUND = Color.Transparent;
		private static readonly Color COLOR_DEFAULT_MARKER = Color.LightGray;

		private int _borderAlpha = 255;
		//unknowledge url https://msdn.microsoft.com/ru-ru/library/system.threading.lazythreadsafetymode(v=vs.110).aspx
		private readonly Lazy<OlapMapToolTip> _toolTipLazy;

		// Малює елемент по aGraphics
		private void ItemDraw(Graphics aGraphics)
		{
			var diameter = Size.Width;
			var radius = diameter / 2f;			 
			using (var brush = new SolidBrush(MarkerColor))
				aGraphics.FillEllipse(brush, LocalPosition.X - radius, LocalPosition.Y - radius, diameter, diameter);
			if (IsInterpolated)
			{
                var diagonal = (float)Math.Sqrt(1f / 2) * radius;
				aGraphics.DrawLine(BorderAlpha >= 255 ? PEN_INTERPOLATED_LINE : new Pen(Color.FromArgb(BorderAlpha, COLOR_INTERPOLATED_LINE)), LocalPosition.X - diagonal, LocalPosition.Y + diagonal, LocalPosition.X + diagonal, LocalPosition.Y - diagonal);
			}
			aGraphics.DrawEllipse(BorderAlpha >= 255 ? PEN_ITEM_LINE : new Pen(Color.FromArgb(BorderAlpha, PEN_ITEM_LINE.Color)), LocalPosition.X - radius, LocalPosition.Y - radius, diameter, diameter);
		}
		/// <summary>
		/// Підсвічує елемент відмальовуючи його разом з підсвіткою по <paramref name="aGraphics"/>
		/// </summary>
		private void ItemHighlight(Graphics aGraphics, Rectangle? aItemsArea)
		{
			if (!IsVisible)
				return;
			var diameter = Size.Width;
			var radius = diameter / 2f;
            var bounds = new RectangleF(LocalPosition.X - radius, LocalPosition.Y - radius, diameter, diameter);
			var itemSizePercent = bounds.Width / (bounds.Width + 2 * ITEM_HIGHLIGHT_WIDTH);
			var tmp = bounds.Width / itemSizePercent - bounds.Width;
			var highlightBounds = bounds;
			highlightBounds.Inflate(tmp / 2, tmp / 2);
			using (var circle = new GraphicsPath())
			{
				circle.AddEllipse(bounds);
				var oldClip = aGraphics.Clip;
				if (aItemsArea != null)
					aGraphics.IntersectClip(aItemsArea.Value);
                using (var commonClip = aGraphics.Clip)
				{
					using (var circleClip = new Region(circle))
						aGraphics.ExcludeClip(circleClip);
					using (var pth = new GraphicsPath())
					{
						pth.AddEllipse(highlightBounds);
						using (var pgb = new PathGradientBrush(pth))
						{
							pgb.CenterColor = COLOR_HIGHLIGHT_DARK;
							pgb.SurroundColors = new Color[] { COLOR_HIGHLIGHT_SURROUND };
							pgb.FocusScales = new PointF(itemSizePercent, itemSizePercent);
							pgb.CenterPoint = new PointF(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
							aGraphics.FillEllipse(pgb, highlightBounds);
						}
					}
					aGraphics.Clip = commonClip;
					ItemDraw(aGraphics);
				}
				aGraphics.Clip = oldClip;
			}
		}
		private static OlapMapToolTip CreateToolTip(OlapMapCircleMarker aMarker)
		{
			//If tooltip is manually set up, return tooltip, else generate new one and return it
			var res = aMarker._toolTip as OlapMapToolTip ?? new OlapMapToolTip(aMarker);
			aMarker._toolTip = res;
			return res;
		}

		private Rectangle? DrawArea
		{
			get { return Overlay?.Control?.DrawArea; }
		}
		private OlapScatter Control
		{
			get { return Overlay?.Control; }
		}

		public OlapMapCircleMarker(
			PointLatLng aPoint, 
			bool aVisible = true)
			: base(aPoint)
		{
			MarkerColor = COLOR_DEFAULT_MARKER;
			Size = new SizeF(ITEM_HIGHLIGHT_WIDTH, ITEM_HIGHLIGHT_WIDTH);
			IsInterpolated = false;
			Visible = aVisible;
			_toolTipMode = MarkerTooltipMode.Always;
			_toolTipLazy = new Lazy<OlapMapToolTip>(() => CreateToolTip(this), LazyThreadSafetyMode.PublicationOnly);
		}

		public override void OnRender(Graphics aGraphics)
		{
	        if (IsHighlighted)
	            ItemHighlight(aGraphics, DrawArea);
	        else
	            ItemDraw(aGraphics);
	    }

		public int PageID { get; set; }
		public int ItemID { get; set; }

		public bool IsHighlighted { get; set; }
	    public int BorderAlpha
	    {
	        get { return _borderAlpha; }
	        set { _borderAlpha = value; }
	    }
	    public Color MarkerColor { get; set; }
        public bool IsInterpolated { get; set; }

		public override string ToolTipText
		{
			get { return _toolTipText; }
			set { _toolTipText = value; }
		}
		public OlapMapToolTip OlapToolTip
		{
			get { return _toolTipLazy.Value; }
		}	 
		//public override GMapToolTip ToolTip
		//{
		//	get { return _toolTipLazy.IsValueCreated ? _toolTipLazy.Value : null; }
		//}
		public new OlapMapOverlay Overlay
		{
			get { return (OlapMapOverlay)_overlay; }
			private set { _overlay = value; }
		}
		public new IMarkerData Tag
		{
			get { return base.Tag as IMarkerData; }
			set { base.Tag = value; }
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
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue(nameof(PageID), PageID);
			info.AddValue(nameof(ItemID), ItemID);
			info.AddValue(nameof(IsHighlighted), IsHighlighted);
			info.AddValue(nameof(BorderAlpha), BorderAlpha);
			info.AddValue(nameof(MarkerColor), MarkerColor);
			info.AddValue(nameof(IsInterpolated), IsInterpolated);

			//info.AddValue("Tag", Tag); //TODO: implement tag
			info.AddValue("ToolTip", ToolTip);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GMapMarker"/> class.
		/// </summary>
		/// <param name="info">The info.</param>
		/// <param name="context">The context.</param>
		protected OlapMapCircleMarker(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			//Tag = Extensions.GetValue<IMarkerData>(info, "Tag", null); //TODO: implement tag		
			//_toolTip = Extensions.GetValue<GMapToolTip>(info, "ToolTip", null); //TODO: override 
			//if (ToolTip != null)
			//	ToolTip.Marker = this;
			//ToolTipText = info.GetString("ToolTipText");
		}
		#endregion
#endif
	}
}