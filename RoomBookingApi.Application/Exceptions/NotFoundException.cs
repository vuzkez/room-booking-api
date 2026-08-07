using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomBookingApi.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string entityName, int id)
            : base($"Object: {entityName} not found by Id: {id}")
        {
        }

        public NotFoundException(string entityName, string fieldName, object fieldValue)
            : base($"{entityName} not found by {fieldName} = {fieldValue}")
        { }
    }
}
