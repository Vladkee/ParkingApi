using Microsoft.AspNetCore.Mvc;
using ParkingApi.IServices;
using ParkingApi.Models.Requests;

namespace ParkingApi.Controllers
{
    [ApiController]
    [Route("parking")]
    public class ParkingController : ControllerBase
    {
        private readonly IParkingService _parkingService;

        public ParkingController(IParkingService parkingService)
        {
            _parkingService = parkingService;
        }

        [HttpPost]
        // POST /parking - Parks a given vehicle in the first available space and returns the vehicle and its space number.
        public async Task<IActionResult> ParkVehicle([FromBody] ParkVehicleRequest request)
        {
            try
            {
                var result = await _parkingService.ParkVehicleAsync(request);
                return CreatedAtAction(nameof(ParkVehicle), result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpGet]
        // GET /parking - Gets available and occupied number of spaces.
        public async Task<IActionResult> GetParkingLotStatus()
        {
            try
            {
                var result = await _parkingService.GetParkingLotStatusAsync();
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpPost("exit")]
        // POST /parking/exit - Should free up this vehicles space and return its final charge from its parking time until now
        public async Task<IActionResult> ExitVehicle([FromBody] ExitVehicleRequest request)
        {
            try
            {
                var result = await _parkingService.ExitVehicleAsync(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }
    }
}