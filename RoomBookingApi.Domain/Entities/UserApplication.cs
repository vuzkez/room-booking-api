using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace RoomBookingApi.Domain.Entities
{
    public class UserApplication : IdentityUser<int>
    {
    }
}
