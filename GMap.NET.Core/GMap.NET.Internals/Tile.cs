﻿
using System;
using System.Collections.Generic;
using System.Threading;

namespace GMap.NET.Internals
{
	/// <summary>
	/// represent tile
	/// </summary>
	public struct Tile : IDisposable
	{
		public static readonly Tile Empty = new Tile();

		private GPoint pos;
		private int zoom;
		private PureImage[] overlays;
		private long OverlaysCount;

		public readonly bool NotEmpty;

		public Tile(int zoom, GPoint pos)
		{
			NotEmpty = true;
			this.zoom = zoom;
			this.pos = pos;
			overlays = null;
			OverlaysCount = 0;
		}

		public IEnumerable<PureImage> Overlays
		{
			get
			{
#if PocketPC
                for (long i = 0, size = OverlaysCount; i < size; i++)
#else
				for (long i = 0, size = Interlocked.Read(ref OverlaysCount); i < size; i++)
#endif
				{
					yield return overlays[i];
				}
			}
		}

		internal void AddOverlay(PureImage i)
		{
			if (overlays == null)
			{
				overlays = new PureImage[4];
			}
#if !PocketPC
			overlays[Interlocked.Increment(ref OverlaysCount) - 1] = i;
#else
            overlays[++OverlaysCount - 1] = i;
#endif
		}

		internal bool HasAnyOverlays
		{
			get
			{
#if PocketPC
                return OverlaysCount > 0;
#else
				return Interlocked.Read(ref OverlaysCount) > 0;
#endif
			}
		}

		public int Zoom
		{
			get { return zoom; }
			private set { zoom = value; }
		}

		public GPoint Pos
		{
			get { return pos; }
			private set { pos = value; }
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (overlays != null)
			{
#if PocketPC
                for (long i = OverlaysCount - 1; i >= 0; i--)

#else
				for (var i = Interlocked.Read(ref OverlaysCount) - 1; i >= 0; i--)
#endif
				{
#if !PocketPC
					Interlocked.Decrement(ref OverlaysCount);
#else
                    OverlaysCount--;
#endif
					overlays[i].Dispose();
					overlays[i] = null;
				}
				overlays = null;
			}
		}

		#endregion

		public static bool operator ==(Tile m1, Tile m2)
		{
			return m1.pos == m2.pos && m1.zoom == m2.zoom;
		}

		public static bool operator !=(Tile m1, Tile m2)
		{
			return !(m1 == m2);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Tile))
				return false;

			var comp = (Tile) obj;
			return comp.Zoom == Zoom && comp.Pos == Pos;
		}

		public override int GetHashCode()
		{
			return zoom ^ pos.GetHashCode();
		}
	}
}