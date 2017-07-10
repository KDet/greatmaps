﻿
using System;

namespace GMap.NET.MapProviders
{
	/// <summary>
	/// GoogleChinaSatelliteMap provider
	/// </summary>
	public class GoogleChinaSatelliteMapProvider : GoogleMapProviderBase
	{
		public static readonly GoogleChinaSatelliteMapProvider Instance;

		private GoogleChinaSatelliteMapProvider()
		{
			RefererUrl = string.Format("http://ditu.{0}/", ServerChina);
		}

		static GoogleChinaSatelliteMapProvider()
		{
			Instance = new GoogleChinaSatelliteMapProvider();
		}

		public string Version = "s@170";

		#region GMapProvider Members

		private readonly Guid id = new Guid("543009AC-3379-4893-B580-DBE6372B1753");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "GoogleChinaSatelliteMap";

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
			var sec1 = string.Empty; // after &x=...
			var sec2 = string.Empty; // after &zoom=...
			GetSecureWords(pos, out sec1, out sec2);

			return string.Format(UrlFormat, UrlFormatServer, GetServerNum(pos, 4), UrlFormatRequest, Version, pos.X, sec1, pos.Y,
				zoom, sec2, ServerChina);
		}

		private static readonly string UrlFormatServer = "mt";
		private static readonly string UrlFormatRequest = "vt";
		private static readonly string UrlFormat = "http://{0}{1}.{9}/{2}/lyrs={3}&gl=cn&x={4}{5}&y={6}&z={7}&s={8}";
	}
}