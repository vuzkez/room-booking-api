using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Options
{
    public class EmailSettings
    {
        [Required] public string Server { get; set; } = string.Empty;
        [Required] public string Port { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        [Required] public string SenderEmail { get; set; } = string.Empty;
        [Required] public string SenderName { get; set; } = string.Empty;
    }
}
