using System;
using System.Collections.Generic;
using System.Text;

namespace RoomBookingApi.Domain.Entities
{
    public class Room
    {
        public int Id { get; set; }
        public string RoomName { get; set; }
        public int Capacity { get; set; }
        public string Location { get; set; }
        public decimal PricePerHour { get; set; }
        public bool IsActive { get; set; }
        public ICollection<Booking> Bookings { get; set; }
    }
}
