
using System;
using System.Globalization;

namespace GMap.NET
{
	/// <summary>
	/// the rect
	/// </summary>
	public struct GRect
	{
		public static readonly GRect Empty = new GRect();

		private long _x;
		private long _y;
		private long _width;
		private long _height;

		public GRect(long x, long y, long width, long height)
		{
			_x = x;
			_y = y;
			_width = width;
			_height = height;
		}

		public GRect(GPoint location, GSize size)
		{
			_x = location.X;
			_y = location.Y;
			_width = size.Width;
			_height = size.Height;
		}

		public static GRect FromLTRB(int left, int top, int right, int bottom)
		{
			return new GRect(left,
				top,
				right - left,
				bottom - top);
		}

		public GPoint Location
		{
			get { return new GPoint(X, Y); }
			set
			{
				X = value.X;
				Y = value.Y;
			}
		}

		public GPoint RightBottom
		{
			get { return new GPoint(Right, Bottom); }
		}

		public GPoint RightTop
		{
			get { return new GPoint(Right, Top); }
		}

		public GPoint LeftBottom
		{
			get { return new GPoint(Left, Bottom); }
		}

		public GSize Size
		{
			get { return new GSize(Width, Height); }
			set
			{
				Width = value.Width;
				Height = value.Height;
			}
		}

		public long X
		{
			get { return _x; }
			set { _x = value; }
		}

		public long Y
		{
			get { return _y; }
			set { _y = value; }
		}

		public long Width
		{
			get { return _width; }
			set { _width = value; }
		}

		public long Height
		{
			get { return _height; }
			set { _height = value; }
		}

		public long Left
		{
			get { return X; }
		}

		public long Top
		{
			get { return Y; }
		}

		public long Right
		{
			get { return X + Width; }
		}

		public long Bottom
		{
			get { return Y + Height; }
		}

		public bool IsEmpty
		{
			get { return _height == 0 && _width == 0 && _x == 0 && _y == 0; }
		}

		public override bool Equals(object obj)
		{
			if (!(obj is GRect))
				return false;

			var comp = (GRect) obj;

			return (comp.X == X) &&
			       (comp.Y == Y) &&
			       (comp.Width == Width) &&
			       (comp.Height == Height);
		}

		public static bool operator ==(GRect left, GRect right)
		{
			return (left.X == right.X
			        && left.Y == right.Y
			        && left.Width == right.Width
			        && left.Height == right.Height);
		}

		public static bool operator !=(GRect left, GRect right)
		{
			return !(left == right);
		}

		public bool Contains(long x, long y)
		{
			return X <= x &&
			       x < X + Width &&
			       Y <= y &&
			       y < Y + Height;
		}

		public bool Contains(GPoint pt)
		{
			return Contains(pt.X, pt.Y);
		}

		public bool Contains(GRect rect)
		{
			return (X <= rect.X) &&
			       ((rect.X + rect.Width) <= (X + Width)) &&
			       (Y <= rect.Y) &&
			       ((rect.Y + rect.Height) <= (Y + Height));
		}

		public override int GetHashCode()
		{
			if (IsEmpty)
			{
				return 0;
			}
			return
				(int) (((X ^ ((Y << 13) | (Y >> 0x13))) ^ ((Width << 0x1a) | (Width >> 6))) ^ ((Height << 7) | (Height >> 0x19)));
		}

		public void Inflate(long width, long height)
		{
			X -= width;
			Y -= height;
			Width += 2*width;
			Height += 2*height;
		}

		public void Inflate(GSize size)
		{
			Inflate(size.Width, size.Height);
		}

		public static GRect Inflate(GRect rect, long x, long y)
		{
			var r = rect;
			r.Inflate(x, y);
			return r;
		}

		public void Intersect(GRect rect)
		{
			var result = Intersect(rect, this);

			X = result.X;
			Y = result.Y;
			Width = result.Width;
			Height = result.Height;
		}

		public static GRect Intersect(GRect a, GRect b)
		{
			var x1 = Math.Max(a.X, b.X);
			var x2 = Math.Min(a.X + a.Width, b.X + b.Width);
			var y1 = Math.Max(a.Y, b.Y);
			var y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

			if (x2 >= x1
			    && y2 >= y1)
			{

				return new GRect(x1, y1, x2 - x1, y2 - y1);
			}
			return Empty;
		}

		public bool IntersectsWith(GRect rect)
		{
			return (rect.X < X + Width) &&
			       (X < (rect.X + rect.Width)) &&
			       (rect.Y < Y + Height) &&
			       (Y < rect.Y + rect.Height);
		}

		public static GRect Union(GRect a, GRect b)
		{
			var x1 = Math.Min(a.X, b.X);
			var x2 = Math.Max(a.X + a.Width, b.X + b.Width);
			var y1 = Math.Min(a.Y, b.Y);
			var y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

			return new GRect(x1, y1, x2 - x1, y2 - y1);
		}

		public void Offset(GPoint pos)
		{
			Offset(pos.X, pos.Y);
		}

		public void OffsetNegative(GPoint pos)
		{
			Offset(-pos.X, -pos.Y);
		}

		public void Offset(long x, long y)
		{
			X += x;
			Y += y;
		}

		public override string ToString()
		{
			return "{X=" + X.ToString(CultureInfo.CurrentCulture) + ",Y=" + Y.ToString(CultureInfo.CurrentCulture) +
			       ",Width=" + Width.ToString(CultureInfo.CurrentCulture) +
			       ",Height=" + Height.ToString(CultureInfo.CurrentCulture) + "}";
		}
	}
}