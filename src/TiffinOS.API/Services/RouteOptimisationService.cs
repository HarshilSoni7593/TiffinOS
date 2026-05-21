namespace TiffinOS.API.Services;

public class RouteOptimisationService
{
    // ── NEAREST NEIGHBOUR ALGORITHM ───────────────────────────
    // Why this algorithm:
    // The Travelling Salesman Problem (find the absolute shortest
    // route through N points) is NP-hard — computationally
    // expensive for large N. Nearest-neighbour is a greedy
    // approximation that runs in O(n²) time and produces a route
    // within 20-25% of optimal — which is good enough for delivery.
    //
    // How it works:
    // 1. Start at the first point (depot/kitchen)
    // 2. At each step, go to the nearest unvisited point
    // 3. Repeat until all points visited
    //
    // For 50 stops this runs in milliseconds.
    // For 200 stops still under 1 second.
    public List<Guid> OptimiseRoute(
        List<DeliveryStop> stops,
        double startLat = 0,
        double startLng = 0)
    {
        if (!stops.Any())
            return new List<Guid>();

        if (stops.Count == 1)
            return new List<Guid> { stops[0].Id };

        var unvisited = stops.ToList();
        var route = new List<Guid>();

        // Start from kitchen/depot coordinates
        double currentLat = startLat;
        double currentLng = startLng;

        // If no start coordinates given, use first stop as start
        if (startLat == 0 && startLng == 0)
        {
            var first = unvisited[0];
            route.Add(first.Id);
            currentLat = first.Lat;
            currentLng = first.Lng;
            unvisited.RemoveAt(0);
        }

        while (unvisited.Any())
        {
            // Find the nearest unvisited stop
            var nearest = unvisited
                .OrderBy(s => HaversineDistance(
                    currentLat, currentLng,
                    s.Lat, s.Lng))
                .First();

            route.Add(nearest.Id);
            currentLat = nearest.Lat;
            currentLng = nearest.Lng;
            unvisited.Remove(nearest);
        }

        return route;
    }

    // ── HAVERSINE DISTANCE FORMULA ────────────────────────────
    // Why Haversine and not straight Euclidean distance:
    // The Earth is curved. At the scale of a city, Euclidean
    // distance (straight line) introduces ~0.5% error per 100km.
    // For Toronto delivery zones that's negligible, but Haversine
    // gives us true great-circle distance in km which is more
    // accurate and costs nothing extra to compute.
    private static double HaversineDistance(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in km

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c; // distance in km
    }

    private static double ToRad(double deg) =>
        deg * (Math.PI / 180);
}

public record DeliveryStop(
    Guid Id,            // delivery_schedule.id
    double Lat,
    double Lng
);