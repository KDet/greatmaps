using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using FrameworkBase.Utils;
using GMap.NET;
using OlapFormsFramework.Windows.Forms.Grid.Formatting;
using OlapFramework.Data;

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

		private static OlapMapCircleMarker ToMarker(
			OlapScatterFormsData aData, 
			FormatRulesMeasures aFormatRules, 
			IMarkerData aItem, 
			int aPageID, 
			int aItemID, 
			Assembly aResourceAssembly)
		{
			if (aItem == null)
				return null;
			var marker = new OlapMapCircleMarker(new PointLatLng(aItem.Latitude, aItem.Longitude), aItem.CanShow);//TODO: check all canShow
			marker.PageID = aPageID;
			marker.ItemID = aItemID;
			marker.IsHighlighted = false;
			marker.MarkerColor = aItem.Color;
			marker.Size = new SizeF(aItem.Size, aItem.Size);
			marker.IsInterpolated = aItem.IsInterpolated;
			marker.ToolTipText = HintConstruct(aData, aFormatRules, aItem, aPageID, aItemID, true, true, aResourceAssembly);
			marker.Tag = aItem;
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
			OlapScatterFormsData aData, 
			FormatRulesMeasures aFormatRules, 
			IMarkerData aItem, 
			int aPageID, 
			int aItemID, 
			bool aAddPageCaption, 
			bool aAddItemID, 
			Assembly aResourceAssembly)
		{
			if (aItem == null)
				return string.Empty;
			var sb = new StringBuilder();
			if (aAddPageCaption && aData.PagesPresent)
				sb.AppendLine(aData.PagesMemberItems[aPageID].Caption);
			if (aAddItemID)
				foreach (var mi in aData.ItemsMemberItems[aItemID])
					sb.AppendLine(mi.Caption);
			for (int i = 0, k = 0; i < OlapScatterDataItem.MEASURES_COUNT; ++i)   //проходимось по всіх мірах
			{
				//якщо міра втягнута і значення не може бути відображене безпосередньо біля міри і значення для цієї міри ще не було додано
				if (aData.Measures[i] != null
					&& !aItem.CanShow //_hints[i].CanShow ??????
					&& aData.Measures.FindIndex(obj => obj != null && obj.ID == aData.Measures[i].ID) == i)
				{
					//додаємо Caption і значення міри
					sb.Append(aData.Measures[i].Caption);
					sb.Append(": ");
					if (aItem.Measures[k].ValueType == MeasureValueType.mvtError)
						sb.AppendLine("#ERR");
					else if (aItem.Measures[k].ValueType == MeasureValueType.mvtNull)
						sb.AppendLine(ResourceUtils.StringGet(OlapScatterLabelNull, aResourceAssembly));
					else if (aItem.MeasureInterpolatedGet(k))
					{
						sb.Append(InterpolationChar);
						sb.AppendLine(aFormatRules.MeasureValueToString(aData.Measures[i], aItem.Measures[k].NumericValue, 2));
					}
					else
						sb.AppendLine(aItem.Measures[k].FormattedValue);
				}
				++k;
			}
			return sb.ToString();
		}

		public MapStoryboard(
			OlapScatterFormsData aData,
			FormatRulesMeasures aFormatRules)
		{
			if (aData == null)
				throw new ArgumentNullException(nameof(aData));
			if (aFormatRules == null)
				throw new ArgumentNullException(nameof(aFormatRules));

			ResourceAssembly = typeof(MapStoryboard).Assembly;
			Pages = aData.Pages?.Select((items, i) => items?.Select(
										(item, j) => ToMarker(aData, aFormatRules, item, i, j, ResourceAssembly)).ToArray()).ToArray()
							   ?? new OlapMapCircleMarker[0][];
		}

		/// <summary>
		/// Масив даних.
		/// Перший індекс - номер сторінки. Другий індекс - номер елементу.
		/// </summary>
		public OlapMapCircleMarker[][] Pages { get; private set; }
		public Assembly ResourceAssembly { get; set; }

		public OlapMapCircleMarker this[int page, int item]
		{
			get { return Pages[page][item]; }
		}   
	}
}
