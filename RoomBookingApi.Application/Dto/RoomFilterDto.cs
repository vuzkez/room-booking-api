using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class RoomFilterDto
    {
        public int? MinCapacity { get; set; }
        public string? Location { get; set; }
        public bool? IsActive { get; set; }
    }
}
