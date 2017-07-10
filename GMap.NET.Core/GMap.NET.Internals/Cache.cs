﻿using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using GMap.NET.CacheProviders;

namespace GMap.NET.Internals
{
	public class CacheLocator
	{
		private static string _location;

		public static string Location
		{
			get
			{
				if (string.IsNullOrEmpty(_location))
					Reset();
				return _location;
			}
			set
			{
				if (string.IsNullOrEmpty(value)) // setting to null resets to default
					Reset();
				else
					_location = value;
				if (Delay)
					Cache.Instance.CacheLocation = _location;
			}
		}

		private static void Reset()
		{
			var appDataLocation = GetApplicationDataFolderPath();

#if !PocketPC
			// http://greatmaps.codeplex.com/discussions/403151
			// by default Network Service don't have disk write access
			if (string.IsNullOrEmpty(appDataLocation))
			{
				GMaps.Instance.Mode = AccessMode.ServerOnly;
				GMaps.Instance.UseDirectionsCache = false;
				GMaps.Instance.UseGeocoderCache = false;
				GMaps.Instance.UsePlacemarkCache = false;
				GMaps.Instance.UseRouteCache = false;
				GMaps.Instance.UseUrlCache = false;
			}
			else
#endif
			{
				Location = appDataLocation;
			}
		}

		public static string GetApplicationDataFolderPath()
		{
#if !PocketPC
			var isSystem = false;
			try
			{
				using (var identity = WindowsIdentity.GetCurrent())
				{
					if (identity != null)
					{
						isSystem = identity.IsSystem;
					}
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine("SQLitePureImageCache, WindowsIdentity.GetCurrent: " + ex);
			}

			var path = string.Empty;

			// https://greatmaps.codeplex.com/workitem/16112
			if (isSystem)
			{
				path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			}
			else
			{
				path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			}
#else
         path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
#endif

			if (!string.IsNullOrEmpty(path))
			{
				path += Path.DirectorySeparatorChar + "GMap.NET" + Path.DirectorySeparatorChar;
			}

			return path;
		}

		public static bool Delay;
	}

	/// <summary>
	/// cache system for tiles, geocoding, etc...
	/// </summary>
	internal class Cache : Singleton<Cache>
	{
		/// <summary>
		/// abstract image cache
		/// </summary>
		public PureImageCache ImageCache;

		/// <summary>
		/// second level abstract image cache
		/// </summary>
		public PureImageCache ImageCacheSecond;

		private string _cache;

		/// <summary>
		/// local cache location
		/// </summary>
		public string CacheLocation
		{
			get { return _cache; }
			set
			{
				_cache = value;
#if SQLite
				if (ImageCache is SQLitePureImageCache)
				{
					(ImageCache as SQLitePureImageCache).CacheLocation = value;
				}
#else
            if(ImageCache is MsSQLCePureImageCache)
            {
               (ImageCache as MsSQLCePureImageCache).CacheLocation = value;
            }
#endif
				CacheLocator.Delay = true;
			}
		}

		public Cache()
		{
			#region singleton check

			if (Instance != null)
			{
				throw (new Exception(
					"You have tried to create a new singleton class where you should have instanced it. Replace your \"new class()\" with \"class.Instance\""));
			}

			#endregion

#if SQLite
			ImageCache = new SQLitePureImageCache();
#else
	// you can use $ms stuff if you like too ;}
         ImageCache = new MsSQLCePureImageCache();
#endif

#if PocketPC
	// use sd card if exist for cache
         string sd = Native.GetRemovableStorageDirectory();
         if(!string.IsNullOrEmpty(sd))
         {
            CacheLocation = sd + Path.DirectorySeparatorChar +  "GMap.NET" + Path.DirectorySeparatorChar;
         }
         else
#endif
			{
#if PocketPC
            CacheLocation = CacheLocator.Location;
#else
				var newCache = CacheLocator.Location;

				var oldCache = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar +
				               "GMap.NET" + Path.DirectorySeparatorChar;

				// move database to non-roaming user directory
				if (Directory.Exists(oldCache))
				{
					try
					{
						if (Directory.Exists(newCache))
						{
							Directory.Delete(oldCache, true);
						}
						else
						{
							Directory.Move(oldCache, newCache);
						}
						CacheLocation = newCache;
					}
					catch (Exception ex)
					{
						CacheLocation = oldCache;
						Trace.WriteLine("SQLitePureImageCache, moving data: " + ex);
					}
				}
				else
				{
					CacheLocation = newCache;
				}
#endif
			}
		}

		#region -- etc cache --

		private static readonly SHA1CryptoServiceProvider HashProvider = new SHA1CryptoServiceProvider();

		private void ConvertToHash(ref string s)
		{
			s = BitConverter.ToString(HashProvider.ComputeHash(Encoding.Unicode.GetBytes(s)));
		}

		public void SaveContent(string url, CacheType type, string content)
		{
			try
			{
				ConvertToHash(ref url);

				var dir = Path.Combine(_cache, type.ToString()) + Path.DirectorySeparatorChar;

				// precrete dir
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}

				var file = dir + url + ".txt";

				using (var writer = new StreamWriter(file, false, Encoding.UTF8))
				{
					writer.Write(content);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("SaveContent: " + ex);
			}
		}

		public string GetContent(string url, CacheType type, TimeSpan stayInCache)
		{
			string ret = null;

			try
			{
				ConvertToHash(ref url);

				var dir = Path.Combine(_cache, type.ToString()) + Path.DirectorySeparatorChar;
				var file = dir + url + ".txt";

				if (File.Exists(file))
				{
					var writeTime = File.GetLastWriteTime(file);
					if (DateTime.Now - writeTime < stayInCache)
					{
						using (var r = new StreamReader(file, Encoding.UTF8))
						{
							ret = r.ReadToEnd();
						}
					}
					else
					{
						File.Delete(file);
					}
				}
			}
			catch (Exception ex)
			{
				ret = null;
				Debug.WriteLine("GetContent: " + ex);
			}

			return ret;
		}

		public string GetContent(string url, CacheType type)
		{
			return GetContent(url, type, TimeSpan.FromDays(88));
		}

		#endregion
	}

	internal enum CacheType
	{
		GeocoderCache,
		PlacemarkCache,
		RouteCache,
		UrlCache,
		DirectionsCache
	}
}