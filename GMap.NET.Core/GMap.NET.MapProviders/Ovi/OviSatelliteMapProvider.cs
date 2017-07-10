﻿
using System;

namespace GMap.NET.MapProviders
{
	/// <summary>
	/// OviSatelliteMap provider
	/// </summary>
	public class OviSatelliteMapProvider : OviMapProviderBase
	{
		public static readonly OviSatelliteMapProvider Instance;

		private OviSatelliteMapProvider()
		{
		}

		static OviSatelliteMapProvider()
		{
			Instance = new OviSatelliteMapProvider();
		}

		#region GMapProvider Members

		private readonly Guid id = new Guid("6696CE12-7694-4073-BC48-79EE849F2563");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "OviSatelliteMap";

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
			// http://b.maptile.maps.svc.ovi.com/maptiler/v2/maptile/newest/satellite.day/12/2313/1275/256/png8

			return string.Format(UrlFormat, UrlServerLetters[GetServerNum(pos, 4)], zoom, pos.X, pos.Y);
		}

		private static readonly string UrlFormat =
			"http://{0}.maptile.maps.svc.ovi.com/maptiler/v2/maptile/newest/satellite.day/{1}/{2}/{3}/256/png8";
	}
}