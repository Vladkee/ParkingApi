using ParkingApi.Models.Enums;

namespace ParkingApi.Models.Entities
{
    public class ParkingSpace
    {
        public int ParkingSpaceId { get; set; }
        public int ParkingLotId { get; set; }
        public ParkingLot ParkingLot { get; set; } = null!;
        public int SpaceNumber { get; set; }
        public string VehicleReg { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.Unknown;
        public DateTime TimeIn { get; set; } = DateTime.UtcNow;
        public DateTime TimeOut { get; set; }
        public bool IsOccupied { get; set; }
    }
}