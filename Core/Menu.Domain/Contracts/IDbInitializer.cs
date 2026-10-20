using System;
using System.Collections.Generic;
using System.Text;

namespace Menu.Domain.Contracts
{
    public interface IDbInitializer
    {
        Task InitializeAsync();
    }
}
