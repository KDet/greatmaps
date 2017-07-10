
namespace GMap.NET
{
	/// <summary>
	/// Tile access mode
	/// </summary>
	public enum AccessMode
	{
		/// <summary>
		/// Access only server
		/// </summary>
		ServerOnly,

		/// <summary>
		/// Access first server and caches localy
		/// </summary>
		ServerAndCache,

		/// <summary>
		/// access only cache
		/// </summary>
		CacheOnly
	}
}