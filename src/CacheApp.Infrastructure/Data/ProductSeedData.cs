using CacheApp.Domain.Products;

namespace CacheApp.Infrastructure.Data;

public static class ProductSeedData
{
    public static Product[] Products =>
    [
        new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Wireless Mouse", "Ergonomic wireless mouse", 24.99m, 48),
        new(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Mechanical Keyboard", "Compact mechanical keyboard", 89.00m, 22),
        new(Guid.Parse("10000000-0000-0000-0000-000000000003"), "USB-C Hub", "Seven-port USB-C hub", 42.50m, 35),
        new(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Webcam", "1080p USB webcam", 59.99m, 18),
        new(Guid.Parse("10000000-0000-0000-0000-000000000005"), "Monitor Stand", "Adjustable aluminum stand", 39.95m, 16),
        new(Guid.Parse("10000000-0000-0000-0000-000000000006"), "Desk Lamp", "Dimmable LED desk lamp", 31.25m, 27),
        new(Guid.Parse("10000000-0000-0000-0000-000000000007"), "Laptop Sleeve", "Protective 14-inch sleeve", 19.50m, 41),
        new(Guid.Parse("10000000-0000-0000-0000-000000000008"), "Bluetooth Speaker", "Portable stereo speaker", 54.00m, 13),
        new(Guid.Parse("10000000-0000-0000-0000-000000000009"), "Noise-Canceling Headphones", "Over-ear wireless headphones", 129.99m, 11),
        new(Guid.Parse("10000000-0000-0000-0000-000000000010"), "USB Microphone", "Cardioid USB microphone", 74.95m, 9),
        new(Guid.Parse("10000000-0000-0000-0000-000000000011"), "Portable SSD", "1 TB portable solid-state drive", 109.00m, 24),
        new(Guid.Parse("10000000-0000-0000-0000-000000000012"), "Wireless Charger", "15 W charging pad", 22.00m, 31),
        new(Guid.Parse("10000000-0000-0000-0000-000000000013"), "Laptop Riser", "Foldable laptop riser", 35.75m, 20),
        new(Guid.Parse("10000000-0000-0000-0000-000000000014"), "HDMI Cable", "2 m high-speed HDMI cable", 12.99m, 63),
        new(Guid.Parse("10000000-0000-0000-0000-000000000015"), "DisplayPort Cable", "1.8 m DisplayPort cable", 14.50m, 38),
        new(Guid.Parse("10000000-0000-0000-0000-000000000016"), "Desk Mat", "Large non-slip desk mat", 18.95m, 29),
        new(Guid.Parse("10000000-0000-0000-0000-000000000017"), "USB-C Cable", "1 m braided USB-C cable", 9.99m, 75),
        new(Guid.Parse("10000000-0000-0000-0000-000000000018"), "Ergonomic Footrest", "Adjustable under-desk footrest", 28.50m, 14),
        new(Guid.Parse("10000000-0000-0000-0000-000000000019"), "Power Strip", "Six-outlet surge protector", 26.99m, 33),
        new(Guid.Parse("10000000-0000-0000-0000-000000000020"), "Cable Organizer", "Reusable cable clips set", 8.75m, 58)
    ];
}
