using System;
using System.Data;

namespace StockPortal.Core.Interfaces
{
    public interface IDapperContext
    {
        IDbConnection CreateConnection();
    }
}
