
using System;

namespace GMap.NET.MapProviders
{
	/// <summary>
	/// OpenCycleMap Landscape provider - http://www.opencyclemap.org
	/// </summary>
	public class OpenCycleLandscapeMapProvider : OpenStreetMapProviderBase
	{
		public static readonly OpenCycleLandscapeMapProvider Instance;

		private OpenCycleLandscapeMapProvider()
		{
			RefererUrl = "http://www.opencyclemap.org/";
		}

		static OpenCycleLandscapeMapProvider()
		{
			Instance = new OpenCycleLandscapeMapProvider();
		}

		#region GMapProvider Members

		private readonly Guid id = new Guid("BDBAA939-6597-4D87-8F4F-261C49E35F56");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "OpenCycleLandscapeMap";

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
			var letter = ServerLetters[GetServerNum(pos, 3)];
			return string.Format(UrlFormat, letter, zoom, pos.X, pos.Y);
		}


		private static readonly string UrlFormat = "http://{0}.tile3.opencyclemap.org/landscape/{1}/{2}/{3}.png";
	}
}