using System;
using System.Data;

namespace Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate
{
    public partial class ImportShopsTask
    {
        private class BackendPhysicalStore
        {
            public const string IsAbc = "IsAbc";
            public const string IsHaw = "IsHaw";

            private const string branchId = "BRANCH_ID";
            private const string branchName = "BRANCH_NAME";
            private const string address = "ADDRESS";
            private const string city = "CITY";
            private const string state = "STATE";
            private const string zip = "ZIP";
            private const string phone = "PHONE";
            private const string email = "EMAIL";
            private const string storeType = "STORE_TYPE";

            public string BranchId { get; private set; }
            public string BranchName { get; private set; }
            public string Address { get; private set; }
            public string City { get; private set; }
            public string State { get; private set; }
            public string Zip { get; private set; }
            public string Phone { get; private set; }
            public string Email { get; private set; }
            public PhysicalStoreType StoreType { get; private set; }

            public static string SelectStmt
            {
                get
                {
                    return $@"
                        SELECT {branchId}, {branchName}, {address}, {city}, {state}, {zip}, {phone}, {email}, '{IsAbc}' AS {storeType} FROM DA1_BRANCH_INFO
                        UNION
                        SELECT {branchId}, {branchName}, {address}, {city}, {state}, {zip}, {phone}, {email}, '{IsHaw}' AS {storeType} FROM DA1_BRANCH_HAW;
                    ";
                }
            }

            public static BackendPhysicalStore FromDataReader(IDataReader reader)
            {
                return new BackendPhysicalStore(reader);
            }

            private BackendPhysicalStore(IDataReader reader)
            {
                BranchId = reader[branchId] as string;
                BranchName = reader[branchName] as string;
                // Remove everything after ' - '
                BranchName = BranchName.IndexOf(" - ") > 0 ? BranchName.Substring(0, BranchName.IndexOf(" - ")) : BranchName;
                Address = reader[address] as string;
                City = reader[city] as string;
                State = reader[state] as string;
                Zip = reader[zip] as string;
                Phone = reader[phone] as string;
                Email = reader[email] as string;
                StoreType = (PhysicalStoreType)Enum.Parse(typeof(PhysicalStoreType), reader[storeType] as string);
            }
        }

        enum PhysicalStoreType
        {
            IsAbc,
            IsHaw
        }
    }
}