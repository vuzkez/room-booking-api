using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Exceptions
{
    public class JwtSettingsNullException : Exception
    {
        public List<string?> Settings { get; }

        public JwtSettingsNullException(List<string?> settings)
            : base($"One or more JWT settings are missing: {string.Join(", ", settings)}")
        {
            Settings = settings;
        }
    }
}
