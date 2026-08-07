using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class CreateBookingRequestDto
    {
        public int RoomId { get; set; }
        public DateTime StartTime {  get; set; }
        public DateTime EndTime { get; set; }
    }
}
