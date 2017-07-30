﻿
using System;

namespace GMap.NET.MapProviders
{
	/// <summary>
	/// CzechHistoryMap provider, http://www.mapy.cz/
	/// </summary>
	public class CzechHistoryMapProviderOld : CzechMapProviderBaseOld
	{
		public static readonly CzechHistoryMapProviderOld Instance;

		private CzechHistoryMapProviderOld()
		{
		}

		static CzechHistoryMapProviderOld()
		{
			Instance = new CzechHistoryMapProviderOld();
		}

		#region GMapProvider Members

		private readonly Guid id = new Guid("C666AAF4-9D27-418F-97CB-7F0D8CC44544");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "CzechHistoryOldMap";

		public override string Name
		{
			get { return name; }
		}

		private GMapProvider[] overlays;

		public override GMapProvider[] Overlays
		{
			get
			{
				if (overlays == null)
				{
					overlays = new GMapProvider[] {this, CzechHybridMapProviderOld.Instance};
				}
				return overlays;
			}
		}

		public override PureImage GetTileImage(GPoint pos, int zoom)
		{
			var url = MakeTileImageUrl(pos, zoom, LanguageStr);

			return GetTileImageUsingHttp(url);
		}

		#endregion

		private string MakeTileImageUrl(GPoint pos, int zoom, string language)
		{
			// http://m4.mapserver.mapy.cz/army2/9_7d00000_8080000

			var xx = pos.X << (28 - zoom);
			var yy = ((((long) Math.Pow(2.0, zoom)) - 1) - pos.Y) << (28 - zoom);

			return string.Format(UrlFormat, GetServerNum(pos, 3) + 1, zoom, xx, yy);
		}

		private static readonly string UrlFormat = "http://m{0}.mapserver.mapy.cz/army2/{1}_{2:x7}_{3:x7}";
	}
}