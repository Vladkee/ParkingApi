using ParkingApi.Models.Enums;

namespace ParkingApi.Models.Requests
{
    public class ParkVehicleRequest
    {
        public string VehicleReg { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.Unknown;
    }
}