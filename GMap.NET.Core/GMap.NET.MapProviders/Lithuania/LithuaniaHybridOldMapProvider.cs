
using System;

namespace GMap.NET.MapProviders
{
	/// <summary>
	/// LithuaniaHybridOldMap, from 2010 data, provider
	/// </summary>
	public class LithuaniaHybridOldMapProvider : LithuaniaMapProviderBase
	{
		public static readonly LithuaniaHybridOldMapProvider Instance;

		private LithuaniaHybridOldMapProvider()
		{
		}

		static LithuaniaHybridOldMapProvider()
		{
			Instance = new LithuaniaHybridOldMapProvider();
		}

		#region GMapProvider Members

		private readonly Guid id = new Guid("35C5C685-E868-4AC7-97BE-10A9A37A81B5");

		public override Guid Id
		{
			get { return id; }
		}

		private readonly string name = "LithuaniaHybridMapOld";

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
					overlays = new GMapProvider[] {LithuaniaOrtoFotoOldMapProvider.Instance, LithuaniaHybridMapProvider.Instance};
				}
				return overlays;
			}
		}

		#endregion
	}
}