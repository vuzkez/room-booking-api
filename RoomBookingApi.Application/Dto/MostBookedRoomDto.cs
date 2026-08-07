using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class MostBookedRoomDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int BookingCount { get; set; }
    }
}
