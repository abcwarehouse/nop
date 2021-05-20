using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Data.Migrations;
using Nop.Data.DataProviders;
using LinqToDB.Mapping;
using LinqToDB.Tools;
using System.Data;

namespace Nop.Plugin.Misc.AbcCore.Data
{
    public partial class CustomMsSqlDataProvider : MsSqlNopDataProvider, ICustomNopDataProvider
    {
        public async Task<int> ExecuteNonQueryAsync(
            string sql,
            int timeout,
            params DataParameter[] dataParameters)
        {
            using var dataContext = await CreateDataConnectionAsync();
            var command = new CommandInfo(dataContext, sql, dataParameters);
            var affectedRecords = await command.ExecuteAsync();
            UpdateOutputParameters(dataContext, dataParameters);
            return affectedRecords;
        }

        // this is duplicated from BaseDataProvider.cs
        private void UpdateOutputParameters(
            DataConnection dataConnection,
            DataParameter[] dataParameters)
        {
            if (dataParameters is null || dataParameters.Length == 0)
                return;

            foreach (var dataParam in dataParameters.Where(p => p.Direction == ParameterDirection.Output))
            {
                UpdateParameterValue(dataConnection, dataParam);
            }
        }

        // this is duplicated from BaseDataProvider.cs
        private void UpdateParameterValue(DataConnection dataConnection, DataParameter parameter)
        {
            if (dataConnection is null)
                throw new ArgumentNullException(nameof(dataConnection));

            if (parameter is null)
                throw new ArgumentNullException(nameof(parameter));

            if (dataConnection.Command is IDbCommand command &&
                command.Parameters.Count > 0 &&
                command.Parameters.Contains(parameter.Name) &&
                command.Parameters[parameter.Name] is IDbDataParameter param)
            {
                parameter.Value = param.Value;
            }
        }
    }
}
