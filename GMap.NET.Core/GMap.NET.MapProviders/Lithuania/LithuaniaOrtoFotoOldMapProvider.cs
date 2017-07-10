
using System;

namespace GMap.NET.MapProviders
{
	/// <summary>
	/// LithuaniaOrtoFotoNewMap, from 2010 data, provider
	/// </summary>
	public class LithuaniaOrtoFotoOldMapProvider : LithuaniaMapProviderBase
	{
		public static readonly LithuaniaOrtoFotoOldMapProvider Instance;

		private LithuaniaOrtoFotoOldMapProvider()
		{
		}

		static LithuaniaOrtoFotoOldMapProvider()
		{
			Instance = new LithuaniaOrtoFotoOldMapProvider();
		}

		#region GMapProvider Members

		private readonly Guid id = new Guid("C37A148E-0A7D-4123-BE4E-D0D3603BE46B");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "LithuaniaOrtoFotoMapOld";

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
			// http://dc1.maps.lt/cache/mapslt_ortofoto_2010/map/_alllayers/L09/R000016b1/C000020e2.jpg

			return string.Format(UrlFormat, zoom, pos.Y, pos.X);
		}

		private static readonly string UrlFormat =
			"http://dc1.maps.lt/cache/mapslt_ortofoto_2010/map/_alllayers/L{0:00}/R{1:x8}/C{2:x8}.jpg";
	}
}