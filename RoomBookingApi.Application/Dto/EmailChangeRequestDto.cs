using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class EmailChangeRequestDto
    {
        [Required]
        [EmailAddress]
        public string NewEmail { get; set; } = null!;
    }
}
