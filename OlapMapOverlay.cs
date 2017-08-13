using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using GMap.NET.WindowsForms;

namespace OlapFormsFramework.Windows.Forms.Grid.Scatter
{
	public class OlapMapOverlay : GMapOverlay
    {
		protected override void OnMarkersCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
				foreach (GMapMarker obj in e.NewItems)
					if (obj != null)
					{
						obj.Overlay = this;
						//TODO: zakomentovano dlya testovyh tsiley
						if (Control != null)
							Control.UpdateMarkerLocalPosition(obj);
					}

			if (Control != null)
			{
				if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
				{
					if (Control.IsMouseOverMarker)
					{
						Control.IsMouseOverMarker = false;
#if !PocketPC
						Control.RestoreCursorOnLeave();
#endif
					}
				}
				if (!Control.HoldInvalidation)
					Control.Invalidate();
			}
		}
		public OlapMapOverlay(string id) : base(id)
        {
        }

        public override void OnRender(Graphics g)
        {
            if (Control != null)
            {
                if (Control.RoutesEnabled)
                    foreach (var r in Routes.Where(r => r.IsVisible))
                        r.OnRender(g);

                if (Control.PolygonsEnabled)
                    foreach (var r in Polygons.Where(r => r.IsVisible))
                        r.OnRender(g);

                if (Control.MarkersEnabled)
                {
                    // markers
                    //if(m.IsVisible && (m.DisableRegionCheck || Control.Core.currentRegion.Contains(m.LocalPosition.X, m.LocalPosition.Y)))
                    foreach (var m in Markers.Where(m => m.IsVisible || m.DisableRegionCheck))
                        m.OnRender(g);
                    // tooltips above
                    //if(m.ToolTip != null && m.IsVisible && Control.Core.currentRegion.Contains(m.LocalPosition.X, m.LocalPosition.Y))
                    foreach (var m in Markers.Where(m => m.ToolTip != null && m.IsVisible)
                        .Where(m => !string.IsNullOrEmpty(m.ToolTipText) &&
                                    (m.ToolTipMode == MarkerTooltipMode.Always ||
                                     (m.ToolTipMode == MarkerTooltipMode.OnMouseOver && m.IsMouseOver))))
                        m.ToolTip.OnRender(g);
                }
            }
        }

		public new OlapScatter Control
		{
			get { return (OlapScatter)_control; }
		}
	}
}