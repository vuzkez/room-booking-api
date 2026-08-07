using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class OccupancyDayStatDto
    {
        public DayOfWeek Day { get; set; }
        public double OccupancyPercent { get; set; }
    }
}
