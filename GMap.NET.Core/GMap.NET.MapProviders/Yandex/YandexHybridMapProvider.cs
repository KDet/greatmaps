
namespace GMap.NET.MapProviders
{
   using System;

   /// <summary>
   /// YandexHybridMap provider
   /// </summary>
   public class YandexHybridMapProvider : YandexMapProviderBase
   {
      public static readonly YandexHybridMapProvider Instance;

       private YandexHybridMapProvider()
      {
      }

      static YandexHybridMapProvider()
      {
         Instance = new YandexHybridMapProvider();
      }

      #region GMapProvider Members

       private readonly Guid id = new Guid("78A3830F-5EE3-432C-A32E-91B7AF6BBCB9");
      public override Guid Id
      {
         get
         {
            return id;
         }
      }

       private readonly string name = "YandexHybridMap";
      public override string Name
      {
         get
         {
            return name;
         }
      }

       private GMapProvider[] overlays;
      public override GMapProvider[] Overlays
      {
         get
         {
            if(overlays == null)
            {
               overlays = new GMapProvider[] { YandexSatelliteMapProvider.Instance, this };
            }
            return overlays;
         }
      }

      public override PureImage GetTileImage(GPoint pos, int zoom)
      {
         string url = MakeTileImageUrl(pos, zoom, LanguageStr);

         return GetTileImageUsingHttp(url);
      }

      #endregion

       private string MakeTileImageUrl(GPoint pos, int zoom, string language)
      {
         return string.Format(UrlFormat, UrlServer, GetServerNum(pos, 4) + 1, Version, pos.X, pos.Y, zoom, language, Server);
      }

       private static readonly string UrlServer = "vec";
       private static readonly string UrlFormat = "http://{0}0{1}.{7}/tiles?l=skl&v={2}&x={3}&y={4}&z={5}&lang={6}";      
   }
}