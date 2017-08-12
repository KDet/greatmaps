using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using GMap.NET.Internals;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;

namespace GMap.NET
{
	/// <summary>
	/// form helping to prefetch tiles on local db
	/// </summary>
	public partial class TilePrefetcher : FormsFramework.Windows.Forms.AdvancedForm
	{
		private BackgroundWorker _worker = new BackgroundWorker();
		private List<GPoint> _list;
		private int _zoom;
		private GMapProvider _provider;
		private int _sleep;
		private int _all;
		public bool ShowCompleteMessage = false;
		private RectLatLng _area;
		private GSize _maxOfTiles;
		public GMapOverlay Overlay;
		private int _retry;
		public bool Shuffle = true;

		public TilePrefetcher()
		{
			InitializeComponent();

			GMaps.Instance.OnTileCacheComplete += OnTileCacheComplete;
			GMaps.Instance.OnTileCacheStart += OnTileCacheStart;
			GMaps.Instance.OnTileCacheProgress += OnTileCacheProgress;

			_worker.WorkerReportsProgress = true;
			_worker.WorkerSupportsCancellation = true;
			_worker.ProgressChanged += worker_ProgressChanged;
			_worker.DoWork += worker_DoWork;
			_worker.RunWorkerCompleted += worker_RunWorkerCompleted;
		}

		private readonly AutoResetEvent _done = new AutoResetEvent(true);

		private void OnTileCacheComplete()
		{
			if (!IsDisposed)
			{
				_done.Set();

				MethodInvoker m = delegate
				{
					label2.Text = "all tiles saved";
				};
				Invoke(m);
			}
		}

		private void OnTileCacheStart()
		{
			if (!IsDisposed)
			{
				_done.Reset();

				MethodInvoker m = delegate
				{
					label2.Text = "saving tiles...";
				};
				Invoke(m);
			}
		}

		private void OnTileCacheProgress(int left)
		{
			if (!IsDisposed)
			{
				MethodInvoker m = delegate
				{
					label2.Text = left + " tile to save...";
				};
				Invoke(m);
			}
		}

		public void Start(RectLatLng area, int zoom, GMapProvider provider, int sleep, int retry)
		{
			if (!_worker.IsBusy)
			{
				label1.Text = "...";
				progressBarDownload.Value = 0;

				_area = area;
				_zoom = zoom;
				_provider = provider;
				_sleep = sleep;
				_retry = retry;

				GMaps.Instance.UseMemoryCache = false;
				GMaps.Instance.CacheOnIdleRead = false;
				GMaps.Instance.BoostCacheEngine = true;

				if (Overlay != null)
				{
					Overlay.Markers.Clear();
				}

				_worker.RunWorkerAsync();

				ShowDialog();
			}
		}

		public void Stop()
		{
			GMaps.Instance.OnTileCacheComplete -= OnTileCacheComplete;
			GMaps.Instance.OnTileCacheStart -= OnTileCacheStart;
			GMaps.Instance.OnTileCacheProgress -= OnTileCacheProgress;

			_done.Set();

			if (_worker.IsBusy)
			{
				_worker.CancelAsync();
			}

			GMaps.Instance.CancelTileCaching();

			_done.Close();
		}

		private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (ShowCompleteMessage)
			{
				if (!e.Cancelled)
				{
					MessageBox.Show(this, "Prefetch Complete! => " + ((int) e.Result) + " of " + _all);
				}
				else
				{
					MessageBox.Show(this, "Prefetch Canceled! => " + ((int) e.Result) + " of " + _all);
				}
			}

			_list.Clear();

			GMaps.Instance.UseMemoryCache = true;
			GMaps.Instance.CacheOnIdleRead = true;
			GMaps.Instance.BoostCacheEngine = false;

			_worker.Dispose();

			Close();
		}

		private bool CacheTiles(int zoom, GPoint p)
		{
			foreach (var pr in _provider.Overlays)
			{
				Exception ex;
				PureImage img;

				// tile number inversion(BottomLeft -> TopLeft)
				if (pr.InvertedAxisY)
				{
					img = GMaps.Instance.GetImageFrom(pr, new GPoint(p.X, _maxOfTiles.Height - p.Y), zoom, out ex);
				}
				else // ok
				{
					img = GMaps.Instance.GetImageFrom(pr, p, zoom, out ex);
				}

				if (img != null)
				{
					img.Dispose();
					img = null;
				}
				else
				{
					return false;
				}
			}
			return true;
		}

		public readonly Queue<GPoint> CachedTiles = new Queue<GPoint>();

		private void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			if (_list != null)
			{
				_list.Clear();
				_list = null;
			}
			_list = _provider.Projection.GetAreaTileList(_area, _zoom, 0);
			_maxOfTiles = _provider.Projection.GetTileMatrixMaxXY(_zoom);
			_all = _list.Count;

			var countOk = 0;
			var retryCount = 0;

			if (Shuffle)
			{
				Stuff.Shuffle(_list);
			}

			lock (this)
			{
				CachedTiles.Clear();
			}

			for (var i = 0; i < _all; i++)
			{
				if (_worker.CancellationPending)
					break;

				var p = _list[i];
				{
					if (CacheTiles(_zoom, p))
					{
						if (Overlay != null)
						{
							lock (this)
							{
								CachedTiles.Enqueue(p);
							}
						}
						countOk++;
						retryCount = 0;
					}
					else
					{
						if (++retryCount <= _retry) // retry only one
						{
							i--;
							Thread.Sleep(1111);
							continue;
						}
						retryCount = 0;
					}
				}

				_worker.ReportProgress((i + 1)*100/_all, i + 1);

				if (_sleep > 0)
				{
					Thread.Sleep(_sleep);
				}
			}

			e.Result = countOk;

			if (!IsDisposed)
			{
				_done.WaitOne();
			}
		}

		private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			label1.Text = "Fetching tile at zoom (" + _zoom + "): " + ((int) e.UserState) + " of " + _all + ", complete: " +
			              e.ProgressPercentage + "%";
			progressBarDownload.Value = e.ProgressPercentage;

			if (Overlay != null)
			{
				GPoint? l = null;

				lock (this)
				{
					if (CachedTiles.Count > 0)
					{
						l = CachedTiles.Dequeue();
					}
				}

				if (l.HasValue)
				{
					var px = Overlay.Control.MapProvider.Projection.FromTileXYToPixel(l.Value);
					var p = Overlay.Control.MapProvider.Projection.FromPixelToLatLng(px, _zoom);

					var r1 = Overlay.Control.MapProvider.Projection.GetGroundResolution(_zoom, p.Lat);
					var r2 = Overlay.Control.MapProvider.Projection.GetGroundResolution((int) Overlay.Control.Zoom, p.Lat);
					var sizeDiff = r2/r1;

					var m = new GMapMarkerTile(p, (int) (Overlay.Control.MapProvider.Projection.TileSize.Width/sizeDiff));
					Overlay.Markers.Add(m);
				}
			}
		}

		private void Prefetch_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				Close();
			}
		}

		private void Prefetch_FormClosed(object sender, FormClosedEventArgs e)
		{
			Stop();
		}
	}

	public class GMapMarkerTile : GMapMarker
	{
		private static Brush _fill = new SolidBrush(Color.FromArgb(155, Color.Blue));

		public GMapMarkerTile(PointLatLng p, int size) : base(p)
		{
			Size = new SizeF(size, size);
		}

		public override void OnRender(Graphics g)
		{
			g.FillRectangle(_fill, new RectangleF(LocalPosition.X, LocalPosition.Y, Size.Width, Size.Height));
		}
	}
}