namespace ParkingApi.Models.Responses
{
    public class ExitVehicleResponse
    {
        public string VehicleReg { get; set; } = string.Empty;
        public double VehicleCharge { get; set; }
        public DateTime TimeIn { get; set; } = DateTime.UtcNow;
        public DateTime TimeOut { get; set; }
    }
}