
namespace GMap.NET.MapProviders
{
   using System;

   /// <summary>
   /// YandexSatelliteMap provider
   /// </summary>
   public class YandexSatelliteMapProvider : YandexMapProviderBase
   {
      public static readonly YandexSatelliteMapProvider Instance;

       private YandexSatelliteMapProvider()
      {
      }

      static YandexSatelliteMapProvider()
      {
         Instance = new YandexSatelliteMapProvider();
      }

      public new string Version = "3.135.0";

      #region GMapProvider Members

       private readonly Guid id = new Guid("2D4CE763-0F91-40B2-A511-13EF428237AD");
      public override Guid Id
      {
         get
         {
            return id;
         }
      }

       private readonly string name = "YandexSatelliteMap";
      public override string Name
      {
         get
         {
            return name;
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

       private static readonly string UrlServer = "sat";
       private static readonly string UrlFormat = "http://{0}0{1}.{7}/tiles?l=sat&v={2}&x={3}&y={4}&z={5}&lang={6}";             
   }
}