using ParkingApi.Models.Requests;
using ParkingApi.Models.Responses;

namespace ParkingApi.IServices
{
    public interface IParkingService
    {
        Task<ParkVehicleResponse> ParkVehicleAsync(ParkVehicleRequest request);
        Task<ParkingLotStatusResponse> GetParkingLotStatusAsync();
        Task<ExitVehicleResponse> ExitVehicleAsync(ExitVehicleRequest request);
    }
}