using Microsoft.EntityFrameworkCore;
using ParkingApi.Data;
using ParkingApi.IServices;
using ParkingApi.Models.Enums;
using ParkingApi.Models.Requests;
using ParkingApi.Models.Responses;

namespace ParkingApi.Services
{
    public class ParkingService : IParkingService
    {
        private readonly ParkingDbContext _dbContext;
        private static readonly Dictionary<VehicleType, double> Rates = new()
            {
                { VehicleType.Small,  0.10 },
                { VehicleType.Medium, 0.20 },
                { VehicleType.Large,  0.40 }
            };

        public ParkingService(ParkingDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        #region Methods
        public async Task<ParkVehicleResponse> ParkVehicleAsync(ParkVehicleRequest request)
        {
            if (!Enum.IsDefined(typeof(VehicleType), request.VehicleType) || request.VehicleType == VehicleType.Unknown)
                throw new ArgumentException("VehicleType must be 1 (Small), 2 (Medium) or 3 (Large).");

            if (String.IsNullOrWhiteSpace(request.VehicleReg))
                throw new ArgumentException("Vehicle registration must be provided.");

            bool isAlreadyParked = await _dbContext.ParkingSpaces
                .AnyAsync(s => s.IsOccupied && s.VehicleReg == request.VehicleReg);

            if (isAlreadyParked)
                throw new InvalidOperationException($"Vehicle {request.VehicleReg} is already parked.");

            var space = await _dbContext.ParkingSpaces
                .Where(s => !s.IsOccupied)
                .OrderBy(s => s.SpaceNumber)
                .FirstOrDefaultAsync();

            if (space is null)
                throw new InvalidOperationException("No available parking spaces.");

            space.VehicleReg = request.VehicleReg;
            space.VehicleType = request.VehicleType;
            space.TimeIn = DateTime.UtcNow;
            space.IsOccupied = true;

            await _dbContext.SaveChangesAsync();

            return new ParkVehicleResponse
            {
                VehicleReg = space.VehicleReg,
                SpaceNumber = space.SpaceNumber,
                TimeIn = space.TimeIn
            };
        }

        public async Task<ParkingLotStatusResponse> GetParkingLotStatusAsync() 
        {
            var lot = await _dbContext.ParkingLots.FirstOrDefaultAsync(); // Assuming we have just 1 parking lot.

            if (lot is null)
                throw new KeyNotFoundException("Parking lot not found.");

            int occupied = await _dbContext.ParkingSpaces.CountAsync(s => s.IsOccupied);
            int available = lot.Capacity - occupied;

            return new ParkingLotStatusResponse
            {
                AvailableSpaces = available,
                OccupiedSpaces = occupied
            };
        }

        public async Task<ExitVehicleResponse> ExitVehicleAsync(ExitVehicleRequest request)
        {
            var space = await _dbContext.ParkingSpaces
                .FirstOrDefaultAsync(s => s.IsOccupied && s.VehicleReg == request.VehicleReg);

            if (space is null)
                throw new KeyNotFoundException($"Vehicle {request.VehicleReg} is not currently parked.");

            var timeOut = DateTime.UtcNow;
            double charge = CalculateCharge(space.TimeIn, timeOut, space.VehicleType);

            var response = new ExitVehicleResponse
            {
                VehicleReg = space.VehicleReg,
                VehicleCharge = charge,
                TimeIn = space.TimeIn,
                TimeOut = timeOut
            };

            space.VehicleReg = String.Empty;
            space.VehicleType = VehicleType.Unknown;
            space.TimeIn = DateTime.MinValue;
            space.IsOccupied = false;

            await _dbContext.SaveChangesAsync();

            return response;
        }

        private static double CalculateCharge(DateTime timeIn, DateTime timeOut, VehicleType vehicleType)
        {
            double totalMinutes = (timeOut - timeIn).TotalMinutes;
            double rate = Rates[vehicleType];
            double charge = totalMinutes * rate + Math.Floor(totalMinutes / 5) * 1.0;
            return Math.Round(charge, 2);
        }

        #endregion Methods
    }
}