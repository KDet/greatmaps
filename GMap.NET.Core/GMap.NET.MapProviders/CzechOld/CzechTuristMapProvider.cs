﻿
using System;

namespace GMap.NET.MapProviders
{
	/// <summary>
	/// CzechTuristMap provider, http://www.mapy.cz/
	/// </summary>
	public class CzechTuristMapProviderOld : CzechMapProviderBaseOld
	{
		public static readonly CzechTuristMapProviderOld Instance;

		private CzechTuristMapProviderOld()
		{
		}

		static CzechTuristMapProviderOld()
		{
			Instance = new CzechTuristMapProviderOld();
		}

		#region GMapProvider Members

		private readonly Guid id = new Guid("B923C81D-880C-42EB-88AB-AF8FE42B564D");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "CzechTuristOldMap";

		public override string Name
		{
			get { return name; }
		}

		public override PureImage GetTileImage(GPoint pos, int zoom)
		{
			var url = MakeTileImageUrl(pos, zoom, LanguageStr);

			return GetTileImageUsingHttp(url);
		}

		#endregion

		private string MakeTileImageUrl(GPoint pos, int zoom, string language)
		{
			// http://m1.mapserver.mapy.cz/turist/3_8000000_8000000

			var xx = pos.X << (28 - zoom);
			var yy = ((((long) Math.Pow(2.0, zoom)) - 1) - pos.Y) << (28 - zoom);

			return string.Format(UrlFormat, GetServerNum(pos, 3) + 1, zoom, xx, yy);
		}

		private static readonly string UrlFormat = "http://m{0}.mapserver.mapy.cz/turist/{1}_{2:x7}_{3:x7}";
	}
}