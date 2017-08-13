using System;
using System.Drawing;
using System.Windows.Forms;
using FormsFramework.Utils;
using FormsFramework.Windows.Forms;
using FrameworkBase.Log;
using GMap.NET.WindowsForms;

namespace OlapFormsFramework.Windows.Forms.Grid.Scatter
{
    [Serializable]
    public class OlapMapToolTip : GMapToolTip
    {
		// При підсвітці елементу означає кількість пікселів між краєм елементу і краєм підсвітки (ширина круга)
		private const float ITEM_HIGHLIGHT_WIDTH = 7;

		//private Point _HintLocationShift;
	    private readonly AdvancedHint _itemHint;
		private bool _isDragging;                       //Вказує чи користувач переміщує хінт.
	    private CachedBmp _cachedBeforeDrag;
	    private bool _itemHintRelocating;

	    private Rectangle? DrawArea
		{
			get { return Control?.DrawArea; }
		}
		private OlapScatter Control
		{
			get { return Marker?.Overlay?.Control; }
		}

		// Встановлює оптимальну позицію для хінта відносно елемента
		private void ItemHintBestPositionSet(AdvancedHint aHint,  string aCaption)
		{
			var hintArea = DrawArea;
			if (hintArea == null)
				return;
			if (aHint.Visible)
				aHint.HintHide();
			var marker = Marker;
			ItemHintPositionSet(aHint, marker.LocalPosition.X, marker.LocalPosition.Y, marker.Size.Width);
			
			aHint.HintArea = hintArea.Value;
			aHint.HintPosition = HintPosition.hpTop;
			aHint.TextOrientation = HintTextOrientation.htoHorizontal;
			aHint.HintPointerLocation = HintPointerLocation.hplFar;
			aHint.Caption = aCaption;
			aHint.PointerPosition = 0f;
			aHint.SizePrecalculate();
			if (aHint.Location.X <= hintArea.Value.X)
			{
				if (aHint.Location.Y <= hintArea.Value.Y)
				{
					aHint.HintPosition = HintPosition.hpBottom;
					aHint.PointerPosition = 1f;
				}
				aHint.HintPointerLocation = HintPointerLocation.hplNear;
			}
			else if (aHint.Location.Y <= hintArea.Value.Y)
			{
				aHint.HintPosition = HintPosition.hpBottom;
				aHint.PointerPosition = 1f;
			}
		}
	    /// <summary>
		/// Встановлює лінію на одну з точок якої повинен вказувати хінт.
		/// </summary>
		/// <param name="aHint">Хінт який потрібно "прив'язати"</param>
		/// <param name="aItemX">абсциса центру елементу</param>
		/// <param name="aItemY">ордината центру елементу</param>
		/// <param name="aItemSize">розмір елементу</param>
		private static void ItemHintPositionSet(AdvancedHint aHint, float aItemX, float aItemY, float aItemSize)
		{
			var itemHintPoint1 = new Point((int)Math.Ceiling(aItemX), (int)Math.Ceiling(aItemY - aItemSize / 2 - ITEM_HIGHLIGHT_WIDTH / 2));
			var itemHintPoint2 = new Point((int)Math.Ceiling(aItemX), (int)Math.Ceiling(aItemY + aItemSize / 2 + ITEM_HIGHLIGHT_WIDTH / 2));
			aHint.PositionSet(itemHintPoint1, itemHintPoint2);
		}
		/// <summary>
		/// Малює лінію що сполучає хінт з елементом
		/// </summary>
		/// <param name="aGraphics">Graphics використовуючи який потрібно малювати</param>
		/// <param name="aHint">Хінт від якого потрібно малювати лінію</param>
		/// <param name="aItemPointX">абсциса центру елементу</param>
		/// <param name="aItemPointY">ордината центру елементу</param>
		private static void LineItemHintToItemDraw(Graphics aGraphics, AdvancedHint aHint, float aItemPointX, float aItemPointY)
		{
			aGraphics.DrawLine(Pens.Black, aItemPointX, aItemPointY, aHint.Left + aHint.Width / 2, aHint.Top + aHint.Height / 2);
		}

		/// <summary>
		/// Відмальовує скеттер сторінку при переміщенні хінта
		/// </summary>
		/// <param name="aItem">Елемент хінт якого переміщується</param>
		/// <param name="aInvalidRect">Регіон що змінився внаслідок переміщення</param>
		private Rectangle ItemHintDraggingRedraw(Graphics graphics)
		{			
			var hint = _itemHint;

			//calculate Rectangle which must be redrawn
			var aInvalidRect = new Rectangle((int)Marker.LocalPosition.X, (int)Marker.LocalPosition.Y, 0, 0);
			var itemDiam = (int)(Math.Max(Marker.Size.Height, Marker.Size.Width) / 2 + ITEM_HIGHLIGHT_WIDTH) + 2;
			aInvalidRect.Inflate(itemDiam, itemDiam);
			aInvalidRect = Rectangle.Union(aInvalidRect, new Rectangle(hint.PreviousLocation, hint.Size));
			aInvalidRect = Rectangle.Intersect(new Rectangle(0, 0, Control.Width, Control.Height), aInvalidRect);
			//redraw from cache
			_cachedBeforeDrag.Draw(graphics, aInvalidRect);
			//draw line from item to hint
			LineItemHintToItemDraw(graphics, hint, Marker.LocalPosition.X, Marker.LocalPosition.Y);
			return aInvalidRect;
		}
		private void ItemHint_EventClose(object aSender, EventArgs aEventArgs)
		{
			var hint = aSender as AdvancedHint;
			if (hint != null)
				Control.ClickMarker(null, Marker);
			//_Data.ItemUnselect(hint.ID);
		}
		private void ItemHint_EventDragEnd(object aSender, EventArgs aEventArgs)
		{
			 
			Control.Redraw(false);
			if (_cachedBeforeDrag != null)
			{
				_cachedBeforeDrag.Dispose();
				_cachedBeforeDrag = null;
			}
		}
		private void ItemHint_EventDragStart(object aSender, EventArgs aEventArgs)
		{
			if (Control == null)
				return;
			_cachedBeforeDrag = new CachedBmp(Control.Width, Control.Height, Control.CreateGraphics());
			Control.Redraw(_cachedBeforeDrag.BufferedGraphics, new Rectangle(0, 0, Control.Width, Control.Height), false, false);
		}
		private void ItemHint_EventMouseEnter(object aSender, EventArgs aEventArgs)
		{
			if (Control == null || Control.IsInAction || Control.PopupMenu.Visible)
				return;
			Tracer.EnterMethod("OlapScatter.ItemHint_EventMouseEnter()");
			//_mouseAt = OlapScatter.MAC.macHint;
			var hint = aSender as AdvancedHint;
			if (hint != null)
				Control?.EnterMarker(Marker);
			//TODO: move to marker
			//if (hint != null)
			//	CurrentItem = new CI(_Data.ShowTrails ? _Data.ItemSelectionStart[hint.ID] : _Data.CurrentPage, hint.ID);
			Tracer.ExitMethod("OlapScatter.ItemHint_EventMouseEnter()");
		}
		private void ItemHint_EventMouseLeave(object aSender, EventArgs aEventArgs)
		{
			if (Control == null || Control.IsInAction || Control.PopupMenu.Visible)
				return;
			//ToDO: move to marker
			var hint = aSender as AdvancedHint;
			if (hint != null)
				Control?.LeaveMarker(Marker);
			//if (hint != null && _mouseAt == MAC.macHint)
			//	CurrentItem = CI.Default;
		}
		private void ItemHint_EventMouseUp(object aSender, MouseEventArgs aEventArgs)
		{
			if (Control == null || Control.IsInAction || Control.PopupMenu.Visible)
				return;
			var hint = aSender as AdvancedHint;
			////ToDO: move to marker
			if (hint != null)
				Control?.ClickMarker(aEventArgs, Marker);
			//if (hint != null)
			//	CurrentItem = new CI(_Data.ShowTrails ? _Data.ItemSelectionStart[hint.ID] : _Data.CurrentPage, hint.ID);
			if (aEventArgs.Button == MouseButtons.Right && Control != null && Control.IsActual)
				Control?.PopupShow(aEventArgs.X + ((AdvancedHint)aSender).Left, aEventArgs.Y + ((AdvancedHint)aSender).Top);
		}
		private void ItemHint_EventLocationChanged(object aSender, EventArgs aEventArgs)
		{
			if (_itemHintRelocating)
				return;
			var hint = aSender as AdvancedHint;
			if (hint != null && hint.Visible )
			{
				var rect = ItemHintDraggingRedraw(_cachedBeforeDrag.BufferedGraphics);
				Control.Invalidate(rect);
				Control.Update();
			}
		}

		private AdvancedHint ItemHintGet()
		{
			var hint = new AdvancedHint(Control, false) {Visible = false}; //_Data.ItemHintGet(aItemID);
			//if (hint == null)
			//hint = _Data.ItemHintCreate(aItemID, this);
			hint.Caption = Marker?.ToolTipText;
			hint.ShowPointer = false;
			hint.LocationChanged += ItemHint_EventLocationChanged;
			hint.MouseEnter += ItemHint_EventMouseEnter;
			hint.MouseLeave += ItemHint_EventMouseLeave;
			hint.EventDragEnd += ItemHint_EventDragEnd;
			hint.EventDragStart += ItemHint_EventDragStart;
			hint.EventClose += ItemHint_EventClose;
			hint.MouseUp += ItemHint_EventMouseUp;
			hint.ID = Marker?.ItemID ?? -1;
			hint.CanDrag = true; //???

			//hint.CanShow = _Data.ShowHints;
			hint.HintArea = DrawArea ?? Rectangle.Empty;
			return hint;
		}

	    public OlapMapToolTip(GMapMarker marker) 
            : base(marker)
		{
			_itemHint = ItemHintGet();
		}

	    public void Show(bool aWithHighlight)
		{
			if (_itemHint != null)
			{
				var marker = Marker;
				if (marker.IsVisible)
				{
					ItemHintBestPositionSet(_itemHint, marker.ToolTipText);
					_itemHint.Visible = true;
					_itemHint.HighlightColor = marker.MarkerColor;
					_itemHint.Highlighted = aWithHighlight;
					_itemHint.HintShow(false);
				}
			}
		}
		/// <summary>
		/// Ховає контрол хінта і встановлює його в непідсвічений стан
		/// </summary>
		public void HintHide()
		{
			if (_itemHint != null)
			{
				_itemHint.Visible = false;
				_itemHint.Highlighted = false;
				_itemHint.HintHide();
			}
		}
		public override void OnRender(Graphics graphics)
	    {
			if (_itemHint != null)
			{
				_itemHint.Update();
				var position = Marker.LocalPosition;
				//if (_itemHint.Visible)
					//LineItemHintToItemDraw(graphics, _itemHint, position.X, position.Y);
			}
	    }

		public new OlapMapCircleMarker Marker
		{
			get { return (OlapMapCircleMarker)_marker; }
			internal set { _marker = value; }
		}
		//public bool WithHighlight { get; set; }
	    public bool CanDrag
	    {
		    get { return _itemHint.CanDrag; }
		    set { _itemHint.CanDrag = value; }
	    }
	}
}