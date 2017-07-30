using System.Drawing.Imaging;

namespace GMap.NET.WindowsForms
{
   public static class ColorMatrixes
   {
#if !PocketPC
      public static readonly ColorMatrix GrayScale = new ColorMatrix(new[]
      {
         new[] {0.3f, 0.3f, 0.3f, 0f, 0f},
         new[] {0.59f, 0.59f, 0.59f, 0f, 0f},
         new[] {0.11f, 0.11f, 0.11f, 0f, 0f},
         new[] {0f, 0f, 0f, 1f, 0f},
         new[] {0f, 0f, 0f, 0f, 1f}
      });
      public static readonly ColorMatrix Negative = new ColorMatrix(new[]
      {
        new[] {-1f, 0f, 0f, 0f, 0f},
        new[] {0f, -1f, 0f, 0f, 0f},
        new[] {0f, 0f, -1f, 0f, 0f},
        new[] {0f, 0f, 0f, 1f, 0f},
        new[] {1f, 1f, 1f, 0f, 1f}
      });
#endif
   }
}