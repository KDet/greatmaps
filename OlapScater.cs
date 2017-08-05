using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Reflection;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraBars;
using FormsFramework.Common.Actions;
using FormsFramework.Utils;
using FormsFramework.Windows.Forms;
using FrameworkBase;
using FrameworkBase.Exceptions;
using FrameworkBase.Log;
using FrameworkBase.Utils;
using OlapFormsFramework.Common.EventArguments;
using OlapFormsFramework.Exceptions;
using OlapFormsFramework.Utils;
using OlapFormsFramework.Windows.Forms.Grid.Formatting;
using OlapFormsFramework.Windows.Forms.Grid.HighlightRules;
using OlapFramework.Data;
using System.Text;
using OlapFramework.Olap.Metadata;
using OSDI = OlapFramework.Data.OlapScatterDataItem;
using MVT = OlapFramework.Data.MeasureValueType;
using System.ComponentModel;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using OlapFormsFramework.Windows.Forms.Grid.Printing;
using SIST = OlapFormsFramework.Windows.Forms.Grid.Scatter.ScatterItemSelectionType;
using CUtils = OlapFormsFramework.Utils.ControlUtils;
using FormatType = OlapFormsFramework.Windows.Forms.Grid.Formatting.FormatType;

namespace OlapFormsFramework.Windows.Forms.Grid.Scatter
{
	public class OlapScatter : GMapControl, IActionListener
	{
		protected enum ActionImage { aiUnknown = -1, aiDrillthrough, aiSaveNRP, aiExportPNG, aiExportPDF, aiPrint, aiFormat, aiHighlight }
		/// <summary>
		/// Скорочено від MouseAtControl.
		/// </summary>
		private enum MAC { macNone, macOlapScatter, macHint }
		/// <summary>
		/// Скорочено від CurrentItem
		/// Ідентифікує елемент по номеру сторінки та номеру елементу
		/// </summary>
		[DebuggerDisplay("Page={PageID}, Item={ItemID}")]
		private struct CI : IEquatable<CI>
		{
			public CI(int aPageID, int aItemID)
			{
				PageID = aPageID;
				ItemID = aItemID;
			}

			public static readonly CI Default = new CI(-1, -1);
			public int ItemID;
			public int PageID;

			public override int GetHashCode()
			{
				return PageID ^ ItemID;
			}
			public override bool Equals(object aObject)
			{
				if (!(aObject is CI))
					return false;
				return Equals((CI)aObject);
			}
			public bool Equals(CI aOther)
			{
				return aOther.PageID == PageID && aOther.ItemID == ItemID;
			}
			public override string ToString()
			{
				return string.Format("Page={0}, Item={1}", PageID, ItemID);
			}

			public static bool operator ==(CI aFirst, CI aSecond)
			{
				return aFirst.Equals(aSecond);
			}
			public static bool operator !=(CI aFirst, CI aSecond)
			{
				return !aFirst.Equals(aSecond);
			}
		}

		#region ResourceIDs
		private const string Common_PopUpMenu_Actions = "Common_PopUpMenu_Actions";
		private const string Common_PopUpMenu_DrillByOnNewPage = "Common_PopUpMenu_DrillByOnNewPage";
		private const string Common_PopUpMenu_NoActions = "Common_PopUpMenu_NoActions";
		private const string OlapScatter_label_Null = "OlapScatter_MeasureNullString";
		private const string OlapScatter_NotAllDragged = "OlapScatter_NotAllDragged";
		private const string OlapScatter_BadResults = "OlapScatter_BadResults";
		private const string OlapScatter_PopUpMenu_DrillBy = "OlapScatter_PopUpMenu_DrillBy";
		private const string OlapScatter_PopUpMenu_DrillDown = "OlapScatter_PopUpMenu_DrillDown";
		private const string OlapScatter_PopUpMenu_Drillthrough = "OlapScatter_PopUpMenu_Drillthrough";
		private const string OlapScatter_PopUpMenu_DrillUp = "OlapScatter_PopUpMenu_DrillUp";
		private const string OlapScatter_PopUpMenu_ExportToNRP = "OlapScatter_PopUpMenu_ExportToNRP";
		private const string OlapScatter_PopUpMenu_ExportToPNG = "OlapScatter_PopUpMenu_ExportToPNG";
		private const string OlapScatter_PopUpMenu_Format = "OlapScatter_PopUpMenu_Format";
		private const string OlapScatter_PopUpMenu_HideItem = "OlapScatter_PopUpMenu_HideItem";
		private const string OlapScatter_PopUpMenu_HideSiblings = "OlapScatter_PopUpMenu_HideSiblings";
		private const string OlapScatter_PopUpMenu_Highlight = "OlapScatter_PopUpMenu_Highlight";
		private const string OlapScatter_PopUpMenu_HighlightView = "OlapScatter_PopUpMenu_HighlightView";
		private const string OlapScatter_PopUpMenu_Print = "OlapScatter_PopUpMenu_Print";
		private const string OlapScatter_PopUpMenu_ShowSiblings = "OlapScatter_PopUpMenu_ShowSiblings";
		#endregion
		#region Actions
		public const string ACTION_DRILL_DOWN = "DrillDown";
		public const string ACTION_DRILLTHROUGH = "Drillthrough";
		public const string ACTION_DRILL_UP = "DrillUp";
		public const string ACTION_EXPORT_PNG = "ExportPNG";
		public const string ACTION_FORMAT = "Format";
		public const string ACTION_HIDE_ITEM = "HideItem";
		public const string ACTION_HIDE_SIBLINGS = "HideSiblings";
		public const string ACTION_HIGHLIGHT = "Highlight";
		public const string ACTION_OLAP_ACTION = "DrillthroughAction";
		public const string ACTION_PRINT = "Print";
		public const string ACTION_SHOW_SIBLINGS = "ShowSiblings";
		#endregion
		#region Constants
		/// <summary>
		/// Мінімальна відстань між лініями сітки
		/// </summary>
		private const int DIST_BTWN_LINES = 50;
		/// <summary>
		/// Мінімальна відстань від центру кожного з елементів до країв скеттера (або осей).
		/// </summary>
		private const int DIST_TO_BOUND = 30;
		/// <summary>
		/// Інтервал часу в мілісекундіх за який з'являються/пропадають кружки що належать до підсвічуваної групи
		/// </summary>
		private const int GROUP_HIGHLIGHT_INTERVAL = 400;
		/// <summary>
		/// Символ, який буде з'являтися перед значенням міри, якщо це значення інтерпольовано
		/// </summary>
		private const char INTERPOLATION_CHAR = (char)0x2248;
		/// <summary>
		/// При підсвітці елементу означає кількість пікселів між краєм елементу і краєм підсвітки (ширина круга)
		/// </summary>
		private const float ITEM_HIGHLIGHT_WIDTH = 7;
		/// <summary>
		/// Інтервал часу в мілісекундах між підсвіткою сусідніх(за номерами сторінок) "кружків" в шляху елемента.
		/// </summary>
		private const int ITEM_HIGHLIGHT_INTERVAL = 700;
		/// <summary>
		/// Час в секундах скільки часу буде показано хінт над підсвіченим елементом у випадку якщо для нього є відмальований шлях
		/// </summary>
		private const float ITEM_HINT_VISIBLE_TIME = 3.5f;
		/// <summary>
		/// Ширина в пікселях лінії що сполучає два сусідніх "кружка" в шляху елементу
		/// </summary>
		private const float WAY_LINE_WIDTH = 3;

		/// <summary>
		/// Brush яким буде зафарбовано фон скеттер діаграми
		/// </summary>
		private static readonly Brush BRUSH_BACK = new SolidBrush(Color.White);
		/// <summary>
		/// Brush яким буде відмальовано текст що сповіщає що недостатньо елементів "натягано"
		/// </summary>
		private static readonly Brush BRUSH_LABEL_FORE_DEFAULT = new SolidBrush(Color.Gray);
		/// <summary>
		/// Brush яким буде відмальовано текст що сповіщає що дані не можуть бути відмальовані
		/// </summary>
		private static readonly Brush BRUSH_LABEL_FORE_SPECIAL = new SolidBrush(Color.Red);
		/// <summary>
		/// Brush яким буде написано Caption сторінки на скеттері
		/// </summary>
		private static readonly Brush BRUSH_STR_BACK = new SolidBrush(Color.FromArgb(75, Color.Black));
		/// <summary>
		/// Колір лінії якою перекреслюється "кружок" у випадку якщо одна з мір для нього інтерпольована
		/// </summary>
		private static readonly Color COLOR_INTERPOLATED_LINE = Color.Black;
		/// <summary>
		/// Шрифт яким буде написано Caption сторінки на скеттері
		/// </summary>
		private static readonly Font FONT_BACK = FontUtils.FontCreate("Verdana", 50);
		/// <summary>
		/// Шрифт яким буде написано користувачу повідомлення про те що дані погані або не все втягнуто
		/// </summary>
		private static readonly Font FONT_LABEL_TEXT = FontUtils.FontCreate(@"Tahoma", 8.25f);
		/// <summary>
		/// Pen яким перекреслюється "кружок" у випадку якщо одна з мір для нього інтерпольована
		/// </summary>
		private static readonly Pen PEN_INTERPOLATED_LINE;
		/// <summary>
		/// Pen яким буде намальовано лінії навколо скеттера
		/// </summary>
		private static readonly Pen PEN_ITEM_AREA_BORDER = Pens.Black;
		/// <summary>
		/// Pen яким буде намальована  лінія навколо "кружка"
		/// </summary>
		private static readonly Pen PEN_ITEM_LINE = Pens.Black;
		/// <summary>
		/// StringFormat враховуючи який буде намальовано Caption сторінки на скеттері
		/// </summary>
		private static readonly StringFormat STR_FORMAT_BACK;
		/// <summary>
		/// StringFormat враховуючи який буде намальовано надпис про помилку або інформацію про дані
		/// </summary>
		private static readonly StringFormat STR_FORMAT_LABEL;
		#endregion

		private readonly OlapMapOverlay _markersLayer = new OlapMapOverlay("markers");

		private static readonly Images _ActionImages;
		private readonly ActionCollection _Actions;
		private AdvancedBarButtonItem _BBINoActions;
		private AdvancedBarSubItem _BSIActions;
		private AdvancedBarSubItem _BSIDrillBy;                                 //Підменю Drill by контекстного меню
		private AdvancedBarSubItem _BSIDrillByOnNewPage;                        //Підменю Drill by on New Page контекстного меню
		private AdvancedBarSubItem _BSIDrillDown;
		private AdvancedBarSubItem _BSIDrillthrough;
		private AdvancedBarSubItem _BSIDrillUp;
		private AdvancedBarSubItem[] _BSIHideShowActions;
		private CachedBmp _bufferedPage = null;                         //Кеш в якому міститься відмальована повністю вся скеттер діаграма
		private CachedBmp _cachedBeforeDrag = null;                     //Кеш скетер сторінки перед переміщенням хінта але без ліній що сполучає хінт що перетягується з відповідним йому елементом
		private CachedBmp _cachedCoord = null;                          //Кеш в якому міститься відмальована сітка для скеттер діаграми
		private bool _CanDrillthrough = true;
		private bool _canExportNRP = true;
		private bool _canExportPng = true;
		private bool _CanFormatting = true;
		private bool _CanHighlight = true;
		private bool _CanNavigate = true;
		private bool _CanOlapActions = true;
		private bool _CanPrint = true;
		private CI _CurrentItem = CI.Default;                           //Поточний елемент
		private CI _CurrentItemForPopup;
		private float _CurrentPage = -1;                                //Поточна сторінка
		private OlapScatterFormsData _Data;                             //Дані які потрібно відмальовувати
		private bool _dataCalculated = false;                           //Вказує чи для даних що містяться в _Data прораховані конкретні позиції на даному скеттері на колір і розмір
		private AdvancedHint _draggedHint = null;                       //Хінт що перетягується
		private GradientKindClass _gradient;                            //Градієнт використовуючи який розмільовуються елементи
		private bool _groupHighlightShow;                               //Вказує чи малювати на скеттері елементи що містяться в_groupHighlightIndixes. При підсвітці поточної групи періодично змінюється.
		private List<int> _groupHighlightIndixes = new List<int>(0);    //Зберігає індекси елементів з однієї групи які треба підсвітити.
		private CI _highlightedItem = CI.Default;                       //Вказує на елемент шляху що зараз підсвічується. Періодично міняється при підсвітці шляху елемента.
		private readonly AdvancedHint[] _hints;                         //Масив хінтів для мір та елементу, спільний для всіх елементів. Містить рівно 5 хінтів (4 для мір в порядку що відповідає OlapScatterDataIte.MEASURES_INDEXES та один для хінта над елементом при наведенні мишкою).
		private bool _IsActual = true;                                  //Вказує чи поточні дані є актуальними
		private bool _IsHighlightViewMode = false;
		private bool _isPlaying = false;                                //Вказує чи скеттер діаграма є в режимі "плей"
		private bool _IsViewMode = false;
		private bool _itemHintRelocating = false;                       //Вказує чи користувач переміщує хінт.
		private int _itemMinSize;                                       //Мінімальний розмір елементу в пікселях
		private int _itemMaxSize;                                       //Максимальний розмір елементу в пікселях.
		private Rectangle _itemsArea;                                   //Rectangle в якому будуть відмальовані всі елементи
		private FormatRulesMeasures _MeasuresFormatRules;
		private bool[] _moreThanOneElementOnLevel;
		private MAC _mouseAt = MAC.macNone;                             //Вказує на контрол над яким перебуває мишка. Потрібно тому що в InternetExplorer спочатку спрацьовує подія MouseEnter для контролу куди ввійшла мишка, а потім MouseLeave для контролу звідки вийшла, а в вінформсах навпаки.
		private float _pageStep;                                        //Крок зміни позиції сторінки між сусідніми відмальовками в режимі "плей". Фактично швидкість програвання.
		private PopupMenu _PopupMenu;
		private readonly PrintData _PrintData = new PrintData();
		private double _sCoef;                                          //Для даних - відношення різниці між макс. і мін. радіусом кружка і макс. і мін. значенням міри що відповідає за розмір.
		private readonly Timer _timer = new Timer();                                //Тімер що використовується для режиму плей
		private readonly Timer _timerGroupHighlight = new Timer();              //Таймер що використовується для підсвітки поточної групи елементів
		private readonly Timer _timerItemHighlight = new Timer();               //Таймер що використовується для підсвітки шляху поточного елементу
		private double _xCoef;                                          //Для даних - відношення різниці між макс. і мін. абсцисою центу "кружка" і макс. і мін. значенням міри що відповідає абсцисі.
		private bool _XLogarithmicScale = false;
		private bool _yAxisLabelsVertical;                              //Вказує чи підписи для координат для осі ординат написані вертикально. Залежить від того чи поміщається найдовший з підписів горизонтально.
		private double _yCoef;                                          //Для даних - відношення різниці між макс. і мін. ординатою центу "кружка" і макс. і мін. значенням міри що відповідає ординаті.
		private bool _YLogarithmicScale = false;

		private static readonly object _EventAction = new object();
		private static readonly object _EventActionsRequired = new object();
		private static readonly object _EventDrillByRequired = new object();
		private static readonly object _EventLevelsRequired = new object();
		private static readonly object _EventPlayStopped = new object();
		private static readonly object _EventPositionChanged = new object();

		private void BSIActions_CloseUp(object aSender, EventArgs aEventArgs)
		{
			BBINoActions.Visibility = BarItemVisibility.Always;
		}
		private void BSIActions_Popup(object aSender, EventArgs aEventArgs)
		{
			for (int i = BSIActions.ItemLinks.Count - 1; i >= 0; i--)
			{
				BarItemLink itemLink = BSIActions.ItemLinks[i];
				if (!itemLink.Item.Equals(BBINoActions))
					BSIActions.RemoveLink(itemLink);
			}
			bool added = false;
			bool itemSelected = _CurrentItem != CI.Default;
			if (itemSelected && Actions.IsAllowed(ACTION_OLAP_ACTION))
			{
				MemberItem[] tuple = _Data.ItemsMemberItems[_CurrentItem.ItemID];
				foreach (MemberItem member in tuple)
				{
					OlapActionCollectionBase actions = EventActionsRequiredRaise(OlapActionType.oatAll, OlapCoordinateType.octMember, member.UniqueName);
					if (actions != null && actions.Count > 0)
					{
						string level = member.LevelName;
						AdvancedBarSubItem bsiLevel = BarManagerHolder.AdvancedBarSubItemCreate();
						bsiLevel.Caption = level.EndsWith("]")
											   ? level.Substring(level.LastIndexOf('[') + 1).Replace("]", string.Empty)
											   : level.Contains(".") ? level.Substring(level.LastIndexOf('.') + 1) : level;
						foreach (OlapActionObjectBase action in actions.Values)
						{
							AdvancedBarButtonItem bbiAction = BarManagerHolder.AdvancedBarButtonItemCreate();
							bbiAction.Caption = action.Caption;
							bbiAction.Name = action.ID;
							bbiAction.Tag = new Pair<OlapActionObjectBase, string>(action, null);
							bbiAction.ItemClick += PopupButton_OlapAction_Click;
							bsiLevel.AddItem(bbiAction);
						}
						BSIActions.AddItem(bsiLevel);
						added = true;
					}
				}
				List<string[]> actionsMeasures = new List<string[]>();
				foreach (OlapMeasureObjectBase item in _Data.Measures)
					if (item != null && !item.IsCalculation && !item.IsKPI
						&& !actionsMeasures.Exists(delegate (string[] obj) { return obj[0] == item.ID; }))
						actionsMeasures.Add(new string[2] { item.ID, item.Caption });
				StringBuilder tupleBuilder = new StringBuilder();
				foreach (MemberItem item in tuple)
				{
					tupleBuilder.Append(item.UniqueName);
					tupleBuilder.Append(",");
				}
				string actionsTuple = tupleBuilder.ToString();
				foreach (string[] measure in actionsMeasures)
					if (measure[0] != null)
					{
						AdvancedBarSubItem bsiMeasure = BarManagerHolder.AdvancedBarSubItemCreate();
						bsiMeasure.Caption = measure[1];
						int actionsCount = 0;
						OlapActionCollectionBase actions = EventActionsRequiredRaise(OlapActionType.oatAll, OlapCoordinateType.octCell, actionsTuple + measure[0]);
						if (actions != null && actions.Count > 0)
							foreach (OlapActionObjectBase action in actions.Values)
							{
								AdvancedBarButtonItem bbiAction = BarManagerHolder.AdvancedBarButtonItemCreate();
								bbiAction.Caption = action.Caption;
								bbiAction.Name = action.ID;
								bbiAction.Tag = new Pair<OlapActionObjectBase, string>(action, measure[0]);
								bbiAction.ItemClick += PopupButton_OlapAction_Click;
								bsiMeasure.AddItem(bbiAction);
								actionsCount++;
							}
						if (actionsCount > 0)
						{
							BSIActions.AddItem(bsiMeasure);
							added = true;
						}
					}
			}
			BBINoActions.Visibility = added ? BarItemVisibility.Never : BarItemVisibility.Always;
		}
		/// <summary>
		/// В даних змінилася поточна група
		/// </summary>
		private void Data_EventCurrentGroupChanged(object aSender, EventArgs aEventArgs)
		{
			if (IsInAction)
				return;
			if (_Data.CurrentGroup != OlapScatterFormsData.GROUP_NONE)
			{
				_groupHighlightShow = true;
				_groupHighlightIndixes = _Data.CurrentGroupItemsIDsGet();
				_timerGroupHighlight.Start();
			}
			else
			{
				_timerGroupHighlight.Stop();
				_groupHighlightIndixes = new List<int>(0);
			}
			Redraw(false);
		}
		/// <summary>
		/// В даних змінився поточний елемент
		/// </summary>
		private void Data_EventCurrentItemChanged(object aSender, EventArgs aEventArgs)
		{
			if (IsInAction)
				return;
			CI currentItem = new CI(_Data.CurrentPage, _Data.CurrentItem);
			if (currentItem.ItemID == -1)
				currentItem = CI.Default;
			if (_CurrentItem.ItemID != currentItem.ItemID)
				CurrentItem = currentItem;
			ItemWayHighlight();
		}
		/// <summary>
		/// Logarithmic scale was enabled or disabled on one of the axes
		/// </summary>
		private void Data_EventLogarithmicScalesSelectionChanged(object aSender, EventArgs aEventArgs)
		{
			_XLogarithmicScale = _Data.XLogarithmicScale;
			_YLogarithmicScale = _Data.YLogarithmicScale;
			_dataCalculated = false;
			Redraw(true);
		}
		/// <summary>
		/// В даних змінилася прозорість для невідмічених елементів
		/// </summary>
		private void Data_EventOpacityChanged(object aSender, EventArgs aEventArgs)
		{
			if (!IsInAction)
				Redraw(false);
		}
		/// <summary>
		/// В даних змінилося вмістиме вибірки
		/// </summary>
		private void Data_EventSelectionChanged(OlapScatterFormsData aSender, EventArgsSelectionChanged aEventArgs)
		{
			if (_Data != null && (_Data.Min.MX.ValueType & MVT.mvtNumber) != MVT.mvtNotSet
				&& (_Data.Min.MY.ValueType & MVT.mvtNumber) != MVT.mvtNotSet)
			{
				if (aEventArgs.ItemSelectionChanged)
					if (aEventArgs.MultipleChanges)
						for (int i = 0; i < _Data.ItemsCount; ++i)
							HintVisibilityUpdate(_Data.PaintOrder[i]);
					else
					{
						if (_highlightedItem.ItemID == aEventArgs.ItemID)
						{
							_timerItemHighlight.Stop();
							_highlightedItem = CI.Default;
						}
						HintVisibilityUpdate(aEventArgs.ItemID);
					}
				Redraw(false);
			}
		}
		/// <summary>
		/// В даних змінився "прапорець" показувати хінти
		/// </summary>
		private void Data_EventShowHintsChanged(object aSender, EventArgs aEventArgs)
		{
			AdvancedHint hint;
			bool showHints = _Data.ShowHints;
			for (int i = 0; i < _Data.ItemsCount; ++i)
			{
				hint = _Data.ItemHintGet(i);
				if (hint != null)
				{
					hint.Highlighted = false;
					hint.CanShow = showHints;
				}
			}
			Redraw(false);
		}
		/// <summary>
		/// В даних змінився "прапорець" показувати шлях
		/// </summary>
		private void Data_EventShowTrailsChanged(object aSender, EventArgs aEventArgs)
		{
			Redraw(false);
		}
		/// <summary>
		/// Для елементів даних для кожного з елементів вираховує колір як екземпляр <c>Color</c> та розмір, X, Y в пікселях.
		/// </summary>
		/// <param name="aData">Дані для елементів яких потрібно обчислити їх властивості що впливають на відображення</param>
		/// <param name="aWidth">Ширина контрола під яку потрібно адаптувати дані</param>
		/// <param name="aHeight">Висота контрола під яку потрібно адаптувати дані</param>
		private void DataPrecalculate(ref OlapScatterFormsData aData, int aWidth, int aHeight)
		{
			Tracer.EnterMethod("OlapScatter.DataPrecalculate()");
			if (_dataCalculated)    //якщо дані вже були адаптовані
				return;
			_dataCalculated = true;
			//Витягуємо в локальні змінні макс. та мін. значення для кожної з мір
			double cMax = aData.Max.MColor.NumericValue;
			double cMin = aData.Min.MColor.NumericValue;
			double sMax = aData.Max.MSize.NumericValue;
			double sMin = aData.Min.MSize.NumericValue;
			double xMax = aData.Max.MX.NumericValue;
			double xMin = aData.Min.MX.NumericValue;
			double yMax = aData.Max.MY.NumericValue;
			double yMin = aData.Min.MY.NumericValue;
			//Обчислюємо відношення між макс. та мін. значеннями можливих властивостей в пікселях та макс. та мін. значеннями мір що відповідають цим властивостям
			_xCoef = xMax == xMin ? 0 : (aWidth - YSLICE_DELTA - 2 * DIST_TO_BOUND) / (xMax - xMin);
			_yCoef = yMax == yMin ? 0 : (aHeight - XSLICE_DELTA - 2 * DIST_TO_BOUND) / (yMax - yMin);
			_sCoef = sMax == sMin || (aData.Min.MSize.ValueType & (MVT.mvtNumber | MVT.mvtDateTime)) == MVT.mvtNotSet
						 ? 0 : (_itemMaxSize - _itemMinSize) / (sMax - sMin);
			#region Coefficients for logarithmic scale
#if DEBUG
			// Перевіряємо чи всі значення одного знаку (і не нульові). Якщо ні, то логарифмічну шкалу неможна використовувати.
			if (_XLogarithmicScale && xMin <= 0 && xMax >= 0)
				throw new ExceptionFramework("Values on X axis have different signs or there are 0 values. Logarithmic scale can't be used in this case.", ExceptionKind.ekDeveloper);
			if (_YLogarithmicScale && yMin <= 0 && yMax >= 0)
				throw new ExceptionFramework("Values on Y axis have different signs or there are 0 values. Logarithmic scale can't be used in this case.", ExceptionKind.ekDeveloper);
#endif
			// знаходимо кількість відрізків на рисунку (дійсне число)
			double xMarksCount = (aWidth - 2d * DIST_TO_BOUND - YSLICE_DELTA) / DIST_BTWN_LINES;
			double yMarksCount = (aHeight - 2d * DIST_TO_BOUND - XSLICE_DELTA) / DIST_BTWN_LINES;
			// знаходимо коефіцієнт геометричної прогресії
			double xValueStep = Math.Pow(xMax / xMin, 1d / xMarksCount);
			double yValueStep = Math.Pow(yMax / yMin, 1d / yMarksCount);
			// знаходимо крок координатної сітки
			double xCoordinateStep = (xMax - xMin) / xMarksCount;
			double yCoordinateStep = (yMax - yMin) / yMarksCount;
			#endregion
			foreach (OlapScatterDataItem[] page in aData.Pages) //проходимось по кожній з сторінок
				if (page != null)   // якщо немає ні сторінок ні елементів, то тут буде одна порожня сторінка
					foreach (OlapScatterDataItem item in page)  //проходимось по всіх елементах кожної з сторінок
					{
						if (item == null || !item.CanShow)  //якщо елементу немає або його не можна відобразити
							continue;
						item.X = (float)(_XLogarithmicScale
							? (YSLICE_DELTA + DIST_TO_BOUND + Math.Log(item.MX.NumericValue / xMin, xValueStep) * xCoordinateStep * _xCoef)
							: (YSLICE_DELTA + DIST_TO_BOUND + (item.MX.NumericValue - xMin) * _xCoef));
						item.Y = aHeight - (float)(_YLogarithmicScale
							? (XSLICE_DELTA + DIST_TO_BOUND + Math.Log(item.MY.NumericValue / yMin, yValueStep) * yCoordinateStep * _yCoef)
							: (XSLICE_DELTA + DIST_TO_BOUND + (item.MY.NumericValue - yMin) * _yCoef));
						//calculate size
						item.Size = _itemMinSize;
						if (aData.Measures[OSDI.M_SIZE_IND] != null)    //якщо міра для розміру втягнута
							if ((item.MSize.ValueType & (MVT.mvtDateTime | MVT.mvtNumber)) != MVT.mvtNotSet)    //якщо значення міри це число чи час
								item.Size = (float)(_itemMinSize + (item.MSize.NumericValue - sMin) * _sCoef);
							else
								item.MeasureInterpolatedSet(OSDI.M_SIZE_IND, true);
						//calculate color
						if (!aData.ColorsFromLevel) //якщо кольори для елементів не обчислені на основі батьківського елементу
						{
							item.Color = GradientUtils.GradientColorGet(_gradient, 0, 0, 0, false);
							if (aData.Measures[OSDI.M_COLOR_IND] != null)   //якщо міра для кольору втягнута
								if ((item.MColor.ValueType & (MVT.mvtNumber | MVT.mvtDateTime)) != MVT.mvtNotSet)   //якщо значення міри це число чи час
									item.Color = GradientUtils.GradientColorGet(_gradient, (decimal)item.MColor.NumericValue, (decimal)cMin, (decimal)cMax, false);
								else    //якщо значення міри не може бути представлено у вигляді числа
								{
									item.MeasureInterpolatedSet(OSDI.M_COLOR_IND, true);
									item.Color = Color.White;
								}
						}
					}
			Tracer.ExitMethod("OlapScatter.DataPrecalculate()");
		}
		/// <summary>
		/// Імпортує нові дані в скеттер на заміну старим
		/// </summary>
		private void DataSet(OlapScatterFormsData aNewData)
		{
			Tracer.EnterMethod("OlapScatter.DataSet()");
			CurrentItem = CI.Default;   //затираємо поточний елемент
			int i;
			if (_Data != null)  //якщо дані вже були присутні на скеттері
			{
				_Data.EventSelectionChanged -= Data_EventSelectionChanged;
				_Data.EventOpacityChanged -= Data_EventOpacityChanged;
				_Data.EventShowTrailsChanged -= Data_EventShowTrailsChanged;
				_Data.EventCurrentItemChanged -= Data_EventCurrentItemChanged;
				_Data.EventCurrentGroupChanged -= Data_EventCurrentGroupChanged;
				_Data.EventLogarithmicScalesSelectionChanged -= Data_EventLogarithmicScalesSelectionChanged;
			}
			if (aNewData != null)
			{
				aNewData.EventSelectionChanged += Data_EventSelectionChanged;
				aNewData.EventOpacityChanged += Data_EventOpacityChanged;
				aNewData.EventShowTrailsChanged += Data_EventShowTrailsChanged;
				aNewData.EventCurrentItemChanged += Data_EventCurrentItemChanged;
				aNewData.EventCurrentGroupChanged += Data_EventCurrentGroupChanged;
				aNewData.EventShowHintsChanged += Data_EventShowHintsChanged;
				aNewData.EventLogarithmicScalesSelectionChanged += Data_EventLogarithmicScalesSelectionChanged;
				if ((aNewData.Min.MX.ValueType & MVT.mvtNumber) != MVT.mvtNotSet && (aNewData.Min.MY.ValueType & MVT.mvtNumber) != MVT.mvtNotSet)
					aNewData.HintsImport(_Data);    //імпортуємо хінти для виділених елментів зі старих даних в нові
			}
			else if (_Data != null) //якщо дані були присутні і нових даних нема
				_Data.HintsDispose();
			if (_Data != null && (_Data.Min.MX.ValueType & MVT.mvtNumber) != MVT.mvtNotSet
				&& (_Data.Min.MY.ValueType & MVT.mvtNumber) != MVT.mvtNotSet)
				_Data.HintsDispose();
			Tracer.Write(TraceLevel.Info, "OlapScatter.DataSet()", "Before new data set");
			_Data = aNewData;
			_dataCalculated = false;
			if (_Data != null)
			{
				_XLogarithmicScale = _Data.XLogarithmicScale;
				_YLogarithmicScale = _Data.YLogarithmicScale;
				DataPrecalculate(ref _Data, Width, Height);
			}
			Tracer.Write(TraceLevel.Info, "OlapScatter.DataSet()", "After new data set");
			if (_Data != null && (_Data.Min.MX.ValueType & MVT.mvtNumber) != MVT.mvtNotSet
				&& (_Data.Min.MY.ValueType & MVT.mvtNumber) != MVT.mvtNotSet)
			{
				CurrentPage = _Data.CurrentPage;
				int op;
				//проходимось по всіх відмічених елементах
				for (i = _Data.SelectedItemsStart; i < _Data.ItemsCount; ++i)
				{
					op = _Data.PaintOrder[i];
					if (_Data.ItemHintGet(op) == null)  //якщо для відмічених елементів нема хінта
					ItemHint_Create(op, false);
				}
				MoreThenOneMemberOnLevelCalculate(_Data);
			}
			Tracer.ExitMethod("OlapScatter.DataSet()");
		}
		private void EventActionRaise(MemberItem[] aData, object aAction, object aState)
		{
			if (Events[_EventAction] != null)
				((EventHandlerOlapScatterAction)Events[_EventAction]).Invoke(aData, aAction, aState);
		}
		private OlapActionCollectionBase EventActionsRequiredRaise(OlapActionType aActionType, OlapCoordinateType aCoordinateType, string aCoordinate)
		{
			if (Events[_EventActionsRequired] != null)
			{
				EventArgsActionsRequired eventArgs = new EventArgsActionsRequired(aActionType, aCoordinateType, aCoordinate);
				((EventHandlerActionsRequired)Events[_EventActionsRequired]).Invoke(this, eventArgs);
				return eventArgs.Actions;
			}
			else
				return null;
		}
		/// <summary>
		/// Повертає список доступних для операції Drill by вимірів, ієрархій, рівнів.
		/// </summary>
		private List<Pair<string, List<Pair<string, object>>>> EventDrillByRequiredRaise()
		{
			if (Events[_EventDrillByRequired] != null)
			{
				EventArgsDrillByRequired eventArgs = new EventArgsDrillByRequired();
				((EventHandlerDrillByRequired)Events[_EventDrillByRequired])(this, eventArgs);
				return eventArgs.DrillByList;
			}
			else
				return null;
		}
		private OlapLevelCollectionBase EventLevelsRequiredRaise(string aHierarchyID)
		{
			if (Events[_EventLevelsRequired] != null)
			{
				EventArgsLevelsRequired eventArgs = new EventArgsLevelsRequired(aHierarchyID);
				((EventHandlerLevelsRequired)Events[_EventLevelsRequired]).Invoke(this, eventArgs);
				return eventArgs.Levels;
			}
			else
				return null;
		}
		private void EventPlayStoppedRaise()
		{
			if (Events[_EventPlayStopped] != null)
				((EventHandler)Events[_EventPlayStopped]).Invoke(this, EventArgs.Empty);
		}
		private void EventPositionChangedRaise()
		{
			if (Events[_EventPositionChanged] != null)
				((EventHandler)Events[_EventPositionChanged]).Invoke(this, EventArgs.Empty);
		}
		/// <summary>
		/// Оновлює межі в яких можуть з'являтися хінти для абсциси і ординати
		/// </summary>
		private void HintBoundsUpdate()
		{
			_hints[OSDI.M_X_IND].SettingsSet(new Point(YSLICE_DELTA + DIST_TO_BOUND, Height - XSLICE_DELTA)
											   , new Point(Width - DIST_TO_BOUND, Height - XSLICE_DELTA)
											   , HintPosition.hpBottom
											   , HintTextOrientation.htoHorizontal);
			_hints[OSDI.M_Y_IND].SettingsSet(new Point(YSLICE_DELTA, Height - XSLICE_DELTA - DIST_TO_BOUND)
											   , new Point(YSLICE_DELTA, DIST_TO_BOUND)
											   , HintPosition.hpLeft
											   , _yAxisLabelsVertical ? HintTextOrientation.htoVertical : HintTextOrientation.htoHorizontal);
		}
		/// <summary>
		/// Генерує текст хінта для заданого елементу
		/// </summary>
		/// <param name="aCurrentItem">Елемент для якого потрібно згенерувати хінт</param>
		/// <param name="aAddPageCaption">Вказує чи додавати до хінта інформацію про сторінку елементу</param>
		/// <param name="aAddItemID">Вказує чи додавати до хінта інформацію про елемент</param>
		/// <returns>Повертає Caption для хінта</returns>
		private string HintConstruct(CI aCurrentItem, bool aAddPageCaption, bool aAddItemID)
		{
			OSDI item = _Data.Pages[aCurrentItem.PageID][aCurrentItem.ItemID];
			if (item == null)
				return string.Empty;
			StringBuilder sb = new StringBuilder();
			if (aAddPageCaption && _Data.PagesPresent)
				sb.AppendLine(_Data.PagesMemberItems[aCurrentItem.PageID].Caption);
			if (aAddItemID)
				foreach (MemberItem mi in _Data.ItemsMemberItems[aCurrentItem.ItemID])
					sb.AppendLine(mi.Caption);
			int i, k = 0;
			for (i = 0; i < OSDI.MEASURES_COUNT; ++i)   //проходимось по всіх мірах
			{
				//якщо міра втягнута і значення не може бути відображене безпосередньо біля міри і значення для цієї міри ще не було додано
				if (_Data.Measures[i] != null
					&& !_hints[i].CanShow
					&& _Data.Measures.FindIndex(delegate (OlapMeasureObjectBase obj) { return obj != null && obj.ID == _Data.Measures[i].ID; }) == i)
				{
					//додаємо Caption і значення міри
					sb.Append(_Data.Measures[i].Caption);
					sb.Append(": ");
					if (item.Measures[k].ValueType == MVT.mvtError)
						sb.AppendLine("#ERR");
					else if (item.Measures[k].ValueType == MVT.mvtNull)
						sb.AppendLine(StringGet(OlapScatter_label_Null));
					else
						if (item.MeasureInterpolatedGet(k))
					{
						sb.Append(INTERPOLATION_CHAR);
						sb.AppendLine(MeasureValueToString(item.Measures[k].NumericValue, 2, _Data.Measures[i]));
					}
					else
						sb.AppendLine(item.Measures[k].FormattedValue);
				}
				++k;
			}
			return sb.ToString();
		}
		/// <summary>
		/// Ховає контрол хінта і встановлює його в непідсвічений стан
		/// </summary>
		/// <param name="aHint">Хінт що потрібно сховати</param>
		private static void HintHide(AdvancedHint aHint)
		{
			aHint.Visible = false;
			aHint.Highlighted = false;
		}
		//private void HintHide(int aItemID, int aPage)
		//{
		//	OSDI item = _Data.Pages[aPage][aItemID];
		//	var marker = (OlapMapCircleMarker)item.Tag;
		//	marker?.OlapToolTip?.HintHide();
		//}
		/// <summary>
		/// Оновлює видимість хінта для елементу в залежності від того чи вибраний елемент чи ні
		/// </summary>
		/// <param name="aID">Номер елементу для якого потрібно оновити видимість</param>
		private void HintVisibilityUpdate(int aID)
		{
			if ((_Data.ItemSelectionGet(aID) & SIST.sistAsItem) == 0)
			{
				if (_Data.ItemHintGet(aID) != null && _Data.ItemHintGet(aID).Visible)
				HintHide(_Data.ItemHintGet(aID));
			}
			else
			{
				_hints[OSDI.MEASURES_COUNT].Visible = false;
				if (_Data.ItemHintGet(aID) == null)
					ItemHint_Create(aID, !IsInAction);
			}
		}
		/// <summary>
		/// Відмальовує хінти по заданому Graphics
		/// </summary>
		/// <param name="aGraphics">Graphics по якому потрібно відмалювати хінти</param>
		/// <param name="aArea">Місце в якому відмальовувалась скеттер діаграма в заданому Graphics</param>
		private void HintsDraw(Graphics aGraphics, Rectangle aArea)
		{
			AdvancedHint hint;
			for (int i = _Data.SelectedItemsStart; i < _Data.ItemsCount; ++i)
			{
				hint = _Data.ItemHintGet(_Data.PaintOrder[i]);
				if (hint != null && hint.Visible)
					hint.DrawToGraphics(aGraphics, new Point(hint.Left + aArea.X, hint.Top + aArea.Y));
			}
		}
		/// <summary>
		/// Ховає всі хінти для для мір та хінти для елементу <paramref name="aCurrentItem"/>
		/// </summary>
		private void HintsHide(CI aCurrentItem)
		{
			int i;
			for (i = 0; i < _hints.Length; ++i)
				_hints[i].HintHide();
			if (aCurrentItem != CI.Default)
			{
				AdvancedHint hint = _Data.ItemHintGet(aCurrentItem.ItemID);
				if (hint != null && hint.Visible && (_Data.ItemSelectionGet(aCurrentItem.ItemID) & SIST.sistAsItem) != 0)
					_Data.ItemHintGet(aCurrentItem.ItemID).Highlighted = false;
			}
		}
		/// <summary>
		/// Відображає хінти для елементу <paramref name="aCurrentItem"/>
		/// </summary>
		private void HintsShow(CI aCurrentItem)
		{
			int line = 0;
			try
			{
				OSDI item = _Data.Pages[aCurrentItem.PageID][aCurrentItem.ItemID];
				line = 1;
				//встановлюємо позицію та відображаємо хінти над мірами
				for (int i = 0; i < OSDI.MEASURES_COUNT; ++i)
				{
					line = 2;
					double minValue = _Data.Min.Measures[i].NumericValue;
					line = 3;
					double maxValue = _Data.Max.Measures[i].NumericValue;
					line = 4;
					var itemMeasure = item.Measures[i];
					line = 5;
					float pos = maxValue.Equals(minValue)
									? 1f
									: (float)(i == OSDI.M_X_IND && _XLogarithmicScale
												   ? (item.X - YSLICE_DELTA - DIST_TO_BOUND) / _xCoef / (maxValue - minValue)
												   : i == OSDI.M_Y_IND && _YLogarithmicScale
														 ? (Height - item.Y - XSLICE_DELTA - DIST_TO_BOUND) / _yCoef / (maxValue - minValue)
													: (itemMeasure.NumericValue - minValue) / (maxValue - minValue));
					line = 6;
					string measureFormattedValue = i == OSDI.M_X_IND && !_Data.XAxisExists || i == OSDI.M_Y_IND && !_Data.YAxisExists
												? itemMeasure.FormattedValue ?? @"null" //vhozov: itemMeasure.FormattedValue contains error message
												: (itemMeasure.ValueType & (MVT.mvtNumber | MVT.mvtDateTime)) != MVT.mvtNotSet
													? item.MeasureInterpolatedGet(i)
														? INTERPOLATION_CHAR + MeasureValueToString(itemMeasure.NumericValue, 2, _Data.Measures[i])
														: MeasureFormatOverriden(_Data.Measures[i].ID)
															? MeasureValueToString(itemMeasure.NumericValue, 2, _Data.Measures[i])
															: itemMeasure.FormattedValue
													: itemMeasure.FormattedValue;
					line = 7;
					_hints[i].HintShow(measureFormattedValue, pos);
				}
				line = 8;
				//show item hint (in OlapScatter)
				bool itemSelected = (_Data.ItemSelectionGet(aCurrentItem.ItemID) & SIST.sistAsItem) != 0;
				line = 9;
				float visibleTime = itemSelected && _Data.ItemSelectionStart[aCurrentItem.ItemID] != _Data.CurrentPage ? ITEM_HINT_VISIBLE_TIME : 0;
				line = 10;
				if (!_Data.ShowHints || !itemSelected)  //якщо над елементом немає "прив'язаного" хінта
				{
					line = 11;
					ItemHintBestPositionSet(_hints[OSDI.MEASURES_COUNT], item, HintConstruct(aCurrentItem, itemSelected, true));
					line = 12;
					_hints[OSDI.MEASURES_COUNT].HighlightColor = ItemHintHighlightColorGet(item);
					_hints[OSDI.MEASURES_COUNT].Highlighted = true;
					_hints[OSDI.MEASURES_COUNT].HintShow(true, visibleTime);
				}
				else    //якщо елемент відмічений і над ним є хінт
				{
					line = 13;
					//підсвічуємо "прив'язаний" хінт
					_Data.ItemHintGet(aCurrentItem.ItemID).HighlightColor = ItemHintHighlightColorGet(item);
					line = 14;
					_Data.ItemHintGet(aCurrentItem.ItemID).Highlighted = true;
					line = 15;
					if (aCurrentItem.PageID != _Data.ItemSelectionStart[aCurrentItem.ItemID])   //якщо поточний елемент належить до іншої сторінки (ми на одному з елементів шляху)
					{
						line = 16;
						//показуємо хінт на елементі шляху
						ItemHintBestPositionSet(_hints[OSDI.MEASURES_COUNT], item, HintConstruct(aCurrentItem, true, false));
						line = 17;
						_hints[OSDI.MEASURES_COUNT].HighlightColor = ItemHintHighlightColorGet(_Data.Pages[aCurrentItem.PageID][aCurrentItem.ItemID]);
						line = 18;
						_hints[OSDI.MEASURES_COUNT].HintShow(true, visibleTime);
					}
				}
				line = 19;
			}
			catch (Exception ex)
			{
				if (FrameworkBaseDefs.Logger != null)
					FrameworkBaseDefs.Logger.Write(new LogRecord("OlapScatter.HintsShow", LogLevel.StubFatal, "Exception in OlapScatter.HintsShow: line=" + line + "; message: " + ex.Message, ex));
			}
		}
		/// <summary>
		/// Малює елемент <paramref name="aItem"/> по <paramref name="aGraphics"/>
		/// </summary>
		private void ItemDraw(Graphics aGraphics, OSDI aItem)
		{
			//var marker = _markerDataItem[aItem];//new OlapMapCircleMarker(new PointLatLng(), aItem);
			//marker.SetDebugPosition(aItem.X, aItem.Y);
			//marker.OnRender(aGraphics);
			float radius = aItem.Size / 2;
			using (SolidBrush brush = new SolidBrush(aItem.Color))
				aGraphics.FillEllipse(brush, aItem.X - radius, aItem.Y - radius, aItem.Size, aItem.Size);
			if (aItem.IsInterpolated)
			{
				float diagPoint = (float)Math.Sqrt(1f / 2) * radius;
				aGraphics.DrawLine(PEN_INTERPOLATED_LINE, aItem.X - diagPoint, aItem.Y + diagPoint, aItem.X + diagPoint, aItem.Y - diagPoint);
			}
			aGraphics.DrawEllipse(PEN_ITEM_LINE, aItem.X - radius, aItem.Y - radius, aItem.Size, aItem.Size);
		}
		/// <summary>
		/// Підсвічує елемент <paramref name="aItem"/> відмальовуючи його разом з підсвіткою по <paramref name="aGraphics"/>
		/// </summary>
		private static void ItemHighlight(Graphics aGraphics, OSDI aItem)
		{
			var marker = (OlapMapCircleMarker) aItem.Tag;
			marker.IsVisible = aItem.CanShow;
			marker.IsHighlighted = true;
			marker.OnRender(aGraphics);

			//if (!aItem.CanShow)
			//	return;
			//float radius = aItem.Size / 2;
			//RectangleF bounds = new RectangleF(aItem.X - radius, aItem.Y - radius, aItem.Size, aItem.Size);
			//float itemSizePercent = bounds.Width / (bounds.Width + 2 * ITEM_HIGHLIGHT_WIDTH);
			//float tmp = (bounds.Width / itemSizePercent) - bounds.Width;
			//RectangleF highlightBounds = bounds;
			//highlightBounds.Inflate(tmp / 2, tmp / 2);
			//using (GraphicsPath circle = new GraphicsPath())
			//{
			//	circle.AddEllipse(bounds);
			//	Region oldClip = aGraphics.Clip;
			//	aGraphics.IntersectClip(_itemsArea);
			//	using (Region commonClip = aGraphics.Clip)
			//	{
			//		using (Region circleClip = new Region(circle))
			//			aGraphics.ExcludeClip(circleClip);
			//		using (GraphicsPath pth = new GraphicsPath())
			//		{
			//			pth.AddEllipse(highlightBounds);
			//			using (PathGradientBrush pgb = new PathGradientBrush(pth))
			//			{
			//				pgb.CenterColor = COLOR_HIGHLIGHT_DARK;
			//				pgb.SurroundColors = new Color[] { COLOR_HIGHLIGHT_SURROUND };
			//				pgb.FocusScales = new PointF(itemSizePercent, itemSizePercent);
			//				pgb.CenterPoint = new PointF(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
			//				aGraphics.FillEllipse(pgb, highlightBounds);
			//			}
			//		}
			//		aGraphics.Clip = commonClip;
			//		ItemDraw(aGraphics, aItem);
			//	}
			//	aGraphics.Clip = oldClip;
			//}
		}
		/// <summary>
		/// Створює новий хінт для елементу з ID == <paramref name="aItemID"/>
		/// </summary>
		/// <param name="aWithHighlight">Якщо <c>true</c>, то хінт буде підсвічено</param>
		private void ItemHint_Create(int aItemID, bool aWithHighlight)
		{
			int page = _Data.CurrentPage;
			//AdvancedHint hint = _Data.ItemHintGet(aItemID);
			//if (hint == null)
			//{
			//	hint = _Data.ItemHintCreate(aItemID, this);
			//	hint.ShowPointer = false;
			//	hint.LocationChanged += ItemHint_EventLocationChanged;
			//	hint.MouseEnter += ItemHint_EventMouseEnter;
			//	hint.MouseLeave += ItemHint_EventMouseLeave;
			//	hint.EventDragEnd += ItemHint_EventDragEnd;
			//	hint.EventDragStart += ItemHint_EventDragStart;
			//	hint.EventClose += ItemHint_EventClose;
			//	hint.MouseUp += ItemHint_EventMouseUp;
			//}
			//hint.ID = aItemID;
			//hint.CanDrag = true;
			//hint.CanShow = _Data.ShowHints;
			var hint = ItemHintGet(aItemID);
			hint.CanDrag = true;
			var item = _Data.Pages[page][aItemID];
			//var marker = (OlapMapCircleMarker)item.Tag;
			////var tooltip = new OlapMapToolTip(marker, () => ItemHintGet(aItemID));
			//marker.ToolTipText = HintConstruct(new CI(_Data.CurrentPage, aItemID), true, true);
			//marker.OlapToolTip.Show(aWithHighlight);
			_hints[OSDI.MEASURES_COUNT].HintHide();			
			ItemHintBestPositionSet(hint, item, HintConstruct(new CI(page, aItemID), true, true));
			if (item.CanShow)
			{
				hint.HighlightColor = item.Color;
				hint.Highlighted = aWithHighlight;
			}
			hint.HintShow(false);
		}
		//private void ItemHint_Create(int aItemID, bool aWithHighlight)
		//{
		//	OSDI item = _Data.Pages[_Data.CurrentPage][aItemID];
		//	var marker = (OlapMapCircleMarker)item.Tag;

		//	//var tooltip = new OlapMapToolTip(marker, () => ItemHintGet(aItemID));
		//	marker.ToolTipText = HintConstruct(new CI(_Data.CurrentPage, aItemID), true, true);
		//	marker.OlapToolTip.Show(aWithHighlight);

		//	int page = _Data.CurrentPage;
		//	AdvancedHint hint = _Data.ItemHintGet(aItemID);
		//	if (hint == null)
		//	{
		//		hint = _Data.ItemHintCreate(aItemID, this);
		//		hint.ShowPointer = false;
		//		hint.LocationChanged += ItemHint_EventLocationChanged;
		//		hint.MouseEnter += ItemHint_EventMouseEnter;
		//		hint.MouseLeave += ItemHint_EventMouseLeave;
		//		hint.EventDragEnd += ItemHint_EventDragEnd;
		//		hint.EventDragStart += ItemHint_EventDragStart;
		//		hint.EventClose += ItemHint_EventClose;
		//		hint.MouseUp += ItemHint_EventMouseUp;
		//	}
		//	hint.ID = aItemID;
		//	hint.CanDrag = true;
		//	hint.CanShow = _Data.ShowHints;
		//	_hints[OSDI.MEASURES_COUNT].HintHide();
		//	OSDI item = _Data.Pages[page][aItemID];
		//	ItemHintBestPositionSet(hint, item, HintConstruct(new CI(page, aItemID), true, true));
		//	if (item.CanShow)
		//	{
		//		hint.HighlightColor = item.Color;
		//		hint.Highlighted = aWithHighlight;
		//	}
		//	hint.HintShow(false);
		//}
		/// <summary>
		/// Відмальовує скеттер сторінку при переміщенні хінта
		/// </summary>
		/// <param name="aItem">Елемент хінт якого переміщується</param>
		/// <param name="aInvalidRect">Регіон що змінився внаслідок переміщення</param>
		private void ItemHintDraggingRedraw(CI aItem, out Rectangle aInvalidRect)
		{
			Graphics graphics = _bufferedPage.BufferedGraphics; //CreateGraphics();//
			OSDI item = _Data.Pages[aItem.PageID][aItem.ItemID];
			AdvancedHint hint = _Data.ItemHintGet(aItem.ItemID);
			//calculate Rectangle which must be redrawn
			aInvalidRect = new Rectangle((int)item.X, (int)item.Y, 0, 0);
			int itemDiam = (int)(item.Size / 2 + ITEM_HIGHLIGHT_WIDTH) + 2;
			aInvalidRect.Inflate(itemDiam, itemDiam);
			aInvalidRect = Rectangle.Union(aInvalidRect, new Rectangle(hint.PreviousLocation, hint.Size));
			aInvalidRect = Rectangle.Intersect(new Rectangle(0, 0, Width, Height), aInvalidRect);
			//redraw from cache
			_cachedBeforeDrag.Draw(graphics, aInvalidRect);
			//draw line from item to hint
			LineItemHintToItemDraw(graphics, hint, item.X, item.Y);
			// dont highlight item! (because item will be highlighted in OnPaint())
			//ItemHighlight(graphics, item);
		}
		private void ItemHint_EventClose(object aSender, EventArgs aEventArgs)
		{
			AdvancedHint hint = aSender as AdvancedHint;
			if (hint != null)
				_Data.ItemUnselect(hint.ID);
		}
		private void ItemHint_EventDragEnd(object aSender, EventArgs aEventArgs)
		{
			_draggedHint = null;
			Redraw(false);
			if (_cachedBeforeDrag != null)
			{
				_cachedBeforeDrag.Dispose();
				_cachedBeforeDrag = null;
			}
		}
		private void ItemHint_EventDragStart(object aSender, EventArgs aEventArgs)
		{
			_draggedHint = aSender as AdvancedHint;
			_cachedBeforeDrag = new CachedBmp(Width, Height, CreateGraphics());
			Redraw(_cachedBeforeDrag.BufferedGraphics, new Rectangle(0, 0, Width, Height), false, false);
		}
		private void ItemHint_EventMouseEnter(object aSender, EventArgs aEventArgs)
		{
			if (IsInAction || PopupMenu.Visible)
				return;
			Tracer.EnterMethod("OlapScatter.ItemHint_EventMouseEnter()");
			_mouseAt = MAC.macHint;
			AdvancedHint hint = aSender as AdvancedHint;
			if (hint != null)
				CurrentItem = new CI(_Data.ShowTrails ? _Data.ItemSelectionStart[hint.ID] : _Data.CurrentPage, hint.ID);
			Tracer.ExitMethod("OlapScatter.ItemHint_EventMouseEnter()");
		}
		private void ItemHint_EventMouseLeave(object aSender, EventArgs aEventArgs)
		{
			if (IsInAction || PopupMenu.Visible)
				return;
			AdvancedHint hint = aSender as AdvancedHint;
			if (hint != null && _mouseAt == MAC.macHint)
				CurrentItem = CI.Default;
		}
		private void ItemHint_EventMouseUp(object aSender, MouseEventArgs aEventArgs)
		{
			if (IsInAction || PopupMenu.Visible)
				return;
			AdvancedHint hint = aSender as AdvancedHint;
			if (hint != null)
				CurrentItem = new CI(_Data.ShowTrails ? _Data.ItemSelectionStart[hint.ID] : _Data.CurrentPage, hint.ID);
			if (aEventArgs.Button == MouseButtons.Right && IsActual)
				PopupShow(aEventArgs.X + ((AdvancedHint)aSender).Left, aEventArgs.Y + ((AdvancedHint)aSender).Top);
		}
		private void ItemHint_EventLocationChanged(object aSender, EventArgs aEventArgs)
		{
			if (_itemHintRelocating)
				return;
			AdvancedHint hint = aSender as AdvancedHint;
			if (hint != null && hint.Visible && _CurrentItem != CI.Default)
			{
				Rectangle rect;
				ItemHintDraggingRedraw(_CurrentItem, out rect);
				Invalidate(rect);
				Update();
			}
		}
		/// <summary>
		/// Встановлює оптимальну позицію для хінта <paramref name="aHint"/> відносно елемента <paramref name="aItem"/>
		/// </summary>
		/// <param name="aCaption">Текст що буде написано в хінті</param>
		private void ItemHintBestPositionSet(AdvancedHint aHint, OSDI aItem, string aCaption)
		{
			if (aHint.Visible)
				aHint.HintHide();
			ItemHintPositionSet(aHint, aItem.X, aItem.Y, aItem.Size);
			Rectangle hintArea = DrawArea;
			aHint.HintArea = hintArea;
			aHint.HintPosition = HintPosition.hpTop;
			aHint.TextOrientation = HintTextOrientation.htoHorizontal;
			aHint.HintPointerLocation = HintPointerLocation.hplFar;
			aHint.Caption = aCaption;
			aHint.PointerPosition = 0f;
			aHint.SizePrecalculate();
			if (aHint.Location.X <= hintArea.X)
			{
				if (aHint.Location.Y <= hintArea.Y)
				{
					aHint.HintPosition = HintPosition.hpBottom;
					aHint.PointerPosition = 1f;
				}
				aHint.HintPointerLocation = HintPointerLocation.hplNear;
			}
			else if (aHint.Location.Y <= hintArea.Y)
			{
				aHint.HintPosition = HintPosition.hpBottom;
				aHint.PointerPosition = 1f;
			}
		}
		/// <summary>
		/// Повертає колір яким потрібно підсвітити хінт над елементом <paramref name="aItem"/>.
		/// </summary>
		/// <returns>Колір елементу, або <c>Color.LightGray</c> якщо колір не визначений(елемент є білим).</returns>
		private static Color ItemHintHighlightColorGet(OSDI aItem)
		{
			return aItem.Color == Color.White ? Color.LightGray : aItem.Color;
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
			Point itemHintPoint1 = new Point((int)Math.Ceiling(aItemX), (int)Math.Ceiling(aItemY - aItemSize / 2 - ITEM_HIGHLIGHT_WIDTH / 2));
			Point itemHintPoint2 = new Point((int)Math.Ceiling(aItemX), (int)Math.Ceiling(aItemY + aItemSize / 2 + ITEM_HIGHLIGHT_WIDTH / 2));
			aHint.PositionSet(itemHintPoint1, itemHintPoint2);
		}
		/// <summary>
		/// Відмальовує всі елементи разом з їх шляхами і лініями що сполучають елементи з прив'язаними до них хінтами.
		/// </summary>
		/// <param name="aGraphics">Graphics використовуючи який потрібно малювати</param>
		private void ItemsDraw(Graphics aGraphics)
		{
			_markersLayer.Markers.Clear();
			int j;
			int pos = _Data.CurrentPage;
			float coef = _CurrentPage - pos;
#if DEBUG
			int drawedItems = 0;
#endif
			#region Пояснення.
			/*
			 * В даному циклі змінна "і" містить порядковий номер елементу в масиві PaintOrder.
			 * У випадку підсвітки поточної групи елементів ці елементи потрібно почергово показувати і не показувати, 
			 *	але при тому їхня позиція в масиві PaintOrder не змінюється і у випадку їх показу вони повинні бути поверх всіх елементів.
			 * Тому цикл проходить по всіх елементах, а потім ще й по елементах поточної підсвічуваної групи. 
			 * Це зроблено лише з тою метою щоб не дублювати тіло циклу (виносити його в окремий метод теж не добре бо це при малюванні кожного з елементів додаткові затрати часу).
			 */
			#endregion
			for (j = 0; j < _Data.ItemsCount + _groupHighlightIndixes.Count; ++j)
			{
				//Визначаємо номер елементу
				int i;
				int op;
				if (j < _Data.ItemsCount)
				{
					i = j;
					op = _Data.PaintOrder[i];
				}
				else
				{
					if (!_groupHighlightShow)
						break;
					i = _Data.OrderPaintIndexGet(_groupHighlightIndixes[j - _Data.ItemsCount]);
					op = _Data.PaintOrder[i];
				}
				//визначаємо прозорість елементу
				var a = _Data.ItemOpacityGet(op);
				if (_groupHighlightIndixes.Contains(op))
					if (j < _Data.ItemsCount)
						continue;
					else
						a = 255;
				//для відмальовки шляху елементу на сторінці де цього елементу нема
				if (_Data.ShowTrails && i >= _Data.SelectedItemsStart
					&& (!_Data.Pages[pos][op].CanShow || (coef != 0 && !_Data.Pages[pos + 1][op].CanShow)))
				{
					SelectedItemDraw(aGraphics, op, -1, -1, -1);
					continue;
				}
				if (!_Data.Pages[pos][op].CanShow)  //якщо елемент не можна відобразити на сторінці
				{
					if (i >= _Data.SelectedItemsStart)
						HintHide(_Data.ItemHintGet(op));
					continue;
				}
				bool interpolated;
				float x;
				float s;
				float y;
				Color c;
				if (coef == 0)  //якщо для поточної позиції дані присутні (ми не між двох сторінок)
				{
					interpolated = _Data.Pages[pos][op].IsInterpolated;
					x = _Data.Pages[pos][op].X;
					y = _Data.Pages[pos][op].Y;
					s = _Data.Pages[pos][op].Size;
					c = Color.FromArgb(a, _Data.Pages[pos][op].Color);

				}
				else    //відмальовка відбувається для позиції що знаходитьсь між двох сторінок
				{
					var p = _Data.Pages[pos][op];
					var n = _Data.Pages[pos + 1][op];
					if (!n.CanShow)
					{
						if (i >= _Data.SelectedItemsStart)
							HintHide(_Data.ItemHintGet(op));
						continue;
					}
					interpolated = p.IsInterpolated | n.IsInterpolated;
					x = p.X.Interpolate(n.X, coef);
					y = p.Y.Interpolate(n.Y, coef);
					s = p.Size.Interpolate(n.Size, coef);
					c = p.Color.Interpolate(n.Color, coef, a);					
				}
#if DEBUG
				++drawedItems;
#endif
				if (i >= _Data.SelectedItemsStart)  //якщо елемент належить до вибірки
					SelectedItemDraw(aGraphics, op, x, y, s);
				//малюємо елемент на сторінці
				var radius = s / 2;
				// Додаткова перевірка, щоб уникнути OverflowException
				if (aGraphics.ClipBounds.Contains(x, y) && Math.Abs(s) <= 1000000f)
				{
					var item = _Data.Pages[pos][op];
					var marker = new OlapMapCircleMarker(new PointLatLng(item.Latitude, item.Longitude), () => ItemHintGet(op));
					item.Tag = marker;

					marker.PageID = pos;
					marker.ItemID = op;
					marker.IsVisible = item.CanShow;
					marker.Size = new SizeF(s, s);
					marker.BorderAlpha = a;
					marker.MarkerColor = c;
					marker.IsInterpolated = interpolated;
					marker.IsHighlighted = false;

					marker.Position = FromLocalToLatLng((int)x, (int)y);

					_markersLayer.Markers.Add(marker);

					//if (!aItem.CanShow)
					//	return;
					//float radius = aItem.Size / 2;
					//RectangleF bounds = new RectangleF(aItem.X - radius, aItem.Y - radius, aItem.Size, aItem.Size);
					//float itemSizePercent = bounds.Width / (bounds.Width + 2 * ITEM_HIGHLIGHT_WIDTH);
					//float tmp = (bounds.Width / itemSizePercent) - bounds.Width;
					//RectangleF highlightBounds = bounds;
					//highlightBounds.Inflate(tmp / 2, tmp / 2);
					//using (GraphicsPath circle = new GraphicsPath())
					//{
					//	circle.AddEllipse(bounds);
					//	Region oldClip = aGraphics.Clip;
					//	aGraphics.IntersectClip(_itemsArea);
					//	using (Region commonClip = aGraphics.Clip)
					//	{
					//		using (Region circleClip = new Region(circle))
					//			aGraphics.ExcludeClip(circleClip);
					//		using (GraphicsPath pth = new GraphicsPath())
					//		{
					//			pth.AddEllipse(highlightBounds);
					//			using (PathGradientBrush pgb = new PathGradientBrush(pth))
					//			{
					//				pgb.CenterColor = COLOR_HIGHLIGHT_DARK;
					//				pgb.SurroundColors = new Color[] { COLOR_HIGHLIGHT_SURROUND };
					//				pgb.FocusScales = new PointF(itemSizePercent, itemSizePercent);
					//				pgb.CenterPoint = new PointF(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
					//				aGraphics.FillEllipse(pgb, highlightBounds);
					//			}
					//		}
					//		aGraphics.Clip = commonClip;
					//		ItemDraw(aGraphics, aItem);
					//	}
					//	aGraphics.Clip = oldClip;
					//}
				}
			}
			//OnPaintOverlays(aGraphics);
#if DEBUG
			using (var font = FontUtils.FontCreate("Verdana", 8))
			using (var brush = new SolidBrush(Color.FromArgb(100, Color.Black)))
				aGraphics.DrawString(string.Format("{0}({1})", drawedItems, _Data.ItemsCount), font, brush, XSLICE_DELTA + 2, 3);
#endif
		}
		/// <summary>
		/// Для поточного елементу стартує відображення його шляху якщо він є.
		/// </summary>
		private void ItemWayHighlight()
		{
			_timerItemHighlight.Stop();
			_highlightedItem = CI.Default;
			if (_CurrentItem != CI.Default
				&& _Data.ItemSelectionStart[_CurrentItem.ItemID] != -1
				&& _Data.ItemSelectionStart[_CurrentItem.ItemID] < _Data.CurrentPage
				&& (_Data.ItemSelectionGet(_CurrentItem.ItemID) & ScatterItemSelectionType.sistAsItem) != 0)
				_timerItemHighlight.Start();
			Invalidate();
		}
		private void LevelIDsGet(string aHierarchyID, string aLevelID, ref string aPrev, ref string aNext)
		{
			bool founded = false;
			aPrev = null;
			aNext = null;
			string level = null;
			OlapLevelCollectionBase levels = EventLevelsRequiredRaise(aHierarchyID);
			if (levels != null)
				foreach (DictionaryEntry entry in levels)
				{
					if (founded)
					{
						aNext = (string)entry.Key;
						break;
					}
					if ((string)entry.Key == aLevelID)
					{
						founded = true;
						aPrev = level;
					}
					level = (string)entry.Key;
				}
		}
		private bool MeasureFormatOverriden(string aMeasureID)
		{
			FormatSettings format = FormattingUtils.FormatSettingsGet(MeasuresFormatRules, aMeasureID);
			return format != null &&
				   (format.FormatType == FormatType.ftDate && format.DateCategory != FormatDateCategory.fdcDefault ||
					format.FormatType == FormatType.ftNumber && format.NumberCategory != FormatNumberCategory.fcDefault);
		}
		/// <summary>
		/// Форматує значення міри.
		/// </summary>
		/// <param name="aValue">Значення що потрібно відформатувати</param>
		/// <param name="aDigitsAfterPoint">Кількість точок після коми</param>
		/// <param name="aMeasure">Міра, значення якої передано в параметрі <paramref name="aValue"/>.</param>
		/// <returns>Повертає відформатоване значення <paramref name="aValue"/> відносно параметрів <paramref name="aDigitsAfterPoint"/> і <paramref name="aMeasure"/>.</returns>
		private string MeasureValueToString(double aValue, int aDigitsAfterPoint, OlapMeasureObjectBase aMeasure)
		{
			return MeasuresFormatRules.MeasureValueToString(aValue, aDigitsAfterPoint, aMeasure);
		}
		/// <summary>
		/// Оновлює мінімальний розмір скеттер контрола.
		/// </summary>
		private void MinimumSizeUpdate()
		{
			MinimumSize = new Size(YSLICE_DELTA + 2 * DIST_TO_BOUND, XSLICE_DELTA + 2 * DIST_TO_BOUND);
		}
		private void MoreThenOneMemberOnLevelCalculate(OlapScatterFormsData aData)
		{
			if (aData.ItemsMemberItems != null && aData.ItemsMemberItems.Length > 0 && aData.ItemsMemberItems[0].Length > 0)
			{
				_moreThanOneElementOnLevel = new bool[aData.ItemsMemberItems[0].Length];
				for (int i = 0; i < _moreThanOneElementOnLevel.Length; ++i)
				{
					string memberName = null;
					_moreThanOneElementOnLevel[i] = false;
					for (int j = aData.ItemsMemberItems.Length - 1; j >= 0; --j)
						if (aData.ItemsMemberItems[j][i].UniqueName != memberName)
						{
							if (memberName == null)
								memberName = aData.ItemsMemberItems[j][i].UniqueName;
							else
							{
								_moreThanOneElementOnLevel[i] = true;
								break;
							}
						}
				}
			}
		}
		/// <summary>
		/// Відмальовує координатну сітку.
		/// </summary>
		/// <param name="aGraphics">Graphics використовуючи який потрібно малювати координатну сітку</param>
		/// <param name="aWidth">Ширина скеттер контрола</param>
		/// <param name="aHeight">Висота скеттер контрола</param>
		private void CoordinatesDraw(Graphics aGraphics, int aWidth, int aHeight)
		{
			Tracer.EnterMethod("OlapScatter.CoordinatesDraw()");
			aGraphics.CoordinatesDraw(aWidth, aHeight, MeasuresFormatRules, _Data, _XLogarithmicScale, _YLogarithmicScale, _xCoef, _yCoef, ref _yAxisLabelsVertical, HintBoundsUpdate);
			Tracer.ExitMethod("OlapScatter.CoordinatesDraw()");
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
		private void OlapScatter_EventBoundsUpdate(object aSender, EventArgs aEventArgs)
		{
			HintBoundsUpdate();
		}
		private void PopupButton_DrillBy_Click(object aSender, ItemClickEventArgs aEventArgs)
		{
			EventActionRaise(_Data.ItemsMemberItems[_CurrentItemForPopup.ItemID], ACTION_DRILL_BY, aEventArgs.Item.Tag);
		}
		private void PopupButton_DrillByOnNewPage_Click(object aSender, ItemClickEventArgs aEventArgs)
		{
			EventActionRaise(_Data.ItemsMemberItems[_CurrentItemForPopup.ItemID], ACTION_DRILL_BY_ON_NEW_PAGE, aEventArgs.Item.Tag);
		}
		private void PopupButton_DrillUpDown_Click(object aSender, ItemClickEventArgs aEventArgs)
		{
			EventActionRaise(_Data.ItemsMemberItems[_CurrentItemForPopup.ItemID], (aEventArgs.Item.Tag as object[])[0], (aEventArgs.Item.Tag as object[])[1]);
		}
		private void PopupButton_Drillthrough_Click(object aSender, ItemClickEventArgs aEventArgs)
		{
			int tupleSize = _Data.ItemsMemberItems[_CurrentItemForPopup.ItemID].Length + (_Data.PagesPresent ? 1 : 0);
			MemberItem[] tuple = new MemberItem[tupleSize];
			for (int i = 0; i < _Data.ItemsMemberItems[_CurrentItemForPopup.ItemID].Length; i++)
				tuple[i] = _Data.ItemsMemberItems[_CurrentItemForPopup.ItemID][i];
			if (_Data.PagesPresent)
				tuple[tupleSize - 1] = _Data.PagesMemberItems[_CurrentItemForPopup.PageID];
			EventActionRaise(tuple, ACTION_DRILLTHROUGH, aEventArgs.Item.Name);
		}
		private void PopupButton_OlapAction_Click(object aSender, ItemClickEventArgs aEventArgs)
		{
			int tupleSize = _Data.ItemsMemberItems[_CurrentItemForPopup.ItemID].Length + (_Data.PagesPresent ? 1 : 0);
			MemberItem[] tuple = new MemberItem[tupleSize];
			for (int i = 0; i < _Data.ItemsMemberItems[_CurrentItemForPopup.ItemID].Length; i++)
				tuple[i] = _Data.ItemsMemberItems[_CurrentItemForPopup.ItemID][i];
			if (_Data.PagesPresent)
				tuple[tupleSize - 1] = _Data.PagesMemberItems[_CurrentItemForPopup.PageID];
			EventActionRaise(tuple, ACTION_OLAP_ACTION, aEventArgs.Item.Tag);
		}
		private void PopupButton_HideShow_Click(object aSender, ItemClickEventArgs aEventArgs)
		{
			EventActionRaise(_Data.ItemsMemberItems[_CurrentItemForPopup.ItemID], (aEventArgs.Item.Tag as object[])[0], (aEventArgs.Item.Tag as object[])[1]);
		}
		private void PopupShow(int X, int Y)
		{
			if (IsInAction)
				return;
			try
			{
				bool itemSelected = _CurrentItem != CI.Default;
				if (itemSelected)
				{
					HintsHide(_CurrentItem);
					_CurrentItemForPopup = _CurrentItem;
				}
				MemberItem[] tuple = itemSelected ? _Data.ItemsMemberItems[_CurrentItem.ItemID] : null;
				bool drillthroughVisible = _CanDrillthrough && !_IsViewMode,
					 olapActionsVisible = _CanOlapActions && !_IsViewMode;
				if (drillthroughVisible)
				{
					List<OlapMeasureObjectBase> measures = _Data.Measures;
					List<string[]> drillthroughMeasures = new List<string[]>();
					bool added = false;
					foreach (OlapMeasureObjectBase item in measures)
					{
						added = true;
						if (item != null && !item.IsCalculation && !item.IsKPI && !item.IsCalculated
							&& !drillthroughMeasures.Exists(delegate (string[] obj) { return obj[0] == item.ID; }))
							drillthroughMeasures.Add(new string[2] { item.ID, item.Caption });
						else
							drillthroughMeasures.Add(new string[2] { null, null });
					}
					if (!added)
						drillthroughMeasures.Add(new string[2] { null, null });
					int drillthroughSubitemsCount = drillthroughMeasures.Count;
					BSIDrillthrough.ClearLinks();
					foreach (string[] measure in drillthroughMeasures)
						if (measure[0] != null)
						{
							AdvancedBarButtonItem bbi = BarManagerHolder.AdvancedBarButtonItemCreate();
							bbi.Caption = measure[1];
							bbi.Name = measure[0];
							bbi.ItemClick += PopupButton_Drillthrough_Click;
							BSIDrillthrough.AddItem(bbi);
						}
						else
							--drillthroughSubitemsCount;
					BSIDrillthrough.Enabled = itemSelected && drillthroughSubitemsCount != 0;
					BSIDrillthrough.Visibility = BarItemVisibility.Always;
					Actions.Enable(ACTION_DRILLTHROUGH, itemSelected);
				}
				else
					BSIDrillthrough.Visibility = BarItemVisibility.Never;
				Actions.Enable(ACTION_OLAP_ACTION, olapActionsVisible && itemSelected);
				BSIActions.Visibility = olapActionsVisible ? BarItemVisibility.Always : BarItemVisibility.Never;
				Actions.Enable(ACTION_DRILL_BY, Properties.CanDrillBy && !IsViewMode && itemSelected);
				Actions.Visible(ACTION_DRILL_BY, Properties.CanDrillBy && !IsViewMode);
				Actions.Enable(ACTION_DRILL_BY_ON_NEW_PAGE, Properties.CanDrillByOnNewPage && !IsViewMode && itemSelected);
				Actions.Visible(ACTION_DRILL_BY_ON_NEW_PAGE, Properties.CanDrillByOnNewPage && !IsViewMode);
				BSIDrillBy.Enabled = Properties.CanDrillBy && !IsViewMode && itemSelected;
				BSIDrillByOnNewPage.Enabled = Properties.CanDrillByOnNewPage && !IsViewMode && itemSelected;
				BSIDrillBy.Visibility = Properties.CanDrillBy && !IsViewMode ? BarItemVisibility.Always : BarItemVisibility.Never;
				BSIDrillByOnNewPage.Visibility = Properties.CanDrillByOnNewPage && !IsViewMode ? BarItemVisibility.Always : BarItemVisibility.Never;
				string prev = null, next = null;
				if (itemSelected)
					LevelIDsGet(tuple[0].Hierarchy, tuple[0].LevelName, ref prev, ref next);
				Actions.Enable(ACTION_DRILL_UP, itemSelected && prev != null);
				Actions.Enable(ACTION_DRILL_DOWN, itemSelected && next != null && !tuple[0].IsCalculatedMember);
				bool canNavigate = _CanNavigate && !IsViewMode && (!itemSelected || tuple.Length == 1);
				Actions.Visible(ACTION_DRILL_UP, canNavigate);
				Actions.Visible(ACTION_DRILL_DOWN, canNavigate);
				Actions.Enable(ACTION_HIDE_ITEM, itemSelected && _CanNavigate && _moreThanOneElementOnLevel[0]);
				Actions.Enable(ACTION_HIDE_SIBLINGS, itemSelected && _CanNavigate && _moreThanOneElementOnLevel[0]);
				Actions.Enable(ACTION_SHOW_SIBLINGS, itemSelected && _CanNavigate);
				Actions.Visible(ACTION_HIDE_ITEM, canNavigate);
				Actions.Visible(ACTION_HIDE_SIBLINGS, canNavigate);
				Actions.Visible(ACTION_SHOW_SIBLINGS, canNavigate);
				if (_CanNavigate && !IsViewMode && itemSelected && tuple.Length > 1)
				{
					BSIDrillUp.ClearLinks();
					BSIDrillDown.ClearLinks();
					AdvancedBarButtonItem bbItem;
					for (int k = 0; k < tuple.Length; ++k)
					{
						LevelIDsGet(tuple[k].Hierarchy, tuple[k].LevelName, ref prev, ref next);
						//add drill up
						bbItem = BarManagerHolder.AdvancedBarButtonItemCreate();
						bbItem.Caption = tuple[k].Caption;
						bbItem.Tag = new object[2] { ACTION_DRILL_UP, k };
						bbItem.ItemClick += PopupButton_DrillUpDown_Click;
						bbItem.Enabled = prev != null;
						BSIDrillUp.AddItem(bbItem);
						//add drill down
						bbItem = BarManagerHolder.AdvancedBarButtonItemCreate();
						bbItem.Caption = tuple[k].Caption;
						bbItem.Tag = new object[2] { ACTION_DRILL_DOWN, k };
						bbItem.ItemClick += PopupButton_DrillUpDown_Click;
						bbItem.Enabled = next != null && !tuple[k].IsCalculatedMember;
						BSIDrillDown.AddItem(bbItem);
					}
					//add hide/show
					for (int i = 0; i < 3; ++i)
					{
						AdvancedBarButtonItem[] bsi = new AdvancedBarButtonItem[tuple.Length];
						for (int k = 0; k < tuple.Length; ++k)
						{
							bsi[k] = BarManagerHolder.AdvancedBarButtonItemCreate();
							bsi[k].Caption = tuple[k].Caption;
							bsi[k].Tag = new object[2] { i == 0 ? ACTION_HIDE_ITEM : i == 1 ? ACTION_HIDE_SIBLINGS : ACTION_SHOW_SIBLINGS, k };
							bsi[k].ItemClick += PopupButton_HideShow_Click;
							bsi[k].Enabled = i == 2 || _moreThanOneElementOnLevel[k];
						}
						BSIHideShowItems[i].ClearLinks();
						BSIHideShowItems[i].AddItems(bsi);
					}
				}
				BarItemVisibility navigationVisibility = _CanNavigate && !IsViewMode && itemSelected && tuple.Length > 1 ? BarItemVisibility.Always : BarItemVisibility.Never;
				BSIDrillUp.Visibility = BSIDrillDown.Visibility = navigationVisibility;
				foreach (AdvancedBarSubItem item in BSIHideShowItems)
					item.Visibility = navigationVisibility;
				Actions.Enable(ACTION_HIGHLIGHT, _Data.Measures[OlapScatterDataItem.M_COLOR_IND] != null);
				Actions.Visible(ACTION_FORMAT, !IsViewMode && _CanFormatting);
				Actions.Visible(ACTION_EXPORT_PNG, _canExportPng);
				Actions.Visible(ACTION_EXPORT_NRP, _canExportNRP && !IsViewMode);
				Actions.Visible(ACTION_PRINT, _CanPrint);
				PopupMenu.ShowPopup(BarManager, PointToScreen(new Point(X, Y)));
			}
			catch (Exception ex)
			{
				throw new ExceptionFramework(@"PopupShow exception.", ex, ExceptionKind.ekDeveloper);
			}
		}
		private void PrintData_BeginPrint(object aSender, PrintEventArgs aEventArgs)
		{
			try
			{
				aEventArgs.Cancel = _PrintData.InitPrint(Width, Height);
			}
			catch (Exception ex)
			{
				aEventArgs.Cancel = true;
				throw new ExceptionPagePrint("Exception in OlapScatter.PrintData_BeginPrint", ex);
			}
		}
		/// <summary>
		/// Друк скеттера
		/// </summary>
		private void PrintData_PrintPage(object aSender, PrintPageEventArgs aEventArgs)
		{
			//if (_TrialEnd)
			//    return;
			try
			{
				Graphics graphics = aEventArgs.Graphics;
				Rectangle pageRectangle = new Rectangle(aEventArgs.MarginBounds.Left - (int)aEventArgs.PageSettings.HardMarginX, aEventArgs.MarginBounds.Top - (int)aEventArgs.PageSettings.HardMarginY, aEventArgs.MarginBounds.Width, aEventArgs.MarginBounds.Height),
					folioRectangle = new Rectangle(pageRectangle.Left, pageRectangle.Bottom, pageRectangle.Width, _PrintData.FolioFont.Height);
				Region clip = graphics.Clip;
				Matrix transform = graphics.Transform;
				graphics.Clip = new Region(pageRectangle);
				Rectangle area = new Rectangle(0, 0, _PrintData.HeaderWidth, _PrintData.HeaderHeight);
				if (area.IntersectsWith(_PrintData.PageRectangle))
				{
					area.Intersect(_PrintData.PageRectangle);
					graphics.TranslateTransform(pageRectangle.Left - _PrintData.PageRectangle.Left, pageRectangle.Top - _PrintData.PageRectangle.Top);
					_PrintData.PrintHeader(graphics, area);
					graphics.Transform = transform;
				}
				area = new Rectangle(0, _PrintData.HeaderHeight, _PrintData.Size.Width, _PrintData.Size.Height - _PrintData.HeaderHeight - _PrintData.SignatureHeight);
				if (area.IntersectsWith(_PrintData.PageRectangle))
				{
					area = new Rectangle(0, 0, Width, Height);
					graphics.TranslateTransform(pageRectangle.Left - _PrintData.PageRectangle.Left, pageRectangle.Top + (_PrintData.HeaderHeight - _PrintData.PageRectangle.Top));
					graphics.ScaleTransform(_PrintData.Scale, _PrintData.Scale);
					graphics.GetType().GetField(@"backingImage", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(graphics, null);
					Redraw(graphics, area, true, true);
					HintsDraw(graphics, area);
					graphics.Transform = transform;
				}
				area = new Rectangle(0, _PrintData.Size.Height - (_PrintData.SignatureHeight - PrintData.DEFAULT_SIGNATURE_SPACING), _PrintData.SignatureWidth, _PrintData.SignatureHeight);
				if (area.IntersectsWith(_PrintData.PageRectangle))
				{
					area.Intersect(_PrintData.PageRectangle);
					graphics.TranslateTransform(pageRectangle.Left - _PrintData.PageRectangle.Left, pageRectangle.Top + (area.Top - _PrintData.PageRectangle.Top));
					area.Offset(0, -(_PrintData.Size.Height - (_PrintData.SignatureHeight - PrintData.DEFAULT_SIGNATURE_SPACING)));
					_PrintData.PrintSignature(graphics, area);
					graphics.Transform = transform;
				}
				graphics.Clip = clip;
				graphics.DrawString(string.Format(_PrintData.FolioText, _PrintData.Page.Y, _PrintData.Page.X), _PrintData.FolioFont, _PrintData.FolioBrush, folioRectangle, _PrintData.FolioFormat);
				aEventArgs.HasMorePages = _PrintData.InitPage();
			}
			catch (Exception ex)
			{
				aEventArgs.Cancel = true;
				throw new ExceptionPagePrint("Exception in OlapScatter.PrintData_PrintPage()", ex);
			}
		}
		/// <summary>
		/// Перемальовує в кеш скеттер діаграму та відображає її на контролі.
		/// </summary>
		/// <param name="aCoordRedraw">Якщо <c>true</c> то координатна сітка буде перерахована та перемальована, а іншому випадку буде відмальована з кешу.</param>
		private void Redraw(bool aCoordRedraw)
		{
			Tracer.EnterMethod("OlapScatter.Redraw(bool)");
			Rectangle rect = new Rectangle(0, 0, Width, Height);
			if (_bufferedPage == null || _bufferedPage.Size != rect.Size)
			{
				if (_bufferedPage != null)
					_bufferedPage.Dispose();
				using (Graphics graphics = CreateGraphics())
					_bufferedPage = new CachedBmp(rect.Width, rect.Height, graphics);
			}
			Redraw(_bufferedPage.BufferedGraphics, rect, aCoordRedraw, false);
			//Redraw(CreateGraphics(), rect, aCoordRedraw, false);
			Refresh();  //негайне оновлення контрола
			Tracer.ExitMethod("OlapScatter.Redraw(bool)");
		}
		/// <summary>
		/// Відмальовує за допомогою об'єкту <paramref name="aGraphics"/> скеттер діаграму.
		/// </summary>
		/// <param name="aGraphics">Graphics використовуючи методи якого буде перемальована скеттер діаграма</param>
		/// <param name="aArea">Місце куди потрібно намалювати скеттер діаграму</param>
		/// <param name="aCoordRedraw">Якщо <c>true</c> то координатна сітка буде перерахована та перемальована, а іншому випадку буде відмальована з кешу.</param>
		/// <param name="aPrinting">Визначає чи відмальовка відбувається для друку</param>
		private void Redraw(Graphics aGraphics, Rectangle aArea, bool aCoordRedraw, bool aPrinting)
		{
			//Tracer.EnterMethod("OlapScatter.Redraw(Graphics, Rectangle, bool, bool)");
			//Tracer.Write(TraceLevel.Info, "OlapScatter.Redraw(Graphics, Rectangle, bool, bool)", string.Format("Parameters: aArea={0}, aCoordRedraw={1}, aPrinting={2}", aArea.ToString(), aCoordRedraw.ToString(), aPrinting.ToString()));
			//var itemsArea = DrawArea;//new Rectangle(YSLICE_DELTA + 1, 1, aArea.Width - YSLICE_DELTA - 2, aArea.Height - XSLICE_DELTA - 1);	//обмежуючий елементи прямокутник
			//GraphicsContainer cont = null;
			//aGraphics.HighQualitySet();
			//if (aPrinting)  //якщо відмальовка відбувається для друку
			//	cont = aGraphics.BeginContainer();  //починаємо новий контейнер (для того щоб якщо нові трансформації будуть робитися з aGraphics, то щоб вони додавалися до попередніх(що були до початку контейнера) а не перетирали їх)
			//										//else	//якщо ми малюємо по бітмапу де тільки є скеттер діаграма
			//										//aGraphics.Clear(SkinBackColorGet());	//очищаємо бітмап кольором фону
			//										//замальовуємо та обводимо контуром прямокутник в якому будуть знаходитись елементи
			//										//aGraphics.FillRectangle(BRUSH_BACK, itemsArea);
			//aGraphics.DrawRectangle(PEN_ITEM_AREA_BORDER, new Rectangle(itemsArea.Left - 1, itemsArea.Top - 1, itemsArea.Width + 1, itemsArea.Height + 1));
			Tracer.EnterMethod("OlapScatter.Redraw(Graphics, Rectangle, bool, bool)");
			Tracer.Write(TraceLevel.Info, "OlapScatter.Redraw(Graphics, Rectangle, bool, bool)", string.Format("Parameters: aArea={0}, aCoordRedraw={1}, aPrinting={2}", aArea.ToString(), aCoordRedraw.ToString(), aPrinting.ToString()));
			_itemsArea = new Rectangle(YSLICE_DELTA + 1, 1, aArea.Width - YSLICE_DELTA - 2, aArea.Height - XSLICE_DELTA - 1);   //обмежуючий елементи прямокутник
			GraphicsContainer cont = null;
			aGraphics.HighQualitySet();
			if (aPrinting)  //якщо відмальовка відбувається для друку
				cont = aGraphics.BeginContainer();  //починаємо новий контейнер (для того щоб якщо нові трансформації будуть робитися з aGraphics, то щоб вони додавалися до попередніх(що були до початку контейнера) а не перетирали їх)
			else    //якщо ми малюємо по бітмапу де тільки є скеттер діаграма
				aGraphics.Clear(SkinBackColorGet());    //очищаємо бітмап кольором фону
														//замальовуємо та обводимо контуром прямокутник в якому будуть знаходитись елементи
			aGraphics.FillRectangle(BRUSH_BACK, _itemsArea);
			aGraphics.DrawRectangle(PEN_ITEM_AREA_BORDER, new Rectangle(_itemsArea.Left - 1, _itemsArea.Top - 1, _itemsArea.Width + 1, _itemsArea.Height + 1));
			if (_Data != null)  //якщо дані присутні, то відмальовуємо їх
				Redraw(aGraphics, aArea, _Data, aCoordRedraw, aPrinting);
			else    //якщо даних немає, то пишемо текст що вони відсутні
			{
				if (CanDragAndDrop)
				{
					const int delta = 12;
					var rectangle = new Rectangle(delta, delta, Math.Max(aArea.Width - delta, delta), Math.Max(aArea.Height - delta, delta));
					aGraphics.DrawString(StringGet(OlapScatter_NotAllDragged), FONT_LABEL_TEXT, BRUSH_LABEL_FORE_DEFAULT, rectangle, STR_FORMAT_LABEL);
				}
			}
			if (cont != null)
				aGraphics.EndContainer(cont);   //завершуємо контейнер
			Tracer.ExitMethod("OlapScatter.Redraw(Graphics, Rectangle, bool, bool)");
		}
		/// <summary>
		/// Відмальовує за допомогою об'єкту <paramref name="aGraphics"/> скеттер діаграму.
		/// </summary>
		/// <param name="aGraphics">Graphics використовуючи методи якого буде перемальована скеттер діаграма</param>
		/// <param name="aArea">Місце куди потрібно намалювати скеттер діаграму</param>
		/// <param name="aData">Дані, які потрібно відмалювати</param>
		/// <param name="aCoordRedraw">Якщо <c>true</c> то координатна сітка буде перерахована та перемальована, а іншому випадку буде відмальована з кешу.</param>
		/// <param name="aPrinting">Визначає чи відмальовка відбувається для друку</param>
		private void Redraw(Graphics aGraphics, Rectangle aArea, OlapScatterFormsData aData, bool aCoordRedraw, bool aPrinting)
		{
			if ((_Data.Min.MX.ValueType & MVT.mvtNumber) != MVT.mvtNotSet
				&& (_Data.Min.MY.ValueType & MVT.mvtNumber) != MVT.mvtNotSet
				)   //якщо на обох осях є хоча б по одному елементу що має числове значення для відповідних мір
			{
				if (aCoordRedraw)   //якщо потрібно перемалювати координатну сітку
				{
					DataPrecalculate(ref aData, aArea.Width, aArea.Height);
					//coordinates redraw
					if (_cachedCoord == null || _cachedCoord.Size != aArea.Size)    //якщо CachedBmp не було створено для кешування відмальовки сітки або його розміри нам не підходять
					{
						if (_cachedCoord != null)
							_cachedCoord.Dispose();
						_cachedCoord = new CachedBmp(aArea.Width, aArea.Height, aGraphics);
					}
					Graphics g = aPrinting ? aGraphics : _cachedCoord.BufferedGraphics; //якщо відмальовка для друку то результати не кешуємо
					if (!aPrinting)
						g.Clear(SkinBackColorGet());
					g.FillRectangle(BRUSH_BACK, _itemsArea);
					CoordinatesDraw(g, aArea.Width, aArea.Height);
				}
				//draw items
				aGraphics.HighQualitySet();
				if (!aPrinting)
					_cachedCoord.Draw(aGraphics);
				if (_CurrentPage >= 0)
				{
					if (_Data.PagesPresent)
						aGraphics.DrawString(_Data.PagesMemberItems[_Data.CurrentPage].Caption, FONT_BACK, BRUSH_STR_BACK, _itemsArea, STR_FORMAT_BACK);
					Region oldClip = aGraphics.Clip;
					aGraphics.Clip = new Region(_itemsArea);
					ItemsDraw(aGraphics);
					aGraphics.Clip = oldClip;
				}
			}
			else  //якщо на осі абсцис або ординат немає жодного елементу з числовим значенням
				aGraphics.DrawString(StringGet(OlapScatter_BadResults), FONT_LABEL_TEXT, BRUSH_LABEL_FORE_SPECIAL, aArea, STR_FORMAT_LABEL);
		}
		//{
		//	if ((_Data.Min.MX.ValueType & MVT.mvtNumber) != MVT.mvtNotSet
		//		&& (_Data.Min.MY.ValueType & MVT.mvtNumber) != MVT.mvtNotSet
		//		)   //якщо на обох осях є хоча б по одному елементу що має числове значення для відповідних мір
		//	{
		//		var itemsArea = DrawArea;
		//		if (aCoordRedraw)   //якщо потрібно перемалювати координатну сітку
		//		{
		//			DataPrecalculate(ref aData, aArea.Width, aArea.Height);
		//			//coordinates redraw
		//			if (_cachedCoord == null || _cachedCoord.Size != aArea.Size)    //якщо CachedBmp не було створено для кешування відмальовки сітки або його розміри нам не підходять
		//			{
		//				if (_cachedCoord != null)
		//					_cachedCoord.Dispose();
		//				_cachedCoord = new CachedBmp(aArea.Width, aArea.Height, aGraphics);
		//			}
		//			Graphics g = aPrinting ? aGraphics : _cachedCoord.BufferedGraphics; //якщо відмальовка для друку то результати не кешуємо
		//																				//if (!aPrinting)
		//																				//	g.Clear(SkinBackColorGet());
		//																				//g.FillRectangle(BRUSH_BACK, itemsArea);
		//			CoordinatesDraw(g, aArea.Width, aArea.Height);
		//		}
		//		//draw items
		//		aGraphics.HighQualitySet();
		//		if (!aPrinting)
		//			_cachedCoord.Draw(aGraphics);
		//		if (_CurrentPage >= 0)
		//		{
		//			if (_Data.PagesPresent)
		//				aGraphics.DrawString(_Data.PagesMemberItems[_Data.CurrentPage].Caption, FONT_BACK, BRUSH_STR_BACK, itemsArea, STR_FORMAT_BACK);
		//			Region oldClip = aGraphics.Clip;
		//			aGraphics.Clip = new Region(itemsArea);
		//			ItemsDraw(aGraphics);
		//			aGraphics.Clip = oldClip;
		//		}
		//	}
		//	else  //якщо на осі абсцис або ординат немає жодного елементу з числовим значенням
		//		aGraphics.DrawString(StringGet(OlapScatter_BadResults), FONT_LABEL_TEXT, BRUSH_LABEL_FORE_SPECIAL, aArea, STR_FORMAT_LABEL);
		//}
		/// <summary>
		/// Відмальовує на скеттер діаграмі один виділений елемент разом з його шляхом і якщо потрібно лінією до хінта.
		/// </summary>
		/// <param name="aGraphics">Graphics використовуючи методи якого буде відбуватись відмальовка</param>
		/// <param name="aItemID">Елемент який потрібно відмалювати</param>
		/// <param name="aLastX">Абсциса центру елементу на поточній сторінці</param>
		/// <param name="aLastY">Ордината центру елементу на поточній сторінці</param>
		/// <param name="aLastSize">Розмір елементу на поточній сторінці</param>
		private void SelectedItemDraw(Graphics aGraphics, int aItemID, float aLastX, float aLastY, float aLastSize)
		{
			if (_Data.ItemSelectionStart[aItemID] == -1)
			{
				AdvancedHint hint = _Data.ItemHintGet(aItemID);
				if (hint != null)
					HintHide(hint);
				return;
			}
			int i;
			float diagPoint, radius;
			OSDI item;
			float x1, x2, y1, y2, s;
			x1 = aLastX;
			y1 = aLastY;
			s = aLastSize;
			if (_Data.ShowHints)    //якщо хінт над елементом є
			{
				//знаходимо елемент до якого прив'язаний "хінт"
				CI hintItem = new CI(_Data.CurrentPage, aItemID);   //вважаємо що хінт прив'язаний до елемента на поточній сторінці
				if (_Data.ShowTrails)   //якщо елемент має шлях
				{
					//хінт прив'язаний до першого елементу шляху
					hintItem.PageID = _Data.ItemSelectionStart[aItemID];
					item = _Data.Pages[hintItem.PageID][aItemID];
					x1 = item.X;
					y1 = item.Y;
					s = item.Size;
				}
				AdvancedHint hint = _Data.ItemHintGet(aItemID);
				if (_Data.Pages[_Data.CurrentPage][aItemID].CanShow || _Data.ItemSelectionStart[aItemID] < _Data.CurrentPage)   //якщо елемент видимий на даній сторінці або елемент має шлях
				{
					if (!_itemHintRelocating && !_Data.ItemHintGet(aItemID).IsDragging)
					{
						//change hint location
						_itemHintRelocating = true;
						hint.Visible = true;
						hint.Caption = HintConstruct(hintItem, true, true);
						hint.CanDrag = !_isPlaying;
						ItemHintPositionSet(hint, x1, y1, s);
						_itemHintRelocating = false;
						hint.Update();  //immediatly repaint
					}
					//draw line from hint to circle
					if (!ReferenceEquals(_draggedHint, _Data.ItemHintGet(aItemID)))
						LineItemHintToItemDraw(aGraphics, _Data.ItemHintGet(aItemID), x1, y1);
				}
				else    //якщо елементу до якого прив'язаний хінт на даній сторінці немає
					HintHide(hint);
			}
			if (_Data.ShowTrails)   //якщо потрібно відмалювати шлях
			{
				float x, y, d, r;
				//малюємо лінії зі стрілочками між елементами
				using (Pen pen = new Pen(Color.Transparent, WAY_LINE_WIDTH))
					for (i = _Data.ItemSelectionStart[aItemID]; i < _CurrentPage; ++i)
					{
						if (i + 1 < _Data.PagesCount && !_Data.Pages[i + 1][aItemID].CanShow) //якщо наступний елемент шляху невидимий
							break;
						//заповнюємо в (x1; y1) координати центру поточного кружка, а в (x2; y2) - наступного
						x1 = _Data.Pages[i][aItemID].X;
						y1 = _Data.Pages[i][aItemID].Y;
						if (i + 1 < _CurrentPage)
						{
							x2 = _Data.Pages[i + 1][aItemID].X;
							y2 = _Data.Pages[i + 1][aItemID].Y;
							s = _Data.Pages[i + 1][aItemID].Size;
						}
						else
						{
							x2 = aLastX;
							y2 = aLastY;
							s = aLastSize;
						}
						d = (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)); //відстань між центрами
						if (d > 0) //якщо центри не співпадають
						{
							//вираховуємо точку на другому кружку куди потрібно показати стрілкою
							r = s / 2;
							y = r * (y2 - y1) / d;
							x = r * (x2 - x1) / d;
							x2 -= x;
							y2 -= y;
							pen.Color = ItemHintHighlightColorGet(_Data.Pages[i][aItemID]);
							if (Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)) < WAY_LINE_WIDTH)
								pen.EndCap = LineCap.Flat;
							else if (!SystemUtils.IsUnderWine)
								pen.CustomEndCap = new AdjustableArrowCap(WAY_LINE_WIDTH, WAY_LINE_WIDTH, true);
							else
								pen.EndCap = LineCap.Flat;
							aGraphics.DrawLine(pen, x1, y1, x2, y2);
						}
					}
				//відмальовуємо всі "кружки" крім останнього
				for (i = _Data.ItemSelectionStart[aItemID]; i < _CurrentPage; ++i)
				{
					item = _Data.Pages[i][aItemID];
					if (!item.CanShow)
						break;
					radius = item.Size / 2;
					using (Brush brush = new SolidBrush(item.Color))
						aGraphics.FillEllipse(brush, item.X - radius, item.Y - radius, item.Size, item.Size);
					if (item.IsInterpolated)
					{
						diagPoint = (float)Math.Sqrt(1f / 2) * radius;
						aGraphics.DrawLine(PEN_INTERPOLATED_LINE, item.X - diagPoint, item.Y + diagPoint, item.X + diagPoint, item.Y - diagPoint);
					}
					aGraphics.DrawEllipse(PEN_ITEM_LINE, item.X - radius, item.Y - radius, item.Size, item.Size);
				}
			}
		}
		//{
		//	if (_Data.ItemSelectionStart[aItemID] == -1)
		//	{
		//		AdvancedHint hint = _Data.ItemHintGet(aItemID);
		//		//var itemTets = _Data.Pages[_Data.CurrentPage][aItemID];
		//		//var marker = _markerDataItem[itemTets];
		//		if (hint != null)
		//			HintHide(hint);
		//		return;
		//	}
		//	int i;
		//	float diagPoint, radius;
		//	OSDI item;
		//	float x1, x2, y1, y2, s;
		//	x1 = aLastX;
		//	y1 = aLastY;
		//	s = aLastSize;
		//	if (_Data.ShowHints)    //якщо хінт над елементом є
		//	{
		//		//знаходимо елемент до якого прив'язаний "хінт"
		//		CI hintItem = new CI(_Data.CurrentPage, aItemID);   //вважаємо що хінт прив'язаний до елемента на поточній сторінці
		//															//хінт прив'язаний до першого елементу шляху
		//		hintItem.PageID = _Data.ItemSelectionStart[aItemID];
		//		var item = _Data.Pages[hintItem.PageID][aItemID];
		//		var marker = (OlapMapCircleMarker)item.Tag;
		//		//OlapMapCircleMarker marker;
		//		////if (!_markerDataItem.TryGetValue(item, out marker))
		//		//{
		//		//	marker = new OlapMapCircleMarker(new PointLatLng(item.Latitude, item.Longitude), () => ItemHintGet(aItemID), item);
		//		//	marker.PageID = _Data.ItemSelectionStart[aItemID];
		//		//	marker.ItemID = aItemID ;
		//		//	marker.Position = FromLocalToLatLng((int)item.X, (int)item.Y);
		//		//	_markersLayer.Markers.Add(marker);
		//		//	//_markerDataItem[item] = marker;
		//		//}
		//		if (_Data.ShowTrails)   //якщо елемент має шлях
		//		{
		//			////хінт прив'язаний до першого елементу шляху
		//			//hintItem.PageID = _Data.ItemSelectionStart[aItemID];
		//			//item = _Data.Pages[hintItem.PageID][aItemID];
		//			x1 = item.X;
		//			y1 = item.Y;
		//			s = item.Size;
		//		}
		//		var tooltip = marker.OlapToolTip;
		//		AdvancedHint hint = _Data.ItemHintGet(aItemID);
		//		if (_Data.Pages[_Data.CurrentPage][aItemID].CanShow || _Data.ItemSelectionStart[aItemID] < _Data.CurrentPage)   //якщо елемент видимий на даній сторінці або елемент має шлях
		//		{
		//			if (!_itemHintRelocating && !_Data.ItemHintGet(aItemID).IsDragging)
		//			{
		//				//change hint location
		//				_itemHintRelocating = true;
		//				//TODO zatu4ka null reference !
		//				//if(tooltip == null)
		//				//ItemHint_Create(aItemID, false);
		//				tooltip.CanDrag = !_isPlaying;
		//				marker.ToolTipText = HintConstruct(hintItem, true, true);
		//				tooltip.Show(false);


		//				//hint.Visible = true;
		//				//hint.Caption = HintConstruct(hintItem, true, true);
		//				//hint.CanDrag = !_isPlaying;
		//				ItemHintPositionSet(hint, x1, y1, s);
		//				_itemHintRelocating = false;
		//				//hint.Update();	//immediatly repaint
		//				tooltip.OnRender(aGraphics);
		//			}
		//			//draw line from hint to circle
		//			if (!ReferenceEquals(_draggedHint, _Data.ItemHintGet(aItemID)))
		//				LineItemHintToItemDraw(aGraphics, _Data.ItemHintGet(aItemID), x1, y1);
		//		}
		//		else    //якщо елементу до якого прив'язаний хінт на даній сторінці немає
		//			HintHide(aItemID, _Data.CurrentPage);
		//		//HintHide(hint);
		//	}
		//	if (_Data.ShowTrails)   //якщо потрібно відмалювати шлях
		//	{
		//		//малюємо лінії зі стрілочками між елементами
		//		int i;
		//		using (Pen pen = new Pen(Color.Transparent, WAY_LINE_WIDTH))
		//			for (i = _Data.ItemSelectionStart[aItemID]; i < _CurrentPage; ++i)
		//			{
		//				if (i + 1 < _Data.PagesCount && !_Data.Pages[i + 1][aItemID].CanShow) //якщо наступний елемент шляху невидимий
		//					break;
		//				//заповнюємо в (x1; y1) координати центру поточного кружка, а в (x2; y2) - наступного
		//				x1 = _Data.Pages[i][aItemID].X;
		//				y1 = _Data.Pages[i][aItemID].Y;
		//				float x2;
		//				float y2;
		//				if (i + 1 < _CurrentPage)
		//				{
		//					x2 = _Data.Pages[i + 1][aItemID].X;
		//					y2 = _Data.Pages[i + 1][aItemID].Y;
		//					s = _Data.Pages[i + 1][aItemID].Size;
		//				}
		//				else
		//				{
		//					x2 = aLastX;
		//					y2 = aLastY;
		//					s = aLastSize;
		//				}
		//				var d = (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
		//				if (d > 0) //якщо центри не співпадають
		//				{
		//					//вираховуємо точку на другому кружку куди потрібно показати стрілкою
		//					var r = s / 2;
		//					var y = r * (y2 - y1) / d;
		//					var x = r * (x2 - x1) / d;
		//					x2 -= x;
		//					y2 -= y;
		//					pen.Color = ItemHintHighlightColorGet(_Data.Pages[i][aItemID]);
		//					if (Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)) < WAY_LINE_WIDTH)
		//						pen.EndCap = LineCap.Flat;
		//					else if (!SystemUtils.IsUnderWine)
		//						pen.CustomEndCap = new AdjustableArrowCap(WAY_LINE_WIDTH, WAY_LINE_WIDTH, true);
		//					else
		//						pen.EndCap = LineCap.Flat;
		//					aGraphics.DrawLine(pen, x1, y1, x2, y2);
		//				}
		//			}
		//		//відмальовуємо всі "кружки" крім останнього
		//		for (i = _Data.ItemSelectionStart[aItemID]; i < _CurrentPage; ++i)
		//		{
		//			var item = _Data.Pages[i][aItemID];
		//			if (!item.CanShow)
		//				break;

		//			var marker = new OlapMapCircleMarker(new PointLatLng(item.Latitude, item.Longitude), () => ItemHintGet(aItemID), item);
		//			item.Tag = marker;

		//			marker.PageID = i;
		//			marker.ItemID = aItemID;
		//			marker.IsVisible = item.CanShow;
		//			marker.Position = FromLocalToLatLng((int)item.X, (int)item.Y);
		//			_markersLayer.Markers.Add(marker);
		//			//marker.OnRender(aGraphics);

		//			//radius = item.Size / 2;
		//			//using (Brush brush = new SolidBrush(item.Color))
		//			//	aGraphics.FillEllipse(brush, item.X - radius, item.Y - radius, item.Size, item.Size);
		//			//if (item.IsInterpolated)
		//			//{
		//			//	diagPoint = (float)Math.Sqrt(1f / 2) * radius;
		//			//	aGraphics.DrawLine(PEN_INTERPOLATED_LINE, item.X - diagPoint, item.Y + diagPoint, item.X + diagPoint, item.Y - diagPoint);
		//			//}
		//			//aGraphics.DrawEllipse(PEN_ITEM_LINE, item.X - radius, item.Y - radius, item.Size, item.Size);
		//		}
		//	}
		//}
		private void Timer_Tick(object aSender, EventArgs aEventArgs)
		{
			if (_CurrentPage + _pageStep > _Data.PagesCount - 1)
			{
				Stop();
				PositionPaint(_Data.PagesCount - 1);
			}
			else
				PositionPaint(_CurrentPage + _pageStep);
		}
		private void TimerGroupHighlight_Tick(object aSender, EventArgs aEventArgs)
		{
			_groupHighlightShow ^= true;
			Redraw(false);
		}
		private void TimerItemHighlight_Tick(object aSender, EventArgs aEventArgs)
		{
			CI nextItem = _highlightedItem;
			if (_highlightedItem == CI.Default)
				nextItem = new CI(_Data.ItemSelectionStart[_CurrentItem.ItemID], _CurrentItem.ItemID);
			else
				nextItem.PageID = _highlightedItem.PageID == _Data.CurrentPage
									? _Data.ItemSelectionStart[_highlightedItem.ItemID]
									: _highlightedItem.PageID + 1;
			if (nextItem.PageID == -1 || nextItem.ItemID == -1)
			{
				_timerItemHighlight.Stop();
				_highlightedItem = CI.Default;
			}
			else
				_highlightedItem = nextItem;
			Invalidate();
		}
		private void Init()
		{
			Location = DrawArea.Location;
			Size = DrawArea.Size;
			Manager.Mode = AccessMode.ServerAndCache;
			// config map         
			MapProvider = GMapProviders.GoogleMap;
			Position = new PointLatLng(54.6961334816182, 25.2985095977783);

			Bearing = 0F;
			CanDragMap = true;

			GrayScaleMode = false;
			HelperLineOption = HelperLineOptions.DontShow;

			MarkersEnabled = true;

			MouseWheelZoomEnabled = true;
			MouseWheelZoomType = MouseWheelZoomType.ViewCenter;
			NegativeMode = false;
			//PolygonsEnabled = true;
			RetryLoadTile = 2;
			//RoutesEnabled = true;
			ScaleMode = ScaleModes.Integer;
			SelectedAreaFillColor = Color.FromArgb(33, 65, 105, 225);
			ShowTileGridLines = false;
			ShowCenter = false;
			MapScaleInfoEnabled = true;

			MaxZoom = 20;
			Zoom = 9;
			SetMinZoom();
		}
		private void SetMinZoom()
		{
			var area = DrawArea;
			double mapWidth = Core.vWidth;
			double mapHeight = Core.vHeight;

			var mapArea = Core.sizeOfMapArea;
			var heightScale = mapArea.Width;//(int)Math.Ceiling(Math.Max(area.Height/mapHeight, 1));
			var widthScale = mapArea.Height;//(int)Math.Ceiling(Math.Max(area.Width/mapWidth, 1));
			var scale = (int)Math.Max(heightScale, widthScale);
			var zoom = Zoom;
			MinZoom = scale;
			Zoom = zoom;
		}

		private AdvancedBarButtonItem BBINoActions
		{
			get
			{
				if (_BBINoActions == null)
				{
					_BBINoActions = BarManagerHolder.AdvancedBarButtonItemCreate();
					_BBINoActions.Name = "NoActions";
					_BBINoActions.Enabled = false;
					_BBINoActions.ResourceID = Common_PopUpMenu_NoActions;
				}
				return _BBINoActions;
			}
		}
		private AdvancedBarSubItem BSIActions
		{
			get
			{
				if (_BSIActions == null)
				{
					_BSIActions = BarManagerHolder.AdvancedBarSubItemCreate();
					_BSIActions.ResourceID = Common_PopUpMenu_Actions;
					_BSIActions.CloseUp += BSIActions_CloseUp;
					_BSIActions.Popup += BSIActions_Popup;
				}
				return _BSIActions;
			}
		}
		/// <summary>
		/// При першому звертанні створює підменю Drill by контекстного меню.
		/// </summary>
		private AdvancedBarSubItem BSIDrillBy
		{
			get
			{
				if (_BSIDrillBy == null)
				{
					_BSIDrillBy = BarManagerHolder.AdvancedBarSubItemCreate();
					_BSIDrillBy.ResourceID = OlapScatter_PopUpMenu_DrillBy;
				}
				return _BSIDrillBy;
			}
		}
		/// <summary>
		/// При першому звертанні створює підменю Drill by on New Page контекстного меню.
		/// </summary>
		private AdvancedBarSubItem BSIDrillByOnNewPage
		{
			get
			{
				if (_BSIDrillByOnNewPage == null)
				{
					_BSIDrillByOnNewPage = BarManagerHolder.AdvancedBarSubItemCreate();
					_BSIDrillByOnNewPage.ResourceID = Common_PopUpMenu_DrillByOnNewPage;
				}
				return _BSIDrillByOnNewPage;
			}
		}
		private AdvancedBarSubItem BSIDrillDown
		{
			get
			{
				if (_BSIDrillDown == null)
				{
					_BSIDrillDown = BarManagerHolder.AdvancedBarSubItemCreate();
					_BSIDrillDown.ResourceID = OlapScatter_PopUpMenu_DrillDown;
				}
				return _BSIDrillDown;
			}
		}
		private AdvancedBarSubItem BSIDrillthrough
		{
			get
			{
				if (_BSIDrillthrough == null)
				{
					_BSIDrillthrough = BarManagerHolder.AdvancedBarSubItemCreate();
					_BSIDrillthrough.ResourceID = OlapScatter_PopUpMenu_Drillthrough;
					_BSIDrillthrough.Glyph = _ActionImages[(int)ActionImage.aiDrillthrough];
				}
				return _BSIDrillthrough;
			}
		}
		private AdvancedBarSubItem BSIDrillUp
		{
			get
			{
				if (_BSIDrillUp == null)
				{
					_BSIDrillUp = BarManagerHolder.AdvancedBarSubItemCreate();
					_BSIDrillUp.ResourceID = OlapScatter_PopUpMenu_DrillUp;
				}
				return _BSIDrillUp;
			}
		}
		private AdvancedBarSubItem[] BSIHideShowItems
		{
			get
			{
				if (_BSIHideShowActions == null)
				{
					_BSIHideShowActions = new AdvancedBarSubItem[3];
					for (int i = 0; i < 3; ++i)
					{
						string resourceID = i == 0 ? OlapScatter_PopUpMenu_HideItem
													: i == 1
														? OlapScatter_PopUpMenu_HideSiblings
														: OlapScatter_PopUpMenu_ShowSiblings;
						_BSIHideShowActions[i] = BarManagerHolder.AdvancedBarSubItemCreate();
						_BSIHideShowActions[i].ResourceID = resourceID;
					}
				}
				return _BSIHideShowActions;
			}
		}
		/// <summary>
		/// Поточний елемент
		/// </summary>
		private CI CurrentItem
		{
			set
			{
				if (_Data == null)
					return;
				Tracer.EnterMethod("OlapScatter.set_CurrentItem");
				Tracer.Write(TraceLevel.Info, "OlapScatter.set_CurrentItem", string.Format("oldValue={0}, newValue={1}", _CurrentItem, value));
				if (_CurrentItem != value)
				{
					CI lastSelectedItem = _CurrentItem;
					if (_CurrentItem != CI.Default) //якщо був інший поточний елемент
					{
						//перемальовуємо ту частину контрола яку займав попередній поточний елемент разом з підсвіткою
						OSDI currItem = _Data.Pages[_CurrentItem.PageID][_CurrentItem.ItemID];
						float radius = currItem.Size / 2;
						_CurrentItem = CI.Default;
						Rectangle rect = new Rectangle((int)(currItem.X - radius - ITEM_HIGHLIGHT_WIDTH)
													   , (int)(currItem.Y - radius - ITEM_HIGHLIGHT_WIDTH)
													   , (int)(currItem.Size + 2 * ITEM_HIGHLIGHT_WIDTH + 2)
													   , (int)(currItem.Size + 2 * ITEM_HIGHLIGHT_WIDTH + 2));
						Invalidate(rect);
						Update();   //force all paint messages to this control
					}
					_CurrentItem = value;
					HintsHide(lastSelectedItem);    //ховаємо хінти з попереднього елементу
					if (value != CI.Default)    //якщо поточний елемент існує
					{
						OSDI item = _Data.Pages[value.PageID][value.ItemID];
						if (item.CanShow)   //якщо його можна показати
						{
							using (Graphics graphics = CreateGraphics().HighQualitySet())
							{
								ItemHighlight(graphics, item); //підсвічуємо поточний елемент
							}
							HintsShow(value);   //показуємо хінти для поточного елементу
						}
					}
				}
				_Data.CurrentItem = _CurrentItem.ItemID;
				Tracer.ExitMethod("OlapScatter.set_CurrentItem");
			}
		}
		/// <summary>
		/// Прямокутник за межі якого не повинен виїжджати жоден з хінтів над елементом.
		/// </summary>
		public Rectangle DrawArea
		{
			get { return new Rectangle(YSLICE_DELTA + 1, 1, Width - YSLICE_DELTA - 2, Height - XSLICE_DELTA - 1); }
		}
		private bool IsPlaying
		{
			set
			{
				_isPlaying = value;
				int op, i;
				//забороняємо/дозволяємо переміщення хінтів
				for (i = _Data.SelectedItemsStart; i < _Data.ItemsCount; ++i)
				{
					op = _Data.PaintOrder[i];
					if (_Data.ItemHintGet(op) != null)
						_Data.ItemHintGet(op).CanDrag = !_isPlaying;
				}
			}
		}

		protected override void Dispose(bool aDisposing)
		{
			Tracer.EnterMethod("OlapScatter.Dispose()");
			try
			{
				if (aDisposing)
				{
					//знищуємо всі таймери, звільняємо кеш
					_timerItemHighlight.Dispose();
					_timerGroupHighlight.Dispose();
					_timer.Dispose();
					if (_bufferedPage != null)
						_bufferedPage.Dispose();
					if (_cachedBeforeDrag != null)
						_cachedBeforeDrag.Dispose();
					if (_cachedCoord != null)
						_cachedCoord.Dispose();
					if (_PopupMenu != null)
						_PopupMenu.Dispose();
					if (_Actions != null)
						((IDisposable)_Actions).Dispose();
					_PrintData.Dispose();
				}
			}
			finally
			{
				base.Dispose(aDisposing);
			}
			Tracer.ExitMethod("OlapScatter.Dispose()");
		}
		protected override void LanguageUpdate()
		{
			base.LanguageUpdate();
			Redraw(false);
		}
		protected AdvancedBarButtonItem MenuItemGet(object aAction, string aResourceID, ActionImage aImage, bool aAddAction)
		{
			var result = BarManagerHolder.AdvancedBarButtonItemCreate();
			result.ResourceID = aResourceID;
			if (aImage != ActionImage.aiUnknown)
				result.Glyph = _ActionImages[(int)aImage];
			if (aAddAction)
				Actions.Add(new FormsFramework.Common.Actions.Action(aAction, new ActionItemBarItem(result)));
			return result;
		}
		protected override void OnBackColorChanged(EventArgs aEventArgs)
		{
			Redraw(true);
			base.OnBackColorChanged(aEventArgs);
		}
		protected override void OnMouseMove(MouseEventArgs aMouseEventArgs)
		{
			//base.OnMouseMove(aMouseEventArgs);
			if (IsInAction || PopupMenu.Visible)
				return;
			int x = aMouseEventArgs.X;
			int y = aMouseEventArgs.Y;
			if (!DrawArea.Contains(x, y))
				return;
			_mouseAt = MAC.macOlapScatter;
			CI curentItemIndex = CI.Default;
			if (_Data != null && (_Data.Min.MX.ValueType & MVT.mvtNumber) != MVT.mvtNotSet
				&& (_Data.Min.MY.ValueType & MVT.mvtNumber) != MVT.mvtNotSet)
			{
				double minDist = double.MaxValue;
				int currentPage = _Data.CurrentPage;
				//зі всіх елементів над якими мишка знаходимо той до центру якого найближче
				for (int i = 0; i < _Data.ItemsCount; ++i)
				{
					var op = _Data.PaintOrder[i];
					var sp = i >= _Data.SelectedItemsStart ? _Data.ItemSelectionStart[op] : currentPage;
					if (sp != -1)
					{
						for (int j = sp; j < currentPage + 1; ++j)
						{
							var item = _Data.Pages[j][op];
							if (item != null && _Data.Pages[sp][op].CanShow)
							{
								var dist = Math.Sqrt((item.X - x) * (item.X - x) + (item.Y - y) * (item.Y - y));
								if (dist < item.Size / 2 && dist < minDist)
								{
									minDist = dist;
									curentItemIndex = new CI(j, op);
								}
							}
						}
					}
				}
			}
			CurrentItem = curentItemIndex;
			base.OnMouseMove(aMouseEventArgs);
		}
		protected override void OnMouseLeave(EventArgs aEventArgs)
		{
			base.OnMouseLeave(aEventArgs);
			if (_mouseAt == MAC.macOlapScatter && !PopupMenu.Visible)
				CurrentItem = CI.Default;
		}
		protected override void OnMouseUp(MouseEventArgs aEventArgs)
		{
			//base.OnMouseUp(aEventArgs);
			if (_Data == null || (_Data.Min.MX.ValueType & MVT.mvtNumber) == MVT.mvtNotSet
				|| (_Data.Min.MY.ValueType & MVT.mvtNumber) == MVT.mvtNotSet)
				return;
			if (aEventArgs.Button == MouseButtons.Right && IsActual)
				PopupShow(aEventArgs.X, aEventArgs.Y);
			if (_CurrentItem == CI.Default)
				return;
			if (aEventArgs.Button == MouseButtons.Left)
			{
				if ((_Data.ItemSelectionGet(_CurrentItem.ItemID) & SIST.sistAsItem) != 0)   //element was selected as item
					_Data.ItemUnselect(_CurrentItem.ItemID);
				else
					_Data.ItemSelect(_CurrentItem.ItemID);
			}
		}
		protected override void OnPaint(PaintEventArgs aEventArgs)
		{
			Tracer.EnterMethod("OlapScatter.OnPaint()");
			if (_bufferedPage != null)
				_bufferedPage.Draw(aEventArgs.Graphics, aEventArgs.ClipRectangle);  //перемальовуємо скеттер з кешу
			if (!IsActual)  //якщо дані не актуальні
			{
				using (var brush = OlapFormsFrameworkDefs.NotActualBrushCreate())
					aEventArgs.Graphics.FillRectangle(brush, aEventArgs.ClipRectangle); //засірюємо
			}
			if (_CurrentItem != CI.Default) //якщо є поточний елемент
			{
				aEventArgs.Graphics.HighQualitySet();
				ItemHighlight(aEventArgs.Graphics, _Data.Pages[_CurrentItem.PageID][_CurrentItem.ItemID]);
			}
			if (_highlightedItem != CI.Default) //якщо є елемент шляхуякий потрібно підсвітити
			{
				aEventArgs.Graphics.HighQualitySet();
				ItemHighlight(aEventArgs.Graphics, _Data.Pages[_highlightedItem.PageID][_highlightedItem.ItemID]);
			}
			Tracer.ExitMethod("OlapScatter.OnPaint()");
		}
		//protected override void OnPaint(PaintEventArgs aEventArgs)
		//{
		//	Tracer.EnterMethod("OlapScatter.OnPaint()");
		//	base.OnPaint(aEventArgs);
		//	//if (_bufferedPage != null)
		//	//	_bufferedPage.Draw(aEventArgs.Graphics, aEventArgs.ClipRectangle);  //перемальовуємо скеттер з кешу
		//	if (!IsActual)  //якщо дані не актуальні
		//	{
		//		//using (var brush = OlapFormsFrameworkDefs.NotActualBrushCreate())
		//		//	aEventArgs.Graphics.FillRectangle(brush, aEventArgs.ClipRectangle); //засірюємо
		//	}
		//	if (_CurrentItem != CI.Default) //якщо є поточний елемент
		//	{
		//		aEventArgs.Graphics.HighQualitySet();
		//		ItemHighlight(aEventArgs.Graphics, _Data.Pages[_CurrentItem.PageID][_CurrentItem.ItemID]);
		//	}
		//	if (_highlightedItem != CI.Default) //якщо є елемент шляхуякий потрібно підсвітити
		//	{
		//		aEventArgs.Graphics.HighQualitySet();
		//		ItemHighlight(aEventArgs.Graphics, _Data.Pages[_highlightedItem.PageID][_highlightedItem.ItemID]);
		//	}
		//	Tracer.ExitMethod("OlapScatter.OnPaint()");
		//}
		protected override void OnPaintBackground(PaintEventArgs aEventArgs) { }
		protected override void OnSizeChanged(EventArgs aEventArgs)
		{
			base.OnSizeChanged(aEventArgs);
			//SetMinZoom();
			if (_Data != null)
			{
				Rectangle hintArea = DrawArea;
				AdvancedHint hint;
				//змінюємо дозволені для хінтів межі
				for (int i = 0; i < _Data.ItemsCount; ++i)
				{
					hint = _Data.ItemHintGet(i);
					if (hint != null)
						hint.HintArea = hintArea;
				}
			}
			_dataCalculated = false;
			Redraw(true);
		}

		public const int XSLICE_DELTA = 12;
		public const int YSLICE_DELTA = 11;

		static OlapScatter()
		{
			PEN_INTERPOLATED_LINE = new Pen(COLOR_INTERPOLATED_LINE);
			//встановлюємо відносні позиції для повідомлень користувачу
			STR_FORMAT_BACK = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
			STR_FORMAT_LABEL = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
			_ActionImages = AdvancedImageCollection.ImageCollectionGet(typeof(OlapScatter), new Size(16, 16)).Images;
		}
		public OlapScatter()
		{
			//встановлюємо стилі контрола для кращої відмальовки і швидкодії
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			//обчислюємо мінімальну відстань між підписами для осей в залежності від шрифта
			//CreateGraphics().MeasureString(SPACE, FONT_COORDINATES).Width * MIN_SPACES_BTWN_COORD_LABELS;
			//для друку
			_PrintData.BeginPrint += PrintData_BeginPrint;
			_PrintData.PrintPage += PrintData_PrintPage;
			//встановлюємо таймери
			_timer.Tick += Timer_Tick;
			_timer.Interval = 40; //25 times per second
			_timerGroupHighlight.Tick += TimerGroupHighlight_Tick;
			_timerGroupHighlight.Interval = GROUP_HIGHLIGHT_INTERVAL;
			_timerItemHighlight.Tick += TimerItemHighlight_Tick;
			_timerItemHighlight.Interval = ITEM_HIGHLIGHT_INTERVAL;
			MinimumSizeUpdate();
			//створюємо хінти для осей та поточного елементу
			_hints = new AdvancedHint[OSDI.MEASURES_COUNT + 1];
			_hints[OSDI.M_X_IND] = new AdvancedHint(this);
			_hints[OSDI.M_Y_IND] = new AdvancedHint(this);
			_hints[OSDI.M_X_IND].EventBoundsUpdate += OlapScatter_EventBoundsUpdate;
			_hints[OSDI.M_Y_IND].EventBoundsUpdate += OlapScatter_EventBoundsUpdate;
			_hints[OSDI.MEASURES_COUNT] = new AdvancedHint(this, false) { MaxTextSize = new Size(500, 500) };
			_Actions = new ActionCollection(this);
			AllowDrop = true;
			DragDrop += DragDropPerform;
			DragLeave += DragLeavePerform;
			DragOver += DragOverPerform;
			//Init();
			//MarkersEnabled = true;
			//Overlays.Add(_markersLayer);
		}
		protected override void EventOnMarkerEnterRaise(GMapMarker m)
		{
			base.EventOnMarkerEnterRaise(m);

			var olapMarker = (OlapMapCircleMarker)m;
			using (var graphics = CreateGraphics().HighQualitySet())
			{
				olapMarker.IsHighlighted = true;
				olapMarker.OnRender(graphics);
			}
		}
		protected override void EventOnMarkerLeaveRaise(GMapMarker m)
		{
			base.EventOnMarkerLeaveRaise(m);

			//var olapMarker = (OlapMapCircleMarker)m;
			//using (var graphics = CreateGraphics().HighQualitySet())
			//{
			//	olapMarker.IsHighlighted = false;
			//	olapMarker.OnRender(graphics);
			//}
		}

		public ScatterExportData ExportDataGet()
		{
			return new ScatterExportData(_CurrentPage
										 , _gradient == null ? null : (GradientKindClass)_gradient.Clone()
										 , _itemMaxSize
										 , _itemMinSize
										 , _IsActual
										 , (OlapScatterFormsData)(_Data == null ? null : _Data.Clone())
										 , _XLogarithmicScale
										 , _YLogarithmicScale
										 , _MeasuresFormatRules != null ? _MeasuresFormatRules.Clone() : null);
		}
		public void ExportDataSet(ScatterExportData aExportData)
		{
			_XLogarithmicScale = aExportData.XLogarithmicScale;
			_YLogarithmicScale = aExportData.YLogarithmicScale;
			_MeasuresFormatRules = aExportData.MeasuresFormatRules;
			VisualSettingsInit(aExportData.Gradient, aExportData.ItemMinSize, aExportData.ItemMaxSize, false);
			DataSet(aExportData.ScatterData);
			CurrentPage = aExportData.CurrentPage;
			_IsActual = aExportData.IsActual;
			Redraw(true);
			EventPositionChangedRaise();
		}
		public void ExportToImage(string aFileName, ImageFormat aImageFormat)
		{
			if (_bufferedPage == null)
			{
				_dataCalculated = false;
				Redraw(true);
			}
			Rectangle rect = new Rectangle(0, 0, Width, Height);
			using (Bitmap tempBitmap = new Bitmap(Width, Height))
			{
				using (Graphics graphics = Graphics.FromImage(tempBitmap))
				{
					if (_bufferedPage != null)
						_bufferedPage.Draw(graphics, rect);
					HintsDraw(graphics, rect);
				}
				tempBitmap.Save(aFileName, aImageFormat);
			}
		}
		public void Play()
		{
			if (_Data == null)
				return;
			CurrentItem = CI.Default;
			if (_CurrentPage == _Data.PagesCount - 1)
				PositionPaint(0);
			_timer.Start();
			IsPlaying = true;
		}
		/// <summary>
		/// Встановлює поточною сторінкою сторінку <paramref name="aPos"/> та відмальовує її.
		/// </summary>
		public void PositionPaint(float aPos)
		{
			Tracer.EnterMethod("OlapScatter.PositionPaint()");
			CurrentPage = aPos;
			Tracer.Write(TraceLevel.Info, "OlapScatter.PositionPaint()", string.Format("Paint position={0}", _CurrentPage));
			Redraw(false);
			EventPositionChangedRaise();
			Tracer.ExitMethod("OlapScatter.PositionPaint()");
		}
		public void Redraw()
		{
			_dataCalculated = false;
			Redraw(true);
		}
		public void Stop()
		{
			if (_Data != null)
			{
				_timer.Stop();
				IsPlaying = false;
			}
			EventPlayStoppedRaise();
		}
		/// <summary>
		/// Зупиняє режим "play" та встановлює почочною чторінкою сторінку <paramref name="aPosition"/>.
		/// </summary>
		public void StopAt(float aPosition)
		{
			if (_Data != null)
			{
				_timer.Stop();
				PositionPaint(aPosition);
				IsPlaying = false;
			}
			EventPlayStoppedRaise();
		}
		public void VisualSettingsInit(GradientKindClass aGradient, int aItemMinSize, int aItemMaxSize, bool aRedraw)
		{
			_gradient = aGradient;
			_itemMinSize = aItemMinSize;
			_itemMaxSize = aItemMaxSize;
			if (aRedraw && _Data != null)
			{
				_dataCalculated = false;
				DataPrecalculate(ref _Data, Width, Height);
				Redraw(false);
			}
		}

		public override bool CanDragAndDrop
		{
			get { return base.CanDragAndDrop; }
			set
			{
				base.CanDragAndDrop = value;
				AllowDrop = value;
			}
		}
		public bool CanDrillthrough
		{
			get { return _CanDrillthrough; }
			set { _CanDrillthrough = value; }
		}
		public bool CanExportNRP
		{
			get { return _canExportNRP; }
			set { _canExportNRP = value; }
		}
		public bool CanExportPNG
		{
			get { return _canExportPng; }
			set { _canExportPng = value; }
		}
		public bool CanFormatting
		{
			get { return _CanFormatting; }
			set { _CanFormatting = value; }
		}
		public bool CanHighlight
		{
			get { return _CanHighlight; }
			set { Actions.Visible(ACTION_HIGHLIGHT, _CanHighlight = value); }
		}
		public bool CanNavigate
		{
			get { return _CanNavigate; }
			set { _CanNavigate = value; }
		}
		public bool CanPrint
		{
			get { return _CanPrint; }
			set { _CanPrint = value; }
		}
		public bool CanOlapActions
		{
			get { return _CanOlapActions; }
			set { _CanOlapActions = value; }
		}
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public AdvancedHint ColorHint
		{
			get { return _hints[OSDI.M_COLOR_IND]; }
			set { _hints[OSDI.M_COLOR_IND] = value; }
		}
		[Browsable(false)]
		public float CurrentPage
		{
			get { return _CurrentPage; }
			private set
			{
				_CurrentPage = value;
				if (_Data != null)
					_Data.CurrentPage = (int)Math.Floor(_CurrentPage);
			}
		}
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public OlapScatterFormsData Data
		{
			get { return _Data; }
			set
			{
				DataSet(value);
				Redraw(true);
				EventPositionChangedRaise();
			}
		}
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsActual
		{
			get { return _IsActual; }
			set
			{
				if (!(_IsActual = value) && _isPlaying)
					Stop();
				Invalidate();
			}
		}
		public bool IsHighlightViewMode
		{
			get { return _IsHighlightViewMode; }
			set { _IsHighlightViewMode = value; }
		}
		[Browsable(false)]
		public bool IsInAction
		{
			get { return _isPlaying; }
		}
		public bool IsViewMode
		{
			get { return _IsViewMode; }
			set { Actions.Visible(ACTION_FORMAT, !(_IsViewMode = value) && _CanFormatting); }
		}
		public FormatRulesMeasures MeasuresFormatRules
		{
			get { return _MeasuresFormatRules; }
			set { _MeasuresFormatRules = value; }
		}
		public PopupMenu PopupMenu
		{
			get
			{
				if (_PopupMenu == null)
				{
					_PopupMenu = new PopupMenu(BarManager);
					_PopupMenu.AddItem(BSIDrillBy);
					if (Properties.CanDrillBy && !IsViewMode)
						CUtils.BSIDrillByCreate(BSIDrillBy, EventDrillByRequiredRaise(), PopupButton_DrillBy_Click, BarManagerHolder);
					_PopupMenu.AddItem(BSIDrillByOnNewPage);
					if (Properties.CanDrillByOnNewPage && !IsViewMode)
						CUtils.BSIDrillByCreate(BSIDrillByOnNewPage, EventDrillByRequiredRaise(), PopupButton_DrillByOnNewPage_Click, BarManagerHolder);
					_PopupMenu.AddItem(BSIDrillUp).BeginGroup = true;
					_PopupMenu.AddItem(BSIDrillDown);
					_PopupMenu.AddItem(MenuItemGet(ACTION_DRILL_UP, OlapScatter_PopUpMenu_DrillUp, ActionImage.aiUnknown, true)).BeginGroup = true;
					_PopupMenu.AddItem(MenuItemGet(ACTION_DRILL_DOWN, OlapScatter_PopUpMenu_DrillDown, ActionImage.aiUnknown, true));
					foreach (var item in BSIHideShowItems)
						_PopupMenu.AddItem(item);
					_PopupMenu.AddItem(MenuItemGet(ACTION_HIDE_ITEM, OlapScatter_PopUpMenu_HideItem, ActionImage.aiUnknown, true)).BeginGroup = true;
					_PopupMenu.AddItem(MenuItemGet(ACTION_HIDE_SIBLINGS, OlapScatter_PopUpMenu_HideSiblings, ActionImage.aiUnknown, true));
					_PopupMenu.AddItem(MenuItemGet(ACTION_SHOW_SIBLINGS, OlapScatter_PopUpMenu_ShowSiblings, ActionImage.aiUnknown, true));
					_PopupMenu.AddItem(BSIDrillthrough).BeginGroup = true;
					_PopupMenu.AddItem(BSIActions);
					BSIActions.AddItem(BBINoActions);
					_PopupMenu.AddItem(MenuItemGet(ACTION_HIGHLIGHT, !_IsHighlightViewMode && !_IsViewMode ? OlapScatter_PopUpMenu_Highlight : OlapScatter_PopUpMenu_HighlightView, ActionImage.aiHighlight, true)).BeginGroup = true;
					_PopupMenu.AddItem(MenuItemGet(ACTION_FORMAT, OlapScatter_PopUpMenu_Format, ActionImage.aiFormat, true));
					_PopupMenu.AddItem(MenuItemGet(ACTION_EXPORT_NRP, OlapScatter_PopUpMenu_ExportToNRP, ActionImage.aiSaveNRP, true)).BeginGroup = true;
					_PopupMenu.AddItem(MenuItemGet(ACTION_EXPORT_PNG, OlapScatter_PopUpMenu_ExportToPNG, ActionImage.aiExportPNG, true));
					_PopupMenu.AddItem(MenuItemGet(ACTION_PRINT, OlapScatter_PopUpMenu_Print, ActionImage.aiPrint, true)).BeginGroup = true;
				}
				return _PopupMenu;
			}
		}
		[Browsable(false)]
		public PrintData PrintData
		{
			get { return _PrintData; }
		}
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public AdvancedHint SizeHint
		{
			get { return _hints[OSDI.M_SIZE_IND]; }
			set { _hints[OSDI.M_SIZE_IND] = value; }
		}
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int Speed
		{
			set { _pageStep = _timer.Interval / (float)value; }
		}
		public bool XLogarithmicScale
		{
			get { return _XLogarithmicScale; }
			set
			{
				if (_XLogarithmicScale != value)
				{
					_XLogarithmicScale = value;
					if (_Data != null)
						_Data.XLogarithmicScale = value;
				}
			}
		}
		public bool YLogarithmicScale
		{
			get { return _YLogarithmicScale; }
			set
			{
				if (_YLogarithmicScale != value)
				{
					_YLogarithmicScale = value;
					if (_Data != null)
						_Data.YLogarithmicScale = value;
				}
			}
		}

		public event EventHandlerOlapScatterAction EventAction
		{
			add { Events.AddHandler(_EventAction, value); }
			remove { Events.RemoveHandler(_EventAction, value); }
		}
		public event EventHandlerActionsRequired EventActionsRequired
		{
			add { Events.AddHandler(_EventActionsRequired, value); }
			remove { Events.RemoveHandler(_EventActionsRequired, value); }
		}
		/// <summary>
		/// Повертає структуру, яку будує клас Designer, з доступними для операції Drill by вимірами, ієрархіями, рівнями.
		/// </summary>
		public event EventHandlerDrillByRequired EventDrillByRequired
		{
			add { Events.AddHandler(_EventDrillByRequired, value); }
			remove { Events.RemoveHandler(_EventDrillByRequired, value); }
		}
		public event EventHandlerLevelsRequired EventLevelsRequired
		{
			add { Events.AddHandler(_EventLevelsRequired, value); }
			remove { Events.RemoveHandler(_EventLevelsRequired, value); }
		}
		public event EventHandler EventPlayStopped
		{
			add { Events.AddHandler(_EventPlayStopped, value); }
			remove { Events.RemoveHandler(_EventPlayStopped, value); }
		}
		public event EventHandler EventPositionChanged
		{
			add { Events.AddHandler(_EventPositionChanged, value); }
			remove { Events.RemoveHandler(_EventPositionChanged, value); }
		}

		#region IActionListener Members
		public object ActionPerform(object aKey, object aState, int aPlace)
		{
			if (ReferenceEquals(ACTION_FORMAT, aKey))
				((EventHandlerOlapScatterAction)Events[_EventAction]).Invoke(null, aKey, string.Empty);
			else if (ReferenceEquals(ACTION_EXPORT_PNG, aKey) || ReferenceEquals(ACTION_PRINT, aKey) || ReferenceEquals(ACTION_EXPORT_NRP, aKey))
				((EventHandlerOlapScatterAction)Events[_EventAction]).Invoke(null, aKey, 0);
			else if (ReferenceEquals(ACTION_DRILL_DOWN, aKey) || ReferenceEquals(ACTION_DRILL_UP, aKey)
				|| ReferenceEquals(ACTION_HIDE_ITEM, aKey) || ReferenceEquals(ACTION_HIDE_SIBLINGS, aKey) || ReferenceEquals(ACTION_SHOW_SIBLINGS, aKey))
				((EventHandlerOlapScatterAction)Events[_EventAction]).Invoke(_Data.ItemsMemberItems[_CurrentItemForPopup.ItemID], aKey, 0);
			else if (Actions.IsAllowed(aKey) && Events[_EventAction] != null)
				((EventHandlerOlapScatterAction)Events[_EventAction]).Invoke(_Data.ItemsMemberItems[_CurrentItemForPopup.ItemID], aKey, aState);
			return null;
		}

		public ActionCollection Actions
		{
			get { return _Actions; }
		}
		public bool ReadOnly
		{
			get { return !Enabled; }
		}
		#endregion

		public AdvancedHint ItemHintGet(int aItemID)
		{
			var hint = _Data.ItemHintGet(aItemID);
			if (hint == null)
			{
				hint = _Data.ItemHintCreate(aItemID, this);
				hint.ShowPointer = false;
				hint.LocationChanged += ItemHint_EventLocationChanged;
				hint.MouseEnter += ItemHint_EventMouseEnter;
				hint.MouseLeave += ItemHint_EventMouseLeave;
				hint.EventDragEnd += ItemHint_EventDragEnd;
				hint.EventDragStart += ItemHint_EventDragStart;
				hint.EventClose += ItemHint_EventClose;
				hint.MouseUp += ItemHint_EventMouseUp;
			}
			hint.ID = aItemID;
			//hint.CanDrag = true;
			hint.CanShow = _Data.ShowHints;
			hint.HintArea = DrawArea;
			return hint;
		}
	}
}