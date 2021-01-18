using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueDeck.Models
{
    interface IBaseModel<T>
    {
        T Default();
    }
}
