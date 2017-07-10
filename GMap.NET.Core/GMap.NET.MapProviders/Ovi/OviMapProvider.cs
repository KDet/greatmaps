
using System;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders
{
	public abstract class OviMapProviderBase : GMapProvider
	{
		public OviMapProviderBase()
		{
			MaxZoom = null;
			RefererUrl = "http://maps.ovi.com/";
			Copyright = string.Format("©{0} OVI Nokia - Map data ©{0} NAVTEQ, Imagery ©{0} DigitalGlobe", DateTime.Today.Year);
		}

		#region GMapProvider Members

		public override Guid Id
		{
			get { throw new NotImplementedException(); }
		}

		public override string Name
		{
			get { throw new NotImplementedException(); }
		}

		public override PureProjection Projection
		{
			get { return MercatorProjection.Instance; }
		}

		private GMapProvider[] overlays;

		public override GMapProvider[] Overlays
		{
			get
			{
				if (overlays == null)
				{
					overlays = new GMapProvider[] {this};
				}
				return overlays;
			}
		}

		public override PureImage GetTileImage(GPoint pos, int zoom)
		{
			throw new NotImplementedException();
		}

		#endregion

		protected static readonly string UrlServerLetters = "bcde";
	}

	/// <summary>
	/// OviMap provider
	/// </summary>
	public class OviMapProvider : OviMapProviderBase
	{
		public static readonly OviMapProvider Instance;

		private OviMapProvider()
		{
		}

		static OviMapProvider()
		{
			Instance = new OviMapProvider();
		}

		#region GMapProvider Members

		private readonly Guid id = new Guid("30DC1083-AC4D-4471-A232-D8A67AC9373A");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "OviMap";

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
			// http://c.maptile.maps.svc.ovi.com/maptiler/v2/maptile/newest/normal.day/12/2321/1276/256/png8

			return string.Format(UrlFormat, UrlServerLetters[GetServerNum(pos, 4)], zoom, pos.X, pos.Y);
		}

		private static readonly string UrlFormat =
			"http://{0}.maptile.maps.svc.ovi.com/maptiler/v2/maptile/newest/normal.day/{1}/{2}/{3}/256/png8";
	}
}