namespace ParkingApi.Models.Entities
{
    public class ParkingLot
    {
        public int ParkingLotId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string Location { get; set; } = String.Empty;
        public int Capacity { get; set; }
        public List<ParkingSpace> ParkingSpaces { get; set; } = new List<ParkingSpace>();
    }
}
