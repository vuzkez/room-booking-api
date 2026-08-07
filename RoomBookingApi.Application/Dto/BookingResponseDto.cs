using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Domain.Enums;

namespace RoomBookingApi.Application.Dto
{
    public class BookingResponseDto
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal TotalPrice { get; set; }
        public Status Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
