using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Models;
using EcommerceApi.Data;
namespace EcommerceApi.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        var categoryNames = new[]
        {
            "Electronics",
            "Books",
            "Clothing",
            "Home & Kitchen",
            "Sports",
            "Beauty",
            "Toys",
            "Automotive",
            "Grocery",
            "Office Supplies",
            "Garden",
            "Pet Supplies"
        };

        var existingCategories = db.Categories.ToList();
        var categoriesByName = existingCategories.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var name in categoryNames)
        {
            if (!categoriesByName.ContainsKey(name))
            {
                var cat = new Category { Name = name };
                db.Categories.Add(cat);
                categoriesByName[name] = cat;
            }
        }
        db.SaveChanges();

        var productsToSeed = new List<(string Name, string? Description, decimal Price, int Stock, string CategoryName)>
        {
            ("Wireless Mouse", "Ergonomic 2.4GHz", 19.99m, 120, "Electronics"),
            ("Mechanical Keyboard", "RGB backlit", 59.99m, 75, "Electronics"),
            ("Noise-Canceling Headphones", "Over-ear", 129.50m, 40, "Electronics"),
            ("USB-C Charger", "65W fast charging", 29.99m, 150, "Electronics"),
            ("4K Monitor", "27-inch IPS", 299.00m, 25, "Electronics"),

            ("C# in Depth", "Programming book", 39.00m, 30, "Books"),
            ("Clean Code", "Robert C. Martin", 34.99m, 45, "Books"),
            ("The Pragmatic Programmer", "20th Anniversary", 37.50m, 40, "Books"),
            ("Design Patterns", "GoF", 44.99m, 35, "Books"),
            ("Refactoring", "Improving Design", 42.00m, 20, "Books"),

            ("Classic T-Shirt", "100% cotton", 12.50m, 200, "Clothing"),
            ("Jeans", "Slim fit", 49.90m, 60, "Clothing"),
            ("Hoodie", "Fleece", 29.99m, 80, "Clothing"),
            ("Sneakers", "Comfort running", 69.99m, 50, "Clothing"),
            ("Baseball Cap", "Adjustable", 14.99m, 120, "Clothing"),

            ("Nonstick Pan", "12-inch", 24.99m, 100, "Home & Kitchen"),
            ("Chef Knife", "8-inch stainless", 39.99m, 70, "Home & Kitchen"),
            ("Coffee Maker", "Drip 12-cup", 59.99m, 40, "Home & Kitchen"),
            ("Vacuum Cleaner", "Bagless", 129.99m, 35, "Home & Kitchen"),
            ("Air Fryer", "4-quart", 89.99m, 45, "Home & Kitchen"),

            ("Yoga Mat", "Non-slip", 19.99m, 100, "Sports"),
            ("Dumbbell Set", "Adjustable", 79.99m, 30, "Sports"),
            ("Tennis Racket", "Graphite", 99.99m, 25, "Sports"),
            ("Soccer Ball", "Size 5", 24.99m, 80, "Sports"),
            ("Water Bottle", "Insulated 1L", 17.99m, 150, "Sports"),

            ("Face Cleanser", "Gentle foaming", 12.99m, 200, "Beauty"),
            ("Moisturizer", "SPF 30", 18.99m, 180, "Beauty"),
            ("Shampoo", "Sulfate-free", 9.99m, 220, "Beauty"),
            ("Hair Dryer", "Ionic", 39.99m, 70, "Beauty"),
            ("Makeup Palette", "12 shades", 22.99m, 90, "Beauty"),

            ("Building Blocks", "120 pcs", 29.99m, 100, "Toys"),
            ("RC Car", "Rechargeable", 49.99m, 60, "Toys"),
            ("Puzzle", "1000 pieces", 19.99m, 120, "Toys"),
            ("Doll", "12-inch", 24.99m, 80, "Toys"),
            ("Board Game", "Family", 34.99m, 70, "Toys"),

            ("Car Phone Mount", "Magnetic", 14.99m, 150, "Automotive"),
            ("Jump Starter", "Portable", 89.99m, 40, "Automotive"),
            ("Tire Inflator", "12V", 39.99m, 60, "Automotive"),
            ("Car Vacuum", "Handheld", 29.99m, 80, "Automotive"),
            ("Seat Covers", "Universal", 59.99m, 30, "Automotive"),

            ("Organic Pasta", "500g", 3.99m, 300, "Grocery"),
            ("Olive Oil", "Extra virgin", 9.99m, 200, "Grocery"),
            ("Granola", "Honey almond", 6.49m, 180, "Grocery"),
            ("Dark Chocolate", "70% cocoa", 4.49m, 250, "Grocery"),
            ("Ground Coffee", "Medium roast", 8.99m, 160, "Grocery"),

            ("Notebook", "A5 ruled", 2.99m, 500, "Office Supplies"),
            ("Gel Pens", "10-pack", 4.99m, 400, "Office Supplies"),
            ("Stapler", "Standard", 6.99m, 150, "Office Supplies"),
            ("Desk Organizer", "Mesh", 12.99m, 120, "Office Supplies"),
            ("Printer Paper", "500 sheets", 5.99m, 600, "Office Supplies"),

            ("Garden Hose", "50 ft", 24.99m, 80, "Garden"),
            ("Pruning Shears", "Bypass", 14.99m, 120, "Garden"),
            ("Planter Pots", "Set of 3", 19.99m, 100, "Garden"),
            ("Lawn Seeds", "Sun & shade", 29.99m, 90, "Garden"),
            ("Watering Can", "2 gallon", 16.99m, 140, "Garden"),

            ("Dog Leash", "6 ft nylon", 9.99m, 200, "Pet Supplies"),
            ("Cat Litter", "Clumping", 14.99m, 180, "Pet Supplies"),
            ("Pet Bed", "Medium", 29.99m, 100, "Pet Supplies"),
            ("Dog Treats", "Chicken", 7.99m, 220, "Pet Supplies"),
            ("Cat Scratcher", "Cardboard", 12.99m, 160, "Pet Supplies")
        };

        var existingProductNames = new HashSet<string>(db.Products.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
        var newProducts = new List<Product>();
        foreach (var p in productsToSeed)
        {
            if (existingProductNames.Contains(p.Name))
                continue;

            if (!categoriesByName.TryGetValue(p.CategoryName, out var cat))
                continue;

            newProducts.Add(new Product
            {
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                CategoryId = cat.Id
            });
        }

        if (newProducts.Count > 0)
        {
            db.Products.AddRange(newProducts);
            db.SaveChanges();
        }
    }
}
