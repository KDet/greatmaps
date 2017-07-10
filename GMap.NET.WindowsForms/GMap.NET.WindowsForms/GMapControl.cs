
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using GMap.NET.Internals;
using GMap.NET.MapProviders;
using GMap.NET.ObjectModel;
using GMap.NET.Projections;

namespace GMap.NET.WindowsForms
{
#if !PocketPC

#else
   using OpenNETCF.ComponentModel;
#endif

	/// <summary>
	/// GMap.NET control for Windows Forms
	/// </summary>   
	public class GMapControl : UserControl, Interface
	{
#if !PocketPC
		/// <summary>
		/// occurs when clicked on marker
		/// </summary>
		public event MarkerClick OnMarkerClick;

		/// <summary>
		/// occurs when clicked on polygon
		/// </summary>
		public event PolygonClick OnPolygonClick;

		/// <summary>
		/// occurs when clicked on route
		/// </summary>
		public event RouteClick OnRouteClick;

		/// <summary>
		/// occurs on mouse enters route area
		/// </summary>
		public event RouteEnter OnRouteEnter;

		/// <summary>
		/// occurs on mouse leaves route area
		/// </summary>
		public event RouteLeave OnRouteLeave;

		/// <summary>
		/// occurs when mouse selection is changed
		/// </summary>        
		public event SelectionChange OnSelectionChange;
#endif

		/// <summary>
		/// occurs on mouse enters marker area
		/// </summary>
		public event MarkerEnter OnMarkerEnter;

		/// <summary>
		/// occurs on mouse leaves marker area
		/// </summary>
		public event MarkerLeave OnMarkerLeave;

		/// <summary>
		/// occurs on mouse enters Polygon area
		/// </summary>
		public event PolygonEnter OnPolygonEnter;

		/// <summary>
		/// occurs on mouse leaves Polygon area
		/// </summary>
		public event PolygonLeave OnPolygonLeave;

		/// <summary>
		/// list of overlays, should be thread safe
		/// </summary>
		public readonly ObservableCollectionThreadSafe<GMapOverlay> Overlays =
			new ObservableCollectionThreadSafe<GMapOverlay>();

		/// <summary>
		/// max zoom
		/// </summary>         
		[Category("GMap.NET")]
		[Description("maximum zoom level of map")]
		public int MaxZoom
		{
			get { return Core.maxZoom; }
			set { Core.maxZoom = value; }
		}

		/// <summary>
		/// min zoom
		/// </summary>      
		[Category("GMap.NET")]
		[Description("minimum zoom level of map")]
		public int MinZoom
		{
			get { return Core.minZoom; }
			set { Core.minZoom = value; }
		}

		/// <summary>
		/// map zooming type for mouse wheel
		/// </summary>
		[Category("GMap.NET")]
		[Description("map zooming type for mouse wheel")]
		public MouseWheelZoomType MouseWheelZoomType
		{
			get { return Core.MouseWheelZoomType; }
			set { Core.MouseWheelZoomType = value; }
		}

		/// <summary>
		/// enable map zoom on mouse wheel
		/// </summary>
		[Category("GMap.NET")]
		[Description("enable map zoom on mouse wheel")]
		public bool MouseWheelZoomEnabled
		{
			get { return Core.MouseWheelZoomEnabled; }
			set { Core.MouseWheelZoomEnabled = value; }
		}

		/// <summary>
		/// text on empty tiles
		/// </summary>
		public string EmptyTileText = "We are sorry, but we don't\nhave imagery at this zoom\nlevel for this region.";

		/// <summary>
		/// pen for empty tile borders
		/// </summary>
#if !PocketPC
		public Pen EmptyTileBorders = new Pen(Brushes.White, 1);
#else
      public Pen EmptyTileBorders = new Pen(Color.White, 1);
#endif

		public bool ShowCenter = true;

		/// <summary>
		/// pen for scale info
		/// </summary>
#if !PocketPC
		public Pen ScalePen = new Pen(Brushes.Blue, 1);

		public Pen CenterPen = new Pen(Brushes.Red, 1);
#else
      public Pen ScalePen = new Pen(Color.Blue, 1);
      public Pen CenterPen = new Pen(Color.Red, 1);
#endif

#if !PocketPC
		/// <summary>
		/// area selection pen
		/// </summary>
		public Pen SelectionPen = new Pen(Brushes.Blue, 2);

		private Brush _selectedAreaFill = new SolidBrush(Color.FromArgb(33, Color.RoyalBlue));
		private Color _selectedAreaFillColor = Color.FromArgb(33, Color.RoyalBlue);

		/// <summary>
		/// background of selected area
		/// </summary>
		[Category("GMap.NET")]
		[Description("background color od the selected area")]
		public Color SelectedAreaFillColor
		{
			get { return _selectedAreaFillColor; }
			set
			{
				if (_selectedAreaFillColor != value)
				{
					_selectedAreaFillColor = value;

					if (_selectedAreaFill != null)
					{
						_selectedAreaFill.Dispose();
						_selectedAreaFill = null;
					}
					_selectedAreaFill = new SolidBrush(_selectedAreaFillColor);
				}
			}
		}

		private HelperLineOptions _helperLineOption = HelperLineOptions.DontShow;

		/// <summary>
		/// draw lines at the mouse pointer position
		/// </summary>
		[Browsable(false)]
		public HelperLineOptions HelperLineOption
		{
			get { return _helperLineOption; }
			set
			{
				_helperLineOption = value;
				_renderHelperLine = (_helperLineOption == HelperLineOptions.ShowAlways);
				if (Core.IsStarted)
				{
					Invalidate();
				}
			}
		}

		public Pen HelperLinePen = new Pen(Color.Blue, 1);
		private bool _renderHelperLine;

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (HelperLineOption == HelperLineOptions.ShowOnModifierKey)
			{
				_renderHelperLine = (e.Modifiers == Keys.Shift || e.Modifiers == Keys.Alt);
				if (_renderHelperLine)
				{
					Invalidate();
				}
			}
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);

			if (HelperLineOption == HelperLineOptions.ShowOnModifierKey)
			{
				_renderHelperLine = (e.Modifiers == Keys.Shift || e.Modifiers == Keys.Alt);
				if (!_renderHelperLine)
				{
					Invalidate();
				}
			}
		}
#endif

		private Brush _emptytileBrush = new SolidBrush(Color.Navy);
		private Color _emptyTileColor = Color.Navy;

		/// <summary>
		/// color of empty tile background
		/// </summary>
		[Category("GMap.NET")]
		[Description("background color of the empty tile")]
		public Color EmptyTileColor
		{
			get { return _emptyTileColor; }
			set
			{
				if (_emptyTileColor != value)
				{
					_emptyTileColor = value;

					if (_emptytileBrush != null)
					{
						_emptytileBrush.Dispose();
						_emptytileBrush = null;
					}
					_emptytileBrush = new SolidBrush(_emptyTileColor);
				}
			}
		}

#if PocketPC
      readonly Brush TileGridLinesTextBrush = new SolidBrush(Color.Red);
      readonly Brush TileGridMissingTextBrush = new SolidBrush(Color.White);
      readonly Brush CopyrightBrush = new SolidBrush(Color.Navy);
#endif

		/// <summary>
		/// show map scale info
		/// </summary>
		public bool MapScaleInfoEnabled = false;

		/// <summary>
		/// enables filling empty tiles using lower level images
		/// </summary>
		public bool FillEmptyTiles = true;

		/// <summary>
		/// if true, selects area just by holding mouse and moving
		/// </summary>
		public bool DisableAltForSelection = false;

		/// <summary>
		/// retry count to get tile 
		/// </summary>
		[Browsable(false)]
		public int RetryLoadTile
		{
			get { return Core.RetryLoadTile; }
			set { Core.RetryLoadTile = value; }
		}

		/// <summary>
		/// how many levels of tiles are staying decompresed in memory
		/// </summary>
		[Browsable(false)]
		public int LevelsKeepInMemmory
		{
			get { return Core.LevelsKeepInMemmory; }

			set { Core.LevelsKeepInMemmory = value; }
		}

		/// <summary>
		/// map dragg button
		/// </summary>
		[Category("GMap.NET")] public MouseButtons DragButton = MouseButtons.Right;

		private bool _showTileGridLines;

		/// <summary>
		/// shows tile gridlines
		/// </summary>
		[Category("GMap.NET")]
		[Description("shows tile gridlines")]
		public bool ShowTileGridLines
		{
			get { return _showTileGridLines; }
			set
			{
				_showTileGridLines = value;
				Invalidate();
			}
		}

		/// <summary>
		/// current selected area in map
		/// </summary>
		private RectLatLng _selectedArea;

		[Browsable(false)]
		public RectLatLng SelectedArea
		{
			get { return _selectedArea; }
			set
			{
				_selectedArea = value;

				if (Core.IsStarted)
				{
					Invalidate();
				}
			}
		}

		/// <summary>
		/// map boundaries
		/// </summary>
		public RectLatLng? BoundsOfMap = null;

		/// <summary>
		/// enables integrated DoubleBuffer for running on windows mobile
		/// </summary>
#if !PocketPC
		public bool ForceDoubleBuffer;

		private readonly bool _mobileMode = false;
#else
      readonly bool ForceDoubleBuffer = true;
#endif

		/// <summary>
		/// stops immediate marker/route/polygon invalidations;
		/// call Refresh to perform single refresh and reset invalidation state
		/// </summary>
		public bool HoldInvalidation;

		/// <summary>
		/// call this to stop HoldInvalidation and perform single forced instant refresh 
		/// </summary>
		public override void Refresh()
		{
			HoldInvalidation = false;

			lock (Core.invalidationLock)
			{
				Core.lastInvalidation = DateTime.Now;
			}

			base.Refresh();
		}

#if !DESIGN
		/// <summary>
		/// enque built-in thread safe invalidation
		/// </summary>
		public void Invalidate()
		{
			if (Core.Refresh != null)
			{
				Core.Refresh.Set();
			}
		}
#endif

#if !PocketPC
		private bool _grayScale;

		[Category("GMap.NET")]
		public bool GrayScaleMode
		{
			get { return _grayScale; }
			set
			{
				_grayScale = value;
				ColorMatrix = (value ? ColorMatrixs.GrayScale : null);
			}
		}

		private bool _negative;

		[Category("GMap.NET")]
		public bool NegativeMode
		{
			get { return _negative; }
			set
			{
				_negative = value;
				ColorMatrix = (value ? ColorMatrixs.Negative : null);
			}
		}

		private ColorMatrix _colorMatrix;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public ColorMatrix ColorMatrix
		{
			get { return _colorMatrix; }
			set
			{
				_colorMatrix = value;
				if (GMapProvider.TileImageProxy != null && GMapProvider.TileImageProxy is GMapImageProxy)
				{
					(GMapProvider.TileImageProxy as GMapImageProxy).ColorMatrix = value;
					if (Core.IsStarted)
					{
						ReloadMap();
					}
				}
			}
		}
#endif

		// internal stuff
		internal readonly Core Core = new Core();

		internal readonly Font CopyrightFont = new Font(FontFamily.GenericSansSerif, 7, FontStyle.Regular);
#if !PocketPC
		internal readonly Font MissingDataFont = new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold);
#else
      internal readonly Font MissingDataFont = new Font(FontFamily.GenericSansSerif, 8, FontStyle.Regular);
#endif
		private Font _scaleFont = new Font(FontFamily.GenericSansSerif, 5, FontStyle.Italic);
		internal readonly StringFormat CenterFormat = new StringFormat();
		internal readonly StringFormat BottomFormat = new StringFormat();
#if !PocketPC
		private readonly ImageAttributes _tileFlipXyAttributes = new ImageAttributes();
#endif
		private double _zoomReal;
		private Bitmap _backBuffer;
		private Graphics _gxOff;

#if !DESIGN
		/// <summary>
		/// construct
		/// </summary>
		public GMapControl()
		{
#if !PocketPC
			if (!IsDesignerHosted)
#endif
			{
#if !PocketPC
				SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
				SetStyle(ControlStyles.AllPaintingInWmPaint, true);
				SetStyle(ControlStyles.UserPaint, true);
				SetStyle(ControlStyles.Opaque, true);
				ResizeRedraw = true;

				_tileFlipXyAttributes.SetWrapMode(WrapMode.TileFlipXY);

				// only one mode will be active, to get mixed mode create new ColorMatrix
				GrayScaleMode = GrayScaleMode;
				NegativeMode = NegativeMode;
#endif
				Core.SystemType = "WindowsForms";

				RenderMode = RenderMode.GDI_PLUS;

				CenterFormat.Alignment = StringAlignment.Center;
				CenterFormat.LineAlignment = StringAlignment.Center;

				BottomFormat.Alignment = StringAlignment.Center;

#if !PocketPC
				BottomFormat.LineAlignment = StringAlignment.Far;
#endif

				if (GMaps.Instance.IsRunningOnMono)
				{
					// no imports to move pointer
					MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
				}

				Overlays.CollectionChanged += Overlays_CollectionChanged;
			}
		}

#endif

		static GMapControl()
		{
#if !PocketPC
			if (!IsDesignerHosted)
#endif
			{
				GMapImageProxy.Enable();
#if !PocketPC
				GMaps.Instance.SQLitePing();
#endif
			}
		}

		private void Overlays_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (GMapOverlay obj in e.NewItems)
				{
					if (obj != null)
					{
						obj.Control = this;
					}
				}

				if (Core.IsStarted && !HoldInvalidation)
				{
					Invalidate();
				}
			}
		}

		private void InvalidatorEngage(object sender, ProgressChangedEventArgs e)
		{
			base.Invalidate();
		}

		/// <summary>
		/// update objects when map is draged/zoomed
		/// </summary>
		internal void ForceUpdateOverlays()
		{
			try
			{
				HoldInvalidation = true;

				foreach (var o in Overlays)
				{
					if (o.IsVisibile)
					{
						o.ForceUpdate();
					}
				}
			}
			finally
			{
				Refresh();
			}
		}

		/// <summary>
		/// updates markers local position
		/// </summary>
		/// <param name="marker"></param>
		public void UpdateMarkerLocalPosition(GMapMarker marker)
		{
			var p = FromLatLngToLocal(marker.Position);
			{
#if !PocketPC
				if (!_mobileMode)
				{
					p.OffsetNegative(Core.renderOffset);
				}
#endif
				marker.LocalPosition = new Point((int) (p.X + marker.Offset.X), (int) (p.Y + marker.Offset.Y));
			}
		}

		/// <summary>
		/// updates routes local position
		/// </summary>
		/// <param name="route"></param>
		public void UpdateRouteLocalPosition(GMapRoute route)
		{
			route.LocalPoints.Clear();

			for (var i = 0; i < route.Points.Count; i++)
			{
				var p = FromLatLngToLocal(route.Points[i]);

#if !PocketPC
				if (!_mobileMode)
				{
					p.OffsetNegative(Core.renderOffset);
				}
#endif
				route.LocalPoints.Add(p);
			}
#if !PocketPC
			route.UpdateGraphicsPath();
#endif
		}

		/// <summary>
		/// updates polygons local position
		/// </summary>
		/// <param name="polygon"></param>
		public void UpdatePolygonLocalPosition(GMapPolygon polygon)
		{
			polygon.LocalPoints.Clear();

			for (var i = 0; i < polygon.Points.Count; i++)
			{
				var p = FromLatLngToLocal(polygon.Points[i]);
#if !PocketPC
				if (!_mobileMode)
				{
					p.OffsetNegative(Core.renderOffset);
				}
#endif
				polygon.LocalPoints.Add(p);
			}
#if !PocketPC
			polygon.UpdateGraphicsPath();
#endif
		}

		/// <summary>
		/// sets zoom to max to fit rect
		/// </summary>
		/// <param name="rect"></param>
		/// <returns></returns>
		public bool SetZoomToFitRect(RectLatLng rect)
		{
			if (_lazyEvents)
			{
				_lazySetZoomToFitRect = rect;
			}
			else
			{
				var maxZoom = Core.GetMaxZoomToFitRect(rect);
				if (maxZoom > 0)
				{
					var center = new PointLatLng(rect.Lat - (rect.HeightLat/2), rect.Lng + (rect.WidthLng/2));
					Position = center;

					if (maxZoom > MaxZoom)
					{
						maxZoom = MaxZoom;
					}

					if ((int) Zoom != maxZoom)
					{
						Zoom = maxZoom;
					}

					return true;
				}
			}

			return false;
		}

		private RectLatLng? _lazySetZoomToFitRect;
		private bool _lazyEvents = true;

		/// <summary>
		/// sets to max zoom to fit all markers and centers them in map
		/// </summary>
		/// <param name="overlayId">overlay id or null to check all</param>
		/// <returns></returns>
		public bool ZoomAndCenterMarkers(string overlayId)
		{
			var rect = GetRectOfAllMarkers(overlayId);
			if (rect.HasValue)
			{
				return SetZoomToFitRect(rect.Value);
			}

			return false;
		}

		/// <summary>
		/// zooms and centers all route
		/// </summary>
		/// <param name="overlayId">overlay id or null to check all</param>
		/// <returns></returns>
		public bool ZoomAndCenterRoutes(string overlayId)
		{
			var rect = GetRectOfAllRoutes(overlayId);
			if (rect.HasValue)
			{
				return SetZoomToFitRect(rect.Value);
			}

			return false;
		}

		/// <summary>
		/// zooms and centers route 
		/// </summary>
		/// <param name="route"></param>
		/// <returns></returns>
		public bool ZoomAndCenterRoute(MapRoute route)
		{
			var rect = GetRectOfRoute(route);
			if (rect.HasValue)
			{
				return SetZoomToFitRect(rect.Value);
			}

			return false;
		}

		/// <summary>
		/// gets rectangle with all objects inside
		/// </summary>
		/// <param name="overlayId">overlay id or null to check all</param>
		/// <returns></returns>
		public RectLatLng? GetRectOfAllMarkers(string overlayId)
		{
			RectLatLng? ret = null;

			var left = double.MaxValue;
			var top = double.MinValue;
			var right = double.MinValue;
			var bottom = double.MaxValue;

			foreach (var o in Overlays)
			{
				if (overlayId == null || o.Id == overlayId)
				{
					if (o.IsVisibile && o.Markers.Count > 0)
					{
						foreach (var m in o.Markers)
						{
							if (m.IsVisible)
							{
								// left
								if (m.Position.Lng < left)
								{
									left = m.Position.Lng;
								}

								// top
								if (m.Position.Lat > top)
								{
									top = m.Position.Lat;
								}

								// right
								if (m.Position.Lng > right)
								{
									right = m.Position.Lng;
								}

								// bottom
								if (m.Position.Lat < bottom)
								{
									bottom = m.Position.Lat;
								}
							}
						}
					}
				}
			}

			if (left != double.MaxValue && right != double.MinValue && top != double.MinValue && bottom != double.MaxValue)
			{
				ret = RectLatLng.FromLtrb(left, top, right, bottom);
			}

			return ret;
		}

		/// <summary>
		/// gets rectangle with all objects inside
		/// </summary>
		/// <param name="overlayId">overlay id or null to check all</param>
		/// <returns></returns>
		public RectLatLng? GetRectOfAllRoutes(string overlayId)
		{
			RectLatLng? ret = null;

			var left = double.MaxValue;
			var top = double.MinValue;
			var right = double.MinValue;
			var bottom = double.MaxValue;

			foreach (var o in Overlays)
			{
				if (overlayId == null || o.Id == overlayId)
				{
					if (o.IsVisibile && o.Routes.Count > 0)
					{
						foreach (var route in o.Routes)
						{
							if (route.IsVisible && route.From.HasValue && route.To.HasValue)
							{
								foreach (var p in route.Points)
								{
									// left
									if (p.Lng < left)
									{
										left = p.Lng;
									}

									// top
									if (p.Lat > top)
									{
										top = p.Lat;
									}

									// right
									if (p.Lng > right)
									{
										right = p.Lng;
									}

									// bottom
									if (p.Lat < bottom)
									{
										bottom = p.Lat;
									}
								}
							}
						}
					}
				}
			}

			if (left != double.MaxValue && right != double.MinValue && top != double.MinValue && bottom != double.MaxValue)
			{
				ret = RectLatLng.FromLtrb(left, top, right, bottom);
			}

			return ret;
		}

		/// <summary>
		/// gets rect of route
		/// </summary>
		/// <param name="route"></param>
		/// <returns></returns>
		public RectLatLng? GetRectOfRoute(MapRoute route)
		{
			RectLatLng? ret = null;

			var left = double.MaxValue;
			var top = double.MinValue;
			var right = double.MinValue;
			var bottom = double.MaxValue;

			if (route.From.HasValue && route.To.HasValue)
			{
				foreach (var p in route.Points)
				{
					// left
					if (p.Lng < left)
					{
						left = p.Lng;
					}

					// top
					if (p.Lat > top)
					{
						top = p.Lat;
					}

					// right
					if (p.Lng > right)
					{
						right = p.Lng;
					}

					// bottom
					if (p.Lat < bottom)
					{
						bottom = p.Lat;
					}
				}
				ret = RectLatLng.FromLtrb(left, top, right, bottom);
			}
			return ret;
		}

		/// <summary>
		/// gets image of the current view
		/// </summary>
		/// <returns></returns>
		public Image ToImage()
		{
			Image ret = null;

			var r = ForceDoubleBuffer;
			try
			{
				UpdateBackBuffer();

				if (!r)
				{
#if !PocketPC
					ForceDoubleBuffer = true;
#endif
				}

				Refresh();
				Application.DoEvents();

				using (var ms = new MemoryStream())
				{
					using (var frame = (_backBuffer.Clone() as Bitmap))
					{
						frame.Save(ms, ImageFormat.Png);
					}
#if !PocketPC
					ret = Image.FromStream(ms);
#else
                    ret = new Bitmap(ms);
#endif
				}
			}
			catch (Exception)
			{
				throw;
			}
			finally
			{
				if (!r)
				{
#if !PocketPC
					ForceDoubleBuffer = false;
#endif
					ClearBackBuffer();
				}
			}
			return ret;
		}

		/// <summary>
		/// offset position in pixels
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void Offset(int x, int y)
		{
			if (IsHandleCreated)
			{
#if !PocketPC
				if (IsRotated)
				{
					Point[] p = {new Point(x, y)};
					_rotationMatrixInvert.TransformVectors(p);
					x = p[0].X;
					y = p[0].Y;
				}
#endif
				Core.DragOffset(new GPoint(x, y));

				ForceUpdateOverlays();
			}
		}

		#region UserControl Events

#if !PocketPC
		public readonly static bool IsDesignerHosted = LicenseManager.UsageMode == LicenseUsageMode.Designtime;

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (!IsDesignerHosted)
			{
				//MethodInvoker m = delegate
				//{
				//   Thread.Sleep(444);

				//OnSizeChanged(null);

				if (_lazyEvents)
				{
					_lazyEvents = false;

					if (_lazySetZoomToFitRect.HasValue)
					{
						SetZoomToFitRect(_lazySetZoomToFitRect.Value);
						_lazySetZoomToFitRect = null;
					}
				}
				Core.OnMapOpen().ProgressChanged += InvalidatorEngage;
				ForceUpdateOverlays();
				//};
				//this.BeginInvoke(m);
			}
		}
#else
	//delegate void MethodInvoker();
      bool IsHandleCreated = false;

      protected override void OnPaintBackground(PaintEventArgs e)
      {
         if(!IsHandleCreated)
         {
            IsHandleCreated = true;

            if(lazyEvents)
            {
               lazyEvents = false;

               if(lazySetZoomToFitRect.HasValue)
               {
                  SetZoomToFitRect(lazySetZoomToFitRect.Value);
                  lazySetZoomToFitRect = null;
               }
            }

            Core.OnMapOpen().ProgressChanged += new ProgressChangedEventHandler(invalidatorEngage);
            ForceUpdateOverlays();
         }
      }
#endif

#if !PocketPC
		protected override void OnCreateControl()
		{
			base.OnCreateControl();

			if (!IsDesignerHosted)
			{
				var f = ParentForm;
				if (f != null)
				{
					while (f.ParentForm != null)
					{
						f = f.ParentForm;
					}

					if (f != null)
					{
						f.FormClosing += ParentForm_FormClosing;
					}
				}
			}
		}

		private void ParentForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing)
			{
				Manager.CancelTileCaching();
			}
		}
#endif

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Core.OnMapClose();

				Overlays.CollectionChanged -= Overlays_CollectionChanged;

				foreach (var o in Overlays)
				{
					o.Dispose();
				}
				Overlays.Clear();

				_scaleFont.Dispose();
				ScalePen.Dispose();
				CenterFormat.Dispose();
				CenterPen.Dispose();
				BottomFormat.Dispose();
				CopyrightFont.Dispose();
				EmptyTileBorders.Dispose();
				_emptytileBrush.Dispose();

#if !PocketPC
				_selectedAreaFill.Dispose();
				SelectionPen.Dispose();
#endif
				ClearBackBuffer();
			}
			base.Dispose(disposing);
		}

		private PointLatLng _selectionStart;
		private PointLatLng _selectionEnd;

#if !PocketPC
		private float? _mapRenderTransform;
#endif

		public Color EmptyMapBackground = Color.WhiteSmoke;

#if !DESIGN
		protected override void OnPaint(PaintEventArgs e)
		{
			if (ForceDoubleBuffer)
			{
				if (_gxOff != null)
				{
					DrawGraphics(_gxOff);
					e.Graphics.DrawImage(_backBuffer, 0, 0);
				}
			}
			else
			{
				DrawGraphics(e.Graphics);
			}

			base.OnPaint(e);
		}

		private void DrawGraphics(Graphics g)
		{
			// render white background
			g.Clear(EmptyMapBackground);

#if !PocketPC
			if (_mapRenderTransform.HasValue)
			{
				#region -- scale --

				if (!_mobileMode)
				{
					var center = new GPoint(Width/2, Height/2);
					var delta = center;
					delta.OffsetNegative(Core.renderOffset);
					var pos = center;
					pos.OffsetNegative(delta);

					g.ScaleTransform(_mapRenderTransform.Value, _mapRenderTransform.Value, MatrixOrder.Append);
					g.TranslateTransform(pos.X, pos.Y, MatrixOrder.Append);

					DrawMap(g);
					g.ResetTransform();

					g.TranslateTransform(pos.X, pos.Y, MatrixOrder.Append);
				}
				else
				{
					DrawMap(g);
					g.ResetTransform();
				}
				OnPaintOverlays(g);

				#endregion
			}
			else
#endif
			{
#if !PocketPC
				if (IsRotated)
				{
					#region -- rotation --

					g.TextRenderingHint = TextRenderingHint.AntiAlias;
					g.SmoothingMode = SmoothingMode.AntiAlias;

					g.TranslateTransform((float) (Core.Width/2.0), (float) (Core.Height/2.0));
					g.RotateTransform(-Bearing);
					g.TranslateTransform((float) (-Core.Width/2.0), (float) (-Core.Height/2.0));

					g.TranslateTransform(Core.renderOffset.X, Core.renderOffset.Y);

					DrawMap(g);

					g.ResetTransform();
					g.TranslateTransform(Core.renderOffset.X, Core.renderOffset.Y);

					OnPaintOverlays(g);

					#endregion
				}
				else
#endif
				{
#if !PocketPC
					if (!_mobileMode)
					{
						g.TranslateTransform(Core.renderOffset.X, Core.renderOffset.Y);
					}
#endif
					DrawMap(g);
					OnPaintOverlays(g);
				}
			}
		}
#endif

		private void DrawMap(Graphics g)
		{
			if (Core.updatingBounds || MapProvider == EmptyProvider.Instance || MapProvider == null)
			{
				Debug.WriteLine("Core.updatingBounds");
				return;
			}

			Core.tileDrawingListLock.AcquireReaderLock();
			Core.Matrix.EnterReadLock();

			//g.TextRenderingHint = TextRenderingHint.AntiAlias;
			//g.SmoothingMode = SmoothingMode.AntiAlias;
			//g.CompositingQuality = CompositingQuality.HighQuality;
			//g.InterpolationMode = InterpolationMode.HighQualityBicubic;  

			try
			{
				foreach (var tilePoint in Core.tileDrawingList)
				{
					{
						Core.tileRect.Location = tilePoint.PosPixel;
						if (ForceDoubleBuffer)
						{
#if !PocketPC
							if (_mobileMode)
							{
								Core.tileRect.Offset(Core.renderOffset);
							}
#else
                            Core.tileRect.Offset(Core.renderOffset);
#endif
						}
						Core.tileRect.OffsetNegative(Core.compensationOffset);

						//if(Core.currentRegion.IntersectsWith(Core.tileRect) || IsRotated)
						{
							var found = false;

							var t = Core.Matrix.GetTileWithNoLock(Core.Zoom, tilePoint.PosXY);
							if (t.NotEmpty)
							{
								// render tile
								{
									foreach (GMapImage img in t.Overlays)
									{
										if (img != null && img.Img != null)
										{
											if (!found)
												found = true;

											if (!img.IsParent)
											{
#if !PocketPC
												if (!_mapRenderTransform.HasValue && !IsRotated)
												{
													g.DrawImage(img.Img, Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height);
												}
												else
												{
													g.DrawImage(img.Img,
														new Rectangle((int) Core.tileRect.X, (int) Core.tileRect.Y, (int) Core.tileRect.Width,
															(int) Core.tileRect.Height), 0, 0, Core.tileRect.Width, Core.tileRect.Height, GraphicsUnit.Pixel,
														_tileFlipXyAttributes);
												}
#else
                                                g.DrawImage(img.Img, (int) Core.tileRect.X, (int) Core.tileRect.Y);
#endif
											}
#if !PocketPC
											else
											{
												// TODO: move calculations to loader thread
												var srcRect = new RectangleF(img.Xoff*(img.Img.Width/img.Ix), img.Yoff*(img.Img.Height/img.Ix),
													(img.Img.Width/img.Ix), (img.Img.Height/img.Ix));
												var dst = new Rectangle((int) Core.tileRect.X, (int) Core.tileRect.Y, (int) Core.tileRect.Width,
													(int) Core.tileRect.Height);

												g.DrawImage(img.Img, dst, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, GraphicsUnit.Pixel,
													_tileFlipXyAttributes);
											}
#endif
										}
									}
								}
							}
#if !PocketPC
							else if (FillEmptyTiles && MapProvider.Projection is MercatorProjection)
							{
								#region -- fill empty lines --

								var zoomOffset = 1;
								var parentTile = Tile.Empty;
								long ix = 0;

								while (!parentTile.NotEmpty && zoomOffset < Core.Zoom && zoomOffset <= LevelsKeepInMemmory)
								{
									ix = (long) Math.Pow(2, zoomOffset);
									parentTile = Core.Matrix.GetTileWithNoLock(Core.Zoom - zoomOffset++,
										new GPoint((int) (tilePoint.PosXY.X/ix), (int) (tilePoint.PosXY.Y/ix)));
								}

								if (parentTile.NotEmpty)
								{
									var xoff = Math.Abs(tilePoint.PosXY.X - (parentTile.Pos.X*ix));
									var yoff = Math.Abs(tilePoint.PosXY.Y - (parentTile.Pos.Y*ix));

									// render tile 
									{
										foreach (GMapImage img in parentTile.Overlays)
										{
											if (img != null && img.Img != null && !img.IsParent)
											{
												if (!found)
													found = true;

												var srcRect = new RectangleF(xoff*(img.Img.Width/ix), yoff*(img.Img.Height/ix), (img.Img.Width/ix),
													(img.Img.Height/ix));
												var dst = new Rectangle((int) Core.tileRect.X, (int) Core.tileRect.Y, (int) Core.tileRect.Width,
													(int) Core.tileRect.Height);

												g.DrawImage(img.Img, dst, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, GraphicsUnit.Pixel,
													_tileFlipXyAttributes);
												g.FillRectangle(_selectedAreaFill, dst);
											}
										}
									}
								}

								#endregion
							}
#endif
							// add text if tile is missing
							if (!found)
							{
								lock (Core.FailedLoads)
								{
									var lt = new LoadTask(tilePoint.PosXY, Core.Zoom);
									if (Core.FailedLoads.ContainsKey(lt))
									{
										var ex = Core.FailedLoads[lt];
#if !PocketPC
										g.FillRectangle(_emptytileBrush,
											new RectangleF(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height));

										g.DrawString("Exception: " + ex.Message, MissingDataFont, Brushes.Red,
											new RectangleF(Core.tileRect.X + 11, Core.tileRect.Y + 11, Core.tileRect.Width - 11,
												Core.tileRect.Height - 11));

										g.DrawString(EmptyTileText, MissingDataFont, Brushes.Blue,
											new RectangleF(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height), CenterFormat);

#else
                              g.FillRectangle(EmptytileBrush, new System.Drawing.Rectangle((int) Core.tileRect.X, (int) Core.tileRect.Y, (int) Core.tileRect.Width, (int) Core.tileRect.Height));

                              g.DrawString("Exception: " + ex.Message, MissingDataFont, TileGridMissingTextBrush, new RectangleF(Core.tileRect.X + 11, Core.tileRect.Y + 11, Core.tileRect.Width - 11, Core.tileRect.Height - 11));

                              g.DrawString(EmptyTileText, MissingDataFont, TileGridMissingTextBrush, new RectangleF(Core.tileRect.X, Core.tileRect.Y + Core.tileRect.Width / 2 + (ShowTileGridLines ? 11 : -22), Core.tileRect.Width, Core.tileRect.Height), BottomFormat);
#endif

										g.DrawRectangle(EmptyTileBorders, (int) Core.tileRect.X, (int) Core.tileRect.Y, (int) Core.tileRect.Width,
											(int) Core.tileRect.Height);
									}
								}
							}

							if (ShowTileGridLines)
							{
								g.DrawRectangle(EmptyTileBorders, (int) Core.tileRect.X, (int) Core.tileRect.Y, (int) Core.tileRect.Width,
									(int) Core.tileRect.Height);
								{
#if !PocketPC
									g.DrawString((tilePoint.PosXY == Core.centerTileXYLocation ? "CENTER: " : "TILE: ") + tilePoint,
										MissingDataFont, Brushes.Red,
										new RectangleF(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height), CenterFormat);
#else
                                    g.DrawString((tilePoint.PosXY == Core.centerTileXYLocation ? "" : "TILE: ") + tilePoint, MissingDataFont, TileGridLinesTextBrush, new RectangleF(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height), CenterFormat);
#endif
								}
							}
						}
					}
				}
			}
			finally
			{
				Core.Matrix.LeaveReadLock();
				Core.tileDrawingListLock.ReleaseReaderLock();
			}
		}

		/// <summary>
		/// override, to render something more
		/// </summary>
		/// <param name="g"></param>
		protected virtual void OnPaintOverlays(Graphics g)
		{
#if !PocketPC
			g.SmoothingMode = SmoothingMode.HighQuality;
#endif
			foreach (var o in Overlays)
			{
				if (o.IsVisibile)
				{
					o.OnRender(g);
				}
			}

			// center in virtual space...
#if DEBUG
#if !PocketPC
			if (!IsRotated)
#endif
			{
				g.DrawLine(ScalePen, -20, 0, 20, 0);
				g.DrawLine(ScalePen, 0, -20, 0, 20);
#if PocketPC
              g.DrawString("debug build", CopyrightFont, CopyrightBrush, 2, CopyrightFont.Size);
#else
				g.DrawString("debug build", CopyrightFont, Brushes.Blue, 2, CopyrightFont.Height);
#endif
			}
#endif

#if !PocketPC

			if (!_mobileMode)
			{
				g.ResetTransform();
			}

			if (!SelectedArea.IsEmpty)
			{
				var p1 = FromLatLngToLocal(SelectedArea.LocationTopLeft);
				var p2 = FromLatLngToLocal(SelectedArea.LocationRightBottom);

				var x1 = p1.X;
				var y1 = p1.Y;
				var x2 = p2.X;
				var y2 = p2.Y;

				g.DrawRectangle(SelectionPen, x1, y1, x2 - x1, y2 - y1);
				g.FillRectangle(_selectedAreaFill, x1, y1, x2 - x1, y2 - y1);
			}

			if (_renderHelperLine)
			{
				var p = PointToClient(MousePosition);

				g.DrawLine(HelperLinePen, p.X, 0, p.X, Height);
				g.DrawLine(HelperLinePen, 0, p.Y, Width, p.Y);
			}
#endif
			if (ShowCenter)
			{
				g.DrawLine(CenterPen, Width/2 - 5, Height/2, Width/2 + 5, Height/2);
				g.DrawLine(CenterPen, Width/2, Height/2 - 5, Width/2, Height/2 + 5);
			}

			#region -- copyright --

			if (!string.IsNullOrEmpty(Core.provider.Copyright))
			{
#if !PocketPC
				g.DrawString(Core.provider.Copyright, CopyrightFont, Brushes.Navy, 3, Height - CopyrightFont.Height - 5);
#else
            g.DrawString(Core.provider.Copyright, CopyrightFont, CopyrightBrush, 3, Height - CopyrightFont.Size - 15);
#endif
			}

			#endregion

			#region -- draw scale --

#if !PocketPC
			if (MapScaleInfoEnabled)
			{
				if (Width > Core.pxRes5000km)
				{
					g.DrawRectangle(ScalePen, 10, 10, Core.pxRes5000km, 10);
					g.DrawString("5000Km", _scaleFont, Brushes.Blue, Core.pxRes5000km + 10, 11);
				}
				if (Width > Core.pxRes1000km)
				{
					g.DrawRectangle(ScalePen, 10, 10, Core.pxRes1000km, 10);
					g.DrawString("1000Km", _scaleFont, Brushes.Blue, Core.pxRes1000km + 10, 11);
				}
				if (Width > Core.pxRes100km && Zoom > 2)
				{
					g.DrawRectangle(ScalePen, 10, 10, Core.pxRes100km, 10);
					g.DrawString("100Km", _scaleFont, Brushes.Blue, Core.pxRes100km + 10, 11);
				}
				if (Width > Core.pxRes10km && Zoom > 5)
				{
					g.DrawRectangle(ScalePen, 10, 10, Core.pxRes10km, 10);
					g.DrawString("10Km", _scaleFont, Brushes.Blue, Core.pxRes10km + 10, 11);
				}
				if (Width > Core.pxRes1000m && Zoom >= 10)
				{
					g.DrawRectangle(ScalePen, 10, 10, Core.pxRes1000m, 10);
					g.DrawString("1000m", _scaleFont, Brushes.Blue, Core.pxRes1000m + 10, 11);
				}
				if (Width > Core.pxRes100m && Zoom > 11)
				{
					g.DrawRectangle(ScalePen, 10, 10, Core.pxRes100m, 10);
					g.DrawString("100m", _scaleFont, Brushes.Blue, Core.pxRes100m + 9, 11);
				}
			}
#endif

			#endregion
		}

#if !PocketPC
		private readonly Matrix _rotationMatrix = new Matrix();
		private readonly Matrix _rotationMatrixInvert = new Matrix();

		/// <summary>
		/// updates rotation matrix
		/// </summary>
		private void UpdateRotationMatrix()
		{
			var center = new PointF(Core.Width/2, Core.Height/2);

			_rotationMatrix.Reset();
			_rotationMatrix.RotateAt(-Bearing, center);

			_rotationMatrixInvert.Reset();
			_rotationMatrixInvert.RotateAt(-Bearing, center);
			_rotationMatrixInvert.Invert();
		}

		/// <summary>
		/// returs true if map bearing is not zero
		/// </summary>    
		[Browsable(false)]
		public bool IsRotated
		{
			get { return Core.IsRotated; }
		}

		/// <summary>
		/// bearing for rotation of the map
		/// </summary>
		[Category("GMap.NET")]
		public float Bearing
		{
			get { return Core.bearing; }
			set
			{
				if (Core.bearing != value)
				{
					var resize = Core.bearing == 0;
					Core.bearing = value;

					//if(VirtualSizeEnabled)
					//{
					// c.X += (Width - Core.vWidth) / 2;
					// c.Y += (Height - Core.vHeight) / 2;
					//}

					UpdateRotationMatrix();

					if (value != 0 && value%360 != 0)
					{
						Core.IsRotated = true;

						if (Core.tileRectBearing.Size == Core.tileRect.Size)
						{
							Core.tileRectBearing = Core.tileRect;
							Core.tileRectBearing.Inflate(1, 1);
						}
					}
					else
					{
						Core.IsRotated = false;
						Core.tileRectBearing = Core.tileRect;
					}

					if (resize)
					{
						Core.OnMapSizeChanged(Width, Height);
					}

					if (!HoldInvalidation && Core.IsStarted)
					{
						ForceUpdateOverlays();
					}
				}
			}
		}
#endif

#if !PocketPC

		/// <summary>
		/// shrinks map area, useful just for testing
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public bool VirtualSizeEnabled
		{
			get { return Core.VirtualSizeEnabled; }
			set { Core.VirtualSizeEnabled = value; }
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
#else
      protected override void OnResize(EventArgs e)
      {
         base.OnResize(e);
#endif
			if (Width == 0 || Height == 0)
			{
				Debug.WriteLine("minimized");
				return;
			}

			if (Width == Core.Width && Height == Core.Height)
			{
				Debug.WriteLine("maximized");
				return;
			}

#if !PocketPC
			if (!IsDesignerHosted)
#endif
			{
				if (ForceDoubleBuffer)
				{
					UpdateBackBuffer();
				}

#if !PocketPC
				if (VirtualSizeEnabled)
				{
					Core.OnMapSizeChanged(Core.vWidth, Core.vHeight);
				}
				else
#endif
				{
					Core.OnMapSizeChanged(Width, Height);
				}
				//Core.currentRegion = new GRect(-50, -50, Core.Width + 50, Core.Height + 50);

				if (Visible && IsHandleCreated && Core.IsStarted)
				{
#if !PocketPC
					if (IsRotated)
					{
						UpdateRotationMatrix();
					}
#endif
					ForceUpdateOverlays();
				}
			}
		}

		private void UpdateBackBuffer()
		{
			ClearBackBuffer();

			_backBuffer = new Bitmap(Width, Height);
			_gxOff = Graphics.FromImage(_backBuffer);
		}

		private void ClearBackBuffer()
		{
			if (_backBuffer != null)
			{
				_backBuffer.Dispose();
				_backBuffer = null;
			}
			if (_gxOff != null)
			{
				_gxOff.Dispose();
				_gxOff = null;
			}
		}

		private bool _isSelected;

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (!IsMouseOverMarker)
			{
#if !PocketPC
				if (e.Button == DragButton && CanDragMap)
#else
            if(CanDragMap)
#endif
				{
#if !PocketPC
					Core.mouseDown = ApplyRotationInversion(e.X, e.Y);
#else
               Core.mouseDown = new GPoint(e.X, e.Y);
#endif
					Invalidate();
				}
				else if (!_isSelected)
				{
					_isSelected = true;
					SelectedArea = RectLatLng.Empty;
					_selectionEnd = PointLatLng.Empty;
					_selectionStart = FromLocalToLatLng(e.X, e.Y);
				}
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			if (_isSelected)
			{
				_isSelected = false;
			}

			if (Core.IsDragging)
			{
				if (_isDragging)
				{
					_isDragging = false;
					Debug.WriteLine("IsDragging = " + _isDragging);
#if !PocketPC
					Cursor = _cursorBefore;
					_cursorBefore = null;
#endif
				}
				Core.EndDrag();

				if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
				{
					if (Core.LastLocationInBounds.HasValue)
					{
						Position = Core.LastLocationInBounds.Value;
					}
				}
			}
			else
			{
#if !PocketPC
				if (e.Button == DragButton)
				{
					Core.mouseDown = GPoint.Empty;
				}

				if (!_selectionEnd.IsEmpty && !_selectionStart.IsEmpty)
				{
					var zoomtofit = false;

					if (!SelectedArea.IsEmpty && ModifierKeys == Keys.Shift)
					{
						zoomtofit = SetZoomToFitRect(SelectedArea);
					}

					if (OnSelectionChange != null)
					{
						OnSelectionChange(SelectedArea, zoomtofit);
					}
				}
				else
				{
					Invalidate();
				}
#endif
			}
		}

#if !PocketPC
		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);

			if (!Core.IsDragging)
			{
				for (var i = Overlays.Count - 1; i >= 0; i--)
				{
					var o = Overlays[i];
					if (o != null && o.IsVisibile)
					{
						foreach (var m in o.Markers)
						{
							if (m.IsVisible && m.IsHitTestVisible)
							{
								#region -- check --

								var rp = new GPoint(e.X, e.Y);
#if !PocketPC
								if (!_mobileMode)
								{
									rp.OffsetNegative(Core.renderOffset);
								}
#endif
								if (m.LocalArea.Contains((int) rp.X, (int) rp.Y))
								{
									if (OnMarkerClick != null)
									{
										OnMarkerClick(m, e);
									}
									break;
								}

								#endregion
							}
						}

						foreach (var m in o.Routes)
						{
							if (m.IsVisible && m.IsHitTestVisible)
							{
								#region -- check --

								var rp = new GPoint(e.X, e.Y);
#if !PocketPC
								if (!_mobileMode)
								{
									rp.OffsetNegative(Core.renderOffset);
								}
#endif
								if (m.IsInside((int) rp.X, (int) rp.Y))
								{
									if (OnRouteClick != null)
									{
										OnRouteClick(m, e);
									}
									break;
								}

								#endregion
							}
						}

						foreach (var m in o.Polygons)
						{
							if (m.IsVisible && m.IsHitTestVisible)
							{
								#region -- check --

								if (m.IsInside(FromLocalToLatLng(e.X, e.Y)))
								{
									if (OnPolygonClick != null)
									{
										OnPolygonClick(m, e);
									}
									break;
								}

								#endregion
							}
						}
					}
				}
			}

			//m_mousepos = e.Location;
			//if(HelperLineOption == HelperLineOptions.ShowAlways)
			//{
			//   base.Invalidate();
			//}            
		}
#endif
#if !PocketPC
		/// <summary>
		/// apply transformation if in rotation mode
		/// </summary>
		private GPoint ApplyRotationInversion(int x, int y)
		{
			var ret = new GPoint(x, y);

			if (IsRotated)
			{
				Point[] tt = {new Point(x, y)};
				_rotationMatrixInvert.TransformPoints(tt);
				var f = tt[0];

				ret.X = f.X;
				ret.Y = f.Y;
			}

			return ret;
		}

		/// <summary>
		/// apply transformation if in rotation mode
		/// </summary>
		private GPoint ApplyRotation(int x, int y)
		{
			var ret = new GPoint(x, y);

			if (IsRotated)
			{
				Point[] tt = {new Point(x, y)};
				_rotationMatrix.TransformPoints(tt);
				var f = tt[0];

				ret.X = f.X;
				ret.Y = f.Y;
			}

			return ret;
		}

		private Cursor _cursorBefore = Cursors.Default;
#endif

		/// <summary>
		/// Gets the width and height of a rectangle centered on the point the mouse
		/// button was pressed, within which a drag operation will not begin.
		/// </summary>
#if !PocketPC
		public Size DragSize = SystemInformation.DragSize;
#else
      public Size DragSize = new Size(4, 4);
#endif

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (!Core.IsDragging && !Core.mouseDown.IsEmpty)
			{
#if PocketPC
            GPoint p = new GPoint(e.X, e.Y);
#else
				var p = ApplyRotationInversion(e.X, e.Y);
#endif
				if (Math.Abs(p.X - Core.mouseDown.X)*2 >= DragSize.Width || Math.Abs(p.Y - Core.mouseDown.Y)*2 >= DragSize.Height)
				{
					Core.BeginDrag(Core.mouseDown);
				}
			}

			if (Core.IsDragging)
			{
				if (!_isDragging)
				{
					_isDragging = true;
					Debug.WriteLine("IsDragging = " + _isDragging);

#if !PocketPC
					_cursorBefore = Cursor;
					Cursor = Cursors.SizeAll;
#endif
				}

				if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
				{
					// ...
				}
				else
				{
#if !PocketPC
					Core.mouseCurrent = ApplyRotationInversion(e.X, e.Y);
#else
               Core.mouseCurrent = new GPoint(e.X, e.Y);
#endif
					Core.Drag(Core.mouseCurrent);
#if !PocketPC
					if (_mobileMode || IsRotated)
					{
						ForceUpdateOverlays();
					}
#else
               ForceUpdateOverlays();
#endif
					base.Invalidate();
				}
			}
			else
			{
#if !PocketPC
				if (_isSelected && !_selectionStart.IsEmpty &&
				    (ModifierKeys == Keys.Alt || ModifierKeys == Keys.Shift || DisableAltForSelection))
				{
					_selectionEnd = FromLocalToLatLng(e.X, e.Y);
					{
						var p1 = _selectionStart;
						var p2 = _selectionEnd;

						var x1 = Math.Min(p1.Lng, p2.Lng);
						var y1 = Math.Max(p1.Lat, p2.Lat);
						var x2 = Math.Max(p1.Lng, p2.Lng);
						var y2 = Math.Min(p1.Lat, p2.Lat);

						SelectedArea = new RectLatLng(y1, x1, x2 - x1, y1 - y2);
					}
				}
				else
#endif
					if (Core.mouseDown.IsEmpty)
					{
						for (var i = Overlays.Count - 1; i >= 0; i--)
						{
							var o = Overlays[i];
							if (o != null && o.IsVisibile)
							{
								foreach (var m in o.Markers)
								{
									if (m.IsVisible && m.IsHitTestVisible)
									{
										#region -- check --

										var rp = new GPoint(e.X, e.Y);
#if !PocketPC
										if (!_mobileMode)
										{
											rp.OffsetNegative(Core.renderOffset);
										}
#endif
										if (m.LocalArea.Contains((int) rp.X, (int) rp.Y))
										{
											if (!m.IsMouseOver)
											{
#if !PocketPC
												SetCursorHandOnEnter();
#endif
												m.IsMouseOver = true;
												IsMouseOverMarker = true;

												if (OnMarkerEnter != null)
												{
													OnMarkerEnter(m);
												}

												Invalidate();
											}
										}
										else if (m.IsMouseOver)
										{
											m.IsMouseOver = false;
											IsMouseOverMarker = false;
#if !PocketPC
											RestoreCursorOnLeave();
#endif
											if (OnMarkerLeave != null)
											{
												OnMarkerLeave(m);
											}

											Invalidate();
										}

										#endregion
									}
								}

#if !PocketPC
								foreach (var m in o.Routes)
								{
									if (m.IsVisible && m.IsHitTestVisible)
									{
										#region -- check --

										var rp = new GPoint(e.X, e.Y);
#if !PocketPC
										if (!_mobileMode)
										{
											rp.OffsetNegative(Core.renderOffset);
										}
#endif
										if (m.IsInside((int) rp.X, (int) rp.Y))
										{
											if (!m.IsMouseOver)
											{
#if !PocketPC
												SetCursorHandOnEnter();
#endif
												m.IsMouseOver = true;
												IsMouseOverRoute = true;

												if (OnRouteEnter != null)
												{
													OnRouteEnter(m);
												}

												Invalidate();
											}
										}
										else
										{
											if (m.IsMouseOver)
											{
												m.IsMouseOver = false;
												IsMouseOverRoute = false;
#if !PocketPC
												RestoreCursorOnLeave();
#endif
												if (OnRouteLeave != null)
												{
													OnRouteLeave(m);
												}

												Invalidate();
											}
										}

										#endregion
									}
								}
#endif

								foreach (var m in o.Polygons)
								{
									if (m.IsVisible && m.IsHitTestVisible)
									{
										#region -- check --

#if !PocketPC
										var rp = new GPoint(e.X, e.Y);

										if (!_mobileMode)
										{
											rp.OffsetNegative(Core.renderOffset);
										}

										if (m.IsInsideLocal((int) rp.X, (int) rp.Y))
#else
                              if (m.IsInside(FromLocalToLatLng(e.X, e.Y)))
#endif
										{
											if (!m.IsMouseOver)
											{
#if !PocketPC
												SetCursorHandOnEnter();
#endif
												m.IsMouseOver = true;
												IsMouseOverPolygon = true;

												if (OnPolygonEnter != null)
												{
													OnPolygonEnter(m);
												}

												Invalidate();
											}
										}
										else
										{
											if (m.IsMouseOver)
											{
												m.IsMouseOver = false;
												IsMouseOverPolygon = false;
#if !PocketPC
												RestoreCursorOnLeave();
#endif
												if (OnPolygonLeave != null)
												{
													OnPolygonLeave(m);
												}

												Invalidate();
											}
										}

										#endregion
									}
								}
							}
						}
					}

#if !PocketPC
				if (_renderHelperLine)
				{
					base.Invalidate();
				}
#endif
			}
		}

#if !PocketPC

		internal void RestoreCursorOnLeave()
		{
			if (OverObjectCount <= 0 && _cursorBefore != null)
			{
				OverObjectCount = 0;
				Cursor = _cursorBefore;
				_cursorBefore = null;
			}
		}

		internal void SetCursorHandOnEnter()
		{
			if (OverObjectCount <= 0 && Cursor != Cursors.Hand)
			{
				OverObjectCount = 0;
				_cursorBefore = Cursor;
				Cursor = Cursors.Hand;
			}
		}

		/// <summary>
		/// prevents focusing map if mouse enters it's area
		/// </summary>
		public bool DisableFocusOnMouseEnter = false;

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);

			if (!DisableFocusOnMouseEnter)
			{
				Focus();
			}
			_mouseIn = true;
		}

		private bool _mouseIn;

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			_mouseIn = false;
		}

		/// <summary>
		/// reverses MouseWheel zooming direction
		/// </summary>
		public bool InvertedMouseWheelZooming = false;

		/// <summary>
		/// lets you zoom by MouseWheel even when pointer is in area of marker
		/// </summary>
		public bool IgnoreMarkerOnMouseWheel = false;

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);

			if (MouseWheelZoomEnabled && _mouseIn && (!IsMouseOverMarker || IgnoreMarkerOnMouseWheel) && !Core.IsDragging)
			{
				if (Core.mouseLastZoom.X != e.X && Core.mouseLastZoom.Y != e.Y)
				{
					if (MouseWheelZoomType == MouseWheelZoomType.MousePositionAndCenter)
					{
						Core.position = FromLocalToLatLng(e.X, e.Y);
					}
					else if (MouseWheelZoomType == MouseWheelZoomType.ViewCenter)
					{
						Core.position = FromLocalToLatLng(Width/2, Height/2);
					}
					else if (MouseWheelZoomType == MouseWheelZoomType.MousePositionWithoutCenter)
					{
						Core.position = FromLocalToLatLng(e.X, e.Y);
					}

					Core.mouseLastZoom.X = e.X;
					Core.mouseLastZoom.Y = e.Y;
				}

				// set mouse position to map center
				if (MouseWheelZoomType != MouseWheelZoomType.MousePositionWithoutCenter)
				{
					if (!GMaps.Instance.IsRunningOnMono)
					{
						var p = PointToScreen(new Point(Width/2, Height/2));
						Stuff.SetCursorPos(p.X, p.Y);
					}
				}

				Core.MouseWheelZooming = true;

				if (e.Delta > 0)
				{
					if (!InvertedMouseWheelZooming)
					{
						Zoom = ((int) Zoom) + 1;
					}
					else
					{
						Zoom = ((int) (Zoom + 0.99)) - 1;
					}
				}
				else if (e.Delta < 0)
				{
					if (!InvertedMouseWheelZooming)
					{
						Zoom = ((int) (Zoom + 0.99)) - 1;
					}
					else
					{
						Zoom = ((int) Zoom) + 1;
					}
				}

				Core.MouseWheelZooming = false;
			}
		}
#endif

		#endregion

		#region IGControl Members

		/// <summary>
		/// Call it to empty tile cache & reload tiles
		/// </summary>
		public void ReloadMap()
		{
			Core.ReloadMap();
		}

		/// <summary>
		/// set current position using keywords
		/// </summary>
		/// <param name="keys"></param>
		/// <returns>true if successfull</returns>
		public GeoCoderStatusCode SetPositionByKeywords(string keys)
		{
			var status = GeoCoderStatusCode.Unknow;

			var gp = MapProvider as GeocodingProvider;
			if (gp == null)
			{
				gp = GMapProviders.OpenStreetMap;
			}

			if (gp != null)
			{
				var pt = gp.GetPoint(keys, out status);
				if (status == GeoCoderStatusCode.G_GEO_SUCCESS && pt.HasValue)
				{
					Position = pt.Value;
				}
			}

			return status;
		}

		/// <summary>
		/// gets world coordinate from local control coordinate 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public PointLatLng FromLocalToLatLng(int x, int y)
		{
#if !PocketPC
			if (_mapRenderTransform.HasValue)
			{
				//var xx = (int)(Core.renderOffset.X + ((x - Core.renderOffset.X) / MapRenderTransform.Value));
				//var yy = (int)(Core.renderOffset.Y + ((y - Core.renderOffset.Y) / MapRenderTransform.Value));

				//PointF center = new PointF(Core.Width / 2, Core.Height / 2);

				//Matrix m = new Matrix();
				//m.Translate(-Core.renderOffset.X, -Core.renderOffset.Y);
				//m.Scale(MapRenderTransform.Value, MapRenderTransform.Value);

				//System.Drawing.Point[] tt = new System.Drawing.Point[] { new System.Drawing.Point(x, y) };
				//m.TransformPoints(tt);
				//var z = tt[0];

				//

				x = (int) (Core.renderOffset.X + ((x - Core.renderOffset.X)/_mapRenderTransform.Value));
				y = (int) (Core.renderOffset.Y + ((y - Core.renderOffset.Y)/_mapRenderTransform.Value));
			}

			if (IsRotated)
			{
				Point[] tt = {new Point(x, y)};
				_rotationMatrixInvert.TransformPoints(tt);
				var f = tt[0];

				if (VirtualSizeEnabled)
				{
					f.X += (Width - Core.vWidth)/2;
					f.Y += (Height - Core.vHeight)/2;
				}

				x = f.X;
				y = f.Y;
			}
#endif
			return Core.FromLocalToLatLng(x, y);
		}

		/// <summary>
		/// gets local coordinate from world coordinate
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public GPoint FromLatLngToLocal(PointLatLng point)
		{
			var ret = Core.FromLatLngToLocal(point);

#if !PocketPC
			if (_mapRenderTransform.HasValue)
			{
				ret.X = (int) (Core.renderOffset.X + ((Core.renderOffset.X - ret.X)*-_mapRenderTransform.Value));
				ret.Y = (int) (Core.renderOffset.Y + ((Core.renderOffset.Y - ret.Y)*-_mapRenderTransform.Value));
			}

			if (IsRotated)
			{
				Point[] tt = {new Point((int) ret.X, (int) ret.Y)};
				_rotationMatrix.TransformPoints(tt);
				var f = tt[0];

				if (VirtualSizeEnabled)
				{
					f.X += (Width - Core.vWidth)/2;
					f.Y += (Height - Core.vHeight)/2;
				}

				ret.X = f.X;
				ret.Y = f.Y;
			}

#endif
			return ret;
		}

#if !PocketPC

		/// <summary>
		/// shows map db export dialog
		/// </summary>
		/// <returns></returns>
		public bool ShowExportDialog()
		{
			using (FileDialog dlg = new SaveFileDialog())
			{
				dlg.CheckPathExists = true;
				dlg.CheckFileExists = false;
				dlg.AddExtension = true;
				dlg.DefaultExt = "gmdb";
				dlg.ValidateNames = true;
				dlg.Title = "GMap.NET: Export map to db, if file exsist only new data will be added";
				dlg.FileName = "DataExp";
				dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				dlg.Filter = "GMap.NET DB files (*.gmdb)|*.gmdb";
				dlg.FilterIndex = 1;
				dlg.RestoreDirectory = true;

				if (dlg.ShowDialog() == DialogResult.OK)
				{
					var ok = GMaps.Instance.ExportToGMDB(dlg.FileName);
					if (ok)
					{
						MessageBox.Show("Complete!", "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else
					{
						MessageBox.Show("Failed!", "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}

					return ok;
				}
			}

			return false;
		}

		/// <summary>
		/// shows map dbimport dialog
		/// </summary>
		/// <returns></returns>
		public bool ShowImportDialog()
		{
			using (FileDialog dlg = new OpenFileDialog())
			{
				dlg.CheckPathExists = true;
				dlg.CheckFileExists = false;
				dlg.AddExtension = true;
				dlg.DefaultExt = "gmdb";
				dlg.ValidateNames = true;
				dlg.Title = "GMap.NET: Import to db, only new data will be added";
				dlg.FileName = "DataImport";
				dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				dlg.Filter = "GMap.NET DB files (*.gmdb)|*.gmdb";
				dlg.FilterIndex = 1;
				dlg.RestoreDirectory = true;

				if (dlg.ShowDialog() == DialogResult.OK)
				{
					var ok = GMaps.Instance.ImportFromGMDB(dlg.FileName);
					if (ok)
					{
						MessageBox.Show("Complete!", "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Information);
						ReloadMap();
					}
					else
					{
						MessageBox.Show("Failed!", "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}

					return ok;
				}
			}

			return false;
		}
#endif

		private ScaleModes _scaleMode = ScaleModes.Integer;

		[Category("GMap.NET")]
		[Description("map scale type")]
		public ScaleModes ScaleMode
		{
			get { return _scaleMode; }
			set { _scaleMode = value; }
		}

		[Category("GMap.NET"), DefaultValue(0)]
		public double Zoom
		{
			get { return _zoomReal; }
			set
			{
				if (_zoomReal != value)
				{
					Debug.WriteLine("ZoomPropertyChanged: " + _zoomReal + " -> " + value);

					if (value > MaxZoom)
					{
						_zoomReal = MaxZoom;
					}
					else if (value < MinZoom)
					{
						_zoomReal = MinZoom;
					}
					else
					{
						_zoomReal = value;
					}

#if !PocketPC
					var remainder = value%1;
					if (ScaleMode == ScaleModes.Fractional && remainder != 0)
					{
						var scaleValue = (float) Math.Pow(2d, remainder);
						{
							_mapRenderTransform = scaleValue;
						}

						ZoomStep = Convert.ToInt32(value - remainder);
					}
					else
#endif
					{
#if !PocketPC
						_mapRenderTransform = null;
#endif
						ZoomStep = (int) Math.Floor(value);
						//zoomReal = ZoomStep;
					}

					if (Core.IsStarted && !IsDragging)
					{
						ForceUpdateOverlays();
					}
				}
			}
		}

		/// <summary>
		/// map zoom level
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		internal int ZoomStep
		{
			get { return Core.Zoom; }
			set
			{
				if (value > MaxZoom)
				{
					Core.Zoom = MaxZoom;
				}
				else if (value < MinZoom)
				{
					Core.Zoom = MinZoom;
				}
				else
				{
					Core.Zoom = value;
				}
			}
		}

		/// <summary>
		/// current map center position
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public PointLatLng Position
		{
			get { return Core.Position; }
			set
			{
				Core.Position = value;

				if (Core.IsStarted)
				{
					ForceUpdateOverlays();
				}
			}
		}

		/// <summary>
		/// current position in pixel coordinates
		/// </summary>
		[Browsable(false)]
		public GPoint PositionPixel
		{
			get { return Core.PositionPixel; }
		}

		/// <summary>
		/// location of cache
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public string CacheLocation
		{
			get
			{
#if !DESIGN
				return CacheLocator.Location;
#else
            return string.Empty;
#endif
			}
			set
			{
#if !DESIGN
				CacheLocator.Location = value;
#endif
			}
		}

		private bool _isDragging;

		/// <summary>
		/// is user dragging map
		/// </summary>
		[Browsable(false)]
		public bool IsDragging
		{
			get { return _isDragging; }
		}

		private bool _isMouseOverMarker;
		internal int OverObjectCount;

		/// <summary>
		/// is mouse over marker
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public bool IsMouseOverMarker
		{
			get { return _isMouseOverMarker; }
			internal set
			{
				_isMouseOverMarker = value;
				OverObjectCount += value ? 1 : -1;
			}
		}

		private bool _isMouseOverRoute;

		/// <summary>
		/// is mouse over route
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public bool IsMouseOverRoute
		{
			get { return _isMouseOverRoute; }
			internal set
			{
				_isMouseOverRoute = value;
				OverObjectCount += value ? 1 : -1;
			}
		}

		private bool _isMouseOverPolygon;

		/// <summary>
		/// is mouse over polygon
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public bool IsMouseOverPolygon
		{
			get { return _isMouseOverPolygon; }
			internal set
			{
				_isMouseOverPolygon = value;
				OverObjectCount += value ? 1 : -1;
			}
		}

		/// <summary>
		/// gets current map view top/left coordinate, width in Lng, height in Lat
		/// </summary>
		[Browsable(false)]
		public RectLatLng ViewArea
		{
			get
			{
#if !PocketPC
				if (!IsRotated)
				{
					return Core.ViewArea;
				}
				if (Core.Provider.Projection != null)
				{
					var p = FromLocalToLatLng(0, 0);
					var p2 = FromLocalToLatLng(Width, Height);

					return RectLatLng.FromLtrb(p.Lng, p.Lat, p2.Lng, p2.Lat);
				}
				return RectLatLng.Empty;
#else
                return Core.ViewArea;
#endif
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public GMapProvider MapProvider
		{
			get { return Core.Provider; }
			set
			{
				if (Core.Provider == null || !Core.Provider.Equals(value))
				{
					Debug.WriteLine("MapType: " + Core.Provider.Name + " -> " + value.Name);

					var viewarea = SelectedArea;
					if (viewarea != RectLatLng.Empty)
					{
						Position = new PointLatLng(viewarea.Lat - viewarea.HeightLat/2, viewarea.Lng + viewarea.WidthLng/2);
					}
					else
					{
						viewarea = ViewArea;
					}

					Core.Provider = value;

					if (Core.IsStarted)
					{
						if (Core.zoomToArea)
						{
							// restore zoomrect as close as possible
							if (viewarea != RectLatLng.Empty && viewarea != ViewArea)
							{
								var bestZoom = Core.GetMaxZoomToFitRect(viewarea);
								if (bestZoom > 0 && Zoom != bestZoom)
								{
									Zoom = bestZoom;
								}
							}
						}
						else
						{
							ForceUpdateOverlays();
						}
					}
				}
			}
		}

		/// <summary>
		/// is routes enabled
		/// </summary>
		[Category("GMap.NET")]
		public bool RoutesEnabled
		{
			get { return Core.RoutesEnabled; }
			set { Core.RoutesEnabled = value; }
		}

		/// <summary>
		/// is polygons enabled
		/// </summary>
		[Category("GMap.NET")]
		public bool PolygonsEnabled
		{
			get { return Core.PolygonsEnabled; }
			set { Core.PolygonsEnabled = value; }
		}

		/// <summary>
		/// is markers enabled
		/// </summary>
		[Category("GMap.NET")]
		public bool MarkersEnabled
		{
			get { return Core.MarkersEnabled; }
			set { Core.MarkersEnabled = value; }
		}

		/// <summary>
		/// can user drag map
		/// </summary>
		[Category("GMap.NET")]
		public bool CanDragMap
		{
			get { return Core.CanDragMap; }
			set { Core.CanDragMap = value; }
		}

		/// <summary>
		/// map render mode
		/// </summary>
		[Browsable(false)]
		public RenderMode RenderMode
		{
			get { return Core.RenderMode; }
			internal set { Core.RenderMode = value; }
		}

		/// <summary>
		/// gets map manager
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public GMaps Manager
		{
			get { return GMaps.Instance; }
		}

		#endregion

		#region IGControl event Members

		/// <summary>
		/// occurs when current position is changed
		/// </summary>
		public event PositionChanged OnPositionChanged
		{
			add { Core.OnCurrentPositionChanged += value; }
			remove { Core.OnCurrentPositionChanged -= value; }
		}

		/// <summary>
		/// occurs when tile set load is complete
		/// </summary>
		public event TileLoadComplete OnTileLoadComplete
		{
			add { Core.OnTileLoadComplete += value; }
			remove { Core.OnTileLoadComplete -= value; }
		}

		/// <summary>
		/// occurs when tile set is starting to load
		/// </summary>
		public event TileLoadStart OnTileLoadStart
		{
			add { Core.OnTileLoadStart += value; }
			remove { Core.OnTileLoadStart -= value; }
		}

		/// <summary>
		/// occurs on map drag
		/// </summary>
		public event MapDrag OnMapDrag
		{
			add { Core.OnMapDrag += value; }
			remove { Core.OnMapDrag -= value; }
		}

		/// <summary>
		/// occurs on map zoom changed
		/// </summary>
		public event MapZoomChanged OnMapZoomChanged
		{
			add { Core.OnMapZoomChanged += value; }
			remove { Core.OnMapZoomChanged -= value; }
		}

		/// <summary>
		/// occures on map type changed
		/// </summary>
		public event MapTypeChanged OnMapTypeChanged
		{
			add { Core.OnMapTypeChanged += value; }
			remove { Core.OnMapTypeChanged -= value; }
		}

		/// <summary>
		/// occurs on empty tile displayed
		/// </summary>
		public event EmptyTileError OnEmptyTileError
		{
			add { Core.OnEmptyTileError += value; }
			remove { Core.OnEmptyTileError -= value; }
		}

		#endregion

#if !PocketPC

		#region Serialization

		private static readonly BinaryFormatter BinaryFormatter = new BinaryFormatter();

		/// <summary>
		/// Serializes the overlays.
		/// </summary>
		/// <param name="stream">The stream.</param>
		public void SerializeOverlays(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			// Create an array from the overlays
			var overlayArray = new GMapOverlay[Overlays.Count];
			Overlays.CopyTo(overlayArray, 0);

			// Serialize the overlays
			BinaryFormatter.Serialize(stream, overlayArray);
		}

		/// <summary>
		/// De-serializes the overlays.
		/// </summary>
		/// <param name="stream">The stream.</param>
		public void DeserializeOverlays(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			// De-serialize the overlays
			var overlayArray = BinaryFormatter.Deserialize(stream) as GMapOverlay[];

			// Populate the collection of overlays.
			foreach (var overlay in overlayArray)
			{
				overlay.Control = this;
				Overlays.Add(overlay);
			}

			ForceUpdateOverlays();
		}

		#endregion

#endif
	}

	public enum ScaleModes
	{
		/// <summary>
		/// no scaling
		/// </summary>
		Integer,

#if !PocketPC
		/// <summary>
		/// scales to fractional level, CURRENT VERSION DOESN'T HANDLE OBJECT POSITIONS CORRECLTY, 
		/// http://greatmaps.codeplex.com/workitem/16046
		/// </summary>
		Fractional
#endif
	}

#if !PocketPC
	public enum HelperLineOptions
	{
		DontShow = 0,
		ShowAlways = 1,
		ShowOnModifierKey = 2
	}

	public delegate void SelectionChange(RectLatLng selection, bool zoomToFit);
#endif
}