using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CacheApp.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class _20261117074448_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                StockQuantity = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "Products",
            columns: new[] { "Id", "Description", "Name", "Price", "StockQuantity" },
            values: new object[,]
            {
                { new Guid("10000000-0000-0000-0000-000000000001"), "Ergonomic wireless mouse", "Wireless Mouse", 24.99m, 48 },
                { new Guid("10000000-0000-0000-0000-000000000002"), "Compact mechanical keyboard", "Mechanical Keyboard", 89.00m, 22 },
                { new Guid("10000000-0000-0000-0000-000000000003"), "Seven-port USB-C hub", "USB-C Hub", 42.50m, 35 },
                { new Guid("10000000-0000-0000-0000-000000000004"), "1080p USB webcam", "Webcam", 59.99m, 18 },
                { new Guid("10000000-0000-0000-0000-000000000005"), "Adjustable aluminum stand", "Monitor Stand", 39.95m, 16 },
                { new Guid("10000000-0000-0000-0000-000000000006"), "Dimmable LED desk lamp", "Desk Lamp", 31.25m, 27 },
                { new Guid("10000000-0000-0000-0000-000000000007"), "Protective 14-inch sleeve", "Laptop Sleeve", 19.50m, 41 },
                { new Guid("10000000-0000-0000-0000-000000000008"), "Portable stereo speaker", "Bluetooth Speaker", 54.00m, 13 },
                { new Guid("10000000-0000-0000-0000-000000000009"), "Over-ear wireless headphones", "Noise-Canceling Headphones", 129.99m, 11 },
                { new Guid("10000000-0000-0000-0000-000000000010"), "Cardioid USB microphone", "USB Microphone", 74.95m, 9 },
                { new Guid("10000000-0000-0000-0000-000000000011"), "1 TB portable solid-state drive", "Portable SSD", 109.00m, 24 },
                { new Guid("10000000-0000-0000-0000-000000000012"), "15 W charging pad", "Wireless Charger", 22.00m, 31 },
                { new Guid("10000000-0000-0000-0000-000000000013"), "Foldable laptop riser", "Laptop Riser", 35.75m, 20 },
                { new Guid("10000000-0000-0000-0000-000000000014"), "2 m high-speed HDMI cable", "HDMI Cable", 12.99m, 63 },
                { new Guid("10000000-0000-0000-0000-000000000015"), "1.8 m DisplayPort cable", "DisplayPort Cable", 14.50m, 38 },
                { new Guid("10000000-0000-0000-0000-000000000016"), "Large non-slip desk mat", "Desk Mat", 18.95m, 29 },
                { new Guid("10000000-0000-0000-0000-000000000017"), "1 m braided USB-C cable", "USB-C Cable", 9.99m, 75 },
                { new Guid("10000000-0000-0000-0000-000000000018"), "Adjustable under-desk footrest", "Ergonomic Footrest", 28.50m, 14 },
                { new Guid("10000000-0000-0000-0000-000000000019"), "Six-outlet surge protector", "Power Strip", 26.99m, 33 },
                { new Guid("10000000-0000-0000-0000-000000000020"), "Reusable cable clips set", "Cable Organizer", 8.75m, 58 }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Products");
    }
}
