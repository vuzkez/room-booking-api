using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class TopUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int BookingCount { get; set; }
    }
}
