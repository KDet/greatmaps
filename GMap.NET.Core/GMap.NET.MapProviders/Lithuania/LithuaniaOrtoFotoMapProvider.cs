﻿
using System;

namespace GMap.NET.MapProviders
{
	/// <summary>
	/// LithuaniaOrtoFotoMap provider
	/// </summary>
	public class LithuaniaOrtoFotoMapProvider : LithuaniaMapProviderBase
	{
		public static readonly LithuaniaOrtoFotoMapProvider Instance;

		private LithuaniaOrtoFotoMapProvider()
		{
		}

		static LithuaniaOrtoFotoMapProvider()
		{
			Instance = new LithuaniaOrtoFotoMapProvider();
		}

		#region GMapProvider Members

		private readonly Guid id = new Guid("043FF9EF-612C-411F-943C-32C787A88D6A");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "LithuaniaOrtoFotoMap";

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
			// http://dc5.maps.lt/cache/mapslt_ortofoto/map/_alllayers/L08/R00000914/C00000d28.jpg

			return string.Format(UrlFormat, zoom, pos.Y, pos.X);
		}

		private static readonly string UrlFormat =
			"http://dc5.maps.lt/cache/mapslt_ortofoto/map/_alllayers/L{0:00}/R{1:x8}/C{2:x8}.jpg";
	}
}