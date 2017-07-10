
using System.Collections.Generic;

namespace GMap.NET
{
	/// <summary>
	/// Geocoding interface
	/// </summary>
	public interface GeocodingProvider
	{
		GeoCoderStatusCode GetPoints(string keywords, out List<PointLatLng> pointList);

		PointLatLng? GetPoint(string keywords, out GeoCoderStatusCode status);

		GeoCoderStatusCode GetPoints(Placemark placemark, out List<PointLatLng> pointList);

		PointLatLng? GetPoint(Placemark placemark, out GeoCoderStatusCode status);

		// ...

		GeoCoderStatusCode GetPlacemarks(PointLatLng location, out List<Placemark> placemarkList);

		Placemark? GetPlacemark(PointLatLng location, out GeoCoderStatusCode status);
	}
}