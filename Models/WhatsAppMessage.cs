namespace location_img_poc.Models
{
    public class WhatsAppMessage
    {
        public int Id { get; set; }
        public string? From { get; set; }
        public string? Type { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
