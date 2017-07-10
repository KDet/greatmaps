
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Forms.Properties;

namespace GMap.NET.WindowsForms.Markers
{
#if !PocketPC

#else
   using GMap.NET.WindowsMobile.Properties;
#endif

	public enum GMarkerGoogleType
	{
		None = 0,
		Arrow,
		Blue,
		BlueSmall,
		BlueDot,
		BluePushpin,
		BrownSmall,
		GraySmall,
		Green,
		GreenSmall,
		GreenDot,
		GreenPushpin,
		GreenBigGo,
		Yellow,
		YellowSmall,
		YellowDot,
		YellowBigPause,
		YellowPushpin,
		Lightblue,
		LightblueDot,
		LightbluePushpin,
		Orange,
		OrangeSmall,
		OrangeDot,
		Pink,
		PinkDot,
		PinkPushpin,
		Purple,
		PurpleSmall,
		PurpleDot,
		PurplePushpin,
		Red,
		RedSmall,
		RedDot,
		RedPushpin,
		RedBigStop,
		BlackSmall,
		WhiteSmall
	}

#if !PocketPC
	[Serializable]
	public class GMarkerGoogle : GMapMarker, ISerializable, IDeserializationCallback
#else
   public class GMarkerGoogle : GMapMarker
#endif
	{
		private Bitmap _bitmap;
		private Bitmap _bitmapShadow;

		private static Bitmap _arrowshadow;
		private static Bitmap _msmarkerShadow;
		private static Bitmap _shadowSmall;
		private static Bitmap _pushpinShadow;

		public readonly GMarkerGoogleType Type;

		public GMarkerGoogle(PointLatLng p, GMarkerGoogleType type)
			: base(p)
		{
			Type = type;

			if (type != GMarkerGoogleType.None)
			{
				LoadBitmap();
			}
		}

		private void LoadBitmap()
		{
			_bitmap = GetIcon(Type.ToString());
			Size = new Size(_bitmap.Width, _bitmap.Height);

			switch (Type)
			{
				case GMarkerGoogleType.Arrow:
				{
					Offset = new Point(-11, -Size.Height);

					if (_arrowshadow == null)
					{
						_arrowshadow = Resources.arrowshadow;
					}
					_bitmapShadow = _arrowshadow;
				}
					break;

				case GMarkerGoogleType.Blue:
				case GMarkerGoogleType.BlueDot:
				case GMarkerGoogleType.Green:
				case GMarkerGoogleType.GreenDot:
				case GMarkerGoogleType.Yellow:
				case GMarkerGoogleType.YellowDot:
				case GMarkerGoogleType.Lightblue:
				case GMarkerGoogleType.LightblueDot:
				case GMarkerGoogleType.Orange:
				case GMarkerGoogleType.OrangeDot:
				case GMarkerGoogleType.Pink:
				case GMarkerGoogleType.PinkDot:
				case GMarkerGoogleType.Purple:
				case GMarkerGoogleType.PurpleDot:
				case GMarkerGoogleType.Red:
				case GMarkerGoogleType.RedDot:
				{
					Offset = new Point(-Size.Width/2 + 1, -Size.Height + 1);

					if (_msmarkerShadow == null)
					{
						_msmarkerShadow = Resources.msmarker_shadow;
					}
					_bitmapShadow = _msmarkerShadow;
				}
					break;

				case GMarkerGoogleType.BlackSmall:
				case GMarkerGoogleType.BlueSmall:
				case GMarkerGoogleType.BrownSmall:
				case GMarkerGoogleType.GraySmall:
				case GMarkerGoogleType.GreenSmall:
				case GMarkerGoogleType.YellowSmall:
				case GMarkerGoogleType.OrangeSmall:
				case GMarkerGoogleType.PurpleSmall:
				case GMarkerGoogleType.RedSmall:
				case GMarkerGoogleType.WhiteSmall:
				{
					Offset = new Point(-Size.Width/2, -Size.Height + 1);

					if (_shadowSmall == null)
					{
						_shadowSmall = Resources.shadow_small;
					}
					_bitmapShadow = _shadowSmall;
				}
					break;

				case GMarkerGoogleType.GreenBigGo:
				case GMarkerGoogleType.YellowBigPause:
				case GMarkerGoogleType.RedBigStop:
				{
					Offset = new Point(-Size.Width/2, -Size.Height + 1);
					if (_msmarkerShadow == null)
					{
						_msmarkerShadow = Resources.msmarker_shadow;
					}
					_bitmapShadow = _msmarkerShadow;
				}
					break;

				case GMarkerGoogleType.BluePushpin:
				case GMarkerGoogleType.GreenPushpin:
				case GMarkerGoogleType.YellowPushpin:
				case GMarkerGoogleType.LightbluePushpin:
				case GMarkerGoogleType.PinkPushpin:
				case GMarkerGoogleType.PurplePushpin:
				case GMarkerGoogleType.RedPushpin:
				{
					Offset = new Point(-9, -Size.Height + 1);

					if (_pushpinShadow == null)
					{
						_pushpinShadow = Resources.pushpin_shadow;
					}
					_bitmapShadow = _pushpinShadow;
				}
					break;
			}
		}

		/// <summary>
		/// marker using manual bitmap, NonSerialized
		/// </summary>
		/// <param name="p"></param>
		/// <param name="bitmap"></param>
		public GMarkerGoogle(PointLatLng p, Bitmap bitmap)
			: base(p)
		{
			_bitmap = bitmap;
			Size = new Size(bitmap.Width, bitmap.Height);
			Offset = new Point(-Size.Width/2, -Size.Height);
		}

		private static readonly Dictionary<string, Bitmap> IconCache = new Dictionary<string, Bitmap>();

		internal static Bitmap GetIcon(string name)
		{
			Bitmap ret;
			if (!IconCache.TryGetValue(name, out ret))
			{
				ret = Resources.ResourceManager.GetObject(name, Resources.Culture) as Bitmap;
				IconCache.Add(name, ret);
			}
			return ret;
		}


		public override void OnRender(Graphics g)
		{
#if !PocketPC
			if (_bitmapShadow != null)
			{
				g.DrawImage(_bitmapShadow, LocalPosition.X, LocalPosition.Y, _bitmapShadow.Width, _bitmapShadow.Height);
			}
			g.DrawImage(_bitmap, LocalPosition.X, LocalPosition.Y, Size.Width, Size.Height);

			//g.DrawString(LocalPosition.ToString(), SystemFonts.DefaultFont, Brushes.Red, LocalPosition);
#else
         if(BitmapShadow != null)
         {
            DrawImageUnscaled(g, BitmapShadow, LocalPosition.X, LocalPosition.Y);
         }
         DrawImageUnscaled(g, Bitmap, LocalPosition.X, LocalPosition.Y);
#endif
		}

		public override void Dispose()
		{
			if (_bitmap != null)
			{
				if (!IconCache.ContainsValue(_bitmap))
				{
					_bitmap.Dispose();
					_bitmap = null;
				}
			}

			base.Dispose();
		}

#if !PocketPC

		#region ISerializable Members

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("type", Type);
			//info.AddValue("Bearing", this.Bearing);

			base.GetObjectData(info, context);
		}

		protected GMarkerGoogle(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Type = Extensions.GetStruct(info, "type", GMarkerGoogleType.None);
			//this.Bearing = Extensions.GetStruct<float>(info, "Bearing", null);
		}

		#endregion

		#region IDeserializationCallback Members

		public void OnDeserialization(object sender)
		{
			if (Type != GMarkerGoogleType.None)
			{
				LoadBitmap();
			}
		}

		#endregion

#endif
	}
}