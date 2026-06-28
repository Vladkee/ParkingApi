using Microsoft.EntityFrameworkCore;
using ParkingApi.Data;
using ParkingApi.IServices;
using ParkingApi.Models.Entities;
using ParkingApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<ParkingDbContext>(options =>
    options.UseInMemoryDatabase("ParkingDb"));

builder.Services.AddScoped<IParkingService, ParkingService>();

builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Data initialization.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ParkingDbContext>();

    if (!db.ParkingLots.Any())
    {
        var lot = new ParkingLot
        {
            Name = "Main Car Park",
            Location = "Main Street",
            Capacity = 10
        };
        db.ParkingLots.Add(lot);
        db.SaveChanges();

        var spaces = Enumerable.Range(1, lot.Capacity).Select(i => new ParkingSpace
        {
            SpaceNumber = i,
            IsOccupied = false,
            ParkingLotId = lot.ParkingLotId
        });
        db.ParkingSpaces.AddRange(spaces);
        db.SaveChanges();
    }
}

app.Run();