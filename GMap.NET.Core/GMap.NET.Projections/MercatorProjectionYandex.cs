
using System;

namespace GMap.NET.Projections
{
	internal class MercatorProjectionYandex : PureProjection
	{
		public static readonly MercatorProjectionYandex Instance = new MercatorProjectionYandex();

		private static readonly double MinLatitude = -85.05112878;
		private static readonly double MaxLatitude = 85.05112878;
		private static readonly double MinLongitude = -177;
		private static readonly double MaxLongitude = 177;

		private static readonly double RAD_DEG = 180/Math.PI;
		private static readonly double DEG_RAD = Math.PI/180;
		private static readonly double MathPiDiv4 = Math.PI/4;

		public override RectLatLng Bounds
		{
			get { return RectLatLng.FromLtrb(MinLongitude, MaxLatitude, MaxLongitude, MinLatitude); }
		}

		private GSize tileSize = new GSize(256, 256);

		public override GSize TileSize
		{
			get { return tileSize; }
		}

		public override double Axis
		{
			get { return 6356752.3142; }
		}

		public override double Flattening
		{
			get { return (1.0/298.257223563); }
		}

		public override GPoint FromLatLngToPixel(double lat, double lng, int zoom)
		{
			lat = Clip(lat, MinLatitude, MaxLatitude);
			lng = Clip(lng, MinLongitude, MaxLongitude);

			var rLon = lng*DEG_RAD; // Math.PI / 180; 
			var rLat = lat*DEG_RAD; // Math.PI / 180; 

			double a = 6378137;
			var k = 0.0818191908426;

			var z = Math.Tan(MathPiDiv4 + rLat/2)/Math.Pow((Math.Tan(MathPiDiv4 + Math.Asin(k*Math.Sin(rLat))/2)), k);
			var z1 = Math.Pow(2, 23 - zoom);

			var DX = ((20037508.342789 + a*rLon)*53.5865938/z1);
			var DY = ((20037508.342789 - a*Math.Log(z))*53.5865938/z1);

			var ret = GPoint.Empty;
			ret.X = (long) DX;
			ret.Y = (long) DY;

			return ret;
		}

		public override PointLatLng FromPixelToLatLng(long x, long y, int zoom)
		{
			var s = GetTileMatrixSizePixel(zoom);

			double mapSizeX = s.Width;
			double mapSizeY = s.Height;

			double a = 6378137;
			var c1 = 0.00335655146887969;
			var c2 = 0.00000657187271079536;
			var c3 = 0.00000001764564338702;
			var c4 = 0.00000000005328478445;
			double z1 = (23 - zoom);
			var mercX = (x*Math.Pow(2, z1))/53.5865938 - 20037508.342789;
			var mercY = 20037508.342789 - (y*Math.Pow(2, z1))/53.5865938;

			var g = Math.PI/2 - 2*Math.Atan(1/Math.Exp(mercY/a));
			var z = g + c1*Math.Sin(2*g) + c2*Math.Sin(4*g) + c3*Math.Sin(6*g) + c4*Math.Sin(8*g);

			var ret = PointLatLng.Empty;
			ret.Lat = z*RAD_DEG;
			ret.Lng = mercX/a*RAD_DEG;

			return ret;
		}

		public override GSize GetTileMatrixMinXY(int zoom)
		{
			return new GSize(0, 0);
		}

		public override GSize GetTileMatrixMaxXY(int zoom)
		{
			long xy = (1 << zoom);
			return new GSize(xy - 1, xy - 1);
		}
	}
}