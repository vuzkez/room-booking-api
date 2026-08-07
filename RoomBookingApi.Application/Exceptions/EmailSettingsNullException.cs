using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Exceptions
{
    public class EmailSettingsNullException : Exception
    {
        public List<string?> Settings { get; }

        public EmailSettingsNullException(List<string?> settings)
            : base($"One or more email settings are missing: {string.Join(", ", settings)}")
        {
            Settings = settings;
        }
    }
}
