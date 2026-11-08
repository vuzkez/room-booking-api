using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class UpdateRoomRequestDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Range(1, 100000)]
        public int Capacity { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; }

        [Range(1, 1000000)]
        public decimal PricePerHour { get; set; }
        public required bool IsActive { get; set; }

    }
}
