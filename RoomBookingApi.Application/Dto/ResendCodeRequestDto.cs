using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class ResendCodeRequestDto
    {
        [EmailAddress]
        [Required]
        public string Email { get; set; }
    }
}
