using System;
using System.Collections.Generic;
using System.Text;
using RoomBookingApi.Domain.Enums;

namespace RoomBookingApi.Domain.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public Room Room { get; set; }
        public int UserId { get; set; }
        public UserApplication User {  get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalPrice { get; set; }
        public int Version { get; set; }
        public Status Status { get; set; }
    }
}
