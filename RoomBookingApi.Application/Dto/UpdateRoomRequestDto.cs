using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class UpdateRoomRequestDto
    {
        public string Name { get; set; }
        public int Capacity { get; set; }
        public string Location { get; set; }
        public decimal PricePerHour { get; set; }
        public bool IsActive { get; set; }
    }
}
