using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Exceptions
{
    public class BookingConflictException : Exception
    {
        public BookingConflictException()
            : base("The room has already been booked.") { }

        public BookingConflictException(string message)
            : base(message) { }

        public BookingConflictException(string message, Exception innerException)
            : base(message, innerException) { }

        public BookingConflictException(Exception innerException)
            : base("The room has already been booked.", innerException) { }
    }
}
