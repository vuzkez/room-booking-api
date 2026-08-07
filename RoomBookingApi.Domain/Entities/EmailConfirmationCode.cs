using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Domain.Entities
{
    public class EmailConfirmationCode
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public UserApplication User { get; set; }
        public string Code { get; set; }
        public DateTime ExpiresAt { get; set; } 
        public bool IsUsed { get; set; }
    }
}
