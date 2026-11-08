using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class ConfirmEmailRequestDto
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        [RegularExpression("^[0-9]{6}$")]
        public string Code { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; }

    }
}
