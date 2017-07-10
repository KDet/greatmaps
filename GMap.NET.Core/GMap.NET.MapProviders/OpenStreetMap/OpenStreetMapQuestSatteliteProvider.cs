
using System;

namespace GMap.NET.MapProviders
{
	/// <summary>
	/// OpenStreetMapQuestSattelite provider - http://wiki.openstreetmap.org/wiki/MapQuest
	/// </summary>
	public class OpenStreetMapQuestSatteliteProvider : OpenStreetMapProviderBase
	{
		public static readonly OpenStreetMapQuestSatteliteProvider Instance;

		private OpenStreetMapQuestSatteliteProvider()
		{
			Copyright = string.Format("© MapQuest - Map data ©{0} MapQuest, OpenStreetMap", DateTime.Today.Year);
		}

		static OpenStreetMapQuestSatteliteProvider()
		{
			Instance = new OpenStreetMapQuestSatteliteProvider();
		}

		#region GMapProvider Members

		private readonly Guid id = new Guid("E590D3B1-37F4-442B-9395-ADB035627F67");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "OpenStreetMapQuestSattelite";

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
					overlays = new GMapProvider[] {this};
				}
				return overlays;
			}
		}

		public override PureImage GetTileImage(GPoint pos, int zoom)
		{
			var url = MakeTileImageUrl(pos, zoom, string.Empty);

			return GetTileImageUsingHttp(url);
		}

		#endregion

		private string MakeTileImageUrl(GPoint pos, int zoom, string language)
		{
			return string.Format(UrlFormat, GetServerNum(pos, 3) + 1, zoom, pos.X, pos.Y);
		}

		private static readonly string UrlFormat = "http://otile{0}.mqcdn.com/tiles/1.0.0/sat/{1}/{2}/{3}.jpg";
	}
}