using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Options
{
    public class JwtSettings
    {
        [Required] public string Key { get; set; } = string.Empty;

        [Required] public string Issuer { get; set; } = string.Empty;

        [Required] public string Audience { get; set; } = string.Empty;

        [Range(1, 1440)] public int ExpiryInMinutes { get; set; }
    }
}
