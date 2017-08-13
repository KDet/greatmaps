using System;
using System.Collections.Generic;
using System.Drawing;
using FrameworkBase.Exceptions;
using FrameworkBase.Utils;
using OlapFramework.Data;
using OlapFramework.Olap.Metadata;
using SIST = OlapFormsFramework.Windows.Forms.Grid.Scatter.ScatterItemSelectionType;
using FormsFramework.Windows.Forms;
using OlapFormsFramework.Utils;
using System.Windows.Forms;

namespace OlapFormsFramework.Windows.Forms.Grid.Scatter
{
	[Flags]
	public enum ScatterItemSelectionType
	{
		//nayremchuk: values used to compare selections
		sistNone = 0x00,
		sistAsGroup = 0x01,
		sistAsItem = 0x02,
	}
	
	public class OlapScatterFormsData: OlapScatterData, ICloneable
	{
		private Pair<string, string> _ColorLevel = LEVEL_NONE;
		private Pair<string, string> _CurrentGroup = GROUP_NONE;
		private int _CurrentItem = -1;
		private int _CurrentPage = -1;
		private List<Pair<string, string>> _DetailsCaptions;
		private int _DetailsLevelsCount = 0;
		private List<string> _groupSelection;
		private string _GroupSelectionLevelID = string.Empty;
		private Point[] _HintsLocationShifts;
		private int _ItemsCount;
		private int[] _ItemSelectionStart;
		private AdvancedHint[] _ItemsHints;
		private List<OlapMeasureObjectBase> _Measures;
		private int _Opacity = 100;
		private int[] _PaintOrder;
		private int _PosGroupStart;
		private int _PosItemStart;
		private bool _RaiseEvents = true;
		private SIST[] _Selection;
		private bool _ShowHints = true;
		private bool _ShowTrails = true;
		private bool _XLogarithmicScale = false;
		private bool _YLogarithmicScale = false;
		
		private EventHandler _EventCurrentGroupChanged = null;
		private EventHandler _EventCurrentItemChanged = null;
		private EventHandler _EventCurrentPageChanged = null;
		private EventHandler _EventLogarithmicScalesSelectionChanged = null;
		private EventHandler _EventNonMDXConfigurationChanged = null;
		private EventHandler _EventOpacityChanged = null;
		private EventHandlerSelectionChanged _EventSelectionChanged = null;
		private EventHandler _EventShowHintsChanged = null;
		private EventHandler _EventShowTrailsChanged = null;

		private void EventCurrentGroupChangedRaise()
		{
			if (!_RaiseEvents)
				return;
			if (_EventCurrentGroupChanged != null)
				_EventCurrentGroupChanged(this, EventArgs.Empty);
		}
		private void EventCurrentItemChangedRaise()
		{
			if (!_RaiseEvents)
				return;
			if (_EventCurrentItemChanged != null)
				_EventCurrentItemChanged(this, EventArgs.Empty);
		}
		private void EventCurrentPageChangedRaise()
		{
			if (!_RaiseEvents)
				return;
			if (_EventCurrentPageChanged != null)
				_EventCurrentPageChanged(this, EventArgs.Empty);
		}
		private void EventLogarithmicScalesSelectionChangedRaise()
		{
			if (!_RaiseEvents)
				return;
			if (_EventLogarithmicScalesSelectionChanged != null)
				_EventLogarithmicScalesSelectionChanged(this, EventArgs.Empty);
		}
		private void EventNonMDXConfigurationChangedRaise()
		{
			if (!_RaiseEvents)
				return;
			if (_EventNonMDXConfigurationChanged != null)
				_EventNonMDXConfigurationChanged(this, EventArgs.Empty);
		}
		private void EventOpacityChangedRaise()
		{
			if (!_RaiseEvents)
				return;
			if (_EventOpacityChanged != null)
				_EventOpacityChanged(this, EventArgs.Empty);
		}
		private void EventSelectionChangedRaise(EventArgsSelectionChanged aEventArgs)
		{
			if (!_RaiseEvents)
				return;
			if (_EventSelectionChanged != null)
				_EventSelectionChanged(this, aEventArgs);
			EventNonMDXConfigurationChangedRaise();
		}
		private void EventShowHintsChangedRaise()
		{
			if (!_RaiseEvents)
				return;
			if (_EventShowHintsChanged != null)
				_EventShowHintsChanged(this, EventArgs.Empty);
		}
		private void EventShowTrailsChangedRaise()
		{
			if (!_RaiseEvents)
				return;
			if (_EventShowTrailsChanged != null)
				_EventShowTrailsChanged(this, EventArgs.Empty);
		}
		private int GroupLevelIndexGet(string aGroupLevelID)
		{
			return _DetailsCaptions.FindIndex(delegate(Pair<string, string> aPair) { return aPair.First == aGroupLevelID; });
		}
		private void Hint_EventLocationShiftChanged(object aSender, EventArgs aEventArgs)
		{
			var hint = aSender as AdvancedHint;
			if (hint != null && hint.Visible)
			{
				if (hint.ID < _HintsLocationShifts.Length)
					_HintsLocationShifts[hint.ID] = hint.LocationShift;
				EventNonMDXConfigurationChangedRaise();
			}
		}
		/// <summary>
		/// Визначає початок та довжину вибірки даного типу в масиві PaintOrder
		/// </summary>
		/// <param name="aItemSelectionType">Тип вибірки для якого потрібно знайти межі</param>
		/// <param name="aStartInd">Індекс елемента масиву PaintOrder з якого починається вибірка даного типу</param>
		/// <param name="aLength">Кількість елементів вибірки даного типу</param>
		/// <remarks>Тип вибірки може містити значення тільки однієї з констант цього перелічуваного типу. 
		/// Якщо в параметрі <c>aItemSelectionType</c> передається значення що є комбінованим з кількох констан перелічуваного типу, то вибирається та константа, в якої найбільше значення.</remarks>
		private void ItemPORangeGet(SIST aItemSelectionType, out int aStartInd, out int aLength)
		{
			if ((aItemSelectionType & SIST.sistAsItem) != 0)
			{
				aStartInd = _PosItemStart;
				aLength = _ItemsCount - _PosItemStart;
			} else if ((aItemSelectionType & SIST.sistAsGroup) != 0)
			{
				aStartInd = _PosGroupStart;
				aLength = _PosItemStart - _PosGroupStart;
			} else if (aItemSelectionType == SIST.sistNone)
			{
				aStartInd = 0;
				aLength = _PosGroupStart;
			} else
			{
				aStartInd = -1;
				aLength = -1;
			}
		}
		/// <summary>
		/// Повертає перший номер сторінки на якій елемент може бути відображений.
		/// </summary>
		/// <param name="aItemID">Номер елементу для якого потрібно знайти першу сторінку на якій він може бути відображений</param>
		/// <returns>Повертає найменший номер сторінки на якій елемент<paramref name="aItemID"/> може бути відображений.
		/// Якщо на жодній з сторінок елемент не може бути відображений то повертає -1.
		/// </returns>
		private int ItemVisibleStartGet(int aItemID)
		{
			for (int i = 0; i < PagesCount; ++i)
				if (Pages[i][aItemID].CanShow)
					return i;
			return -1;
		}
		/// <summary>
		/// Повертає перший номер сторінки на якій елемент може бути відображений, починаючи з індекса <param name="aStartIndex"/>.
		/// </summary>
		/// <param name="aItemID">Номер елементу для якого потрібно знайти першу сторінку на якій він може бути відображений</param>
		/// <returns>Повертає найменший номер сторінки на якій елемент<paramref name="aItemID"/> може бути відображений.
		/// Якщо на жодній з сторінок елемент не може бути відображений то повертає -1.
		/// </returns>
		private int ItemVisibleStartGet(int aItemID, int aStartIndex)
		{
			for (int i = aStartIndex; i < PagesCount; ++i)
				if (Pages[i][aItemID].CanShow)
					return i;
			return -1;
		}
		/// <summary>
		/// Для кожного елемента що вибраний як елемент оновлює інформацію про сторінку з якої потрібно малювати шлях
		/// </summary>
		private void ItemsSelectionStartUpdate()
		{
			if (_CurrentPage < 0)
				throw new ExceptionFramework("Impossible. Check code.", ExceptionKind.ekDeveloper);
			int i, op, iss;
			for (i = _PosItemStart; i < _ItemsCount; ++i)	//проходимось по всіх елементах що вибрані як елемент
			{
				op = _PaintOrder[i];	//порядковий номер елементу
				iss = _ItemSelectionStart[op];	//номер сторінки з якої починав малюватися шлях для даного елемента
				if (iss > _CurrentPage || iss == -1 || !_ShowTrails
					|| (iss != -1 && !Pages[iss][op].CanShow))
					_ItemSelectionStart[op] = _CurrentPage;
				if (_ItemSelectionStart[op] != -1 && !Pages[_ItemSelectionStart[op]][op].CanShow)
					_ItemSelectionStart[op] = ItemVisibleStartGet(op, _ItemSelectionStart[op]);
			}
		}
		/// <summary>
		/// Переміщає елемент в масиві PaintOrder з однієї групи в іншу
		/// </summary>
		/// <param name="aItemID">Номер елемента для який потрібно перемістити</param>
		/// <param name="aSelectionType">Тип вибірки в який потібно перемістити елемент</param>
		/// <returns>Повертає <c>true</c> якщо елемент перемістили з одного типу вибірки в інший і <c>false</c> в іншому випадку</returns>
		private bool Select(int aItemID, SIST aSelectionType)
		{
			if (_Selection[aItemID] == aSelectionType)
				return false;
			//search item position
			int searchSi, searchLen;
			ItemPORangeGet(_Selection[aItemID], out searchSi, out searchLen);
			int ind = Array.BinarySearch(_PaintOrder, searchSi, searchLen, aItemID);
			//search new position for item
			ItemPORangeGet(aSelectionType, out searchSi, out searchLen);
			int pasteInd = -1 ^ Array.BinarySearch(_PaintOrder, searchSi, searchLen, aItemID);
			//shift array and paste item
			sbyte shift;
			if (pasteInd <= ind)
			{
				shift = 1;
				Array.Copy(_PaintOrder, pasteInd, _PaintOrder, pasteInd + 1, ind - pasteInd);
			}
			else 
			{
				shift = -1;
				--pasteInd;
				Array.Copy(_PaintOrder, ind + 1, _PaintOrder, ind, pasteInd - ind);
			}
			_PaintOrder[pasteInd] = aItemID;
			if ((_Selection[aItemID] | aSelectionType) != (SIST.sistAsGroup | SIST.sistAsItem))
				_PosGroupStart += shift;
			if (((_Selection[aItemID] | aSelectionType) & SIST.sistAsItem) != 0)
				_PosItemStart += shift;
			return true;
		}

		protected override void CloneTo(OlapScatterData aData)
		{
			base.CloneTo(aData);
			OlapScatterFormsData data = (OlapScatterFormsData)aData;
			data._ColorLevel = new Pair<string, string>(_ColorLevel.First, _ColorLevel.Second);
			data._CurrentGroup = new Pair<string, string>(_CurrentGroup.First, _CurrentGroup.Second);
			data._CurrentItem = _CurrentItem;
			data._CurrentPage = _CurrentPage;
			data._DetailsCaptions = _DetailsCaptions;
			data._DetailsLevelsCount = _DetailsLevelsCount;
			data._groupSelection = new List<string>(_groupSelection.Count);
			data._groupSelection.AddRange(_groupSelection);
			data._GroupSelectionLevelID = _GroupSelectionLevelID;
			data._HintsLocationShifts = BaseUtils.ArrayCopy(_HintsLocationShifts);
			data._ItemsCount = _ItemsCount;
			data._ItemSelectionStart = BaseUtils.ArrayCopy(_ItemSelectionStart);
			data._ItemsHints = new AdvancedHint[_ItemsCount];
			data._Measures = _Measures;
			data._Opacity = _Opacity;
			data._PaintOrder = BaseUtils.ArrayCopy(_PaintOrder);
			data._PosGroupStart = _PosGroupStart;
			data._PosItemStart = _PosItemStart;
			data._Selection = BaseUtils.ArrayCopy(_Selection);
			data._ShowHints = _ShowHints;
			data._ShowTrails = _ShowTrails;
			data._XLogarithmicScale = _XLogarithmicScale;
			data._YLogarithmicScale = _YLogarithmicScale;
		}
		
		public static readonly Pair<string, string> GROUP_NONE = new Pair<string, string>(string.Empty, string.Empty);
		public static readonly Pair<string, string> LEVEL_NONE = new Pair<string, string>(string.Empty, string.Empty);
		
		public OlapScatterFormsData(int aPagesCount, int aItemsCount): base(aPagesCount, aItemsCount)
		{
			_ItemsCount = aItemsCount;
			_Selection = new SIST[aItemsCount];
			_PaintOrder = new int[aItemsCount];
			_ItemSelectionStart = new int[aItemsCount];
			_HintsLocationShifts = new Point[aItemsCount];
			int i;
			for (i = 0; i < aItemsCount; ++i)
			{
				_PaintOrder[i] = i;
				_ItemSelectionStart[i] = -1;
			}
			_PosGroupStart = _PosItemStart = aItemsCount;
			_groupSelection = new List<string>();
			_ItemsHints = new AdvancedHint[aItemsCount];
		}
		
		/// <summary>
		/// Забирає шлях елементів, але залишає вибірку незмінною
		/// </summary>
		public void ClearTrails()
		{
			if (_ShowTrails)
			{
				_ShowTrails = false;
				ItemsSelectionStartUpdate();
				ShowTrails = true;
			}
		}
		public override object Clone()
		{
			//do not call base method
			OlapScatterFormsData data = new OlapScatterFormsData(PagesCount, ItemsCount);
			CloneTo(data);
			return data;
		}
		/// <summary>
		/// Для кожного з елементів прораховує його колір. Якщо колір елементів залежить від міри, то нічого не робить.
		/// </summary>
		public void ColorsCalculate()
		{
			OlapScatterDataItem[][] pages = Pages;
			Color color;
			if (ColorsFromLevel)
				for (int i = 0; i < _ItemsCount; ++i)
				{
					color = GradientUtils.DifferentColorGet(ItemColorParent[i]);
					for (int p = 0; p < PagesCount; ++p)
						pages[p][i].Color = color;
				}
		}
		/// <summary>
		/// Повертає список індексів елементів поточної групи 
		/// </summary>
		/// <returns>Список індексів елементів поточної групи в порядку відмальовки. 
		/// Якщо поточна група відсутня, то результатом буде порожній список.</returns>
		public List<int> CurrentGroupItemsIDsGet()
		{
			List<int> list = new List<int>();
			int i;
			int groupLevelHeight = GroupLevelIndexGet(_CurrentGroup.First);
			if (groupLevelHeight > -1)	//якщо ID поточної групи є серед деталей
			{
				for (i = 0; i < _ItemsCount; ++i)
					if (ItemsMemberItems[i][groupLevelHeight].UniqueName == _CurrentGroup.Second)
						list.Add(i);
			}
			else if (_CurrentGroup.First == _ColorLevel.First)	//якщо поточна група належить до кольору
			{
				int ind = -1;
				for (i = 0; i < ColorsMemberItems.Count; ++i)
					if (ColorsMemberItems[i].UniqueName == _CurrentGroup.Second)
					{
						ind = i;
						break;
					}
				for (i = 0; i < _ItemsCount; ++i)
					if (ItemColorParent[i] == ind)
						list.Add(i);
			}
			return list;
		}
		/// <summary>
		/// Видаляє групу з вибірки
		/// </summary>
		/// <param name="aGroupLevelID">ID рівня до якого належить група</param>
		/// <param name="aGroupID">ID елемента рівня <c>aGroupLevelID</c></param>
		public void GroupUnselect(string aGroupLevelID, string aGroupID)
		{
			if (aGroupLevelID != _GroupSelectionLevelID || !_groupSelection.Contains(aGroupID))	//якщо група не вибрана
				return;
			MemberItem[][] itemsMemberItems = ItemsMemberItems;
			List<MemberItem> colorsMemberItems = ColorsMemberItems;
			int[] itemColorParent = ItemColorParent;
			bool selectionChanged = _groupSelection.Contains(aGroupID);
			_groupSelection.Remove(aGroupID);
			int i, op, groupLevelHeight = GroupLevelIndexGet(aGroupLevelID);
			for (i = _PosGroupStart; i < _ItemsCount; ++i)
			{
				op = _PaintOrder[i];
				if ((groupLevelHeight == -1 ? colorsMemberItems[itemColorParent[op]] : itemsMemberItems[op][groupLevelHeight]).UniqueName == aGroupID)
				{
					if (i < _PosItemStart)
						selectionChanged |= Select(op, SIST.sistNone);
					_Selection[op] ^= SIST.sistAsGroup;
				}
			}
			if (selectionChanged)
				EventSelectionChangedRaise(new EventArgsSelectionChanged(aGroupID));
		}
		/// <summary>
		/// Видаляє всі групи з вибірки
		/// </summary>
		public void GroupsAllUnselect()
		{
			int i, op;
			bool selectionChanged = false;
			for (i = _PosGroupStart; i < _ItemsCount; ++i)
			{
				op = _PaintOrder[i];
				if ((_Selection[op] & SIST.sistAsGroup) != 0)
				{
					selectionChanged = true;
					_Selection[op] ^= SIST.sistAsGroup;
				}
			}
			_PosGroupStart = _PosItemStart;
			Array.Sort(_PaintOrder, 0, _PosGroupStart);
			_groupSelection.Clear();
			if (selectionChanged)
				EventSelectionChangedRaise(new EventArgsSelectionChanged(true, true, false));
		}
		/// <summary>
		/// Додає групу до вибірки
		/// </summary>
		/// <param name="aGroupLevelID">ID рівня до якого належить група</param>
		/// <param name="aGroupID">ID елемента рівня <c>aGroupLevelID</c></param>
		public void GroupSelect(string aGroupLevelID, string aGroupID)
		{
			if (_groupSelection.Contains(aGroupID))
				return;
			_groupSelection.Add(aGroupID);
			_GroupSelectionLevelID = aGroupLevelID;
			MemberItem[][] itemsMemberItems = ItemsMemberItems;
			int i, op, groupLevelHeight = GroupLevelIndexGet(aGroupLevelID);
			bool selectionChanged = false;
			//search among unselected elements
			for (i = 0; i < _PosGroupStart; )
			{
				op = _PaintOrder[i];
				if ((groupLevelHeight == -1 ? ColorsMemberItems[ItemColorParent[op]]
						: itemsMemberItems[op][groupLevelHeight]).UniqueName == aGroupID)
				{
					selectionChanged |= Select(op, SIST.sistAsGroup);
					_Selection[op] |= SIST.sistAsGroup;
				}
				else
					++i;
			}
			//search among selected items
			for (i = _PosItemStart; i < _ItemsCount; ++i)
			{
				op = _PaintOrder[i];
				if ((groupLevelHeight == -1 ? ColorsMemberItems[ItemColorParent[op]]
						: itemsMemberItems[op][groupLevelHeight]).UniqueName == aGroupID
				    && (_Selection[op] & SIST.sistAsGroup) == 0)
				{
					selectionChanged = true;
					_Selection[op] |= SIST.sistAsGroup;
				}
			}
			if (selectionChanged)
				EventSelectionChangedRaise(new EventArgsSelectionChanged(aGroupID));
		}
		/// <summary>
		/// Перевіряє чи належить група до вибірки
		/// </summary>
		/// <param name="aGroupID">ID групи</param>
		/// <returns><c>true</c> якщо група належить до вибірки і <c>false</c> в іншому випадку</returns>
		public bool GroupSelectedCheck(string aGroupID)
		{
			return _groupSelection.Contains(aGroupID);
		}
		/// <summary>
		/// Видаляє всі хінти
		/// </summary>
		public void HintsDispose()
		{
			int i, oop;
			for (i = _PosItemStart; i < _ItemsCount; ++i)
			{
				oop = _PaintOrder[i];
				if (_ItemsHints[oop] != null)
				{
					//_ItemsHints[oop].Visible = false;
					_ItemsHints[oop].EventLocationShiftChanged -= Hint_EventLocationShiftChanged;
					_ItemsHints[oop].Dispose();
					_ItemsHints[oop] = null;
				}
			}
		}
		/// <summary>
		/// Завантажує з старих даних хінти для вибраних елементів у випадку якщо елементи співпадають.
		/// </summary>
		/// <param name="aOldData">Дані з яких потрібно завантажити хінти</param>
		public void HintsImport(OlapScatterFormsData aOldData)
		{
			if (aOldData == null)
				return;
			int line = 0;
			try
			{
				int i, j, nop, oop, k;
				bool equals;
																																					line = 1;
				//будуємо колекцію кореляції рівнів старих до нових даних
				int[] levelsCorelation = new int[aOldData.DetailsLevelsCount];
				for (i = 0; i < aOldData.DetailsLevelsCount; ++i)
					levelsCorelation[i] = _DetailsCaptions.IndexOf(aOldData._DetailsCaptions[i]);
																																					line = 2;
				for (i = _PosItemStart; i < _ItemsCount; ++i)	//проходимося по всіх вибраних елементах в нових даних
				{
																																					line = 3;
					nop = _PaintOrder[i];
																																					line = 4;
					if (_ItemsHints[nop] == null)	//елемент не має хінта
						for (j = aOldData._PosItemStart; j < aOldData._ItemsCount; ++j)	//проходимося по всіх вибраних елементах в старих даних
						{
																																					line = 5;
							//знаходимо серед старих даних елемент що відповідає відміченому в нових даних
							oop = aOldData._PaintOrder[j];
							equals = true;
																																					line = 6;
							for (k = 0; k < levelsCorelation.Length; ++k)
								if (levelsCorelation[k] != -1)
									if (!aOldData.ItemsMemberItems[oop][k].Equals(ItemsMemberItems[nop][levelsCorelation[k]]))
									{
																																					line = 7;
										equals = false;
										break;
									}
																																					line = 8;
							if (equals && aOldData._ItemsHints[oop] != null)	//знайшли еквівалентний елемент в старих даних
							{
								//імпортуємо хінт в нові дані
																																					line = 9;
								_ItemsHints[nop] = aOldData._ItemsHints[oop];
																																					line = 10;
								_ItemsHints[nop].ID = nop;
																																					line = 11;
								_ItemsHints[nop].LocationShift = _HintsLocationShifts[nop];
																																					line = 12;
								_ItemsHints[nop].EventLocationShiftChanged += Hint_EventLocationShiftChanged;
																																					line = 13;
								//видаляємо хінт зі старих даних
								aOldData._ItemsHints[oop].EventLocationShiftChanged -= aOldData.Hint_EventLocationShiftChanged;
								aOldData._ItemsHints[oop] = null;
								//припиняємо шукати еквівалентний елемент, бо його вже знайдено
								break;
							}
						}
				}
																																					line = 14;
				//Dispose old hints which are not assigned to new selected elements
				aOldData.HintsDispose();
			}
			catch(Exception ex)
			{
				throw new ExceptionFramework(string.Format("HintsImport exception, line = {0}", line), ex, ExceptionKind.ekDeveloper);
			}
		}
		/// <summary>
		/// Створює новий хінт для елемента
		/// </summary>
		/// <param name="aItemID">Номер елемента для якого потрібно створити хінт</param>
		/// <param name="aControlledControl">Контрол в який потрібно добавити хінт</param>
		/// <returns>створений для елемента хінт</returns>
		public AdvancedHint ItemHintCreate(int aItemID, Control aControlledControl)
		{
			_ItemsHints[aItemID] = new AdvancedHint(aControlledControl, false) {Visible = false, LocationShift = _HintsLocationShifts[aItemID]};
			_ItemsHints[aItemID].EventLocationShiftChanged += Hint_EventLocationShiftChanged;
			return _ItemsHints[aItemID];
		}
		/// <summary>
		/// Повертає хінт для елемента
		/// </summary>
		/// <param name="aItemID">Номер елемента для якого потрібно повернути хінт</param>
		/// <returns>Повертає хінт для елемента</returns>
		public AdvancedHint ItemHintGet(int aItemID)
		{
			return _ItemsHints[aItemID];
		}
		/// <summary>
		/// Обчислює прозорість елемента в залежності від того вибраний він чи ні
		/// </summary>
		/// <param name="aItemID">Номер елемента для якого потрібно обчислити прозорість</param>
		/// <returns>Повертає ціле число - прозорість елемента</returns>
		public int ItemOpacityGet(int aItemID)
		{
			//Якщо елемент не вибраний і існують вибрані елементи, то елементи напівпрозорий інакше - непрозорий
			return _Selection[aItemID] == SIST.sistNone && _ItemsCount - _PosGroupStart > 0 ? _Opacity : 255;
		}
		/// <summary>
		/// Видаляє всі елементи з вибірки які вибрані як елементи
		/// </summary>
		public void ItemsAllUnselect()
		{
			if (_PosItemStart == _ItemsCount)
				return;
			int i, op;
			int[] tmpGroups = new int[_ItemsCount - _PosGroupStart];
			int[] tmpItems = new int[_ItemsCount - _PosItemStart];
			int gCount = _PosItemStart - _PosGroupStart, iCount = 0;
			//move selected groups
			Array.Copy(_PaintOrder, _PosGroupStart, tmpGroups, 0, gCount);
			//go throught selected items and if it was also selected as group add to group else add to items
			for (i = _PosItemStart; i < _ItemsCount; ++i)
			{
				op = _PaintOrder[i];
				_Selection[op] ^= SIST.sistAsItem;
				if (_Selection[op] == SIST.sistAsGroup)
				{
					tmpGroups[gCount] = op;
					++gCount;
				}
				else
				{
					tmpItems[iCount] = op;
					++iCount;
				}
			}
			//paste now unselected items
			Array.Copy(tmpItems, 0, _PaintOrder, _PosGroupStart, iCount);
			//move slection indixes
			_PosGroupStart += iCount;
			_PosItemStart = _ItemsCount;
			//paste selected groups
			Array.Copy(tmpGroups, 0, _PaintOrder, _PosGroupStart, gCount);
			//sort unselected elements
			Array.Sort(_PaintOrder, 0, _PosGroupStart);
			Array.Sort(_PaintOrder, _PosGroupStart, _ItemsCount - _PosGroupStart);
			EventSelectionChangedRaise(new EventArgsSelectionChanged(true, false, true));
		}
		/// <summary>
		/// Додає елемент до вибірки
		/// </summary>
		/// <param name="aItemID">Номер елемента</param>
		public void ItemSelect(int aItemID)
		{
			if (Select(aItemID, SIST.sistAsItem))
			{
				_Selection[aItemID] |= SIST.sistAsItem;
				if (_CurrentPage >= 0)
					_ItemSelectionStart[aItemID] = Math.Max(_CurrentPage, ItemVisibleStartGet(aItemID, _CurrentPage));
				EventSelectionChangedRaise(new EventArgsSelectionChanged(aItemID));
			}
		}
		/// <summary>
		/// Повертає тип вибірки для елементу
		/// </summary>
		/// <param name="aItemID">Номер елементу для якого потрібно повернути тип вибірки</param>
		/// <returns>Тип вибірки для елементу</returns>
		public SIST ItemSelectionGet(int aItemID)
		{
			return _Selection[aItemID];
		}
		/// <summary>
		/// Видаляє елемент з вибірки
		/// </summary>
		/// <param name="aItemID">Номер елемента якого потрібно видалити з вибірки</param>
		public void ItemUnselect(int aItemID)
		{
			SIST newSelection = (_Selection[aItemID] & SIST.sistAsGroup) == 0 ? SIST.sistNone : SIST.sistAsGroup;
			if (Select(aItemID, newSelection))
			{
				_Selection[aItemID] = newSelection;
				_ItemSelectionStart[aItemID] = -1;
				EventSelectionChangedRaise(new EventArgsSelectionChanged(aItemID));
			}
		}
		/// <summary>
		/// Визначає порядковий номер елемента при відмальовці
		/// </summary>
		/// <param name="aItemID">Номер елементу</param>
		/// <returns>ціле число - порядковий номер елемента при відмальовці</returns>
		public int OrderPaintIndexGet(int aItemID)
		{
			int si, len;
			ItemPORangeGet(ItemSelectionGet(aItemID), out si, out len);
			if (si < 0 || len < 0)
				throw new ExceptionFramework("Invalid ScatterItemSelectionType", ExceptionKind.ekDeveloper);
			return Array.BinarySearch(_PaintOrder, si, len, aItemID);
		}
		/// <summary>
		/// Встановлює вибірку
		/// </summary>
		/// <param name="aSelection">Вибірка, яку потрібна накласти на дані</param>
		public void SelectionSet(OlapScatterSelection aSelection)
		{
			if (aSelection == null)
				return;
			_RaiseEvents = false;
			try
			{
				_ShowTrails = aSelection.ShowTrails;
				_ShowHints = aSelection.ShowHints;
				_Opacity = aSelection.Opacity;
				//встановлюємо поточну сторінку
				if (PagesPresent)
					_CurrentPage = PagesMemberItems.FindIndex(delegate(MemberItem aMi) { return aMi.UniqueName == aSelection.SelectedPageID; });
				if (_CurrentPage == -1)
					_CurrentPage = 0;
				MemberItem[][] itemsMemberItems = ItemsMemberItems;
				if (itemsMemberItems.Length > 0)
				{
					//generate 2 dictionaries with LevelIDs as Keys and position in Details as Values
					int oldLevelCount = aSelection.Details.Length;
					Dictionary<string, int> oldLevels = new Dictionary<string, int>(oldLevelCount);
					Dictionary<string, int> newLevels = new Dictionary<string, int>(itemsMemberItems[0].Length);
					int i;
					for (i = 0; i < oldLevelCount; ++i)
						oldLevels.Add(aSelection.Details[i], i);
					for (i = 0; i < itemsMemberItems[0].Length; ++i)
						newLevels.Add(itemsMemberItems[0][i].LevelName, i);
					//Create level corelation array
					int[] levelsCorelation = new int[oldLevelCount];
					foreach (KeyValuePair<string, int> pair in oldLevels)
						levelsCorelation[pair.Value] = newLevels.ContainsKey(pair.Key) ? newLevels[pair.Key] : -1;
					//create group selection
					int levelHeight = Array.IndexOf(aSelection.Details, aSelection.GroupSelectionLevelID);
					if (_ColorLevel.First == aSelection.GroupSelectionLevelID ||
					    (levelHeight != -1 && levelsCorelation[levelHeight] != -1))
						foreach (string str in aSelection.SelectedGroups)
							GroupSelect(aSelection.GroupSelectionLevelID, str);
					//Check was added some new level?
					bool addItemSelection = true;
					foreach (KeyValuePair<string, int> pair in newLevels)
						if (!oldLevels.ContainsKey(pair.Key))
						{
							addItemSelection = false;
							break;
						}
					if (addItemSelection) //if no new level present
					{
						//add item selection
						int j, k, nop;
						bool addItem;
						for (i = 0; i < aSelection.SelectedItems.Count; ++i)
						{
							for (j = 0; j < _ItemsCount; ++j)
							{
								nop = _PaintOrder[j];
								addItem = true;
								for (k = 0; k < oldLevelCount; ++k)
									if (levelsCorelation[k] != -1)
										if (aSelection.SelectedItems[i][k] != itemsMemberItems[nop][levelsCorelation[k]].UniqueName)
										{
											addItem = false;
											break;
										}
								if (addItem)
								{
									ItemSelect(nop);
									_HintsLocationShifts[nop] = aSelection.HintsLocationShift[i];
									_ItemSelectionStart[nop] = PagesMemberItems.FindIndex(delegate(MemberItem aMi) { return aMi.UniqueName == aSelection.SelectionStartPageIDs[i]; });
									if (_ItemSelectionStart[nop] == -1 || _ItemSelectionStart[nop] > _CurrentPage)
										_ItemSelectionStart[nop] = _CurrentPage;
									if (_ItemSelectionStart[nop] != -1 && !Pages[_ItemSelectionStart[nop]][nop].CanShow)
										_ItemSelectionStart[nop] = ItemVisibleStartGet(nop, _ItemSelectionStart[nop]);
									break;
								}
							}
						}
					}
				}
				if ((Min.MX.ValueType & MeasureValueType.mvtNumber) != MeasureValueType.mvtNotSet && (Min.MY.ValueType & MeasureValueType.mvtNumber) != MeasureValueType.mvtNotSet)
				{
					_XLogarithmicScale = aSelection.XLogarithmicScale && !(Min.MX.NumericValue <= 0 && Max.MX.NumericValue >= 0);
					_YLogarithmicScale = aSelection.YLogarithmicScale && !(Min.MY.NumericValue <= 0 && Max.MY.NumericValue >= 0);
				}
				else
					_XLogarithmicScale = _YLogarithmicScale = false;
			}
			finally
			{
				_RaiseEvents = true;
			}
		}

		/// <summary>
		/// Рівень що в кольорі. Перший елемент - ID, другий - Caption. 
		/// Якщо в кольорі не рівень, то повертає OlapScatterFormsData.LEVEL_NONE.
		/// </summary>
		public Pair<string, string> ColorLevel
		{
			get { return _ColorLevel; }
			set
			{
				_ColorLevel = value ?? LEVEL_NONE;
			}
		}
		/// <summary>
		/// Поточна група. Перший елемент - ID рівня, другий ID - елемента з цього рівня.
		/// OlapScatterFormsData.GROUP_NONE означає що поточної групи немає.
		/// </summary>
		public Pair<string, string> CurrentGroup
		{
			get { return _CurrentGroup; }
			set
			{
				if (_CurrentGroup != value)
				{
					_CurrentGroup = value;
					EventCurrentGroupChangedRaise();
				}
			}
		}
		/// <summary>
		/// Номер поточного елементу.
		/// -1 означає що поточного елементу немає.
		/// </summary>
		public int CurrentItem
		{
			get { return _CurrentItem; }
			set
			{
				if (_CurrentItem != value)
				{
					_CurrentItem = value;
					EventCurrentItemChangedRaise();
				}
			}
		}
		/// <summary>
		/// Номер поточної сторінки
		/// </summary>
		public int CurrentPage
		{
			get { return _CurrentPage; }
			set
			{
				if (_CurrentPage != value)
				{
					_CurrentPage = value;
					ItemsSelectionStartUpdate();
					EventCurrentPageChangedRaise();
				}
			}
		}
		/// <summary>
		/// Список рівнів що в деталях. 
		/// В кожному елементі списку перший елемент - ID другий - Caption для рівня.
		/// </summary>
		public List<Pair<string, string>> DetailsCaptions
		{
			get { return _DetailsCaptions; }
			set
			{
				_DetailsCaptions = value;
				_DetailsLevelsCount = _DetailsCaptions == null ? 0 : _DetailsCaptions.Count;
			}
		}
		/// <summary>
		/// Кількість рівнів що в деталях
		/// </summary>
		public int DetailsLevelsCount
		{
			get { return _DetailsLevelsCount; }
		}
		/// <summary>
		/// ID рівня з якого є вибрані групи
		/// </summary>
		public string GroupSelectionLevelID
		{
			get { return _GroupSelectionLevelID; }
		}
		/// <summary>
		/// Масив зміщення хінтів відносно елементів до яких вони належать
		/// </summary>
		public Point[] HintsLocationShifts
		{
			get { return _HintsLocationShifts; }
		}
		/// <summary>
		/// Масив що містить номери сторінок з яких починається відмальовка шляху для кожного з елементів.
		/// </summary>
		public int[] ItemSelectionStart
		{
			get { return _ItemSelectionStart; }
		}
		/// <summary>
		/// Список мір з яких сформовані дані.
		/// Порядок мір див. константи регіону OlapScatterDataItem.MEASURES_INDEXES
		/// </summary>
		public List<OlapMeasureObjectBase> Measures
		{
			get { return _Measures; }
			set { _Measures = value; }
		}
		/// <summary>
		/// Прозорість не вибраних елементів
		/// </summary>
		public int Opacity
		{
			get { return _Opacity; }
			set
			{
				if (_Opacity != value)
				{
					_Opacity = value;
					EventOpacityChangedRaise();
				}
			}
		}
		/// <summary>
		/// Порядок відмальовки елементів.
		/// </summary>
		public int[] PaintOrder
		{
			get { return _PaintOrder; }
		}
		/// <summary>
		/// Enables or disables events raising when data is changed.
		/// </summary>
		public bool RaiseEvents
		{
			get { return _RaiseEvents; }
			set { _RaiseEvents = value; }
		}
		/// <summary>
		/// Кількість вибраних груп
		/// </summary>
		public int SelectedGroupsCount
		{
			get { return _groupSelection.Count; }
		}
		/// <summary>
		/// Індекс в масиві PaintOrder з якого починаються вибрані елементи як групи
		/// </summary>
		public int SelectedGroupsStart
		{
			get { return _PosGroupStart; }
		}
		/// <summary>
		/// Індекс в масиві PaintOrder з якого починаються вибрані елементи як елементи
		/// </summary>
		public int SelectedItemsStart
		{
			get { return _PosItemStart; }
		}
		public bool ShowHints
		{
			get { return _ShowHints; }
			set
			{
				if (_ShowHints != value)
				{
					_ShowHints = value;
					EventShowHintsChangedRaise();
				}
			}
		}
		public bool ShowTrails
		{
			get { return _ShowTrails; }
			set
			{
				if (_ShowTrails != value)
				{
					_ShowTrails = value;
					ItemsSelectionStartUpdate();
					EventShowTrailsChangedRaise();
				}
			}
		}
		/// <summary>
		/// Повертає структуру яка містить все що пов'язано з вибіркою.
		/// </summary>
		public OlapScatterSelection TotalSelection
		{
			get
			{
				int j, op = Int32.MinValue;
				try
				{
					int i;
					string[] details = new string[_DetailsLevelsCount];
					for (i = 0; i < _DetailsCaptions.Count; ++i)
						details[i] = _DetailsCaptions[i].First;
					OlapScatterSelection selection = new OlapScatterSelection(details) { GroupSelectionLevelID = _GroupSelectionLevelID, Opacity = _Opacity };
					selection.SelectedGroups.AddRange(_groupSelection);
					selection.SelectedPageID = PagesPresent ? PagesMemberItems[_CurrentPage].UniqueName : string.Empty;
					selection.ShowHints = _ShowHints;
					selection.ShowTrails = _ShowTrails;
					string[] tmp;
					MemberItem[][] imi = ItemsMemberItems;
					for (i = _PosItemStart; i < _ItemsCount; ++i)
					{
						tmp = new string[_DetailsLevelsCount];
						op = _PaintOrder[i];
						for (j = 0; j < _DetailsLevelsCount; ++j)
							tmp[j] = imi[op][j].UniqueName;
						selection.HintsLocationShift.Add(_ItemsHints[op] == null || !_ItemsHints[op].Visible
															? Point.Empty
															: _ItemsHints[op].LocationShift);
						selection.SelectedItems.Add(tmp);
						selection.SelectionStartPageIDs.Add(PagesPresent && _ItemSelectionStart[op] != -1
																? PagesMemberItems[_ItemSelectionStart[op]].UniqueName
																: string.Empty);
					}
					selection.XLogarithmicScale = _XLogarithmicScale;
					selection.YLogarithmicScale = _YLogarithmicScale;
					return selection;
				}
				catch (Exception ex)
				{
					string message = string.Format("Exception in OlapScatterFormsData.TotalSelection getter." + Environment.NewLine + 
						" _DetailsLevelsCount: {0}, _DetailsCaptions.Count: {1}, " +
						"_PosItemStart: {2}, _ItemsCount: {3}, _PaintOrder.Length: {4}, op: {5}, _ItemsHints.Length: {6}, _ItemSelectionStart.Length: {7}",
						_DetailsLevelsCount, _DetailsCaptions.Count, _PosItemStart, _ItemsCount, _PaintOrder.Length, op, _ItemsHints.Length, _ItemSelectionStart.Length);
					if (op >= 0 && op < _ItemSelectionStart.Length)
						message += string.Format(", PagesMemberItems.Count: {0}, _ItemSelectionStart[op]: {1}", PagesMemberItems.Count, _ItemSelectionStart[op]);
					throw new ExceptionFramework(message, ex, ExceptionKind.ekDeveloper);
				}
			}
		}
		public bool XLogarithmicScale
		{
			get { return _XLogarithmicScale; }
			set
			{
				if (_XLogarithmicScale != value)
				{
					_XLogarithmicScale = value;
					EventLogarithmicScalesSelectionChangedRaise();
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
					EventLogarithmicScalesSelectionChangedRaise();
				}
			}
		}

		public event EventHandler EventCurrentGroupChanged
		{
			add { _EventCurrentGroupChanged = (EventHandler)Delegate.Combine(_EventCurrentGroupChanged, value); }
			remove { _EventCurrentGroupChanged = (EventHandler)Delegate.Remove(_EventCurrentGroupChanged, value); }
		}
		public event EventHandler EventCurrentItemChanged
		{
			add { _EventCurrentItemChanged = (EventHandler)Delegate.Combine(_EventCurrentItemChanged, value); }
			remove { _EventCurrentItemChanged = (EventHandler)Delegate.Remove(_EventCurrentItemChanged, value); }
		}
		public event EventHandler EventCurrentPageChanged
		{
			add { _EventCurrentPageChanged = (EventHandler)Delegate.Combine(_EventCurrentPageChanged, value); }
			remove { _EventCurrentPageChanged = (EventHandler)Delegate.Remove(_EventCurrentPageChanged, value); }
		}
		public event EventHandler EventLogarithmicScalesSelectionChanged
		{
			add { _EventLogarithmicScalesSelectionChanged = (EventHandler)Delegate.Combine(_EventLogarithmicScalesSelectionChanged, value); }
			remove { _EventLogarithmicScalesSelectionChanged = (EventHandler)Delegate.Remove(_EventLogarithmicScalesSelectionChanged, value); }
		}
		public event EventHandler EventNonMDXConfigurationChanged
		{
			add
			{
				EventCurrentPageChanged += value;
				EventLogarithmicScalesSelectionChanged += value;
				EventOpacityChanged += value;
				EventShowHintsChanged += value;
				EventShowTrailsChanged += value;
				_EventNonMDXConfigurationChanged = (EventHandler)Delegate.Combine(_EventNonMDXConfigurationChanged, value);
			}
			remove
			{
				EventCurrentPageChanged -= value;
				EventLogarithmicScalesSelectionChanged -= value;
				EventOpacityChanged -= value;
				EventShowHintsChanged -= value;
				EventShowTrailsChanged -= value;
				_EventNonMDXConfigurationChanged = (EventHandler)Delegate.Remove(_EventNonMDXConfigurationChanged, value);
			}
		}
		public event EventHandler EventOpacityChanged
		{
			add { _EventOpacityChanged = (EventHandler)Delegate.Combine(_EventOpacityChanged, value); }
			remove { _EventOpacityChanged = (EventHandler)Delegate.Remove(_EventOpacityChanged, value); }
		}
		public event EventHandlerSelectionChanged EventSelectionChanged
		{
			add { _EventSelectionChanged = (EventHandlerSelectionChanged)Delegate.Combine(_EventSelectionChanged, value); }
			remove { _EventSelectionChanged = (EventHandlerSelectionChanged)Delegate.Remove(_EventSelectionChanged, value); }
		}
		public event EventHandler EventShowHintsChanged
		{
			add { _EventShowHintsChanged = (EventHandler)Delegate.Combine(_EventShowHintsChanged, value); }
			remove { _EventShowHintsChanged = (EventHandler)Delegate.Remove(_EventShowHintsChanged, value); }
		}
		public event EventHandler EventShowTrailsChanged
		{
			add { _EventShowTrailsChanged = (EventHandler)Delegate.Combine(_EventShowTrailsChanged, value); }
			remove { _EventShowTrailsChanged = (EventHandler)Delegate.Remove(_EventShowTrailsChanged, value); }
		}
	}
}