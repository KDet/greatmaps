using System;
using System.Drawing;
using DevExpress.Skins;
using FormsFramework.Windows.Forms;
using OlapFormsFramework.Utils;
using OlapFormsFramework.Windows.Forms.Grid.Formatting;
using OlapFramework.Olap.Metadata;

namespace OlapFormsFramework.Windows.Forms.Grid.Scatter
{
	public static class OlapControlsExtensions
	{
		public static Color Interpolate(this Color aColor, 
			Color aTo, 
			float aCoefficient = 1, 
			int aAlpha = 255)
		{
			var cr = (int)(aColor.R + (aTo.R - aColor.R) * aCoefficient);
			var cg = (int)(aColor.G + (aTo.G - aColor.G) * aCoefficient);
			var cb = (int)(aColor.B + (aTo.B - aColor.B) * aCoefficient);
			return Color.FromArgb(aAlpha, cr, cg, cb);
		}
		public static float Interpolate(this float aValue, 
			float aTo, 
			float aCoefficient = 1)
		{
			return aValue + (aTo - aValue) * aCoefficient;
		}
		public static Color ReplaceColor(this Color aColor, 
			Color aWhich, 
			Color aTo)
        {
            //return color == Color.White ? Color.LightGray : color;
            return aColor == aWhich ? aTo : aColor;
        }
		public static Color SkinBackColorGet(this ISkinProvider aLookAndFeel)
		{
			var skin = CommonSkins.GetSkin(aLookAndFeel);
			return skin.Colors["Control"];
		}
		public static PointF Offset(this PointF aPointF, 
			float aX, 
			float aY)
		{
			return new PointF(aPointF.X + aX, aPointF.Y + aY);
		}

		/// <summary>
		/// Форматує значення міри.
		/// </summary>
		/// <param name="aValue">Значення що потрібно відформатувати</param>
		/// <param name="aDigitsAfterPoint">Кількість точок після коми</param>
		/// <param name="aMeasure">Міра, значення якої передано в параметрі <paramref name="aValue"/>.</param>
		/// <returns>Повертає відформатоване значення <paramref name="aValue"/> відносно параметрів <paramref name="aDigitsAfterPoint"/> і <paramref name="aMeasure"/>.</returns>
		public static string MeasureValueToString(this FormatRulesMeasures aRules, 
			OlapMeasureObjectBase aMeasure, 
			double aValue, 
			int aDigitsAfterPoint)
		{
			var format = FormattingUtils.FormatSettingsGet(aRules, aMeasure.ID);
			if (format != null
				&& (format.FormatType == FormatType.ftDate && format.DateCategory != FormatDateCategory.fdcDefault
				   || format.FormatType == FormatType.ftNumber && format.NumberCategory != FormatNumberCategory.fcDefault))
				return format.Format((decimal)aValue);
			//if no custom format has been specified
			var measureFormat = aMeasure.MeasureFormat;
			switch (measureFormat)
			{
				case OlapMeasureFormat.mfPercent:
					aValue *= 100;
					aDigitsAfterPoint = Math.Max(0, aDigitsAfterPoint - 2);
					break;
			}
			var pow = Math.Pow(10, aDigitsAfterPoint + 1);
			if ((aValue * pow) % pow < 1)
			{
				aValue = Math.Round(aValue, aDigitsAfterPoint);
				aDigitsAfterPoint = 0;
			}
			var str = aDigitsAfterPoint == 0 ? aValue.ToString()
							: aValue.ToString(string.Format("0.{0}", new string('0', aDigitsAfterPoint)));
			switch (measureFormat)
			{
				case OlapMeasureFormat.mfPercent:
					return string.Format("{0}%", str);
				default:
					return str;
			}
		}
	}
}
