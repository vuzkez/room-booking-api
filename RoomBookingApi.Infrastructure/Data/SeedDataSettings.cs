using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Infrastructure.Data
{
    public class SeedDataSettings
    {
        [EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;
        [MinLength(8)]
        public string AdminPassword { get; set; } = string.Empty;
        public string AdminName { get; set; } = "Admin";

        public string[] Roles { get; set; } = Array.Empty<string>();
    }
}
