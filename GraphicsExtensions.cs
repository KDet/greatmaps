using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using FormsFramework.Utils;
using FrameworkBase.Exceptions;
using FrameworkBase.Log;
using FrameworkBase.Utils;
using OlapFormsFramework.Windows.Forms.Grid.Formatting;
using OlapFramework.Olap.Metadata;

namespace OlapFormsFramework.Windows.Forms.Grid.Scatter
{
	public static class GraphicsExtensions
	{	
		/// <summary>
		/// Масив кроків для підписів осей.
		/// Кожних два сусідні підписи на осі будуть по значенню відрізнятися рівно на COORD_GRADUATE[X]*Math.Pow(10, Y),
		///		де X та Y залежать від даних та значень інших констант.
		/// </summary>
		private static readonly int[] COORD_GRADUATE = { 1, 2, 5 };
		/// <summary>
		/// Шрифт яким буде підписано координатну сітку
		/// </summary>
		private static readonly Font FONT_COORDINATES = FontUtils.FontCreate("Verdana", 8);
		/// <summary>
		/// Pen яким буде намальовано лінії координатної сітки
		/// </summary>
		private static readonly Pen PEN_COORD_LINE = Pens.LightGray;
		/// <summary>
		/// Brush яким буде відмальовано підписи на осях
		/// </summary>
		private static readonly Brush BRUSH_COORD_LABELS = new SolidBrush(Color.Black);
		/// <summary>
		/// Підпис значень осі, коли всі значення рівні null.
		/// </summary>
		private const string AXIS_NULL_CAPTION = @"NULL";
		/// <summary>
		/// Символ, ширину в пікселях якого беруть за одиницю відстані між сусідніми підписами на осях
		/// </summary>
		private const string SPACE = " ";
		/// <summary>
		/// Мінімальна кількість символів <c>SPACE</c> які мають вміститися між кожною парою сусідніх підписів на осях
		/// </summary>
		private const float MIN_SPACES_BTWN_COORD_LABELS = 2f;
		/// <summary>
		/// Мінімальна відстань від центру кожного з елементів до країв скеттера (або осей).
		/// </summary>
		private const int DIST_TO_BOUND = 30;
		/// <summary>
		/// Мінімальна відстань між лініями сітки
		/// </summary>
		private const int DIST_BTWN_LINES = 50;
		/// <summary>
		/// Maximum relative error of values shown on coordinate axes from exact values in logarithmic scale.
		/// </summary>
		private const double MAX_DEVIATION_FROM_EXACT_VALUE = 0.01;
		/// <summary>
		/// Мінімальна відстань в пікселях між двома сусідніми підписами координатної сітки на одній з осей
		/// </summary>
		//private static readonly float MIN_DIST_BTWN_COORD_LABELS= CreateGraphics().MeasureString(SPACE, FONT_COORDINATES).Width * MIN_SPACES_BTWN_COORD_LABELS;
		/// <summary>
		/// Ширина в пікселях лінії що сполучає два сусідніх "кружка" в шляху елементу
		/// </summary>
		private const float WAY_LINE_WIDTH = 3;

		public const int XSLICE_DELTA = 12;
		public const int YSLICE_DELTA = 11;

		/// <summary>
		/// Обчислює з якого значення потрібно починати підписи та з яким кроком, а також в яких точках їх ставити для логарифмічної шкали.
		/// </summary>
		/// <param name="aGraphics">Graphics використовуючи який потрібно малювати</param>
		/// <param name="aWidth">Ширина або висота контрола (в залежності від осі для якої обчислюємо координати)</param>
		/// <param name="aMinM">Мінімальне значення міри яке потрібно відобразити</param>
		/// <param name="aMaxM">Максимальне значення міри яке потрібно відобразити</param>
		/// <param name="aSliceDelta">Відстань від осі до прилягаючого краю контрола (відстань на підписи)</param>
		/// <param name="aMaxSize">Розмір найбільшого з підписів в пікселях</param>
		/// <param name="aCoef">Відношення між кількістю точок на осі і різницею макс. та мін. значення мір</param>
		/// <param name="aMeasure">Міра що лежить на осі (потрібна для форматування)</param>
		/// <returns>Повертає список триплетів кожен з який представляє один підпис на осі. Значення триплету означають:
		/// перший елемент - відформатоване значення міри
		/// другий елемент - точка на осі до якої це значення відноситься
		/// третій елемент - розмір підпису значення міри в пікселях
		/// </returns>
		private static List<Triplet<string, int, SizeF>> LogarithmicCoordinatesPrecalculate(Graphics aGraphics, FormatRulesMeasures aMeasuresFormatRules, int aWidth, double aMinM, double aMaxM, int aSliceDelta, out SizeF aMaxSize, double aCoef, OlapMeasureObjectBase aMeasure)
		{
			aMaxSize = new SizeF(-1, -1);
			if (aMinM <= 0 && aMaxM >= 0)
				return null;
			// знаходимо кількість відрізків на рисунку (дійсне число)
			var marksCount = (aWidth - 2d * DIST_TO_BOUND - aSliceDelta) / DIST_BTWN_LINES;
			// знаходимо коефіцієнт геометричної прогресії
			var valueStep = Math.Pow(aMaxM / aMinM, 1d / marksCount);
			// знаходимо крок координатної сітки
			var coordinateStep = (aMaxM - aMinM) / marksCount;
			//знаходимо значення з якого потрібно починати робити підписи
			var currentTotalValue = Math.Pow(valueStep, -DIST_TO_BOUND / coordinateStep / aCoef) * aMinM;
			//створюємо та заповнюємо колекцію підписами
			var coords = new List<Triplet<string, int, SizeF>>();
			SizeF size;
			var currentPos = (int)(aSliceDelta + DIST_TO_BOUND + Math.Log(currentTotalValue / aMinM, valueStep) * coordinateStep * aCoef);  //позиція в пікселях початкового підпису
			while (currentPos < aWidth)
			{
				if (currentPos > aSliceDelta)   //якщо мітка за своєю координатою в пікселях більша за мінімальну можливу
				{
					int digitsAfterPoint;
					var textToPaint = aMeasuresFormatRules.MeasureValueToString(MostPrettyValueNearCurrentGet(currentTotalValue, out digitsAfterPoint), digitsAfterPoint >= 0 ? digitsAfterPoint : 0, aMeasure);  //форматоване значення підпису
					size = aGraphics.MeasureString(textToPaint, FONT_COORDINATES);  //взнаємо розмір підпису в пікселях
					coords.Add(new Triplet<string, int, SizeF>(textToPaint, currentPos, size)); //додаємо новий підпис в колекцію
																								//перевірки щоб знайти максимальний розмір підпису зі всіх доданих
					if (aMaxSize.Width < size.Width)
						aMaxSize.Width = size.Width;
					if (aMaxSize.Height < size.Height)
						aMaxSize.Height = size.Height;
				}
				//рухаємося до наступного підпису
				currentTotalValue *= valueStep;
				currentPos = (int)(aSliceDelta + DIST_TO_BOUND + Math.Log(currentTotalValue / aMinM, valueStep) * coordinateStep * aCoef);
			}
			return coords;
		}
		/// <summary>
		/// Обчислює для з якого значення потрібно починати підписи та з яким кроком, а також в який точках їх ставити.
		/// </summary>
		/// <param name="aGraphics">Graphics використовуючи який потрібно малювати</param>
		/// <param name="aMeasuresFormatRules"></param>
		/// <param name="aWidth">Ширина або висота контрола (в залежності від осі для якої обчислюємо координати)</param>
		/// <param name="aMinM">Мінімальне значення міри яке потрібно відобразити</param>
		/// <param name="aMaxM">Максимальне значення міри яке потрібно відобразити</param>
		/// <param name="aSliceDelta">Відстань від осі до прилягаючого краю контрола (відстань на підписи)</param>
		/// <param name="aMaxSize">Розмір найбільшого з підписів в пікселях</param>
		/// <param name="aCoef">Відношення між кількістю точок на осі і різницею макс. та мін. значення мір</param>
		/// <param name="aMeasure">Міра що лежить на осі (потрібна для форматування)</param>
		/// <returns>Повертає список триплетів кожен з який представляє один підпис на осі. Значення триплету означають:
		/// перший елемент - відформатоване значення міри
		/// другий елемент - точка на осі до якої це значення відноситься
		/// третій елемент - розмір підпису значення міри в пікселях
		/// </returns>
		private static List<Triplet<string, int, SizeF>> LinearCoordinatesPrecalculate(Graphics aGraphics, FormatRulesMeasures aMeasuresFormatRules, int aWidth, double aMinM, double aMaxM, int aSliceDelta, out SizeF aMaxSize, double aCoef, OlapMeasureObjectBase aMeasure)
		{
			var diffValue = (decimal)(aMaxM - aMinM);
			decimal maxCount = Math.Max(aWidth / DIST_BTWN_LINES, 1);   //максимальна к-ть позначок
			var b = diffValue / maxCount;
			int i, currentPow, pow10 = int.MaxValue, coordGrad = 1;
			//знаходимо крок підписів так, щоб їх було якомога більше, але так щоб не частіше ніж кожних DIST_BTWN_LINES пікселів
			for (i = 0; i < COORD_GRADUATE.Length; ++i)
			{
				currentPow = (int)Math.Ceiling(Math.Log10((double)(b / COORD_GRADUATE[i])));
				if (currentPow < pow10 && (COORD_GRADUATE[i] * Math.Pow(10, currentPow)) * aCoef > DIST_BTWN_LINES)
				{
					pow10 = currentPow;
					coordGrad = COORD_GRADUATE[i];
				}
			}
			aMaxSize = new SizeF(-1, -1);
			if (pow10 == int.MaxValue)  //якщо крок знайти не вдалося
				return null;
			var gradPow = pow10;
			var gradStep = coordGrad * Math.Pow(10, gradPow);    //крок підпису
																	//знаходимо значення з якого потрібно починати робити підписи
			var currentTotalValue = aMinM;
			var currentPos = (int)(aSliceDelta + DIST_TO_BOUND + (currentTotalValue - aMinM) * aCoef);
			if (currentPos > aSliceDelta)
				currentTotalValue -= (currentPos - aSliceDelta + 1) / aCoef;
			currentTotalValue = currentTotalValue - (currentTotalValue % gradStep);
			//створюємо та заповнюємо колекцію підписами
			var coords = new List<Triplet<string, int, SizeF>>();
			SizeF size;
			currentPos = (int)(aSliceDelta + DIST_TO_BOUND + (currentTotalValue - aMinM) * aCoef);  //позиція в пікселях початкового підпису
			while (currentPos < aWidth)
			{
				if (currentPos > aSliceDelta)   //якщо мітка за своєю координатою в пікселях більша за мінімальну можливу
				{
					int digitsAfterPoint;
					var textToPaint = aMeasuresFormatRules.MeasureValueToString(MostPrettyValueNearCurrentGet(currentTotalValue, out digitsAfterPoint), gradPow < 0 ? -gradPow : 0, aMeasure);    //форматоване значення підпису
					size = aGraphics.MeasureString(textToPaint, FONT_COORDINATES);  //взнаємо розмір підпису в пікселях
					coords.Add(new Triplet<string, int, SizeF>(textToPaint, currentPos, size)); //додаємо новий підпис в колекцію
																								//перевірки щоб знайти максимальний розмір підпису зі всіх доданих
					if (aMaxSize.Width < size.Width)
						aMaxSize.Width = size.Width;
					if (aMaxSize.Height < size.Height)
						aMaxSize.Height = size.Height;
				}
				//рухаємося до наступного підпису
				currentTotalValue += gradStep;
				currentPos = (int)(aSliceDelta + DIST_TO_BOUND + (currentTotalValue - aMinM) * aCoef);
			}
			return coords;
		}
		/// <summary>
		/// Returns the value which has the smallest number of valuable digits (digits not including 
		/// trailing zeroes and leading zeroes after decimal point) and has a relative deviation from 
		/// exact value <paramref name="aValue"/> not more than MAX_DEVIATION_FROM_EXACT_VALUE.
		/// </summary>
		/// <param name="aDigitsAfterPoint">The number of digits after the decimal point the resulting 
		/// number should be rounded to</param>
		/// <returns></returns>
		private static double MostPrettyValueNearCurrentGet(double aValue, out int aDigitsAfterPoint)
		{
			var sign = Math.Sign(aValue);
			double value = sign < 0 ? -aValue : aValue,
				   left = value * (1 - MAX_DEVIATION_FROM_EXACT_VALUE),
				   right = value * (1 + MAX_DEVIATION_FROM_EXACT_VALUE),
				   st10 = 1,
				   number = 0,
				   rem = value;
			aDigitsAfterPoint = 0;
			while (st10 * 10 <= value)
			{
				st10 *= 10;
				--aDigitsAfterPoint;
			}
			while (st10 > value)
			{
				st10 /= 10;
				++aDigitsAfterPoint;
			}
			for (var i = 0; i < 50; ++i)
			{
				var digit = Math.Floor(rem / st10);
				number += digit * st10;
				rem -= digit * st10;
				double lower = number,
					   upper = number + st10;
				if (lower >= left && lower <= right)
					if (upper >= left && upper <= right)
						return Math.Abs(value - lower) < Math.Abs(value - upper) ? lower * sign : upper * sign;
					else
						return lower * sign;
				if (upper >= left && upper <= right)
					return upper * sign;
				st10 /= 10;
				++aDigitsAfterPoint;
			}
			return aValue;
		}

		/// <summary>
		/// Відмальовує координатну сітку.
		/// </summary>
		/// <param name="aGraphics">Graphics використовуючи який потрібно малювати координатну сітку</param>
		/// <param name="aWidth">Ширина скеттер контрола</param>
		/// <param name="aHeight">Висота скеттер контрола</param>
		public static bool CoordinatesDraw(this Graphics aGraphics,
			FormatRulesMeasures aMeasuresFormatRules,
			int aWidth,
			int aHeight,
			double aXMax,
			double aXMin,
			double aYMax,
			double aYMin,
			bool XAxisExists,
			bool YAxisExists,
			OlapMeasureObjectBase aXMeasure,
			OlapMeasureObjectBase aYMeasure,
			bool aXLogarithmicScale,
			bool aYLogarithmicScale,
			bool aYAxisLabelsVertical)
		{
			Tracer.EnterMethod("OlapScatter.CoordinatesDraw()");

			var MIN_DIST_BTWN_COORD_LABELS = aGraphics.MeasureString(SPACE, FONT_COORDINATES).Width * MIN_SPACES_BTWN_COORD_LABELS;

			//var xMax = aData.Max.MX.NumericValue;
			//var xMin = aData.Min.MX.NumericValue;
			//var yMax = aData.Max.MY.NumericValue;
			//var yMin = aData.Min.MY.NumericValue;

			var aXCoef = aXMax == aXMin ? 0 : (aWidth - YSLICE_DELTA - 2 * DIST_TO_BOUND) / (aXMax - aXMin);
			var aYCoef = aYMax == aYMin ? 0 : (aHeight - XSLICE_DELTA - 2 * DIST_TO_BOUND) / (aYMax - aYMin);

			Array.Sort(COORD_GRADUATE); //assume that coordinates steps are sorted
			if (COORD_GRADUATE[0] < 1)
				throw new ExceptionFramework("Coordinates steps must be natural numbers", ExceptionKind.ekDeveloper);
			if (COORD_GRADUATE[COORD_GRADUATE.Length - 1] >= 10)
				throw new ExceptionFramework("Each coordinate step must be less than 10", ExceptionKind.ekDeveloper);
			SizeF maxSize;
			var clipRect = new Rectangle(YSLICE_DELTA, 0, aWidth - YSLICE_DELTA - 1, aHeight - XSLICE_DELTA);
			//Draw X axis
			var coords = aXLogarithmicScale
					   ? LogarithmicCoordinatesPrecalculate(aGraphics, aMeasuresFormatRules, aWidth, aXMin, aXMax, YSLICE_DELTA, out maxSize, aXCoef, aXMeasure)//aData.Measures[0])
					   : LinearCoordinatesPrecalculate(aGraphics, aMeasuresFormatRules, aWidth, aXMin, aXMax, YSLICE_DELTA, out maxSize, aXCoef, aXMeasure);
			if (coords != null)
			{
				var printEvery = 1; //якщо підписи занадто довгі, то потрібно підписувати кожну "printEvery" позначку для того щоб підписи не наклалися
				if (coords.Count > 1)
					printEvery = (int)Math.Ceiling((maxSize.Width + MIN_DIST_BTWN_COORD_LABELS / 2) / (coords[1].Second - coords[0].Second));
				//проходимось по всіх позначках на осі
				for (var i = 0; i < coords.Count; ++i)
				{
					aGraphics.DrawLine(PEN_COORD_LINE, coords[i].Second, 0, coords[i].Second, aHeight - XSLICE_DELTA);
					if (XAxisExists)
					{
						if (i % printEvery == 0)    //якщо потрібно підписати позначку
						{
							var x1 = coords[i].Second - coords[i].Third.Width / 2;  //абсциса початку підпису
							var x2 = x1 + coords[i].Third.Width;
							if (i == 0 && x1 < clipRect.X)  //якщо це перший підпис і він не вміщається
							{
								//пробуєм зсунути вправо
								x2 += clipRect.X - x1;
								x1 = clipRect.X;
								var nextLabel = i + printEvery;
								if (nextLabel < coords.Count
									&& coords[nextLabel].Second - coords[nextLabel].Third.Width / 2 < x2 + MIN_DIST_BTWN_COORD_LABELS)  //якщо після зміщення на потрібну кількість пікселів надпис налазить на наступний
									continue;
							}
							else if (x2 > clipRect.Right)   //якщо надпис вилазить за межі контрола
							{
								//пробуєм зсунути вліво
								x1 -= x2 - clipRect.Right;
								var prevLabel = i - printEvery;
								if (prevLabel >= 0
									&& coords[prevLabel].Second + coords[prevLabel].Third.Width / 2 > x1 - MIN_DIST_BTWN_COORD_LABELS)  //якщо після зсуву на потрібну кількість пікселів надпис налазить на попередній
									continue;
							}
							//малюємо підпис
							aGraphics.DrawString(coords[i].First, FONT_COORDINATES, BRUSH_COORD_LABELS, x1, aHeight - XSLICE_DELTA);
						}
					}
					else if (i == coords.Count / 2)
					{
						var x1 = coords[i].Second - aGraphics.MeasureString(AXIS_NULL_CAPTION, FONT_COORDINATES).Width / 2; //абсциса початку підпису
																															//малюємо підпис
						aGraphics.DrawString(AXIS_NULL_CAPTION, FONT_COORDINATES, BRUSH_COORD_LABELS, x1, aHeight - XSLICE_DELTA);
					}
				}
			}
			//draw Y axis
			coords = aYLogarithmicScale
				? LogarithmicCoordinatesPrecalculate(aGraphics, aMeasuresFormatRules, aHeight, aYMin, aYMax, XSLICE_DELTA, out maxSize, aYCoef, aYMeasure)
				: LinearCoordinatesPrecalculate(aGraphics, aMeasuresFormatRules, aHeight, aYMin, aYMax, XSLICE_DELTA, out maxSize, aYCoef, aYMeasure);
			var h = FONT_COORDINATES.GetHeight() / 2;
			//перевіряємо чи потрібно писати мітки горизонтально чи вертикально
			if (aYAxisLabelsVertical != YSLICE_DELTA < maxSize.Width - FONT_COORDINATES.Size / 4)
				aYAxisLabelsVertical = !aYAxisLabelsVertical;
			//aHintBoundsUpdate();
			if (coords != null)
			{
				var printEvery = 1; //якщо підписи занадто довгі, то потрібно підписувати кожну "printEvery" позначку для того щоб підписи не наклалися
				if (coords.Count > 1)
					printEvery = (int)Math.Ceiling((maxSize.Width + MIN_DIST_BTWN_COORD_LABELS / 2) / (coords[1].Second - coords[0].Second));
				float y1, y2;
				for (var i = 0; i < coords.Count; ++i)
				{
					coords[i].Second = aHeight - coords[i].Second;
					aGraphics.DrawLine(PEN_COORD_LINE, YSLICE_DELTA, coords[i].Second, aWidth, coords[i].Second);
					if (YAxisExists)
					{
						if (i % printEvery == 0)    //якщо потрібно підписаи позначку
						{
							var mid = (aYAxisLabelsVertical ? coords[i].Third.Width : coords[i].Third.Height) / 2;
							y1 = coords[i].Second + mid;
							y2 = y1 - mid * 2;
							if (y1 > clipRect.Bottom)   //якщо надпис "вилазить" за нижню межу
							{
								//зміщаємо надпис вверх
								y2 -= y1 - clipRect.Bottom;
								y1 = clipRect.Bottom;
								var nextLabel = i + printEvery;
								if (nextLabel < coords.Count && coords[nextLabel].Second + mid > y2 + MIN_DIST_BTWN_COORD_LABELS)   //якщо після зміщення надпис перекриється з іншим
									continue;
							}
							else if (y2 < clipRect.Y)   //якщо надпис "вилазить" за верхню межу
							{
								//зміщаємо надпис вниз
								y1 += clipRect.Y - y2;
								y2 = clipRect.Y;
								var prevLabel = i - printEvery;
								if (prevLabel >= 0 && coords[prevLabel].Second - mid < y1 + MIN_DIST_BTWN_COORD_LABELS) //якщо після зміщення надпис перекриється з іншим
									continue;
							}
							//пишемо надпис
							if (aYAxisLabelsVertical)   //якщо надписи потрібно писати вертикально
							{
								//змінюємо матрицю трансформації так, щоб написати надпис горизонтально а він став вертикально в потрібному місці
								var m = new Matrix();
								m.RotateAt(-90, new PointF(YSLICE_DELTA - h, (y1 + y2) / 2));
								aGraphics.Transform = m;
								aGraphics.DrawString(coords[i].First, FONT_COORDINATES, BRUSH_COORD_LABELS
													 , YSLICE_DELTA - h - coords[i].Third.Width / 2
													 , (y1 + y2) / 2 - h);
								aGraphics.Transform = new Matrix();
							}
							else    //якщо надписи потрібно малювати горизонтально
								aGraphics.DrawString(coords[i].First, FONT_COORDINATES, BRUSH_COORD_LABELS
													 , YSLICE_DELTA - coords[i].Third.Width
													 , (y1 + y2) / 2 - h);

						}
					}
					else if (i == coords.Count / 2)
					{
						var size = aGraphics.MeasureString(AXIS_NULL_CAPTION, FONT_COORDINATES);
						float width = size.Width,
							  height = size.Height,
							  mid = (aYAxisLabelsVertical ? width : height) / 2;
						y1 = coords[i].Second + mid;
						y2 = y1 - mid * 2;
						if (aYAxisLabelsVertical)
						{
							//змінюємо матрицю трансформації так, щоб написати надпис горизонтально а він став вертикально в потрібному місці
							var m = new Matrix();
							m.RotateAt(-90, new PointF(YSLICE_DELTA - h, (y1 + y2) / 2));
							aGraphics.Transform = m;
							aGraphics.DrawString(AXIS_NULL_CAPTION, FONT_COORDINATES, BRUSH_COORD_LABELS, YSLICE_DELTA - h - mid, (y1 + y2) / 2 - h);
							aGraphics.Transform = new Matrix();
						}
						else    //якщо надписи потрібно малювати горизонтально
							aGraphics.DrawString(AXIS_NULL_CAPTION, FONT_COORDINATES, BRUSH_COORD_LABELS, YSLICE_DELTA - width, (y1 + y2) / 2 - h);
					}
				}
			}
			//малюємо чорний обмежуючий прямокутник
			aGraphics.DrawRectangle(Pens.Black, clipRect);
			Tracer.ExitMethod("OlapScatter.CoordinatesDraw()");
			return aYAxisLabelsVertical;
		}
		/// <summary>
		/// Встановлюємо потрібну якість відмальовки для <paramref name="aGraphics"/>
		/// </summary>
		public static Graphics HighQualitySet(this Graphics aGraphics)
		{
			aGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			aGraphics.CompositingQuality = CompositingQuality.HighQuality;
			aGraphics.SmoothingMode = SmoothingMode.AntiAlias;
			return aGraphics;
		}
		public static void DrawRectangle(this Graphics graphics, Pen pen, RectangleF rec)
		{
			graphics.DrawRectangle(pen, rec.X, rec.Y, rec.Width, rec.Height);
		}
		public static void DrawArrowTo(this Graphics graphics, Pen pen, RectangleF from, RectangleF to)
		{
			var x1 = from.X;
			var y1 = from.Y;
			var x2 = to.X;
			var y2 = to.Y;
			var s = Math.Min(to.Size.Height, to.Size.Width);
			var d = (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
			if (d > 0) //якщо центри не співпадають
			{
				//вираховуємо точку на другому кружку куди потрібно показати стрілкою
				var r = s / 2;
				var y = r * (y2 - y1) / d;
				var x = r * (x2 - x1) / d;
				x2 -= x;
				y2 -= y;
				//pen.Color = ItemHintHighlightColorGet(_Data.Pages[i][aItemID]);
				if (Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)) < WAY_LINE_WIDTH)
					pen.EndCap = LineCap.Flat;
				else if (!SystemUtils.IsUnderWine)
					pen.CustomEndCap = new AdjustableArrowCap(WAY_LINE_WIDTH, WAY_LINE_WIDTH, true);
				else
					pen.EndCap = LineCap.Flat;
				graphics.DrawLine(pen, x1, y1, x2, y2);
			}
		}
		public static void DrawStatisticString(this Graphics aGraphics, int actual, int expected)
		{
			using (var font = FontUtils.FontCreate("Verdana", 8))
			using (var brush = new SolidBrush(Color.FromArgb(100, Color.Black)))
				aGraphics.DrawString(string.Format("{0}({1})", actual, expected), font, brush, XSLICE_DELTA + 2, 3);
		}
	}
}
