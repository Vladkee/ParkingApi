namespace ParkingApi.Models.Responses
{
    public class ParkVehicleResponse
    {
        public string VehicleReg { get; set; } = string.Empty;
        public int SpaceNumber { get; set; }
        public DateTime TimeIn { get; set; } = DateTime.UtcNow;
    }
}