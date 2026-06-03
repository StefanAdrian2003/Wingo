namespace Proiect_Licenta.Services
{
    public static class VoucherTemplates
    {
        public static readonly List<(string Name, string Description, int DiscountPercent)> Templates = new()
        {
            ("10% Flight Discount", "Get 10% off next flight", 10),
            ("15% Flight Discount", "Get 15% off next flight", 15),
            ("20% Flight Discount", "Get 20% off next flight", 20),
            ("25% Flight Discount", "Get 25% off next flight", 25),
            ("30% Flight Discount", "Get 30% off next flight", 30),
            ("35% Flight Discount", "Get 35% off next flight", 35),
            ("40% Flight Discount", "Get 40% off next flight", 40)
        };
    }
}
