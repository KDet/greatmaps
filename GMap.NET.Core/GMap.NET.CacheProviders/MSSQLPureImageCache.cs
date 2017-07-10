
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using GMap.NET.MapProviders;

namespace GMap.NET.CacheProviders
{
	/// <summary>
	/// image cache for ms sql server
	/// optimized by mmurfinsimmons@gmail.com
	/// </summary>
	public class MsSQLPureImageCache : PureImageCache, IDisposable
	{
		private string connectionString = string.Empty;

		public string ConnectionString
		{
			get { return connectionString; }
			set
			{
				if (connectionString != value)
				{
					connectionString = value;

					if (Initialized)
					{
						Dispose();
						Initialize();
					}
				}
			}
		}

		private SqlCommand cmdInsert;
		private SqlCommand cmdFetch;
		private SqlConnection cnGet;
		private SqlConnection cnSet;

		private bool initialized;

		/// <summary>
		/// is cache initialized
		/// </summary>
		public bool Initialized
		{
			get
			{
				lock (this)
				{
					return initialized;
				}
			}
			private set
			{
				lock (this)
				{
					initialized = value;
				}
			}
		}

		/// <summary>
		/// inits connection to server
		/// </summary>
		/// <returns></returns>
		public bool Initialize()
		{
			lock (this)
			{
				if (!Initialized)
				{
					#region prepare mssql & cache table

					try
					{
						// different connections so the multi-thread inserts and selects don't collide on open readers.
						cnGet = new SqlConnection(connectionString);
						cnGet.Open();
						cnSet = new SqlConnection(connectionString);
						cnSet.Open();

						var tableexists = false;
						using (var cmd = new SqlCommand("select object_id('GMapNETcache')", cnGet))
						{
							var objid = cmd.ExecuteScalar();
							tableexists = (objid != null && objid != DBNull.Value);
						}
						if (!tableexists)
						{
							using (var cmd = new SqlCommand(
								"CREATE TABLE [GMapNETcache] ( \n"
								+ "   [Type] [int]   NOT NULL, \n"
								+ "   [Zoom] [int]   NOT NULL, \n"
								+ "   [X]    [int]   NOT NULL, \n"
								+ "   [Y]    [int]   NOT NULL, \n"
								+ "   [Tile] [image] NOT NULL, \n"
								+ "   CONSTRAINT [PK_GMapNETcache] PRIMARY KEY CLUSTERED (Type, Zoom, X, Y) \n"
								+ ")", cnGet))
							{
								cmd.ExecuteNonQuery();
							}
						}

						cmdFetch =
							new SqlCommand(
								"SELECT [Tile] FROM [GMapNETcache] WITH (NOLOCK) WHERE [X]=@x AND [Y]=@y AND [Zoom]=@zoom AND [Type]=@type",
								cnGet);
						cmdFetch.Parameters.Add("@x", SqlDbType.Int);
						cmdFetch.Parameters.Add("@y", SqlDbType.Int);
						cmdFetch.Parameters.Add("@zoom", SqlDbType.Int);
						cmdFetch.Parameters.Add("@type", SqlDbType.Int);
						cmdFetch.Prepare();

						cmdInsert =
							new SqlCommand(
								"INSERT INTO [GMapNETcache] ( [X], [Y], [Zoom], [Type], [Tile] ) VALUES ( @x, @y, @zoom, @type, @tile )", cnSet);
						cmdInsert.Parameters.Add("@x", SqlDbType.Int);
						cmdInsert.Parameters.Add("@y", SqlDbType.Int);
						cmdInsert.Parameters.Add("@zoom", SqlDbType.Int);
						cmdInsert.Parameters.Add("@type", SqlDbType.Int);
						cmdInsert.Parameters.Add("@tile", SqlDbType.Image); //, calcmaximgsize);
						//can't prepare insert because of the IMAGE field having a variable size.  Could set it to some 'maximum' size?

						Initialized = true;
					}
					catch (Exception ex)
					{
						initialized = false;
						Debug.WriteLine(ex.Message);
					}

					#endregion
				}
				return Initialized;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			lock (cmdInsert)
			{
				if (cmdInsert != null)
				{
					cmdInsert.Dispose();
					cmdInsert = null;
				}

				if (cnSet != null)
				{
					cnSet.Dispose();
					cnSet = null;
				}
			}

			lock (cmdFetch)
			{
				if (cmdFetch != null)
				{
					cmdFetch.Dispose();
					cmdFetch = null;
				}

				if (cnGet != null)
				{
					cnGet.Dispose();
					cnGet = null;
				}
			}
			Initialized = false;
		}

		#endregion

		#region PureImageCache Members

		public bool PutImageToCache(byte[] tile, int type, GPoint pos, int zoom)
		{
			var ret = true;
			{
				if (Initialize())
				{
					try
					{
						lock (cmdInsert)
						{
							cmdInsert.Parameters["@x"].Value = pos.X;
							cmdInsert.Parameters["@y"].Value = pos.Y;
							cmdInsert.Parameters["@zoom"].Value = zoom;
							cmdInsert.Parameters["@type"].Value = type;
							cmdInsert.Parameters["@tile"].Value = tile;
							cmdInsert.ExecuteNonQuery();
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.ToString());
						ret = false;
						Dispose();
					}
				}
			}
			return ret;
		}

		public PureImage GetImageFromCache(int type, GPoint pos, int zoom)
		{
			PureImage ret = null;
			{
				if (Initialize())
				{
					try
					{
						object odata = null;
						lock (cmdFetch)
						{
							cmdFetch.Parameters["@x"].Value = pos.X;
							cmdFetch.Parameters["@y"].Value = pos.Y;
							cmdFetch.Parameters["@zoom"].Value = zoom;
							cmdFetch.Parameters["@type"].Value = type;
							odata = cmdFetch.ExecuteScalar();
						}

						if (odata != null && odata != DBNull.Value)
						{
							var tile = (byte[]) odata;
							if (tile != null && tile.Length > 0)
							{
								if (GMapProvider.TileImageProxy != null)
								{
									ret = GMapProvider.TileImageProxy.FromArray(tile);
								}
							}
							tile = null;
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.ToString());
						ret = null;
						Dispose();
					}
				}
			}
			return ret;
		}

		/// <summary>
		/// NotImplemented
		/// </summary>
		/// <param name="date"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		int PureImageCache.DeleteOlderThan(DateTime date, int? type)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}