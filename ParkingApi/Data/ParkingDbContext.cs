using Microsoft.EntityFrameworkCore;
using ParkingApi.Models.Entities;

namespace ParkingApi.Data
{
    public class ParkingDbContext : DbContext
    {
        public ParkingDbContext(DbContextOptions<ParkingDbContext> options) : base(options) { }

        public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();
        public DbSet<ParkingSpace> ParkingSpaces => Set<ParkingSpace>();
    }
}
