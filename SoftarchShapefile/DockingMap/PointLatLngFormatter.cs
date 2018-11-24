using GMap.NET;

namespace ShapefileEditor
{
    class PointLatLngFormatter
    {
        public static string Format(PointLatLng p)
        {
            return string.Format("Lat: {0,9:F6}°,  long: {1,10:F6}°", p.Lat, p.Lng);
        }
    }
}