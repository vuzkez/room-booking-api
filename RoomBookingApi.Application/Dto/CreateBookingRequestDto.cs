using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class CreateBookingRequestDto
    {
        [Range(1, int.MaxValue)]
        public required int RoomId { get; set; }
        public required DateTimeOffset StartTime { get; set; }
        public required DateTimeOffset EndTime { get; set; }
    }
}
