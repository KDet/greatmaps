using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using DevExpress.Skins;
using FormsFramework.Windows.Forms;
using FrameworkBase.Exceptions;
using FrameworkBase.Log;
using OlapFormsFramework.Utils;
using OlapFormsFramework.Windows.Forms.Grid.Formatting;
using OlapFramework.Data;
using OlapFramework.Olap.Metadata;

namespace OlapFormsFramework.Windows.Forms.Grid.Scatter
{
	public static class OlapControlsExtensions
	{
		public static Color SkinBackColorGet(this ISkinProvider aLookAndFeel)
		{
			var skin = CommonSkins.GetSkin(aLookAndFeel);
			return skin.Colors["Control"];
		}
        public static Color ReplaceColor(this Color color, Color which, Color to)
        {
            //return color == Color.White ? Color.LightGray : color;
            return color == which ? to : color;
        }
		public static PointF Offset(this PointF pointF, float x, float y)
		{
			return new PointF(pointF.X + x, pointF.Y + y);
		}
		public static Color Interpolate(this Color color, Color to, float coefficient = 1, int alpha=255)
	    {
            var cr = (int)(color.R + (to.R - color.R) * coefficient);
            var cg = (int)(color.G + (to.G - color.G) * coefficient);
            var cb = (int)(color.B + (to.B - color.B) * coefficient);
	        return Color.FromArgb(alpha, cr, cg, cb);
	    }
	    public static float Interpolate(this float value, float to, float coefficient = 1)
        {
            return value + (to - value) * coefficient;
        }
		public static void HintDismiss(this AdvancedHint aHint)
		{
			if (aHint == null)
				throw new ArgumentNullException(nameof(aHint));
			aHint.Visible = false;
			aHint.Highlighted = false;
		}

		/// <summary>
		/// Форматує значення міри.
		/// </summary>
		/// <param name="aValue">Значення що потрібно відформатувати</param>
		/// <param name="aDigitsAfterPoint">Кількість точок після коми</param>
		/// <param name="aMeasure">Міра, значення якої передано в параметрі <paramref name="aValue"/>.</param>
		/// <returns>Повертає відформатоване значення <paramref name="aValue"/> відносно параметрів <paramref name="aDigitsAfterPoint"/> і <paramref name="aMeasure"/>.</returns>
		public static string MeasureValueToString(this FormatRulesMeasures aRules, double aValue, int aDigitsAfterPoint, OlapMeasureObjectBase aMeasure)
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
