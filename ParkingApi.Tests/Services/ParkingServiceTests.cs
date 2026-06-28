using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ParkingApi.Data;
using ParkingApi.Models.Entities;
using ParkingApi.Models.Enums;
using ParkingApi.Models.Requests;
using ParkingApi.Services;

namespace ParkingApi.Tests.Services;

public class ParkingServiceTests
{
    #region Helpers

    private static ParkingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ParkingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ParkingDbContext(options);
    }

    private static void SeedLotWithSpaces(ParkingDbContext db, int capacity = 10)
    {
        var lot = new ParkingLot { Name = "Test Lot", Location = "Test", Capacity = capacity };
        db.ParkingLots.Add(lot);
        db.SaveChanges();

        var spaces = Enumerable.Range(1, capacity).Select(i => new ParkingSpace
        {
            SpaceNumber = i,
            IsOccupied = false,
            ParkingLotId = lot.ParkingLotId
        });
        db.ParkingSpaces.AddRange(spaces);
        db.SaveChanges();
    }

    #endregion

    #region ParkVehicleAsync

    [Fact]
    public async Task ParkVehicleAsync_ValidRequest_ReturnsCorrectResponse()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db);
        var service = new ParkingService(db);

        var result = await service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "AB12 CDE",
            VehicleType = VehicleType.Small
        });

        result.VehicleReg.Should().Be("AB12 CDE");
        result.SpaceNumber.Should().Be(1);
        result.TimeIn.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task ParkVehicleAsync_ValidRequest_MarksSpaceAsOccupied()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db);
        var service = new ParkingService(db);

        await service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "AB12 CDE",
            VehicleType = VehicleType.Small
        });

        var space = await db.ParkingSpaces.FirstAsync(s => s.VehicleReg == "AB12 CDE");
        space.IsOccupied.Should().BeTrue();
    }

    [Fact]
    public async Task ParkVehicleAsync_AssignsLowestAvailableSpaceNumber()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db, capacity: 3);
        var service = new ParkingService(db);

        var first  = await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "AA11 AAA", VehicleType = VehicleType.Small });
        var second = await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "BB22 BBB", VehicleType = VehicleType.Small });

        first.SpaceNumber.Should().Be(1);
        second.SpaceNumber.Should().Be(2);
    }

    [Fact]
    public async Task ParkVehicleAsync_UnknownVehicleType_ThrowsArgumentException()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db);
        var service = new ParkingService(db);

        Func<Task> act = () => service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "AB12 CDE",
            VehicleType = VehicleType.Unknown
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ParkVehicleAsync_OutOfRangeVehicleType_ThrowsArgumentException()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db);
        var service = new ParkingService(db);

        Func<Task> act = () => service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "AB12 CDE",
            VehicleType = (VehicleType)99
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ParkVehicleAsync_EmptyOrWhitespaceVehicleReg_ThrowsArgumentException(string vehicleReg)
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db);
        var service = new ParkingService(db);

        Func<Task> act = () => service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = vehicleReg,
            VehicleType = VehicleType.Small
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ParkVehicleAsync_VehicleAlreadyParked_ThrowsInvalidOperationException()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db);
        var service = new ParkingService(db);

        await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "AB12 CDE", VehicleType = VehicleType.Small });

        Func<Task> act = () => service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "AB12 CDE", VehicleType = VehicleType.Small });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ParkVehicleAsync_NoAvailableSpaces_ThrowsInvalidOperationException()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db, capacity: 1);
        var service = new ParkingService(db);

        await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "AB12 CDE", VehicleType = VehicleType.Small });

        Func<Task> act = () => service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "XY99 ZZZ", VehicleType = VehicleType.Medium });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region GetParkingLotStatusAsync

    [Fact]
    public async Task GetParkingLotStatusAsync_NoParkingLot_ThrowsKeyNotFoundException()
    {
        using var db = CreateContext();
        var service = new ParkingService(db);

        Func<Task> act = () => service.GetParkingLotStatusAsync();

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetParkingLotStatusAsync_NoVehiclesParked_ReturnsAllAvailable()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db, capacity: 10);
        var service = new ParkingService(db);

        var result = await service.GetParkingLotStatusAsync();

        result.AvailableSpaces.Should().Be(10);
        result.OccupiedSpaces.Should().Be(0);
    }

    [Fact]
    public async Task GetParkingLotStatusAsync_SomeVehiclesParked_ReturnsCorrectCounts()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db, capacity: 10);
        var service = new ParkingService(db);

        await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "AA11 AAA", VehicleType = VehicleType.Small });
        await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "BB22 BBB", VehicleType = VehicleType.Medium });
        await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "CC33 CCC", VehicleType = VehicleType.Large });

        var result = await service.GetParkingLotStatusAsync();

        result.AvailableSpaces.Should().Be(7);
        result.OccupiedSpaces.Should().Be(3);
    }

    [Fact]
    public async Task GetParkingLotStatusAsync_AllSpacesOccupied_ReturnsZeroAvailable()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db, capacity: 2);
        var service = new ParkingService(db);

        await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "AA11 AAA", VehicleType = VehicleType.Small });
        await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "BB22 BBB", VehicleType = VehicleType.Small });

        var result = await service.GetParkingLotStatusAsync();

        result.AvailableSpaces.Should().Be(0);
        result.OccupiedSpaces.Should().Be(2);
    }

    #endregion

    #region ExitVehicleAsync

    [Fact]
    public async Task ExitVehicleAsync_VehicleNotParked_ThrowsKeyNotFoundException()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db);
        var service = new ParkingService(db);

        Func<Task> act = () => service.ExitVehicleAsync(new ExitVehicleRequest { VehicleReg = "XX99 ZZZ" });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ExitVehicleAsync_ParkedVehicle_ReturnsCorrectVehicleReg()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db);
        var service = new ParkingService(db);

        await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "AB12 CDE", VehicleType = VehicleType.Small });
        var result = await service.ExitVehicleAsync(new ExitVehicleRequest { VehicleReg = "AB12 CDE" });

        result.VehicleReg.Should().Be("AB12 CDE");
        result.TimeOut.Should().BeOnOrAfter(result.TimeIn);
    }

    [Fact]
    public async Task ExitVehicleAsync_ParkedVehicle_FreesSpace()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db);
        var service = new ParkingService(db);

        await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "AB12 CDE", VehicleType = VehicleType.Small });
        await service.ExitVehicleAsync(new ExitVehicleRequest { VehicleReg = "AB12 CDE" });

        var space = await db.ParkingSpaces.FirstAsync(s => s.SpaceNumber == 1);
        space.IsOccupied.Should().BeFalse();
        space.VehicleReg.Should().BeEmpty();
    }

    [Fact]
    public async Task ExitVehicleAsync_FreedSpace_CanBeReusedByAnotherVehicle()
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db, capacity: 1);
        var service = new ParkingService(db);

        await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "AB12 CDE", VehicleType = VehicleType.Small });
        await service.ExitVehicleAsync(new ExitVehicleRequest { VehicleReg = "AB12 CDE" });

        var result = await service.ParkVehicleAsync(new ParkVehicleRequest { VehicleReg = "XY99 ZZZ", VehicleType = VehicleType.Large });
        result.VehicleReg.Should().Be("XY99 ZZZ");
    }

    // Charge = totalMinutes * rate + floor(totalMinutes / 5) * 1.0, rounded to 2dp
    // Tests set TimeIn directly on the space to control duration precisely.
    [Theory]
    [InlineData(1, 10, 3.00)]  // Small  (£0.10/min): 10 * 0.10 + floor(10/5) * 1 = 1.00 + 2.00 = 3.00
    [InlineData(2, 10, 4.00)]  // Medium (£0.20/min): 10 * 0.20 + floor(10/5) * 1 = 2.00 + 2.00 = 4.00
    [InlineData(3, 10, 6.00)]  // Large  (£0.40/min): 10 * 0.40 + floor(10/5) * 1 = 4.00 + 2.00 = 6.00
    [InlineData(1,  4, 0.40)]  // Small, 4 min: no bonus                           = 0.40 + 0.00 = 0.40
    [InlineData(1,  5, 1.50)]  // Small, 5 min: first bonus triggers               = 0.50 + 1.00 = 1.50
    [InlineData(1, 15, 4.50)]  // Small, 15 min: 3 bonuses                         = 1.50 + 3.00 = 4.50
    public async Task ExitVehicleAsync_CalculatesChargeCorrectly(int vehicleTypeInt, int minutes, double expectedCharge)
    {
        using var db = CreateContext();
        SeedLotWithSpaces(db);
        var service = new ParkingService(db);

        // Set TimeIn directly to control parking duration precisely
        var space = db.ParkingSpaces.First(s => s.SpaceNumber == 1);
        space.VehicleReg = "AB12 CDE";
        space.VehicleType = (VehicleType)vehicleTypeInt;
        space.TimeIn = DateTime.UtcNow.AddMinutes(-minutes);
        space.IsOccupied = true;
        db.SaveChanges();

        var result = await service.ExitVehicleAsync(new ExitVehicleRequest { VehicleReg = "AB12 CDE" });

        result.VehicleCharge.Should().BeApproximately(expectedCharge, precision: 0.01);
    }

    #endregion
}
