using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using FrameworkBase.Utils;
using GMap.NET;
using OlapFormsFramework.Windows.Forms.Grid.Formatting;
using OlapFramework.Data;
using OlapFramework.Olap.Metadata;

namespace OlapFormsFramework.Windows.Forms.Grid.Scatter
{
	public class MapStoryboard
	{
		/// <summary>
		/// Символ, який буде з'являтися перед значенням міри, якщо це значення інтерпольовано
		/// </summary>
		private const char InterpolationChar = (char)0x2248;

		#region ResourceIDs
		private const string OlapScatterLabelNull = "OlapScatter_MeasureNullString";
		#endregion

		/// <summary>
		/// Масив даних.
		/// Перший індекс - номер сторінки. Другий індекс - номер елементу.
		/// </summary>
		public OlapMapCircleMarker[][] Pages { get; private set; }

		public OlapMapCircleMarker this[int page, int item]
		{
			get { return Pages[page][item]; }
			 
		}

		public Assembly ResourceAssembly { get; set; }


		public MapStoryboard(OlapScatterFormsData data, FormatRulesMeasures measuresFormatRules)
		{
			ResourceAssembly = typeof (MapStoryboard).Assembly;
			Pages = data.Pages.Select((items, i) => items?.Select(
									  (item,j) => ToMarker(data, measuresFormatRules, item, i, j, ResourceAssembly)).ToArray()).ToArray();
		}

		private static OlapMapCircleMarker ToMarker(
			OlapScatterFormsData data, 
			FormatRulesMeasures measuresFormatRules, 
			IMarkerData item, 
			int page, 
			int pos, 
			Assembly aResourceAssembly)
		{
			if (item == null)
				return null;
			var marker = new OlapMapCircleMarker(new PointLatLng(item.Latitude, item.Longitude));
			marker.PageID = page;
			marker.ItemID = pos;
			marker.IsVisible = item.CanShow; //TODO: check all canShow
			marker.IsHighlighted = false;
			marker.MarkerColor = item.Color;
			marker.Size = new SizeF(item.Size, item.Size);
			marker.IsInterpolated = item.IsInterpolated;
			marker.ToolTipText = HintConstruct(data, measuresFormatRules, item, page, pos, true, true, aResourceAssembly);
			marker.Tag = item;
			return marker;
		}


		/// <summary>
		/// Генерує текст хінта для заданого елементу
		/// </summary>
		/// <param name="aCurrentItem">Елемент для якого потрібно згенерувати хінт</param>
		/// <param name="aAddPageCaption">Вказує чи додавати до хінта інформацію про сторінку елементу</param>
		/// <param name="aAddItemID">Вказує чи додавати до хінта інформацію про елемент</param>
		/// <returns>Повертає Caption для хінта</returns>
		private static string HintConstruct(
			OlapScatterFormsData data, 
			FormatRulesMeasures measuresFormatRules, 
			IMarkerData item, 
			int aPageId, 
			int aItemId, 
			bool aAddPageCaption, 
			bool aAddItemID, 
			Assembly resourceAssembly)
		{
			if (item == null)
				return string.Empty;
			var sb = new StringBuilder();
			if (aAddPageCaption && data.PagesPresent)
				sb.AppendLine(data.PagesMemberItems[aPageId].Caption);
			if (aAddItemID)
				foreach (var mi in data.ItemsMemberItems[aItemId])
					sb.AppendLine(mi.Caption);
			int i, k = 0;
			for (i = 0; i < OlapScatterDataItem.MEASURES_COUNT; ++i)   //проходимось по всіх мірах
			{
				//якщо міра втягнута і значення не може бути відображене безпосередньо біля міри і значення для цієї міри ще не було додано
				if (data.Measures[i] != null
					&& !item.CanShow //_hints[i].CanShow ??????
					&& data.Measures.FindIndex(obj => obj != null && obj.ID == data.Measures[i].ID) == i)
				{
					//додаємо Caption і значення міри
					sb.Append(data.Measures[i].Caption);
					sb.Append(": ");
					if (item.Measures[k].ValueType == MeasureValueType.mvtError)
						sb.AppendLine("#ERR");
					else if (item.Measures[k].ValueType == MeasureValueType.mvtNull)
						sb.AppendLine(ResourceUtils.StringGet(OlapScatterLabelNull, resourceAssembly));
					else
						if (item.MeasureInterpolatedGet(k))
					{
						sb.Append(InterpolationChar);
						sb.AppendLine(measuresFormatRules.MeasureValueToString(item.Measures[k].NumericValue, 2, data.Measures[i]));
					}
					else
						sb.AppendLine(item.Measures[k].FormattedValue);
				}
				++k;
			}
			return sb.ToString();
		}
	}
}
