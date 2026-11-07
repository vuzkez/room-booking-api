using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Dto
{
    public class ConfirmEmailChangeDto
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        [RegularExpression("^[0-9]{6}$")]
        public string Code { get; set; } = null!;
    }
}
