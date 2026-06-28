using ParkingApi.Models.Enums;

namespace ParkingApi.Models.Entities
{
    public class Vehicle
    {
        // Created for future.
        public int VehicleId { get; set; }
        public string VehicleReg { get; set; } = String.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.Unknown;
    }
}