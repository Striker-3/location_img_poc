using System;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Net.Http.Json;
using location_img_poc.Models;

namespace location_img_poc.Services
{
    public class WhatsAppService
    {
        private readonly string? _connectionString;
        private readonly string? _accessToken;
        private readonly string? _phoneNumberId;

        public WhatsAppService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _accessToken = config["WhatsApp:AccessToken"];
            _phoneNumberId = config["WhatsApp:PhoneNumberId"];
        }

        // 🧩 Parse incoming webhook message
        public WhatsAppMessage? ParseMessage(JsonElement data)
        {
            try
            {
                var message = data.GetProperty("entry")[0]
                                  .GetProperty("changes")[0]
                                  .GetProperty("value")
                                  .GetProperty("messages")[0];

                var msgType = message.GetProperty("type").GetString();
                var from = message.GetProperty("from").GetString();
                var result = new WhatsAppMessage { From = from, Type = msgType };

                if (msgType == "text")
                {
                    string text = message.GetProperty("text").GetProperty("body").GetString();
                    result.Text = text;

                    if (text.Contains("address", StringComparison.OrdinalIgnoreCase) ||
                        text.Contains("location", StringComparison.OrdinalIgnoreCase) ||
                        text.Contains("place", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Address = text;
                        result.Type = "text_address";
                    }
                }
                else if (msgType == "image")
                {
                    result.ImageUrl = message.GetProperty("image").GetProperty("id").GetString();
                }
                else if (msgType == "location")
                {
                    result.Latitude = message.GetProperty("location").GetProperty("latitude").GetDouble();
                    result.Longitude = message.GetProperty("location").GetProperty("longitude").GetDouble();

                    if (message.GetProperty("location").TryGetProperty("name", out var name))
                        result.Address = name.GetString();
                    else if (message.GetProperty("location").TryGetProperty("address", out var addr))
                        result.Address = addr.GetString();
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error parsing message: {ex.Message}");
                return null;
            }
        }

        // 💾 Save message to SQL
        public async Task SaveToDatabase(WhatsAppMessage msg)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    INSERT INTO WhatsAppMessages 
                    (Sender, Type, Text, ImageUrl, Latitude, Longitude, Address, Timestamp)
                    VALUES (@From, @Type, @Text, @ImageUrl, @Lat, @Lng, @Addr, @Ts)", conn);

                cmd.Parameters.AddWithValue("@From", msg.From ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Type", msg.Type ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Text", msg.Text ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ImageUrl", msg.ImageUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Lat", msg.Latitude ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Lng", msg.Longitude ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Addr", msg.Address ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Ts", msg.Timestamp);

                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Message saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Database error: {ex.Message}");
            }
        }

        // 💬 Send WhatsApp reply
        public async Task SendReply(string to, string message)
        {
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_phoneNumberId))
            {
                Console.WriteLine("⚠️ Missing WhatsApp credentials.");
                return;
            }

            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                var body = new
                {
                    messaging_product = "whatsapp",
                    to,
                    type = "text",
                    text = new { body = message }
                };

                var url = $"https://graph.facebook.com/v19.0/{_phoneNumberId}/messages";
                var response = await http.PostAsJsonAsync(url, body);

                if (response.IsSuccessStatusCode)
                    Console.WriteLine($"✅ Reply sent to {to}");
                else
                    Console.WriteLine($"❌ Failed to send reply: {await response.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error sending reply: {ex.Message}");
            }
        }
    }
}
