using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Domain.Enums;

namespace RoomBookingApi.Application.Dto
{
    public class BookingFilterDto
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public Status? Status { get; set; }
        public int? UserId { get; set; }
    }
}
