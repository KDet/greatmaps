
using System;

namespace GMap.NET.MapProviders
{
	/// <summary>
	/// OviHybridMap provider
	/// </summary>
	public class OviHybridMapProvider : OviMapProviderBase
	{
		public static readonly OviHybridMapProvider Instance;

		private OviHybridMapProvider()
		{
		}

		static OviHybridMapProvider()
		{
			Instance = new OviHybridMapProvider();
		}

		#region GMapProvider Members

		private readonly Guid id = new Guid("B85A8FD2-40F4-40EE-9B45-491AA45D86C1");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "OviHybridMap";

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
			// http://c.maptile.maps.svc.ovi.com/maptiler/v2/maptile/newest/hybrid.day/12/2316/1277/256/png8

			return string.Format(UrlFormat, UrlServerLetters[GetServerNum(pos, 4)], zoom, pos.X, pos.Y);
		}

		private const string UrlFormat =
			"http://{0}.maptile.maps.svc.ovi.com/maptiler/v2/maptile/newest/hybrid.day/{1}/{2}/{3}/256/png8";
	}
}