using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Application.Interfaces.Strategies
{
    public interface IPricingStrategy
    {
        /// <summary>
        /// Рассчитывает стоимость аренды комнаты за указанный период.
        /// </summary>
        /// <param name="room">Объект комнаты (содержит цену за час).</param>
        /// <param name="start">Начало бронирования (UTC).</param>
        /// <param name="end">Конец бронирования (UTC).</param>
        /// <returns>Итоговая стоимость в десятичном формате.</returns>
        decimal CalculatePrice(Room room, DateTime start, DateTime end);
    }
}
