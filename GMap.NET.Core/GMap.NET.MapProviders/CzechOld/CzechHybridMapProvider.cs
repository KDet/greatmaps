﻿
using System;

namespace GMap.NET.MapProviders
{
	/// <summary>
	/// CzechHybridMap provider, http://www.mapy.cz/
	/// </summary>
	public class CzechHybridMapProviderOld : CzechMapProviderBaseOld
	{
		public static readonly CzechHybridMapProviderOld Instance;

		private CzechHybridMapProviderOld()
		{
		}

		static CzechHybridMapProviderOld()
		{
			Instance = new CzechHybridMapProviderOld();
		}

		#region GMapProvider Members

		private readonly Guid id = new Guid("F785D98E-DD1D-46FD-8BC1-1AAB69604980");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "CzechHybridOldMap";

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
					overlays = new GMapProvider[] {CzechSatelliteMapProviderOld.Instance, this};
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
			// http://m2.mapserver.mapy.cz/hybrid/9_7d00000_7b80000

			var xx = pos.X << (28 - zoom);
			var yy = ((((long) Math.Pow(2.0, zoom)) - 1) - pos.Y) << (28 - zoom);

			return string.Format(UrlFormat, GetServerNum(pos, 3) + 1, zoom, xx, yy);
		}

		private static readonly string UrlFormat = "http://m{0}.mapserver.mapy.cz/hybrid/{1}_{2:x7}_{3:x7}";
	}
}