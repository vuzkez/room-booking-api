using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string? Error { get; set; } = null;
        public string? Token { get; set; } = null;
    }
}
